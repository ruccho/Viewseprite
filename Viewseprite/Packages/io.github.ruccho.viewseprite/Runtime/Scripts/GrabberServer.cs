using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace Viewseprite
{
    public class GrabberServer : IDisposable
    {
        public static GrabberServer Instance { get; private set; }

        public static GrabberServer GetOrCreateInstance()
        {
            return Instance ?? new GrabberServer();
        }

        private SynchronizationContext UnityMainThread { get; }

        private WebSocketServer Server { get; }

        private readonly List<GrabberSession> sessions = new List<GrabberSession>();

        public IReadOnlyList<GrabberSession> Sessions => sessions;

        public event Action<GrabberSession> OnNewSession;

        private static readonly int MaxWidth = 1024;

        private static readonly int MaxHeight = 1024;
        private static readonly int HeaderSize = 12;
        private static readonly int BytesPerPixel = 4;

        GrabberServer()
        {
            if (Instance != null) throw new InvalidOperationException();
            Instance = this;
            UnityMainThread = SynchronizationContext.Current;

            Server = new WebSocketServer("ws://localhost:6435");
            Server.AddWebSocketService("/", () =>
            {
                Debug.Log("[Viewseprite] New session");
                var session = new GrabberSession(UnityMainThread, (s) => { sessions.Remove(s); });
                sessions.Add(session);
                SetupNewSession(session);
                return session;
            });
            Server.Start();
            Debug.Log("[Viewseprite] Server has been started.");
        }

        public void Dispose()
        {
            Instance = null;
            Server.Stop();
        }

        private async Task SetupNewSession(GrabberSession session)
        {
            try
            {
                await session.WaitForHandshake();
                OnNewSession?.Invoke(session);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                throw;
            }
        }
    }


    public class GrabberSession : WebSocketBehavior
    {
        private Action<GrabberSession> OnClosed { get; }

        private readonly List<GrabberChannel> channels = new List<GrabberChannel>();

        public string[] Layers { get; private set; }
        private SynchronizationContext UnityMainThread { get; }

        public GrabberSession(SynchronizationContext unityMainThread, Action<GrabberSession> onClosed)
        {
            OnClosed = onClosed;
            UnityMainThread = unityMainThread;
        }

        private TaskCompletionSource<bool> waitForPingSource = default;
        public Task WaitForHandshake()
        {
            waitForPingSource ??= new TaskCompletionSource<bool>();
            return waitForPingSource.Task;
        }

        protected override void OnOpen()
        {
            Debug.Log("[Viewseprite] GrabberSession opened.");
        }

        protected override void OnClose(CloseEventArgs e)
        {
            Debug.Log("[Viewseprite] GrabberSession closed.");
            OnClosed?.Invoke(this);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Debug.LogException(e.Exception);
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            base.OnMessage(e);
            if (!e.IsBinary) return;

            //Debug.Log($"{e.RawData.Length} bytes");

            try
            {
                var id = BitConverter.ToUInt32(e.RawData, 0);

                //Debug.Log((char)id);

                if (id == 'P')
                {
                    waitForPingSource?.TrySetResult(true);
                }
                else if (id == 'I')
                {
                    var channelId = BitConverter.ToUInt32(e.RawData, 4);
                    var channel = channels.ElementAtOrDefault((int)channelId);
                    channel?.SetImage(e.RawData);
                }
                else if (id == 'M')
                {
                    var numLayers = BitConverter.ToUInt32(e.RawData, 4);
                    var numChannels = BitConverter.ToUInt32(e.RawData, 8);
                    string body = System.Text.Encoding.UTF8.GetString(e.RawData, 12, e.RawData.Length - 12);

                    if (Layers == null || Layers.Length != numLayers)
                    {
                        Layers = new string[numLayers];
                    }

                    string[] bodyLines = body.Split('\n');

                    Array.Copy(bodyLines, Layers, numLayers);

                    for (int i = 0; i < numChannels; i++)
                    {
                        if (ulong.TryParse(bodyLines[(int)numLayers + i], out ulong layerMask))
                        {
                            var channel = channels.ElementAtOrDefault(i);
                            channel?.SetLayerMaskWithoutNotify(layerMask);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                throw;
            }
        }

        public GrabberChannel CreateNewChannel()
        {
            var channel = new GrabberChannel(UnityMainThread, c =>
                {
                    var i = channels.IndexOf(c);
                    channels.RemoveAt(i);
                    SendRemoveChannelMessage(i);
                },
                (c, layerMask) =>
                {
                    var i = channels.IndexOf(c);
                    SendSetLayerMaskMessage(i, layerMask);
                });
            channels.Add(channel);

            SendAddChannelMessage();

            return channel;
        }

        private void SendAddChannelMessage()
        {
            byte[] message = new byte[4];
            Array.Copy(BitConverter.GetBytes((uint) 'A'), message, 4);
            Send(message);
        }

        private void SendRemoveChannelMessage(int index)
        {
            byte[] message = new byte[8];
            Array.Copy(BitConverter.GetBytes((uint) 'R'), message, 4);
            Array.Copy(BitConverter.GetBytes((uint) index), 0, message, 4, 4);
            Send(message);
        }

        private void SendSetLayerMaskMessage(int index, ulong layerMask)
        {
            byte[] message = new byte[16];
            Array.Copy(BitConverter.GetBytes((uint) 'L'), message, 4);
            Array.Copy(BitConverter.GetBytes((uint) index), 0, message, 4, 4);
            Array.Copy(BitConverter.GetBytes(layerMask), 0, message, 8, 4);
            Send(message);
        }
    }

    public class GrabberChannel : IDisposable
    {
        private ImageMessage Message { get; }
        private bool IsMessageDirty { get; set; } = false;
        private SynchronizationContext UnityMainThread { get; }
        private Texture2D Tex { get; set; }
        private Action<GrabberChannel> OnClosed { get; }
        private Action<GrabberChannel, ulong> SetLayerMask { get; }

        private ulong layerMask = 0;

        private ulong LayerMask
        {
            get => layerMask;
            set
            {
                //Debug.Log($"mask set to {value.ToString()}");
                layerMask = value;
                SetLayerMask(this, value);
            }
        }

        private bool isDisposed = false;

        public GrabberChannel(SynchronizationContext unityMainThread, Action<GrabberChannel> onClosed,
            Action<GrabberChannel, ulong> setLayerMask)
        {
            UnityMainThread = unityMainThread;
            OnClosed = onClosed;
            SetLayerMask = setLayerMask;
            Message = new ImageMessage();
        }

        public void SetLayer(int layer = -1)
        {
            if (layer == -1) LayerMask = 0;
            else LayerMask = 1u << layer;
        }

        public void SetLayerMaskWithoutNotify(ulong layerMask)
        {
            //Debug.Log($"mask set to {layerMask.ToString()}");
            this.layerMask = layerMask;
        }
        
        public void SetImage(byte[] messageBytes)
        {
            var width = BitConverter.ToUInt32(messageBytes, 8);
            var height = BitConverter.ToUInt32(messageBytes, 12);

            lock (Message)
            {
                Message.Set(width, height, messageBytes, 16, messageBytes.Length - 16);

                //Debug.Log($"width: {Message.Width}, height: {Message.Height}, bytes: {Message.Length}");
            }

            IsMessageDirty = true;
        }

        public void BindTexture(Texture2D texture)
        {
            Tex = texture;
        }

        public void ApplyImage()
        {
            if (!IsMessageDirty) return;

            if (Tex)
            {
                lock (Message)
                {
                    if (Tex.width != Message.Width || Tex.height != Message.Height)
                    {
                        Tex.Resize((int) Message.Width, (int) Message.Height, TextureFormat.RGBA32, false);
                        Tex.Apply();
                        return;
                    }

                    Tex.filterMode = FilterMode.Point;

                    Tex.LoadRawTextureData(Message.Bytes);
                }

                IsMessageDirty = false;
                Tex.Apply();
            }
        }

        public void Dispose()
        {
            if (isDisposed) return;
            isDisposed = true;
            
            OnClosed?.Invoke(this);
        }

        class ImageMessage
        {
            public uint Width { get; private set; }
            public uint Height { get; private set; }

            public byte[] Bytes { get; private set; }
            public int Length { get; private set; }

            public void Set(uint width, uint height, byte[] source, int index, int length)
            {
                if (Bytes == null || length > Bytes.Length)
                {
                    Bytes = new byte[length];
                }
                
                //flip vertically
                
                //RGBA32
                uint bytesPerPixel = 4;
                for(uint y = 0; y < Height; y++)
                {
                    uint y_tr = Height - y - 1u;
                    Array.Copy(source, index + bytesPerPixel * Width * y, Bytes, bytesPerPixel * Width * y_tr, Width * bytesPerPixel);
                }


                Width = width;
                Height = height;
                Length = length;
            }
        }
    }
}
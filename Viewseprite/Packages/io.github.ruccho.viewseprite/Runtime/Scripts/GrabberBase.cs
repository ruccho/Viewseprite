using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Viewseprite
{
    [ExecuteAlways]
    public abstract class GrabberBase : MonoBehaviour
    {
        [SerializeField] private bool runInEditor = false;
        [SerializeField] private string visibleLayer = default;
        
        private GrabberServer server = default;

        private GrabberSession currentSession = default;
        private GrabberChannel currentChannel = default;

        private Texture2D texture = default;
        private bool isInitialized = false;

        protected virtual void OnEnable()
        {
            if (!runInEditor && !Application.isPlaying) return;
            Initialize();
        }

        private void Initialize()
        {
            if (isInitialized) return;
            isInitialized = true;
            
            if (!texture)
                texture = new Texture2D(2, 2);
            
            SetTexture(texture);

            server = GrabberServer.GetOrCreateInstance();
            server.OnNewSession += ServerOnOnNewSession;
            var exist = server.Sessions.FirstOrDefault();
            if (exist != null) ServerOnOnNewSession(exist);
        }

        private void ServerOnOnNewSession(GrabberSession session)
        {
            currentChannel?.Dispose();
            currentChannel = null;
            
            currentSession = session;
            currentChannel = currentSession.CreateNewChannel();
            currentChannel.BindTexture(texture);

            int layerIndex = -1;
            int i = 0;
            foreach (var layer in currentSession.Layers)
            {
                if (layer == visibleLayer)
                {
                    layerIndex = i;
                }

                i++;
            }

            currentChannel.SetLayer(layerIndex);
        }

        protected virtual void Update()
        {
            if (!runInEditor && !Application.isPlaying) return;

            if (!isInitialized)
            {
                Initialize();
            }

            currentChannel?.ApplyImage();

        }

        protected virtual void OnDisable()
        {
            if (isInitialized)
            {
                server.OnNewSession -= ServerOnOnNewSession;
                currentSession = null;
                currentChannel?.Dispose();
                currentChannel = null;
                isInitialized = false;
            }
        }

        protected abstract void SetTexture(Texture2D texture);
    }
}
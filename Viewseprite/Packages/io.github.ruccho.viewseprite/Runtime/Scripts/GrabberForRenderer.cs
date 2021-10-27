using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Viewseprite
{
    [RequireComponent(typeof(Renderer))]
    public class GrabberForRenderer : GrabberBase
    {
        protected override void SetTexture(Texture2D texture)
        {
            var renderer = GetComponent<Renderer>();
            var mat = Application.isPlaying ? renderer.material : renderer.sharedMaterial;
            mat.mainTexture = texture;
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Viewseprite
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class GrabberForSpriteRenderer : GrabberWithSprite
    {
        protected override void SetSprite(Sprite sprite)
        {
            GetComponent<SpriteRenderer>().sprite = sprite;
        }
    }
}
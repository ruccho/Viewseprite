using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Viewseprite
{
    public abstract class GrabberWithSprite : GrabberBase
    {
        [SerializeField] private Vector2 pivot = new Vector2(0.5f, 0.5f);
        [SerializeField] private float pixelsPerUnit = 100;
        
        private Sprite currentSprite = default;
        
        protected override void SetTexture(Texture2D texture)
        {
            if (currentSprite == null || currentSprite.texture != texture)
            {
                currentSprite =
                    Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit);
            }

            SetSprite(currentSprite);
        }

        protected abstract void SetSprite(Sprite sprite);
        

        protected override void Update()
        {
            base.Update();

            if (currentSprite)
            {
                if (currentSprite.pivot != pivot || currentSprite.pixelsPerUnit != pixelsPerUnit)
                {
                    var texture = currentSprite.texture;
                    currentSprite =
                        Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), pivot, pixelsPerUnit);
                    SetSprite(currentSprite);
                }
            }
        }
    }
}
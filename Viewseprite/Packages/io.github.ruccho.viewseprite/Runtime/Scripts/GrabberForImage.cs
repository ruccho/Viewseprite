using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Viewseprite
{
    [RequireComponent(typeof(Image))]
    public class GrabberForImage : GrabberWithSprite
    {
        protected override void SetSprite(Sprite sprite)
        {
            GetComponent<Image>().sprite = sprite;
        }
    }
}
using UnityEngine;

namespace UnityEngine
{
    public class Canvas : Behaviour
    {
        public delegate void WillRenderCanvases();
        public static event WillRenderCanvases willRenderCanvases
        {
            add { }
            remove { }
        }
        public Canvas rootCanvas { get; }
        public static void ForceUpdateCanvases() { }
    }

    public static class RectTransformUtility
    {
        public static Bounds CalculateRelativeRectTransformBounds(Transform root, Transform child) => default;
    }
}

using UnityEngine;
using UnityEngine.Events;

namespace UnityEngine.UI
{
    public class Graphic : Behaviour
    {
        public Color color { get; set; }
        public bool raycastTarget { get; set; }
        public void SetAllDirty() { }
    }

    public class Image : Graphic
    {
        public Sprite sprite { get; set; }
        public bool preserveAspect { get; set; }
    }

    public struct Navigation
    {
        public enum Mode { None = 0, Horizontal = 1, Vertical = 2, Automatic = 3, Explicit = 4 }
        public Mode mode { get; set; }
        public Selectable selectOnUp { get; set; }
        public Selectable selectOnDown { get; set; }
        public Selectable selectOnLeft { get; set; }
        public Selectable selectOnRight { get; set; }
    }

    public class Selectable : Behaviour
    {
        public bool interactable { get; set; }
        public Graphic targetGraphic { get; set; }
        public Navigation navigation { get; set; }
    }

    public class Slider : Selectable
    {
        public class SliderEvent : UnityEvent<float> { }
        public SliderEvent onValueChanged { get; set; }
        public float minValue { get; set; }
        public float maxValue { get; set; }
        public bool wholeNumbers { get; set; }
        public float value { get; set; }
        public void SetValueWithoutNotify(float input) { }
    }

    public class Scrollbar : Selectable
    {
        public enum Direction { LeftToRight = 0, RightToLeft = 1, BottomToTop = 2, TopToBottom = 3 }
        public RectTransform handleRect { get; set; }
        public Direction direction { get; set; }
        public int numberOfSteps { get; set; }
        public float size { get; set; }
    }

    public class ScrollRect : Behaviour
    {
        public enum MovementType { Unrestricted = 0, Elastic = 1, Clamped = 2 }
        public enum ScrollbarVisibility { Permanent = 0, AutoHide = 1, AutoHideAndExpandViewport = 2 }
        public bool horizontal { get; set; }
        public bool vertical { get; set; }
        public MovementType movementType { get; set; }
        public bool inertia { get; set; }
        public float scrollSensitivity { get; set; }
        public RectTransform viewport { get; set; }
        public RectTransform content { get; set; }
        public Scrollbar verticalScrollbar { get; set; }
        public ScrollbarVisibility verticalScrollbarVisibility { get; set; }
        public float verticalScrollbarSpacing { get; set; }
        public float verticalNormalizedPosition { get; set; }
    }

    public class LayoutElement : Behaviour
    {
        public float preferredWidth { get; set; }
        public float minWidth { get; set; }
        public float flexibleWidth { get; set; }
        public float preferredHeight { get; set; }
        public float minHeight { get; set; }
        public float flexibleHeight { get; set; }
    }

    public class LayoutGroup : Behaviour
    {
        public RectOffset padding { get; set; }
    }

    public class VerticalLayoutGroup : LayoutGroup { }

    public class ContentSizeFitter : Behaviour
    {
        public enum FitMode { Unconstrained = 0, MinSize = 1, PreferredSize = 2 }
        public FitMode horizontalFit { get; set; }
        public FitMode verticalFit { get; set; }
    }

    public class RectMask2D : Behaviour { }

    public static class LayoutUtility
    {
        public static float GetPreferredHeight(RectTransform rect) => 0f;
        public static float GetPreferredWidth(RectTransform rect) => 0f;
    }

    public static class LayoutRebuilder
    {
        public static void ForceRebuildLayoutImmediate(RectTransform layoutRoot) { }
    }
}

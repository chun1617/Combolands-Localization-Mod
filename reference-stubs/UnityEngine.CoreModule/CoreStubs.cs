using System;

namespace UnityEngine
{
    public enum FindObjectsInactive { Exclude = 0, Include = 1 }
    public enum FindObjectsSortMode { None = 0 }

    public class Object
    {
        public string name { get; set; }
        public int GetInstanceID() => 0;
        public static T[] FindObjectsByType<T>(FindObjectsInactive inactive, FindObjectsSortMode sortMode) where T : Object => Array.Empty<T>();
        public static T Instantiate<T>(T original, Transform parent, bool worldPositionStays) where T : Object => original;
        public static void Destroy(Object obj) { }
        public static void DontDestroyOnLoad(Object target) { }
        public static bool operator ==(Object left, Object right) => ReferenceEquals(left, right);
        public static bool operator !=(Object left, Object right) => !ReferenceEquals(left, right);
        public override bool Equals(object obj) => base.Equals(obj);
        public override int GetHashCode() => base.GetHashCode();
    }

    public class Component : Object
    {
        public GameObject gameObject { get; }
        public Transform transform { get; }
        public T GetComponent<T>() where T : class => default;
        public T GetComponentInParent<T>() where T : class => default;
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class => Array.Empty<T>();
    }

    public class Behaviour : Component
    {
        public bool enabled { get; set; }
    }

    public class MonoBehaviour : Behaviour { }

    public class GameObject : Object
    {
        public GameObject() { }
        public GameObject(string name, params Type[] components) { this.name = name; }
        public int layer { get; set; }
        public bool activeSelf { get; }
        public bool activeInHierarchy { get; }
        public Transform transform { get; }
        public T GetComponent<T>() where T : class => default;
        public T AddComponent<T>() where T : Component, new() => new T();
        public T[] GetComponentsInChildren<T>(bool includeInactive) where T : class => Array.Empty<T>();
        public void SetActive(bool value) { }
    }

    public class Transform : Component
    {
        public Transform parent { get; }
        public int childCount { get; }
        public Vector3 position { get; set; }
        public Transform Find(string path) => default;
        public Transform GetChild(int index) => default;
        public int GetSiblingIndex() => 0;
        public void SetSiblingIndex(int index) { }
        public void SetParent(Transform parent, bool worldPositionStays) { }
        public Vector3 TransformPoint(Vector3 position) => position;
        public Vector3 InverseTransformPoint(Vector3 position) => position;
    }

    public class RectTransform : Transform
    {
        public Vector2 anchorMin { get; set; }
        public Vector2 anchorMax { get; set; }
        public Vector2 pivot { get; set; }
        public Vector2 sizeDelta { get; set; }
        public Vector2 offsetMin { get; set; }
        public Vector2 offsetMax { get; set; }
        public Vector2 anchoredPosition { get; set; }
        public Rect rect { get; }
        public void GetWorldCorners(Vector3[] fourCornersArray) { }
    }

    public class CanvasRenderer : Component { }

    public struct Vector2
    {
        public float x;
        public float y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => default;
        public static Vector2 one => new Vector2(1f, 1f);
    }

    public struct Vector3
    {
        public float x;
        public float y;
        public float z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => default;
        public static Vector3 operator +(Vector3 left, Vector3 right) => default;
        public static Vector3 operator -(Vector3 left, Vector3 right) => default;
    }

    public struct Vector4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    public struct Color
    {
        public Color(float r, float g, float b, float a) { }
        public static Color clear => default;
    }

    public struct Rect
    {
        public float xMin { get; }
        public float yMin { get; }
        public float width { get; }
        public float height { get; }
    }

    public struct Bounds
    {
        public Vector3 min { get; }
        public Vector3 max { get; }
    }

    public class RectOffset
    {
        public int left { get; set; }
        public int right { get; set; }
        public int top { get; set; }
        public int bottom { get; set; }
    }

    public static class Mathf
    {
        public static float Max(float a, float b) => a;
        public static float Min(float a, float b) => a;
        public static float Clamp01(float value) => value;
        public static float Abs(float value) => value;
        public static float Round(float value) => value;
        public static int RoundToInt(float value) => 0;
    }
}

namespace UnityEngine.Events
{
    public delegate void UnityAction<T>(T arg0);

    public class UnityEvent<T>
    {
        public void AddListener(UnityAction<T> call) { }
        public void RemoveListener(UnityAction<T> call) { }
    }
}

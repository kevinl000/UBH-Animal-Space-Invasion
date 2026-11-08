// Minimal stand-in for the UnityEngine API the enemy code uses, so it can be compiled and
// driven frame by frame without Unity. Lives OUTSIDE Assets/ on purpose.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace UnityEngine
{
    public class Object
    {
        public string name = "";
        internal bool destroyed;
        internal static readonly List<Object> Live = new List<Object>();

        public static bool operator ==(Object a, Object b)
        {
            bool an = ReferenceEquals(a, null) || a.destroyed, bn = ReferenceEquals(b, null) || b.destroyed;
            if (an || bn) return an && bn;
            return ReferenceEquals(a, b);
        }
        public static bool operator !=(Object a, Object b) { return !(a == b); }
        public override bool Equals(object o) { return ReferenceEquals(this, o); }
        public override int GetHashCode() { return base.GetHashCode(); }
        public static implicit operator bool(Object o) { return !ReferenceEquals(o, null) && !o.destroyed; }

        public static T Instantiate<T>(T original, Transform parent) where T : Object
        {
            var clone = (T)original.MemberwiseClone();
            clone.destroyed = false;
            var mb = clone as MonoBehaviour;
            if (mb != null)
            {
                mb.transform = new Transform { parent = parent };
                mb.components = new Dictionary<Type, Component>(); // the clone gets its own Rigidbody2D / Collider2D
                mb.inactive = false;
                CopyListsDeep(clone); // fresh copies of runtime lists the prefab may hold
                MonoBehaviour.Call(mb, "Awake");
                MonoBehaviour.Call(mb, "OnEnable");
            }
            Live.Add(clone);
            return clone;
        }

        static void CopyListsDeep(object o)
        {
            for (var t = o.GetType(); t != null && t != typeof(Object); t = t.BaseType)
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
                {
                    var v = f.GetValue(o);
                    if (v != null && v.GetType().IsGenericType && v.GetType().GetGenericTypeDefinition() == typeof(List<>))
                        f.SetValue(o, Activator.CreateInstance(v.GetType()));
                }
        }

        public static void Destroy(Object o)
        {
            var mb = o as GameObject;
            Object target = mb != null ? mb.owner : o;
            if (ReferenceEquals(target, null) || target.destroyed) return;
            target.destroyed = true;
            Live.Remove(target);
            if (target is MonoBehaviour) MonoBehaviour.Call((MonoBehaviour)target, "OnDestroy");
        }
    }

    public class GameObject : Object
    {
        internal Object owner;
        public bool activeSelf { get { var mb = owner as MonoBehaviour; return mb == null || !mb.inactive; } }
        public T AddComponent<T>() where T : Component, new() { return ((MonoBehaviour)owner).GetComponent<T>(); }
        public void SetActive(bool value)
        {
            var mb = owner as MonoBehaviour;
            if (mb == null || mb.inactive == !value) return;
            mb.inactive = !value;
            MonoBehaviour.Call(mb, value ? "OnEnable" : "OnDisable");
        }
    }

    public class Component { public MonoBehaviour owner; }

    public enum RigidbodyType2D { Dynamic, Kinematic, Static }
    public enum RigidbodyInterpolation2D { None, Interpolate, Extrapolate }

    /// <summary>Kinematic body: MovePosition lands on the next physics step; here, immediately (one FixedUpdate = one step).</summary>
    public class Rigidbody2D : Component
    {
        public RigidbodyType2D bodyType = RigidbodyType2D.Dynamic;
        public bool useFullKinematicContacts;
        public float gravityScale = 1f;
        public RigidbodyInterpolation2D interpolation;
        public Vector2 position;
        public float rotation;
        public Vector2 linearVelocity;
        public float angularVelocity;
        public int MovePositionCalls;
        public void MovePosition(Vector2 p) { position = p; MovePositionCalls++; }
        public void MoveRotation(float angle) { rotation = angle; }
    }

    public class Collider2D : Component
    {
        /// <summary>Test helper: something attached to this collider's object (e.g. a fake bullet implementing IEnemyHitter).</summary>
        public object attached;
        public T GetComponentInParent<T>() where T : class
        {
            if (attached is T) return (T)attached;
            return owner as T;
        }
    }

    public class Collision2D { public Collider2D collider; }

    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }

    public class MonoBehaviour : Object
    {
        public Transform transform = new Transform();
        public GameObject gameObject { get { return new GameObject { owner = this }; } }
        internal bool inactive;
        internal Dictionary<Type, Component> components = new Dictionary<Type, Component>();

        /// <summary>Every behaviour in the stub has a Rigidbody2D and a Collider2D on its object.</summary>
        public T GetComponent<T>() where T : Component, new()
        {
            Component c;
            if (!components.TryGetValue(typeof(T), out c))
            {
                if (typeof(T) != typeof(Rigidbody2D) && typeof(T) != typeof(Collider2D)) return null;
                c = new T { owner = this };
                components[typeof(T)] = c;
            }
            return (T)c;
        }

        internal static void Call(MonoBehaviour mb, string method)
        {
            for (var t = mb.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (m != null) { m.Invoke(mb, null); return; }
            }
        }

        /// <summary>Test helper: register a hand-made component as live and run Awake.</summary>
        public static T Create<T>() where T : MonoBehaviour, new()
        {
            var c = new T();
            Live.Add(c);
            Call(c, "Awake");
            return c;
        }

        /// <summary>Test helper: one frame for every live behaviour.</summary>
        public static void Frame(float dt)
        {
            Time.deltaTime = dt;
            Time.fixedDeltaTime = dt;
            foreach (var o in Live.ToArray())
                if (!o.destroyed && o is MonoBehaviour && !((MonoBehaviour)o).inactive) Call((MonoBehaviour)o, "FixedUpdate");
            foreach (var o in Live.ToArray())
                if (!o.destroyed && o is MonoBehaviour && !((MonoBehaviour)o).inactive) Call((MonoBehaviour)o, "Update");
        }

        public static int LiveCount<T>() { int n = 0; foreach (var o in Live) if (o is T && !o.destroyed) n++; return n; }
        public static int ActiveCount<T>() { int n = 0; foreach (var o in Live) if (o is T && !o.destroyed && !((MonoBehaviour)o).inactive) n++; return n; }
        public static void ClearAll() { foreach (var o in Live.ToArray()) Destroy(o); Live.Clear(); }
    }

    /// <summary>Planet root at the origin, unrotated, scale 1, so local = world.</summary>
    public class Transform
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 eulerAngles { get { return new Vector3(0, 0, 0); } }
        public Vector3 TransformPoint(Vector3 p) { return p; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public float sqrMagnitude { get { return x * x + y * y + z * z; } }
        public float magnitude { get { return (float)Math.Sqrt(sqrMagnitude); } }
        public static Vector3 MoveTowards(Vector3 a, Vector3 b, float step)
        {
            var d = b - a; float m = d.magnitude;
            if (m <= step || m == 0) return b;
            return new Vector3(a.x + d.x / m * step, a.y + d.y / m * step, a.z + d.z / m * step);
        }
    }

    public struct Quaternion { public float z; public static Quaternion Euler(float x, float y, float z) { return new Quaternion { z = z }; } }

    public static class Mathf
    {
        public const float Deg2Rad = (float)(Math.PI / 180.0);
        public static float Cos(float a) { return (float)Math.Cos(a); }
        public static float Sin(float a) { return (float)Math.Sin(a); }
        public static float Max(float a, float b) { return a > b ? a : b; }
        public static int Max(int a, int b) { return a > b ? a : b; }
        public static float Min(float a, float b) { return a < b ? a : b; }
        public static float Clamp01(float v) { return v < 0 ? 0 : v > 1 ? 1 : v; }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * Clamp01(t); }
        public static int RoundToInt(float f) { return (int)Math.Round(f); }
        public static int FloorToInt(float f) { return (int)Math.Floor(f); }
        public static float DeltaAngle(float current, float target)
        {
            float d = (target - current) % 360f; if (d < 0) d += 360f;
            return d > 180f ? d - 360f : d;
        }
    }

    public static class Time { public static float deltaTime; public static float fixedDeltaTime = 0.02f; }

    public static class Random
    {
        public static System.Random Rng = new System.Random(1);
        public static float value { get { return (float)Rng.NextDouble(); } }
        public static float Range(float a, float b) { return a + (b - a) * value; }
    }

    public static class Debug
    {
        public static readonly List<string> Log = new List<string>();
        public static void LogWarning(string s) { Log.Add("WARN " + s); }
        public static void LogError(string s) { Log.Add("ERROR " + s); }
    }

    public class HeaderAttribute : Attribute { public HeaderAttribute(string s) { } }
    public class TooltipAttribute : Attribute { public TooltipAttribute(string s) { } }
    public class CreateAssetMenuAttribute : Attribute { public string fileName, menuName; }
}

namespace UnityEngine.Pool
{
    using System;
    using System.Collections.Generic;

    public interface IObjectPool<T> where T : class
    {
        int CountInactive { get; }
        T Get();
        void Release(T element);
        void Clear();
    }

    /// <summary>Same behaviour as Unity's stack-based ObjectPool (collection check throws on double release).</summary>
    public class ObjectPool<T> : IObjectPool<T> where T : class
    {
        readonly Stack<T> stack = new Stack<T>();
        readonly Func<T> create; readonly Action<T> onGet, onRelease, onDestroy;
        readonly bool collectionCheck; readonly int maxSize;
        public int CountAll { get; private set; }
        public int CountActive { get { return CountAll - CountInactive; } }
        public int CountInactive { get { return stack.Count; } }
        public int Created;

        public ObjectPool(Func<T> createFunc, Action<T> actionOnGet = null, Action<T> actionOnRelease = null, Action<T> actionOnDestroy = null,
                          bool collectionCheck = true, int defaultCapacity = 10, int maxSize = 10000)
        {
            create = createFunc; onGet = actionOnGet; onRelease = actionOnRelease; onDestroy = actionOnDestroy;
            this.collectionCheck = collectionCheck; this.maxSize = maxSize;
        }

        public T Get()
        {
            T e;
            if (stack.Count == 0) { e = create(); CountAll++; Created++; }
            else e = stack.Pop();
            if (onGet != null) onGet(e);
            return e;
        }

        public void Release(T e)
        {
            if (collectionCheck && stack.Contains(e)) throw new InvalidOperationException("Trying to release an object that has already been released to the pool.");
            if (onRelease != null) onRelease(e);
            if (CountInactive < maxSize) stack.Push(e);
            else { CountAll--; if (onDestroy != null) onDestroy(e); }
        }

        public void Clear()
        {
            if (onDestroy != null) foreach (var e in stack) onDestroy(e);
            stack.Clear(); CountAll = 0;
        }
    }
}

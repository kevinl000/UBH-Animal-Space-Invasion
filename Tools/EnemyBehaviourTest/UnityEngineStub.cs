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
                mb.transform = new Transform { parent = parent, owner = mb };
                mb.components = new Dictionary<Type, Component>(); // the clone gets its own Rigidbody2D / Collider2D
                mb.inactive = false;
                var prefabArt = ((MonoBehaviour)(object)original).art;
                CopyListsDeep(clone); // fresh copies of runtime lists the prefab may hold
                foreach (var a in prefabArt) mb.art.Add(new SpriteRenderer { owner = mb, color = a.color });
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

        public static T[] FindObjectsByType<T>(FindObjectsSortMode mode) where T : class
        {
            var list = new List<T>();
            foreach (var o in Live) if (o is T && !o.destroyed) list.Add(o as T);
            return list.ToArray();
        }

        public static T FindAnyObjectByType<T>() where T : class
        {
            foreach (var o in Live) if (o is T && !o.destroyed && !(o is MonoBehaviour && ((MonoBehaviour)o).inactive)) return o as T;
            return null;
        }

        public static void DestroyImmediate(Object o) { Destroy(o); }

        public static void Destroy(Object o)
        {
            var mb = o as GameObject;
            Object target = mb != null ? mb.owner : o;
            if (ReferenceEquals(target, null) || target.destroyed) return;
            target.destroyed = true;
            Live.Remove(target);
            if (target is MonoBehaviour && !((MonoBehaviour)target).inactive) MonoBehaviour.Call((MonoBehaviour)target, "OnDisable");
            if (target is MonoBehaviour) MonoBehaviour.Call((MonoBehaviour)target, "OnDestroy");
        }
    }

    public class GameObject : Object
    {
        internal Object owner;
        public GameObject() { }
        public GameObject(string n) { name = n; }
        // compile-only members used by editor scripts (never called in the tests)
        public Transform transform { get { var mb = owner as MonoBehaviour; return mb != null ? mb.transform : null; } }
        public Component AddComponent(Type t) { return null; }
        public T GetComponent<T>() { return default(T); }
        public T[] GetComponentsInChildren<T>(bool includeInactive) { return new T[0]; }
        public static GameObject Find(string n) { return null; }
        public bool activeSelf { get { var mb = owner as MonoBehaviour; return mb == null || !mb.inactive; } }
        public T GetComponentInParent<T>() where T : class { return owner as T; }
        public T AddComponent<T>() where T : Component, new() { return ((MonoBehaviour)owner).GetComponent<T>(); }
        public void SetActive(bool value)
        {
            var mb = owner as MonoBehaviour;
            if (mb == null || mb.inactive == !value) return;
            mb.inactive = !value;
            MonoBehaviour.Call(mb, value ? "OnEnable" : "OnDisable");
        }
    }

    public class Component : Object
    {
        public MonoBehaviour owner;
        public Transform transform { get { return owner != null ? owner.transform : null; } }
        public T GetComponent<T>() where T : Component, new() { return owner != null ? owner.GetComponent<T>() : null; }
        public T GetComponentInParent<T>() where T : class { return owner as T; }
        public GameObject gameObject { get { return new GameObject { owner = owner }; } }
    }

    public class Sprite : Object { }
    public struct Bounds { public Vector3 size; }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color Lerp(Color x, Color y, float t)
        {
            t = t < 0 ? 0 : t > 1 ? 1 : t;
            return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        }
        public static Color operator *(Color x, Color y) { return new Color(x.r * y.r, x.g * y.g, x.b * y.b, x.a * y.a); }
    }
    /// <summary>Stub: a behaviour's art. Tests attach them with MonoBehaviour.AddArt.</summary>
    public class SpriteRenderer : Component { public Sprite sprite; public int sortingOrder; public Color color = Color.white; public Bounds bounds; }

    public enum CameraClearFlags { Skybox = 1, SolidColor = 2, Depth = 3, Nothing = 4 }
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : MonoBehaviour
    {
        public RenderMode renderMode; public Camera worldCamera; public float planeDistance; public int sortingOrder; public bool isRootCanvas = true;
        public Canvas rootCanvas { get { return this; } }
    }
    public static class Screen { public static int width = 1920, height = 1080; }

    public enum FindObjectsSortMode { None, InstanceID }
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
        public bool isTrigger;
        /// <summary>Stub physics: every collider is a circle of this radius.</summary>
        public float stubRadius = 0.25f;
        /// <summary>Test helper: something attached to this collider's object (e.g. a fake bullet implementing IEnemyHitter).</summary>
        public object attached;
        public T GetComponentInParent<T>() where T : class
        {
            if (attached is T) return (T)attached;
            return owner as T;
        }
    }

    public class CircleCollider2D : Collider2D
    {
        public Vector2 offset;
        public float radius { get { return stubRadius; } set { stubRadius = value; } }
    }

    public class Collision2D { public Collider2D collider; }

    public class RequireComponent : Attribute { public RequireComponent(Type t) { } }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }

    public class MonoBehaviour : Component
    {
        public new Transform transform = new Transform();
        public MonoBehaviour() { transform.owner = this; }
        public GameObject gameObject { get { return new GameObject { owner = this }; } }
        internal bool inactive;
        internal Dictionary<Type, Component> components = new Dictionary<Type, Component>();

        /// <summary>Every behaviour in the stub has a Rigidbody2D and a Collider2D on its object.</summary>
        public new T GetComponent<T>() where T : Component, new()
        {
            Component c;
            Type key = typeof(Collider2D).IsAssignableFrom(typeof(T)) ? typeof(Collider2D) : typeof(T);
            if (!components.TryGetValue(key, out c))
            {
                if (typeof(T) != typeof(Rigidbody2D) && typeof(T) != typeof(Collider2D)) return null;
                c = new T { owner = this };
                var rb = c as Rigidbody2D;
                if (rb != null) { rb.position = transform.freePosition; rb.rotation = transform.eulerAngles.z; } // a new body starts at the transform's pose
                components[key] = c;
            }
            return c as T;
        }

        internal List<SpriteRenderer> art = new List<SpriteRenderer>();
        /// <summary>Test helper: give this object a sprite renderer (its art).</summary>
        public SpriteRenderer AddArt() { var r = new SpriteRenderer { owner = this }; art.Add(r); return r; }
        public new T[] GetComponentsInChildren<T>() where T : class
        {
            var list = new List<T>();
            foreach (var r in art) if (r is T) list.Add(r as T);
            return list.ToArray();
        }

        /// <summary>Test helper: give this object a circle collider (replaces the default one).</summary>
        public CircleCollider2D AddCircle(float radius, bool trigger)
        {
            var c = new CircleCollider2D { owner = this, radius = radius, isTrigger = trigger };
            components[typeof(Collider2D)] = c;
            return c;
        }

        public bool enabled = true;

        static readonly HashSet<long> touching = new HashSet<long>();
        public static bool StubPhysics = false;

        /// <summary>Stub 2D physics: OnTriggerEnter2D when two active bodies' circles start to overlap.</summary>
        static void Physics()
        {
            var bodies = new List<MonoBehaviour>();
            foreach (var o in Live)
            {
                var mb = o as MonoBehaviour;
                if (mb == null || mb.destroyed || mb.inactive) continue;
                Component rb, col;
                if (mb.components.TryGetValue(typeof(Rigidbody2D), out rb) && mb.components.TryGetValue(typeof(Collider2D), out col)) bodies.Add(mb);
            }
            var now = new HashSet<long>();
            for (int i = 0; i < bodies.Count; i++)
                for (int j = i + 1; j < bodies.Count; j++)
                {
                    var a = bodies[i]; var b = bodies[j];
                    var pa = ((Rigidbody2D)a.components[typeof(Rigidbody2D)]).position; var pb = ((Rigidbody2D)b.components[typeof(Rigidbody2D)]).position;
                    var ca = (Collider2D)a.components[typeof(Collider2D)]; var cb = (Collider2D)b.components[typeof(Collider2D)];
                    if (Vector2.Distance(pa, pb) > ca.stubRadius + cb.stubRadius) continue;
                    long key = ((long)a.GetHashCode() << 32) ^ (uint)b.GetHashCode();
                    now.Add(key);
                    if (touching.Contains(key)) continue;
                    if (!a.inactive && !b.inactive) Call(a, "OnTriggerEnter2D", cb);
                    if (!a.inactive && !b.inactive) Call(b, "OnTriggerEnter2D", ca);
                }
            touching.Clear(); foreach (var k in now) touching.Add(k);
        }

        internal static void Call(MonoBehaviour mb, string method, object arg)
        {
            for (var t = mb.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (m != null && m.GetParameters().Length == 1) { m.Invoke(mb, new[] { arg }); return; }
            }
        }

        internal static void Call(MonoBehaviour mb, string method)
        {
            for (var t = mb.GetType(); t != null; t = t.BaseType)
            {
                var m = t.GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly, null, Type.EmptyTypes, null);
                if (m != null) { m.Invoke(mb, null); return; }
            }
        }

        /// <summary>Test helper: register an object made with new as live, run Awake and OnEnable.</summary>
        public static void Register(MonoBehaviour mb)
        {
            Live.Add(mb);
            Call(mb, "Awake");
            Call(mb, "OnEnable");
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
            if (StubPhysics) Physics();
            foreach (var o in Live.ToArray())
                if (!o.destroyed && o is MonoBehaviour && !((MonoBehaviour)o).inactive) Call((MonoBehaviour)o, "Update");
        }

        public static int LiveCount<T>() { int n = 0; foreach (var o in Live) if (o is T && !o.destroyed) n++; return n; }
        public static int ActiveCount<T>() { int n = 0; foreach (var o in Live) if (o is T && !o.destroyed && !((MonoBehaviour)o).inactive) n++; return n; }
        public static void ClearAll() { foreach (var o in Live.ToArray()) Destroy(o); Live.Clear(); }
    }

    /// <summary>
    /// Roots are unrotated with scale 1 and children don't inherit parent offsets in the stub, so world = local + root offset.
    /// An object with a Rigidbody2D reports the body's pose (that's what Unity shows after the physics step).
    /// </summary>
    public class Transform
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        internal MonoBehaviour owner;
        public Vector3 lossyScale = new Vector3(1, 1, 1);
        public Vector3 localScale = new Vector3(1, 1, 1);
        internal Vector3 freePosition;
        private float freeRotation;

        Rigidbody2D Body { get { Component c; return owner != null && owner.components.TryGetValue(typeof(Rigidbody2D), out c) ? (Rigidbody2D)c : null; } }

        public Vector3 position
        {
            get { var b = Body; return b != null ? (Vector3)b.position : freePosition; }
            set { var b = Body; if (b != null) b.position = value; freePosition = value; }
        }
        public Vector3 eulerAngles
        {
            get { var b = Body; return new Vector3(0, 0, b != null ? b.rotation : freeRotation); }
            set { var b = Body; if (b != null) b.rotation = value.z; freeRotation = value.z; }
        }
        public Vector3 up
        {
            get { float a = eulerAngles.z * Mathf.Deg2Rad; return new Vector3(-Mathf.Sin(a), Mathf.Cos(a), 0); }
        }
        public Vector3 TransformPoint(Vector3 p) { var o = position; return new Vector3(p.x + o.x, p.y + o.y, p.z); }
        public Vector3 InverseTransformPoint(Vector3 p) { var o = position; return new Vector3(p.x - o.x, p.y - o.y, p.z); }
        public void SetParent(Transform p, bool worldPositionStays) { parent = p; }
        public T GetComponent<T>() where T : Component, new() { return owner != null ? owner.GetComponent<T>() : null; }
        public GameObject gameObject { get { return owner != null ? owner.gameObject : null; } }
        public string name { get { return owner != null ? owner.name : ""; } }
        public T GetComponentInParent<T>() where T : class { return owner as T; }
        public T[] GetComponents<T>() where T : Component, new() { var c = GetComponent<T>(); return c != null ? new[] { c } : new T[0]; }
        public T[] GetComponentsInChildren<T>() where T : class { return owner != null ? owner.GetComponentsInChildren<T>() : new T[0]; }
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(0, 0); } }
        public static implicit operator Vector2(Vector3 v) { return new Vector2(v.x, v.y); }
        public static implicit operator Vector3(Vector2 v) { return new Vector3(v.x, v.y, 0); }
        public float magnitude { get { return (float)Math.Sqrt(x * x + y * y); } }
        public Vector2 normalized { get { float m = magnitude; return m > 1e-6f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) { return new Vector2(a.x + b.x, a.y + b.y); }
        public static Vector2 operator -(Vector2 a, Vector2 b) { return new Vector2(a.x - b.x, a.y - b.y); }
        public static Vector2 operator *(Vector2 a, float f) { return new Vector2(a.x * f, a.y * f); }
        public static float Distance(Vector2 a, Vector2 b) { return (a - b).magnitude; }
        public static Vector2 one { get { return new Vector2(1, 1); } }
        public static bool operator ==(Vector2 a, Vector2 b) { return a.x == b.x && a.y == b.y; }
        public static bool operator !=(Vector2 a, Vector2 b) { return !(a == b); }
        public override bool Equals(object o) { return o is Vector2 && (Vector2)o == this; }
        public override int GetHashCode() { return x.GetHashCode() ^ y.GetHashCode(); }
        public static float Angle(Vector2 a, Vector2 b)
        {
            float d = a.magnitude * b.magnitude; if (d < 1e-6f) return 0f;
            float c = (a.x * b.x + a.y * b.y) / d; c = c < -1 ? -1 : c > 1 ? 1 : c;
            return (float)(Math.Acos(c) * 180.0 / Math.PI);
        }
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 operator -(Vector3 a, Vector3 b) { return new Vector3(a.x - b.x, a.y - b.y, a.z - b.z); }
        public static Vector3 operator *(Vector3 a, float f) { return new Vector3(a.x * f, a.y * f, a.z * f); }
        public static Vector3 zero { get { return new Vector3(0, 0, 0); } }
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
        public const float Rad2Deg = (float)(180.0 / Math.PI);
        public static float Atan2(float y, float x) { return (float)Math.Atan2(y, x); }
        public static float Abs(float v) { return Math.Abs(v); }
        public static float Sqrt(float v) { return (float)Math.Sqrt(v); }
        public static int Clamp(int v, int lo, int hi) { return v < lo ? lo : v > hi ? hi : v; }
        public static int Abs(int v) { return Math.Abs(v); }
        public static float DeltaAngle(float current, float target)
        {
            float d = (target - current) % 360f; if (d < 0) d += 360f;
            return d > 180f ? d - 360f : d;
        }
    }

    public class Camera : MonoBehaviour
    {
        public static Camera main;
        public bool orthographic = true;
        public float orthographicSize = 5f;
        public Color backgroundColor;
        public CameraClearFlags clearFlags;
        // orthographic camera at the origin: screen centre = world (0,0), Screen.height pixels = 2 * orthographicSize units
        float Ppu { get { return Screen.height / (2f * orthographicSize); } }
        public Vector3 ScreenToWorldPoint(Vector3 p) { return new Vector3((p.x - Screen.width / 2f) / Ppu, (p.y - Screen.height / 2f) / Ppu, 0); }
        public Vector3 WorldToScreenPoint(Vector3 p) { return new Vector3(p.x * Ppu + Screen.width / 2f, p.y * Ppu + Screen.height / 2f, 0); }
    }

    public static class Time { public static float deltaTime; public static float fixedDeltaTime = 0.02f; public static float timeScale = 1f; }

    public static class Random
    {
        public static System.Random Rng = new System.Random(1);
        public static float value { get { return (float)Rng.NextDouble(); } }
        public static float Range(float a, float b) { return a + (b - a) * value; }
    }

    public static class Debug
    {
        public static readonly List<string> Logs = new List<string>();
        public static void Log(string s) { Logs.Add("LOG " + s); }
        public static void LogWarning(string s) { Logs.Add("WARN " + s); }
        public static void LogError(string s) { Logs.Add("ERROR " + s); }
    }

    public class SerializeField : Attribute { }
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

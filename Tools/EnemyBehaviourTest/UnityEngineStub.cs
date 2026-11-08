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
                CopyListsDeep(clone); // fresh copies of runtime lists the prefab may hold
                MonoBehaviour.Call(mb, "Awake");
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

    public class GameObject : Object { internal Object owner; }
    public class ScriptableObject : Object
    {
        public static T CreateInstance<T>() where T : ScriptableObject, new() { return new T(); }
    }

    public class MonoBehaviour : Object
    {
        public Transform transform = new Transform();
        public GameObject gameObject { get { return new GameObject { owner = this }; } }

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
            foreach (var o in Live.ToArray())
                if (!o.destroyed && o is MonoBehaviour) Call((MonoBehaviour)o, "Update");
        }

        public static int LiveCount<T>() { int n = 0; foreach (var o in Live) if (o is T && !o.destroyed) n++; return n; }
        public static void ClearAll() { foreach (var o in Live.ToArray()) Destroy(o); Live.Clear(); }
    }

    public class Transform
    {
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
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
    }

    public static class Time { public static float deltaTime; }

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

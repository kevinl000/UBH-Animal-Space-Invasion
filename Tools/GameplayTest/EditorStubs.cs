// Compile-only stand-ins for the UnityEditor / UnityEngine members the editor scripts use (type check, never run).
using System;
using System.Collections.Generic;
#pragma warning disable 67, 649

namespace UnityEngine
{
    public enum RenderMode { ScreenSpaceOverlay, ScreenSpaceCamera, WorldSpace }
    public class Canvas : MonoBehaviour { public RenderMode renderMode; public Camera worldCamera; public float planeDistance; public int sortingOrder; public bool isRootCanvas; }
    public class Sprite : Object { }
    public struct Bounds { public Vector3 size; }
    public class SpriteRenderer : Component { public Sprite sprite; public int sortingOrder; public Color color; public Bounds bounds; }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1); } }
        public static Color Lerp(Color x, Color y, float t) { return x; }
        public static Color operator *(Color x, Color y) { return x; }
    }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Object
    {
        public Texture2D(int w, int h, TextureFormat f, bool mip) { }
        public void SetPixel(int x, int y, Color c) { }
        public void Apply() { }
        public byte[] EncodeToPNG() { return new byte[0]; }
    }
}

namespace UnityEngine.SceneManagement
{
    public struct Scene { public string name; }
    public static class SceneManager { public static Scene GetActiveScene() { return new Scene(); } }
}

namespace UnityEditor
{
    using UnityEngine;
    public class MenuItem : Attribute { public MenuItem(string s) { } }
    public static class AssetDatabase
    {
        public static T LoadAssetAtPath<T>(string p) where T : class { return null; }
        public static UnityEngine.Object[] LoadAllAssetsAtPath(string p) { return new UnityEngine.Object[0]; }
        public static void CreateAsset(UnityEngine.Object o, string p) { }
        public static void SaveAssets() { }
        public static void Refresh() { }
        public static void ImportAsset(string p) { }
        public static string[] FindAssets(string f) { return new string[0]; }
        public static string GUIDToAssetPath(string g) { return g; }
    }
    public static class EditorUtility
    {
        public static void SetDirty(UnityEngine.Object o) { }
        public static bool DisplayDialog(string a, string b, string c) { return true; }
    }
    public static class PrefabUtility
    {
        public static UnityEngine.Object InstantiatePrefab(UnityEngine.Object o) { return null; }
        public static GameObject SaveAsPrefabAsset(GameObject g, string p) { return g; }
    }
    public enum TextureImporterType { Default, Sprite }
    public enum SpriteImportMode { None, Single, Multiple }
    public class AssetImporter { public static AssetImporter GetAtPath(string p) { return null; } }
    public class TextureImporter : AssetImporter { public TextureImporterType textureType; public SpriteImportMode spriteImportMode; public float spritePixelsPerUnit; public void SaveAndReimport() { } }
    public class SerializedProperty { public UnityEngine.Object objectReferenceValue; }
    public class SerializedObject
    {
        public SerializedObject(UnityEngine.Object o) { }
        public SerializedProperty FindProperty(string n) { return new SerializedProperty(); }
        public bool ApplyModifiedProperties() { return true; }
    }
    public static class Undo
    {
        public static void RecordObject(UnityEngine.Object o, string n) { }
        public static void RegisterCreatedObjectUndo(UnityEngine.Object o, string n) { }
        public static T AddComponent<T>(GameObject g) where T : Component { return null; }
    }
}

namespace UnityEditor.SceneManagement
{
    public static class EditorSceneManager
    {
        public static bool MarkSceneDirty(UnityEngine.SceneManagement.Scene s) { return true; }
        public static bool SaveScene(UnityEngine.SceneManagement.Scene s) { return true; }
    }
}

public class PlanetPresentation : UnityEngine.ScriptableObject { public string sceneName; public int level; }

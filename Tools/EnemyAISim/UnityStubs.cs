// Minimal stand-ins for the UnityEngine types the enemy AI and LevelData use, so they compile and run without Unity.
// Lives OUTSIDE Assets/ on purpose: Unity must never compile this file.
namespace UnityEngine {
 public class Object { public string name = "Level"; }
 public class ScriptableObject : Object {}
 public class MonoBehaviour : Object {}
 public class TooltipAttribute : System.Attribute { public TooltipAttribute(string s){} }
 public class CreateAssetMenuAttribute : System.Attribute { public string fileName, menuName; }
 public static class Debug { public static void LogWarning(string s){ System.Console.WriteLine("WARN "+s);} }
 public static class Mathf { public static float Clamp01(float v){return v<0?0:v>1?1:v;} public static float Lerp(float a,float b,float t){return a+(b-a)*Clamp01(t);} public static int FloorToInt(float f){return (int)System.Math.Floor(f);} public static int Max(int a,int b){return a>b?a:b;} public static float Max(float a,float b){return a>b?a:b;} }
}
public class BaseEnemyData : UnityEngine.MonoBehaviour {}

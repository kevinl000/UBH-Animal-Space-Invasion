// Compile-only stand-ins for UI / input / menu types the gameplay scripts reference. Lives OUTSIDE Assets/.
using System;
using System.Collections.Generic;

namespace UnityEngine.UI
{
    public class Text : MonoBehaviour { public string text = ""; }
    public class Selectable : MonoBehaviour { }
    public class ScrollRect : MonoBehaviour { }
}

namespace UnityEngine.EventSystems
{
    public class RaycastResult { public GameObject gameObject; }
    public class PointerEventData { public PointerEventData(EventSystem es) { } public Vector2 position; }
    public class EventSystem : MonoBehaviour
    {
        public static EventSystem current;
        public void RaycastAll(PointerEventData d, List<RaycastResult> results) { }
    }
}

namespace UnityEngine.InputSystem
{
    public enum Key { None, Digit1 = 41, Digit2, Digit3, Digit4, Digit5, Digit6, Digit7, Digit8, Digit9, Digit0 }
    public class ButtonControl { public bool wasPressedThisFrame; }
    public class Vector2Control { public Vector2 value; public Vector2 ReadValue() { return value; } }
    public class Mouse
    {
        public static Mouse current;
        public ButtonControl leftButton = new ButtonControl(), rightButton = new ButtonControl();
        public Vector2Control position = new Vector2Control();
    }
    public class Keyboard
    {
        public static Keyboard current;
        readonly Dictionary<Key, ButtonControl> keys = new Dictionary<Key, ButtonControl>();
        public ButtonControl this[Key k] { get { ButtonControl b; if (!keys.TryGetValue(k, out b)) keys[k] = b = new ButtonControl(); return b; } }
    }
}

// The real menu scripts need the full UI; these match the members the gameplay scripts use.
public class TowerPresentation : UnityEngine.ScriptableObject { public string displayName; }
public class TowerCard : UnityEngine.MonoBehaviour { public TowerPresentation tower; public bool Available { get; set; } }
public class UnionPrimeVisualTest : UnityEngine.MonoBehaviour { }
public class PlanetPageController : UnityEngine.MonoBehaviour { public TowerCard SelectedCard { get; set; } }
public class SceneController
{
    public static SceneController Instance = new SceneController();
    public bool IsLoading;
    public bool TryLoadScene(string name) { return true; }
}

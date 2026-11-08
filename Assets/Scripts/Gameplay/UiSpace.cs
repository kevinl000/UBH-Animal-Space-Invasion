using UnityEngine;

/// <summary>
/// The planets are UI (RectTransforms on a Screen Space - Overlay canvas, whose world units are screen pixels),
/// while enemies, towers and projectiles are sprites seen by the main camera. This converts a UI object's
/// position and size into the gameplay (camera) world so both line up on screen.
/// For objects not on an overlay canvas it changes nothing.
/// </summary>
public static class UiSpace
{
    static bool OnOverlay(Transform t)
    {
        if (t == null) return false;
        Canvas c = t.GetComponentInParent<Canvas>();
        return c != null && c.rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay && Camera.main != null;
    }

    /// <summary>A point in t's world space (pixels on an overlay canvas) as a gameplay world point (z = 0).</summary>
    public static Vector3 ToGameplay(Transform t, Vector3 point)
    {
        if (!OnOverlay(t)) return new Vector3(point.x, point.y, 0f);
        Camera cam = Camera.main;
        Vector3 w = cam.ScreenToWorldPoint(new Vector3(point.x, point.y, -cam.transform.position.z));
        return new Vector3(w.x, w.y, 0f);
    }

    /// <summary>A gameplay world point as a point in t's world space (pixels on an overlay canvas).</summary>
    public static Vector3 FromGameplay(Transform t, Vector3 point)
    {
        if (!OnOverlay(t)) return point;
        Vector3 s = Camera.main.WorldToScreenPoint(point);
        return new Vector3(s.x, s.y, t.position.z);
    }

    /// <summary>Gameplay world units per unit of t's world space (1 when not on an overlay canvas).</summary>
    public static float Scale(Transform t)
    {
        if (!OnOverlay(t)) return 1f;
        Camera cam = Camera.main;
        return cam.orthographic ? cam.orthographicSize * 2f / Mathf.Max(1, Screen.height) : 1f;
    }
}

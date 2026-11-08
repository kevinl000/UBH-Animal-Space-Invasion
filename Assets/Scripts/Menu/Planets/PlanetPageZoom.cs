using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

// Overlay pages scale their art; gameplay camera zoom keeps all combat sprites in step.
[DisallowMultipleComponent]
public class PlanetPageZoom : MonoBehaviour
{
    [Tooltip("Overlay page binding. Leave empty when attached to a world-space GameplayLoopController.")]
    public PlanetPageController page;
    public RectTransform viewport;
    [Tooltip("Border used for spawning. Normally the usable playfield; can be the canvas for a full-screen border.")]
    public RectTransform spawnViewport;
    [Min(0.01f)] public float minimumZoom = 0.5f;
    [Min(0.01f)] public float maximumZoom = 1f;
    [Min(0.01f)] public float startingZoom = 0.75f;
    [Min(0.01f)] public float wheelSensitivity = 0.12f;
    [Min(0.01f)] public float zoomSpeed = 12f;

    public float CurrentZoom { get; private set; }
    public float TargetZoom { get; private set; }

    private Canvas canvas;
    private Vector3 authoredScale;
    private readonly Vector3[] corners = new Vector3[4];
    private int firstTouchId = -1;
    private int secondTouchId = -1;
    private float previousPinchDistance;
    private Camera battleCamera;
    private GameplayLoopController gameplay;
    private float fittedCameraSize;
    private PointerEventData pointer;
    private EventSystem pointerEvents;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        if (page != null) authoredScale = page.visualMount.localScale;
        else
        {
            gameplay = GetComponent<GameplayLoopController>();
            battleCamera = gameplay != null ? gameplay.battleCamera : null;
            if (battleCamera != null) fittedCameraSize = battleCamera.orthographicSize;
        }
        SetZoom(startingZoom, true);
    }

    private void Start()
    {
        if (page != null && FindAnyObjectByType<LevelPlanetDefense>() != null)
        {
            battleCamera = Camera.main;
            if (battleCamera != null)
            {
                fittedCameraSize = battleCamera.orthographicSize;
                ApplyZoom(CurrentZoom);
            }
        }
    }

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
    }

    private void OnDisable()
    {
        EnhancedTouchSupport.Disable();
        ResetPinch();
    }

    private void OnValidate()
    {
        minimumZoom = Mathf.Max(0.01f, minimumZoom);
        maximumZoom = Mathf.Max(minimumZoom, maximumZoom);
        startingZoom = Mathf.Clamp(startingZoom, minimumZoom, maximumZoom);
    }

    private void Update()
    {
        if (SceneLoadingScreen.IsLoading || Time.timeScale <= 0f
            || (page != null && !page.controls.interactable)
            || (page == null && (gameplay == null || !gameplay.Ready || gameplay.Paused
                || gameplay.Phase == GameplayLoopController.GameplayPhase.BossEntrance
                || gameplay.Phase == GameplayLoopController.GameplayPhase.Victory
                || gameplay.Phase == GameplayLoopController.GameplayPhase.Defeat)))
        {
            ResetPinch();
            TargetZoom = CurrentZoom;
            return;
        }

        if (!ReadPinch())
        {
            var mouse = Mouse.current;
            if (mouse != null && Contains(mouse.position.ReadValue()))
            {
                float scroll = mouse.scroll.ReadValue().y;
                // Input System 1.20 normalizes ticks by default. Native Windows ticks are 120.
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_WSA
                if (InputSystem.settings.scrollDeltaBehavior == InputSettings.ScrollDeltaBehavior.KeepPlatformSpecificInputRange)
                    scroll /= 120f;
#endif
                if (scroll != 0) SetZoom(TargetZoom * Mathf.Exp(scroll * wheelSensitivity));
            }
        }

        float zoom = Mathf.Lerp(CurrentZoom, TargetZoom, 1 - Mathf.Exp(-zoomSpeed * Time.unscaledDeltaTime));
        ApplyZoom(Mathf.Abs(zoom - TargetZoom) < 0.0001f ? TargetZoom : zoom);
    }

    public void SetZoom(float zoom, bool immediate = false)
    {
        TargetZoom = Mathf.Clamp(zoom, minimumZoom, maximumZoom);
        if (immediate) ApplyZoom(TargetZoom);
    }

    // Resizing the playfield keeps the player's zoom relative to the newly fitted view.
    public void SetFittedCameraSize(float size)
    {
        fittedCameraSize = size;
        ApplyZoom(CurrentZoom);
    }

    private void ApplyZoom(float zoom)
    {
        CurrentZoom = zoom;
        if (page != null)
            page.visualMount.localScale = new Vector3(authoredScale.x * zoom, authoredScale.y * zoom, authoredScale.z);
        if (battleCamera == null || fittedCameraSize <= 0f) return;

        // Keep an overlay planet's screen centre at the same gameplay-world point.
        // Its pixel radius grows with zoom while units/pixel shrink, preserving combat geometry.
        Vector3 anchor = page != null ? UiSpace.ToGameplay(page.visualMount, page.visualMount.position) : Vector3.zero;
        battleCamera.orthographicSize = fittedCameraSize * startingZoom / zoom;
        if (page != null)
            battleCamera.transform.position += anchor - UiSpace.ToGameplay(page.visualMount, page.visualMount.position);
    }

    private bool Contains(Vector2 screenPoint)
    {
        bool inside;
        if (page == null) inside = battleCamera != null && battleCamera.pixelRect.Contains(screenPoint);
        else
        {
            var camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            inside = RectTransformUtility.RectangleContainsScreenPoint(viewport, screenPoint, camera);
        }
        if (!inside || EventSystem.current == null) return inside;
        if (pointer == null || pointerEvents != EventSystem.current)
        {
            pointerEvents = EventSystem.current;
            pointer = new PointerEventData(pointerEvents);
        }
        pointer.position = screenPoint;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointer, uiHits);
        foreach (RaycastResult hit in uiHits)
            if (hit.gameObject.GetComponentInParent<Selectable>() != null
                || hit.gameObject.GetComponentInParent<ScrollRect>() != null)
                return false;
        return true;
    }

    private bool ReadPinch()
    {
        var touches = Touch.activeTouches;
        Touch first = default;
        Touch second = default;
        int count = 0;
        foreach (var touch in touches)
        {
            if (!touch.isInProgress) continue;
            if (count == 0) first = touch;
            if (count == 1) second = touch;
            count++;
        }
        if (count != 2 || !Contains(first.startScreenPosition) || !Contains(second.startScreenPosition)
            || !Contains(first.screenPosition) || !Contains(second.screenPosition))
        {
            ResetPinch();
            return count > 0;
        }

        float distance = Vector2.Distance(first.screenPosition, second.screenPosition);
        if (first.touchId == firstTouchId && second.touchId == secondTouchId && previousPinchDistance > 1 && distance > 1)
            SetZoom(TargetZoom * distance / previousPinchDistance, true);
        firstTouchId = first.touchId;
        secondTouchId = second.touchId;
        previousPinchDistance = distance;
        return true;
    }

    private void ResetPinch()
    {
        firstTouchId = secondTouchId = -1;
        previousPinchDistance = 0;
    }

    // Local coordinates in page.visualMount at maximum zoom-out. Changing zoom does
    // not move this border; a viewport resize recalculates it for the new aspect ratio.
    public Rect MaximumViewBounds
    {
        get
        {
            var root = page.visualMount;
            var scale = new Vector3(authoredScale.x * minimumZoom, authoredScale.y * minimumZoom, authoredScale.z);
            var inverse = Matrix4x4.TRS(root.localPosition, root.localRotation, scale).inverse;
            spawnViewport.GetWorldCorners(corners);
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            foreach (var corner in corners)
            {
                Vector2 point = inverse.MultiplyPoint3x4(root.parent.InverseTransformPoint(corner));
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }
    }

    // 0 degrees is right, 90 is up. Clearance is the enemy radius plus any desired
    // gap, measured in visualMount local units. Parent the resulting visual there.
    public Vector2 GetSpawnPosition(float angleDegrees, float clearance)
    {
        Rect bounds = MaximumViewBounds;
        float margin = Mathf.Max(0, clearance) + 1;
        bounds.xMin -= margin;
        bounds.xMax += margin;
        bounds.yMin -= margin;
        bounds.yMax += margin;
        float radians = angleDegrees * Mathf.Deg2Rad;
        var direction = new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        float xDistance = Mathf.Abs(direction.x) < 0.00001f ? float.PositiveInfinity
            : (direction.x > 0 ? bounds.xMax : bounds.xMin) / direction.x;
        float yDistance = Mathf.Abs(direction.y) < 0.00001f ? float.PositiveInfinity
            : (direction.y > 0 ? bounds.yMax : bounds.yMin) / direction.y;
        return direction * Mathf.Min(xDistance, yDistance);
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Building towers in a level:
///  - Pick a tower: click its card in the HUD (PlanetPageController's selected card), or press 1-9 / 0.
///  - Choose a numbered socket in the quick placement panel to build without tracking the planet.
///  - Left click near the planet's edge: build it in the closest slot (money is checked).
///  - Right click a tower: sell it during setup/breaks for a full refund (legacy levels refund half).
/// </summary>
public class TowerPlacementInput : MonoBehaviour
{
    public LevelPlanetDefense defense;
    [Tooltip("Silverman Luna's moon. Empty = found automatically if there is one.")]
    public MoonOrbit moon;
    [Tooltip("Every TowerData that can be built, in hotkey order (1 = first). Names must match the HUD cards (e.g. 'Laser').")]
    public TowerData[] towers = new TowerData[0];
    [Tooltip("Optional: the HUD's tower list. Empty = found automatically.")]
    public PlanetPageController hud;
    [Tooltip("Optional text to show what's selected and why a build failed.")]
    public Text message;
    public Camera cam;
    [Tooltip("Clicks count if they're between these distances from the centre, in planet radii.")]
    public float minClickRadius = 0.5f;
    public float maxClickRadius = 1.8f;
    [Tooltip("The planet art sandbox (UnionPrimeVisualTest) places free test lasers on click. Turn it off in a real level.")]
    public bool disableArtSandbox = true;

    public TowerData Selected { get; private set; }

    private TowerPlacementPanel quickPlacement;
    private bool CanBuild => !SceneLoadingScreen.IsLoading
        && (GameplayLoopController.Instance?.CanBuild ?? (Time.timeScale > 0f));

    private string lastCardName;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();
    private readonly List<SpriteRenderer> towerSprites = new List<SpriteRenderer>();
    private PointerEventData pointerData;
    private EventSystem pointerEvents;

    private void Awake()
    {
        if (defense == null) defense = FindAnyObjectByType<LevelPlanetDefense>();
        if (hud == null) hud = FindAnyObjectByType<PlanetPageController>();
        if (moon == null) moon = FindAnyObjectByType<MoonOrbit>();
        if (cam == null) cam = Camera.main;
        if (towers.Length > 0) Selected = towers[0];
    }

    private void Start()
    {
        // its planet art is already built in Awake; only its click-to-place test mode stops
        if (disableArtSandbox)
            foreach (UnionPrimeVisualTest sandbox in FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None))
                sandbox.enabled = false;
    }

    public void SelectTower(TowerData tower)
    {
        if (System.Array.IndexOf(towers, tower) < 0) return;
        Selected = tower;
        Show("Selected " + tower.name + " ($" + tower.cost + ")");
    }

    private void Update()
    {
        var loop = GameplayLoopController.Instance;
        if (!CanBuild) return;
        ReadSelection();

        Mouse mouse = Mouse.current;
        if (mouse == null || defense == null || cam == null) return;
        bool left = mouse.leftButton.wasPressedThisFrame;
        bool right = mouse.rightButton.wasPressedThisFrame;
        if (!left && !right) return;

        if (!TryGetHoveredSlot(mouse.position.ReadValue(), out int slot, out bool onMoon, right)) return;

        if (right)
        {
            if (loop != null && !loop.CanSell) { Show("Sell for a full refund during setup or breaks."); return; }
            bool sold = onMoon ? moon.SellTower(slot) : defense.SellTower(slot);
            Show(sold ? "Sold." : "No tower there to sell.");
            return;
        }

        PlaceSelected(slot, onMoon);
    }

    private void LateUpdate()
    {
        if (quickPlacement == null && defense != null && Selected != null)
            quickPlacement = new TowerPlacementPanel(transform, defense, moon,
                hud != null && hud.planetName != null ? hud.planetName.font : FindAnyObjectByType<GameplayHUD>()?.font, PlaceSelected);
        int slot = -1;
        bool onMoon = false;
        if (CanBuild && Selected != null && Mouse.current != null)
            TryGetHoveredSlot(Mouse.current.position.ReadValue(), out slot, out onMoon);
        quickPlacement?.Refresh(CanBuild ? Selected : null, slot, onMoon);
    }

    // Shared by clicking and previewing, so the ghost always uses a placeable socket.
    private bool TryGetHoveredSlot(Vector2 screen, out int slot, out bool onMoon, bool includeTowerArt = false)
    {
        slot = -1;
        onMoon = false;
        if (defense == null || cam == null || !cam.pixelRect.Contains(screen) || OverControls(screen)) return false;
        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        // Selling targets the visible tower, which can extend beyond the socket's build hit area.
        if (includeTowerArt && TryGetTowerAt(world, out slot, out onMoon)) return true;
        // The moon first: it can overlap the main planet's screen area.
        if (moon != null && moon.TryGetSlotAt(world, out slot))
        {
            onMoon = true;
            return true;
        }
        Vector2 local = defense.Root.InverseTransformPoint(world);
        float dist = local.magnitude / Mathf.Max(0.0001f, defense.Radius);
        if (dist < minClickRadius || dist > maxClickRadius) return false;
        float angle = PlanetMath.Normalize(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        int nearest = defense.SlotAt(angle);
        if (defense.socketMounts.Length > 0 && Vector2.Distance(world, defense.SlotPosition(nearest)) > 0.7f) return false;
        slot = nearest;
        return true;
    }

    private bool TryGetTowerAt(Vector3 world, out int slot, out bool onMoon)
    {
        slot = -1;
        onMoon = false;
        float closest = float.PositiveInfinity;
        int count = defense.SlotCount + (moon != null ? moon.SlotCount : 0);
        for (int i = 0; i < count; i++)
        {
            bool isMoon = i >= defense.SlotCount;
            int candidate = isMoon ? i - defense.SlotCount : i;
            BaseTowerData tower = isMoon ? moon.TowerIn(candidate) : defense.TowerIn(candidate);
            if (tower == null) continue;
            tower.GetComponentsInChildren<SpriteRenderer>(false, towerSprites);
            foreach (SpriteRenderer renderer in towerSprites)
            {
                if (!renderer.enabled || renderer.sprite == null) continue;
                Bounds bounds = renderer.bounds;
                Vector3 point = world;
                point.z = bounds.center.z;
                if (!bounds.Contains(point)) continue;
                float distance = ((Vector2)(world - tower.transform.position)).sqrMagnitude;
                if (distance >= closest) continue;
                closest = distance;
                slot = candidate;
                onMoon = isMoon;
            }
        }
        return slot >= 0;
    }

    private void OnDisable()
    {
        quickPlacement?.Hide();
    }

    private void PlaceSelected(int slot, bool onMoon)
    {
        if (!isActiveAndEnabled || !CanBuild) return;
        if (Selected == null) { Show("Pick a tower first."); return; }

        string reason;
        bool built = onMoon ? moon.TryPlaceTower(Selected, slot, out reason)
            : defense.TryPlaceTower(Selected, slot, out reason);
        string result = built ? "Built " + Selected.name + (onMoon ? " on the moon." : ".") : reason;
        Show(result);
        quickPlacement?.ShowResult(result);
    }

    private void ReadSelection()
    {
        // HUD card
        if (hud != null && hud.SelectedCard != null && hud.SelectedCard.tower != null)
        {
            string cardName = hud.SelectedCard.tower.displayName;
            if (cardName != lastCardName)
            {
                lastCardName = cardName;
                TowerData match = Find(cardName);
                if (!hud.SelectedCard.Available) { Selected = null; Show(cardName + " is locked on this planet."); }
                else if (match != null) { Selected = match; Show("Selected " + match.name + " ($" + match.cost + ")"); }
            }
        }

        // hotkeys 1-9, 0
        Keyboard kb = Keyboard.current;
        if (kb == null) return;
        for (int i = 0; i < 10 && i < towers.Length; i++)
        {
            Key key = i == 9 ? Key.Digit0 : Key.Digit1 + i;
            if (kb[key].wasPressedThisFrame && towers[i] != null)
            {
                Selected = towers[i];
                Show("Selected " + Selected.name + " ($" + Selected.cost + ")");
            }
        }
    }

    private TowerData Find(string displayName)
    {
        string key = Normalize(displayName);
        foreach (TowerData t in towers)
            if (t != null && Normalize(t.name) == key) return t;
        return null;
    }

    private static string Normalize(string s)
    {
        return string.IsNullOrEmpty(s) ? "" : s.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }

    private bool OverControls(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        if (pointerData == null || pointerEvents != EventSystem.current)
        {
            pointerEvents = EventSystem.current;
            pointerData = new PointerEventData(pointerEvents);
        }
        pointerData.position = screen;
        uiHits.Clear();
        EventSystem.current.RaycastAll(pointerData, uiHits);
        foreach (RaycastResult r in uiHits)
            if ((quickPlacement != null && quickPlacement.Contains(r.gameObject.transform))
                || r.gameObject.GetComponentInParent<Selectable>() != null || r.gameObject.GetComponentInParent<ScrollRect>() != null)
                return true;
        return false;
    }

    private void Show(string text)
    {
        if (message != null) message.text = text;
        else Debug.Log("[Towers] " + text);
    }
}

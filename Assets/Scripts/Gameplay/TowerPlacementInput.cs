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
///  - Right click near a tower: sell it (half the cost back).
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

        Vector2 screen = mouse.position.ReadValue();
        if (OverControls(screen)) return;

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

        // the moon first (it's smaller and sits over the planet's space)
        int moonSlot;
        if (moon != null && moon.TryGetSlotAt(world, out moonSlot))
        {
            if (right) { Show(moon.SellTower(moonSlot) ? "Sold." : "No tower there to sell."); return; }
            PlaceSelected(moonSlot, true);
            return;
        }

        Vector2 local = defense.Root.InverseTransformPoint(world);
        float dist = local.magnitude / Mathf.Max(0.0001f, defense.Radius);
        if (dist < minClickRadius || dist > maxClickRadius) return;

        float angle = PlanetMath.Normalize(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        int slot = defense.SlotAt(angle);
        if (defense.socketMounts.Length > 0 && Vector2.Distance(world, defense.SlotPosition(slot)) > 0.7f) return;

        if (right)
        {
            if (loop != null && !loop.CanSell) { Show("Sell for a full refund during setup or breaks."); return; }
            Show(defense.SellTower(slot) ? "Sold." : "No tower there to sell.");
            return;
        }

        PlaceSelected(slot, false);
    }

    private void LateUpdate()
    {
        if (quickPlacement == null && defense != null && Selected != null)
            quickPlacement = new TowerPlacementPanel(transform, defense, moon,
                hud != null && hud.planetName != null ? hud.planetName.font : FindAnyObjectByType<GameplayHUD>()?.font, PlaceSelected);
        quickPlacement?.Refresh(CanBuild ? Selected : null);
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
        var data = new PointerEventData(EventSystem.current) { position = screen };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
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

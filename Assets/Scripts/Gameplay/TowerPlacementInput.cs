using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Building towers in a level:
///  - Pick a tower: click its card in the HUD (PlanetPageController's selected card), or press 1-9 / 0.
///  - Left click near the planet's edge: build it in the closest slot (money is checked).
///  - Right click near a tower: sell it (half the cost back).
/// </summary>
public class TowerPlacementInput : MonoBehaviour
{
    public LevelPlanetDefense defense;
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

    private string lastCardName;
    private readonly List<RaycastResult> uiHits = new List<RaycastResult>();

    private void Awake()
    {
        if (defense == null) defense = FindAnyObjectByType<LevelPlanetDefense>();
        if (hud == null) hud = FindAnyObjectByType<PlanetPageController>();
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

    private void Update()
    {
        ReadSelection();

        Mouse mouse = Mouse.current;
        if (mouse == null || defense == null || cam == null) return;
        bool left = mouse.leftButton.wasPressedThisFrame;
        bool right = mouse.rightButton.wasPressedThisFrame;
        if (!left && !right) return;

        Vector2 screen = mouse.position.ReadValue();
        if (OverButton(screen)) return; // clicks on HUD buttons are for the HUD

        Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
        Vector2 local = defense.Root.InverseTransformPoint(world);
        float dist = local.magnitude / Mathf.Max(0.0001f, defense.Radius);
        if (dist < minClickRadius || dist > maxClickRadius) return;

        float angle = PlanetMath.Normalize(Mathf.Atan2(local.y, local.x) * Mathf.Rad2Deg);
        int slot = defense.SlotAt(angle);

        if (right)
        {
            Show(defense.SellTower(slot) ? "Sold." : "No tower there to sell.");
            return;
        }

        if (Selected == null) { Show("Pick a tower first."); return; }
        string reason;
        if (defense.TryPlaceTower(Selected, slot, out reason)) Show("Built " + Selected.name + ".");
        else Show(reason);
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

    private bool OverButton(Vector2 screen)
    {
        if (EventSystem.current == null) return false;
        var data = new PointerEventData(EventSystem.current) { position = screen };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);
        foreach (RaycastResult r in uiHits)
            if (r.gameObject.GetComponentInParent<Selectable>() != null || r.gameObject.GetComponentInParent<ScrollRect>() != null)
                return true;
        return false;
    }

    private void Show(string text)
    {
        if (message != null) message.text = text;
        else Debug.Log("[Towers] " + text);
    }
}

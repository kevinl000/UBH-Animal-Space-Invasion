using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Scene-local art sandbox: placement does not spend resources or create combat towers.
public class UnionPrimeVisualTest : MonoBehaviour
{
    [Serializable]
    public struct FacadeLayer
    {
        public Sprite sprite;
        public Vector2 origin;
    }

    [Serializable]
    public struct CraterSite
    {
        public int number;
        public Vector2 mount;
        public float clockwiseAngle;
    }

    public GameObject silhouette;
    public Sprite rearGround;
    public Sprite terrain;
    public Sprite foreground;
    public Sprite laserBase;
    public Sprite laserGun;
    public Sprite laserPortrait;
    public TowerPresentation laser;
    public FacadeLayer[] facades;
    public CraterSite[] sites;

    public int PlacedCount { get; private set; }
    public float AimDegrees { get; private set; }
    public RectTransform ArtRoot { get; private set; }
    public int SiteCount => sites.Length;

    private PlanetPageController page;
    private Canvas canvas;
    private RectTransform towerLayer;
    private RectTransform[] placed;
    private RectTransform[] guns;
    private RectTransform ghost;
    private RectTransform ghostGun;
    private bool hintsReady;
    private const float SourceSize = 1254;
    private const float TowerScale = 1.5f;
    private const float HitRadius = 60;

    private void Awake()
    {
        page = GetComponentInParent<PlanetPageController>();
        canvas = GetComponentInParent<Canvas>();
        if (silhouette != null) silhouette.SetActive(false);
        ArtRoot = Rect("Union Prime Art", transform, new Vector2(SourceSize, SourceSize));
        ArtRoot.localScale = Vector3.one * (640f / SourceSize);
        Layer("Rear ground", ArtRoot, rearGround, Vector2.zero);
        foreach (var facade in facades)
            Layer(facade.sprite.name, ArtRoot, facade.sprite, facade.origin);
        Layer("Terrain", ArtRoot, terrain, Vector2.zero);
        towerLayer = Rect("Placed Lasers", ArtRoot, new Vector2(SourceSize, SourceSize));
        placed = new RectTransform[sites.Length];
        guns = new RectTransform[sites.Length];
        ghost = MakeLaser("Placement preview", out ghostGun);
        ghost.gameObject.AddComponent<CanvasGroup>().alpha = 0.5f;
        ghost.gameObject.SetActive(false);
        Layer("Foreground soil", ArtRoot, foreground, Vector2.zero);
    }

    private void Update()
    {
        if (page == null) return;
        if (!hintsReady)
        {
            page.planetDescription.text = "Laser: click a crater to build / Right-click to remove\nQ / E: aim    R: reset aim    Free placement";
            var hint = page.transform.Find("InspectionHint");
            if (hint != null) hint.GetComponent<Text>().text = "LASER VISUAL TEST / FREE";
            foreach (var card in page.cards)
                if (card.tower == laser)
                {
                    card.cost.text = "FREE / TEST";
                    card.portrait.sprite = laserPortrait;
                }
            hintsReady = true;
        }
        if (LaserSelected())
            page.towerPortrait.sprite = laserPortrait;
        ghost.gameObject.SetActive(false);
        if (!CanInteract()) return;

        var keyboard = Keyboard.current;
        if (keyboard != null)
        {
            float direction = (keyboard.eKey.isPressed ? 1 : 0) - (keyboard.qKey.isPressed ? 1 : 0);
            if (direction != 0) SetAim(AimDegrees + direction * 60 * Time.unscaledDeltaTime);
            if (keyboard.rKey.wasPressedThisFrame) SetAim(0);
        }
        var mouse = Mouse.current;
        if (mouse == null || (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())) return;
        var camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(ArtRoot, mouse.position.ReadValue(), camera, out var point)) return;
        int index = FindSite(point);
        if (index < 0) return;
        if (mouse.rightButton.wasPressedThisFrame) RemoveAt(index);
        if (LaserSelected() && placed[index] == null)
        {
            PlaceTransform(ghost, index);
            ghost.gameObject.SetActive(true);
            if (mouse.leftButton.wasPressedThisFrame && TryPlace(index)) ghost.gameObject.SetActive(false);
        }
    }

    private bool CanInteract()
    {
        return isActiveAndEnabled && page != null && page.controls.interactable
            && !SceneLoadingScreen.IsLoading;
    }

    private bool LaserSelected()
    {
        return page.SelectedCard != null && page.SelectedCard.tower == laser;
    }

    private int FindSite(Vector2 localPoint)
    {
        int closest = -1;
        float best = HitRadius * HitRadius;
        for (int i = 0; i < sites.Length; i++)
        {
            float distance = (localPoint - LocalMount(i)).sqrMagnitude;
            if (distance <= best)
            {
                best = distance;
                closest = i;
            }
        }
        return closest;
    }

    public Vector2 LocalMount(int index)
    {
        return new Vector2(sites[index].mount.x - SourceSize / 2, SourceSize / 2 - sites[index].mount.y);
    }

    public bool IsOccupied(int index)
    {
        return index >= 0 && index < placed.Length && placed[index] != null;
    }

    public bool TryPlace(int index)
    {
        if (!CanInteract() || !LaserSelected() || index < 0 || index >= placed.Length || IsOccupied(index)) return false;
        placed[index] = MakeLaser("Laser / site " + sites[index].number, out guns[index]);
        PlaceTransform(placed[index], index);
        PlacedCount++;
        return true;
    }

    public bool RemoveAt(int index)
    {
        if (!CanInteract() || !IsOccupied(index)) return false;
        placed[index].gameObject.SetActive(false);
        Destroy(placed[index].gameObject);
        placed[index] = null;
        guns[index] = null;
        PlacedCount--;
        return true;
    }

    public void SetAim(float degrees)
    {
        AimDegrees = Mathf.Clamp(degrees, -60, 60);
        var rotation = Quaternion.Euler(0, 0, -AimDegrees);
        ghostGun.localRotation = rotation;
        foreach (var gun in guns)
            if (gun != null) gun.localRotation = rotation;
    }

    private void PlaceTransform(RectTransform tower, int index)
    {
        tower.anchoredPosition = LocalMount(index);
        tower.localRotation = Quaternion.Euler(0, 0, -sites[index].clockwiseAngle);
    }

    private RectTransform MakeLaser(string title, out RectTransform gun)
    {
        var tower = Rect(title, towerLayer, Vector2.zero);
        tower.localScale = Vector3.one * TowerScale;
        var body = SpriteImage("Base and operator", tower, laserBase);
        body.pivot = new Vector2(34f / 70, 1f / 72);
        gun = SpriteImage("Gun", tower, laserGun);
        gun.pivot = new Vector2(12f / 24, 1 - 28f / 33);
        gun.anchoredPosition = new Vector2(30, 47);
        gun.localRotation = Quaternion.Euler(0, 0, -AimDegrees);
        return tower;
    }

    private static void Layer(string title, Transform parent, Sprite sprite, Vector2 sourceOrigin)
    {
        var rect = SpriteImage(title, parent, sprite);
        rect.pivot = new Vector2(0, 1);
        rect.anchoredPosition = new Vector2(sourceOrigin.x - SourceSize / 2, SourceSize / 2 - sourceOrigin.y);
    }

    private static RectTransform SpriteImage(string title, Transform parent, Sprite sprite)
    {
        var rect = Rect(title, parent, sprite.rect.size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return rect;
    }

    private static RectTransform Rect(string title, Transform parent, Vector2 size)
    {
        var rect = new GameObject(title, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        return rect;
    }
}

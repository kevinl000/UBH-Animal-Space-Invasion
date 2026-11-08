using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlanetPageController : MonoBehaviour
{
    public PlanetPresentation planet;
    public CanvasGroup controls;
    public Text planetName;
    public Text planetDescription;
    public Text levelLabel;
    public Text availableCount;
    public Text towerName;
    public Text towerAvailability;
    public Text towerStats;
    public Text towerDescription;
    public Image towerPortrait;
    public TowerCard[] cards;
    public ScrollRect towerScroll;
    public Button returnButton;
    public RectTransform visualMount;
    public AudioClip confirmSound;
    public AudioClip backSound;
    public TowerCard SelectedCard { get; private set; }

    private bool changingScene;

    private void Start()
    {
        RefreshPresentation();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(cards[0].gameObject);
    }

    public void RefreshPresentation()
    {
        planetName.text = planet.displayName;
        levelLabel.text = planet.LevelLabel.ToUpperInvariant() + " / CAMPUS DEFENSE";
        planetDescription.text = planet.description;
        int count = 0;
        foreach (var card in cards)
        {
            card.Refresh(planet.level);
            if (card.Available) count++;
        }
        availableCount.text = count + " / " + cards.Length + " AVAILABLE";
        ShowDetails(cards[0]);
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            ReturnToLevelSelect();
    }

    public void SelectTower(TowerCard card)
    {
        if (changingScene || SceneLoadingScreen.IsLoading) return;
        ShowDetails(card);
        AudioManager.Instance.PlaySFX(confirmSound, 0.65f);
    }

    private void ShowDetails(TowerCard card)
    {
        SelectedCard = card;
        foreach (var item in cards) item.selection.enabled = item == card;
        var tower = card.tower;
        towerName.text = tower.displayName;
        towerPortrait.sprite = tower.portrait;
        towerAvailability.text = card.Available ? "AVAILABLE AT THIS DESTINATION" : "LOCKED / UNLOCKS AT LEVEL " + tower.unlockLevel;
        string fuel = tower.fuelRate == "0" ? "FREE" : tower.fuelRate
            + (tower.displayName == "Oil Rig" ? " F/s" : tower.displayName == "Gamma Radar" ? " F/pulse" : " F/shot");
        towerStats.text = "HP " + tower.health + "    COST $" + tower.cost + "\nFUEL " + fuel
            + "    FOV " + tower.fieldOfView + "°\nDAMAGE " + tower.damage + "    " + tower.attackType;
        towerDescription.text = tower.description;
    }

    public void RevealCard(TowerCard card)
    {
        Canvas.ForceUpdateCanvases();
        var content = towerScroll.content;
        var viewport = towerScroll.viewport;
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, card.transform);
        float offset = bounds.min.y < viewport.rect.yMin ? viewport.rect.yMin - bounds.min.y
            : bounds.max.y > viewport.rect.yMax ? viewport.rect.yMax - bounds.max.y : 0;
        towerScroll.StopMovement();
        var position = content.anchoredPosition;
        position.y = Mathf.Clamp(position.y + offset, 0, Mathf.Max(0, content.rect.height - viewport.rect.height));
        content.anchoredPosition = position;
    }

    public void ReturnToLevelSelect()
    {
        if (changingScene || SceneLoadingScreen.IsLoading) return;
        GameManager.Instance.GoToLevelSelect();
        if (!SceneController.Instance.IsLoading) return;
        changingScene = true;
        controls.interactable = false;
        AudioManager.Instance.PlaySFX(backSound);
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TowerCard : MonoBehaviour, ISelectHandler
{
    public TowerPresentation tower;
    public Button button;
    public Image portrait;
    public Image selection;
    public Text title;
    public Text cost;
    public Text availability;
    public bool Available { get; private set; }

    private PlanetPageController page;

    private void Awake()
    {
        page = GetComponentInParent<PlanetPageController>();
        button.onClick.AddListener(Inspect);
    }

    public void Refresh(int level)
    {
        Available = tower.unlockLevel <= level;
        title.text = tower.displayName;
        portrait.sprite = tower.portrait;
        portrait.color = Available ? Color.white : new Color32(100, 116, 140, 255);
        cost.text = "$" + tower.cost;
        availability.text = Available ? "AVAILABLE" : "LOCKED / LEVEL " + tower.unlockLevel;
        availability.color = Available ? new Color32(147, 187, 133, 255) : new Color32(155, 183, 218, 255);
    }

    private void Inspect()
    {
        page.SelectTower(this);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (page != null && !SceneLoadingScreen.IsLoading)
            page.RevealCard(this);
    }
}

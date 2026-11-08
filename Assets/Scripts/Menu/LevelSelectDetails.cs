using System;
using UnityEngine;
using UnityEngine.UI;

// Scene-local presentation data; gameplay level definitions belong to the gameplay systems.
public class LevelSelectDetails : MonoBehaviour
{
    // The hackathon submission ends at Davis Nova (level 2).
    private const int LastPlayableLevel = 2;

    [Serializable]
    public class Destination
    {
        public string landmarkAndLevel;
        [TextArea(2, 4)] public string description;
        public string bossName;
        public Sprite bossSprite;
        public PlanetPresentation planet;
    }

    public Destination[] destinations;
    public Text landmarkAndLevel;
    public Text description;
    public Text bossName;
    public Image bossPreview;
    public Sprite unknownBoss;
    public Button playButton;
    public int SelectedIndex { get; private set; }

    public string SelectedSceneName => destinations[SelectedIndex].planet != null
        && destinations[SelectedIndex].planet.level <= LastPlayableLevel
        ? destinations[SelectedIndex].planet.sceneName : null;

    public void ShowDestination(int index)
    {
        var destination = destinations[index];
        SelectedIndex = index;
        landmarkAndLevel.text = destination.landmarkAndLevel;
        description.text = destination.description;
        bossName.text = string.IsNullOrWhiteSpace(destination.bossName) ? "Unknown" : destination.bossName;
        bossPreview.sprite = destination.bossSprite != null ? destination.bossSprite : unknownBoss;
        playButton.interactable = !string.IsNullOrEmpty(SelectedSceneName);
        playButton.GetComponentInChildren<Text>().text = playButton.interactable ? "PLAY" : "Coming soon";
    }
}

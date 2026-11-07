using System;
using UnityEngine;
using UnityEngine.UI;

// Scene-local presentation data; gameplay level definitions belong to the gameplay systems.
public class LevelSelectDetails : MonoBehaviour
{
    [Serializable]
    public class Destination
    {
        public string landmarkAndLevel;
        [TextArea(2, 4)] public string description;
        public string bossName;
        public Sprite bossSprite;
    }

    public Destination[] destinations;
    public Text landmarkAndLevel;
    public Text description;
    public Text bossName;
    public Image bossPreview;
    public Sprite unknownBoss;
    public Button playButton;

    public void ShowDestination(int index)
    {
        var destination = destinations[index];
        landmarkAndLevel.text = destination.landmarkAndLevel;
        description.text = destination.description;
        bossName.text = string.IsNullOrWhiteSpace(destination.bossName) ? "Unknown" : destination.bossName;
        bossPreview.sprite = destination.bossSprite != null ? destination.bossSprite : unknownBoss;
        // No scene mappings have been agreed yet; selection never launches gameplay.
        playButton.interactable = false;
    }
}

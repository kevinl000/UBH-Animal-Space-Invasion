using UnityEngine;

// Presentation motion for the three independent, six-socket buffet worlds.
[DisallowMultipleComponent]
public class C3VisualMotion : MonoBehaviour
{
    public RectTransform[] planets;
    [Min(0)] public float orbitRadius = 180f;
    [Tooltip("Clockwise degrees per second; presentation only.")]
    public float orbitSpeed = 4;
    [Tooltip("Clockwise degrees per second around each planet's own center.")]
    public float spinSpeed = 10;

    private PlanetPageController page;
    private float orbitDegrees;
    private float spinDegrees;

    private void Awake()
    {
        page = GetComponentInParent<PlanetPageController>();
        ApplyPose();
    }

    private void Update()
    {
        if (SceneLoadingScreen.IsLoading || (page != null && !page.controls.interactable)) return;
        orbitDegrees = Mathf.Repeat(orbitDegrees + orbitSpeed * Time.deltaTime, 360);
        spinDegrees = Mathf.Repeat(spinDegrees + spinSpeed * Time.deltaTime, 360);
        ApplyPose();
    }

    private void ApplyPose()
    {
        for (int i = 0; i < planets.Length; i++)
        {
            float angle = (90 - i * 120 - orbitDegrees) * Mathf.Deg2Rad;
            planets[i].anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * orbitRadius;
            planets[i].localRotation = Quaternion.Euler(0, 0, -spinDegrees);
        }
    }
}

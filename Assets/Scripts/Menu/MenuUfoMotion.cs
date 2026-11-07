using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MenuUfoMotion : MonoBehaviour
{
    public Vector2 travel = new Vector2(12f, 7f);
    public float period = 7f;

    private RectTransform rect;
    private Vector2 origin;

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        origin = rect.anchoredPosition;
    }

    private void Update()
    {
        float phase = Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.1f, period);
        // Whole reference pixels keep the sprite's motion consistent with the artwork.
        rect.anchoredPosition = origin + new Vector2(
            Mathf.Round(Mathf.Sin(phase) * travel.x),
            Mathf.Round(Mathf.Sin(phase * 1.3f) * travel.y));
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonFeedback : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    public AudioSource audioSource;
    public AudioClip selectSound;

    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (button.IsInteractable() && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(gameObject);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (button.IsInteractable() && audioSource != null && selectSound != null)
            audioSource.PlayOneShot(selectSound, 0.35f);
    }
}

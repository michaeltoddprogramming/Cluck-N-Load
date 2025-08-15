using UnityEngine;
using UnityEngine.EventSystems;

public class UIButtonSound : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    public MainMenuController menuController;
    private Vector3 originalScale;
    private LTDescr scaleTween;

    void Awake()
    {
        if (menuController == null)
            menuController = FindObjectOfType<MainMenuController>();
        originalScale = transform.localScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (menuController != null && menuController.uiAudioSource != null && menuController.hoverSound != null)
            menuController.uiAudioSource.PlayOneShot(menuController.hoverSound);

        // Tween scale up
        if (scaleTween != null) LeanTween.cancel(gameObject);
        scaleTween = LeanTween.scale(gameObject, originalScale * 1.07f, 0.15f).setEase(LeanTweenType.easeOutQuad);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // Tween scale back to original
        if (scaleTween != null) LeanTween.cancel(gameObject);
        scaleTween = LeanTween.scale(gameObject, originalScale, 0.15f).setEase(LeanTweenType.easeOutQuad);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (menuController != null && menuController.uiAudioSource != null && menuController.clickSound != null)
            menuController.uiAudioSource.PlayOneShot(menuController.clickSound);
    }
}
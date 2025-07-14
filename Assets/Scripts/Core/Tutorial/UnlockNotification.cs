using UnityEngine;
using TMPro;

public class UnlockNotification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private CanvasGroup canvasGroup;
    
    public void Initialize(string title, string description)
    {
        if (titleText != null)
            titleText.text = title;
            
        if (descriptionText != null)
            descriptionText.text = description;
            
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }
    
    public void SetAlpha(float alpha)
    {
        if (canvasGroup != null)
            canvasGroup.alpha = alpha;
    }
}

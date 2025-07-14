using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script for UI arrow prefab - points to UI elements during tutorial
/// </summary>
public class UIArrowPrefab : MonoBehaviour
{
    [Header("Components")]
    public Image arrowImage;
    public Animator animator;
    
    [Header("Settings")]
    public float pulseSpeed = 1f;
    public Vector2 offset = Vector2.zero;
    
    private RectTransform rectTransform;
    private RectTransform targetRectTransform;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        AutoAssignComponents();
    }
    
    private void AutoAssignComponents()
    {
        if (arrowImage == null)
            arrowImage = GetComponent<Image>();
            
        if (animator == null)
            animator = GetComponent<Animator>();
    }
    
    private void Update()
    {
        if (targetRectTransform != null)
        {
            // Position arrow near target UI element
            Vector2 targetPos = targetRectTransform.anchoredPosition + offset;
            rectTransform.anchoredPosition = targetPos;
        }
    }
    
    public void SetTarget(RectTransform target)
    {
        targetRectTransform = target;
        gameObject.SetActive(target != null);
        
        if (target != null)
        {
            // Point arrow toward target
            Vector2 direction = (target.anchoredPosition - rectTransform.anchoredPosition).normalized;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            rectTransform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }
    }
    
    public void ClearTarget()
    {
        targetRectTransform = null;
        gameObject.SetActive(false);
    }
    
    public void StartPulseAnimation()
    {
        if (animator != null)
        {
            animator.SetTrigger("Pulse");
        }
    }
}

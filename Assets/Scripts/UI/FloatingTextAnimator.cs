using UnityEngine;
using TMPro;

public class FloatingTextAnimator : MonoBehaviour
{
    private Vector3 startPosition;
    private Vector3 endPosition;
    private float duration;
    private float elapsed;
    private float delay;
    private bool animationStarted;
    private TextMeshProUGUI textComponent;
    private UnityEngine.UI.Image backgroundImage;
    
    public void Initialize(Vector3 start, Vector3 end, float animDuration)
    {
        InitializeWithDelay(start, end, animDuration, 0f);
    }
    
    public void InitializeWithDelay(Vector3 start, Vector3 end, float animDuration, float animDelay)
    {
        startPosition = start;
        endPosition = end;
        duration = animDuration;
        delay = animDelay;
        elapsed = 0f;
        animationStarted = delay <= 0f;
        
        // Get components for fading
        textComponent = GetComponent<TextMeshProUGUI>();
        backgroundImage = GetComponentInChildren<UnityEngine.UI.Image>();
        
        // If there's a delay, hide the text initially
        if (delay > 0f)
        {
            SetAlpha(0f);
        }
    }
    
    void Update()
    {
        elapsed += Time.deltaTime;
        
        // Handle delay period
        if (!animationStarted)
        {
            if (elapsed >= delay)
            {
                animationStarted = true;
                elapsed = 0f; // Reset elapsed time for animation
                SetAlpha(1f); // Make visible
            }
            return;
        }
        
        float progress = elapsed / duration;
        
        if (progress >= 1f)
        {
            // Animation complete, destroy the object
            Destroy(gameObject);
            return;
        }
        
        // Move upward with easing
        float easedProgress = Mathf.Sin(progress * Mathf.PI * 0.5f); // Ease out
        transform.position = Vector3.Lerp(startPosition, endPosition, easedProgress);
        
        // Fade out (start fading after 50% of animation)
        float fadeProgress = Mathf.Clamp01((progress - 0.5f) * 2f);
        float alpha = 1f - fadeProgress;
        SetAlpha(alpha);
    }
    
    private void SetAlpha(float alpha)
    {
        if (textComponent != null)
        {
            Color textColor = textComponent.color;
            textColor.a = alpha;
            textComponent.color = textColor;
        }
        
        if (backgroundImage != null)
        {
            Color bgColor = backgroundImage.color;
            bgColor.a = alpha * 0.6f; // Background is always more transparent
            backgroundImage.color = bgColor;
        }
    }
}
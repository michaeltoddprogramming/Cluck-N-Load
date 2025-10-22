using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class NotificationManager : MonoBehaviour
{
    [System.Serializable]
    public class NotificationTheme
    {
        public string themeName;
        public Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.9f);
        public Color borderColor = Color.white;
        public Color textColor = Color.white;
        public Color iconColor = Color.white;
        public Sprite iconSprite;
        public AudioClip soundEffect;
        public float soundVolume = 1f;
    }

    [Header("Notification Prefab")]
    public GameObject notificationPrefab;
    public Transform notificationContainer;
    public Canvas notificationCanvas;

    [Header("Audio")]
    public AudioSource audioSource;
    
    [Header("Themes")]
    public NotificationTheme[] themes = new NotificationTheme[]
    {
        // Success - Green with golden glow like the upgrade items
        new NotificationTheme { 
            themeName = "Success", 
            backgroundColor = new Color(0.1f, 0.4f, 0.1f, 0.95f),
            borderColor = new Color(0.3f, 0.8f, 0.3f, 1f),
            textColor = new Color(0.9f, 1f, 0.9f, 1f),
            iconColor = new Color(0.4f, 1f, 0.4f, 1f)
        },
        // Warning - Orange/amber glow
        new NotificationTheme { 
            themeName = "Warning", 
            backgroundColor = new Color(0.4f, 0.2f, 0.0f, 0.95f),
            borderColor = new Color(1f, 0.6f, 0.1f, 1f),
            textColor = new Color(1f, 0.95f, 0.8f, 1f),
            iconColor = new Color(1f, 0.7f, 0.2f, 1f)
        },
        // Error - Red with bright red glow
        new NotificationTheme { 
            themeName = "Error", 
            backgroundColor = new Color(0.4f, 0.1f, 0.1f, 0.95f),
            borderColor = new Color(1f, 0.3f, 0.3f, 1f),
            textColor = new Color(1f, 0.9f, 0.9f, 1f),
            iconColor = new Color(1f, 0.4f, 0.4f, 1f)
        },
        // Info - Blue with electric blue glow
        new NotificationTheme { 
            themeName = "Info", 
            backgroundColor = new Color(0.1f, 0.2f, 0.4f, 0.95f),
            borderColor = new Color(0.3f, 0.6f, 1f, 1f),
            textColor = new Color(0.9f, 0.95f, 1f, 1f),
            iconColor = new Color(0.4f, 0.7f, 1f, 1f)
        },
        // Achievement - Gold with brilliant golden glow like "NEW" items
        new NotificationTheme { 
            themeName = "Achievement", 
            backgroundColor = new Color(0.3f, 0.25f, 0.1f, 0.95f),
            borderColor = new Color(1f, 0.8f, 0.2f, 1f),
            textColor = new Color(1f, 0.95f, 0.8f, 1f),
            iconColor = new Color(1f, 0.9f, 0.3f, 1f)
        },
        // New - Bright purple/magenta like the "NEW" highlight in your image
        new NotificationTheme { 
            themeName = "New", 
            backgroundColor = new Color(0.3f, 0.1f, 0.3f, 0.95f),
            borderColor = new Color(1f, 0.4f, 1f, 1f),
            textColor = new Color(1f, 0.9f, 1f, 1f),
            iconColor = new Color(1f, 0.5f, 1f, 1f)
        }
    };

    [Header("Animation Settings")]
    public float slideInDuration = 0.6f;
    public float displayDuration = 3f;
    public float slideOutDuration = 0.4f;
    public float bounceScale = 1.1f;
    public float sparkleIntensity = 0.5f;
    
    [Header("Notification Positioning & Size")]
    [Range(0f, 1f)] public float anchorX = 0.5f; // 0 = left, 0.5 = center, 1 = right
    [Range(0f, 1f)] public float anchorY = 0.5f; // 0 = bottom, 0.5 = center, 1 = top
    public Vector2 notificationSize = new Vector2(500f, 150f);
    public Vector2 positionOffset = Vector2.zero;
    
    [Header("Glow Effects")]
    public bool enableGlowEffects = true;
    public float glowIntensity = 2f;
    public float glowPulseSpeed = 0.8f;
    public float outerGlowSize = 20f;

    private static NotificationManager instance;
    public static NotificationManager Instance
    {
        get
        {
            if (instance == null)
                instance = FindObjectOfType<NotificationManager>();
            return instance;
        }
    }

    private Queue<NotificationData> notificationQueue = new Queue<NotificationData>();
    private bool isShowingNotification = false;

    [System.Serializable]
    private class NotificationData
    {
        public string title;
        public string message;
        public string theme;
        public float duration;
        public System.Action onComplete;
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }

    public static void ShowNotification(string title, string message, string theme = "Info", float duration = 3f, System.Action onComplete = null)
    {
        if (Instance == null) return;

        NotificationData data = new NotificationData
        {
            title = title,
            message = message,
            theme = theme,
            duration = duration,
            onComplete = onComplete
        };

        Instance.notificationQueue.Enqueue(data);
        Instance.ProcessQueue();
    }

    public static void ShowSuccess(string title, string message = "", float duration = 3f)
    {
        ShowNotification(title, message, "Success", duration);
    }

    public static void ShowWarning(string title, string message = "", float duration = 4f)
    {
        ShowNotification(title, message, "Warning", duration);
    }

    public static void ShowError(string title, string message = "", float duration = 5f)
    {
        ShowNotification(title, message, "Error", duration);
    }

    public static void ShowAchievement(string title, string message = "", float duration = 4f)
    {
        ShowNotification(title, message, "Achievement", duration);
    }

    private void ProcessQueue()
    {
        if (isShowingNotification || notificationQueue.Count == 0) return;

        NotificationData data = notificationQueue.Dequeue();
        StartCoroutine(ShowNotificationCoroutine(data));
    }

    private IEnumerator ShowNotificationCoroutine(NotificationData data)
    {
        isShowingNotification = true;

        GameObject notification = CreateNotificationObject(data);
        if (notification == null)
        {
            isShowingNotification = false;
            yield break;
        }

        // Get components
        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        
        // Setup initial state for animation - use editor settings
        Vector3 targetPosition = positionOffset; // Use editor offset
        Vector3 startPosition = targetPosition + Vector3.right * (notificationSize.x + 100f); // Start off-screen based on size
        
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        // Play sound effect
        NotificationTheme theme = GetTheme(data.theme);
        if (theme?.soundEffect != null && audioSource != null)
        {
            audioSource.PlayOneShot(theme.soundEffect, theme.soundVolume);
        }

        // === DRAMATIC ENTRANCE ANIMATION ===
        // Start with a flash effect (fixed to not create lines)
        StartCoroutine(FlashEffect(notification, theme));
        
        // Slide in from right with smoother animation (less bouncy)
        LeanTween.moveX(rectTransform, targetPosition.x, slideInDuration)
            .setEase(LeanTweenType.easeOutQuart)
            .setDelay(0.1f);

        // Scale up with gentle entrance (much less bouncy)
        LeanTween.scale(notification, Vector3.one, slideInDuration * 0.8f)
            .setEase(LeanTweenType.easeOutQuart)
            .setDelay(0.2f);

        // Fade in
        LeanTween.alphaCanvas(canvasGroup, 1f, slideInDuration * 0.6f)
            .setDelay(0.15f);

        // Gentle settle effect (much less bouncy)
        yield return new WaitForSeconds(slideInDuration * 0.9f);
        
        LeanTween.scale(notification, Vector3.one * 1.03f, 0.2f)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(notification, Vector3.one, 0.3f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });

        // Sparkle effects during display - RE-ENABLED with line effects removed
        StartCoroutine(SparkleEffectNoLines(notification, data.duration));

        // === DISPLAY DURATION ===
        yield return new WaitForSeconds(data.duration);

        // === EXIT ANIMATION ===
        // Pulse before exit
        LeanTween.scale(notification, Vector3.one * 0.95f, 0.15f)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(notification, Vector3.one, 0.15f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });

        yield return new WaitForSeconds(0.3f);

        // Slide out to left with smooth acceleration
        LeanTween.moveX(rectTransform, targetPosition.x - 700f, slideOutDuration)
            .setEase(LeanTweenType.easeInQuart);

        // Scale down smoothly
        LeanTween.scale(notification, Vector3.zero, slideOutDuration)
            .setEase(LeanTweenType.easeInQuart)
            .setDelay(0.1f);

        // Fade out
        LeanTween.alphaCanvas(canvasGroup, 0f, slideOutDuration * 0.7f);

        yield return new WaitForSeconds(slideOutDuration);

        // Cleanup
        if (notification != null)
            Destroy(notification);

        data.onComplete?.Invoke();
        isShowingNotification = false;

        // Process next notification
        ProcessQueue();
    }

    private GameObject CreateNotificationObject(NotificationData data)
    {
        if (notificationPrefab == null || notificationContainer == null)
        {
            Debug.LogWarning("NotificationManager: Missing prefab or container!");
            return null;
        }

        // Debug Canvas setup
        DebugCanvasSetup();

        GameObject notification = Instantiate(notificationPrefab, notificationContainer);
        NotificationTheme theme = GetTheme(data.theme);

        // === EDITOR-CONFIGURABLE POSITIONING & SIZE ===
        RectTransform notificationRect = notification.GetComponent<RectTransform>();
        if (notificationRect != null)
        {
            // Use editor settings for anchoring
            Vector2 anchor = new Vector2(anchorX, anchorY);
            notificationRect.anchorMin = anchor;
            notificationRect.anchorMax = anchor;
            notificationRect.pivot = new Vector2(0.5f, 0.5f); // Always center pivot
            
            // Use editor settings for size and position
            notificationRect.sizeDelta = notificationSize;
            notificationRect.anchoredPosition = positionOffset;
            notificationRect.localScale = Vector3.one;
            
            Debug.Log($"Notification anchored at: {anchor}, Size: {notificationSize}, Offset: {positionOffset}");
        }

        // Setup text components
        TextMeshProUGUI titleText = notification.transform.Find("titleText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageText = notification.transform.Find("messageText")?.GetComponent<TextMeshProUGUI>();
        Image backgroundImage = notification.GetComponent<Image>();
        Image iconImage = notification.transform.Find("Icon")?.GetComponent<Image>();
        Image borderImage = notification.transform.Find("Border")?.GetComponent<Image>();

        if (titleText != null)
        {
            titleText.text = data.title;
            titleText.color = theme?.textColor ?? Color.white;
        }

        if (messageText != null)
        {
            messageText.text = data.message;
            messageText.color = theme?.textColor ?? Color.white;
            messageText.gameObject.SetActive(!string.IsNullOrEmpty(data.message));
        }

        if (backgroundImage != null && theme != null)
            backgroundImage.color = theme.backgroundColor;

        if (iconImage != null && theme != null)
        {
            if (theme.iconSprite != null)
            {
                iconImage.sprite = theme.iconSprite;
                iconImage.color = theme.iconColor;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (borderImage != null && theme != null)
        {
            borderImage.color = theme.borderColor;
            
            // === ACTUALLY GOOD GLOW EFFECTS ===
            if (enableGlowEffects)
            {
                StartCoroutine(ProperBorderGlow(borderImage, theme.borderColor));
                CreateRealGlowEffect(notification, theme);
            }
        }

        // Add canvas group for fading
        if (notification.GetComponent<CanvasGroup>() == null)
            notification.AddComponent<CanvasGroup>();

        return notification;
    }

    // === ACTUALLY GOOD GLOW EFFECTS ===
    
    private IEnumerator ProperBorderGlow(Image borderImage, Color glowColor)
    {
        if (borderImage == null) yield break;
        
        Color originalColor = glowColor;
        Color brightColor = new Color(glowColor.r + 0.5f, glowColor.g + 0.5f, glowColor.b + 0.5f, 1f);
        
        while (borderImage != null && borderImage.gameObject != null)
        {
            // Simple color pulse - no fancy stuff, just brightness change
            LeanTween.value(borderImage.gameObject, originalColor, brightColor, glowPulseSpeed)
                .setEase(LeanTweenType.easeInOutSine)
                .setOnUpdate((Color color) => {
                    if (borderImage != null) borderImage.color = color;
                });
                
            yield return new WaitForSeconds(glowPulseSpeed);
            
            if (borderImage != null)
            {
                LeanTween.value(borderImage.gameObject, brightColor, originalColor, glowPulseSpeed)
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnUpdate((Color color) => {
                        if (borderImage != null) borderImage.color = color;
                    });
            }
                
            yield return new WaitForSeconds(glowPulseSpeed);
        }
    }
    
    private void CreateRealGlowEffect(GameObject notification, NotificationTheme theme)
    {
        // Just create a simple scale pulse effect instead of weird boxes
        StartCoroutine(PulseScale(notification, theme));
        
        // Create some floating particles instead of that weird line
        if (enableGlowEffects)
        {
            StartCoroutine(CreateFloatingParticles(notification, theme));
        }
    }
    
    private IEnumerator PulseScale(GameObject notification, NotificationTheme theme)
    {
        RectTransform rect = notification.GetComponent<RectTransform>();
        if (rect == null) yield break;
        
        Vector3 originalScale = Vector3.one;
        Vector3 pulseScale = Vector3.one * 1.02f; // Very subtle scale pulse
        
        while (notification != null)
        {
            // Subtle scale up
            LeanTween.scale(notification, pulseScale, glowPulseSpeed * 2f)
                .setEase(LeanTweenType.easeInOutSine);
                
            yield return new WaitForSeconds(glowPulseSpeed * 2f);
            
            // Scale back
            if (notification != null)
            {
                LeanTween.scale(notification, originalScale, glowPulseSpeed * 2f)
                    .setEase(LeanTweenType.easeInOutSine);
            }
                
            yield return new WaitForSeconds(glowPulseSpeed * 2f);
        }
    }
    
    private IEnumerator CreateFloatingParticles(GameObject notification, NotificationTheme theme)
    {
        float duration = displayDuration;
        float elapsed = 0f;
        
        while (elapsed < duration && notification != null)
        {
            // Create a floating particle every so often
            if (Random.value < 0.3f) // 30% chance
            {
                CreateFloatingParticle(notification.transform, theme.borderColor);
            }
            
            elapsed += 0.5f; // Check every half second
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void CreateFloatingParticle(Transform parent, Color particleColor)
    {
        GameObject particle = new GameObject("FloatingParticle");
        particle.transform.SetParent(parent, false);
        
        Image particleImage = particle.AddComponent<Image>();
        
        // Create a simple circle texture
        Texture2D circleTexture = CreateCircleTexture(16);
        particleImage.sprite = Sprite.Create(circleTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        
        RectTransform particleRect = particle.GetComponent<RectTransform>();
        particleRect.sizeDelta = new Vector2(6, 6); // Small particles
        
        // Random position around the notification
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(50f, 100f);
        Vector2 startPos = new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
        particleRect.anchoredPosition = startPos;
        
        // Glowing particle color
        Color glowColor = particleColor;
        glowColor.a = 0.8f;
        particleImage.color = glowColor;
        
        // Animate particle floating upward and fading
        Vector2 endPos = startPos + Vector2.up * 30f;
        
        LeanTween.move(particleRect, endPos, 2f)
            .setEase(LeanTweenType.easeOutQuad);
            
        LeanTween.alpha(particleRect, 0f, 2f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() => {
                if (particle != null) Destroy(particle);
            });
            
        // Add a little random drift
        LeanTween.moveX(particleRect, endPos.x + Random.Range(-20f, 20f), 2f)
            .setEase(LeanTweenType.easeInOutSine);
    }
    
    private Texture2D CreateCircleTexture(int size)
    {
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f;
        
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance / radius));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        
        texture.Apply();
        return texture;
    }
    
    private IEnumerator FlashEffect(GameObject notification, NotificationTheme theme)
    {
        if (notification == null) yield break;
        
        // Create a flash overlay
        GameObject flash = new GameObject("Flash");
        flash.transform.SetParent(notification.transform);
        flash.transform.SetAsLastSibling(); // On top of everything
        
        RectTransform flashRect = flash.AddComponent<RectTransform>();
        Image flashImage = flash.AddComponent<Image>();
        
        // Setup flash overlay
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.localScale = Vector3.one;
        
        // Bright white flash
        Color flashColor = new Color(1f, 1f, 1f, 0.8f);
        if (theme != null)
        {
            // Tint the flash with the theme color
            flashColor = new Color(
                theme.borderColor.r + 0.5f, 
                theme.borderColor.g + 0.5f, 
                theme.borderColor.b + 0.5f, 
                0.8f
            );
        }
        
        flashImage.color = flashColor;
        
        // Quick flash and fade out
        yield return new WaitForSeconds(0.1f);
        
        LeanTween.alpha(flashRect, 0f, 0.3f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() => {
                if (flash != null) Destroy(flash);
            });
    }

    private IEnumerator SparkleEffectNoLines(GameObject notification, float duration)
    {
        float elapsed = 0f;
        Image backgroundImage = notification.GetComponent<Image>();
        Color originalColor = backgroundImage.color;
        
        while (elapsed < duration && notification != null)
        {
            // Subtle glow pulse on the background - this creates the nice glow effect
            float glowAmount = Mathf.Sin(elapsed * 3f) * sparkleIntensity * 0.3f;
            Color glowColor = originalColor + Color.white * glowAmount;
            glowColor.a = originalColor.a;
            
            if (backgroundImage != null)
                backgroundImage.color = glowColor;

            // REMOVED: CreateSparkleParticle call that was creating lines
            // No particles = no weird lines!

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Restore original color
        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    private IEnumerator SparkleEffect(GameObject notification, float duration)
    {
        float elapsed = 0f;
        Image backgroundImage = notification.GetComponent<Image>();
        Color originalColor = backgroundImage.color;
        
        while (elapsed < duration && notification != null)
        {
            // Subtle glow pulse
            float glowAmount = Mathf.Sin(elapsed * 3f) * sparkleIntensity * 0.3f;
            Color glowColor = originalColor + Color.white * glowAmount;
            glowColor.a = originalColor.a;
            
            if (backgroundImage != null)
                backgroundImage.color = glowColor;

            // Random sparkle positions (you could add particle effects here)
            if (Random.value < 0.1f) // 10% chance per frame
            {
                CreateSparkleParticle(notification.transform);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    private void CreateSparkleParticle(Transform parent)
    {
        // Create a simple sparkle effect
        GameObject sparkle = new GameObject("Sparkle");
        sparkle.transform.SetParent(parent, false);
        
        Image sparkleImage = sparkle.AddComponent<Image>();
        sparkleImage.color = new Color(1f, 1f, 0.8f, 0.8f);
        
        RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
        sparkleRect.sizeDelta = new Vector2(8, 8);
        sparkleRect.anchoredPosition = new Vector2(
            Random.Range(-150f, 150f),
            Random.Range(-30f, 30f)
        );

        // Animate sparkle
        LeanTween.scale(sparkle, Vector3.one * 2f, 0.5f)
            .setEase(LeanTweenType.easeOutQuad);
        
        LeanTween.alpha(sparkleRect, 0f, 0.5f)
            .setOnComplete(() => {
                if (sparkle != null) Destroy(sparkle);
            });

        LeanTween.rotateAroundLocal(sparkle, Vector3.forward, 180f, 0.5f);
    }

    private NotificationTheme GetTheme(string themeName)
    {
        foreach (NotificationTheme theme in themes)
        {
            if (theme.themeName.Equals(themeName, System.StringComparison.OrdinalIgnoreCase))
                return theme;
        }
        return themes.Length > 0 ? themes[0] : null;
    }
    
    private void DebugCanvasSetup()
    {
        if (notificationCanvas != null)
        {
            CanvasScaler scaler = notificationCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                Debug.Log($"Canvas Scaler - UI Scale Mode: {scaler.uiScaleMode}, Reference Resolution: {scaler.referenceResolution}");
            }
            
            RectTransform canvasRect = notificationCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                Debug.Log($"Canvas RectTransform - Size: {canvasRect.sizeDelta}, Scale: {canvasRect.localScale}");
            }
        }
        
        if (notificationContainer != null)
        {
            RectTransform containerRect = notificationContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                Debug.Log($"Container RectTransform - Size: {containerRect.sizeDelta}, Position: {containerRect.anchoredPosition}, Anchors: {containerRect.anchorMin} to {containerRect.anchorMax}");
            }
        }
    }
}
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
        // Optional per-theme UI click sound (played for Continue button if set)
        public AudioClip clickSound;
        public float soundVolume = 1f;
    }

    [Header("Notification Prefab")]
    public GameObject notificationPrefab;
    public GameObject blockingNotificationPrefab; // Separate prefab for blocking notifications
    public Transform notificationContainer;
    public Canvas notificationCanvas;

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Optional click sound to play when notification Continue button is pressed.")]
    public AudioClip buttonClickSound;
    [Tooltip("If enabled, generic blocking notifications will use the current season's theme (Spring/Summer/Fall/Winter) instead of the 'Blocking' theme.")]
    public bool useSeasonThemeForBlocking = true;
    [Tooltip("Optional dedicated UI audio source. If left empty, a runtime UI audio source will be created that ignores listener pause so notification sounds continue when the game is paused.")]
    public AudioSource uiAudioSource;
    
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
        },
        // Badge - Dramatic red/orange for major achievements that "slam" onto screen
        new NotificationTheme { 
            themeName = "Badge", 
            backgroundColor = new Color(0.4f, 0.1f, 0.05f, 0.98f),
            borderColor = new Color(1f, 0.3f, 0.1f, 1f),
            textColor = new Color(1f, 1f, 1f, 1f),
            iconColor = new Color(1f, 0.6f, 0.2f, 1f)
        }
        ,
        // Animal - Soft natural green for animal unlocks
        new NotificationTheme {
            themeName = "Animal",
            backgroundColor = new Color(0.12f, 0.3f, 0.18f, 0.95f),
            borderColor = new Color(0.5f, 0.8f, 0.5f, 1f),
            textColor = new Color(0.95f, 1f, 0.9f, 1f),
            iconColor = new Color(0.7f, 1f, 0.6f, 1f)
        },
        // Raccoon - Gray/teal accent for raccoon-specific messages
        new NotificationTheme {
            themeName = "Raccoon",
            backgroundColor = new Color(0.13f, 0.15f, 0.16f, 0.97f),
            borderColor = new Color(0.4f, 0.8f, 0.9f, 1f),
            textColor = new Color(1f, 1f, 1f, 1f),
            iconColor = new Color(0.6f, 0.85f, 0.9f, 1f)
        },
        // Boar - Brown/earth tones for boar-specific messages
        new NotificationTheme {
            themeName = "Boar",
            backgroundColor = new Color(0.25f, 0.15f, 0.1f, 0.97f),
            borderColor = new Color(0.8f, 0.5f, 0.3f, 1f),
            textColor = new Color(1f, 0.95f, 0.85f, 1f),
            iconColor = new Color(0.9f, 0.6f, 0.4f, 1f)
        },
        // Bear - Dark blue/gray for bear-specific messages (winter/ice theme)
        new NotificationTheme {
            themeName = "Bear",
            backgroundColor = new Color(0.1f, 0.15f, 0.25f, 0.97f),
            borderColor = new Color(0.4f, 0.6f, 0.9f, 1f),
            textColor = new Color(0.9f, 0.95f, 1f, 1f),
            iconColor = new Color(0.6f, 0.8f, 1f, 1f)
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
    
    [Header("Blocking Notification Overrides")]
    [Tooltip("If set, blocking notifications will use this size instead of the default notificationSize. If left as (0,0), the blocking prefab's own size will be preserved.")]
    public Vector2 blockingNotificationSize = Vector2.zero;
    [Tooltip("If true, blocking notifications will ignore the editor-position offset and preserve their prefab anchored position when possible.")]
    public bool blockingPreservePrefabPosition = true;
    [Tooltip("If true, the manager will apply custom anchor/pivot values for blocking notifications.")]
    public bool blockingUseCustomAnchors = false;
    [Tooltip("AnchorMin for blocking notifications when blockingUseCustomAnchors is true.")]
    public Vector2 blockingAnchorMin = new Vector2(0.5f, 0.5f);
    [Tooltip("AnchorMax for blocking notifications when blockingUseCustomAnchors is true.")]
    public Vector2 blockingAnchorMax = new Vector2(0.5f, 0.5f);
    [Tooltip("Pivot for blocking notifications when blockingUseCustomAnchors is true.")]
    public Vector2 blockingPivot = new Vector2(0.5f, 0.5f);

    [Header("Blocking Visuals")]
    [Tooltip("If true, blocking notification background Image will be forced fully opaque (alpha = 1)")]
    public bool forceBlockingOpaque = true;
    [Range(0f, 1f)]
    [Tooltip("Backdrop alpha when a blocking notification is shown (0 = transparent, 1 = solid black)")]
    public float blockingBackdropAlpha = 0.85f;
    [Tooltip("Optional UI material that performs a blur (e.g. a GrabPass-based blur shader). If left empty no blur is used.")]
    public Material backdropBlurMaterial;
    [Tooltip("Blur size/intensity passed to common shader property names (_Size, _Radius, _BlurSize)")]
    public float backdropBlurSize = 2f;
    [Tooltip("Fade duration for the blur when a blocking notification appears")]
    public float backdropBlurFade = 0.25f;
    
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
                instance = FindFirstObjectByType<NotificationManager>();
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
        public bool pauseOnShow;
        public System.Action onComplete;
        public Sprite customIcon;
        // Optional bonus display values that blocking seasonal notifications can show
        public string bonusText;
        public Sprite bonusIcon;
        // Optional unlocks text (designer-populated per-season)
        public string unlocksText;
    }

    [System.Serializable]
    public class SeasonInfo
    {
        public int season = 1;
        public string title = "";
        [TextArea]
        public string message = "";
        [TextArea]
        public string bonusText = "";
        [TextArea]
        public string unlocksText = "";
        public Sprite image;
        public Sprite bonusIcon;
        public string theme = "Blocking";
    }

    // Helper component to track a backdrop GameObject created for a notification
    private class BackdropHolder : MonoBehaviour
    {
        public GameObject backdrop;
    }

    [Header("Seasonal Blocking Notifications")]
    public SeasonInfo[] seasonalInfos = new SeasonInfo[0];

    /// <summary>
    /// Show a blocking season notification using the configured SeasonInfo for the provided season id.
    /// </summary>
    public static void ShowSeasonalBlocking(int season, string unlocksText = null)
    {
        if (Instance == null) return;

        SeasonInfo info = null;
        foreach (var s in Instance.seasonalInfos)
        {
            if (s != null && s.season == season)
            {
                info = s;
                break;
            }
        }

        if (info == null)
        {
            // Fallback: generic title
            ShowBlockingNotification($"Season {season}", "A new season has begun.");
            return;
        }

        // Create NotificationData with custom icon and forced blocking theme
        NotificationData data = new NotificationData
        {
            title = info.title ?? $"Season {season}",
            message = info.message ?? "",
            theme = info.theme ?? "Blocking",
            duration = 0f,
            pauseOnShow = true,
            onComplete = null,
            customIcon = info.image,
            bonusText = info.bonusText,
            bonusIcon = info.bonusIcon
        };

        // Carry over any designer-provided unlocks text for this season, or use the passed unlocksText
        data.unlocksText = unlocksText ?? info.unlocksText;

        Instance.notificationQueue.Enqueue(data);
        Instance.ProcessQueue();
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }

        // If seasonalInfos hasn't been filled in the inspector, provide sensible filler defaults
        if (seasonalInfos == null || seasonalInfos.Length == 0)
        {
            seasonalInfos = new SeasonInfo[4];
            seasonalInfos[0] = new SeasonInfo { season = 1, title = "Spring Arrives", message = "The fields wake up and new seeds sprout. Time to plant and grow!", bonusText = "Spring Bonus: +10% Crop Growth", theme = "Info" };
            seasonalInfos[1] = new SeasonInfo { season = 2, title = "Summer Heat", message = "Long sunny days bring plenty of growth and hungry customers.", bonusText = "Summer Bonus: +5% Sales", theme = "Info" };
            seasonalInfos[2] = new SeasonInfo { season = 3, title = "Autumn Harvest", message = "Leaves fall and the harvest is rich. Prepare for cool nights.", bonusText = "Autumn Bonus: +2 Rare Drops", theme = "Info" };
            seasonalInfos[3] = new SeasonInfo { season = 4, title = "Winter Chill", message = "Snow blankets the land — take care of your animals and torches.", bonusText = "Winter Bonus: +15% Animal Warmth", theme = "Info" };
        }

        // Ensure there's a UI audio source that ignores listener pause so modal sounds still play when the game is paused
        if (uiAudioSource == null)
        {
            // Create a child audio source dedicated to UI/notification sounds
            GameObject go = new GameObject("Notification UI AudioSource");
            go.transform.SetParent(this.transform, false);
            uiAudioSource = go.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.spatialBlend = 0f; // 2D UI sound
        }
    }

    public static void ShowNotification(string title, string message, string theme = "Info", float duration = 2f, System.Action onComplete = null)
    {
        if (Instance == null) return;

        NotificationData data = new NotificationData
        {
            title = title,
            message = message,
            theme = theme,
            duration = duration,
            pauseOnShow = false,
            onComplete = onComplete
        };

        Instance.notificationQueue.Enqueue(data);
        Instance.ProcessQueue();
    }

    public static void ShowBlockingNotification(string title, string message, string theme = "Blocking", System.Action onComplete = null)
    {
        if (Instance == null) return;

        // Determine final theme: prefer caller-specified theme, but if the manager
        // is configured to use season themes for blocking use the current season's theme.
        string finalTheme = theme ?? "Blocking";
        if (Instance.useSeasonThemeForBlocking && (string.IsNullOrEmpty(theme) || theme.Equals("Blocking", System.StringComparison.OrdinalIgnoreCase)))
        {
            var night = FindFirstObjectByType<NightManager>();
            if (night != null)
            {
                int season = night.GetCurrentSeason();
                foreach (var s in Instance.seasonalInfos)
                {
                    if (s != null && s.season == season && !string.IsNullOrEmpty(s.theme))
                    {
                        finalTheme = s.theme;
                        break;
                    }
                }
            }
        }

        NotificationData data = new NotificationData
        {
            title = title,
            message = message,
            theme = finalTheme,
            duration = 0f, // Not used for blocking
            pauseOnShow = true,
            onComplete = onComplete
        };

        Instance.notificationQueue.Enqueue(data);
        Instance.ProcessQueue();
    }

    public static void ShowSuccess(string title, string message = "", float duration = 1.5f)
    {
        ShowNotification(title, message, "Success", duration);
    }

    public static void ShowWarning(string title, string message = "", float duration = 1.5f)
    {
        ShowNotification(title, message, "Warning", duration);
    }

    public static void ShowError(string title, string message = "", float duration = 1.5f)
    {
        ShowNotification(title, message, "Error", duration);
    }

    public static void ShowAchievement(string title, string message = "", float duration = 1.5f)
    {
        ShowNotification(title, message, "Achievement", duration);
    }

    public static void ShowBadge(string title, string message = "", float duration = 1.5f)
    {
        ShowNotification(title, message, "Badge", duration);
    }

    public static void ShowAnimalUnlock(string animalName, string message = "", float duration = 1.5f)
    {
        string title = $"New Animal: {animalName}";
        ShowNotification(title, message, "Animal", duration);
    }

    public static void ShowRaccoon(string message = "", float duration = 2f)
    {
        ShowNotification("Raccoon Unlocked!", message, "Raccoon", duration);
    }

    public static void ShowBoar(string message = "", float duration = 2f)
    {
        ShowNotification("Boar Unlocked!", message, "Boar", duration);
    }

    public static void ShowBear(string message = "", float duration = 2f)
    {
        ShowNotification("Bear Unlocked!", message, "Bear", duration);
    }

    public static void ShowBlockingSuccess(string title, string message = "")
    {
        // Use the modal 'Blocking' theme by default for blocking variants
        ShowBlockingNotification(title, message, "Blocking");
    }

    public static void ShowBlockingWarning(string title, string message = "")
    {
        ShowBlockingNotification(title, message, "Blocking");
    }

    public static void ShowBlockingError(string title, string message = "")
    {
        ShowBlockingNotification(title, message, "Blocking");
    }

    public static void ShowBlockingAchievement(string title, string message = "")
    {
        ShowBlockingNotification(title, message, "Blocking");
    }

    public static void ShowBlockingBadge(string title, string message = "")
    {
        ShowBlockingNotification(title, message, "Blocking");
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
        Button continueButton = notification.GetComponentInChildren<Button>();

        bool buttonClicked = false;
        
        // Setup initial state for animation - use editor settings
        Vector3 targetPosition = positionOffset; // Use editor offset
        Vector3 startPosition = targetPosition + Vector3.right * (notificationSize.x + 100f); // Start off-screen based on size
        
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

    // Play sound effect
    NotificationTheme theme = GetTheme(data.theme);
        AudioSource playSource = uiAudioSource != null ? uiAudioSource : audioSource;
        if (theme?.soundEffect != null && playSource != null)
        {
            playSource.PlayOneShot(theme.soundEffect, theme.soundVolume);
        }

        // Attach click sound + completion handler to the continue button (if present).
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() =>
            {
                AudioSource clickSource = uiAudioSource != null ? uiAudioSource : audioSource;
                if (clickSource != null)
                {
                    if (theme?.clickSound != null)
                    {
                        clickSource.PlayOneShot(theme.clickSound, theme.soundVolume);
                    }
                    else if (buttonClickSound != null)
                    {
                        clickSource.PlayOneShot(buttonClickSound, 1f);
                    }
                    else if (theme?.soundEffect != null)
                    {
                        clickSource.PlayOneShot(theme.soundEffect, theme.soundVolume);
                    }
                }

                buttonClicked = true;
            });
        }

        // === DRAMATIC ENTRANCE ANIMATION ===
        bool isBadge = data.theme == "Badge";
        
        // Start with a flash effect (more intense for badges)
        StartCoroutine(FlashEffect(notification, theme, isBadge));
        
        // Slide in from right - FASTER and more forceful for badges
        float slideDuration = isBadge ? slideInDuration * 0.6f : slideInDuration;
        LeanTween.moveX(rectTransform, targetPosition.x, slideDuration)
            .setEase(isBadge ? LeanTweenType.easeOutBack : LeanTweenType.easeOutQuart)
            .setDelay(isBadge ? 0f : 0.1f);

        // Scale up - BIGGER bounce for badges
        float scaleDuration = isBadge ? slideDuration * 0.7f : slideDuration * 0.8f;
        float maxScale = isBadge ? 1.2f : 1.0f;
        LeanTween.scale(notification, Vector3.one * maxScale, scaleDuration)
            .setEase(isBadge ? LeanTweenType.easeOutBack : LeanTweenType.easeOutQuart)
            .setDelay(isBadge ? 0.05f : 0.2f);

        // Fade in - faster for badges
        float fadeDelay = isBadge ? 0.05f : 0.15f;
        LeanTween.alphaCanvas(canvasGroup, 1f, slideDuration * 0.5f)
            .setDelay(fadeDelay);

        // Settle effect - more dramatic bounce for badges
        yield return new WaitForSeconds(slideDuration * 0.8f);
        
        if (isBadge)
        {
            // Dramatic badge settle - bigger bounce back to normal
            LeanTween.scale(notification, Vector3.one * 1.1f, 0.15f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() =>
                {
                    LeanTween.scale(notification, Vector3.one * 0.98f, 0.1f)
                        .setEase(LeanTweenType.easeInOutQuad)
                        .setOnComplete(() =>
                        {
                            LeanTween.scale(notification, Vector3.one, 0.2f)
                                .setEase(LeanTweenType.easeOutBounce);
                        });
                });
        }
        else
        {
            // Gentle settle effect for other notifications
            LeanTween.scale(notification, Vector3.one * 1.03f, 0.2f)
                .setEase(LeanTweenType.easeInOutQuad)
                .setOnComplete(() =>
                {
                    LeanTween.scale(notification, Vector3.one, 0.3f)
                        .setEase(LeanTweenType.easeInOutQuad);
                });
        }

        // Sparkle effects during display - more intense for badges
        StartCoroutine(SparkleEffectNoLines(notification, data.duration, isBadge));

        // === DISPLAY DURATION ===
        if (data.pauseOnShow)
        {
            // Pause the game
            PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
            if (pauseManager != null)
            {
                pauseManager.pauseGame();
            }

            // Wait for button click
            yield return new WaitUntil(() => buttonClicked);

            // Unpause the game
            if (pauseManager != null)
            {
                pauseManager.playGame();
            }
        }
        else
        {
            yield return new WaitForSeconds(data.duration);
        }

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
        {
            // If we attached a backdrop, destroy it first
            BackdropHolder holder = notification.GetComponent<BackdropHolder>();
            if (holder != null && holder.backdrop != null)
            {
                Destroy(holder.backdrop);
            }

            Destroy(notification);
        }

        data.onComplete?.Invoke();
        isShowingNotification = false;

        // Process next notification
        ProcessQueue();
    }

    private GameObject CreateNotificationObject(NotificationData data)
    {
        GameObject prefabToUse = data.pauseOnShow && blockingNotificationPrefab != null ? blockingNotificationPrefab : notificationPrefab;
        
        if (prefabToUse == null || notificationContainer == null)
        {
            Debug.LogWarning("NotificationManager: Missing prefab or container!");
            return null;
        }

        // Debug Canvas setup
        DebugCanvasSetup();

        GameObject notification = Instantiate(prefabToUse, notificationContainer);
        NotificationTheme theme = GetTheme(data.theme);

        // Safety: Ensure the notification (or its child canvases) has a GraphicRaycaster
        // and uses a sortable canvas so it receives input in front of other UI.
        Canvas[] childCanvases = notification.GetComponentsInChildren<Canvas>(true);
        if (childCanvases == null || childCanvases.Length == 0)
        {
            // If the prefab doesn't include a canvas, add a lightweight one on the root.
            Canvas added = notification.AddComponent<Canvas>();
            added.renderMode = RenderMode.ScreenSpaceOverlay;
            added.overrideSorting = true;
            added.sortingOrder = 1000; // ensure it's on top
            if (notification.GetComponent<GraphicRaycaster>() == null)
                notification.AddComponent<GraphicRaycaster>();
        }
        else
        {
            // Make sure all canvases are configured to receive raycasts and are sorted above other UI
            foreach (Canvas c in childCanvases)
            {
                if (c == null) continue;
                c.overrideSorting = true;
                c.sortingOrder = Mathf.Max(c.sortingOrder, 1000);
                if (c.renderMode == RenderMode.ScreenSpaceCamera && c.worldCamera == null)
                    c.worldCamera = Camera.main;
                if (c.GetComponent<GraphicRaycaster>() == null)
                    c.gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        // Ensure any child Button is configured to be interactable and its Image accepts raycasts
        Button childButton = notification.GetComponentInChildren<Button>(true);
        if (childButton != null)
        {
            childButton.interactable = true;
            // Ensure target graphic is raycastable
            var targetImg = childButton.targetGraphic as Image;
            if (targetImg == null)
            {
                targetImg = childButton.GetComponent<Image>() ?? childButton.GetComponentInChildren<Image>();
            }
            if (targetImg != null)
                targetImg.raycastTarget = true;
        }

        // If this is a blocking notification, create a semi-opaque fullscreen backdrop
        // so clicks to underlying UI are blocked and the notification feels modal.
        // The backdrop is added as the first sibling so notification content renders above it.
        if (data != null && data.pauseOnShow)
        {
            // Parent the backdrop to the notification container (or canvas) so it can cover the full area
            Transform parentForBackdrop = notificationContainer != null ? notificationContainer : notification.transform.parent;

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parentForBackdrop, false);
            RectTransform bRect = backdrop.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            bRect.localScale = Vector3.one;

            // Add CanvasRenderer + Image for raycast-blocking and visual dimming
            backdrop.AddComponent<CanvasRenderer>();

            // Optional blur layer (behind the dark tint) if material provided
            GameObject blurGO = null;
            RawImage blurImage = null;
            if (backdropBlurMaterial != null)
            {
                blurGO = new GameObject("BackdropBlur");
                blurGO.transform.SetParent(parentForBackdrop, false);
                RectTransform blurRect = blurGO.AddComponent<RectTransform>();
                blurRect.anchorMin = Vector2.zero;
                blurRect.anchorMax = Vector2.one;
                blurRect.offsetMin = Vector2.zero;
                blurRect.offsetMax = Vector2.zero;
                blurRect.localScale = Vector3.one;

                blurGO.AddComponent<CanvasRenderer>();
                blurImage = blurGO.AddComponent<RawImage>();
                blurImage.material = backdropBlurMaterial;
                blurImage.raycastTarget = false;

                // Try common property names for blur size
                if (backdropBlurSize > 0f)
                {
                    if (blurImage.material.HasProperty("_Size")) blurImage.material.SetFloat("_Size", backdropBlurSize);
                    if (blurImage.material.HasProperty("_Radius")) blurImage.material.SetFloat("_Radius", backdropBlurSize);
                    if (blurImage.material.HasProperty("_BlurSize")) blurImage.material.SetFloat("_BlurSize", backdropBlurSize);
                }
            }

            Image bgImage = backdrop.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, blockingBackdropAlpha);
            bgImage.raycastTarget = true; // block clicks to background

            // Place backdrop behind the notification in the same parent if possible
            if (backdrop.transform.parent == notification.transform.parent)
            {
                int notifIndex = notification.transform.GetSiblingIndex();
                // Put backdrop at the same index so it's behind; then push notification to top
                // If we have a blur object, insert it before the backdrop so blur sits below tint
                if (blurGO != null)
                {
                    blurGO.transform.SetSiblingIndex(Mathf.Max(0, notifIndex));
                    backdrop.transform.SetSiblingIndex(Mathf.Max(0, notifIndex + 1));
                }
                else
                {
                    backdrop.transform.SetSiblingIndex(Mathf.Max(0, notifIndex));
                }
                notification.transform.SetAsLastSibling();
            }
            else
            {
                // Otherwise just ensure the backdrop is below other siblings
                if (blurGO != null)
                {
                    blurGO.transform.SetAsFirstSibling();
                    backdrop.transform.SetAsFirstSibling();
                }
                else
                {
                    backdrop.transform.SetAsFirstSibling();
                }
                notification.transform.SetAsLastSibling();
            }

            // Attach a BackdropHolder to the notification so we can remove the backdrop when notification closes
            BackdropHolder holder = notification.GetComponent<BackdropHolder>();
            if (holder == null) holder = notification.AddComponent<BackdropHolder>();
            holder.backdrop = backdrop;
            // Also store blur as a child of the holder for cleanup
            if (blurImage != null)
            {
                // attach blur GameObject as a child of the notification so cleanup is simplified
                blurGO.transform.SetParent(notification.transform, false);
                // store original parent as the holder too (we only store the main backdrop in holder.backdrop)
            }

            // If we created a blur, fade it in
            if (blurImage != null)
            {
                CanvasGroup blurCg = blurGO.GetComponent<CanvasGroup>();
                if (blurCg == null) blurCg = blurGO.AddComponent<CanvasGroup>();
                blurCg.alpha = 0f;
                LeanTween.alphaCanvas(blurCg, 1f, backdropBlurFade);
            }
        }

        // === EDITOR-CONFIGURABLE POSITIONING & SIZE ===
        RectTransform notificationRect = notification.GetComponent<RectTransform>();
        if (notificationRect != null)
        {
            // Use editor settings for anchoring unless the prefab wants its own anchors
            Vector2 anchor = new Vector2(anchorX, anchorY);
            notificationRect.anchorMin = anchor;
            notificationRect.anchorMax = anchor;
            notificationRect.pivot = new Vector2(0.5f, 0.5f); // Default center pivot

            // For blocking notifications, prefer the prefab's size/position unless an override is provided
            if (data != null && data.pauseOnShow && blockingNotificationPrefab != null)
            {
                // Apply custom anchors/pivot if requested
                if (blockingUseCustomAnchors)
                {
                    notificationRect.anchorMin = blockingAnchorMin;
                    notificationRect.anchorMax = blockingAnchorMax;
                    notificationRect.pivot = blockingPivot;
                }
                if (blockingNotificationSize != Vector2.zero)
                {
                    notificationRect.sizeDelta = blockingNotificationSize;
                }
                else
                {
                    // Preserve prefab's own size by not overwriting sizeDelta
                }

                if (!blockingPreservePrefabPosition)
                {
                    notificationRect.anchoredPosition = positionOffset;
                }
            }
            else
            {
                // Use editor settings for size and position
                notificationRect.sizeDelta = notificationSize;
                notificationRect.anchoredPosition = positionOffset;
            }

            notificationRect.localScale = Vector3.one;
        }

        // Setup text components
        TextMeshProUGUI titleText = notification.transform.Find("titleText")?.GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI messageText = notification.transform.Find("messageText")?.GetComponent<TextMeshProUGUI>();
    TextMeshProUGUI bonusText = notification.transform.Find("bonusText")?.GetComponent<TextMeshProUGUI>();
        Image backgroundImage = notification.GetComponent<Image>();
        Image iconImage = notification.transform.Find("Icon")?.GetComponent<Image>();
    Image bonusIconImage = notification.transform.Find("bonusIcon")?.GetComponent<Image>();
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

        if (bonusText != null)
        {
            bonusText.text = data.bonusText ?? "";
            bonusText.color = theme?.textColor ?? Color.white;
            bonusText.gameObject.SetActive(!string.IsNullOrEmpty(data.bonusText));
        }

        // Designer-authored unlocks text (multi-line).
        // Preferred: child TextMeshProUGUI named "unlocksText". But be tolerant of slightly different naming
        // and also support legacy UnityEngine.UI.Text fields.
        TextMeshProUGUI unlocksTMP = notification.transform.Find("unlocksText")?.GetComponent<TextMeshProUGUI>();
        UnityEngine.UI.Text unlocksUIText = null;

        if (unlocksTMP == null)
        {
            // Fallback: search all TMP children for a name that contains "unlock"
            var allTmps = notification.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var t in allTmps)
            {
                if (t == null) continue;
                string n = t.gameObject.name.ToLowerInvariant();
                if (n.Contains("unlock") || n.Contains("unlocks"))
                {
                    unlocksTMP = t;
                    break;
                }
            }
        }

        // If no TMP found, try classic UI.Text components (some prefabs may still use them)
        if (unlocksTMP == null)
        {
            var allTexts = notification.GetComponentsInChildren<UnityEngine.UI.Text>(true);
            foreach (var t in allTexts)
            {
                if (t == null) continue;
                string n = t.gameObject.name.ToLowerInvariant();
                if (n.Contains("unlock") || n.Contains("unlocks"))
                {
                    unlocksUIText = t;
                    break;
                }
            }
        }

        if (unlocksTMP != null)
        {
            unlocksTMP.text = string.IsNullOrEmpty(data.unlocksText) ? "" : data.unlocksText;
            unlocksTMP.color = theme?.textColor ?? Color.white;
            unlocksTMP.gameObject.SetActive(!string.IsNullOrEmpty(data.unlocksText));
        }
        else if (unlocksUIText != null)
        {
            unlocksUIText.text = string.IsNullOrEmpty(data.unlocksText) ? "" : data.unlocksText;
            unlocksUIText.color = theme?.textColor ?? Color.white;
            unlocksUIText.gameObject.SetActive(!string.IsNullOrEmpty(data.unlocksText));
        }
        else
        {
            // Helpful debug if designer forgot to include a field in the prefab
            if (!string.IsNullOrEmpty(data.unlocksText))
            {
                // Log available TMP/Text child names for easier debugging
                var tmps = notification.GetComponentsInChildren<TextMeshProUGUI>(true);
                var texts = notification.GetComponentsInChildren<UnityEngine.UI.Text>(true);
                string found = "";
                foreach (var t in tmps) found += "TMP:" + t.gameObject.name + "; ";
                foreach (var t in texts) found += "Text:" + t.gameObject.name + "; ";
                Debug.LogWarning($"NotificationManager: Blocking notification prefab is missing an unlocks field. Expected child named 'unlocksText' (TextMeshProUGUI) or similar. Unlocks text: {data.unlocksText}. Found children: {found}");
            }
        }

        if (backgroundImage != null && theme != null)
            backgroundImage.color = theme.backgroundColor;

        // If this is a blocking notification and we prefer opaque blocking panels, force alpha to 1
        if (data != null && data.pauseOnShow && forceBlockingOpaque && backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }

        if (iconImage != null && theme != null)
        {
            // Prefer a custom icon set on the NotificationData (used for seasonal images)
            Sprite chosen = null;
            var dataField = data as object;
            // We have access to the local 'data' variable in caller scope — prefer customIcon
            if (data != null && data.customIcon != null)
            {
                chosen = data.customIcon;
            }
            else if (theme.iconSprite != null)
            {
                chosen = theme.iconSprite;
            }

            if (chosen != null)
            {
                iconImage.sprite = chosen;
                iconImage.color = theme?.iconColor ?? Color.white;
                // Ensure icon doesn't block clicks on the continue button
                iconImage.raycastTarget = false;
            }
            else
            {
                iconImage.gameObject.SetActive(false);
            }
        }

        if (bonusIconImage != null)
        {
            Sprite chosenBonus = data != null ? data.bonusIcon : null;
            if (chosenBonus != null)
            {
                bonusIconImage.sprite = chosenBonus;
                bonusIconImage.color = theme?.iconColor ?? Color.white;
                bonusIconImage.gameObject.SetActive(true);
                // Bonus icon should not intercept pointer events
                bonusIconImage.raycastTarget = false;
            }
            else
            {
                bonusIconImage.gameObject.SetActive(false);
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

        // Add canvas group for fading and ensure it allows interaction/raycasting
        CanvasGroup cg = notification.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = notification.AddComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.ignoreParentGroups = false;
        }

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
    
    private IEnumerator FlashEffect(GameObject notification, NotificationTheme theme, bool isBadge = false)
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
        
        // Bright white flash - more intense for badges
        Color flashColor = new Color(1f, 1f, 1f, isBadge ? 1f : 0.8f);
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

    private IEnumerator SparkleEffectNoLines(GameObject notification, float duration, bool isBadge = false)
    {
        float elapsed = 0f;
        Image backgroundImage = notification.GetComponent<Image>();
        Color originalColor = backgroundImage.color;
        
        // More intense sparkle for badges
        float intensityMultiplier = isBadge ? 2f : 1f;
        float frequency = isBadge ? 4f : 3f;
        
        while (elapsed < duration && notification != null)
        {
            // Subtle glow pulse on the background - more intense for badges
            float glowAmount = Mathf.Sin(elapsed * frequency) * sparkleIntensity * 0.3f * intensityMultiplier;
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
        if (string.IsNullOrEmpty(themeName))
            return themes.Length > 0 ? themes[0] : null;

        // Normalize input: case-insensitive and collapse duplicated letters (e.g., "Racoon" vs "Raccoon").
        string Normalize(string s)
        {
            s = s.Trim().ToLowerInvariant();
            // Collapse repeated characters (only collapse runs >1 to a single char)
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            char? last = null;
            foreach (char c in s)
            {
                if (last == null || c != last)
                {
                    sb.Append(c);
                }
                last = c;
            }
            return sb.ToString();
        }

        string target = Normalize(themeName);
        foreach (NotificationTheme theme in themes)
        {
            if (theme == null || string.IsNullOrEmpty(theme.themeName)) continue;
            if (Normalize(theme.themeName).Equals(target))
                return theme;
        }

        // fallback to exact-case-insensitive match, then default theme
        foreach (NotificationTheme theme in themes)
        {
            if (theme == null || theme.themeName == null) continue;
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
                
            }
            
            RectTransform canvasRect = notificationCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                
            }
        }
        
        if (notificationContainer != null)
        {
            RectTransform containerRect = notificationContainer.GetComponent<RectTransform>();
            if (containerRect != null)
            {
                
            }
        }
    }
}
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
        public AudioClip clickSound;
        public float soundVolume = 1f;
    }

    [Header("Notification Prefab")]
    public GameObject notificationPrefab;
    public GameObject blockingNotificationPrefab;
    public Transform notificationContainer;
    public Canvas notificationCanvas;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip buttonClickSound;
    public bool useSeasonThemeForBlocking = true;
    public AudioSource uiAudioSource;
    
    [Header("Themes")]
    public NotificationTheme[] themes = new NotificationTheme[]
    {
        new NotificationTheme { 
            themeName = "Success", 
            backgroundColor = new Color(0.1f, 0.4f, 0.1f, 0.95f),
            borderColor = new Color(0.3f, 0.8f, 0.3f, 1f),
            textColor = new Color(0.9f, 1f, 0.9f, 1f),
            iconColor = new Color(0.4f, 1f, 0.4f, 1f)
        },
        new NotificationTheme { 
            themeName = "Warning", 
            backgroundColor = new Color(0.4f, 0.2f, 0.0f, 0.95f),
            borderColor = new Color(1f, 0.6f, 0.1f, 1f),
            textColor = new Color(1f, 0.95f, 0.8f, 1f),
            iconColor = new Color(1f, 0.7f, 0.2f, 1f)
        },
        new NotificationTheme { 
            themeName = "Error", 
            backgroundColor = new Color(0.4f, 0.1f, 0.1f, 0.95f),
            borderColor = new Color(1f, 0.3f, 0.3f, 1f),
            textColor = new Color(1f, 0.9f, 0.9f, 1f),
            iconColor = new Color(1f, 0.4f, 0.4f, 1f)
        },
        new NotificationTheme { 
            themeName = "Info", 
            backgroundColor = new Color(0.1f, 0.2f, 0.4f, 0.95f),
            borderColor = new Color(0.3f, 0.6f, 1f, 1f),
            textColor = new Color(0.9f, 0.95f, 1f, 1f),
            iconColor = new Color(0.4f, 0.7f, 1f, 1f)
        },
        new NotificationTheme { 
            themeName = "Achievement", 
            backgroundColor = new Color(0.3f, 0.25f, 0.1f, 0.95f),
            borderColor = new Color(1f, 0.8f, 0.2f, 1f),
            textColor = new Color(1f, 0.95f, 0.8f, 1f),
            iconColor = new Color(1f, 0.9f, 0.3f, 1f)
        },
        new NotificationTheme { 
            themeName = "New", 
            backgroundColor = new Color(0.3f, 0.1f, 0.3f, 0.95f),
            borderColor = new Color(1f, 0.4f, 1f, 1f),
            textColor = new Color(1f, 0.9f, 1f, 1f),
            iconColor = new Color(1f, 0.5f, 1f, 1f)
        },
        new NotificationTheme { 
            themeName = "Badge", 
            backgroundColor = new Color(0.4f, 0.1f, 0.05f, 0.98f),
            borderColor = new Color(1f, 0.3f, 0.1f, 1f),
            textColor = new Color(1f, 1f, 1f, 1f),
            iconColor = new Color(1f, 0.6f, 0.2f, 1f)
        },
        new NotificationTheme {
            themeName = "Animal",
            backgroundColor = new Color(0.12f, 0.3f, 0.18f, 0.95f),
            borderColor = new Color(0.5f, 0.8f, 0.5f, 1f),
            textColor = new Color(0.95f, 1f, 0.9f, 1f),
            iconColor = new Color(0.7f, 1f, 0.6f, 1f)
        },
        new NotificationTheme {
            themeName = "Raccoon",
            backgroundColor = new Color(0.13f, 0.15f, 0.16f, 0.97f),
            borderColor = new Color(0.4f, 0.8f, 0.9f, 1f),
            textColor = new Color(1f, 1f, 1f, 1f),
            iconColor = new Color(0.6f, 0.85f, 0.9f, 1f)
        },
        new NotificationTheme {
            themeName = "Boar",
            backgroundColor = new Color(0.25f, 0.15f, 0.1f, 0.97f),
            borderColor = new Color(0.8f, 0.5f, 0.3f, 1f),
            textColor = new Color(1f, 0.95f, 0.85f, 1f),
            iconColor = new Color(0.9f, 0.6f, 0.4f, 1f)
        },
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
    [Range(0f, 1f)] public float anchorX = 0.5f;
    [Range(0f, 1f)] public float anchorY = 0.5f;
    public Vector2 notificationSize = new Vector2(500f, 150f);
    public Vector2 positionOffset = Vector2.zero;
    
    [Header("Blocking Notification Overrides")]
    public Vector2 blockingNotificationSize = Vector2.zero;
    public bool blockingPreservePrefabPosition = true;
    public bool blockingUseCustomAnchors = false;
    public Vector2 blockingAnchorMin = new Vector2(0.5f, 0.5f);
    public Vector2 blockingAnchorMax = new Vector2(0.5f, 0.5f);
    public Vector2 blockingPivot = new Vector2(0.5f, 0.5f);

    [Header("Blocking Visuals")]
    public bool forceBlockingOpaque = true;
    [Range(0f, 1f)]
    public float blockingBackdropAlpha = 0.85f;
    public Material backdropBlurMaterial;
    public float backdropBlurSize = 10f;
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
        public string bonusText;
        public Sprite bonusIcon;
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

    private class BackdropHolder : MonoBehaviour
    {
        public GameObject backdrop;
    }

    [Header("Seasonal Blocking Notifications")]
    public SeasonInfo[] seasonalInfos = new SeasonInfo[0];

    public static void ShowSeasonalBlocking(int season, string unlocksText = null)
    {
        if (Instance == null) return;
        EnsureActive();

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
            ShowBlockingNotification($"Season {season}", "A new season has begun.");
            return;
        }

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

        if (seasonalInfos == null || seasonalInfos.Length == 0)
        {
            seasonalInfos = new SeasonInfo[4];
            seasonalInfos[0] = new SeasonInfo { season = 1, title = "Spring Arrives", message = "The fields wake up and new seeds sprout. Time to plant and grow!", bonusText = "Spring Bonus: +10% Crop Growth", theme = "Info" };
            seasonalInfos[1] = new SeasonInfo { season = 2, title = "Summer Heat", message = "Long sunny days bring plenty of growth and hungry customers.", bonusText = "Summer Bonus: +5% Sales", theme = "Info" };
            seasonalInfos[2] = new SeasonInfo { season = 3, title = "Autumn Harvest", message = "Leaves fall and the harvest is rich. Prepare for cool nights.", bonusText = "Autumn Bonus: +2 Rare Drops", theme = "Info" };
            seasonalInfos[3] = new SeasonInfo { season = 4, title = "Winter Chill", message = "Snow blankets the land — take care of your animals and torches.", bonusText = "Winter Bonus: +15% Animal Warmth", theme = "Info" };
        }

        if (uiAudioSource == null)
        {
            GameObject go = new GameObject("Notification UI AudioSource");
            go.transform.SetParent(this.transform, false);
            uiAudioSource = go.AddComponent<AudioSource>();
            uiAudioSource.playOnAwake = false;
            uiAudioSource.ignoreListenerPause = true;
            uiAudioSource.spatialBlend = 0f;
        }
    }

    /// <summary>
    /// Ensures the NotificationManager GameObject is active before showing notifications
    /// </summary>
    private static void EnsureActive()
    {
        if (Instance == null) return;

        // Only activate THIS GameObject, not the entire hierarchy
        // This allows coroutines to run without keeping parent UI containers visible
        if (!Instance.gameObject.activeSelf)
        {
            Instance.gameObject.SetActive(true);
        }
    }

    public static void ShowNotification(string title, string message, string theme = "Info", float duration = 2f, System.Action onComplete = null)
    {
        if (Instance == null) return;
        EnsureActive();

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
        EnsureActive();

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
            duration = 0f,
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

    // Test method to trigger unlock notifications manually
    public static void TestUnlockNotification()
    {
        ShowBadge("New Structure Unlocked!", "Test Building is now available!", 3f);
        Debug.Log("[NotificationManager] Test unlock notification triggered!");
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

        // Safety check: ensure THIS GameObject is active before starting coroutine
        // We don't activate the entire hierarchy to avoid keeping parent UI containers visible
        if (!gameObject.activeSelf)
        {
            Debug.LogWarning("NotificationManager: ProcessQueue called while inactive. Activating NotificationManager GameObject...");
            gameObject.SetActive(true);
        }

        // If parent hierarchy is inactive, we can't run coroutines
        // In this case, the NotificationManager should be moved to an always-active hierarchy
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogError("NotificationManager: Parent hierarchy is inactive! Cannot start coroutines. Please ensure NotificationManager is in an active hierarchy.");
            return;
        }

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

        RectTransform rectTransform = notification.GetComponent<RectTransform>();
        CanvasGroup canvasGroup = notification.GetComponent<CanvasGroup>();
        Button continueButton = notification.GetComponentInChildren<Button>();

        bool buttonClicked = false;
        
        Vector3 targetPosition = positionOffset;
        Vector3 startPosition = targetPosition + Vector3.right * (notificationSize.x + 100f);
        
        rectTransform.anchoredPosition = startPosition;
        rectTransform.localScale = Vector3.zero;
        canvasGroup.alpha = 0f;

        NotificationTheme theme = GetTheme(data.theme);
        AudioSource playSource = uiAudioSource != null ? uiAudioSource : audioSource;

        // Check if tutorial is active
        bool isTutorialActive = SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive();
        
        // Only suppress production boost and unlock notification open sounds during tutorial, not UI click sounds
        // Suppress: Info (season changes), Achievement (unlocks), Badge (structure unlocks), Animal (animal events),
        // Success (production boosts), Warning (season warnings)
        bool isProductionOrUnlockNotification = data.theme == "Info" || data.theme == "Achievement" || data.theme == "Badge" || 
                                                 data.theme == "Animal" || data.theme == "Success" || data.theme == "Warning";
        bool suppressNotificationOpenSound = isTutorialActive && isProductionOrUnlockNotification;

        if (!suppressNotificationOpenSound && theme?.soundEffect != null && playSource != null)
        {
            playSource.PlayOneShot(theme.soundEffect, theme.soundVolume);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(() =>
            {
                AudioSource clickSource = uiAudioSource != null ? uiAudioSource : audioSource;
                // UI click sounds are ALWAYS allowed, even during tutorial
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

        bool isBadge = data.theme == "Badge";
        
        StartCoroutine(FlashEffect(notification, theme, isBadge));
        
        float slideDuration = isBadge ? slideInDuration * 0.6f : slideInDuration;
        bool isBlocking = data != null && data.pauseOnShow;

        if (isBlocking)
        {
            rectTransform.anchoredPosition = targetPosition;
            rectTransform.localScale = Vector3.zero;

            LeanTween.scale(notification, Vector3.one * (isBadge ? 1.2f : 1.05f), slideDuration)
                .setEase(isBadge ? LeanTweenType.easeOutBack : LeanTweenType.easeOutQuart)
                .setOnComplete(() =>
                {
                    LeanTween.scale(notification, Vector3.one, 0.25f).setEase(LeanTweenType.easeOutBounce);
                });

            LeanTween.alphaCanvas(canvasGroup, 1f, slideDuration * 0.5f).setDelay(0.02f);
        }
        else
        {
            LeanTween.moveX(rectTransform, targetPosition.x, slideDuration)
                .setEase(isBadge ? LeanTweenType.easeOutBack : LeanTweenType.easeOutQuart)
                .setDelay(isBadge ? 0f : 0.1f);

            float scaleDuration = isBadge ? slideDuration * 0.7f : slideDuration * 0.8f;
            float maxScale = isBadge ? 1.2f : 1.0f;
            LeanTween.scale(notification, Vector3.one * maxScale, scaleDuration)
                .setEase(isBadge ? LeanTweenType.easeOutBack : LeanTweenType.easeOutQuart)
                .setDelay(isBadge ? 0.05f : 0.2f);

            float fadeDelay = isBadge ? 0.05f : 0.15f;
            LeanTween.alphaCanvas(canvasGroup, 1f, slideDuration * 0.5f)
                .setDelay(fadeDelay);
        }

        yield return new WaitForSeconds(slideDuration * 0.8f);
        
        if (isBadge)
        {
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
            LeanTween.scale(notification, Vector3.one * 1.03f, 0.2f)
                .setEase(LeanTweenType.easeInOutQuad)
                .setOnComplete(() =>
                {
                    LeanTween.scale(notification, Vector3.one, 0.3f)
                        .setEase(LeanTweenType.easeInOutQuad);
                });
        }

        StartCoroutine(SparkleEffectNoLines(notification, data.duration, isBadge));

        // === DISPLAY DURATION WITH BLUR ===
        if (data.pauseOnShow)
        {
            // Switch canvas to camera mode so blur can affect it
            // if (canvasModeSwitcher != null)
            // {
            //     canvasModeSwitcher.SwitchToCameraMode();
            // }

            // Small delay to let canvas mode switch take effect
            yield return null;

            // Pause the game
            PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
            if (pauseManager != null)
            {
                pauseManager.pauseGame();
            }

            // Wait for button click
            yield return new WaitUntil(() => buttonClicked);

            // Wait for blur to fade out
            yield return new WaitForSeconds(backdropBlurFade);

            // Restore canvas mode
            // if (canvasModeSwitcher != null)
            // {
            //     canvasModeSwitcher.RestoreOriginalMode();
            // }

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
        LeanTween.scale(notification, Vector3.one * 0.95f, 0.15f)
            .setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() =>
            {
                LeanTween.scale(notification, Vector3.one, 0.15f)
                    .setEase(LeanTweenType.easeInOutQuad);
            });

        yield return new WaitForSeconds(0.3f);

        LeanTween.moveX(rectTransform, targetPosition.x - 700f, slideOutDuration)
            .setEase(LeanTweenType.easeInQuart);

        LeanTween.scale(notification, Vector3.zero, slideOutDuration)
            .setEase(LeanTweenType.easeInQuart)
            .setDelay(0.1f);

        LeanTween.alphaCanvas(canvasGroup, 0f, slideOutDuration * 0.7f);

        yield return new WaitForSeconds(slideOutDuration);

        // Cleanup
        if (notification != null)
        {
            BackdropHolder holder = notification.GetComponent<BackdropHolder>();
            if (holder != null && holder.backdrop != null)
            {
                Destroy(holder.backdrop);
            }

            Destroy(notification);
        }

        data.onComplete?.Invoke();
        isShowingNotification = false;

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

        DebugCanvasSetup();

        GameObject notification = Instantiate(prefabToUse, notificationContainer);
        NotificationTheme theme = GetTheme(data.theme);

        Canvas[] childCanvases = notification.GetComponentsInChildren<Canvas>(true);
        if (childCanvases == null || childCanvases.Length == 0)
        {
            Canvas added = notification.AddComponent<Canvas>();
            added.renderMode = RenderMode.ScreenSpaceOverlay;
            added.overrideSorting = true;
            added.sortingOrder = 1000;
            if (notification.GetComponent<GraphicRaycaster>() == null)
                notification.AddComponent<GraphicRaycaster>();
        }
        else
        {
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

        Button childButton = notification.GetComponentInChildren<Button>(true);
        if (childButton != null)
        {
            childButton.interactable = true;
            var targetImg = childButton.targetGraphic as Image;
            if (targetImg == null)
            {
                targetImg = childButton.GetComponent<Image>() ?? childButton.GetComponentInChildren<Image>();
            }
            if (targetImg != null)
                targetImg.raycastTarget = true;
        }

        // SIMPLIFIED BACKDROP - NO UI BLUR MATERIAL
        if (data != null && data.pauseOnShow)
        {
            Transform parentForBackdrop = notificationCanvas != null ? notificationCanvas.transform : 
                                           (notificationContainer != null ? notificationContainer : notification.transform.parent);

            GameObject backdrop = new GameObject("Backdrop");
            backdrop.transform.SetParent(parentForBackdrop, false);
            RectTransform bRect = backdrop.AddComponent<RectTransform>();
            bRect.anchorMin = Vector2.zero;
            bRect.anchorMax = Vector2.one;
            bRect.offsetMin = Vector2.zero;
            bRect.offsetMax = Vector2.zero;
            bRect.localScale = Vector3.one;

            backdrop.AddComponent<CanvasRenderer>();

            Image bgImage = backdrop.AddComponent<Image>();
            bgImage.color = new Color(0f, 0f, 0f, blockingBackdropAlpha);
            bgImage.raycastTarget = true;

            notification.transform.SetAsLastSibling();

            BackdropHolder holder = notification.GetComponent<BackdropHolder>();
            if (holder == null) holder = notification.AddComponent<BackdropHolder>();
            holder.backdrop = backdrop;
        }

        RectTransform notificationRect = notification.GetComponent<RectTransform>();
        if (notificationRect != null)
        {
            Vector2 anchor = new Vector2(anchorX, anchorY);
            notificationRect.anchorMin = anchor;
            notificationRect.anchorMax = anchor;
            notificationRect.pivot = new Vector2(0.5f, 0.5f);

            if (data != null && data.pauseOnShow && blockingNotificationPrefab != null)
            {
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

                if (!blockingPreservePrefabPosition)
                {
                    notificationRect.anchoredPosition = positionOffset;
                }
            }
            else
            {
                notificationRect.sizeDelta = notificationSize;
                notificationRect.anchoredPosition = positionOffset;
            }

            notificationRect.localScale = Vector3.one;
        }

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

        TextMeshProUGUI unlocksTMP = notification.transform.Find("unlocksText")?.GetComponent<TextMeshProUGUI>();
        UnityEngine.UI.Text unlocksUIText = null;

        if (unlocksTMP == null)
        {
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

        if (backgroundImage != null && theme != null)
            backgroundImage.color = theme.backgroundColor;

        if (data != null && data.pauseOnShow && forceBlockingOpaque && backgroundImage != null)
        {
            Color c = backgroundImage.color;
            c.a = 1f;
            backgroundImage.color = c;
        }

        if (iconImage != null && theme != null)
        {
            Sprite chosen = null;
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
            
            if (enableGlowEffects)
            {
                StartCoroutine(ProperBorderGlow(borderImage, theme.borderColor));
                CreateRealGlowEffect(notification, theme);
            }
        }

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

    private IEnumerator ProperBorderGlow(Image borderImage, Color glowColor)
    {
        if (borderImage == null) yield break;
        
        Color originalColor = glowColor;
        Color brightColor = new Color(glowColor.r + 0.5f, glowColor.g + 0.5f, glowColor.b + 0.5f, 1f);
        
        while (borderImage != null && borderImage.gameObject != null)
        {
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
        StartCoroutine(PulseScale(notification, theme));
        
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
        Vector3 pulseScale = Vector3.one * 1.02f;
        
        while (notification != null)
        {
            LeanTween.scale(notification, pulseScale, glowPulseSpeed * 2f)
                .setEase(LeanTweenType.easeInOutSine);
                
            yield return new WaitForSeconds(glowPulseSpeed * 2f);
            
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
            if (Random.value < 0.3f)
            {
                CreateFloatingParticle(notification.transform, theme.borderColor);
            }
            
            elapsed += 0.5f;
            yield return new WaitForSeconds(0.5f);
        }
    }
    
    private void CreateFloatingParticle(Transform parent, Color particleColor)
    {
        GameObject particle = new GameObject("FloatingParticle");
        particle.transform.SetParent(parent, false);
        
        Image particleImage = particle.AddComponent<Image>();
        
        Texture2D circleTexture = CreateCircleTexture(16);
        particleImage.sprite = Sprite.Create(circleTexture, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
        
        RectTransform particleRect = particle.GetComponent<RectTransform>();
        particleRect.sizeDelta = new Vector2(6, 6);
        
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        float distance = Random.Range(50f, 100f);
        Vector2 startPos = new Vector2(
            Mathf.Cos(angle) * distance,
            Mathf.Sin(angle) * distance
        );
        particleRect.anchoredPosition = startPos;
        
        Color glowColor = particleColor;
        glowColor.a = 0.8f;
        particleImage.color = glowColor;
        
        Vector2 endPos = startPos + Vector2.up * 30f;
        
        LeanTween.move(particleRect, endPos, 2f)
            .setEase(LeanTweenType.easeOutQuad);
            
        LeanTween.alpha(particleRect, 0f, 2f)
            .setEase(LeanTweenType.easeOutQuad)
            .setOnComplete(() => {
                if (particle != null) Destroy(particle);
            });
            
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
        
        GameObject flash = new GameObject("Flash");
        flash.transform.SetParent(notification.transform);
        flash.transform.SetAsLastSibling();
        
        RectTransform flashRect = flash.AddComponent<RectTransform>();
        Image flashImage = flash.AddComponent<Image>();
        
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        flashRect.localScale = Vector3.one;
        
        Color flashColor = new Color(1f, 1f, 1f, isBadge ? 1f : 0.8f);
        if (theme != null)
        {
            flashColor = new Color(
                theme.borderColor.r + 0.5f, 
                theme.borderColor.g + 0.5f, 
                theme.borderColor.b + 0.5f, 
                0.8f
            );
        }
        
        flashImage.color = flashColor;
        
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
        
        float intensityMultiplier = isBadge ? 2f : 1f;
        float frequency = isBadge ? 4f : 3f;
        
        while (elapsed < duration && notification != null)
        {
            float glowAmount = Mathf.Sin(elapsed * frequency) * sparkleIntensity * 0.3f * intensityMultiplier;
            Color glowColor = originalColor + Color.white * glowAmount;
            glowColor.a = originalColor.a;
            
            if (backgroundImage != null)
                backgroundImage.color = glowColor;

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (backgroundImage != null)
            backgroundImage.color = originalColor;
    }

    private NotificationTheme GetTheme(string themeName)
    {
        if (string.IsNullOrEmpty(themeName))
            return themes.Length > 0 ? themes[0] : null;

        string Normalize(string s)
        {
            s = s.Trim().ToLowerInvariant();
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
        // Empty method for debugging canvas setup if needed
    }
}
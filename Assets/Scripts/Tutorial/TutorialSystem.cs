using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Main integration script for the tutorial system.
/// Place this on a GameObject in your main scene to set up the entire tutorial system.
/// </summary>
public class TutorialSystem : MonoBehaviour
{
    [Header("Tutorial Components")]
    [SerializeField] private TutorialManager tutorialManager;
    [SerializeField] private TutorialConditionTracker conditionTracker;
    [SerializeField] private TutorialProgressTracker progressTracker;
    [SerializeField] private TutorialHighlighter highlighter;
    [SerializeField] private TutorialUISetup uiSetup;
    
    [Header("Auto Setup")]
    [SerializeField] private bool autoCreateComponents = true;
    [SerializeField] private bool startTutorialOnGameStart = true;
    [SerializeField] private float startDelay = 1f;
    
    [Header("Tutorial Assets")]
    [SerializeField] private GameObject tutorialUIPrefab;
    [SerializeField] private GameObject worldPointerPrefab;
    [SerializeField] private GameObject uiArrowPrefab;
    [SerializeField] private Material highlightMaterial;
    
    [Header("Old Man Character")]
    [SerializeField] private Sprite oldManPortrait;
    [SerializeField] private AudioClip[] oldManVoiceClips;
    
    [Header("Events")]
    public UnityEvent OnTutorialStarted;
    public UnityEvent OnTutorialCompleted;
    public UnityEvent OnTutorialSkipped;

    public static TutorialSystem Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Auto-create components if needed
        if (autoCreateComponents)
        {
            SetupTutorialComponents();
        }
    }

    private void Start()
    {
        // Initialize tutorial system
        InitializeTutorialSystem();

        // Start tutorial if enabled
        if (startTutorialOnGameStart && ShouldStartTutorial())
        {
            Invoke(nameof(StartTutorial), startDelay);
        }
    }

    private void SetupTutorialComponents()
    {
        // Setup Tutorial Manager
        if (tutorialManager == null)
        {
            tutorialManager = FindFirstObjectByType<TutorialManager>();
            if (tutorialManager == null)
            {
                GameObject managerObj = new GameObject("TutorialManager");
                managerObj.transform.SetParent(transform);
                tutorialManager = managerObj.AddComponent<TutorialManager>();
            }
        }

        // Setup Condition Tracker
        if (conditionTracker == null)
        {
            conditionTracker = FindFirstObjectByType<TutorialConditionTracker>();
            if (conditionTracker == null)
            {
                GameObject trackerObj = new GameObject("TutorialConditionTracker");
                trackerObj.transform.SetParent(transform);
                conditionTracker = trackerObj.AddComponent<TutorialConditionTracker>();
            }
        }

        // Setup Progress Tracker
        if (progressTracker == null)
        {
            progressTracker = FindFirstObjectByType<TutorialProgressTracker>();
            if (progressTracker == null)
            {
                GameObject progressObj = new GameObject("TutorialProgressTracker");
                progressObj.transform.SetParent(transform);
                progressTracker = progressObj.AddComponent<TutorialProgressTracker>();
            }
        }

        // Setup Highlighter
        if (highlighter == null)
        {
            highlighter = FindFirstObjectByType<TutorialHighlighter>();
            if (highlighter == null)
            {
                GameObject highlighterObj = new GameObject("TutorialHighlighter");
                highlighterObj.transform.SetParent(transform);
                highlighter = highlighterObj.AddComponent<TutorialHighlighter>();
            }
        }

        // Setup UI
        if (uiSetup == null && tutorialUIPrefab != null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                GameObject uiObj = Instantiate(tutorialUIPrefab, canvas.transform);
                uiSetup = uiObj.GetComponent<TutorialUISetup>();
            }
        }
    }

    private void InitializeTutorialSystem()
    {
        // Configure tutorial manager
        if (tutorialManager != null)
        {
            // Configure with assets
            // tutorialManager.SetCharacterAssets(oldManPortrait, oldManVoiceClips);
            
            // Connect auto-created UI to tutorial manager
            ConnectUIToTutorialManager();
        }

        // Configure highlighter
        if (highlighter != null)
        {
            // Setup highlighter prefabs and materials
            // highlighter.SetPrefabs(worldPointerPrefab, uiArrowPrefab, highlightMaterial);
        }

        // Subscribe to events
        SubscribeToEvents();
    }

    private void ConnectUIToTutorialManager()
    {
        // Find the auto-created tutorial UI
        GameObject tutorialUI = GameObject.Find("TutorialUI");
        if (tutorialUI == null)
        {
            Debug.LogWarning("TutorialUI not found! Make sure auto-setup ran correctly.");
            return;
        }

        // Get UI components and connect them to TutorialManager
        var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
        if (uiScript != null && tutorialManager != null)
        {
            // Use reflection or direct assignment to connect UI elements
            var tutorialManagerType = typeof(TutorialManager);
            
            // Set tutorial panel
            var tutorialPanelField = tutorialManagerType.GetField("tutorialPanel", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialPanelField != null)
            {
                tutorialPanelField.SetValue(tutorialManager, tutorialUI);
            }

            // Set dialogue text
            var tutorialDescriptionField = tutorialManagerType.GetField("tutorialDescription", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialDescriptionField != null && uiScript.dialogueText != null)
            {
                tutorialDescriptionField.SetValue(tutorialManager, uiScript.dialogueText);
            }

            // Set title text (character name)
            var tutorialTitleField = tutorialManagerType.GetField("tutorialTitle", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (tutorialTitleField != null && uiScript.characterNameText != null)
            {
                tutorialTitleField.SetValue(tutorialManager, uiScript.characterNameText);
            }

            // Set next button
            var nextButtonField = tutorialManagerType.GetField("nextButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (nextButtonField != null && uiScript.nextButton != null)
            {
                nextButtonField.SetValue(tutorialManager, uiScript.nextButton);
            }

            // Set skip button
            var skipButtonField = tutorialManagerType.GetField("skipButton", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (skipButtonField != null && uiScript.skipButton != null)
            {
                skipButtonField.SetValue(tutorialManager, uiScript.skipButton);
            }

            // Set character portrait
            var characterPortraitField = tutorialManagerType.GetField("characterPortrait", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (characterPortraitField != null && uiScript.characterPortrait != null)
            {
                characterPortraitField.SetValue(tutorialManager, uiScript.characterPortrait);
            }

            Debug.Log("UI components connected to TutorialManager!");
        }
        else
        {
            Debug.LogWarning("Could not find TutorialUIPrefab component on auto-created UI!");
        }
    }

    private void SubscribeToEvents()
    {
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted += HandleTutorialCompleted;
        }

        if (progressTracker != null)
        {
            progressTracker.OnTutorialCompleted += HandleTutorialFinished;
        }
    }

    private bool ShouldStartTutorial()
    {
        // Check if tutorial is enabled in settings
        if (!TutorialSettingsMenu.IsTutorialEnabled())
        {
            return false;
        }

        // Check if tutorial should be skipped because it's already completed
        if (TutorialSettingsMenu.ShouldSkipCompleted() && HasCompletedTutorial())
        {
            return false;
        }

        // Check if this is a new game or tutorial restart
        return true;
    }

    private bool HasCompletedTutorial()
    {
        return progressTracker != null && progressTracker.IsTutorialCompleted();
    }

    public void StartTutorial()
    {
        if (tutorialManager != null)
        {
            Debug.Log("Starting tutorial system...");
            OnTutorialStarted?.Invoke();
            
            // The tutorial manager will handle the rest
            tutorialManager.OnConditionMet(TutorialCondition.GameStarted);
        }
        else
        {
            Debug.LogError("TutorialManager not found! Cannot start tutorial.");
        }
    }

    public void StopTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
        }
    }

    public void RestartTutorial()
    {
        if (progressTracker != null)
        {
            progressTracker.ResetProgress();
        }

        if (tutorialManager != null)
        {
            tutorialManager.ResetTutorial();
        }

        // Start tutorial again after a short delay
        Invoke(nameof(StartTutorial), 0.5f);
    }

    public void PauseTutorial()
    {
        // Implementation depends on how you want to handle pausing
        // Could pause time, disable input, etc.
        Time.timeScale = 0f;
    }

    public void ResumeTutorial()
    {
        Time.timeScale = 1f;
    }

    private void HandleTutorialCompleted()
    {
        Debug.Log("Tutorial completed!");
        OnTutorialCompleted?.Invoke();
    }

    private void HandleTutorialFinished(float totalTime)
    {
        Debug.Log($"Tutorial finished in {totalTime:F1} seconds!");
        OnTutorialCompleted?.Invoke();
    }

    public void SkipCurrentStep()
    {
        if (tutorialManager != null)
        {
            tutorialManager.NextTutorialStep();
        }
    }

    public void SkipEntireTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
            OnTutorialSkipped?.Invoke();
        }
    }

    // Public API for external systems
    public bool IsTutorialActive()
    {
        return tutorialManager != null && tutorialManager.IsTutorialActive();
    }

    public bool IsTutorialCompleted()
    {
        return progressTracker != null && progressTracker.IsTutorialCompleted();
    }

    public void TriggerCondition(TutorialCondition condition)
    {
        if (tutorialManager != null)
        {
            tutorialManager.OnConditionMet(condition);
        }
    }

    public void HighlightUIElement(string tag)
    {
        if (highlighter != null)
        {
            highlighter.HighlightUIElement(tag);
        }
    }

    public void HighlightWorldPosition(Vector3 position)
    {
        if (highlighter != null)
        {
            highlighter.HighlightWorldPosition(position);
        }
    }

    public void ClearAllHighlights()
    {
        if (highlighter != null)
        {
            highlighter.ClearAllHighlights();
        }
    }

    // Debug methods for testing
    [ContextMenu("Start Tutorial (Debug)")]
    public void Debug_StartTutorial()
    {
        StartTutorial();
    }

    [ContextMenu("Complete Tutorial (Debug)")]
    public void Debug_CompleteTutorial()
    {
        if (tutorialManager != null)
        {
            tutorialManager.SkipTutorial();
        }
    }

    [ContextMenu("Reset Tutorial (Debug)")]
    public void Debug_ResetTutorial()
    {
        RestartTutorial();
    }

    [ContextMenu("Trigger Shop Opened (Debug)")]
    public void Debug_TriggerShopOpened()
    {
        TriggerCondition(TutorialCondition.ShopOpened);
    }

    [ContextMenu("Trigger First Structure Placed (Debug)")]
    public void Debug_TriggerFirstStructure()
    {
        TriggerCondition(TutorialCondition.FirstStructurePlaced);
    }

    [ContextMenu("Force Show Tutorial UI (Debug)")]
    public void Debug_ForceShowTutorialUI()
    {
        // Find and force show the tutorial UI
        GameObject tutorialUI = GameObject.Find("TutorialUI");
        if (tutorialUI != null)
        {
            tutorialUI.SetActive(true);
            
            var canvasGroup = tutorialUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            Debug.Log("Forced tutorial UI to show!");
        }
        else
        {
            Debug.LogError("TutorialUI GameObject not found!");
        }
    }

    [ContextMenu("Debug Tutorial UI Components")]
    public void Debug_TutorialUIComponents()
    {
        GameObject tutorialUI = GameObject.Find("TutorialUI");
        if (tutorialUI != null)
        {
            Debug.Log($"TutorialUI found: {tutorialUI.name}");
            Debug.Log($"TutorialUI active: {tutorialUI.activeInHierarchy}");
            
            var canvasGroup = tutorialUI.GetComponent<CanvasGroup>();
            if (canvasGroup != null)
            {
                Debug.Log($"CanvasGroup alpha: {canvasGroup.alpha}");
            }
            
            var uiScript = tutorialUI.GetComponent<TutorialUIPrefab>();
            if (uiScript != null)
            {
                Debug.Log("TutorialUIPrefab script found");
                Debug.Log($"Dialogue text: {(uiScript.dialogueText != null ? "Found" : "Missing")}");
                Debug.Log($"Next button: {(uiScript.nextButton != null ? "Found" : "Missing")}");
            }
        }
        else
        {
            Debug.LogError("TutorialUI GameObject not found in scene!");
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (tutorialManager != null)
        {
            tutorialManager.OnTutorialCompleted -= HandleTutorialCompleted;
        }

        if (progressTracker != null)
        {
            progressTracker.OnTutorialCompleted -= HandleTutorialFinished;
        }
    }
}

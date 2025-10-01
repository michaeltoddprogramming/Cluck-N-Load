using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialManager : MonoBehaviour
{
    [Header("UI References")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI titleText;
    public Image characterPortraitImage;
    public Button skipTutorialButton;
    public GameObject keyIndicatorPrefab;
    public RectTransform keyIndicatorContainer;
    
    [Header("Mouse Icon Sprites")]
    public Sprite lmbIcon;      // Left Mouse Button
    public Sprite rmbIcon;      // Right Mouse Button  
    public Sprite mmbIcon;      // Middle Mouse Button
    public Sprite mmbDownIcon;  // Middle Mouse Button Down (scroll down)
    public Sprite mmbUpIcon;    // Middle Mouse Button Up (scroll up)
    
    [Header("Mouse Icon Settings")]
    [Range(0.5f, 5.0f)]
    public float mouseIconScale = 2.0f;  // Configurable scale for mouse icons
    
    private GameObject tutorialArrow;
    private RectTransform arrowRect;

    [Header("Tutorial UI References")]
    public GameObject shopButton;
    public GameObject farmhouseButton;
    public GameObject cropPlotButton;
    public GameObject siloButton;
    public GameObject chickenCoopButton;
    public GameObject barracksButton;
    public Button nextStepButton;
    public Button discoveryCloseButton;

    [Header("Audio")]
    public AudioSource mumbleAudioSource;
    public AudioClip[] mumbleClips;
    public float typeSpeed = 0.04f;
    public AudioClip keyPressSound;
    private AudioSource effectsAudioSource;

    [Header("Tutorial Steps")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private int currentStepIndex = -1;
    private bool waitingForStepToComplete = false;
    private Coroutine typingCoroutine;

    private static TutorialManager instance;
    public static TutorialManager Instance => instance;

    private HashSet<KeyCode> detectedInputs = new HashSet<KeyCode>();
    private List<GameObject> keyIndicators = new List<GameObject>();
    private Dictionary<KeyCode, GameObject> keyIndicatorMap = new Dictionary<KeyCode, GameObject>();

    [Header("Discovery System")]
    private Dictionary<TutorialTrigger, TutorialStep> discoverySteps = new Dictionary<TutorialTrigger, TutorialStep>();
    private HashSet<string> shownDiscoveries = new HashSet<string>();
    private bool isShowingDiscovery = false;
    private TutorialStep currentDiscoveryStep;

    private Queue<TutorialTrigger> pendingTriggers = new Queue<TutorialTrigger>();
    private bool isProcessingStep = false;
    private float lastStepCompletionTime = 0f;
    private const float MIN_STEP_DURATION = 0.5f;
    private bool isMumblePaused = false;

    private bool wasTutorialSkippedByDev = false; // Add this field near the other private fields
    
    [Header("Melony Hunt System")]
    public GameObject melonyPrefab; // Civilian chicken prefab
    public AudioClip melonyExplosionSound;
    public GameObject explosionEffectPrefab;
    
    private GameObject currentMelony;
    private string currentMelonyTask = "";
    private List<KeyCode> detectedMelonyInputs = new List<KeyCode>();
    private List<string> detectedMelonyActions = new List<string>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }
            else
            {
                Debug.LogError("TutorialManager: tutorialPanel is not assigned in the Inspector!");
            }
            mumbleAudioSource = mumbleAudioSource ?? gameObject.AddComponent<AudioSource>();
            effectsAudioSource = gameObject.AddComponent<AudioSource>();
            effectsAudioSource.playOnAwake = false;
            InitializeTutorialSteps();
            skipTutorialButton?.gameObject.SetActive(false); // Hide skip button at start
            SetupChecklist();

            // Subscribe to pause/resume events
            if (GameEventManager.Instance != null)
            {
                GameEventManager.Instance.OnGamePaused.AddListener(PauseMumbleAudio);
                GameEventManager.Instance.OnGameResumed.AddListener(ResumeMumbleAudio);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        StartTutorial();
    }

    private void Update()
    {
        // Developer option: Skip tutorial with Backspace key
        if (Input.GetKeyDown(KeyCode.Backspace) && IsTutorialActive())
        {
            DevSkipTutorial(); // Call developer-specific method
        }
    
        HandleRequiredInputDetection();
        HandleMelonyClickDetection();
    
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
    
            if (scrollDelta > 0 && step.requiredInputs.Contains(KeyCode.Mouse3))
            {
                detectedInputs.Add(KeyCode.Mouse3);
                UpdateKeyIndicatorVisual(KeyCode.Mouse3, true);
            }
            else if (scrollDelta < 0 && step.requiredInputs.Contains(KeyCode.Mouse4))
            {
                detectedInputs.Add(KeyCode.Mouse4);
                UpdateKeyIndicatorVisual(KeyCode.Mouse4, true);
            }
        }
    
        if (!isProcessingStep && pendingTriggers.Count > 0)
        {
            TutorialTrigger nextTrigger = pendingTriggers.Dequeue();
            ProcessTrigger(nextTrigger);
        }
    }

    public void StartTutorial()
    {
        // Prevent multiple simultaneous starts
        if (isProcessingStep || IsTutorialActive())
        {
            return;
        }
        
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }
        else
        {
            Debug.LogError("Tutorial panel is null - cannot start tutorial");
            return;
        }
        
        if (steps.Count == 0)
        {
            Debug.LogError("No tutorial steps found - cannot start tutorial");
            return;
        }
        
        currentStepIndex = -1;
        waitingForStepToComplete = false;
        NextStep();
        
        // Notify UI systems that tutorial has started
        NotifyUISystemsOfStepChange();
    }

    void NextStep()
    {
        // Prevent spam-clicking by checking if we're already processing a step
        if (isProcessingStep)
        {
            return;
        }

        // Add minimum time between steps to prevent rapid advancement (but not for the first step)
        if (lastStepCompletionTime > 0 && Time.realtimeSinceStartup - lastStepCompletionTime < MIN_STEP_DURATION)
        {
            return;
        }

        // Safety check: ensure we have valid steps collection
        if (steps == null || steps.Count == 0)
        {
            EndTutorial();
            return;
        }

        // Safety check: if we're already past the end, don't continue
        if (currentStepIndex >= steps.Count)
        {
            return; // Already ended
        }

        // Mark current step as complete if it hasn't been already
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            if (!string.IsNullOrEmpty(step.stepId) && !GetCompletedStepIds().Contains(step.stepId))
            {
                if (step.uiToHighlight != null)
                    HighlightUI(step.uiToHighlight, false);

                step.onStepComplete?.Invoke();
                MarkStepComplete(step.stepId);
            }
        }

        isProcessingStep = true;
        
        // Disable the next button immediately to prevent further clicks
        if (nextStepButton != null)
        {
            nextStepButton.interactable = false;
        }
        
        StartCoroutine(ProcessNextStepSafely());
    }

    private IEnumerator ProcessNextStepSafely()
    {
        try
        {
            if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                var currentStep = steps[currentStepIndex];

                if (currentStep.uiToHighlight != null)
                    HighlightUI(currentStep.uiToHighlight, false);

                ClearKeyIndicators();
                CleanupAllWorldHighlights();
                CleanupShopHighlights();
            }

            currentStepIndex++;
            
            // Notify UI systems that tutorial step has changed
            NotifyUISystemsOfStepChange();
            
            if (currentStepIndex >= steps.Count)
            {
                EndTutorial();
                yield break;
            }

            // Get the step reference immediately after bounds check to prevent index issues
            // Double-check bounds again in case something changed during execution
            if (currentStepIndex < 0 || currentStepIndex >= steps.Count)
            {
                EndTutorial();
                yield break;
            }
            
            var step = steps[currentStepIndex];

            yield return new WaitForSecondsRealtime(0.1f);
            
            // Triple-check that we still have a valid step after the yield
            if (currentStepIndex < 0 || currentStepIndex >= steps.Count || step == null)
            {
                EndTutorial();
                yield break;
            }
            
            detectedInputs.Clear();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextWithMumble(step.instructionText));
            
            // Aggressively ensure rich text support is enabled for title text
            if (titleText != null)
            {
                titleText.richText = true;
                titleText.parseCtrlCharacters = true; // This is important for rich text
                titleText.SetAllDirty();
                titleText.ForceMeshUpdate();
            }
            
            titleText.text = step.title;
            UpdateCharacterPortrait(step);

            // Highlight UI for this step if needed
            if (step.uiToHighlight != null)
                HighlightUI(step.uiToHighlight, true);

            // Show key indicators if required
            if (step.requiredInputs != null && step.requiredInputs.Count > 0)
                ShowKeyIndicators(step.requiredInputs);
            else
                ClearKeyIndicators();

            // Disable nextStepButton if the step requires user action
            bool requiresUserAction = step.triggerToWaitFor != TutorialTrigger.None || (step.requiredInputs != null && step.requiredInputs.Count > 0);
            if (nextStepButton != null)
            {
                nextStepButton.gameObject.SetActive(true); // Always visible
                nextStepButton.interactable = !requiresUserAction; // Disable if action required
                nextStepButton.onClick.RemoveAllListeners();
                if (!requiresUserAction)
                {
                    nextStepButton.onClick.AddListener(() => {
                        // Double-check that we can still advance (prevent bypassing action-required steps)
                        if (!waitingForStepToComplete && !isProcessingStep)
                        {
                            NextStep();
                        }
                    });
                }
            }

            step.onStepStart?.Invoke();

            waitingForStepToComplete = requiresUserAction;

            // Hide the close button for normal tutorial steps
            if (discoveryCloseButton != null)
                discoveryCloseButton.gameObject.SetActive(false);
        }
        finally
        {
            isProcessingStep = false;
            lastStepCompletionTime = Time.realtimeSinceStartup;
        }

        // Show the next button for normal steps
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(true);
    }

    public void RegisterDiscoveryStep(TutorialStep step)
    {
        discoverySteps[step.triggerToWaitFor] = step;
    }

    public void Trigger(TutorialTrigger trigger)
    {
        if (isProcessingStep)
        {
            pendingTriggers.Enqueue(trigger);
            return;
        }

        if (trigger != TutorialTrigger.None && IsStepCompletedForTrigger(trigger))
        {
            return;
        }

        ProcessTrigger(trigger);
    }

    private bool IsStepCompletedForTrigger(TutorialTrigger trigger)
    {
        // Map triggers to step IDs (add more as needed)
        string stepId = trigger switch
        {
            TutorialTrigger.BoughtFirstAnimals => "buy_chickens",
            TutorialTrigger.FedFirstAnimals => "feed_chickens",
            TutorialTrigger.PlantedCrop => "plant_first_crop",
            TutorialTrigger.HarvestedCrop => "harvest_first_crops",
            TutorialTrigger.CollectedFirstProducts => "collect_eggs",
            TutorialTrigger.BuiltSilo => "build_silo",
            TutorialTrigger.BuiltFirstHayBale => "build_first_hay_bale",
            TutorialTrigger.Built10HayBales => "build_wall_chain",
            // Add other mappings here
            _ => null
        };
        bool isCompleted = stepId != null && completedStepIds.Contains(stepId);
        return isCompleted;
    }

    private void ProcessTrigger(TutorialTrigger trigger)
    {
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];

            if (step.triggerToWaitFor == trigger)
            {
                waitingForStepToComplete = false;

                if (step.uiToHighlight != null)
                    HighlightUI(step.uiToHighlight, false);

                step.onStepComplete?.Invoke();

                if (!string.IsNullOrEmpty(step.stepId))
                    MarkStepComplete(step.stepId);

                // Only advance if we're not already processing a step
                if (!isProcessingStep)
                {
                    // Add a small delay to prevent immediate re-triggering
                    StartCoroutine(DelayedNextStep());
                }
                return;
            }
        }

        if (discoverySteps.ContainsKey(trigger) && !shownDiscoveries.Contains(discoverySteps[trigger].stepId))
        {
            var discoveryStep = discoverySteps[trigger];
            ShowDiscoveryPopup(discoveryStep);
            shownDiscoveries.Add(discoveryStep.stepId);
        }
    }

    public void EndTutorial()
    {
        // Hide tutorial UI
        tutorialPanel.SetActive(false);
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);
    
        currentStepIndex = steps.Count;
        waitingForStepToComplete = false;
        isProcessingStep = false; // Reset processing flag
        
        // Reset any inappropriate instant production states now that tutorial is over
        AnimalStructure.ResetAllInstantProductionStates();
        
        NotifyUISystemsOfStepChange();
    }
    
    public void SkipTutorial()
    {
        // Prevent double execution - only skip if tutorial is actually active
        if (!IsTutorialActive())
        {
            Debug.Log("SkipTutorial called but tutorial is not active");
            return;
        }
        
        Debug.Log($"SkipTutorial: Starting skip process. Current step: {currentStepIndex}/{steps.Count}");
        
        // Mark all tutorial steps as completed WITHOUT triggering UI updates for each one
        int completedCount = 0;
        foreach (var step in steps)
        {
            if (!string.IsNullOrEmpty(step.stepId) && !completedStepIds.Contains(step.stepId))
            {
                // Add to completed list directly without calling MarkStepComplete to avoid animation spam
                completedStepIds.Add(step.stepId);
                completedCount++;
            }
        }
        
        Debug.Log($"SkipTutorial: Marked {completedCount} main steps as complete");
        
        // Also mark all discovery steps as completed
        int discoveryCompletedCount = 0;
        foreach (var discoveryStep in discoverySteps.Values)
        {
            if (!string.IsNullOrEmpty(discoveryStep.stepId) && !completedStepIds.Contains(discoveryStep.stepId))
            {
                // Add to completed list directly without calling MarkStepComplete to avoid animation spam
                completedStepIds.Add(discoveryStep.stepId);
                discoveryCompletedCount++;
            }
        }
        
        Debug.Log($"SkipTutorial: Marked {discoveryCompletedCount} discovery steps as complete");
        
        // Set tutorial as completed
        currentStepIndex = steps.Count;
        waitingForStepToComplete = false;
        isProcessingStep = false;
        
        // Hide tutorial UI
        tutorialPanel.SetActive(false);
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);

        // Clean up any active states
        CleanupAllWorldHighlights();
        CleanupShopHighlights();
        ClearKeyIndicators();
        
        // Stop any ongoing coroutines
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }
        
        // Update checklist to show all items as completed (do this ONCE at the end)
        try
        {
            UpdateChecklistUI();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating checklist UI during skip: {e.Message}");
        }
        
        // Reset any inappropriate instant production states since tutorial is being skipped
        AnimalStructure.ResetAllInstantProductionStates();
        
        // Notify all systems that tutorial is complete
        NotifyUISystemsOfStepChange();
        
        // Finalize tutorial completion
        Debug.Log($"SkipTutorial: Calling EndTutorial. Final state: {currentStepIndex}/{steps.Count}, IsActive: {IsTutorialActive()}");
        EndTutorial();
    }
    
    private void DevSkipTutorial()
    {
        tutorialPanel.SetActive(false);
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);

        CleanupAllWorldHighlights();
        CleanupShopHighlights();

        wasTutorialSkippedByDev = true;

        EndTutorial();
    }

    IEnumerator AutoAdvanceStep()
    {
        yield return new WaitForSecondsRealtime(10f);
        Trigger(TutorialTrigger.None);
    }

    IEnumerator DelayedNextStep()
    {
        yield return new WaitForSecondsRealtime(0.2f);
        NextStep();
    }

    public bool IsShowingDiscovery()
    {
        return isShowingDiscovery;
    }

    private void ShowDiscoveryPopup(TutorialStep discoveryStep)
    {
        if (isShowingDiscovery)
            return;

        isShowingDiscovery = true;
        currentDiscoveryStep = discoveryStep;

        bool wasTutorialActive = tutorialPanel.activeSelf;
        bool wasChecklistActive = checklistPanel != null && checklistPanel.activeSelf;

        tutorialPanel.SetActive(true);
        if (checklistPanel != null)
            checklistPanel.SetActive(false);

        // Show close button only for discovery steps
        if (discoveryCloseButton != null)
        {
            discoveryCloseButton.gameObject.SetActive(true);
            discoveryCloseButton.onClick.RemoveAllListeners();
            discoveryCloseButton.onClick.AddListener(() => CloseDiscoveryPopup());
        }

        // Hide next button for discovery steps
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);

        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(false);

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeTextWithMumble(discoveryStep.instructionText));
        titleText.text = discoveryStep.title;
        UpdateCharacterPortrait(discoveryStep);
        HighlightUI(discoveryStep.uiToHighlight, true);
    }

    private IEnumerator AutoCloseDiscovery(bool restoreTutorialState, bool restoreChecklistState)
    {
        yield return new WaitForSeconds(Mathf.Max(10f, currentDiscoveryStep.instructionText.Length * typeSpeed + 3f));

        HighlightUI(currentDiscoveryStep.uiToHighlight, false);
        tutorialPanel.SetActive(restoreTutorialState);

        if (checklistPanel != null)
            checklistPanel.SetActive(restoreChecklistState);

        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(true);

        isShowingDiscovery = false;
        currentDiscoveryStep = null;
    }

    public bool IsTutorialActive()
    {
        bool active = currentStepIndex >= 0 && currentStepIndex < steps.Count && !IsTutorialCompleted();
        return active;
    }

    public List<string> GetCompletedStepIds() => new List<string>(completedStepIds);
    public int GetCurrentStepIndex() => currentStepIndex;
    public bool IsTutorialCompleted() => currentStepIndex >= steps.Count;

    public void SetTutorialProgress(List<string> completedSteps, int stepIndex, bool completed)
    {
        // Prevent concurrent modifications while processing steps
        if (isProcessingStep)
        {
            return;
        }
        
        completedStepIds = completedSteps ?? new List<string>();
        currentStepIndex = stepIndex;
        if (completed)
            EndTutorial();
        else
            StartTutorialFromStep(stepIndex);
    }

    public void StartTutorialFromStep(int stepIndex)
    {
        // Prevent concurrent modifications while processing steps
        if (isProcessingStep)
        {
            return;
        }
        
        // Validate step index before proceeding
        if (stepIndex < 0 || stepIndex > steps.Count)
        {
            return;
        }
        
        tutorialPanel.SetActive(true);
        currentStepIndex = stepIndex - 1;
        waitingForStepToComplete = false;
        NextStep();
    }

    private void CloseDiscoveryPopup()
    {
        HighlightUI(currentDiscoveryStep.uiToHighlight, false);
        tutorialPanel.SetActive(false);

        if (checklistPanel != null)
            checklistPanel.SetActive(true);

        if (skipTutorialButton != null)
            skipTutorialButton.gameObject.SetActive(true);

        // Hide close button after closing discovery
        if (discoveryCloseButton != null)
            discoveryCloseButton.gameObject.SetActive(false);

        // Restore next button if appropriate
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(true);

        isShowingDiscovery = false;
        currentDiscoveryStep = null;
    }

    public void ShowPeteSeasonNotification(TutorialStep seasonStep)
    {
        // Use the existing discovery popup system to show Pete's season notifications
        if (isShowingDiscovery) return; // Don't interrupt existing discovery

        // Temporarily register this as a discovery step
        isShowingDiscovery = true;
        currentDiscoveryStep = seasonStep;

        // Show the tutorial panel and populate with Pete's season message
        tutorialPanel.SetActive(true);
        
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeTextWithMumble(seasonStep.instructionText));
        titleText.text = seasonStep.title;
        UpdateCharacterPortrait(seasonStep);

        // Hide next step button and show discovery close button
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);

        if (discoveryCloseButton != null)
        {
            discoveryCloseButton.gameObject.SetActive(true);
            discoveryCloseButton.onClick.RemoveAllListeners();
            discoveryCloseButton.onClick.AddListener(ClosePeteSeasonNotification);
        }
    }

    private void ClosePeteSeasonNotification()
    {
        // Clean up Pete season notification
        tutorialPanel.SetActive(false);
        
        if (discoveryCloseButton != null)
            discoveryCloseButton.gameObject.SetActive(false);

        isShowingDiscovery = false;
        currentDiscoveryStep = null;
    }

    // Static flag to track if we've already shown the farm house healing explanation
    private static bool hasShownFarmHouseHealing = false;

    public void TriggerFarmHouseSeasonalHealing(int season)
    {
        // Only show Pete's explanation the first time this happens and not during active tutorial
        if (hasShownFarmHouseHealing || IsTutorialActive()) return;
        
        hasShownFarmHouseHealing = true;
        
        string seasonName = GetSeasonNameForHealing(season);
        string peteMessage = $"Hey there, partner! Notice anything about your farm house?\n\n" +
                           $"Every time a new season rolls around, your farm house gets restored to <b>full health</b>! " +
                           $"Mother Nature's way of giving you a fresh start.\n\n" +
                           $"No need to worry about repairs - each season brings renewal!";
        
        // Create a discovery step for Pete's healing explanation
        var healingStep = new TutorialStep
        {
            stepId = "pete_farmhouse_healing",
            title = "Pete's Farm House Tip",
            instructionText = peteMessage,
            triggerToWaitFor = TutorialTrigger.None
        };

        // Show Pete's healing explanation as a discovery popup
        ShowPeteSeasonNotification(healingStep);
    }

    private string GetSeasonNameForHealing(int season)
    {
        return season switch
        {
            1 => "Spring",
            2 => "Summer", 
            3 => "Fall",
            4 => "Winter",
            _ => "Unknown Season"
        };
    }

    // --- Pause/Resume tutorial mumble audio on game pause/resume ---
    private void OnDestroy()
    {
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnGamePaused.RemoveListener(PauseMumbleAudio);
            GameEventManager.Instance.OnGameResumed.RemoveListener(ResumeMumbleAudio);
        }
    }

    private void PauseMumbleAudio()
    {
        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying && !isMumblePaused)
        {
            mumbleAudioSource.Pause();
            isMumblePaused = true;
        }
    }

    private void ResumeMumbleAudio()
    {
        if (mumbleAudioSource != null && isMumblePaused)
        {
            mumbleAudioSource.UnPause();
            isMumblePaused = false;
        }
    }

    public string GetCurrentStepId()
    {
        if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            return steps[currentStepIndex].stepId;
        return "";
    }
    
    private void NotifyUISystemsOfStepChange()
    {
        
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnTutorialStepChanged();
        }
        
    }

    public bool WasTutorialSkippedByDev()
    {
        return wasTutorialSkippedByDev;
    }
    
    private void HandleMelonyClickDetection()
    {
        if (currentMelony == null) return;
        
        // Check for mouse click
        if (Input.GetMouseButtonDown(0))
        {
            // Check if we're not clicking on UI
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
                
            // Cast ray from camera through mouse position
            Camera cam = Camera.main;
            if (cam == null) return;
            
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                if (hit.transform.gameObject == currentMelony)
                {
                    OnMelonyClicked();
                }
            }
        }
    }
    
    public void ShowMelonyFeedback(string message)
    {
        // Temporarily update the dialogue text to show feedback
        if (dialogueText != null)
        {
            StartCoroutine(ShowTemporaryFeedback(message));
        }
    }
    
    private System.Collections.IEnumerator ShowTemporaryFeedback(string message)
    {
        // Store original text
        string originalText = dialogueText.text;
        
        // Show feedback message
        dialogueText.text = $"Melony says: \"{message}\"";
        
        // Wait for 3 seconds
        yield return new WaitForSeconds(3f);
        
        // Restore original text
        dialogueText.text = originalText;
    }
}

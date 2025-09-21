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

    private void Awake()
    {
        Debug.Log("TutorialManager: Awake called");
        if (instance == null)
        {
            instance = this;
            Debug.Log("TutorialManager: Instance set");
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
                Debug.Log("TutorialManager: Tutorial panel deactivated");
            }
            else
            {
                Debug.LogError("TutorialManager: tutorialPanel is not assigned in the Inspector!");
            }
            mumbleAudioSource = mumbleAudioSource ?? gameObject.AddComponent<AudioSource>();
            effectsAudioSource = gameObject.AddComponent<AudioSource>();
            effectsAudioSource.playOnAwake = false;
            Debug.Log("TutorialManager: Audio sources configured");
            InitializeTutorialSteps();
            Debug.Log("TutorialManager: Tutorial steps initialized");
            skipTutorialButton?.gameObject.SetActive(false); // Hide skip button at start
            SetupChecklist();
            Debug.Log("TutorialManager: Awake completed successfully");
        }
        else
        {
            Debug.Log("TutorialManager: Destroying duplicate instance");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        Debug.Log("TutorialManager: Starting tutorial...");
        StartTutorial();
    }

    private void Update()
    {
        HandleRequiredInputDetection();

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
        Debug.Log($"TutorialManager: StartTutorial called - Panel exists: {tutorialPanel != null}, Steps count: {steps.Count}");
        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
            Debug.Log("TutorialManager: Tutorial panel activated");
        }
        else
        {
            Debug.LogError("TutorialManager: Tutorial panel is null! Cannot start tutorial.");
            return;
        }
        
        if (steps.Count == 0)
        {
            Debug.LogError("TutorialManager: No tutorial steps found! Cannot start tutorial.");
            return;
        }
        
        currentStepIndex = -1;
        waitingForStepToComplete = false;
        Debug.Log($"TutorialManager: Starting with {steps.Count} steps, calling NextStep()");
        NextStep();
        
        // Notify UI systems that tutorial has started
        NotifyUISystemsOfStepChange();
    }

    void NextStep()
    {
        Debug.Log($"TutorialManager: NextStep called - currentStepIndex: {currentStepIndex}, steps.Count: {steps.Count}, isProcessingStep: {isProcessingStep}");
        
        if (isProcessingStep)
        {
            isProcessingStep = false;
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

            yield return new WaitForSecondsRealtime(0.1f);

            var step = steps[currentStepIndex];
            detectedInputs.Clear();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            typingCoroutine = StartCoroutine(TypeTextWithMumble(step.instructionText));
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
                    nextStepButton.onClick.AddListener(NextStep);
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

        // Prevent re-triggering for completed steps (fixes progress reset on movement)
        if (trigger != TutorialTrigger.None && IsStepCompletedForTrigger(trigger))
        {
            Debug.LogWarning($"Tutorial: Ignoring trigger {trigger} because the step is already completed.");
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
            TutorialTrigger.BuiltSilo => "build_silo",
            // Add other mappings here
            _ => null
        };
        return stepId != null && completedStepIds.Contains(stepId);
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

                NextStep();
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
        Debug.Log("EndTutorial called");
        tutorialPanel.SetActive(false);
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);
    
        currentStepIndex = steps.Count;
        waitingForStepToComplete = false;
        Debug.Log($"Tutorial ended: currentStepIndex={currentStepIndex}, waitingForStepToComplete={waitingForStepToComplete}");
        
        // Notify UI systems that tutorial has ended
        NotifyUISystemsOfStepChange();
    }
    
    public void SkipTutorial()
    {
        Debug.Log("SkipTutorial called");
        tutorialPanel.SetActive(false);
        if (nextStepButton != null)
            nextStepButton.gameObject.SetActive(false);
    
        CleanupAllWorldHighlights();
        CleanupShopHighlights();
    
        EndTutorial();
    }

    IEnumerator AutoAdvanceStep()
    {
        yield return new WaitForSecondsRealtime(10f);
        Trigger(TutorialTrigger.None);
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
            skipTutorialButton.gameObject.SetActive(true); // Show skip button again

        isShowingDiscovery = false;
        currentDiscoveryStep = null;
    }

    public bool IsTutorialActive()
    {
        // FIX: Tutorial should be active based on step progress, not panel visibility
        // The panel can be hidden temporarily but tutorial restrictions should remain
        bool active = currentStepIndex >= 0 && currentStepIndex < steps.Count && !IsTutorialCompleted();
        Debug.Log($"IsTutorialActive: currentStepIndex={currentStepIndex}, stepsCount={steps.Count}, completed={IsTutorialCompleted()}, result={active}");
        return active;
    }

    public List<string> GetCompletedStepIds() => new List<string>(completedStepIds);
    public int GetCurrentStepIndex() => currentStepIndex;
    public bool IsTutorialCompleted() => currentStepIndex >= steps.Count;

    public void SetTutorialProgress(List<string> completedSteps, int stepIndex, bool completed)
    {
        completedStepIds = completedSteps ?? new List<string>();
        currentStepIndex = stepIndex;
        if (completed)
            EndTutorial();
        else
            StartTutorialFromStep(stepIndex);
    }

    public void StartTutorialFromStep(int stepIndex)
    {
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

    private void OnDestroy()
    {
        // Clean up typing coroutine to prevent memory leaks
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
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
        Debug.Log($"TUTORIAL SYSTEM: Notifying UI systems of step change to '{GetCurrentStepId()}'");
        
        // Notify ShopUIManager to update shop button state
        if (ShopUIManager.Instance != null)
        {
            ShopUIManager.Instance.OnTutorialStepChanged();
        }
        
        // ShopPanelUI tutorial filtering is handled in PopulateShop method
        // No need for OnTutorialStepChanged notification
    }
}

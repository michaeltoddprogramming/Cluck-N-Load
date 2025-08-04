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

    // Add these variables to your TutorialManager class
    private Queue<TutorialTrigger> pendingTriggers = new Queue<TutorialTrigger>();
    private bool isProcessingStep = false;
    private float lastStepCompletionTime = 0f;
    private const float MIN_STEP_DURATION = 0.5f; // Minimum time between step transitions

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            tutorialPanel.SetActive(false);
            mumbleAudioSource = mumbleAudioSource ?? gameObject.AddComponent<AudioSource>();
            effectsAudioSource = gameObject.AddComponent<AudioSource>();
            effectsAudioSource.playOnAwake = false;
            InitializeTutorialSteps();
            skipTutorialButton?.onClick.AddListener(SkipTutorial);
            SetupChecklist();
        }
        else
            Destroy(gameObject);
    }

    private void Start()
    {
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

        // Add this new logic to process queued triggers
        if (!isProcessingStep && pendingTriggers.Count > 0)
        {
            TutorialTrigger nextTrigger = pendingTriggers.Dequeue();
            Debug.Log($"Tutorial: Processing queued trigger: {nextTrigger}");
            ProcessTrigger(nextTrigger);
        }
    }

    public void StartTutorial()
    {
        tutorialPanel.SetActive(true);
        currentStepIndex = -1;
        waitingForStepToComplete = false;
        NextStep();
    }

    // Replace your NextStep method with this simpler version
    void NextStep()
    {
        // Simple safety check to prevent being stuck
        if (isProcessingStep)
        {
            Debug.LogWarning("Tutorial: Force resetting stuck processing state");
            isProcessingStep = false;
        }

        isProcessingStep = true;
        StartCoroutine(ProcessNextStepSafely());
    }

    private IEnumerator ProcessNextStepSafely()
    {
        try
        {
            // Clean up current step's UI if there is one
            if (currentStepIndex >= 0 && currentStepIndex < steps.Count)
            {
                var currentStep = steps[currentStepIndex];
                if (currentStep.uiToHighlight != null)
                    HighlightUI(currentStep.uiToHighlight, false);
                ClearKeyIndicators();
                CleanupAllWorldHighlights();
                CleanupShopHighlights();
            }

            // Move to next step
            currentStepIndex++;
            if (currentStepIndex >= steps.Count)
            {
                EndTutorial();
                yield break;
            }

            // Brief delay for stability
            yield return new WaitForSecondsRealtime(0.1f);

            // Setup new step
            var step = steps[currentStepIndex];
            detectedInputs.Clear();

            if (typingCoroutine != null)
                StopCoroutine(typingCoroutine);

            // Set up new step content
            typingCoroutine = StartCoroutine(TypeTextWithMumble(step.instructionText));
            titleText.text = step.title;
            UpdateCharacterPortrait(step);

            // Highlight UI with a small delay for stability
            yield return new WaitForSeconds(0.05f);
            if (step.uiToHighlight != null)
                HighlightUI(step.uiToHighlight, true);

            ShowKeyIndicators(step.requiredInputs);

            if (step.triggerToWaitFor == TutorialTrigger.None)
                StartCoroutine(AutoAdvanceStep());

            if (step.onStepStart != null)
                step.onStepStart.Invoke();

            waitingForStepToComplete = true;
        }
        finally
        {
            // Always reset this flag when done, even if errors occur
            isProcessingStep = false;
        }
    }

    public void RegisterDiscoveryStep(TutorialStep step)
    {
        discoverySteps[step.triggerToWaitFor] = step;
    }

    public void Trigger(TutorialTrigger trigger)
    {
        // If already processing a step, queue this trigger
        if (isProcessingStep)
        {
            pendingTriggers.Enqueue(trigger);
            Debug.Log($"Tutorial: Queued trigger {trigger} for later processing");
            return;
        }

        ProcessTrigger(trigger);
    }

    // New method to process triggers
    private void ProcessTrigger(TutorialTrigger trigger)
    {
        Debug.Log($"Processing tutorial trigger: {trigger}");
        
        // First check if we're waiting for this specific trigger in the current step
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            if (step.triggerToWaitFor == trigger)
            {
                Debug.Log($"Trigger {trigger} matches current step - advancing");
                
                // Mark the step as not waiting anymore BEFORE calling NextStep
                waitingForStepToComplete = false;
                
                // Complete the step
                if (step.uiToHighlight != null)
                    HighlightUI(step.uiToHighlight, false);
                    
                if (step.onStepComplete != null)
                    step.onStepComplete.Invoke();
                    
                if (!string.IsNullOrEmpty(step.stepId))
                    MarkStepComplete(step.stepId);
                
                // Move to the next step
                NextStep();
                return;
            }
        }

        // Handle discovery steps
        if (discoverySteps.ContainsKey(trigger) && !shownDiscoveries.Contains(discoverySteps[trigger].stepId))
        {
            var discoveryStep = discoverySteps[trigger];
            ShowDiscoveryPopup(discoveryStep);
            shownDiscoveries.Add(discoveryStep.stepId);
        }
    }

    void EndTutorial()
    {
        tutorialPanel.SetActive(false);
    }

    void SkipTutorial()
    {
        tutorialPanel.SetActive(false);
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
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextWithMumble(discoveryStep.instructionText));
        titleText.text = discoveryStep.title;
        UpdateCharacterPortrait(discoveryStep);
        HighlightUI(discoveryStep.uiToHighlight, true);
        StartCoroutine(AutoCloseDiscovery(wasTutorialActive, wasChecklistActive));
    }

    private IEnumerator AutoCloseDiscovery(bool restoreTutorialState, bool restoreChecklistState)
    {
        yield return new WaitForSeconds(Mathf.Max(10f, currentDiscoveryStep.instructionText.Length * typeSpeed + 3f));
        HighlightUI(currentDiscoveryStep.uiToHighlight, false);
        tutorialPanel.SetActive(restoreTutorialState);
        if (checklistPanel != null)
            checklistPanel.SetActive(restoreChecklistState);
        isShowingDiscovery = false;
        currentDiscoveryStep = null;
    }

    public bool IsTutorialActive()
    {
        return tutorialPanel != null && tutorialPanel.activeSelf && currentStepIndex >= 0 && currentStepIndex < steps.Count;
    }

    public bool IsTutorialCompleted()
    {
        return currentStepIndex >= steps.Count;
    }
}
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
    }

    public void StartTutorial()
    {
        tutorialPanel.SetActive(true);
        currentStepIndex = -1;
        waitingForStepToComplete = false;
        NextStep();
    }

    void NextStep()
    {
        currentStepIndex++;
        if (currentStepIndex >= steps.Count)
        {
            EndTutorial();
            return;
        }

        var step = steps[currentStepIndex];
        detectedInputs.Clear();
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(TypeTextWithMumble(step.instructionText));
        titleText.text = step.title;
        UpdateCharacterPortrait(step);
        HighlightUI(step.uiToHighlight, true);
        ShowKeyIndicators(step.requiredInputs);
        if (step.triggerToWaitFor == TutorialTrigger.None)
            StartCoroutine(AutoAdvanceStep());
        step.onStepStart?.Invoke();
        waitingForStepToComplete = true;
    }

    public void RegisterDiscoveryStep(TutorialStep step)
    {
        discoverySteps[step.triggerToWaitFor] = step;
    }

    public void Trigger(TutorialTrigger trigger)
    {
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            if (step.triggerToWaitFor == trigger)
            {
                HighlightUI(step.uiToHighlight, false);
                step.onStepComplete?.Invoke();
                if (!string.IsNullOrEmpty(step.stepId))
                    MarkStepComplete(step.stepId);
                waitingForStepToComplete = false;
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
        yield return new WaitForSeconds(10f);
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
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

    [Header("Audio")]
    public AudioSource mumbleAudioSource;
    public AudioClip[] mumbleClips;
    public float typeSpeed = 0.04f;
    public AudioClip keyPressSound;

    [Header("Tutorial Steps")]
    public List<TutorialStep> steps = new();

    private int currentStepIndex = -1;
    private bool waitingForStepToComplete = false;
    private Coroutine typingCoroutine;

    private static TutorialManager instance;
    public static TutorialManager Instance => instance;

    private HashSet<KeyCode> detectedInputs = new();
    private List<GameObject> keyIndicators = new();
    private Dictionary<KeyCode, GameObject> keyIndicatorMap = new Dictionary<KeyCode, GameObject>();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            tutorialPanel.SetActive(false);

            if (mumbleAudioSource == null)
                mumbleAudioSource = gameObject.AddComponent<AudioSource>();

            InitializeTutorialSteps();
            skipTutorialButton?.onClick.AddListener(SkipTutorial);
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
        HandleRequiredInputDetection();
 
        if (waitingForStepToComplete && currentStepIndex >= 0 && currentStepIndex < steps.Count)
        {
            var step = steps[currentStepIndex];
            float scrollDelta = Input.GetAxis("Mouse ScrollWheel");
            
            if (scrollDelta > 0 && step.requiredInputs.Contains(KeyCode.Mouse3))
            {
                detectedInputs.Add(KeyCode.Mouse3);
                UpdateKeyIndicatorVisual(KeyCode.Mouse3, true);
                Debug.Log("Detected Mouse Wheel Up");
            }
            else if (scrollDelta < 0 && step.requiredInputs.Contains(KeyCode.Mouse4))
            {
                detectedInputs.Add(KeyCode.Mouse4);
                UpdateKeyIndicatorVisual(KeyCode.Mouse4, true);
                Debug.Log("Detected Mouse Wheel Down");
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

    public void Trigger(TutorialTrigger trigger)
    {
        if (!waitingForStepToComplete)
            return;

        var step = steps[currentStepIndex];
        if (step.triggerToWaitFor == trigger)
        {
            HighlightUI(step.uiToHighlight, false);
            step.onStepComplete?.Invoke();
            waitingForStepToComplete = false;
            NextStep();
        }
    }

    void EndTutorial()
    {
        tutorialPanel.SetActive(false);
        Debug.Log("Tutorial finished");
    }

    void SkipTutorial()
    {
        tutorialPanel.SetActive(false);
        Debug.Log("Tutorial skipped");
    }

    IEnumerator AutoAdvanceStep()
    {
        yield return new WaitForSeconds(2f);
        Trigger(TutorialTrigger.None);
    }
}
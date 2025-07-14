using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private TextMeshProUGUI tutorialText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Image highlightOverlay;
    [SerializeField] private Canvas tutorialCanvas;

    [Header("Tutorial Settings")]
    [SerializeField] private bool startTutorialOnPlay = true;
    [SerializeField] private float highlightPulseSpeed = 2f;
    [SerializeField] private Color highlightColor = Color.yellow;

    private Queue<TutorialStep> tutorialSteps = new Queue<TutorialStep>();
    private TutorialStep currentStep;
    private bool isActive = false;
    private Coroutine highlightCoroutine;
    private GameObject currentHighlightTarget;

    private void Awake()
    {
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

        InitializeTutorial();
    }

    private void Start()
    {
        if (startTutorialOnPlay && !HasCompletedTutorial())
        {
            StartCoroutine(DelayedTutorialStart());
        }
    }

    private IEnumerator DelayedTutorialStart()
    {
        yield return new WaitForSeconds(1f); // Give other systems time to initialize
        StartTutorial();
    }

    private void InitializeTutorial()
    {
        // Setup tutorial steps
        SetupTutorialSteps();

        // Setup UI
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        if (nextButton != null)
            nextButton.onClick.AddListener(NextStep);

        if (skipButton != null)
            skipButton.onClick.AddListener(SkipTutorial);

        if (tutorialCanvas != null)
            tutorialCanvas.sortingOrder = 100; // Ensure tutorial appears on top
    }

    private void SetupTutorialSteps()
    {
        // Clear any existing steps
        tutorialSteps.Clear();

        // Step 1: Introduction
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Welcome to Cluck-N-Load! This is your farm defense game. Let's learn the basics!",
            highlightTarget = null,
            waitForCondition = TutorialCondition.None,
            unlockFeature = "tutorial_intro"
        });

        // Step 2: Camera movement
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Use WASD keys or arrow keys to move the camera around your farm.",
            highlightTarget = null,
            waitForCondition = TutorialCondition.CameraMoved,
            unlockFeature = "camera_movement"
        });

        // Step 3: Shop introduction
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Click the Shop button to open the building menu. You can only build during the day!",
            highlightTarget = "ShopButton",
            waitForCondition = TutorialCondition.ShopOpened,
            unlockFeature = "shop_access"
        });

        // Step 4: First building
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Try placing your first structure! Select a crop or animal building from the shop.",
            highlightTarget = null,
            waitForCondition = TutorialCondition.FirstStructurePlaced,
            unlockFeature = "basic_building"
        });

        // Step 5: Resource management
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Watch your money in the top corner. Buildings cost money, but they generate resources!",
            highlightTarget = "MoneyUI",
            waitForCondition = TutorialCondition.None,
            unlockFeature = "resource_awareness"
        });

        // Step 6: Day/Night cycle
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Notice the day/night cycle. During the day you build - at night, wolves attack!",
            highlightTarget = "TimeUI",
            waitForCondition = TutorialCondition.NightStarted,
            unlockFeature = "day_night_cycle"
        });

        // Step 7: Defense
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Wolves will attack your structures! Build barracks to defend your farm.",
            highlightTarget = null,
            waitForCondition = TutorialCondition.BarracksPlaced,
            unlockFeature = "defense_system"
        });

        // Step 8: Tutorial complete
        tutorialSteps.Enqueue(new TutorialStep
        {
            text = "Great! You've learned the basics. Build, defend, and survive as long as you can!",
            highlightTarget = null,
            waitForCondition = TutorialCondition.None,
            unlockFeature = "tutorial_complete"
        });
    }

    public void StartTutorial()
    {
        if (isActive) return;

        isActive = true;
        if (tutorialPanel != null)
            tutorialPanel.SetActive(true);

        ShowNextStep();
    }

    public void NextStep()
    {
        if (currentStep != null && currentStep.waitForCondition != TutorialCondition.None)
        {
            // Don't advance if waiting for a specific condition
            return;
        }

        ShowNextStep();
    }

    private void ShowNextStep()
    {
        if (tutorialSteps.Count == 0)
        {
            CompleteTutorial();
            return;
        }

        currentStep = tutorialSteps.Dequeue();
        
        // Update text
        if (tutorialText != null)
            tutorialText.text = currentStep.text;

        // Handle highlighting
        if (!string.IsNullOrEmpty(currentStep.highlightTarget))
        {
            HighlightTarget(currentStep.highlightTarget);
        }
        else
        {
            ClearHighlight();
        }

        // Unlock feature
        if (!string.IsNullOrEmpty(currentStep.unlockFeature))
        {
            FeatureUnlockManager.Instance?.UnlockFeature(currentStep.unlockFeature);
        }

        // Update button visibility
        if (nextButton != null)
        {
            nextButton.gameObject.SetActive(currentStep.waitForCondition == TutorialCondition.None);
        }
    }

    private void HighlightTarget(string targetName)
    {
        ClearHighlight();

        GameObject target = GameObject.Find(targetName);
        if (target == null)
        {
            // Try finding by tag
            target = GameObject.FindGameObjectWithTag(targetName);
        }

        if (target != null)
        {
            currentHighlightTarget = target;
            highlightCoroutine = StartCoroutine(HighlightEffect(target));
        }
    }

    private IEnumerator HighlightEffect(GameObject target)
    {
        if (highlightOverlay == null) yield break;

        // Position highlight overlay over target
        RectTransform targetRect = target.GetComponent<RectTransform>();
        if (targetRect != null)
        {
            highlightOverlay.rectTransform.position = targetRect.position;
            highlightOverlay.rectTransform.sizeDelta = targetRect.sizeDelta * 1.2f;
        }

        highlightOverlay.gameObject.SetActive(true);

        while (currentHighlightTarget == target)
        {
            float alpha = Mathf.PingPong(Time.time * highlightPulseSpeed, 0.5f) + 0.3f;
            Color color = highlightColor;
            color.a = alpha;
            highlightOverlay.color = color;
            yield return null;
        }
    }

    private void ClearHighlight()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
            highlightCoroutine = null;
        }

        if (highlightOverlay != null)
            highlightOverlay.gameObject.SetActive(false);

        currentHighlightTarget = null;
    }

    public void OnConditionMet(TutorialCondition condition)
    {
        if (!isActive || currentStep == null) return;

        if (currentStep.waitForCondition == condition)
        {
            ShowNextStep();
        }
    }

    public void SkipTutorial()
    {
        CompleteTutorial();
        MarkTutorialAsCompleted();
    }

    private void CompleteTutorial()
    {
        isActive = false;
        ClearHighlight();
        
        if (tutorialPanel != null)
            tutorialPanel.SetActive(false);

        // Unlock all basic features
        FeatureUnlockManager.Instance?.UnlockAllBasicFeatures();
        
        MarkTutorialAsCompleted();
    }

    private void MarkTutorialAsCompleted()
    {
        PlayerPrefs.SetInt("TutorialCompleted", 1);
        PlayerPrefs.Save();
    }

    private bool HasCompletedTutorial()
    {
        return PlayerPrefs.GetInt("TutorialCompleted", 0) == 1;
    }

    public bool IsActive => isActive;

    private void OnDestroy()
    {
        if (highlightCoroutine != null)
        {
            StopCoroutine(highlightCoroutine);
        }
    }
}

[System.Serializable]
public class TutorialStep
{
    public string text;
    public string highlightTarget;
    public TutorialCondition waitForCondition;
    public string unlockFeature;
}

public enum TutorialCondition
{
    None,
    CameraMoved,
    ShopOpened,
    FirstStructurePlaced,
    NightStarted,
    BarracksPlaced,
    DefenseSuccessful
}

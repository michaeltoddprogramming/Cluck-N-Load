using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject tutorialPanel;
    public TextMeshProUGUI dialogueText;
    public TextMeshProUGUI titleText;                  
    public Image characterPortraitImage;      
    public GameObject startButton;            
    public Button skipTutorialButton;


    [Header("Steps")]
    public List<TutorialStep> steps = new List<TutorialStep>();

    private int currentStepIndex = -1;
    private bool waitingForStepToComplete = false;

    private static TutorialManager instance;
    public static TutorialManager Instance => instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            tutorialPanel.SetActive(false);

            // Assign skip button logic
            if (skipTutorialButton != null)
                skipTutorialButton.onClick.AddListener(SkipTutorial);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void StartTutorial()
    {
        startButton.SetActive(false);
        tutorialPanel.SetActive(true);
        currentStepIndex = -1;
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

        // Update dialogue and title
        dialogueText.text = step.instructionText;
        if (titleText != null)
            titleText.text = step.title;

        // Update character portrait
        if (characterPortraitImage != null)
        {
            if (step.characterSprite != null)
            {
                characterPortraitImage.sprite = step.characterSprite;
                characterPortraitImage.gameObject.SetActive(true);
            }
            else
            {
                characterPortraitImage.gameObject.SetActive(false);
            }
        }

        // Highlight UI
        if (step.uiToHighlight != null)
            HighlightUI(step.uiToHighlight, true);

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
            if (step.uiToHighlight != null)
                HighlightUI(step.uiToHighlight, false);

            step.onStepComplete?.Invoke();
            waitingForStepToComplete = false;

            NextStep();
        }
    }
    
    void HighlightUI(GameObject target, bool enable)
    {
        Outline outline = target.GetComponent<Outline>();
        if (outline == null && enable)
        {
            outline = target.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(5, 5);
        }
    
        if (outline != null)
        {
            outline.enabled = enable;
            if (enable)
            {
                // Animate outline thickness and color using LeanTween and a coroutine
                StopCoroutine("AnimateOutline");
                StartCoroutine(AnimateOutline(outline));
            }
            else
            {
                StopCoroutine("AnimateOutline");
                outline.effectDistance = new Vector2(5, 5);
                outline.effectColor = Color.yellow;
            }
        }
    }

    
    
private System.Collections.IEnumerator AnimateOutline(Outline outline)
{
    float time = 0f;
    float duration = 0.5f;
    Vector2 start = new Vector2(5, 5);
    Vector2 end = new Vector2(10, 10);
    Color startColor = Color.yellow;
    Color endColor = Color.cyan;

    while (outline.enabled)
    {
        time += Time.unscaledDeltaTime;
        float t = Mathf.PingPong(time / duration, 1f);
        outline.effectDistance = Vector2.Lerp(start, end, t);
        outline.effectColor = Color.Lerp(startColor, endColor, t);
        yield return null;
    }
}

    void SkipTutorial()
    {
        tutorialPanel.SetActive(false);
        Debug.Log("Tutorial skipped");
    }

    void EndTutorial()
    {
        tutorialPanel.SetActive(false);
        Debug.Log("Tutorial finished");
    }
}

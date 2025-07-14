using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Script for the main tutorial UI prefab.
/// Attach this to your tutorial UI prefab to auto-configure components.
/// </summary>
public class TutorialUIPrefab : MonoBehaviour
{
    [Header("UI References - Auto-assigned")]
    public Image characterPortrait;
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Button skipButton;
    public Button skipAllButton;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Image backgroundPanel;
    public CanvasGroup canvasGroup;

    [Header("Animation")]
    public Animator uiAnimator;
    
    private void Awake()
    {
        // Auto-assign components if not already set
        AutoAssignComponents();
    }

    private void AutoAssignComponents()
    {
        if (characterPortrait == null)
            characterPortrait = transform.Find("CharacterPortrait")?.GetComponent<Image>();
            
        if (characterNameText == null)
            characterNameText = transform.Find("CharacterName")?.GetComponent<TextMeshProUGUI>();
            
        if (dialogueText == null)
            dialogueText = transform.Find("DialogueText")?.GetComponent<TextMeshProUGUI>();
            
        if (nextButton == null)
            nextButton = transform.Find("NextButton")?.GetComponent<Button>();
            
        if (skipButton == null)
            skipButton = transform.Find("SkipButton")?.GetComponent<Button>();
            
        if (skipAllButton == null)
            skipAllButton = transform.Find("SkipAllButton")?.GetComponent<Button>();
            
        if (progressSlider == null)
            progressSlider = transform.Find("ProgressSlider")?.GetComponent<Slider>();
            
        if (progressText == null)
            progressText = transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            
        if (backgroundPanel == null)
            backgroundPanel = GetComponent<Image>();
            
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
            
        if (uiAnimator == null)
            uiAnimator = GetComponent<Animator>();
    }

    public void ConfigureForTutorial()
    {
        // Setup default values
        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
    }
}

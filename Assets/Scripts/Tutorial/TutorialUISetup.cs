using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TutorialUITheme
{
    [Header("Colors")]
    public Color backgroundColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
    public Color titleColor = Color.white;
    public Color textColor = new Color(0.9f, 0.9f, 0.9f, 1f);
    public Color buttonColor = new Color(0.3f, 0.6f, 1f, 1f);
    public Color buttonHoverColor = new Color(0.4f, 0.7f, 1f, 1f);
    public Color accentColor = new Color(1f, 0.8f, 0.2f, 1f);
    
    [Header("Fonts")]
    public TMP_FontAsset titleFont;
    public TMP_FontAsset bodyFont;
    public TMP_FontAsset buttonFont;
    
    [Header("Sizes")]
    public float titleFontSize = 28f;
    public float bodyFontSize = 18f;
    public float buttonFontSize = 16f;
    
    [Header("Spacing")]
    public float panelPadding = 20f;
    public float elementSpacing = 15f;
    public float buttonSpacing = 10f;
}

public class TutorialUISetup : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject tutorialPanel;
    [SerializeField] private Image panelBackground;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI bodyText;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button skipButton;
    [SerializeField] private Image characterPortrait;
    [SerializeField] private RectTransform contentArea;
    
    [Header("Theme")]
    [SerializeField] private TutorialUITheme uiTheme;
    
    [Header("Animation Settings")]
    [SerializeField] private bool enableAnimations = true;
    [SerializeField] private float fadeInDuration = 0.5f;
    [SerializeField] private float slideInDistance = 100f;
    [SerializeField] private AnimationCurve fadeInCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Layout Settings")]
    [SerializeField] private bool autoSize = true;
    [SerializeField] private Vector2 minPanelSize = new Vector2(400, 200);
    [SerializeField] private Vector2 maxPanelSize = new Vector2(800, 600);
    [SerializeField] private bool followTarget = false;
    [SerializeField] private Transform followTransform;
    [SerializeField] private Vector3 followOffset = new Vector3(0, 100, 0);
    
    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip buttonClickSound;
    [SerializeField] private AudioSource audioSource;
    
    private CanvasGroup canvasGroup;
    private Vector3 originalPosition;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeUI();
    }

    private void InitializeUI()
    {
        if (isInitialized) return;

        // Setup canvas group for animations
        if (canvasGroup == null)
        {
            canvasGroup = tutorialPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = tutorialPanel.AddComponent<CanvasGroup>();
            }
        }

        // Store original position
        if (tutorialPanel != null)
        {
            originalPosition = tutorialPanel.transform.position;
        }

        // Apply theme
        ApplyTheme();

        // Setup audio
        SetupAudio();

        // Setup button events
        SetupButtons();

        isInitialized = true;
    }

    private void ApplyTheme()
    {
        if (uiTheme == null) return;

        // Apply colors
        if (panelBackground != null)
        {
            panelBackground.color = uiTheme.backgroundColor;
        }

        if (titleText != null)
        {
            titleText.color = uiTheme.titleColor;
            titleText.fontSize = uiTheme.titleFontSize;
            if (uiTheme.titleFont != null)
            {
                titleText.font = uiTheme.titleFont;
            }
        }

        if (bodyText != null)
        {
            bodyText.color = uiTheme.textColor;
            bodyText.fontSize = uiTheme.bodyFontSize;
            if (uiTheme.bodyFont != null)
            {
                bodyText.font = uiTheme.bodyFont;
            }
        }

        // Style buttons
        StyleButton(nextButton, "Next");
        StyleButton(skipButton, "Skip Tutorial");
    }

    private void StyleButton(Button button, string defaultText)
    {
        if (button == null) return;

        // Apply button colors
        ColorBlock colors = button.colors;
        colors.normalColor = uiTheme.buttonColor;
        colors.highlightedColor = uiTheme.buttonHoverColor;
        colors.pressedColor = uiTheme.buttonColor * 0.8f;
        colors.selectedColor = uiTheme.buttonHoverColor;
        button.colors = colors;

        // Style button text
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            buttonText.fontSize = uiTheme.buttonFontSize;
            if (uiTheme.buttonFont != null)
            {
                buttonText.font = uiTheme.buttonFont;
            }
            
            if (string.IsNullOrEmpty(buttonText.text))
            {
                buttonText.text = defaultText;
            }
        }
    }

    private void SetupAudio()
    {
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.volume = 0.7f;
    }

    private void SetupButtons()
    {
        if (nextButton != null)
        {
            nextButton.onClick.AddListener(() => {
                PlayButtonSound();
                TutorialManager.Instance?.NextTutorialStep();
            });
        }

        if (skipButton != null)
        {
            skipButton.onClick.AddListener(() => {
                PlayButtonSound();
                TutorialManager.Instance?.SkipTutorial();
            });
        }
    }

    public void ShowPanel(string title, string body, Sprite portrait = null)
    {
        if (!isInitialized)
        {
            InitializeUI();
        }

        // Set content
        if (titleText != null)
        {
            titleText.text = title;
        }

        if (bodyText != null)
        {
            bodyText.text = body;
        }

        if (characterPortrait != null && portrait != null)
        {
            characterPortrait.sprite = portrait;
            characterPortrait.gameObject.SetActive(true);
        }
        else if (characterPortrait != null)
        {
            characterPortrait.gameObject.SetActive(false);
        }

        // Auto-size panel if enabled
        if (autoSize)
        {
            AutoSizePanel();
        }

        // Show with animation
        if (enableAnimations)
        {
            StartCoroutine(AnimateShow());
        }
        else
        {
            tutorialPanel.SetActive(true);
            canvasGroup.alpha = 1f;
        }

        // Play open sound
        if (openSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    public void HidePanel()
    {
        if (enableAnimations)
        {
            StartCoroutine(AnimateHide());
        }
        else
        {
            tutorialPanel.SetActive(false);
        }

        // Play close sound
        if (closeSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(closeSound);
        }
    }

    private void AutoSizePanel()
    {
        if (contentArea == null) return;

        // Calculate required size based on content
        float requiredHeight = uiTheme.panelPadding * 2;

        if (titleText != null && titleText.gameObject.activeInHierarchy)
        {
            requiredHeight += titleText.preferredHeight + uiTheme.elementSpacing;
        }

        if (bodyText != null && bodyText.gameObject.activeInHierarchy)
        {
            requiredHeight += bodyText.preferredHeight + uiTheme.elementSpacing;
        }

        if (nextButton != null && nextButton.gameObject.activeInHierarchy)
        {
            requiredHeight += nextButton.GetComponent<RectTransform>().sizeDelta.y + uiTheme.buttonSpacing;
        }

        // Clamp to min/max sizes
        Vector2 newSize = contentArea.sizeDelta;
        newSize.y = Mathf.Clamp(requiredHeight, minPanelSize.y, maxPanelSize.y);
        newSize.x = Mathf.Clamp(newSize.x, minPanelSize.x, maxPanelSize.x);

        contentArea.sizeDelta = newSize;
    }

    private System.Collections.IEnumerator AnimateShow()
    {
        tutorialPanel.SetActive(true);
        
        // Start with panel off-screen and transparent
        canvasGroup.alpha = 0f;
        Vector3 startPos = originalPosition + Vector3.up * slideInDistance;
        tutorialPanel.transform.position = startPos;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / fadeInDuration;
            float curveValue = fadeInCurve.Evaluate(progress);

            canvasGroup.alpha = curveValue;
            tutorialPanel.transform.position = Vector3.Lerp(startPos, originalPosition, curveValue);

            yield return null;
        }

        canvasGroup.alpha = 1f;
        tutorialPanel.transform.position = originalPosition;
    }

    private System.Collections.IEnumerator AnimateHide()
    {
        Vector3 endPos = originalPosition + Vector3.up * slideInDistance;

        float elapsed = 0f;
        while (elapsed < fadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / fadeInDuration;
            float curveValue = fadeInCurve.Evaluate(1f - progress);

            canvasGroup.alpha = curveValue;
            tutorialPanel.transform.position = Vector3.Lerp(originalPosition, endPos, progress);

            yield return null;
        }

        tutorialPanel.SetActive(false);
        tutorialPanel.transform.position = originalPosition;
    }

    private void Update()
    {
        if (followTarget && followTransform != null && tutorialPanel.activeInHierarchy)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(followTransform.position + followOffset);
            tutorialPanel.transform.position = screenPos;
        }
    }

    private void PlayButtonSound()
    {
        if (buttonClickSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(buttonClickSound);
        }
    }

    public void SetFollowTarget(Transform target, Vector3 offset)
    {
        followTransform = target;
        followOffset = offset;
        followTarget = target != null;
    }

    public void ClearFollowTarget()
    {
        followTarget = false;
        followTransform = null;
    }
}

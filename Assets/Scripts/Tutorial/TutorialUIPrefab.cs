using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Script for the main tutorial UI prefab.
/// Attach this to your tutorial UI prefab to auto-configure components.
/// </summary>
public class TutorialUIPrefab : MonoBehaviour
{
    [Header("Talking Audio")]
    public AudioSource mumbleSource;
    public AudioClip[] mumbleClips;
    public float typeSpeed = 0.04f;

    private Coroutine typingCoroutine;
    private Coroutine mumbleCoroutine;

    /// <summary>
    /// Animate the tutorial panel in with a pop/fade effect using LeanTween
    /// </summary>
    public void AnimatePanelIn()
    {
        // Only animate the background panel, never touch the root panel's CanvasGroup or scale
        if (backgroundPanel != null)
        {
            // Ensure CanvasGroup is present on the background
            var bgGroup = backgroundPanel.GetComponent<CanvasGroup>();
            if (bgGroup == null)
                bgGroup = backgroundPanel.gameObject.AddComponent<CanvasGroup>();

            // Reset scale and alpha for background only
            backgroundPanel.transform.localScale = Vector3.one * 0.7f;
            bgGroup.alpha = 0f;
            bgGroup.interactable = false;
            bgGroup.blocksRaycasts = false;

            // Animate scale (pop) and fade in for background only
            LeanTween.scale(backgroundPanel.gameObject, Vector3.one, 0.35f)
                .setEase(LeanTweenType.easeOutBack)
                .setIgnoreTimeScale(true);
            LeanTween.value(backgroundPanel.gameObject, 0f, 1f, 0.28f)
                .setOnUpdate((float val) => { bgGroup.alpha = val; })
                .setOnComplete(() => {
                    bgGroup.interactable = true;
                    bgGroup.blocksRaycasts = true;
                })
                .setIgnoreTimeScale(true);
        }
    }

    /// <summary>
    /// Starts typewriter effect with mumble SFX
    /// </summary>
    public void PlayTypingWithMumble(string line)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        if (mumbleCoroutine != null) StopCoroutine(mumbleCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line));
        mumbleCoroutine = StartCoroutine(PlayMumbling());
    }

    private IEnumerator TypeText(string line)
    {
        if (dialogueText == null) yield break;
        dialogueText.text = "";
        foreach (char c in line)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }

        // Stop mumbling after text is done
        if (mumbleCoroutine != null) StopCoroutine(mumbleCoroutine);
        if (mumbleSource != null && mumbleSource.isPlaying) mumbleSource.Stop();
    }

    private IEnumerator PlayMumbling()
    {
        if (mumbleClips == null || mumbleClips.Length == 0 || mumbleSource == null) yield break;
        while (true)
        {
            if (!mumbleSource.isPlaying)
            {
                mumbleSource.clip = mumbleClips[Random.Range(0, mumbleClips.Length)];
                mumbleSource.pitch = Random.Range(0.92f, 1.08f); // Randomize pitch
                mumbleSource.volume = Random.Range(0.55f, 0.95f); // Randomize volume
                mumbleSource.Play();
            }
            // Wait a more random interval (sometimes quick, sometimes longer)
            yield return new WaitForSecondsRealtime(Random.Range(0.13f, 0.44f));
        }
    }
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

    [Header("3D Model Portrait")]
    public RawImage characterPortraitRawImage;
    public RenderTexture characterPortraitRenderTexture;
    public GameObject characterModelPrefab; // Assign in Inspector
    public Camera portraitCamera; // Assign in Inspector

    private GameObject portraitModelInstance;

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

        // Also check for SkipTutorialButton as that seems to be in your hierarchy
        if (skipButton == null)
            skipButton = transform.Find("SkipTutorialButton")?.GetComponent<Button>();

        if (skipAllButton == null)
            skipAllButton = transform.Find("SkipAllButton")?.GetComponent<Button>();

        // Also check for SkipTutorialButton for skipAllButton
        if (skipAllButton == null)
            skipAllButton = transform.Find("SkipTutorialButton")?.GetComponent<Button>();

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

    private void Start()
    {
        // If portraitCamera is not assigned, find it by name or tag
        if (portraitCamera == null)
        {
            portraitCamera = GameObject.Find("PortraitCamera")?.GetComponent<Camera>();
            // Or, if you set a tag: portraitCamera = GameObject.FindWithTag("PortraitCamera")?.GetComponent<Camera>();
        }
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

    // Call this to set up the portrait
    public void SetupCharacterPortrait()
    {
        if (characterPortraitRawImage != null && characterPortraitRenderTexture != null)
        {
            characterPortraitRawImage.texture = characterPortraitRenderTexture;
        }
    }

    public void ShowPortraitModel()
    {
        // Destroy previous instance if it exists
        if (portraitModelInstance != null)
            Destroy(portraitModelInstance);

        if (characterModelPrefab == null || portraitCamera == null)
            return;

        // Instantiate the model as a child of the camera for easy positioning
        portraitModelInstance = Instantiate(characterModelPrefab, portraitCamera.transform);

        // Set the layer for the model and all its children
        int portraitLayer = LayerMask.NameToLayer("PortraitLayer");
        portraitModelInstance.layer = portraitLayer;
        foreach (Transform t in portraitModelInstance.GetComponentsInChildren<Transform>())
            t.gameObject.layer = portraitLayer;

        // Position, rotate, and scale as needed (adjust these values for your model)
        portraitModelInstance.transform.localPosition = new Vector3(0, 0, 2); // In front of camera
        portraitModelInstance.transform.localRotation = Quaternion.identity;
        portraitModelInstance.transform.localScale = Vector3.one;
    }
}


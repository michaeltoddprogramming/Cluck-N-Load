using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Enhanced tutorial UI script that supports both 2D portrait and 3D character model
/// </summary>
public class TutorialUI3DCharacter : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Button nextButton;
    public Button skipButton;
    public Button skipAllButton;
    public Slider progressSlider;
    public TextMeshProUGUI progressText;
    public Image backgroundPanel;
    public CanvasGroup canvasGroup;

    [Header("Character Display")]
    public bool use3DCharacter = false;
    
    [Header("2D Portrait (Legacy)")]
    public Image characterPortrait2D;
    
    [Header("3D Character Setup")]
    public RawImage character3DDisplay;
    public Camera character3DCamera;
    public GameObject character3DModel;
    public Transform character3DCameraPosition;
    
    [Header("3D Character Animation")]
    public Animator character3DAnimator;
    public string talkingTrigger = "StartTalking";
    public string idleTrigger = "StopTalking";
    public string greetingTrigger = "Greeting";
    public string farewellTrigger = "Farewell";
    
    [Header("3D Character Rendering")]
    public RenderTexture characterRenderTexture;
    public int renderTextureWidth = 512;
    public int renderTextureHeight = 512;
    
    [Header("Animation Settings")]
    public bool animateWhileTalking = true;
    public float talkingAnimationSpeed = 1f;

    private bool isCurrentlyTalking = false;

    private void Awake()
    {
        SetupCharacterDisplay();
    }

    private void SetupCharacterDisplay()
    {
        if (use3DCharacter)
        {
            Setup3DCharacter();
        }
        else
        {
            Setup2DPortrait();
        }
    }

    private void Setup2DPortrait()
    {
        // Hide 3D elements
        if (character3DDisplay != null)
            character3DDisplay.gameObject.SetActive(false);
        if (character3DCamera != null)
            character3DCamera.gameObject.SetActive(false);
        if (character3DModel != null)
            character3DModel.SetActive(false);

        // Show 2D portrait
        if (characterPortrait2D != null)
            characterPortrait2D.gameObject.SetActive(true);
    }

    private void Setup3DCharacter()
    {
        // Hide 2D portrait
        if (characterPortrait2D != null)
            characterPortrait2D.gameObject.SetActive(false);

        // Setup 3D character display
        if (character3DDisplay != null)
            character3DDisplay.gameObject.SetActive(true);

        // Create render texture if needed
        if (characterRenderTexture == null)
        {
            characterRenderTexture = new RenderTexture(renderTextureWidth, renderTextureHeight, 16);
            characterRenderTexture.name = "TutorialCharacterRT";
        }

        // Setup camera
        if (character3DCamera != null)
        {
            character3DCamera.gameObject.SetActive(true);
            character3DCamera.targetTexture = characterRenderTexture;
            character3DCamera.cullingMask = LayerMask.GetMask("TutorialCharacter"); // Use dedicated layer
            
            // Position camera if transform is provided
            if (character3DCameraPosition != null)
            {
                character3DCamera.transform.position = character3DCameraPosition.position;
                character3DCamera.transform.rotation = character3DCameraPosition.rotation;
            }
        }

        // Setup 3D model
        if (character3DModel != null)
        {
            character3DModel.SetActive(true);
            // Set to TutorialCharacter layer
            SetLayerRecursively(character3DModel, LayerMask.NameToLayer("TutorialCharacter"));
        }

        // Assign render texture to UI
        if (character3DDisplay != null && characterRenderTexture != null)
        {
            character3DDisplay.texture = characterRenderTexture;
        }

        // Get animator reference
        if (character3DAnimator == null && character3DModel != null)
        {
            character3DAnimator = character3DModel.GetComponent<Animator>();
        }
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    public void StartDialogue(string dialogue)
    {
        if (dialogueText != null)
        {
            dialogueText.text = dialogue;
        }

        if (use3DCharacter && character3DAnimator != null)
        {
            StartTalkingAnimation();
        }
    }

    public void EndDialogue()
    {
        if (use3DCharacter && character3DAnimator != null)
        {
            StopTalkingAnimation();
        }
    }

    private void StartTalkingAnimation()
    {
        if (!isCurrentlyTalking && character3DAnimator != null)
        {
            isCurrentlyTalking = true;
            
            if (!string.IsNullOrEmpty(talkingTrigger))
            {
                character3DAnimator.SetTrigger(talkingTrigger);
            }
            
            // Set talking speed
            if (animateWhileTalking)
            {
                character3DAnimator.speed = talkingAnimationSpeed;
            }
        }
    }

    private void StopTalkingAnimation()
    {
        if (isCurrentlyTalking && character3DAnimator != null)
        {
            isCurrentlyTalking = false;
            
            if (!string.IsNullOrEmpty(idleTrigger))
            {
                character3DAnimator.SetTrigger(idleTrigger);
            }
            
            // Reset speed
            character3DAnimator.speed = 1f;
        }
    }

    public void PlayGreetingAnimation()
    {
        if (use3DCharacter && character3DAnimator != null && !string.IsNullOrEmpty(greetingTrigger))
        {
            character3DAnimator.SetTrigger(greetingTrigger);
        }
    }

    public void PlayFarewellAnimation()
    {
        if (use3DCharacter && character3DAnimator != null && !string.IsNullOrEmpty(farewellTrigger))
        {
            character3DAnimator.SetTrigger(farewellTrigger);
        }
    }

    public void SetCharacterEmotion(string emotionTrigger)
    {
        if (use3DCharacter && character3DAnimator != null && !string.IsNullOrEmpty(emotionTrigger))
        {
            character3DAnimator.SetTrigger(emotionTrigger);
        }
    }

    // Method to switch between 2D and 3D at runtime
    public void SwitchTo3D(GameObject characterModel, Camera renderCamera)
    {
        character3DModel = characterModel;
        character3DCamera = renderCamera;
        use3DCharacter = true;
        SetupCharacterDisplay();
    }

    public void SwitchTo2D(Sprite portraitSprite)
    {
        if (characterPortrait2D != null)
        {
            characterPortrait2D.sprite = portraitSprite;
        }
        use3DCharacter = false;
        SetupCharacterDisplay();
    }

    private void OnDestroy()
    {
        // Cleanup render texture
        if (characterRenderTexture != null)
        {
            characterRenderTexture.Release();
        }
    }
}

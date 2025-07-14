using UnityEngine;

/// <summary>
/// Setup helper for converting tutorial UI from 2D portrait to 3D character model
/// </summary>
[System.Serializable]
public class Tutorial3DCharacterSetup : MonoBehaviour
{
    [Header("3D Character Assets")]
    [SerializeField] private GameObject characterModelPrefab;
    [SerializeField] private Transform characterSpawnPoint;
    [SerializeField] private Transform cameraPosition;
    
    [Header("Render Settings")]
    [SerializeField] private int renderTextureResolution = 512;
    [SerializeField] private LayerMask characterLayer = -1;
    [SerializeField] private Color backgroundColor = Color.clear;
    
    [Header("Animation Setup")]
    [SerializeField] private RuntimeAnimatorController characterAnimatorController;
    [SerializeField] private string[] requiredAnimationTriggers = {
        "StartTalking", "StopTalking", "Greeting", "Farewell", 
        "Happy", "Concerned", "Excited", "Thinking"
    };

    private GameObject spawnedCharacter;
    private Camera renderCamera;
    private RenderTexture characterRenderTexture;

    [ContextMenu("Setup 3D Character for Tutorial")]
    public void Setup3DCharacterSystem()
    {
        Debug.Log("Setting up 3D Character for Tutorial...");
        
        // Step 1: Create the layer
        CreateTutorialCharacterLayer();
        
        // Step 2: Setup render camera
        SetupRenderCamera();
        
        // Step 3: Spawn and configure character
        SpawnAndConfigureCharacter();
        
        // Step 4: Create render texture
        CreateRenderTexture();
        
        // Step 5: Update UI references
        UpdateTutorialUI();
        
        Debug.Log("3D Character setup complete!");
    }

    private void CreateTutorialCharacterLayer()
    {
        // Note: Layers need to be created manually in Project Settings
        // This just logs the requirement
        Debug.Log("MANUAL STEP: Create 'TutorialCharacter' layer in Project Settings > Tags and Layers");
    }

    private void SetupRenderCamera()
    {
        // Create camera for rendering character
        GameObject cameraObj = new GameObject("TutorialCharacterCamera");
        cameraObj.transform.SetParent(transform);
        
        renderCamera = cameraObj.AddComponent<Camera>();
        renderCamera.clearFlags = CameraClearFlags.SolidColor;
        renderCamera.backgroundColor = backgroundColor;
        renderCamera.cullingMask = characterLayer;
        renderCamera.orthographic = false;
        renderCamera.fieldOfView = 30f;
        renderCamera.nearClipPlane = 0.1f;
        renderCamera.farClipPlane = 10f;
        
        // Position camera
        if (cameraPosition != null)
        {
            renderCamera.transform.position = cameraPosition.position;
            renderCamera.transform.rotation = cameraPosition.rotation;
        }
        else
        {
            // Default position
            renderCamera.transform.position = new Vector3(0, 1.5f, 2f);
            renderCamera.transform.LookAt(Vector3.up * 1.5f);
        }
    }

    private void SpawnAndConfigureCharacter()
    {
        if (characterModelPrefab == null)
        {
            Debug.LogWarning("No character model prefab assigned!");
            return;
        }

        // Spawn character
        Vector3 spawnPos = characterSpawnPoint != null ? characterSpawnPoint.position : Vector3.zero;
        spawnedCharacter = Instantiate(characterModelPrefab, spawnPos, Quaternion.identity);
        spawnedCharacter.name = "TutorialOldManCharacter";
        spawnedCharacter.transform.SetParent(transform);

        // Set layer recursively
        SetLayerRecursively(spawnedCharacter, LayerMask.NameToLayer("TutorialCharacter"));

        // Setup animator if needed
        Animator animator = spawnedCharacter.GetComponent<Animator>();
        if (animator == null)
        {
            animator = spawnedCharacter.AddComponent<Animator>();
        }

        if (characterAnimatorController != null)
        {
            animator.runtimeAnimatorController = characterAnimatorController;
        }

        // Validate animation triggers
        ValidateAnimationTriggers(animator);
    }

    private void CreateRenderTexture()
    {
        characterRenderTexture = new RenderTexture(renderTextureResolution, renderTextureResolution, 16);
        characterRenderTexture.name = "TutorialCharacterRenderTexture";
        
        if (renderCamera != null)
        {
            renderCamera.targetTexture = characterRenderTexture;
        }
    }

    private void UpdateTutorialUI()
    {
        // Find the tutorial UI setup
        TutorialUI3DCharacter tutorialUI = FindFirstObjectByType<TutorialUI3DCharacter>();
        
        if (tutorialUI == null)
        {
            // Need to replace existing TutorialUIPrefab with TutorialUI3DCharacter
            Debug.LogWarning("Replace TutorialUIPrefab script with TutorialUI3DCharacter script on your tutorial panel!");
            return;
        }

        // Configure the 3D character settings
        tutorialUI.use3DCharacter = true;
        tutorialUI.character3DCamera = renderCamera;
        tutorialUI.character3DModel = spawnedCharacter;
        tutorialUI.characterRenderTexture = characterRenderTexture;
        
        if (spawnedCharacter != null)
        {
            tutorialUI.character3DAnimator = spawnedCharacter.GetComponent<Animator>();
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

    private void ValidateAnimationTriggers(Animator animator)
    {
        if (animator.runtimeAnimatorController == null) return;

        foreach (string trigger in requiredAnimationTriggers)
        {
            bool hasParameter = false;
            foreach (AnimatorControllerParameter param in animator.parameters)
            {
                if (param.name == trigger && param.type == AnimatorControllerParameterType.Trigger)
                {
                    hasParameter = true;
                    break;
                }
            }
            
            if (!hasParameter)
            {
                Debug.LogWarning($"Animation trigger '{trigger}' not found in animator controller!");
            }
        }
    }

    [ContextMenu("Test Character Animations")]
    public void TestCharacterAnimations()
    {
        if (spawnedCharacter == null) return;
        
        Animator animator = spawnedCharacter.GetComponent<Animator>();
        if (animator == null) return;

        // Test greeting animation
        animator.SetTrigger("Greeting");
        
        Debug.Log("Playing greeting animation. Check the character!");
    }
}

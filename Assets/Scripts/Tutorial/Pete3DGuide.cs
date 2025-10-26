using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pete3D Guide System - Integrates with existing TutorialManager
/// Provides contextual 3D Pete positioning and reactions
/// </summary>
public class Pete3DGuide : MonoBehaviour
{
    [Header("Pete 3D Model")]
    [SerializeField] private GameObject pete3DModel;
    [SerializeField] private Transform peteTransform;
    [SerializeField] private Camera gameCamera;
    
    [Header("Existing Render Texture Setup - Just Link Components")]
    [SerializeField] private RawImage peteUIDisplay;           // Assign your existing RawImage in dialogue panel
    
    // Note: You already have camera and render texture setup - just assign the RawImage component
    
    [Header("Context Positioning")]
    [SerializeField] private Transform worldSpaceContainer;      // Parent for world Pete
    [SerializeField] private Transform uiSpaceContainer;         // Parent for UI Pete  
    [SerializeField] private Transform cornerBuddyContainer;     // Parent for corner Pete
    
    [Header("Pete Variants")]
    [SerializeField] private GameObject worldPete;      // Pete for world tutorials
    [SerializeField] private GameObject uiPete;         // Pete for UI tutorials  
    [SerializeField] private GameObject cornerPete;     // Pete for complex UI
    
    [Header("Positioning Settings")]
    [SerializeField] private float worldOffset = 5f;           // Distance from world targets (increased)
    [SerializeField] private Vector3 uiOffset = new Vector3(100, 0, 0); // UI space offset
    [SerializeField] private float transitionSpeed = 2f;       // Movement animation speed
    [SerializeField] private bool useScreenSpaceUI = true;     // Whether to use screen space for UI Pete
    
    [Header("Emotional States")]
    [SerializeField] private ParticleSystem excitementParticles;
    [SerializeField] private ParticleSystem worryParticles;
    [SerializeField] private ParticleSystem thinkingParticles;
    [SerializeField] private ParticleSystem celebrationParticles;
    
    [Header("Audio Integration")]
    [SerializeField] private AudioSource peteAudioSource;
    [SerializeField] private AudioClip[] excitedSounds;
    [SerializeField] private AudioClip[] worriedSounds;
    [SerializeField] private AudioClip[] neutralSounds;
    
    [Header("Speech Bubble System")]
    [SerializeField] private GameObject speechBubblePrefab;
    [SerializeField] private Canvas speechBubbleCanvas;
    [SerializeField] private float speechBubbleOffset = 1.5f;
    
    // Runtime state
    private PeteContext currentContext = PeteContext.Hidden;
    private GameObject currentActivePete;
    private GameObject currentSpeechBubble;
    private Coroutine currentAnimation;
    private TutorialManager tutorialManager;
    
    // Integration with existing tutorial system
    public static Pete3DGuide Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        
        // Find main camera if not assigned
        if (gameCamera == null)
        {
            gameCamera = Camera.main;
            if (gameCamera == null)
            {
                gameCamera = FindFirstObjectByType<Camera>();
            }
        }
        
        // Find tutorial manager
        tutorialManager = FindFirstObjectByType<TutorialManager>();
        
        // Setup Pete variants if not assigned
        if (pete3DModel != null)
        {
            SetupPeteVariants();
        }
        
        // Initialize all Pete variants as hidden
        HideAllPeteVariants();
        
        Debug.Log($"Pete3DGuide initialized. Camera: {gameCamera?.name}, TutorialManager: {tutorialManager?.name}");
    }
    
    private void SetupPeteVariants()
    {
        // Create Pete variants if they don't exist
        if (worldPete == null && pete3DModel != null)
        {
            worldPete = Instantiate(pete3DModel, worldSpaceContainer);
            worldPete.name = "WorldPete";
            Debug.Log("Created WorldPete");
        }
        
        // For UI Pete, we'll use render texture (manually assigned)
        if (uiPete == null && pete3DModel != null)
        {
            uiPete = Instantiate(pete3DModel, uiSpaceContainer);
            uiPete.name = "UIPete";
            Debug.Log("Created UIPete for render texture");
        }
        
        if (cornerPete == null && pete3DModel != null)
        {
            cornerPete = Instantiate(pete3DModel, cornerBuddyContainer);
            cornerPete.name = "CornerPete";
            Debug.Log("Created CornerPete");
            
            // Scale down corner Pete and lock the scale
            cornerPete.transform.localScale = Vector3.one * 0.6f;
            
            // Prevent scale changes during runtime
            var lockScale = cornerPete.AddComponent<LockTransform>();
            lockScale.lockScale = true;
        }
        
        Debug.Log($"Pete variants setup: World={worldPete != null}, UI={uiPete != null}, Corner={cornerPete != null}");
    }
    
    /// <summary>
    /// Main method called by TutorialManager to position Pete for current step
    /// </summary>
    public void PositionPeteForStep(TutorialStep step)
    {
        if (step == null) return;
        
        // Stop any current animation
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
        }
        
        // Determine Pete context
        PeteContext targetContext = DeterminePeteContext(step);
        
        // Position Pete based on context
        currentAnimation = StartCoroutine(TransitionPeteToContext(step, targetContext));
        
        // Show emotional state
        ShowPeteEmotion(step.peteEmotion);
        
        // Handle speech bubble
        ShowSpeechBubble(step.instructionText);
    }
    
    private PeteContext DeterminePeteContext(TutorialStep step)
    {
        // If explicitly set, use that
        if (step.peteContext != PeteContext.Auto)
        {
            return step.peteContext;
        }
        
        // Auto-determine based on step content
        if (step.peteWorldTarget != null || step.peteWorldPosition != Vector3.zero)
        {
            return PeteContext.WorldGuide;
        }
        
        if (step.peteUITarget != null || step.uiToHighlight != null)
        {
            // Check if it's a complex UI (shop, barracks, etc.)
            if (IsComplexUI(step.uiToHighlight) || IsComplexUI(step.peteUITarget?.gameObject))
            {
                return PeteContext.CornerBuddy;
            }
            return PeteContext.UIHelper;
        }
        
        // Default to world guide
        return PeteContext.WorldGuide;
    }
    
    private bool IsComplexUI(GameObject uiElement)
    {
        if (uiElement == null) return false;
        
        // Check for complex UI components
        string[] complexUINames = { "Shop", "Barracks", "AnimalStructureUI", "Panel" };
        
        foreach (string name in complexUINames)
        {
            if (uiElement.name.Contains(name))
            {
                return true;
            }
        }
        
        return false;
    }
    
    private IEnumerator TransitionPeteToContext(TutorialStep step, PeteContext targetContext)
    {
        // Hide current Pete
        HideAllPeteVariants();
        yield return new WaitForSeconds(0.2f); // Brief pause for visual clarity
        
        // Show appropriate Pete variant
        currentContext = targetContext;
        
        switch (targetContext)
        {
            case PeteContext.WorldGuide:
                yield return StartCoroutine(ShowWorldPete(step));
                break;
                
            case PeteContext.UIHelper:
                yield return StartCoroutine(ShowUIPete(step));
                break;
                
            case PeteContext.CornerBuddy:
                yield return StartCoroutine(ShowCornerPete(step));
                break;
                
            case PeteContext.Hidden:
                // Pete stays hidden
                break;
        }
        
        currentAnimation = null;
    }
    
    private IEnumerator ShowWorldPete(TutorialStep step)
    {
        if (worldPete == null) yield break;
        
        currentActivePete = worldPete;
        worldPete.SetActive(true);
        
        // Determine target position
        Vector3 targetPosition = GetWorldTargetPosition(step);
        Debug.Log($"Pete positioning at World: {targetPosition}");
        
        // Animate Pete to position
        yield return StartCoroutine(AnimateToPosition(worldPete.transform, targetPosition));
        
        // Make Pete look at target if specified
        if (step.peteLooksAtTarget)
        {
            GameObject lookTarget = step.peteWorldTarget?.gameObject ?? step.uiToHighlight;
            if (lookTarget != null)
            {
                LookAtTarget(worldPete.transform, lookTarget);
            }
        }
    }
    
    private IEnumerator ShowUIPete(TutorialStep step)
    {
        if (uiPete == null)
        {
            Debug.LogError("UIPete is null!");
            yield break;
        }
        
        currentActivePete = uiPete;
        
        // Animate the existing RawImage sliding up
        if (peteUIDisplay != null)
        {
            peteUIDisplay.gameObject.SetActive(true);
            
            // Get RectTransform for animation
            RectTransform peteRect = peteUIDisplay.GetComponent<RectTransform>();
            if (peteRect != null)
            {
                // Store the target position
                Vector2 targetPos = peteRect.anchoredPosition;
                
                // Start Pete below the visible area (slide up from bottom)
                float slideDistance = peteRect.rect.height + 50f; // Slide from below
                peteRect.anchoredPosition = new Vector2(targetPos.x, targetPos.y - slideDistance);
                
                // Animate Pete sliding up with easing and bounce
                float elapsed = 0f;
                float duration = 0.5f; // Half second slide-up animation
                
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = elapsed / duration;
                    
                    // Cubic ease-out for smooth deceleration with overshoot
                    float easedT = 1f - Mathf.Pow(1f - t, 3f);
                    
                    // Add slight overshoot for bounce effect
                    float overshoot = 1.1f;
                    if (t > 0.8f) // Only apply bounce near the end
                    {
                        float bounceT = (t - 0.8f) / 0.2f;
                        easedT = easedT + (Mathf.Sin(bounceT * Mathf.PI) * 0.05f);
                    }
                    
                    peteRect.anchoredPosition = Vector2.Lerp(
                        new Vector2(targetPos.x, targetPos.y - slideDistance),
                        targetPos,
                        easedT
                    );
                    
                    yield return null;
                }
                
                // Ensure final position is exact
                peteRect.anchoredPosition = targetPos;
            }
            
            Debug.Log("Animated Pete RawImage sliding up in tutorial panel with bounce");
        }
        else
        {
            Debug.LogError("Pete RawImage not assigned! Assign your existing RawImage component in Inspector.");
        }
    }
    
    private void SetPeteRenderingLayer(GameObject pete, string layer)
    {
        // Set Pete and all children to specified layer
        pete.layer = LayerMask.NameToLayer(layer);
        
        foreach (Transform child in pete.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = LayerMask.NameToLayer(layer);
        }
        
        // Ensure renderers are enabled
        Renderer[] renderers = pete.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }
    
    private IEnumerator ShowCornerPete(TutorialStep step)
    {
        if (cornerPete == null) yield break;
        
        currentActivePete = cornerPete;
        cornerPete.SetActive(true);
        
        // Position in corner
        Vector3 cornerPosition = GetCornerPosition();
        cornerPete.transform.position = cornerPosition;
        
        // Scale animation for entrance
        yield return StartCoroutine(ScaleAnimation(cornerPete.transform, Vector3.zero, Vector3.one * 0.6f));
    }
    
    private Vector3 GetWorldTargetPosition(TutorialStep step)
    {
        Vector3 targetPos;
        
        if (step.peteWorldTarget != null)
        {
            targetPos = step.peteWorldTarget.position + Vector3.up * worldOffset;
        }
        else if (step.peteWorldPosition != Vector3.zero)
        {
            targetPos = step.peteWorldPosition;
        }
        else
        {
            // Default to camera forward position
            if (gameCamera != null)
            {
                targetPos = gameCamera.transform.position + gameCamera.transform.forward * 5f;
            }
            else
            {
                targetPos = new Vector3(0, 5, 5); // Safe default above ground
            }
        }
        
        // Ensure Pete is above ground using raycast
        targetPos = EnsureAboveGround(targetPos);
        
        return targetPos;
    }
    
    private Vector3 EnsureAboveGround(Vector3 position)
    {
        // Cast ray downward to find ground
        RaycastHit hit;
        Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z); // Start well above
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 50f))
        {
            // Found ground, position Pete above it
            return new Vector3(position.x, hit.point.y + worldOffset, position.z);
        }
        
        // No ground found, use original position but ensure it's high enough
        return new Vector3(position.x, Mathf.Max(position.y, 3f), position.z);
    }
    
    private Vector3 GetUITargetPosition(TutorialStep step)
    {
        GameObject uiTarget = step.peteUITarget?.gameObject ?? step.uiToHighlight;
        
        if (uiTarget != null && gameCamera != null)
        {
            if (useScreenSpaceUI)
            {
                // Convert UI position to world space for Pete, but in front of camera
                RectTransform rectTransform = uiTarget.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    // Get screen position of UI element
                    Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(gameCamera, rectTransform.position);
                    
                    // Convert to world position at a fixed distance from camera
                    float distanceFromCamera = 5f;
                    Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, distanceFromCamera));
                    
                    // Add offset to position Pete near but not on the UI element
                    Vector3 offsetWorldPos = worldPos + gameCamera.transform.TransformDirection(uiOffset.normalized * 2f);
                    
                    Debug.Log($"UI Pete positioning at: {offsetWorldPos} (screen: {screenPos})");
                    return offsetWorldPos;
                }
            }
        }
        
        // Default UI position (front of camera, slightly to the right)
        if (gameCamera != null)
        {
            Vector3 cameraForward = gameCamera.transform.forward * 4f;
            Vector3 cameraRight = gameCamera.transform.right * 2f;
            Vector3 defaultPos = gameCamera.transform.position + cameraForward + cameraRight;
            Debug.Log($"UI Pete default position: {defaultPos}");
            return defaultPos;
        }
        
        return new Vector3(2, 2, 2); // Fallback position
    }
    
    private Vector3 GetCornerPosition()
    {
        // Position Pete in bottom-right corner
        return gameCamera.ScreenToWorldPoint(new Vector3(Screen.width * 0.85f, Screen.height * 0.15f, 3f));
    }
    
    private void LookAtTarget(Transform peteTransform, GameObject target)
    {
        if (target != null)
        {
            Vector3 direction = (target.transform.position - peteTransform.position).normalized;
            peteTransform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private void LookAtUITarget(Transform peteTransform, GameObject uiTarget)
    {
        if (uiTarget != null)
        {
            Vector3 screenPos = RectTransformUtility.WorldToScreenPoint(gameCamera, uiTarget.transform.position);
            Vector3 worldPos = gameCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, 5f));
            Vector3 direction = (worldPos - peteTransform.position).normalized;
            peteTransform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    private IEnumerator AnimateToPosition(Transform target, Vector3 endPosition)
    {
        Vector3 startPosition = target.position;
        float elapsed = 0f;
        float duration = 1f / transitionSpeed;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }
        
        target.position = endPosition;
    }
    
    private IEnumerator ScaleAnimation(Transform target, Vector3 startScale, Vector3 endScale)
    {
        float elapsed = 0f;
        float duration = 0.5f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        
        target.localScale = endScale;
    }
    
    public void ShowPeteEmotion(PeteEmotion emotion)
    {
        // Stop all particles first
        StopAllParticles();
        
        // Play appropriate particle effect and sound
        switch (emotion)
        {
            case PeteEmotion.Excited:
                if (excitementParticles != null) excitementParticles.Play();
                PlayRandomSound(excitedSounds);
                break;
                
            case PeteEmotion.Worried:
                if (worryParticles != null) worryParticles.Play();
                PlayRandomSound(worriedSounds);
                break;
                
            case PeteEmotion.Thinking:
                if (thinkingParticles != null) thinkingParticles.Play();
                break;
                
            case PeteEmotion.Celebrating:
                if (celebrationParticles != null) celebrationParticles.Play();
                PlayRandomSound(excitedSounds);
                break;
                
            default:
                PlayRandomSound(neutralSounds);
                break;
        }
    }
    
    private void StopAllParticles()
    {
        if (excitementParticles != null) excitementParticles.Stop();
        if (worryParticles != null) worryParticles.Stop();
        if (thinkingParticles != null) thinkingParticles.Stop();
        if (celebrationParticles != null) celebrationParticles.Stop();
    }
    
    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips != null && clips.Length > 0 && peteAudioSource != null)
        {
            AudioClip clip = clips[Random.Range(0, clips.Length)];
            peteAudioSource.clip = clip;
            peteAudioSource.Play();
        }
    }
    
    public void ShowSpeechBubble(string text)
    {
        HideSpeechBubble();
        
        if (string.IsNullOrEmpty(text) || currentActivePete == null) return;
        
        if (speechBubblePrefab != null && speechBubbleCanvas != null)
        {
            currentSpeechBubble = Instantiate(speechBubblePrefab, speechBubbleCanvas.transform);
            
            // Position above Pete
            Vector3 bubblePosition = currentActivePete.transform.position + Vector3.up * speechBubbleOffset;
            Vector3 screenPos = gameCamera.WorldToScreenPoint(bubblePosition);
            
            RectTransform bubbleRect = currentSpeechBubble.GetComponent<RectTransform>();
            if (bubbleRect != null)
            {
                bubbleRect.position = screenPos;
            }
            
            // Set text
            TextMeshProUGUI bubbleText = currentSpeechBubble.GetComponentInChildren<TextMeshProUGUI>();
            if (bubbleText != null)
            {
                bubbleText.text = text;
            }
        }
    }
    
    public void HideSpeechBubble()
    {
        if (currentSpeechBubble != null)
        {
            Destroy(currentSpeechBubble);
            currentSpeechBubble = null;
        }
    }
    
    private void HideAllPeteVariants()
    {
        HideAllPeteVariants(false); // Default to instant hide
    }
    
    private void HideAllPeteVariants(bool animated)
    {
        if (worldPete != null) worldPete.SetActive(false);
        if (uiPete != null) uiPete.SetActive(false);
        if (cornerPete != null) cornerPete.SetActive(false);
        
        // Animate Pete sliding down before hiding (only if animated is true)
        if (peteUIDisplay != null) 
        {
            if (animated && peteUIDisplay.gameObject.activeSelf)
            {
                StartCoroutine(HidePeteWithAnimation());
            }
            else
            {
                // Instant hide without animation
                peteUIDisplay.gameObject.SetActive(false);
            }
        }
        
        currentActivePete = null;
        currentContext = PeteContext.Hidden;
        
        HideSpeechBubble();
    }
    
    private IEnumerator HidePeteWithAnimation()
    {
        if (peteUIDisplay == null || !peteUIDisplay.gameObject.activeSelf)
        {
            yield break;
        }
        
        RectTransform peteRect = peteUIDisplay.GetComponent<RectTransform>();
        if (peteRect != null)
        {
            // Store the current position
            Vector2 startPos = peteRect.anchoredPosition;
            
            // Calculate slide-down target (below visible area)
            float slideDistance = peteRect.rect.height + 50f;
            Vector2 targetPos = new Vector2(startPos.x, startPos.y - slideDistance);
            
            // Animate Pete sliding down
            float elapsed = 0f;
            float duration = 0.3f; // Faster slide-down
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Cubic ease-in for smooth acceleration
                float easedT = t * t * t;
                
                peteRect.anchoredPosition = Vector2.Lerp(startPos, targetPos, easedT);
                
                yield return null;
            }
        }
        
        // Hide after animation completes
        peteUIDisplay.gameObject.SetActive(false);
    }
    
    /// <summary>
    /// Public method for TutorialManager to hide Pete
    /// </summary>
    public void HidePete()
    {
        if (currentAnimation != null)
        {
            StopCoroutine(currentAnimation);
            currentAnimation = null;
        }
        
        HideAllPeteVariants(true); // Use animated hide when explicitly hiding Pete
        StopAllParticles();
    }
    
    /// <summary>
    /// Integration method - called when tutorial step completes
    /// </summary>
    public void OnStepComplete()
    {
        ShowPeteEmotion(PeteEmotion.Celebrating);
    }
    
    /// <summary>
    /// Integration method - called when tutorial step starts
    /// </summary>
    public void OnStepStart(TutorialStep step)
    {
        PositionPeteForStep(step);
    }
    
    /// <summary>
    /// Debug method - call this to force show UI Pete in visible location
    /// </summary>
    [ContextMenu("Force Show UI Pete")]
    public void ForceShowUIPete()
    {
        if (uiPete == null)
        {
            Debug.LogError("UIPete is null!");
            return;
        }
        
        uiPete.SetActive(true);
        
        // Position directly in front of camera
        if (gameCamera != null)
        {
            Vector3 frontOfCamera = gameCamera.transform.position + gameCamera.transform.forward * 3f;
            uiPete.transform.position = frontOfCamera;
            uiPete.transform.LookAt(gameCamera.transform);
            Debug.Log($"Forced UI Pete to: {frontOfCamera}");
        }
        else
        {
            uiPete.transform.position = new Vector3(0, 2, 2);
            Debug.Log("No camera found, positioned at (0,2,2)");
        }
        
        // Make sure it's rendering
        SetPeteRenderingLayer(uiPete, "Default");
        currentActivePete = uiPete;
        
        Debug.Log($"UI Pete active: {uiPete.activeInHierarchy}, position: {uiPete.transform.position}");
    }
    
    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
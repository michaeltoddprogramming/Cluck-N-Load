using UnityEngine;

/// <summary>
/// Integration partial class for TutorialManager to work with Pete3DGuide
/// This maintains backward compatibility while adding Pete functionality
/// </summary>
public partial class TutorialManager
{
    [Header("Pete 3D Integration")]
    [SerializeField] private Pete3DGuide pete3DGuide;
    [SerializeField] private bool usePete3D = true;
    [SerializeField] private bool minimizeChecklistWithPete = true;
    
    // Pete integration state
    private bool peteIntegrationInitialized = false;
    
    /// <summary>
    /// Initialize Pete3D integration - called from existing Awake
    /// </summary>
    private void InitializePete3DIntegration()
    {
        if (peteIntegrationInitialized) return;
        
        // Find Pete3DGuide if not assigned
        if (pete3DGuide == null)
        {
            pete3DGuide = FindFirstObjectByType<Pete3DGuide>();
        }
        
        // If Pete3D is enabled, minimize checklist
        if (usePete3D && minimizeChecklistWithPete)
        {
            MinimizeChecklistForPete();
        }
        
        peteIntegrationInitialized = true;
    }
    
    /// <summary>
    /// Enhanced step presentation with Pete integration
    /// Integrates with existing ShowStep method
    /// </summary>
    private void ShowStepWithPete(TutorialStep step)
    {
        if (!usePete3D || pete3DGuide == null)
        {
            // Fallback to original system
            return;
        }
        
        // Show Pete for this step
        pete3DGuide.OnStepStart(step);
        
        // Modify dialogue system when Pete is active
        ModifyDialogueForPete(step);
    }
    
    /// <summary>
    /// Enhanced step completion with Pete integration
    /// </summary>
    private void CompleteStepWithPete()
    {
        if (usePete3D && pete3DGuide != null)
        {
            pete3DGuide.OnStepComplete();
        }
    }
    
    /// <summary>
    /// Modify dialogue presentation when Pete is active
    /// </summary>
    private void ModifyDialogueForPete(TutorialStep step)
    {
        if (tutorialPanel != null && usePete3D)
        {
            // Check Pete's context to determine dialogue placement
            PeteContext context = DetermineStepPeteContext(step);
            
            switch (context)
            {
                case PeteContext.WorldGuide:
                    // Hide main dialogue panel, let Pete's speech bubble handle it
                    if (tutorialPanel.activeInHierarchy)
                    {
                        tutorialPanel.SetActive(false);
                    }
                    break;
                    
                case PeteContext.UIHelper:
                    // Minimize dialogue panel
                    MinimizeDialoguePanel();
                    break;
                    
                case PeteContext.CornerBuddy:
                    // Keep dialogue but position it differently
                    RepositionDialogueForCornerPete();
                    break;
                    
                default:
                    // Use normal dialogue
                    if (!tutorialPanel.activeInHierarchy)
                    {
                        tutorialPanel.SetActive(true);
                    }
                    break;
            }
        }
    }
    
    /// <summary>
    /// Determine Pete context for a step (mirrors Pete3DGuide logic)
    /// </summary>
    private PeteContext DetermineStepPeteContext(TutorialStep step)
    {
        if (step.peteContext != PeteContext.Auto)
        {
            return step.peteContext;
        }
        
        if (step.peteWorldTarget != null || step.peteWorldPosition != Vector3.zero)
        {
            return PeteContext.WorldGuide;
        }
        
        if (step.peteUITarget != null || step.uiToHighlight != null)
        {
            return PeteContext.UIHelper;
        }
        
        return PeteContext.WorldGuide;
    }
    
    /// <summary>
    /// Minimize checklist when Pete is active
    /// </summary>
    private void MinimizeChecklistForPete()
    {
        if (checklistPanel != null)
        {
            // Scale down checklist to small corner indicator
            RectTransform checklistRect = checklistPanel.GetComponent<RectTransform>();
            if (checklistRect != null)
            {
                // Store original size for restoration if needed
                Vector2 originalSize = checklistRect.sizeDelta;
                
                // Minimize to small corner panel
                checklistRect.sizeDelta = new Vector2(200, 150); // Much smaller
                checklistRect.anchorMin = new Vector2(1, 1); // Top-right corner
                checklistRect.anchorMax = new Vector2(1, 1);
                checklistRect.anchoredPosition = new Vector2(-10, -10); // Small offset from corner
                
                // Reduce opacity
                CanvasGroup checklistGroup = checklistPanel.GetComponent<CanvasGroup>();
                if (checklistGroup == null)
                {
                    checklistGroup = checklistPanel.AddComponent<CanvasGroup>();
                }
                checklistGroup.alpha = 0.7f; // Semi-transparent
            }
        }
    }
    
    /// <summary>
    /// Minimize dialogue panel for UI helper context
    /// </summary>
    private void MinimizeDialoguePanel()
    {
        if (tutorialPanel != null)
        {
            RectTransform dialogueRect = tutorialPanel.GetComponent<RectTransform>();
            if (dialogueRect != null)
            {
                // Make dialogue smaller and position it contextually
                dialogueRect.sizeDelta = new Vector2(400, 100);
                dialogueRect.anchoredPosition = new Vector2(0, -50);
            }
        }
    }
    
    /// <summary>
    /// Reposition dialogue when corner Pete is active
    /// </summary>
    private void RepositionDialogueForCornerPete()
    {
        if (tutorialPanel != null)
        {
            RectTransform dialogueRect = tutorialPanel.GetComponent<RectTransform>();
            if (dialogueRect != null)
            {
                // Move dialogue to avoid corner Pete
                dialogueRect.anchorMin = new Vector2(0, 0);
                dialogueRect.anchorMax = new Vector2(0.7f, 0.3f); // Left side, avoiding corner
                dialogueRect.anchoredPosition = Vector2.zero;
            }
        }
    }
    
    /// <summary>
    /// Hide Pete when tutorial ends or is skipped
    /// </summary>
    private void HidePeteOnTutorialEnd()
    {
        if (usePete3D && pete3DGuide != null)
        {
            pete3DGuide.HidePete();
        }
    }
    
    /// <summary>
    /// Public method to toggle Pete3D system
    /// </summary>
    public void TogglePete3D(bool enabled)
    {
        usePete3D = enabled;
        
        if (!enabled && pete3DGuide != null)
        {
            pete3DGuide.HidePete();
        }
    }
    
    /// <summary>
    /// Public method to check if Pete3D is active
    /// </summary>
    public bool IsPete3DActive()
    {
        return usePete3D && pete3DGuide != null;
    }
    
    /// <summary>
    /// Method to configure Pete for specific step (for external use)
    /// </summary>
    public void ConfigurePeteForStep(string stepId, PeteContext context, Vector3 worldPos, Transform target)
    {
        if (!usePete3D) return;
        
        // Find step and modify it
        for (int i = 0; i < steps.Count; i++)
        {
            if (steps[i].stepId == stepId)
            {
                steps[i].peteContext = context;
                steps[i].peteWorldPosition = worldPos;
                steps[i].peteWorldTarget = target;
                break;
            }
        }
    }
}
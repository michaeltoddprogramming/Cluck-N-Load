using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseStructureUI : MonoBehaviour, IStructureUI
{
    [SerializeField] protected TextMeshProUGUI structureNameText;
    [SerializeField] protected TextMeshProUGUI healthText;
    [SerializeField] protected TextMeshProUGUI description;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button moveButton;

    protected Structure structure;
    [System.NonSerialized]
    protected NightManager nightManager;
    
    // Optimization: Change detection for day/night and pause state
    protected bool lastIsDayState = true;
    protected bool lastPauseState = false;

    [Header("Health Bars")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Image healthBarFill;
    private Color healthyColor = new Color(0.2f, 1f, 0.2f);
    private Color midColor = new Color(1f, 0.9f, 0.2f);
    private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    [System.NonSerialized]
    protected UIHoverManager hoverManager; 

    public void Start()
    {
        hoverManager = FindFirstObjectByType<UIHoverManager>();
    }

    public virtual void Initialize(Structure structure)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 1;
        this.transform.SetAsFirstSibling();
        // if (canvas != null) canvas.sortingOrder = -1000000;
        this.structure = structure;


        // Get reference to NightManager
        nightManager = FindFirstObjectByType<NightManager>();

        if (structureNameText != null) structureNameText.text = structure.GetStructureName();
        UpdateHealthDisplay();

        if (description != null) description.text = structure.GetDescription();

        if (structure != null)
        {
            structure.OnHealthChanged += UpdateHealthDisplay;
        }

        closeButton?.onClick.AddListener(() =>
        {
            // Don't call Deselect here - HideStructureUI will handle it
            StructureUIManager.Instance?.HideStructureUI();
        });

        moveButton?.onClick.AddListener(() =>
        {
            // Check if it's night time or paused before allowing move
            if (nightManager != null && (!nightManager.IsDay || nightManager.getIsPaused()))
            {
                // Play error sound to indicate action is not allowed
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayErrorSound();
                }
                return;
            }

            BuildController buildController = FindFirstObjectByType<BuildController>();
            if (buildController != null)
            {
                buildController.StartMoveModeForStructure(structure);
                StructureUIManager.Instance?.HideStructureUI();
            }
        });
    }

    protected virtual void Update()
    {
        // Debug.Log("Move button update called ++++++++++++++++++++++++");
        // OPTIMIZATION: Only update move button when state actually changes
        // Avoids 300 property accesses/second with 5 barracks (5 × 60 FPS)
        if (moveButton != null)
        {
            bool currentIsDay = NightManager.Instance.IsDay;
            bool currentIsPaused = NightManager.Instance.getIsPaused();
            // bool currentIsDay = nightManager.IsDay;
            // bool currentIsPaused = nightManager.getIsPaused();
            
            // Only update if state changed (change detection pattern)
            if (currentIsDay != lastIsDayState || currentIsPaused != lastPauseState)
            {
                lastIsDayState = currentIsDay;
                lastPauseState = currentIsPaused;
                
                bool canMove = currentIsDay && !currentIsPaused;
                moveButton.interactable = canMove;
                // Debug.Log("The move button should be disabled ----------------");
            }
            
            // Let Unity's Button component handle the visual disabled state
            // (uses the Button's Color Tint settings in the Inspector)
        }
    }

    protected virtual void UpdateHealthDisplay()
    {
        if (structure == null) return;

        if (healthText != null)
            healthText.text = $"{structure.GetCurrentHealth()}/{structure.GetMaxHealth()}";

        if (healthBarSlider != null)
        {
            float pct = Mathf.Clamp01((float)structure.GetCurrentHealth() / structure.GetMaxHealth());
            healthBarSlider.value = pct;
            if (healthBarFill != null)
            {
                healthBarFill.color =
                    pct > 0.6f ? healthyColor : pct > 0.3f ? midColor : dangerColor;
            }
        }
    }

    // protected virtual void DisplayDescription()
    // {
    //     if (structure != null && description != null)
    //         description.text = $"{structure.GetDescription()}";
    // }

    protected virtual void OnDestroy()
    {
        // FIX: Changed from UnityEvent syntax to standard C# event syntax
        if (structure != null)
            structure.OnHealthChanged -= UpdateHealthDisplay;
    }

     public void OnMoveButtonHoverEnter(GameObject button)
    {

        if(hoverManager != null)
        {
            //when they try to move at night 
            if(button == moveButton.gameObject && !nightManager.IsDay)
            {
                hoverManager.ShowHover(moveButton, "Sleeping!", $"Buildings can’t be moved at night!", true, new Vector2(200, 0));
            }
            //when they try to move when paused 
            if(button == moveButton.gameObject && nightManager.getIsPaused())
            {
                hoverManager.ShowHover(moveButton, "Freeze!", $"Buildings don’t move while time’s stopped!", true, new Vector2(200, 0));
            }
        }
    }

    public void OnMoveButtonHoverExit()
    {
        if(hoverManager != null)
        {
            hoverManager.HideHover();
        }
    }

    public void OnMoveButtonClick(GameObject button)
    {
        //when they try to move at night 
        if(button == moveButton.gameObject && !nightManager.IsDay)
        {
            hoverManager.PlayErrorFeedback(false, moveButton);
        }
        //when they try to move when paused 
        if(button == moveButton.gameObject && nightManager.getIsPaused())
        {
            hoverManager.PlayErrorFeedback(false, moveButton);
        }
    }
}
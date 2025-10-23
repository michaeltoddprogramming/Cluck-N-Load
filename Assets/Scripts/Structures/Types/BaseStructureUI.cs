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

    [Header("Health Bars")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Image healthBarFill;
    private Color healthyColor = new Color(0.2f, 1f, 0.2f);
    private Color midColor = new Color(1f, 0.9f, 0.2f);
    private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    public UIHoverManager hoverManager; 

    public void Awake()
    {
        hoverManager = FindObjectOfType<UIHoverManager>();
    }

    public virtual void Initialize(Structure structure)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 1;
        this.structure = structure;


        // Get reference to NightManager
        nightManager = FindFirstObjectByType<NightManager>();

        if (structureNameText != null) structureNameText.text = structure.GetStructureName();
        UpdateHealthDisplay();
        UpdateHealthBar();

        if (description != null) description.text = structure.GetDescription();

        if (structure != null)
        {
            structure.OnHealthChanged += UpdateHealthDisplay;
            structure.OnHealthChanged += UpdateHealthBar;
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
        // Update move button interactability based on day/night and pause state
        if (moveButton != null && nightManager != null)
        {
            bool canMove = nightManager.IsDay && !nightManager.getIsPaused();
            moveButton.interactable = canMove;
            
            // Optional: Visual feedback for disabled state
            if (moveButton.image != null)
            {
                Color buttonColor = moveButton.image.color;
                buttonColor.a = canMove ? 1f : 0.5f;
                moveButton.image.color = buttonColor;
            }
        }
    }

    protected virtual void UpdateHealthDisplay()
    {
        if (structure != null && healthText != null)
            healthText.text = $"{structure.GetCurrentHealth()}/{structure.GetMaxHealth()}";
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

    protected void UpdateHealthBar()
    {
        // Debug.Log("it is calleing why not showing===================================");
        if (structure == null || healthBarSlider == null) return;

        // Debug.Log("it is calleing why not showing===================================");

        float healthPercent = (float)structure.GetCurrentHealth() / structure.GetMaxHealth();
        healthBarSlider.value = healthPercent;

        if (healthBarFill != null)
            healthBarFill.color = healthPercent > 0.6f ? healthyColor : healthPercent > 0.3f ? midColor : dangerColor;
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
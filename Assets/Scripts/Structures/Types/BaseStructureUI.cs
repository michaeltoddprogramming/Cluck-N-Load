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

    [Header("Health Bars")]
    [SerializeField] private Slider healthBarSlider;
    [SerializeField] private Image healthBarFill;
    private Color healthyColor = new Color(0.2f, 1f, 0.2f);
    private Color midColor = new Color(1f, 0.9f, 0.2f);
    private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    public virtual void Initialize(Structure structure)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 1;
        this.structure = structure;

        if (structureNameText != null) structureNameText.text = structure.GetStructureName();
        UpdateHealthDisplay();

        if (description != null) description.text = structure.GetDescription();

        if (structure != null)
            structure.OnHealthChanged += UpdateHealthDisplay;

        closeButton?.onClick.AddListener(() =>
        {
            // Don't call Deselect here - HideStructureUI will handle it
            StructureUIManager.Instance?.HideStructureUI();
        });

        moveButton?.onClick.AddListener(() =>
        {
            BuildController buildController = FindFirstObjectByType<BuildController>();
            if (buildController != null)
            {
                buildController.StartMoveModeForStructure(structure);
                StructureUIManager.Instance?.HideStructureUI();
            }
        });
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
}
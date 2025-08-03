using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseStructureUI : MonoBehaviour, IStructureUI
{
    [SerializeField] protected TextMeshProUGUI structureNameText;
    [SerializeField] protected TextMeshProUGUI healthText;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button moveButton;

    protected Structure structure;

    public virtual void Initialize(Structure structure)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 1;
        this.structure = structure;

        if (structureNameText != null) structureNameText.text = structure.GetStructureName();
        UpdateHealthDisplay();
        
        if (structure != null)
            structure.OnHealthChanged += UpdateHealthDisplay;

        closeButton?.onClick.AddListener(() =>
        {
            structure.Deselect();
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

    protected virtual void OnDestroy()
    {
        // FIX: Changed from UnityEvent syntax to standard C# event syntax
        if (structure != null)
            structure.OnHealthChanged -= UpdateHealthDisplay;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseStructureUI : MonoBehaviour, IStructureUI
{
    [SerializeField] protected TextMeshProUGUI structureNameText;
    [SerializeField] protected TextMeshProUGUI healthText;
    [SerializeField] protected Button closeButton;
    [SerializeField] protected Button moveButton; // New: Move button

    protected Structure structure;

    public virtual void Initialize(Structure structure)
    {

        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null)
        {
            canvas.sortingOrder = 1; // Lower than Shop UI
        }
        this.structure = structure;

        // Set basic information
        if (structureNameText != null)
            structureNameText.text = structure.GetStructureName();

        UpdateHealthDisplay();

        // Subscribe to health changes for event-driven updates
        if (structure != null)
        {
            structure.OnHealthChanged += UpdateHealthDisplay;
        }

        // Set close button action
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(() =>
            {
                // Hide UI and deselect structure
                structure.Deselect();
                StructureUIManager.Instance?.HideStructureUI();
            });
        }
        else
        {
            Debug.LogWarning("CloseButton not assigned in BaseStructureUI prefab!");
        }

        // Set move button action
        if (moveButton != null)
        {
            moveButton.onClick.RemoveAllListeners();
            moveButton.onClick.AddListener(() =>
            {
                BuildController buildController = FindFirstObjectByType<BuildController>();
                if (buildController != null)
                {
                    buildController.StartMoveModeForStructure(structure);
                    StructureUIManager.Instance?.HideStructureUI();
                    Debug.Log($"Move button clicked for {structure.GetStructureName()}");
                }
                else
                {
                    Debug.LogError("BuildController not found, cannot start move mode!");
                }
            });
        }
        else
        {
            Debug.LogWarning("MoveButton not assigned in BaseStructureUI prefab!");
        }
    }

    protected virtual void UpdateHealthDisplay()
    {
        if (structure != null && healthText != null)
        {
            healthText.text = $"Health: {structure.GetCurrentHealth()}/{structure.GetMaxHealth()}";
        }
    }

    protected virtual void OnDestroy()
    {
        // Unsubscribe from events to prevent memory leaks
        if (structure != null)
        {
            structure.OnHealthChanged -= UpdateHealthDisplay;
        }
    }
}
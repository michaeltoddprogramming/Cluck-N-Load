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
        this.structure = structure;

        // Set basic information
        if (structureNameText != null)
            structureNameText.text = structure.GetStructureName();

        if (healthText != null)
            healthText.text = $"Health: {structure.GetCurrentHealth()}/{structure.GetMaxHealth()}";

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
                BuildController buildController = FindObjectOfType<BuildController>();
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

    protected virtual void Update()
    {
        // Update health value if needed
        if (structure != null && healthText != null)
        {
            healthText.text = $"Health: {structure.GetCurrentHealth()}/{structure.GetMaxHealth()}";
        }
    }
}
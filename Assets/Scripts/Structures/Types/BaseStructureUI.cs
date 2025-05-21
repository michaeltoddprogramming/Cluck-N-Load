using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BaseStructureUI : MonoBehaviour, IStructureUI
{
    [SerializeField] protected TextMeshProUGUI structureNameText;
    [SerializeField] protected TextMeshProUGUI healthText;
    [SerializeField] protected Button closeButton;
    
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
            closeButton.onClick.AddListener(() => {
                // Hide UI and deselect structure
                structure.Deselect();
                StructureUIManager.Instance.HideStructureUI();
            });
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
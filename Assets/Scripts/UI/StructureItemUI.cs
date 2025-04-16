using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StructureItemUI : MonoBehaviour
{
    public Image icon;
    public TMP_Text nameText;
    public Button selectButton;

    private StructureData data;

    // Setup is called when populating the UI from the database
    public void Setup(StructureData structure)
    {
        if (structure == null)
        {
            Debug.LogError("StructureData is null!");
            return;
        }

        data = structure;

        if (icon != null)
            icon.sprite = structure.icon;
        else
            Debug.LogWarning("Icon Image is not assigned!");

        if (nameText != null)
            nameText.text = structure.structureName;
        else
            Debug.LogWarning("Name Text is not assigned!");

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // Prevent stacking listeners
            selectButton.onClick.AddListener(() => SelectStructure());
        }
        else
            Debug.LogWarning("Select Button is not assigned!");
    }

    public void SelectStructure()
    {
        if (data != null)
        {
            Debug.Log($"Selected structure: {data.structureName}");
            // Pass the StructureData to the GridController
            GridController controller = FindObjectOfType<GridController>();
            if (controller != null)
            {
                controller.SetBuildTarget(data);
            }
            else
            {
                Debug.LogError("GridController not found in scene!");
            }
        }
        else
        {
            Debug.LogError("StructureData is null when selecting structure!");
        }
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();
    }
}

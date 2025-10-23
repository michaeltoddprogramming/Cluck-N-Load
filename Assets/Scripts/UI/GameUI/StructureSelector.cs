using UnityEngine;
using System.Collections;

// DISABLED: This script has been disabled to avoid conflicts with SelectionManager.cs
// All selection logic has been consolidated into SelectionManager.cs for consistency
public class StructureSelector : MonoBehaviour
{
    private Structure lastSelectedStructure;

    // DISABLED: Update method commented out to prevent conflicts with SelectionManager
    // void Update()
    // {
    //     if (Input.GetMouseButtonDown(0) && !isProcessingClick)
    //     {
    //         StartCoroutine(ProcessClick());
    //     }
    // }

    private IEnumerator ProcessClick()
    {
        yield return null; // Wait one frame to clear input state

        BuildController buildController = FindFirstObjectByType<BuildController>();
        if (buildController != null && buildController.IsDeleteModeActive())
        {
            yield break;
        }

        bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (isOverUI)
        {
            yield break;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast")); // Fixed typo: NameToName → NameToLayer
        if (Physics.Raycast(ray, out hit, 1000f, layerMask))
        {
            Transform hitTransform = hit.transform;
            while (hitTransform != null)
            {
                if (hitTransform.name.StartsWith("Item_"))
                {
                    Structure structure = hitTransform.GetComponent<Structure>();
                    // if (structure != null && structure.GetAllowSelectionAndUI())
                    // {
                    //     if (lastSelectedStructure != null && lastSelectedStructure == structure && structure.IsSelected())
                    //     {
                    //         structure.Deselect();
                    //         StructureUIManager.Instance?.HideStructureUI();
                    //         lastSelectedStructure = null;
                    //     }
                    //     // if (structure == lastSelectedStructure && structure.IsSelected())
                    //     // {
                    //     //     structure.Deselect();
                    //     //     StructureUIManager.Instance?.HideStructureUI();
                    //     //     lastSelectedStructure = null;
                    //     // }
                    //     else
                    //     {
                    //         if (lastSelectedStructure != null && lastSelectedStructure != structure)
                    //         {
                    //             lastSelectedStructure.Deselect();
                    //             StructureUIManager.Instance?.HideStructureUI();
                    //         }
                    //         structure.Select();
                    //         StructureUIManager.Instance?.ShowStructureUI(structure);
                    //         lastSelectedStructure = structure;
                    //     }
                    //     isProcessingClick = false;
                    //     yield break;
                    // }

                    if (structure != null && structure.GetAllowSelectionAndUI() && structure.GetCurrentHealth() > 0)
                    {
                        if (lastSelectedStructure != null && lastSelectedStructure == structure && structure.IsSelected())
                        {
                            structure.Deselect();
                            StructureUIManager.Instance?.HideStructureUI();
                            lastSelectedStructure = null;
                        }
                        else
                        {
                            if (lastSelectedStructure != null && lastSelectedStructure != structure)
                            {
                                lastSelectedStructure.Deselect();
                                StructureUIManager.Instance?.HideStructureUI();
                            }
                            structure.Select();
                            StructureUIManager.Instance?.ShowStructureUI(structure);
                            lastSelectedStructure = structure;
                        }
                        yield break;
                    }
                }
                hitTransform = hitTransform.parent;
            }
            if (lastSelectedStructure != null)
            {
                lastSelectedStructure.Deselect();
                StructureUIManager.Instance?.HideStructureUI();
                lastSelectedStructure = null;
            }
        }
        else
        {
            if (lastSelectedStructure != null)
            {
                lastSelectedStructure.Deselect();
                StructureUIManager.Instance?.HideStructureUI();
                lastSelectedStructure = null;
            }
        }
    }
}
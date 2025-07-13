using UnityEngine;
using System.Collections;

public class StructureSelector : MonoBehaviour
{
    private Structure lastSelectedStructure;
    private bool isProcessingClick = false;

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isProcessingClick)
        {
            StartCoroutine(ProcessClick());
        }
    }

    private IEnumerator ProcessClick()
    {
        isProcessingClick = true;
        yield return null; // Wait one frame to clear input state

        bool isOverUI = UnityEngine.EventSystems.EventSystem.current != null &&
                       UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
        if (isOverUI)
        {
            isProcessingClick = false;
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
                    if (structure != null && structure.GetAllowSelectionAndUI())
                    {
                        if (structure == lastSelectedStructure && structure.IsSelected())
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
                        isProcessingClick = false;
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
        isProcessingClick = false;
    }
}
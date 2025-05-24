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
            Debug.Log("Click over UI, skipping structure selection.");
            isProcessingClick = false;
            yield break;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast")); // Fixed typo: NameToName → NameToLayer
        if (Physics.Raycast(ray, out hit, 1000f, layerMask))
        {
            Debug.Log($"Raycast hit: {hit.transform.name}, Layer: {LayerMask.LayerToName(hit.transform.gameObject.layer)}");
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
                            Debug.Log($"Deselected {structure.GetStructureName()}.");
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
                            Debug.Log($"Selected {structure.GetStructureName()}.");
                        }
                        isProcessingClick = false;
                        yield break;
                    }
                }
                hitTransform = hitTransform.parent;
            }
            Debug.Log("No valid structure found under cursor.");
            if (lastSelectedStructure != null)
            {
                lastSelectedStructure.Deselect();
                StructureUIManager.Instance?.HideStructureUI();
                lastSelectedStructure = null;
            }
        }
        else
        {
            Debug.Log("Raycast missed: No hit detected.");
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
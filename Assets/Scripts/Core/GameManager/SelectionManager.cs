using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    private Structure currentSelectedStructure;

private void Update()
{
    if (Input.GetMouseButtonDown(0))
    {
        if (EventSystem.current.IsPointerOverGameObject())
        {
            Debug.Log("Click ignored: Mouse is over a UI element");
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Structure structure = hit.collider.GetComponentInParent<Structure>();
            if (structure != null && structure.gameObject.name != "BuildGhost" && structure.GetAllowSelectionAndUI())
            {
                Debug.Log($"Structure clicked: {structure.GetStructureName()}, AllowSelectionAndUI: {structure.GetAllowSelectionAndUI()}");
                SelectStructure(structure);
            }
            else
            {
                Debug.Log("No valid structure clicked or selection not allowed");
                DeselectCurrent();
            }
        }
        else
        {
            Debug.Log("Raycast missed: Nothing clicked");
            DeselectCurrent();
        }
    }
}

private void SelectStructure(Structure structure)
{
    if (currentSelectedStructure == structure) return;
    DeselectCurrent();
    currentSelectedStructure = structure;
    structure.Select();
    
    if (StructureUIManager.Instance == null)
    {
        Debug.LogError("StructureUIManager.Instance is null! Make sure StructureUIManager is in the scene.");
        return;
    }
    
    Debug.Log("Calling ShowStructureUI");
    StructureUIManager.Instance.ShowStructureUI(structure);
}

    private void DeselectCurrent()
    {
        if (currentSelectedStructure != null)
        {
            currentSelectedStructure.Deselect();
            StructureUIManager.Instance.HideStructureUI();
            currentSelectedStructure = null;
        }
    }
}
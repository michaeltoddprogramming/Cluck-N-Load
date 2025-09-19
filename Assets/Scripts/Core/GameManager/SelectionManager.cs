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
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Structure structure = hit.collider.GetComponentInParent<Structure>();
            if (structure != null && structure.gameObject.name != "BuildGhost" && structure.GetAllowSelectionAndUI())
            {
                SelectStructure(structure);
            }
            else
            {
                DeselectCurrent();
            }
        }
        else
        {
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
    
    // Animate selection if BuildingSelector is present
    SelectionAnimation selector = structure.GetComponent<SelectionAnimation>();
    if (selector == null)
    {
        // selector.AnimateSelect();
        selector = structure.gameObject.AddComponent<SelectionAnimation>();
    }
    selector.AnimateSelect();
    
    if (StructureUIManager.Instance == null)
    {
        Debug.LogError("StructureUIManager.Instance is null! Make sure StructureUIManager is in the scene.");
        return;
    }
    
    StructureUIManager.Instance.ShowStructureUI(structure);
}

    public void DeselectCurrent()
    {
        if (currentSelectedStructure != null)
        {
            currentSelectedStructure.Deselect();
            StructureUIManager.Instance.HideStructureUI();
            currentSelectedStructure = null;
        }
    }
}
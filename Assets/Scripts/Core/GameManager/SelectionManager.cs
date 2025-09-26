using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    private Structure currentSelectedStructure;

    private void Update()
    {
        // Check if BuildController is in delete mode and skip processing
        BuildController buildController = FindFirstObjectByType<BuildController>();
        if (buildController != null && buildController.IsDeleteModeActive())
        {
            return; // Let BuildController handle the input
        }
        
        // Check if any barracks is placing a flag and skip processing
        if (BarracksStructureUI.IsAnyBarracksPlacingFlag())
        {
            return; // Let BarracksStructureUI handle the flag placement input
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            bool isOverUI = EventSystem.current.IsPointerOverGameObject();
            if (isOverUI)
            {
                return;
            }


            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layerMask = ~(1 << LayerMask.NameToLayer("Ignore Raycast"));
            if (Physics.Raycast(ray, out RaycastHit hit, 1000f, layerMask))
            {
                Debug.Log($"Raycast hit: {hit.transform.name}");
                Transform hitTransform = hit.transform;
                while (hitTransform != null)
                {
                    if (hitTransform.name.StartsWith("Item_"))
                    {
                        Structure structure = hitTransform.GetComponent<Structure>();
                        if (structure != null && structure.gameObject.name != "BuildGhost" &&
                            structure.GetAllowSelectionAndUI() && structure.GetCurrentHealth() > 0)
                        {
                            Debug.Log($"Found valid structure: {structure.name}");
                            SelectStructure(structure);
                            return;
                        }
                    }
                    hitTransform = hitTransform.parent;
                }

                DeselectCurrent();
            }
            else
            {
                DeselectCurrent();
            }
        }

        if (Input.GetMouseButtonDown(1))
        {
            bool isOverUI = EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"Right-click detected - Over UI: {isOverUI}");
            if (!isOverUI)
            {
                DeselectCurrent();
            }
            else
            {
                Debug.Log("Right-click deselection blocked: Pointer over UI");
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
            StructureUIManager.Instance?.HideStructureUI();
            currentSelectedStructure = null;
        }
        else
        {
            Debug.Log("DeselectCurrent called but no structure was selected - this is normal after UI close button");
        }
    }

    public void ForceClearSelection()
    {
        currentSelectedStructure = null;
    }

    public Structure GetCurrentSelectedStructure()
    {
        return currentSelectedStructure;
    }
}
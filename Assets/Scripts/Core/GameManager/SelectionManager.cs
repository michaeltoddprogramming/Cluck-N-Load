using UnityEngine;
using UnityEngine.EventSystems;

public class SelectionManager : MonoBehaviour
{
    private Structure currentSelectedStructure;

    private void Update()
    {
        // Temporarily disable BuildController checks to resolve compilation issue
        // BuildController buildController = FindFirstObjectByType<BuildController>();
        // if (buildController != null && (buildController.IsBuildModeActive() || buildController.IsMoveModeActiveProperty || buildController.IsDeleteModeActive()))
        // {
        //     return;
        // }

        if (Input.GetMouseButtonDown(0))
        {
            // Check if pointer is over UI
            bool isOverUI = EventSystem.current.IsPointerOverGameObject();
            Debug.Log($"Mouse click - Over UI: {isOverUI}");
            if (isOverUI)
            {
                Debug.Log("Selection blocked: Pointer over UI");
                return;
            }

            Debug.Log($"Mouse click detected. Current selected structure: {currentSelectedStructure?.name ?? "null"}");

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

                // If we hit something but no valid structure, deselect
                Debug.Log("Hit something but no valid structure found, deselecting");
                DeselectCurrent();
            }
            else
            {
                // If we didn't hit anything, deselect
                Debug.Log("No raycast hit, deselecting");
                DeselectCurrent();
            }
        }

        // Right-click to deselect when not in build mode
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
        Debug.Log($"Selected structure: {structure.name}");
    }

    public void DeselectCurrent()
    {
        Debug.Log($"DeselectCurrent called. Current structure: {currentSelectedStructure?.name ?? "null"}");
        if (currentSelectedStructure != null)
        {
            currentSelectedStructure.Deselect();
            StructureUIManager.Instance?.HideStructureUI();
            currentSelectedStructure = null;
            Debug.Log("Deselected current structure");
        }
        else
        {
            Debug.Log("DeselectCurrent called but no structure was selected - this is normal after UI close button");
        }
    }

    // Public method to force clear selection state (called by UI when closed)
    public void ForceClearSelection()
    {
        // Structure is already deselected by StructureUIManager, just clear the reference
        currentSelectedStructure = null;
        Debug.Log("Force cleared selection state");
    }

    public Structure GetCurrentSelectedStructure()
    {
        return currentSelectedStructure;
    }
}
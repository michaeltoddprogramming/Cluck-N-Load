using UnityEngine;
using System.Collections.Generic;

public class GhostPlacer : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private Material ghostMaterial;

    private GameObject ghostInstance;
    private GridDataGenerator gridDataGenerator;
    private ShopPanelUI shopPanel;
    private Quaternion currentRotation = Quaternion.identity;

    private void Start()
    {
        gridDataGenerator = FindFirstObjectByType<GridDataGenerator>();
        if (gridDataGenerator == null)
        {
            Debug.LogError("No GridDataGenerator found in scene.");
            enabled = false;
            return;
        }

        shopPanel = FindFirstObjectByType<ShopPanelUI>();
        if (shopPanel == null)
        {
            Debug.LogWarning("No ShopPanelUI found in scene. Ghost placement may not respect shop state.");
        }
    }

    private void OnDestroy()
    {
        // Clean up ghost instance when component is destroyed
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }
    }

    public void UpdateGhostAtCell(int x, int y)
    {
        // Don't show ghost if shop is closed or no ghost exists
        if (ghostInstance == null || (shopPanel != null && !shopPanel.IsShopOpen()))
        {
            if (ghostInstance != null)
                ghostInstance.SetActive(false);
            return;
        }

        // Check cell validity
        if (!IsValidPlacementCell(x, y))
        {
            ghostInstance.SetActive(false);
            return;
        }

        // Get all cells that would be occupied by this structure
        List<Vector2Int> footprint = GetStructureFootprint(ghostInstance, x, y);
        
        // Check if ALL cells in the footprint are valid
        foreach (Vector2Int cell in footprint)
        {
            if (!IsValidPlacementCell(cell.x, cell.y))
            {
                ghostInstance.SetActive(false);
                return;
            }
        }

        // All cells are valid, position and show the ghost
        Vector3 center = GetCellCenterFromTexture(x, y);
        ghostInstance.transform.position = center;
        ghostInstance.transform.rotation = currentRotation;
        ghostInstance.SetActive(true);
    }

    public void SetGhostPrefab(GameObject prefab)
    {
        if (ghostInstance != null)
        {
            Destroy(ghostInstance);
        }

        if (prefab == null)
        {
            ghostInstance = null;
            return;
        }

        ghostInstance = Instantiate(prefab);
        ghostInstance.name = "GhostItem";
        
        // Apply ghost material to all renderers
        ApplyGhostMaterial(ghostInstance);
        
        ghostInstance.SetActive(false);
    }

    public void RotateGhost(float yRotation)
    {
        currentRotation *= Quaternion.Euler(0, yRotation, 0);
        
        if (ghostInstance != null)
        {
            ghostInstance.transform.rotation = currentRotation;
        }
    }

    private void ApplyGhostMaterial(GameObject obj)
    {
        if (ghostMaterial == null)
        {
            return;
        }

        foreach (Renderer renderer in obj.GetComponentsInChildren<Renderer>())
        {
            renderer.material = ghostMaterial;
        }
    }

    private List<Vector2Int> GetStructureFootprint(GameObject structure, int centerX, int centerY)
    {
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        
        // Get the object's bounds in world space
        Renderer[] renderers = structure.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) 
        {
            // If no renderers found, just consider the center cell
            occupiedCells.Add(new Vector2Int(centerX, centerY));
            return occupiedCells;
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        // Convert bounds to grid coordinates
        Vector2Int bottomLeft = WorldToGridCoords(bounds.min);
        Vector2Int topRight = WorldToGridCoords(bounds.max);

        // Add all cells within the boundary
        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                occupiedCells.Add(new Vector2Int(x, y));
            }
        }

        return occupiedCells;
    }

    private Vector2Int WorldToGridCoords(Vector3 worldPos)
    {
        Vector4 origin = gridDataGenerator.GetGridOrigin();
        Vector4 worldSize = gridDataGenerator.GetGridWorldSize();
        int gridW = gridDataGenerator.GetGridWidth();
        int gridH = gridDataGenerator.GetGridHeight();

        float u = Mathf.InverseLerp(origin.x, origin.x + worldSize.x, worldPos.x);
        float v = Mathf.InverseLerp(origin.y, origin.y + worldSize.y, worldPos.z);
        
        int cellX = Mathf.FloorToInt(u * gridW);
        int cellY = Mathf.FloorToInt(v * gridH);

        return new Vector2Int(cellX, cellY);
    }

    private bool IsValidPlacementCell(int x, int y)
    {
        if (x < 0 || x >= gridDataGenerator.GetGridWidth() ||
            y < 0 || y >= gridDataGenerator.GetGridHeight())
            return false;

        GridCell cell = gridDataGenerator.GetCell(x, y);
        return cell.flags.isOwned && !cell.flags.isOccupied;
    }

    private Vector3 GetCellCenterFromTexture(int x, int y)
    {
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();
        Vector4 gridOrigin = gridDataGenerator.GetGridOrigin();
        float cellSize = gridDataGenerator.cellSize;

        float textureCellWidth = 1f / gridWidth;
        float textureCellHeight = 1f / gridHeight;
        float textureCenterX = (x + 0.5f) * textureCellWidth;
        float textureCenterY = (y + 0.5f) * textureCellHeight;

        float worldX = gridOrigin.x + textureCenterX * gridDataGenerator.GetGridWorldSize().x;
        float worldZ = gridOrigin.y + textureCenterY * gridDataGenerator.GetGridWorldSize().y;

        float worldY = gridDataGenerator.GetCell(x, y).worldPosition.y;

        return new Vector3(worldX, worldY, worldZ);
    }
}
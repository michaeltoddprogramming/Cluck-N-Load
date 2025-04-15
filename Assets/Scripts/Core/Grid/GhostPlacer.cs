using UnityEngine;

public class GhostPlacer : MonoBehaviour
{
    [Header("Ghost Settings")]
    [SerializeField] private GameObject ghostPrefab;

    private GameObject ghostInstance;
    private GridDataGenerator gridDataGenerator;

    private void Start()
    {
        gridDataGenerator = FindObjectOfType<GridDataGenerator>();
        if (gridDataGenerator == null)
        {
            Debug.LogError("No GridDataGenerator found in scene.");
            enabled = false;
            return;
        }

        if (ghostPrefab != null)
        {
            ghostInstance = Instantiate(ghostPrefab);
            ghostInstance.name = "GhostItem";
            ghostInstance.SetActive(false);
        }
    }

    public void UpdateGhostAtCell(int x, int y)
    {
        if (ghostInstance == null || !IsValidPlacementCell(x, y))
        {
            ghostInstance?.SetActive(false);
            return;
        }

        Vector3 center = GetCellCenterFromTexture(x, y);
        ghostInstance.transform.position = center;
        ghostInstance.SetActive(true);
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

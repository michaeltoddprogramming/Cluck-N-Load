using UnityEngine;

public class OwnershipController : MonoBehaviour
{
    [Header("Grid References")]
    public GridController gridController;
    public GridDataGenerator gridDataGenerator;

    [Header("Ownership Center")]
    [Tooltip("If assigned, this transform's position will be used as center point")]
    public Transform centerBuilding;
    [Tooltip("Used if centerBuilding is not assigned")]
    public Vector3 centerPosition;

    [Header("Ownership Settings")]
    [Range(1, 50)]
    public int ownershipRadius = 10;
    [Tooltip("0 = perfect circle, 1 = perfect square")]
    [Range(0, 1)]
    public float shapeBlend = 0.5f;

    [Header("Debug")]
    public bool visualizeInEditor = true;
    public Color gizmoColor = new Color(0, 1, 0, 0.3f);
    public bool logDebugInfo = false; // Disabled by default for performance
    
    [Header("Performance Settings")]
    [SerializeField] private bool enableRealTimeUpdates = true; // Can disable for potato devices
    [SerializeField] private float updateInterval = 0.1f; // Throttle updates
    
    [Header("Grid Monitoring")]
    [SerializeField] private GridMonitor gridMonitor;
    
    // Event that fires when shop is open to ensure ownership updates are visible
    private ShopPanelUI shopPanelUI;
    
    // Track changes for real-time updating
    private int lastRadius;
    private float lastShapeBlend;
    private Vector3 lastCenterPosition;
    private Transform lastCenterBuilding;
    
    // Used to track manually purchased cells
    private bool[,] manuallyOwnedCells;
    
    // Performance tracking
    private float lastUpdateTime = 0f;

    private void Start()
    {
        // Save initial values for comparison
        lastRadius = ownershipRadius;
        lastShapeBlend = shapeBlend;
        lastCenterPosition = centerPosition;
        lastCenterBuilding = centerBuilding;
        
        // Find references if not assigned
        if (gridController == null)
            gridController = FindFirstObjectByType<GridController>();
            
        if (gridDataGenerator == null && gridController != null)
            gridDataGenerator = gridController.GetComponent<GridDataGenerator>();

        if (gridController == null || gridDataGenerator == null)
        {
            Debug.LogError("Grid references not found. Ownership controller cannot function.");
            return;
        }
        
        // Connect to shop events
        shopPanelUI = FindFirstObjectByType<ShopPanelUI>(FindObjectsInactive.Include);
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.AddListener(HandleShopOpened);
        }

        // Initialize the manually owned cells array
        InitializeManualOwnership();
        
        // Wait for one frame to ensure grid is initialized
        Invoke("ApplyOwnership", 0.1f);

        // Initial visibility calculation should happen after applying ownership
        Invoke("UpdateCellVisibility", 0.2f);
        
        // Find grid monitor if not assigned
        if (gridMonitor == null)
            gridMonitor = FindFirstObjectByType<GridMonitor>();
    }
    
    private void InitializeManualOwnership()
    {
        if (gridDataGenerator == null || !gridDataGenerator.IsInitialized) return;
        
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        manuallyOwnedCells = new bool[width, height];
    }

    private void OnDestroy()
    {
        // Cleanup event listener
        if (shopPanelUI != null)
        {
            shopPanelUI.OnShopOpened.RemoveListener(HandleShopOpened);
        }
    }

    private void HandleShopOpened()
    {
        if (logDebugInfo)
            // Only refresh the grid texture without recalculating ownership
        RefreshGridTexture();
    }
    
    private void Update()
    {
        // Performance optimization: Only update if real-time updates are enabled
        if (!enableRealTimeUpdates) return;
        
        // Throttle updates based on updateInterval
        if (Time.time - lastUpdateTime < updateInterval) return;
        lastUpdateTime = Time.time;
        
        // Check if any ownership parameters changed
        bool radiusChanged = lastRadius != ownershipRadius;
        bool shapeChanged = !Mathf.Approximately(lastShapeBlend, shapeBlend);
        bool centerPosChanged = lastCenterPosition != centerPosition;
        bool centerBuildingChanged = lastCenterBuilding != centerBuilding;
        
        if (radiusChanged || shapeChanged || centerPosChanged || centerBuildingChanged)
        {
            // Update last values
            lastRadius = ownershipRadius;
            lastShapeBlend = shapeBlend;
            lastCenterPosition = centerPosition;
            lastCenterBuilding = centerBuilding;
            
            // Real-time update of ownership
            ApplyOwnership();
        }
    }

    // This method now PRESERVES manually purchased land
    public void ApplyOwnership()
    {
        if (gridDataGenerator == null || !gridDataGenerator.IsInitialized)
        {
            Invoke("ApplyOwnership", 0.5f);
            return;
        }

        // Initialize manual ownership array if needed
        if (manuallyOwnedCells == null) 
            InitializeManualOwnership();

        // Get the center point in world space
        Vector3 center = (centerBuilding != null) ? centerBuilding.position : centerPosition;
        
        // Convert to grid coordinates
        Vector2Int centerCell = gridController.WorldToGridCoords(center);
            
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();

        // Calculate squared radius (for optimization)
        float radiusSquared = ownershipRadius * ownershipRadius;
        
        // Store current ownership before resetting
        bool[,] wasOwned = new bool[gridWidth, gridHeight];
        
        // 1. Save any manually set ownerships to preserve them
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    // Remember if this cell was owned
                    wasOwned[x, y] = cell.flags.isOwned;
                    
                    // Don't lose manual ownerships
                    if (cell.flags.isOwned && manuallyOwnedCells[x, y])
                    {
                        continue; // Keep manual ownership
                    }
                    
                    // Reset automatically owned cells
                    cell.flags.isOwned = false;
                }
            }
        }

        int cellsChanged = 0;
        
        // 2. Apply ownership radius (automatic ownership)
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                // Skip if manually owned already
                if (manuallyOwnedCells[x, y]) continue;
                
                // Calculate distance from center (both circle and square metrics)
                float dx = x - centerCell.x;
                float dy = y - centerCell.y;
                
                // Circle distance (Euclidean)
                float circleDistance = (dx * dx + dy * dy);
                
                // Square distance (Chebyshev)
                float squareDistance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                
                // Blend between circle and square distance
                float blendedDistance = Mathf.Lerp(
                    circleDistance / radiusSquared,  // Normalized circle distance
                    squareDistance / ownershipRadius, // Normalized square distance
                    shapeBlend
                );
                
                // If within radius, set as owned
                if (blendedDistance <= 1.0f)
                {
                    GridCell cell = gridDataGenerator.GetCell(x, y);
                    if (cell != null && !cell.flags.isOwned)
                    {
                        cell.flags.isOwned = true;
                        cellsChanged++;
                    }
                }
            }
        }

        if (logDebugInfo)
            // After ownership is applied, update visibility
        UpdateCellVisibility();
        
        // Update the grid texture
        RefreshGridTexture();
        
        if (gridMonitor != null && cellsChanged > 0)
        {
            gridMonitor.ScanEntireGridForChanges(); // Scan all changes at once
        }
    }
    
    // DISABLED: Land buying functionality removed
    /*
    public void BuyLandAtPosition(Vector3 worldPosition)
    {
        if (gridDataGenerator == null || !gridDataGenerator.IsInitialized) return;
        
        // Initialize manual ownership array if needed
        if (manuallyOwnedCells == null) 
            InitializeManualOwnership();
        
        // Convert world position to grid coordinates
        Vector2Int cellCoords = gridController.WorldToGridCoords(worldPosition);
        
        if (gridController.IsValidCell(cellCoords.x, cellCoords.y))
        {
            // Get cell at position
            GridCell cell = gridDataGenerator.GetCell(cellCoords.x, cellCoords.y);
            if (cell == null) return;
            
            // IMPORTANT: Only allow buying visible cells that aren't already owned
            if (!cell.flags.isVisible)
            {
                if (logDebugInfo)
                    return;
            }
            
            if (cell.flags.isOwned)
            {
                if (logDebugInfo)
                    return;
            }
            
            // Set cell as owned and mark it as manually purchased
            cell.flags.isOwned = true;
            manuallyOwnedCells[cellCoords.x, cellCoords.y] = true;
            
            // Notify grid monitor
            if (gridMonitor != null)
            {
                gridMonitor.NotifyCell

gridMonitor.NotifyCellChanged(cellCoords.x, cellCoords.y, GridChangeType.Ownership);
            }
            
            if (logDebugInfo)
                // Update visibility after buying to expand the visible area
            UpdateCellVisibility();
            
            // Update the grid texture
            RefreshGridTexture();
        }
    }
    */

    // Just update the texture without recalculating ownership
    private void RefreshGridTexture()
    {
        if (gridController != null)
        {
            // Force an update of the grid texture 
            gridController.UpdateGridTexture();
        }
    }

    // Visualize in editor
    private void OnDrawGizmos()
    {
        if (!visualizeInEditor) return;

        Vector3 center = (centerBuilding != null) ? centerBuilding.position : centerPosition;
        
        // Draw a blended shape between circle and square
        int segments = 72;
        float angleStep = 2 * Mathf.PI / segments;
        
        Vector3 prevPoint = Vector3.zero;
        
        for (int i = 0; i <= segments; i++)
        {
            float angle = i * angleStep;
            float cosAngle = Mathf.Cos(angle);
            float sinAngle = Mathf.Sin(angle);
            
            // Circle point
            Vector3 circlePoint = new Vector3(
                cosAngle * ownershipRadius,
                0.05f,  // Slight elevation
                sinAngle * ownershipRadius
            );
            
            // Square point
            float maxComp = Mathf.Max(Mathf.Abs(cosAngle), Mathf.Abs(sinAngle));
            Vector3 squarePoint = new Vector3(
                (cosAngle / maxComp) * ownershipRadius,
                0.05f,
                (sinAngle / maxComp) * ownershipRadius
            );
            
            // Blend between circle and square
            Vector3 blendedPoint = Vector3.Lerp(circlePoint, squarePoint, shapeBlend);
            
            // Move to center position
            blendedPoint += center;
            
            if (i > 0)
            {
                Gizmos.color = gizmoColor;
                Gizmos.DrawLine(prevPoint, blendedPoint);
            }
            
            prevPoint = blendedPoint;
        }
    }

    // Button to manually apply ownership (useful for editor testing)
    [ContextMenu("Apply Ownership")]
    public void ManualApplyOwnership()
    {
        ApplyOwnership();
    }

    // Reset all cells to not owned (for testing)
    [ContextMenu("Clear All Ownership")]
    public void ClearAllOwnership()
    {
        if (gridDataGenerator == null || !gridDataGenerator.IsInitialized) return;

        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();

        // Re-initialize manual ownership tracking
        manuallyOwnedCells = new bool[gridWidth, gridHeight];

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    cell.flags.isOwned = false;
                }
            }
        }

        RefreshGridTexture();
    }

    // Add this method to calculate cell visibility
    private void UpdateCellVisibility()
    {
        if (gridDataGenerator == null || !gridDataGenerator.IsInitialized) return;
        
        int gridWidth = gridDataGenerator.GetGridWidth();
        int gridHeight = gridDataGenerator.GetGridHeight();
        
        // First pass: Set all cells to invisible
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    cell.flags.isVisible = false;
                }
            }
        }
        
        // Second pass: Mark owned cells as visible
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null && cell.flags.isOwned)
                {
                    // Mark this cell as visible
                    cell.flags.isVisible = true;
                    
                    // Mark all neighboring cells as visible
                    for (int nx = Mathf.Max(0, x-1); nx <= Mathf.Min(gridWidth-1, x+1); nx++)
                    {
                        for (int ny = Mathf.Max(0, y-1); ny <= Mathf.Min(gridHeight-1, y+1); ny++)
                        {
                            GridCell neighbor = gridDataGenerator.GetCell(nx, ny);
                            if (neighbor != null)
                            {
                                neighbor.flags.isVisible = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
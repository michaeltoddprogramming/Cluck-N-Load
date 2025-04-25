using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GridMonitor acts as a central communication hub between grid systems and AI systems.
/// It observes grid changes, throttles notifications, and dispatches events to subscribers.
/// </summary>
public class GridMonitor : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GridController gridController;
    [SerializeField] private GridDataGenerator gridDataGenerator;
    
    [Header("Throttling Settings")]
    [SerializeField] private float updateThrottleTime = 0.5f;
    [SerializeField] private bool debugLogging = false;
    
    // Events that systems can subscribe to
    public event Action<GridChangeType> OnGridChanged;
    public event Action<Vector2Int> OnCellOccupied;
    public event Action<Vector2Int> OnCellCleared;
    public event Action<List<Vector2Int>> OnMultipleCellsChanged;
    
    // Flags to track pending updates
    private bool updatePending = false;
    private HashSet<Vector2Int> changedCells = new HashSet<Vector2Int>();
    private HashSet<GridChangeType> pendingChangeTypes = new HashSet<GridChangeType>();
    
    // Snapshot of the grid state used to detect changes
    private bool[,] occupancySnapshot;
    private bool[,] visibilitySnapshot;
    private bool[,] ownershipSnapshot;
    
    // References to other systems
    private FlowFieldGenerator flowFieldGenerator;
    
    private void Awake()
    {
        // Find references if not assigned
        if (gridController == null)
            gridController = FindObjectOfType<GridController>();
            
        if (gridDataGenerator == null && gridController != null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            
        if (gridController == null || gridDataGenerator == null)
        {
            Debug.LogError("GridMonitor could not find required grid components.");
            enabled = false;
            return;
        }
    }
    
    private void Start()
    {
        // Wait for grid to initialize before taking snapshot
        StartCoroutine(InitializeWhenGridReady());
        
        // Find and cache FlowFieldGenerator reference - to be removed once fully decoupled
        flowFieldGenerator = FindObjectOfType<FlowFieldGenerator>();
    }
    
    private IEnumerator InitializeWhenGridReady()
    {
        // Wait until grid data is fully initialized
        while (gridDataGenerator == null || !gridDataGenerator.IsInitialized)
        {
            yield return null;
        }
        
        // Initialize snapshots
        TakeGridSnapshot();
        
        if (debugLogging)
            Debug.Log("GridMonitor initialized and ready to track changes");
    }
    
    /// <summary>
    /// Creates a snapshot of the current grid state for comparison
    /// </summary>
    private void TakeGridSnapshot()
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        // Initialize snapshot arrays
        occupancySnapshot = new bool[width, height];
        visibilitySnapshot = new bool[width, height];
        ownershipSnapshot = new bool[width, height];
        
        // Store current state
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell != null)
                {
                    occupancySnapshot[x, y] = cell.flags.isOccupied;
                    visibilitySnapshot[x, y] = cell.flags.isVisible;
                    ownershipSnapshot[x, y] = cell.flags.isOwned;
                }
            }
        }
    }
    
    /// <summary>
    /// Notifies the monitor that a cell's state has changed
    /// </summary>
    public void NotifyCellChanged(int x, int y, GridChangeType changeType)
    {
        if (!gridController.IsValidCell(x, y)) return;
        
        Vector2Int cellPos = new Vector2Int(x, y);
        
        // Add to pending changes
        changedCells.Add(cellPos);
        pendingChangeTypes.Add(changeType);
        
        // Schedule update if not already pending
        if (!updatePending)
        {
            updatePending = true;
            StartCoroutine(ThrottledGridUpdate());
        }
        
        if (debugLogging)
            Debug.Log($"Grid change registered at ({x}, {y}): {changeType}");
    }
    
    /// <summary>
    /// Notifies the monitor that multiple cells have changed at once
    /// </summary>
    public void NotifyMultipleCellsChanged(List<Vector2Int> cells, GridChangeType changeType)
    {
        bool anyValidCells = false;
        
        foreach (Vector2Int cell in cells)
        {
            if (gridController.IsValidCell(cell.x, cell.y))
            {
                changedCells.Add(cell);
                anyValidCells = true;
            }
        }
        
        if (anyValidCells)
        {
            pendingChangeTypes.Add(changeType);
            
            // Schedule update if not already pending
            if (!updatePending)
            {
                updatePending = true;
                StartCoroutine(ThrottledGridUpdate());
            }
            
            if (debugLogging)
                Debug.Log($"Multiple grid changes registered ({cells.Count} cells): {changeType}");
        }
    }
    
    /// <summary>
    /// Force an immediate grid update regardless of throttling
    /// </summary>
    public void ForceGridUpdate()
    {
        if (debugLogging)
            Debug.Log("Forcing immediate grid update");
            
        StopAllCoroutines();
        ProcessGridChanges();
    }
    
    /// <summary>
    /// Delays grid updates to prevent excessive processing when many changes happen rapidly
    /// </summary>
    private IEnumerator ThrottledGridUpdate()
    {
        // Wait for throttle time to collect potential additional changes
        yield return new WaitForSeconds(updateThrottleTime);
        
        // Process all accumulated changes
        ProcessGridChanges();
    }
    
    /// <summary>
    /// Processes all pending grid changes and notifies subscribers
    /// </summary>
    private void ProcessGridChanges()
    {
        if (changedCells.Count == 0)
        {
            updatePending = false;
            return;
        }
        
        if (debugLogging)
            Debug.Log($"Processing {changedCells.Count} grid changes");
        
        // Check for changes by comparing against snapshot
        List<Vector2Int> occupiedCells = new List<Vector2Int>();
        List<Vector2Int> clearedCells = new List<Vector2Int>();
        bool structuralChanges = false;
        
        foreach (Vector2Int cell in changedCells)
        {
            GridCell gridCell = gridDataGenerator.GetCell(cell.x, cell.y);
            if (gridCell == null) continue;
            
            // Check for occupancy changes
            if (gridCell.flags.isOccupied != occupancySnapshot[cell.x, cell.y])
            {
                if (gridCell.flags.isOccupied)
                    occupiedCells.Add(cell);
                else
                    clearedCells.Add(cell);
                    
                structuralChanges = true;
            }
            
            // Update snapshot with current values
            occupancySnapshot[cell.x, cell.y] = gridCell.flags.isOccupied;
            visibilitySnapshot[cell.x, cell.y] = gridCell.flags.isVisible;
            ownershipSnapshot[cell.x, cell.y] = gridCell.flags.isOwned;
        }
        
        // Notify subscribers of changes
        if (structuralChanges && OnGridChanged != null)
        {
            OnGridChanged(GridChangeType.Structural);
            
            // Legacy direct connection - to be removed once other systems are updated
            if (flowFieldGenerator != null)
            {
                flowFieldGenerator.GenerateFlowFieldManually();
            }
        }
        
        // Fire individual events
        if (occupiedCells.Count > 0 && OnMultipleCellsChanged != null)
        {
            OnMultipleCellsChanged(occupiedCells);
            
            // Also fire individual events
            foreach (Vector2Int cell in occupiedCells)
            {
                if (OnCellOccupied != null)
                    OnCellOccupied(cell);
            }
        }
        
        if (clearedCells.Count > 0 && OnMultipleCellsChanged != null)
        {
            OnMultipleCellsChanged(clearedCells);
            
            // Also fire individual events
            foreach (Vector2Int cell in clearedCells)
            {
                if (OnCellCleared != null)
                    OnCellCleared(cell);
            }
        }
        
        // Broadcast all accumulated change types
        foreach (GridChangeType changeType in pendingChangeTypes)
        {
            if (OnGridChanged != null)
                OnGridChanged(changeType);
        }
        
        // Clear pending changes
        changedCells.Clear();
        pendingChangeTypes.Clear();
        updatePending = false;
    }
    
    /// <summary>
    /// Detects changes in the entire grid by comparing current state to snapshot
    /// </summary>
    public void ScanEntireGridForChanges()
    {
        if (debugLogging)
            Debug.Log("Scanning entire grid for changes");
            
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                GridCell cell = gridDataGenerator.GetCell(x, y);
                if (cell == null) continue;
                
                // Check for any changes
                if (cell.flags.isOccupied != occupancySnapshot[x, y] ||
                    cell.flags.isVisible != visibilitySnapshot[x, y] ||
                    cell.flags.isOwned != ownershipSnapshot[x, y])
                {
                    NotifyCellChanged(x, y, GridChangeType.Structural);
                }
            }
        }
    }
}

/// <summary>
/// Describes the type of changes that can occur on the grid
/// </summary>
public enum GridChangeType
{
    Structural,      // Buildings/obstacles built or destroyed
    Visibility,      // Cell visibility changed
    Ownership,       // Cell ownership changed
    FlowField,       // Flow field values changed
    Integration      // Integration field values changed
}
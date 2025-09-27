
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

public class DefenseStructure : Structure
{
    [Header("Defense Structure Settings")]
    [Tooltip("Prefab for the connector object (fence segment, wire, etc.)")]
    [SerializeField] private GameObject connectorPrefab;
    
    [Tooltip("Offset from center where connectors should be positioned")]
    [SerializeField] private float connectorOffset = 0.5f;
    
    [Tooltip("Height at which connectors should be placed")]
    [SerializeField] private float connectorHeight = 1.0f;
    
    // Track active connectors and their directions
    private Dictionary<Vector2Int, GameObject> activeConnectors = new Dictionary<Vector2Int, GameObject>();
    
    // Static registry using grid positions for O(1) lookup
    private static Dictionary<Vector2Int, DefenseStructure> defenseRegistry = new Dictionary<Vector2Int, DefenseStructure>();
    
    // Grid directions for neighbor checking (4-directional)
    private static readonly Vector2Int[] neighborDirections = {
        Vector2Int.up,     // North
        Vector2Int.right,  // East  
        Vector2Int.down,   // South
        Vector2Int.left    // West
    };

    private Vector2Int myGridPosition;
    private bool isRegistered = false;

    protected override void Start()
    {
        base.Start();
        
        // Delay registration to ensure proper positioning
        StartCoroutine(DelayedRegistration());
        
        Debug.Log($"DefenseStructure started, will register after delay");
    }

    private System.Collections.IEnumerator DelayedRegistration()
    {
        // Wait a frame to ensure transform is properly set
        yield return null;
        
        // Register this defense structure in the grid-based registry
        RegisterInRegistry();
    }

    protected override void OnDestroy()
    {
        // Unregister and clean up
        UnregisterFromRegistry();
        base.OnDestroy();
    }

    private void RegisterInRegistry()
    {
        myGridPosition = GetGridPosition();
        Debug.Log($"Attempting to register DefenseStructure at world position {transform.position} -> grid position {myGridPosition}");
        
        if (myGridPosition != new Vector2Int(-1, -1) && !isRegistered)
        {
            defenseRegistry[myGridPosition] = this;
            isRegistered = true;
            
            Debug.Log($"Successfully registered DefenseStructure at grid position: {myGridPosition}. Total registered: {defenseRegistry.Count}");
            
            // Update connectors for this structure and immediate neighbors only
            UpdateConnectors();
            UpdateImmediateNeighbors();
        }
        else
        {
            Debug.LogError($"Failed to register DefenseStructure. Grid position: {myGridPosition}, Already registered: {isRegistered}");
        }
    }

    private void UnregisterFromRegistry()
    {
        if (isRegistered)
        {
            Debug.Log($"Unregistering DefenseStructure at grid position: {myGridPosition}");
            
            // Clean up connectors
            ClearAllConnectors();
            
            // Remove from registry
            defenseRegistry.Remove(myGridPosition);
            isRegistered = false;
            
            // Update only immediate neighbors
            UpdateImmediateNeighbors();
            
            Debug.Log($"Successfully unregistered. Remaining structures: {defenseRegistry.Count}");
        }
    }

    // Efficient method to update only immediate neighbors (4 structures max)
    private void UpdateImmediateNeighbors()
    {
        foreach (Vector2Int direction in neighborDirections)
        {
            Vector2Int neighborPos = myGridPosition + direction;
            if (defenseRegistry.TryGetValue(neighborPos, out DefenseStructure neighbor))
            {
                neighbor.UpdateConnectors();
            }
        }
    }

    // Main method to update connectors based on neighbors - now very efficient
    public void UpdateConnectors()
    {
        if (connectorPrefab == null) 
        {
            Debug.LogWarning($"ConnectorPrefab is null for DefenseStructure at {myGridPosition}");
            return;
        }

        Debug.Log($"Updating connectors for DefenseStructure at {myGridPosition}");

        // Check each direction for neighbors using O(1) dictionary lookup
        foreach (Vector2Int direction in neighborDirections)
        {
            Vector2Int neighborPos = myGridPosition + direction;
            
            // Check if neighbor exists and is valid
            if (defenseRegistry.TryGetValue(neighborPos, out DefenseStructure neighbor))
            {
                if (neighbor != null && neighbor.gameObject != null && neighbor != this)
                {
                    // Create connector if we don't have one in this direction
                    if (!activeConnectors.ContainsKey(direction))
                    {
                        Debug.Log($"  Creating connector from {myGridPosition} to {neighborPos} in direction {direction}");
                        CreateConnector(direction);
                    }
                    else
                    {
                        Debug.Log($"  Connector already exists in direction {direction}");
                    }
                }
                else
                {
                    Debug.LogWarning($"  Invalid neighbor at {neighborPos}, removing from registry");
                    defenseRegistry.Remove(neighborPos);
                    // Remove any connector in that direction
                    if (activeConnectors.ContainsKey(direction))
                    {
                        DestroyConnector(direction);
                    }
                }
            }
            else
            {
                Debug.Log($"  No neighbor in direction {direction} at {neighborPos}");
                // Remove any connector in that direction if no neighbor exists
                if (activeConnectors.ContainsKey(direction))
                {
                    Debug.Log($"    Removing connector in direction {direction} - no neighbor");
                    DestroyConnector(direction);
                }
            }
        }
        
        Debug.Log($"  Final active connectors: {activeConnectors.Count}");
        foreach (var kvp in activeConnectors)
        {
            Debug.Log($"    Active connector: {kvp.Key} -> {kvp.Value?.name ?? "NULL"}");
        }
    }

    // Create a connector in the specified direction
    private void CreateConnector(Vector2Int direction)
    {
        if (connectorPrefab == null) return;

        // Calculate connector position
        Vector3 connectorWorldPos = transform.position + new Vector3(
            direction.x * connectorOffset,
            connectorHeight,
            direction.y * connectorOffset
        );

        // Create connector object
        GameObject connector = Instantiate(connectorPrefab, connectorWorldPos, Quaternion.identity, transform);
        
        // Rotate connector to face the correct direction
        // Convert grid direction to world rotation
        Quaternion rotation = Quaternion.identity;
        
        if (direction == Vector2Int.up)         // North (forward in Unity)
            rotation = Quaternion.Euler(0, 0, 0);
        else if (direction == Vector2Int.right) // East (right in Unity)  
            rotation = Quaternion.Euler(0, 90, 0);
        else if (direction == Vector2Int.down)  // South (backward in Unity)
            rotation = Quaternion.Euler(0, 180, 0);
        else if (direction == Vector2Int.left)  // West (left in Unity)
            rotation = Quaternion.Euler(0, 270, 0);

        connector.transform.rotation = rotation;

        // Store connector reference
        activeConnectors[direction] = connector;
        
        Debug.Log($"Created connector at {myGridPosition} pointing {direction} with rotation {rotation.eulerAngles}");
    }

    // Destroy connector in the specified direction
    private void DestroyConnector(Vector2Int direction)
    {
        if (activeConnectors.TryGetValue(direction, out GameObject connector))
        {
            if (connector != null)
            {
                DestroyImmediate(connector);
            }
            activeConnectors.Remove(direction);
        }
    }

    // Clear all connectors (used on destroy)
    private void ClearAllConnectors()
    {
        foreach (var kvp in activeConnectors)
        {
            if (kvp.Value != null)
            {
                DestroyImmediate(kvp.Value);
            }
        }
        activeConnectors.Clear();
    }

    // Get the grid position of this structure (cached and validated)
    private Vector2Int GetGridPosition()
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogError("GridController not found!");
            return new Vector2Int(-1, -1);
        }

        Vector2Int gridPos = gridController.WorldToGridCoords(transform.position);
        
        // Add validation to ensure the grid position makes sense
        Vector3 cellCenter = gridController.GetCellCenterFromTexture(gridPos.x, gridPos.y);
        float distance = Vector3.Distance(transform.position, cellCenter);
        
        if (distance > 2.0f) // If structure is more than 2 units away from cell center, something is wrong
        {
            Debug.LogWarning($"DefenseStructure at world pos {transform.position} mapped to grid pos {gridPos}, but cell center is {cellCenter} (distance: {distance:F2})");
        }
        
        Debug.Log($"DefenseStructure '{name}' at world {transform.position} -> grid {gridPos} (cell center: {cellCenter})");
        return gridPos;
    }

    // Public method to manually refresh connectors (lightweight now)
    public void RefreshConnectors()
    {
        UpdateConnectors();
    }

    // Static method to debug the registry state
    public static void DebugRegistry()
    {
        Debug.Log($"DefenseStructure Registry State - Total structures: {defenseRegistry.Count}");
        foreach (var kvp in defenseRegistry)
        {
            Vector2Int pos = kvp.Key;
            DefenseStructure defense = kvp.Value;
            if (defense != null)
            {
                Vector3 worldPos = defense.transform.position;
                Debug.Log($"  Grid pos {pos} -> World pos {worldPos} (Structure: {defense.name})");
            }
            else
            {
                Debug.LogWarning($"  Grid pos {pos} -> NULL STRUCTURE (should clean up)");
            }
        }
    }

    // Static method to clean up any null or destroyed entries in the registry
    public static void CleanupRegistry()
    {
        var keysToRemove = new List<Vector2Int>();
        
        foreach (var kvp in defenseRegistry)
        {
            if (kvp.Value == null || kvp.Value.gameObject == null)
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            Debug.LogWarning($"Removing stale registry entry at {key}");
            defenseRegistry.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up {keysToRemove.Count} stale registry entries. Remaining: {defenseRegistry.Count}");
        }
    }

    // Static method to completely rebuild all connectors from scratch
    public static void RebuildAllConnectors()
    {
        Debug.Log($"RebuildAllConnectors called - clearing all connectors first");
        
        // First, clear all existing connectors from all structures
        foreach (var kvp in defenseRegistry)
        {
            if (kvp.Value != null)
            {
                kvp.Value.ClearAllConnectors();
            }
        }
        
        // Clean up any stale entries
        CleanupRegistry();
        
        // Now rebuild connectors for all structures
        foreach (var kvp in defenseRegistry)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateConnectors();
            }
        }
        
        Debug.Log($"Finished rebuilding connectors for {defenseRegistry.Count} structures");
    }

    // Static method to refresh all defense structure connectors (only when needed)
    public static void RefreshAllDefenseConnectors()
    {
        Debug.Log($"RefreshAllDefenseConnectors called for {defenseRegistry.Count} structures");
        
        // Use the rebuild method to ensure clean state
        RebuildAllConnectors();
    }

    // Static method to get defense structure at position (O(1) lookup)
    public static DefenseStructure GetDefenseAt(Vector2Int gridPosition)
    {
        defenseRegistry.TryGetValue(gridPosition, out DefenseStructure defense);
        return defense;
    }

    // Static method to check if position has defense structure (O(1) lookup)
    public static bool HasDefenseAt(Vector2Int gridPosition)
    {
        return defenseRegistry.ContainsKey(gridPosition);
    }
}
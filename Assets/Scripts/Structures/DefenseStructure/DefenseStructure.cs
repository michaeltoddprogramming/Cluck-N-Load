
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
        
        // Only register if this is not a ghost object
        if (!IsGhostObject())
        {
            // Register this defense structure in the grid-based registry
            RegisterInRegistry();
        }
        else
        {
            Debug.Log($"DefenseStructure is ghost object - skipping registration and connectors");
        }
    }

    // Check if this is a ghost object (part of building system preview)
    public bool IsGhostObject()
    {
        // Primary check: if this DefenseStructure component is disabled, it's likely a ghost
        if (!this.enabled)
        {
            Debug.Log($"DefenseStructure {gameObject.name} detected as ghost - component disabled");
            return true;
        }

        // Check if this object has ghost/preview indicators in its name
        string name = gameObject.name.ToLower();
        if (name.Contains("ghost") || 
            name.Contains("preview") ||
            name.Contains("(clone)") ||
            name.Contains("temp") ||
            name.Contains("placeholder"))
        {
            Debug.Log($"DefenseStructure {gameObject.name} detected as ghost by name");
            return true;
        }

        // Check if any parent has ghost naming
        Transform parent = transform.parent;
        while (parent != null)
        {
            string parentName = parent.name.ToLower();
            if (parentName.Contains("ghost") || 
                parentName.Contains("preview") ||
                parentName.Contains("temp") ||
                parentName.Contains("placeholder"))
            {
                Debug.Log($"DefenseStructure {gameObject.name} detected as ghost by parent {parent.name}");
                return true;
            }
            parent = parent.parent;
        }

        // Additional check: if the object has no collider or is disabled, might be ghost
        Collider objectCollider = GetComponent<Collider>();
        if (objectCollider == null || !objectCollider.enabled)
        {
            Debug.Log($"DefenseStructure {gameObject.name} detected as ghost - no enabled collider");
            return true;
        }

        return false;
    }

    protected override void OnDestroy()
    {
        // Unregister and clean up
        UnregisterFromRegistry();
        base.OnDestroy();
    }

    private void RegisterInRegistry()
    {
        // Double-check we're not registering a ghost
        if (IsGhostObject())
        {
            Debug.LogWarning($"Attempted to register ghost object {gameObject.name} - BLOCKED");
            return;
        }

        myGridPosition = GetGridPosition();
        Debug.Log($"Attempting to register DefenseStructure {gameObject.name} at world position {transform.position} -> grid position {myGridPosition}");
        
        if (myGridPosition != new Vector2Int(-1, -1) && !isRegistered)
        {
            // Final safety check - make sure no ghost is already at this position
            if (defenseRegistry.ContainsKey(myGridPosition))
            {
                DefenseStructure existing = defenseRegistry[myGridPosition];
                if (existing != null && existing.IsGhostObject())
                {
                    Debug.LogWarning($"Removing ghost object at {myGridPosition} to make room for real structure");
                    defenseRegistry.Remove(myGridPosition);
                }
            }

            defenseRegistry[myGridPosition] = this;
            isRegistered = true;
            
            Debug.Log($"Successfully registered DefenseStructure {gameObject.name} at grid position: {myGridPosition}. Total registered: {defenseRegistry.Count}");
            
            // Update connectors for this structure and immediate neighbors only
            UpdateConnectors();
            UpdateImmediateNeighbors();
        }
        else
        {
            Debug.LogError($"Failed to register DefenseStructure {gameObject.name}. Grid position: {myGridPosition}, Already registered: {isRegistered}");
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

        // Don't create connectors for ghost objects
        if (IsGhostObject())
        {
            Debug.Log($"DefenseStructure at {myGridPosition} is ghost - skipping connector update");
            return;
        }

        Debug.Log($"Updating connectors for DefenseStructure at {myGridPosition}");

        // Check each direction for neighbors using O(1) dictionary lookup
        // NOTE: We ignore grid obstacle states and only care about actual DefenseStructure objects in registry
        foreach (Vector2Int direction in neighborDirections)
        {
            Vector2Int neighborPos = myGridPosition + direction;
            
            // Check if neighbor exists and is valid in the DefenseStructure registry
            // This automatically ignores obstacle states, ghost objects, and non-defense structures
            if (defenseRegistry.TryGetValue(neighborPos, out DefenseStructure neighbor))
            {
                if (neighbor != null && neighbor.gameObject != null && neighbor != this && !neighbor.IsGhostObject())
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
                Debug.Log($"  No defense structure neighbor in direction {direction} at {neighborPos}");
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
                bool isGhost = defense.IsGhostObject();
                Debug.Log($"  Grid pos {pos} -> World pos {worldPos} (Structure: {defense.name}, IsGhost: {isGhost})");
                
                if (isGhost)
                {
                    Debug.LogError($"    ^^^ GHOST OBJECT FOUND IN REGISTRY - THIS SHOULD NOT HAPPEN!");
                }
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
            if (kvp.Value == null || kvp.Value.gameObject == null || kvp.Value.IsGhostObject())
            {
                keysToRemove.Add(kvp.Key);
            }
        }
        
        foreach (var key in keysToRemove)
        {
            Debug.LogWarning($"Removing stale/ghost registry entry at {key}");
            defenseRegistry.Remove(key);
        }
        
        if (keysToRemove.Count > 0)
        {
            Debug.Log($"Cleaned up {keysToRemove.Count} stale/ghost registry entries. Remaining: {defenseRegistry.Count}");
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
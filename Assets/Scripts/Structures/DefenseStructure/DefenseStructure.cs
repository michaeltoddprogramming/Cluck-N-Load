
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
        
        // Register this defense structure in the grid-based registry
        RegisterInRegistry();
        
        Debug.Log($"DefenseStructure started at grid position: {myGridPosition}");
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
            // Clean up connectors
            ClearAllConnectors();
            
            // Remove from registry
            defenseRegistry.Remove(myGridPosition);
            isRegistered = false;
            
            // Update only immediate neighbors
            UpdateImmediateNeighbors();
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
        if (connectorPrefab == null) return;

        Debug.Log($"Updating connectors for DefenseStructure at {myGridPosition}");

        // Check each direction for neighbors using O(1) dictionary lookup
        foreach (Vector2Int direction in neighborDirections)
        {
            Vector2Int neighborPos = myGridPosition + direction;
            bool hasNeighbor = defenseRegistry.ContainsKey(neighborPos);

            Debug.Log($"  Checking direction {direction}: neighbor at {neighborPos} = {hasNeighbor}");

            if (hasNeighbor && !activeConnectors.ContainsKey(direction))
            {
                // Create connector in this direction
                Debug.Log($"    Creating connector in direction {direction}");
                CreateConnector(direction);
            }
            else if (!hasNeighbor && activeConnectors.ContainsKey(direction))
            {
                // Remove connector in this direction
                Debug.Log($"    Removing connector in direction {direction}");
                DestroyConnector(direction);
            }
            else if (hasNeighbor && activeConnectors.ContainsKey(direction))
            {
                Debug.Log($"    Connector already exists in direction {direction}");
            }
        }
        
        Debug.Log($"  Total active connectors: {activeConnectors.Count}");
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

    // Get the grid position of this structure (cached)
    private Vector2Int GetGridPosition()
    {
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            Debug.LogError("GridController not found!");
            return new Vector2Int(-1, -1);
        }

        return gridController.WorldToGridCoords(transform.position);
    }

    // Public method to manually refresh connectors (lightweight now)
    public void RefreshConnectors()
    {
        UpdateConnectors();
    }

    // Static method to refresh all defense structure connectors (only when needed)
    public static void RefreshAllDefenseConnectors()
    {
        // Only update structures that exist in the registry
        foreach (var kvp in defenseRegistry)
        {
            if (kvp.Value != null)
            {
                kvp.Value.UpdateConnectors();
            }
        }
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
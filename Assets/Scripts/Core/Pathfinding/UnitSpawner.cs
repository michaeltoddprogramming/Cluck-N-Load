using UnityEngine;
using System.Collections.Generic;

public class UnitSpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    [Tooltip("Unit prefabs to spawn - must have or will get FlowFieldAgent component")]
    public GameObject[] unitPrefabs;
    [Tooltip("Currently selected prefab index")]
    public int selectedPrefabIndex = 0;
    [Tooltip("Height offset above the terrain to spawn units")]
    public float spawnHeightOffset = 0.1f;
    [Tooltip("Layers to detect for unit placement")]
    public LayerMask placementLayerMask = -1;

    [Header("Flow Field References")]
    [Tooltip("Optional - will find automatically if not set")]
    public FlowFieldGenerator flowFieldGenerator;
    [Tooltip("Optional - will find automatically if not set")]
    public GridController gridController;

    [Header("Debug")]
    public bool showDebugInfo = true;
    public Color debugColor = Color.yellow;
    public float debugSphereRadius = 0.25f;
    public float debugDuration = 1f;

    [Header("Spawn Statistics")]
    [SerializeField] private int spawnCount = 0;
    private List<GameObject> spawnedUnits = new List<GameObject>();

    private void Start()
    {
        // Auto-find references if not set
        if (flowFieldGenerator == null)
            flowFieldGenerator = FindObjectOfType<FlowFieldGenerator>();
            
        if (gridController == null)
            gridController = FindObjectOfType<GridController>();
            
        // Validate prefabs
        ValidatePrefabs();
    }

    private void ValidatePrefabs()
    {
        if (unitPrefabs == null || unitPrefabs.Length == 0)
        {
            Debug.LogWarning("No unit prefabs assigned to UnitSpawner!");
            return;
        }

        for (int i = 0; i < unitPrefabs.Length; i++)
        {
            if (unitPrefabs[i] == null)
            {
                Debug.LogWarning($"Null prefab at index {i} in UnitSpawner!");
                continue;
            }

            // Check if prefab has FlowFieldAgent
            if (unitPrefabs[i].GetComponent<FlowFieldAgent>() == null)
            {
                Debug.LogWarning($"Prefab '{unitPrefabs[i].name}' doesn't have FlowFieldAgent component. It will be added at runtime.");
            }
        }
    }

    private void Update()
    {
        // Check for Shift+RMB
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetMouseButtonDown(1))
        {
            SpawnUnitAtMousePosition();
        }

        // Optional key for cycling through prefabs
        if (Input.GetKeyDown(KeyCode.Tab) && unitPrefabs.Length > 0)
        {
            selectedPrefabIndex = (selectedPrefabIndex + 1) % unitPrefabs.Length;
            Debug.Log($"Selected prefab: {unitPrefabs[selectedPrefabIndex].name}");
        }
    }

    private void SpawnUnitAtMousePosition()
    {
        if (unitPrefabs == null || unitPrefabs.Length == 0 || selectedPrefabIndex >= unitPrefabs.Length)
        {
            Debug.LogWarning("No valid prefab selected for spawning");
            return;
        }

        // Get mouse position in world
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f, placementLayerMask))
        {
            // Get the selected prefab
            GameObject prefab = unitPrefabs[selectedPrefabIndex];
            if (prefab == null) return;

            // Calculate spawn position with height offset
            Vector3 spawnPosition = hit.point + new Vector3(0, spawnHeightOffset, 0);
            
            // Check if position is within valid grid
            Vector2Int gridCoords = gridController != null 
                ? gridController.WorldToGridCoords(spawnPosition) 
                : new Vector2Int(0, 0);
                
            bool isValidCell = gridController != null && gridController.IsValidCell(gridCoords.x, gridCoords.y);
            
            // Only spawn if we have a valid grid position or if we don't have a grid controller
            if (isValidCell || gridController == null)
            {
                // Instantiate the prefab
                GameObject newUnit = Instantiate(prefab, spawnPosition, Quaternion.identity);
                newUnit.name = $"{prefab.name}_{spawnCount++}";
                
                // Make sure it has a FlowFieldAgent component
                FlowFieldAgent agent = newUnit.GetComponent<FlowFieldAgent>();
                if (agent == null)
                {
                    agent = newUnit.AddComponent<FlowFieldAgent>();
                    Debug.Log($"Added FlowFieldAgent component to {newUnit.name}");
                }
                
                // Assign references using the public setter methods
                if (gridController != null)
                    agent.SetGridController(gridController);
                    
                if (flowFieldGenerator != null)
                    agent.SetFlowFieldGenerator(flowFieldGenerator);

                // Add to tracking list
                spawnedUnits.Add(newUnit);
                
                // Debug spawn visual
                if (showDebugInfo)
                {
                    Debug.DrawRay(spawnPosition, Vector3.up * 2f, debugColor, debugDuration);
                    Debug.Log($"Spawned unit: {newUnit.name} at {spawnPosition}");
                }
            }
            else
            {
                if (showDebugInfo)
                {
                    Debug.LogWarning($"Cannot spawn unit: Position {spawnPosition} is not on a valid grid cell");
                    Debug.DrawRay(spawnPosition, Vector3.up * 2f, Color.red, debugDuration);
                }
            }
        }
    }

    [ContextMenu("Clear All Spawned Units")]
    public void ClearAllSpawnedUnits()
    {
        foreach (var unit in spawnedUnits)
        {
            if (unit != null)
                Destroy(unit);
        }
        
        spawnedUnits.Clear();
        spawnCount = 0;
        Debug.Log("All spawned units cleared");
    }

    private void OnDrawGizmos()
    {
        if (!showDebugInfo) return;
        
        // Draw a sphere at the most recent spawn point if we have units
        if (spawnedUnits.Count > 0 && spawnedUnits[spawnedUnits.Count - 1] != null)
        {
            Gizmos.color = debugColor;
            Gizmos.DrawWireSphere(spawnedUnits[spawnedUnits.Count - 1].transform.position, debugSphereRadius);
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using FarmDefender.Core.AI.FlowField; // Add this line for the new namespace

public class UnitSpawner : MonoBehaviour 
{
    [SerializeField] private UnitDatabase _unitDatabase;
    [SerializeField] private Transform[] _spawnPoints;
    [SerializeField] private float _spawnHeightOffset = 0.1f;
    
    // DEVELOPMENT-ONLY references for testing - will be refactored later
    [Header("Development Testing")]
    [Tooltip("TEMPORARY: Reference to the flow field generator for spawn positioning")]
    [SerializeField] private FlowFieldManager flowFieldManager; // Changed from FlowFieldGenerator
    [Tooltip("TEMPORARY: Reference to the grid data generator for spawn positioning")]
    [SerializeField] private GridDataGenerator _gridDataGenerator;
    [Tooltip("TEMPORARY: Number of units to spawn when using development shortcuts")]
    [SerializeField] private int _devSpawnCount = 5;
    [Tooltip("TEMPORARY: Radius around target for military unit spawning")]
    [SerializeField] private float _militarySpawnRadius = 5f;
    [Tooltip("TEMPORARY: Inset from grid edge for hostile spawning")]
    [SerializeField] private float _edgeInset = 1f;

    private void Start()
    {
        // Find required components if not assigned
        if (flowFieldManager == null)
            flowFieldManager = FindObjectOfType<FlowFieldManager>();
            
        // Rest of Start method stays the same
    }

    // Original methods
    public Unit SpawnUnitOfType(UnitType type, Vector3 position) 
    {
        if (_unitDatabase == null) 
        {
            Debug.LogError("Unit database not assigned to spawner");
            return null;
        }
        
        UnitData randomData = _unitDatabase.GetRandomUnitOfType(type);
        if (randomData == null) return null;
        
        Vector3 spawnPos = position + Vector3.up * _spawnHeightOffset;
        return UnitFactory.CreateUnit(randomData, spawnPos);
    }
    
    public Unit SpawnRandomEnemy() 
    {
        if (_spawnPoints.Length == 0) return null;
        
        Transform spawnPoint = _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        return SpawnUnitOfType(UnitType.Hostile, spawnPoint.position);
    }
    
    public void SpawnEnemyWave(int count) 
    {
        for (int i = 0; i < count; i++) 
        {
            SpawnRandomEnemy();
        }
    }

    // DEVELOPMENT-ONLY methods for testing - will be refactored later
    private void Update()
    {
        // TEMPORARY: Development shortcuts for spawning units
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {
            if (Input.GetKeyDown(KeyCode.H))
            {
                // Debug.Log($"[DEV] Spawning {_devSpawnCount} hostile units at grid edges");
                SpawnHostileUnitsAtEdges(_devSpawnCount);
            }
            else if (Input.GetKeyDown(KeyCode.M))
            {
                // Debug.Log($"[DEV] Spawning {_devSpawnCount} military units around target");
                SpawnMilitaryUnitsAroundTarget(_devSpawnCount);
            }
        }
    }

    // DEVELOPMENT-ONLY: Spawn hostile units at grid edges
    public void SpawnHostileUnitsAtEdges(int count)
    {
        if (_gridDataGenerator == null)
        {
            // Debug.LogError("[DEV] Grid data generator reference missing for hostile spawning");
            return;
        }

        List<Vector3> edgePositions = GetRandomEdgePositions(count);
        foreach (Vector3 position in edgePositions)
        {
            SpawnUnitOfType(UnitType.Hostile, position);
        }
    }

    // DEVELOPMENT-ONLY: Spawn military units around flow field target
    public void SpawnMilitaryUnitsAroundTarget(int count)
    {
        if (flowFieldManager == null)
        {
            Debug.LogError("[DEV] Flow field generator reference missing for military spawning");
            return;
        }

        Vector2Int targetCoord = flowFieldManager.GetTargetCoordinates();
        GridCell targetCell = null;
        
        // Get the grid cell at target coordinates
        if (_gridDataGenerator != null && 
            targetCoord.x >= 0 && targetCoord.x < _gridDataGenerator.GetGridWidth() &&
            targetCoord.y >= 0 && targetCoord.y < _gridDataGenerator.GetGridHeight())
        {
            targetCell = _gridDataGenerator.GetCell(targetCoord.x, targetCoord.y);
        }

        if (targetCell == null)
        {
            Debug.LogWarning("[DEV] Invalid target for military unit spawning");
            return;
        }

        Vector3 targetPosition = targetCell.worldPosition;
        
        for (int i = 0; i < count; i++)
        {
            // Generate random position in circle around target
            float angle = Random.Range(0f, Mathf.PI * 2f);
            float radius = Random.Range(1f, _militarySpawnRadius);
            Vector3 offset = new Vector3(
                Mathf.Cos(angle) * radius,
                0f,
                Mathf.Sin(angle) * radius
            );

            Vector3 spawnPosition = targetPosition + offset;
            
            // Check if position is valid on the grid
            if (IsValidSpawnPosition(spawnPosition))
            {
                SpawnUnitOfType(UnitType.Military, spawnPosition);
            }
            else
            {
                // Try again if position is invalid
                i--;
            }
        }
    }

    // DEVELOPMENT-ONLY: Get random positions on the grid edges
    private List<Vector3> GetRandomEdgePositions(int count)
    {
        List<Vector3> positions = new List<Vector3>();
        
        if (_gridDataGenerator == null)
            return positions;
            
        int width = _gridDataGenerator.GetGridWidth();
        int height = _gridDataGenerator.GetGridHeight();
        
        for (int i = 0; i < count; i++)
        {
            // Choose which edge (0=top, 1=right, 2=bottom, 3=left)
            int edge = Random.Range(0, 4);
            int x, y;
            
            switch (edge)
            {
                case 0: // Top edge
                    x = Random.Range(0, width);
                    y = height - 1;
                    break;
                case 1: // Right edge
                    x = width - 1;
                    y = Random.Range(0, height);
                    break;
                case 2: // Bottom edge
                    x = Random.Range(0, width);
                    y = 0;
                    break;
                case 3: // Left edge
                    x = 0;
                    y = Random.Range(0, height);
                    break;
                default:
                    x = 0;
                    y = 0;
                    break;
            }
            
            // Get the cell and use its position
            GridCell cell = _gridDataGenerator.GetCell(x, y);
            if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
            {
                // Apply a small inset from the edge
                Vector3 position = cell.worldPosition;
                
                // Inset the position slightly from the edge
                if (edge == 0) position.z -= _edgeInset;
                else if (edge == 1) position.x -= _edgeInset;
                else if (edge == 2) position.z += _edgeInset;
                else if (edge == 3) position.x += _edgeInset;
                
                positions.Add(position);
            }
            else
            {
                // Try again if the position is invalid
                i--;
            }
        }
        
        return positions;
    }

    // DEVELOPMENT-ONLY: Check if a position is valid for spawning
    private bool IsValidSpawnPosition(Vector3 position)
    {
        if (_gridDataGenerator == null)
            return false;
            
        // Convert world position to grid coordinates
        Vector2Int gridCoord = flowFieldManager.GridController.WorldToGridCoords(position);
        
        // Check if coordinates are within grid bounds
        if (gridCoord.x < 0 || gridCoord.x >= _gridDataGenerator.GetGridWidth() ||
            gridCoord.y < 0 || gridCoord.y >= _gridDataGenerator.GetGridHeight())
            return false;
            
        // Get the cell and check if it's valid for spawning
        GridCell cell = _gridDataGenerator.GetCell(gridCoord.x, gridCoord.y);
        return cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied;
    }
}
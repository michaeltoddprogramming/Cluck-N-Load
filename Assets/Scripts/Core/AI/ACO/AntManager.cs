using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    public class AntManager : MonoBehaviour
    {
        [Header("Ant Settings")]
        [Tooltip("Minimum number of ants to spawn")]
        [SerializeField] private int maxAnts = 10;
        [SerializeField] private int minStructuresPerAnt = 3;
        [SerializeField] private float baseFlowFieldDesirability = 0.2f;
        [Tooltip("Set to 0 for instant algorithm execution")]
        [SerializeField] private float antSpeed = 8f;
        [Tooltip("How long ants stay alive before expiring (seconds)")]
        [SerializeField] private float antLifetime = 30f;
        
        [Header("Visualization")]
        [Tooltip("Enable to visualize ant movement (for debugging)")]
        [SerializeField] private bool visualizeAnts = false;
        [SerializeField] private bool showDiscoveredStructures = true;
        [SerializeField] private Color discoveredStructureColor = Color.cyan;
        [SerializeField] private Color exploringColor = Color.green;
        [SerializeField] private Color returningColor = Color.yellow;
        [SerializeField] private Color snoopingColor = Color.magenta;
        
        [Header("Spawn Settings")]
        [SerializeField] private float spawnDistanceFromEdge = 2f;
        
        [Header("Debug")]
        [SerializeField] private bool showStructureDebug = true;
        
        [Header("Adaptation Settings")]
        [Tooltip("Time (seconds) before flow field influence starts increasing")]
        [SerializeField] private float timeBeforeFlowFieldIncrease = 5f;
        [Tooltip("How quickly flow field influence grows when no structures found")]
        [SerializeField] private float flowFieldInfluenceGrowthRate = 0.05f;
        [Tooltip("Maximum flow field influence during snooping")]
        [SerializeField] private float maxSnoopingFlowFieldInfluence = 0.6f;
        
        [Header("Pheromone Settings")]
        [SerializeField] private float pheromoneLayInterval = 0.2f;
        [SerializeField] private float basePheromoneStrength = 1f;
        [SerializeField] private bool scalePheromonesByStructures = true;
        [SerializeField] private float maxPheromoneStrength = 5f;
        [SerializeField] private int pheromoneSpreadRadius = 1;
        [SerializeField] private float[] pheromoneSpreadFactors = new float[] { 1.0f, 0.5f, 0.25f }; // Center, adjacent, diagonal
        [SerializeField] private bool enablePheromoneVisualization = true; // Flag to enable/disable visualization

        [Header("Pheromone Statistics")]
        [SerializeField] private int totalPheromonesLaid = 0;
        [SerializeField] private float averageStrength = 0f;
        [SerializeField] private float highestStrength = 0f;
        [SerializeField] private int cellsWithPheromones = 0;

        // Structure tracking collections
        private HashSet<Vector2Int> allPlayerStructures = new HashSet<Vector2Int>();
        private HashSet<Vector2Int> remainingStructuresToFind = new HashSet<Vector2Int>();
        
        // References
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        private FarmDefender.Core.AI.FlowField.FlowFieldManager flowFieldManager;
        private GridMonitor gridMonitor;
        
        // Internal state
        private List<VirtualAnt> virtualAnts = new List<VirtualAnt>();
        private HashSet<Vector2Int> discoveredStructures = new HashSet<Vector2Int>();
        private int totalStructuresInWorld = 0;
        private bool hasCountedStructures = false;
        private float updateInterval = 0.1f; // How often to update the virtual ants
        private float timeSinceLastUpdate = 0f;
        private float structureSearchRadius = 2f;
        private float pheromoneStrength = 1f;
        private int defaultEnemyTypeIndex = 0; // 0=regular, 1=fast, 2=strong
        private float randomMovementFactor = 0.3f;
        
        private void Start()
        {
            gridController = FindObjectOfType<GridController>();
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            flowFieldManager = FindObjectOfType<FarmDefender.Core.AI.FlowField.FlowFieldManager>();
            gridMonitor = FindObjectOfType<GridMonitor>();
            
            if (gridController == null || gridDataGenerator == null || flowFieldManager == null)
            {
                Debug.LogError("AntManager is missing required references");
                enabled = false;
                return;
            }
            
            // Subscribe to grid monitor events to detect when buildings are placed/removed
            if (gridMonitor != null)
            {
                gridMonitor.OnCellOccupied += HandleCellOccupied;
                gridMonitor.OnCellCleared += HandleCellCleared;
            }
            
            // Initial scan for structures
            UpdatePlayerStructures();
            
            SetupPheromoneVisualizer();
        }
        
        private void OnDestroy()
        {
            // Unsubscribe from events
            if (gridMonitor != null)
            {
                gridMonitor.OnCellOccupied -= HandleCellOccupied;
                gridMonitor.OnCellCleared -= HandleCellCleared;
            }
        }
        
        private void HandleCellOccupied(Vector2Int cell)
        {
            GridCell gridCell = gridController.GetCell(cell.x, cell.y);
            if (gridCell != null && gridCell.flags.isOwned && gridCell.flags.isOccupied)
            {
                // A new structure was placed
                allPlayerStructures.Add(cell);
                remainingStructuresToFind.Add(cell);
                
                if (showStructureDebug)
                {
                    Debug.Log($"New structure detected at {cell}, adding to structures to find. " +
                              $"Total: {allPlayerStructures.Count}, Remaining: {remainingStructuresToFind.Count}");
                }
            }
        }
        
        private void HandleCellCleared(Vector2Int cell)
        {
            // A structure was removed
            allPlayerStructures.Remove(cell);
            remainingStructuresToFind.Remove(cell);
            
            if (showStructureDebug)
            {
                Debug.Log($"Structure removed at {cell}. " +
                          $"Total: {allPlayerStructures.Count}, Remaining: {remainingStructuresToFind.Count}");
            }
        }
        
        private void Update()
        {
            // Check for keyboard shortcut (Shift + P)
            if ((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) && Input.GetKeyDown(KeyCode.P))
            {
                TriggerAnts();
                Debug.Log("ACO algorithm triggered with Shift+P");
            }
            
            // Remove regular ant update logic - we only use the fast algorithm now
        }
        
        // Method to scan for all player structures in the world
        public void UpdatePlayerStructures()
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            // Store the current list to detect removed structures
            HashSet<Vector2Int> oldStructures = new HashSet<Vector2Int>(allPlayerStructures);
            allPlayerStructures.Clear();
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null && cell.flags.isOccupied && cell.flags.isOwned)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        allPlayerStructures.Add(pos);
                        
                        // If this is a new structure, also add it to the remaining structures
                        if (!oldStructures.Contains(pos))
                        {
                            remainingStructuresToFind.Add(pos);
                        }
                    }
                }
            }
            
            if (showStructureDebug)
            {
                Debug.Log($"Updated player structures: {allPlayerStructures.Count} total, {remainingStructuresToFind.Count} remaining to find");
            }
        }
        
        private void CheckReturnConditions(VirtualAnt ant)
        {
            // Only return if we've found our minimum number of structures
            // or there are no more structures to find
            bool minimumStructuresFound = ant.discoveredStructures.Count >= minStructuresPerAnt;
            
            // Only consider global completion if ALL structures are actually discovered
            // Don't just rely on remainingStructuresToFind.Count == 0
            bool allStructuresFound = discoveredStructures.Count >= allPlayerStructures.Count;
            bool noMoreStructures = remainingStructuresToFind.Count == 0 && allStructuresFound;
            
            if (!ant.isReturning && (minimumStructuresFound || noMoreStructures))
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                
                if (showStructureDebug)
                {
                    string reason = minimumStructuresFound ? 
                        $"found minimum required structures ({ant.discoveredStructures.Count})" : 
                        "all structures found";
                    
                    Debug.Log($"Ant returning: {reason}. Structures found: {ant.discoveredStructures.Count}");
                }
                return;
            }
            
            // Return if we've reached flow field target and found no structures
            if (!ant.isReturning && HasReachedFlowFieldTarget(ant) && ant.discoveredStructures.Count == 0)
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                
                if (showStructureDebug)
                {
                    Debug.Log("Ant returning: reached target without finding structures");
                }
                return;
            }
            
            // Return if we're out of lifetime
            float initialLifetime = antLifetime; // Assuming default lifetime
            if (!ant.isReturning && ant.lifetime < antLifetime * 0.3f)
            {
                ant.isReturning = true;
                DetermineReturnTarget(ant);
                
                if (showStructureDebug)
                {
                    Debug.Log($"Ant returning: out of time. Structures found: {ant.discoveredStructures.Count}");
                }
                return;
            }
        }
        
        private bool HasReachedFlowFieldTarget(VirtualAnt ant)
        {
            Vector2Int targetCoords = flowFieldManager.GetTargetCoordinates();
            
            // Check if we're at the target coordinates
            return Vector2Int.Distance(ant.position, targetCoords) < 2f;
        }
        
        private void DetermineReturnTarget(VirtualAnt ant)
        {
            // Find closest edge
            Vector2Int gridPos = ant.position;
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            int distToLeft = gridPos.x;
            int distToRight = width - 1 - gridPos.x;
            int distToBottom = gridPos.y;
            int distToTop = height - 1 - gridPos.y;
            
            // Find the shortest distance to an edge
            int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            
            if (minDist == distToLeft)
                ant.targetPosition = new Vector2Int(0, gridPos.y);
            else if (minDist == distToRight)
                ant.targetPosition = new Vector2Int(width - 1, gridPos.y);
            else if (minDist == distToBottom)
                ant.targetPosition = new Vector2Int(gridPos.x, 0);
            else
                ant.targetPosition = new Vector2Int(gridPos.x, height - 1);
        }
        
        private bool IsAtEdge(Vector2Int position)
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            return position.x == 0 || position.x == width - 1 || 
                   position.y == 0 || position.y == height - 1;
        }
        
        private void UpdateExplorationBehavior(VirtualAnt ant)
        {
            // Track visited cells
            if (!ant.visitedCells.Contains(ant.position))
            {
                ant.visitedCells.Add(ant.position);
            }
            
            // Check if we're in owned territory
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            bool wasInOwnedTerritory = ant.isInOwnedTerritory;
            ant.isInOwnedTerritory = cell != null && cell.flags.isOwned;
            
            // If we just entered owned territory, update desirability
            if (!wasInOwnedTerritory && ant.isInOwnedTerritory)
            {
                ant.currentFlowFieldInfluence = baseFlowFieldDesirability;
            }
            
            // Store previous structure count before checking
            int previousStructureCount = ant.discoveredStructures.Count;
            
            // Check for structures around us
            CheckForStructuresAround(ant);
            
            // Did we find a new structure?
            bool foundNewStructure = ant.discoveredStructures.Count > previousStructureCount;
            
            // Update snooping state based on structure discovery
            if (ant.discoveredStructures.Count > 0 && !ant.isSnooping)
            {
                ant.isSnooping = true;
                ant.currentFlowFieldInfluence = 0f; // Start with no flow field influence when snooping
                ant.timeSinceLastStructureFound = 0f;
            }
            
            // Reset timer if we found a new structure
            if (foundNewStructure)
            {
                ant.timeSinceLastStructureFound = 0f;
                
                // Reduce flow field influence when we find structures (prefer local exploration)
                ant.currentFlowFieldInfluence = baseFlowFieldDesirability;
                
                if (showStructureDebug)
                {
                    Debug.Log($"Ant found new structure - resetting flow field influence to {baseFlowFieldDesirability}");
                }
            }
            else
            {
                // Increase timer since we didn't find anything
                ant.timeSinceLastStructureFound += updateInterval;
            }
            
            // When snooping or in owned territory, increase flow field influence over time if no structures found recently
            if (ant.isInOwnedTerritory || ant.isSnooping)
            {
                if (ant.timeSinceLastStructureFound > timeBeforeFlowFieldIncrease)
                {
                    // Gradually increase flow field influence
                    float previousInfluence = ant.currentFlowFieldInfluence;
                    ant.currentFlowFieldInfluence += flowFieldInfluenceGrowthRate * updateInterval;
                    ant.currentFlowFieldInfluence = Mathf.Clamp(ant.currentFlowFieldInfluence, 0f, maxSnoopingFlowFieldInfluence);
                    
                    // Log change if significant
                    if (showStructureDebug && Mathf.Abs(previousInfluence - ant.currentFlowFieldInfluence) > 0.05f)
                    {
                        Debug.Log($"Ant has not found structures for {ant.timeSinceLastStructureFound:F1}s - " + 
                                  $"flow field influence increased to {ant.currentFlowFieldInfluence:F2}");
                    }
                }
            }
            
            // Now decide movement direction based on our state
            if (ant.isInOwnedTerritory)
            {
                // In owned territory: blend between flow field and random exploration
                MoveWithSnoop(ant);
            }
            else
            {
                // Outside owned territory: use flow field
                MoveWithFlowField(ant);
            }
        }
        
        private void CheckForStructuresAround(VirtualAnt ant)
        {
            int radius = (int)structureSearchRadius;
            
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                    
                    // Skip invalid cells
                    if (!gridController.IsValidCell(checkPos.x, checkPos.y))
                        continue;
                        
                    GridCell checkCell = gridController.GetCell(checkPos.x, checkPos.y);
                    if (checkCell != null && checkCell.flags.isOccupied && checkCell.flags.isOwned)
                    {
                        // Check if this structure is still in the remaining structures to find list
                        if (remainingStructuresToFind.Contains(checkPos))
                        {
                            // Found a valid structure, add it to our list
                            if (!ant.discoveredStructures.Contains(checkPos))
                            {
                                ant.discoveredStructures.Add(checkPos);
                                ant.timeSinceLastStructureFound = 0f;
                                
                                // Add to manager's master list of discovered structures
                                discoveredStructures.Add(checkPos);
                                
                                // Remove from the remaining structures to find
                                remainingStructuresToFind.Remove(checkPos);
                                
                                if (showStructureDebug)
                                {
                                    Debug.Log($"Ant found structure at {checkPos}. " +
                                              $"Ant has found {ant.discoveredStructures.Count}/{minStructuresPerAnt} structures. " +
                                              $"Structures left to find: {remainingStructuresToFind.Count}");
                                }
                            }
                        }
                        else if (showStructureDebug && !ant.discoveredStructures.Contains(checkPos))
                        {
                            // Structure already found by another ant
                            Debug.Log($"Ant found structure at {checkPos} but it was already discovered by another ant");
                        }
                    }
                }
            }
        }
        
        private void MoveWithSnoop(VirtualAnt ant)
        {
            Vector2Int newPosition = ant.position;
            
            // Blend between flow field and random exploration
            if (Random.value < ant.currentFlowFieldInfluence)
            {
                // Use flow field
                GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                if (cell != null && cell.flowDirection != Vector2.zero)
                {
                    // Convert flow direction to a grid movement
                    Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                    newPosition = ant.position + flowMove;
                }
            }
            else
            {
                // Look for unvisited cells around current position
                List<Vector2Int> possibleDirections = new List<Vector2Int>();
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        // Skip current cell
                        if (x == 0 && y == 0)
                            continue;
                        
                        // Check if valid and not visited
                        Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                        if (gridController.IsValidCell(checkPos.x, checkPos.y))
                        {
                            // Prefer unvisited cells
                            if (!ant.visitedCells.Contains(checkPos))
                            {
                                // Double the chance for unvisited cells
                                possibleDirections.Add(checkPos);
                                possibleDirections.Add(checkPos);
                            }
                            else
                            {
                                // Already visited, but still an option
                                possibleDirections.Add(checkPos);
                            }
                        }
                    }
                }
                
                // Prefer continuing in previous direction
                if (ant.lastDirection != Vector2Int.zero)
                {
                    Vector2Int continueDir = ant.position + ant.lastDirection;
                    if (gridController.IsValidCell(continueDir.x, continueDir.y))
                    {
                        // Add it a few times to increase probability
                        possibleDirections.Add(continueDir);
                        possibleDirections.Add(continueDir);
                    }
                }
                
                if (possibleDirections.Count > 0)
                {
                    // Randomly select a direction
                    newPosition = possibleDirections[Random.Range(0, possibleDirections.Count)];
                    ant.lastDirection = newPosition - ant.position;
                }
                else
                {
                    // Fallback: use flow field direction
                    GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                    if (cell != null && cell.flowDirection != Vector2.zero)
                    {
                        Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                        newPosition = ant.position + flowMove;
                    }
                }
            }
            
            // Add some randomness if specified
            if (randomMovementFactor > 0 && Random.value < randomMovementFactor)
            {
                int randomX = Random.Range(-1, 2);
                int randomY = Random.Range(-1, 2);
                Vector2Int randomPos = new Vector2Int(ant.position.x + randomX, ant.position.y + randomY);
                
                if (gridController.IsValidCell(randomPos.x, randomPos.y))
                {
                    newPosition = randomPos;
                }
            }
            
            // Ensure we're not moving to an invalid cell
            if (gridController.IsValidCell(newPosition.x, newPosition.y))
            {
                // Only move if the cell isn't an obstacle
                GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                if (targetCell != null && !targetCell.flags.isObstacle)
                {
                    ant.position = newPosition;
                }
            }
        }
        
        private void MoveWithFlowField(VirtualAnt ant)
        {
            // Use flow field to guide toward player base
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            if (cell != null && cell.flowDirection != Vector2.zero)
            {
                // Convert flow direction to a grid movement
                Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                Vector2Int newPosition = ant.position + flowMove;
                
                // Ensure we're not moving to an invalid cell
                if (gridController.IsValidCell(newPosition.x, newPosition.y))
                {
                    // Only move if the cell isn't an obstacle
                    GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                    if (targetCell != null && !targetCell.flags.isObstacle)
                    {
                        ant.position = newPosition;
                    }
                }
            }
        }
        
        private Vector2Int GetGridMoveFromDirection(Vector2 direction)
        {
            // Convert a normalized flow direction to a grid move
            // We need to round to the nearest grid cell move (-1, 0, 1)
            int x = Mathf.RoundToInt(direction.x);
            int y = Mathf.RoundToInt(direction.y);
            
            // If we ended up with (0,0) but had a direction, force a move
            if (x == 0 && y == 0 && direction.magnitude > 0.1f)
            {
                // Take the larger component
                if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
                {
                    x = direction.x > 0 ? 1 : -1;
                }
                else
                {
                    y = direction.y > 0 ? 1 : -1;
                }
            }
            
            return new Vector2Int(x, y);
        }
        
        private void ReturnToEdge(VirtualAnt ant)
        {
            Vector2Int originalPosition = ant.position;
            bool moved = false;
            
            // FIRST STRATEGY: Always check for obstacles in the direct path
            if (!ant.useFlowFieldForReturn)
            {
                // Calculate direction to edge target
                Vector2Int direction = ant.targetPosition - ant.position;
                
                if (direction.sqrMagnitude > 0)
                {
                    Vector2Int moveDir = new Vector2Int(
                        Mathf.Clamp(direction.x, -1, 1),
                        Mathf.Clamp(direction.y, -1, 1)
                    );
                    
                    Vector2Int newPosition = ant.position + moveDir;
                    
                    // Check if this direct move would hit an obstacle
                    if (gridController.IsValidCell(newPosition.x, newPosition.y))
                    {
                        GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                        if (targetCell != null && targetCell.flags.isObstacle)
                        {
                            // Hit an obstacle in direct path - switch to flow field for a significant duration
                            ant.obstacleHitCount++;
                            
                            // The more obstacles we've hit, the longer we use flow field
                            ant.useFlowFieldForReturn = true;
                            // Base duration: 2s, increasing with each obstacle (max 5s)
                            ant.flowFieldUseDuration = Mathf.Min(2.0f + (ant.obstacleHitCount * 0.5f), 5.0f);
                            ant.flowFieldUseTimer = ant.flowFieldUseDuration;
                            
                            if (showStructureDebug)
                            {
                                Debug.Log($"Ant hit obstacle during return. Using ONLY flow field for {ant.flowFieldUseDuration}s. " + 
                                          $"Hit count: {ant.obstacleHitCount}");
                            }
                        }
                    }
                }
            }
            
            // Update flow field timer
            if (ant.flowFieldUseTimer > 0)
            {
                ant.flowFieldUseTimer -= updateInterval;
                ant.useFlowFieldForReturn = true;
                
                // Once timer expires, switch back to direct navigation
                if (ant.flowFieldUseTimer <= 0)
                {
                    ant.useFlowFieldForReturn = false;
                    ant.obstacleHitCount = Mathf.Max(0, ant.obstacleHitCount - 1); // Reduce hit count when successful
                    
                    if (showStructureDebug)
                    {
                        Debug.Log($"Ant returning to direct navigation after using flow field for {ant.flowFieldUseDuration}s");
                    }
                }
            }
            
            // STRATEGY 1: Use flow field when obstacle was encountered
            if (ant.useFlowFieldForReturn)
            {
                GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                if (cell != null && cell.flowDirection != Vector2.zero)
                {
                    // Invert the flow direction to head away from the base
                    Vector2 inverseFlow = -cell.flowDirection;
                    Vector2Int flowMove = GetGridMoveFromDirection(inverseFlow);
                    Vector2Int newPosition = ant.position + flowMove;
                    
                    // Ensure we're not moving to an invalid cell or obstacle
                    if (gridController.IsValidCell(newPosition.x, newPosition.y))
                    {
                        GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                        if (targetCell != null && !targetCell.flags.isObstacle)
                        {
                            ant.position = newPosition;
                            moved = true;
                        }
                        else
                        {
                            // Even flow field hit an obstacle, try emergency moves
                            // But keep using flow field overall (don't reset the timer)
                        }
                    }
                }
            }
            // STRATEGY 2: Direct movement toward edge if not using flow field
            else if (!moved)
            {
                // Calculate direction to edge target
                Vector2Int direction = ant.targetPosition - ant.position;
                
                if (direction.sqrMagnitude > 0)
                {
                    Vector2Int moveDir = new Vector2Int(
                        Mathf.Clamp(direction.x, -1, 1),
                        Mathf.Clamp(direction.y, -1, 1)
                    );
                    
                    Vector2Int newPosition = ant.position + moveDir;
                    
                    // Check if this direct move is valid and not blocked
                    if (gridController.IsValidCell(newPosition.x, newPosition.y))
                    {
                        GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                        if (targetCell != null && !targetCell.flags.isObstacle)
                        {
                            ant.position = newPosition;
                            moved = true;
                        }
                    }
                }
            }
            
            // EMERGENCY STRATEGY: If still stuck, try any valid move
            if (!moved)
            {
                List<Vector2Int> validMoves = new List<Vector2Int>();
                
                // Check all neighbors for valid moves
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        if (x == 0 && y == 0) continue;
                        
                        Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                        if (gridController.IsValidCell(checkPos.x, checkPos.y))
                        {
                            GridCell checkCell = gridController.GetCell(checkPos.x, checkPos.y);
                            if (checkCell != null && !checkCell.flags.isObstacle)
                            {
                                validMoves.Add(checkPos);
                            }
                        }
                    }
                }
                
                // If valid moves found, take one randomly
                if (validMoves.Count > 0)
                {
                    ant.position = validMoves[Random.Range(0, validMoves.Count)];
                    moved = true;
                    
                    // After using emergency move, try flow field for a while
                    if (!ant.useFlowFieldForReturn)
                    {
                        ant.useFlowFieldForReturn = true;
                        ant.flowFieldUseDuration = 2.0f;  // 2 seconds of flow field after emergency move
                        ant.flowFieldUseTimer = ant.flowFieldUseDuration;
                        
                        if (showStructureDebug)
                        {
                            Debug.Log("Ant used emergency move - switching to flow field for 2s");
                        }
                    }
                }
            }
            
            // If this point is reached and the ant hasn't moved, it's completely trapped
            // Let's try again next update - hopefully something will change
        }
        
        private void LayPheromones(VirtualAnt ant)
        {
            // Only lay pheromones when returning and at specified interval
            ant.lastPheromoneTime += updateInterval;
            if (ant.lastPheromoneTime < pheromoneLayInterval)
                return;
                
            ant.lastPheromoneTime = 0f;
            
            if (!gridController.IsValidCell(ant.position.x, ant.position.y))
                return;
                
            // Calculate pheromone strength based on structures found
            float strength = basePheromoneStrength;
            
            // Scale pheromone strength based on how many structures the ant found
            if (scalePheromonesByStructures && ant.discoveredStructures.Count > 0)
            {
                // More structures = stronger pheromone trail
                strength = Mathf.Min(
                    basePheromoneStrength * (1f + ant.discoveredStructures.Count * 0.5f), 
                    maxPheromoneStrength
                );
            }
            
            // Optimization: Skip diffusion for weak pheromones
            int effectiveRadius = strength > 2f ? pheromoneSpreadRadius : 
                                  strength > 1f ? Mathf.Min(1, pheromoneSpreadRadius) : 0;
            
            // Apply pheromones with diffusion to the grid
            ApplyPheromonesWithDiffusion(ant.position, strength, defaultEnemyTypeIndex, effectiveRadius);
        }

        private void ApplyPheromonesWithDiffusion(Vector2Int center, float strength, int enemyType, int radius)
        {
            // Apply to center cell at full strength
            GridCell centerCell = gridController.GetCell(center.x, center.y);
            if (centerCell != null)
            {
                centerCell.pheromones[enemyType] += strength * pheromoneSpreadFactors[0];
            }
            
            // Skip diffusion if radius is 0
            if (radius <= 0)
            {
                // Just update statistics
                totalPheromonesLaid++;
                averageStrength = ((averageStrength * (totalPheromonesLaid - 1)) + strength) / totalPheromonesLaid;
                highestStrength = Mathf.Max(highestStrength, strength);
                return;
            }
            
            // Optimization: Pre-compute factor array indices to avoid bounds checking in loop
            int maxDistFactor = pheromoneSpreadFactors.Length - 1;
            
            // Apply to neighboring cells with diminishing strength
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Skip center cell (already processed)
                    if (x == 0 && y == 0)
                        continue;
                        
                    Vector2Int neighborPos = new Vector2Int(center.x + x, center.y + y);
                    if (!gridController.IsValidCell(neighborPos.x, neighborPos.y))
                        continue;
                        
                    GridCell neighborCell = gridController.GetCell(neighborPos.x, neighborPos.y);
                    if (neighborCell == null)
                        continue;
                        
                    // Calculate distance factor (adjacent or diagonal)
                    int distFactor = (Mathf.Abs(x) + Mathf.Abs(y) == 1) ? 1 : 2;  // 1 for adjacent, 2 for diagonal
                    
                    // Apply pheromone with reduced strength based on distance
                    if (distFactor <= maxDistFactor)
                    {
                        neighborCell.pheromones[enemyType] += strength * pheromoneSpreadFactors[distFactor];
                    }
                }
            }
            
            // Update statistics less frequently to improve performance
            totalPheromonesLaid++;
            if (totalPheromonesLaid % 10 == 0) // Only update averages every 10 pheromones
            {
                averageStrength = ((averageStrength * (totalPheromonesLaid - 1)) + strength) / totalPheromonesLaid;
                highestStrength = Mathf.Max(highestStrength, strength);
            }
            
            // Count cells with pheromones very infrequently to avoid performance hit
            if (totalPheromonesLaid % 500 == 0)
            {
                CountCellsWithPheromones();
            }
        }

        private void CountCellsWithPheromones()
        {
            // Use sampling instead of checking every cell
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            int count = 0;
            int samplesPerAxis = 10; // Sample only 10 cells per axis
            
            int xStep = Mathf.Max(1, width / samplesPerAxis);
            int yStep = Mathf.Max(1, height / samplesPerAxis);
            
            for (int x = 0; x < width; x += xStep)
            {
                for (int y = 0; y < height; y += yStep)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null)
                    {
                        bool hasPheromone = false;
                        for (int i = 0; i < cell.pheromones.Length; i++)
                        {
                            if (cell.pheromones[i] > 0.1f)
                            {
                                hasPheromone = true;
                                break;
                            }
                        }
                        
                        if (hasPheromone)
                            count++;
                    }
                }
            }
            
            // Estimate total based on sampling ratio
            float samplingRatio = (float)(samplesPerAxis * samplesPerAxis) / (width * height);
            cellsWithPheromones = Mathf.RoundToInt(count / samplingRatio);
        }
        
        // Count all player structures in the world
        private int CountTotalStructures()
        {
            if (hasCountedStructures)
                return totalStructuresInWorld;
                
            UpdatePlayerStructures();
            totalStructuresInWorld = allPlayerStructures.Count;
            hasCountedStructures = true;
            return totalStructuresInWorld;
        }
        
        // Determine how many ants to spawn based on structure count
        private int DetermineOptimalAntCount()
        {
            int structureCount = CountTotalStructures();
            
            // Calculate needed ants based on structures and min structures per ant
            int neededAnts = Mathf.CeilToInt((float)structureCount / minStructuresPerAnt);
            
            // Use at least the minimum number specified
            return Mathf.Max(neededAnts, maxAnts);
        }
        
        private void SpawnVirtualAnt()
        {
            // Get a random edge position
            Vector2Int spawnPos = GetRandomEdgeCellPosition();
            
            // Create a new virtual ant
            VirtualAnt ant = new VirtualAnt(spawnPos, antLifetime); // 30 seconds lifetime
            virtualAnts.Add(ant);
        }
        
        private Vector2Int GetRandomEdgeCellPosition()
        {
            int gridWidth = gridDataGenerator.GetGridWidth();
            int gridHeight = gridDataGenerator.GetGridHeight();
            
            // Get a random edge cell
            Vector2Int edgeCell = Vector2Int.zero;
            
            // Randomly choose which edge to spawn on
            int edge = Random.Range(0, 4);
            
            switch (edge)
            {
                case 0: // Top
                    edgeCell = new Vector2Int(Random.Range(0, gridWidth), gridHeight - 1);
                    break;
                case 1: // Right
                    edgeCell = new Vector2Int(gridWidth - 1, Random.Range(0, gridHeight));
                    break;
                case 2: // Bottom
                    edgeCell = new Vector2Int(Random.Range(0, gridWidth), 0);
                    break;
                case 3: // Left
                    edgeCell = new Vector2Int(0, Random.Range(0, gridHeight));
                    break;
            }
            
            return edgeCell;
        }
        
        // Called by game events to trigger ant spawning
        public void TriggerAnts()
        {
            // Clear existing ants
            virtualAnts.Clear();
            
            // Reset discovery data
            discoveredStructures.Clear();
            hasCountedStructures = false;
            
            // Reset statistics
            totalPheromonesLaid = 0;
            averageStrength = 0f;
            highestStrength = 0f;
            cellsWithPheromones = 0;
            
            // Update player structures
            UpdatePlayerStructures();
            
            // Reset the remaining structures to find
            remainingStructuresToFind = new HashSet<Vector2Int>(allPlayerStructures);
            
            // Determine how many ants to spawn
            int antsToSpawn = DetermineOptimalAntCount();
            
            // Spawn all ants at once
            for (int i = 0; i < antsToSpawn; i++)
            {
                SpawnVirtualAnt();
            }
            
            Debug.Log($"ACO algorithm spawned {antsToSpawn} virtual ants to explore the map");
            Debug.Log($"Structures to find: {remainingStructuresToFind.Count}");
            
            // Always use the fast algorithm
            StartCoroutine(RunFastAlgorithm());
        }
        
        // Optimized RunFastAlgorithm coroutine
private IEnumerator RunFastAlgorithm()
{
    Debug.Log("Running ACO algorithm in fast mode");
    
    // Temporarily disable pheromone visualization during execution
    PheromoneVisualizer[] visualizers = FindObjectsOfType<PheromoneVisualizer>();
    bool wasVisualizationEnabled = enablePheromoneVisualization;
    
    if (wasVisualizationEnabled)
    {
        foreach (var viz in visualizers)
        {
            viz.enabled = false;
        }
    }
    
    // Optimization: Larger batch size for faster processing
    int batchSize = 250;
    int antUpdatesThisFrame = 0;
    
    // Optimization: Larger time steps for faster simulation
    float fastTimeStep = 0.25f;
    
    // Cache counts for progress tracking
    int totalAnts = virtualAnts.Count;
    int completedAnts = 0;
    float startTime = Time.realtimeSinceStartup;
    
    // Keep running until all ants are done
    while (virtualAnts.Count > 0)
    {
        // Process each ant
        for (int i = virtualAnts.Count - 1; i >= 0; i--)
        {
            if (i >= virtualAnts.Count) 
                continue; // Safety check
                
            VirtualAnt ant = virtualAnts[i];
            
            // Update lifetime
            ant.lifetime -= fastTimeStep;
            
            // If lifetime is expired, remove the ant
            if (ant.lifetime <= 0)
            {
                virtualAnts.RemoveAt(i);
                completedAnts++;
                continue;
            }
            
            // Check if we need to start returning
            CheckReturnConditions(ant);
            
            // Different behavior based on state
            if (ant.isReturning)
            {
                // Optimization: Process multiple steps at once for returning ants
                for (int step = 0; step < 3; step++) // Process 3 steps at once
                {
                    ReturnToEdge(ant);
                    LayPheromones(ant);
                    
                    // Check if reached edge
                    if (IsAtEdge(ant.position))
                    {
                        virtualAnts.RemoveAt(i);
                        completedAnts++;
                        break;
                    }
                }
            }
            else
            {
                UpdateExplorationBehavior(ant);
            }
            
            // Count this update
            antUpdatesThisFrame++;
            
            // If we've hit our batch size, yield to let Unity breathe
            if (antUpdatesThisFrame >= batchSize)
            {
                antUpdatesThisFrame = 0;
                yield return null; // Wait for next frame
            }
        }
        
        // Always yield at least once per loop
        yield return null;
    }
    
    // Re-enable visualizers if they were enabled before
    if (wasVisualizationEnabled)
    {
        foreach (var viz in visualizers)
        {
            viz.enabled = true;
        }
    }
    
    float endTime = Time.realtimeSinceStartup;
    
    Debug.Log($"ACO algorithm completed in {(endTime - startTime):F2} seconds. " +
              $"Found {discoveredStructures.Count}/{allPlayerStructures.Count} structures. " +
              $"Laid {totalPheromonesLaid} pheromones.");
              
    // Final update to visualize pheromones
    if (enablePheromoneVisualization && visualizers.Length > 0)
    {
        visualizers[0].ForceUpdate();
    }
}
        
        // Public accessor for discovered structures
        public HashSet<Vector2Int> GetDiscoveredStructures()
        {
            return discoveredStructures;
        }
        
        // Draw debug visualizations
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !visualizeAnts)
                return;
            
            // Draw virtual ants
            foreach (var ant in virtualAnts)
            {
                // Skip if grid controller is not available
                if (gridController == null)
                    continue;
                    
                // Calculate world position for visualization
                Vector3 worldPos = gridController.GetCellCenterFromTexture(ant.position.x, ant.position.y);
                
                // Draw ant based on state
                if (ant.isReturning)
                    Gizmos.color = returningColor;
                else if (ant.isSnooping)
                    Gizmos.color = snoopingColor;
                else
                    Gizmos.color = exploringColor;
                    
                Gizmos.DrawWireSphere(worldPos, 0.3f);
                
                // Draw line to target when returning
                if (ant.isReturning)
                {
                    Vector3 targetWorld = gridController.GetCellCenterFromTexture(ant.targetPosition.x, ant.targetPosition.y);
                    Gizmos.DrawLine(worldPos, targetWorld);
                }
                
                // Draw count of discovered structures and flow field influence
                if (visualizeAnts)
                {
                    // Create a formatted label showing structure count and flow field influence
                    string label = $"{ant.discoveredStructures.Count}/{minStructuresPerAnt}\nFF: {ant.currentFlowFieldInfluence:F2}";
                    UnityEditor.Handles.Label(worldPos + Vector3.up * 0.5f, label);
                    
                    // Visualize time without finding structures
                    if (ant.timeSinceLastStructureFound > timeBeforeFlowFieldIncrease && !ant.isReturning)
                    {
                        // Draw a small timer indicator
                        float timerRatio = Mathf.Clamp01((ant.timeSinceLastStructureFound - timeBeforeFlowFieldIncrease) / 10f);
                        Gizmos.color = Color.Lerp(Color.yellow, Color.red, timerRatio);
                        Gizmos.DrawWireSphere(worldPos, 0.3f + timerRatio * 0.2f);
                    }
                }
            }
            
            // Draw discovered structures
            if (showDiscoveredStructures && discoveredStructures != null && gridController != null)
            {
                Gizmos.color = discoveredStructureColor;
                
                foreach (Vector2Int pos in discoveredStructures)
                {
                    Vector3 worldPos = gridController.GetCellCenterFromTexture(pos.x, pos.y);
                    Gizmos.DrawWireCube(worldPos, new Vector3(1f, 0.1f, 1f));
                }
            }
            
            // Draw remaining structures to find
            if (showStructureDebug && remainingStructuresToFind != null && gridController != null)
            {
                Gizmos.color = Color.magenta;
                
                foreach (Vector2Int pos in remainingStructuresToFind)
                {
                    Vector3 worldPos = gridController.GetCellCenterFromTexture(pos.x, pos.y);
                    Gizmos.DrawWireSphere(worldPos, 0.2f);
                }
            }
        }

        // Add to your AntManager's Start method
private void SetupPheromoneVisualizer()
{
    // Check if visualization should be enabled
    PheromoneVisualizer existingVisualizer = FindObjectOfType<PheromoneVisualizer>();

    if (!enablePheromoneVisualization)
    {
        // Find and disable any existing visualizer
        if (existingVisualizer != null)
        {
            existingVisualizer.gameObject.SetActive(false);
        }
        return;
    }
    
    // Check if a visualizer already exists
    if (existingVisualizer != null)
    {
        existingVisualizer.gameObject.SetActive(true);
        return;
    }
    
    // Create a new game object for the visualizer
    GameObject visualizerObject = new GameObject("PheromoneVisualizer");
    visualizerObject.transform.position = new Vector3(0, 0.05f, 0);
    
    // Add a quad mesh
    MeshFilter meshFilter = visualizerObject.AddComponent<MeshFilter>();
    meshFilter.mesh = CreateQuadMesh();
    
    // Add mesh renderer
    visualizerObject.AddComponent<MeshRenderer>();
    
    // Add the visualizer component
    PheromoneVisualizer visualizer = visualizerObject.AddComponent<PheromoneVisualizer>();
}

private Mesh CreateQuadMesh()
{
    Mesh mesh = new Mesh();
    
    // Define vertices (simple quad)
    Vector3[] vertices = new Vector3[4]
    {
        new Vector3(-0.5f, 0, -0.5f), // Bottom left
        new Vector3(0.5f, 0, -0.5f),  // Bottom right
        new Vector3(-0.5f, 0, 0.5f),  // Top left
        new Vector3(0.5f, 0, 0.5f)    // Top right
    };
    
    // Define UVs
    Vector2[] uv = new Vector2[4]
    {
        new Vector2(0, 0),
        new Vector2(1, 0),
        new Vector2(0, 1),
        new Vector2(1, 1)
    };
    
    // Define triangles
    int[] triangles = new int[6]
    {
        0, 2, 1, // First triangle
        2, 3, 1  // Second triangle
    };
    
    // Apply to mesh
    mesh.vertices = vertices;
    mesh.uv = uv;
    mesh.triangles = triangles;
    
    return mesh;
}
    }
}
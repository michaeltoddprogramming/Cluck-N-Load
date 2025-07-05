using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    public class AntNavigationSystem
    {
        // References
        private AntManager antManager;
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        private FarmDefender.Core.AI.FlowField.FlowFieldManager flowFieldManager;
        
        // Navigation settings
        private float updateInterval;
        private float baseFlowFieldDesirability = 0.2f;
        private float timeBeforeFlowFieldIncrease = 5f;
        private float flowFieldInfluenceGrowthRate = 0.05f;
        private float maxSnoopingFlowFieldInfluence = 0.6f;
        private float structureSearchRadius = 2f;
        private float randomMovementFactor = 0.3f;
        private int minStructuresPerAnt = 3;
        private bool showStructureDebug = true;
        
        // Return navigation settings
        private float edgeDirectionWeight = 2.0f;
        private float pheromoneAvoidanceWeight = 1.5f;
        private float flowFieldAlignmentWeight = 1.0f;
        private float returnPathRandomness = 0.2f;
        private float maxPheromoneStrength = 5.0f;
        private int defaultEnemyTypeIndex = 0;
        
        public AntNavigationSystem(AntManager manager, 
                                  GridController gridController, 
                                  GridDataGenerator gridDataGenerator,
                                  FarmDefender.Core.AI.FlowField.FlowFieldManager flowFieldManager,
                                  float updateInterval)
        {
            this.antManager = manager;
            this.gridController = gridController;
            this.gridDataGenerator = gridDataGenerator;
            this.flowFieldManager = flowFieldManager;
            this.updateInterval = updateInterval;
        }
        
        // Initialize settings from AntManager
        public void InitializeSettings(AntManager manager)
        {
            // Extract settings from the manager
            baseFlowFieldDesirability = manager.BaseFlowFieldDesirability;
            timeBeforeFlowFieldIncrease = manager.TimeBeforeFlowFieldIncrease;
            flowFieldInfluenceGrowthRate = manager.FlowFieldInfluenceGrowthRate;
            maxSnoopingFlowFieldInfluence = manager.MaxSnoopingFlowFieldInfluence;
            
            // Exploration behavior settings
            structureSearchRadius = manager.StructureSearchRadius;
            randomMovementFactor = manager.RandomMovementFactor;
            minStructuresPerAnt = manager.MinStructuresPerAnt;
            showStructureDebug = manager.ShowStructureDebug;
            
            // Return path settings
            edgeDirectionWeight = manager.EdgeDirectionWeight;
            pheromoneAvoidanceWeight = manager.PheromoneAvoidanceWeight;
            flowFieldAlignmentWeight = manager.FlowFieldAlignmentWeight;
            returnPathRandomness = manager.ReturnPathRandomness;
            
            // Pheromone system reference
            maxPheromoneStrength = manager.MaxPheromoneStrength;
            defaultEnemyTypeIndex = manager.DefaultEnemyTypeIndex;
        }
        
        public void CheckReturnConditions(VirtualAnt ant, 
                                         HashSet<Vector2Int> remainingStructuresToFind,
                                         HashSet<Vector2Int> discoveredStructures, 
                                         HashSet<Vector2Int> allPlayerStructures)
        {
            // Only return if we've found our minimum number of structures
            // or there are no more structures to find
            bool minimumStructuresFound = ant.discoveredStructures.Count >= minStructuresPerAnt;
            
            // Only consider global completion if ALL structures are actually discovered
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
            if (!ant.isReturning && ant.lifetime < 0.3f * ant.lifetime)
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
            
            // Find shortest distance to an edge
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
        
        public bool IsAtEdge(Vector2Int position)
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            return position.x == 0 || position.x == width - 1 || 
                   position.y == 0 || position.y == height - 1;
        }
        
        public void UpdateExplorationBehavior(VirtualAnt ant, 
                                            HashSet<Vector2Int> remainingStructuresToFind,
                                            HashSet<Vector2Int> discoveredStructures)
        {
            // Track visited cells
            if (!ant.visitedCells.Contains(ant.position))
            {
                ant.visitedCells.Add(ant.position);
            }
            
            // Check if in owned territory
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            bool wasInOwnedTerritory = ant.isInOwnedTerritory;
            ant.isInOwnedTerritory = cell != null && cell.flags.isOwned;
            
            // If just entered owned territory, update desirability
            if (!wasInOwnedTerritory && ant.isInOwnedTerritory)
            {
                ant.currentFlowFieldInfluence = baseFlowFieldDesirability;
            }
            
            // Store previous structure count
            int previousStructureCount = ant.discoveredStructures.Count;
            
            // Check for structures
            CheckForStructuresAround(ant, remainingStructuresToFind, discoveredStructures);
            
            // Did we find a new structure?
            bool foundNewStructure = ant.discoveredStructures.Count > previousStructureCount;
            
            // Update snooping state
            if (ant.discoveredStructures.Count > 0 && !ant.isSnooping)
            {
                ant.isSnooping = true;
                ant.currentFlowFieldInfluence = 0f;
                ant.timeSinceLastStructureFound = 0f;
            }
            
            // Reset timer if new structure found
            if (foundNewStructure)
            {
                ant.timeSinceLastStructureFound = 0f;
                ant.currentFlowFieldInfluence = baseFlowFieldDesirability;
                
                if (showStructureDebug)
                {
                    Debug.Log($"Ant found new structure - resetting flow field influence to {baseFlowFieldDesirability}");
                }
            }
            else
            {
                // Increase timer
                ant.timeSinceLastStructureFound += updateInterval;
            }
            
            // When snooping or in owned territory, increase flow field influence
            if (ant.isInOwnedTerritory || ant.isSnooping)
            {
                if (ant.timeSinceLastStructureFound > timeBeforeFlowFieldIncrease)
                {
                    // Gradually increase
                    float previousInfluence = ant.currentFlowFieldInfluence;
                    ant.currentFlowFieldInfluence += flowFieldInfluenceGrowthRate * updateInterval;
                    ant.currentFlowFieldInfluence = Mathf.Clamp(ant.currentFlowFieldInfluence, 0f, maxSnoopingFlowFieldInfluence);
                    
                    // Log change
                    if (showStructureDebug && Mathf.Abs(previousInfluence - ant.currentFlowFieldInfluence) > 0.05f)
                    {
                        Debug.Log($"Ant has not found structures for {ant.timeSinceLastStructureFound:F1}s - " + 
                                  $"flow field influence increased to {ant.currentFlowFieldInfluence:F2}");
                    }
                }
            }
            
            // Decide movement
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
        
        private void CheckForStructuresAround(VirtualAnt ant, 
                                            HashSet<Vector2Int> remainingStructuresToFind,
                                            HashSet<Vector2Int> discoveredStructures)
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
                        // Check if this structure is still in the remaining structures list
                        if (remainingStructuresToFind.Contains(checkPos))
                        {
                            // Found a valid structure
                            if (!ant.discoveredStructures.Contains(checkPos))
                            {
                                ant.discoveredStructures.Add(checkPos);
                                ant.timeSinceLastStructureFound = 0f;
                                
                                // Add to master list
                                discoveredStructures.Add(checkPos);
                                
                                // Remove from remaining
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
                            // Already found by another ant
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
                // Look for unvisited cells
                List<Vector2Int> possibleDirections = new List<Vector2Int>();
                
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        // Skip current cell
                        if (x == 0 && y == 0)
                            continue;
                        
                        // Check if valid
                        Vector2Int checkPos = new Vector2Int(ant.position.x + x, ant.position.y + y);
                        if (gridController.IsValidCell(checkPos.x, checkPos.y))
                        {
                            // Prefer unvisited
                            if (!ant.visitedCells.Contains(checkPos))
                            {
                                // Double chance
                                possibleDirections.Add(checkPos);
                                possibleDirections.Add(checkPos);
                            }
                            else
                            {
                                // Already visited
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
                        // Add several times to increase probability
                        possibleDirections.Add(continueDir);
                        possibleDirections.Add(continueDir);
                    }
                }
                
                if (possibleDirections.Count > 0)
                {
                    // Random selection
                    newPosition = possibleDirections[Random.Range(0, possibleDirections.Count)];
                    ant.lastDirection = newPosition - ant.position;
                }
                else
                {
                    // Fallback: flow field
                    GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                    if (cell != null && cell.flowDirection != Vector2.zero)
                    {
                        Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                        newPosition = ant.position + flowMove;
                    }
                }
            }
            
            // Add randomness
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
            
            // Check valid cell
            if (gridController.IsValidCell(newPosition.x, newPosition.y))
            {
                // Avoid obstacles
                GridCell targetCell = gridController.GetCell(newPosition.x, newPosition.y);
                if (targetCell != null && !targetCell.flags.isObstacle)
                {
                    ant.position = newPosition;
                }
            }
        }
        
        private void MoveWithFlowField(VirtualAnt ant)
        {
            // Use flow field
            GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
            if (cell != null && cell.flowDirection != Vector2.zero)
            {
                // Convert flow direction
                Vector2Int flowMove = GetGridMoveFromDirection(cell.flowDirection);
                Vector2Int newPosition = ant.position + flowMove;
                
                // Check valid
                if (gridController.IsValidCell(newPosition.x, newPosition.y))
                {
                    // Avoid obstacles
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
            // Convert normalized flow direction to grid move
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
        
        public void ReturnToEdge(VirtualAnt ant)
        {
            Vector2Int originalPosition = ant.position;
            bool moved = false;
            
            // Build a list of possible moves with scores
            List<Vector2Int> possibleMoves = new List<Vector2Int>();
            Dictionary<Vector2Int, float> moveScores = new Dictionary<Vector2Int, float>();
            
            // Check all neighbors
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    // Skip center
                    if (x == 0 && y == 0) continue;
                    
                    Vector2Int nextPos = ant.position + new Vector2Int(x, y);
                    
                    // Skip invalid and obstacles
                    if (!gridController.IsValidCell(nextPos.x, nextPos.y))
                        continue;
                        
                    GridCell nextCell = gridController.GetCell(nextPos.x, nextPos.y);
                    if (nextCell == null || nextCell.flags.isObstacle)
                        continue;
                        
                    // Valid move - calculate score
                    possibleMoves.Add(nextPos);
                    
                    // Base score
                    float score = 1.0f;
                    
                    // Factor 1: Direction toward edge target
                    Vector2Int dirToTarget = ant.targetPosition - ant.position;
                    Vector2Int moveDir = nextPos - ant.position;
                    float dotProduct = (dirToTarget.x * moveDir.x + dirToTarget.y * moveDir.y);
                    float directionScore = dotProduct / Mathf.Max(Mathf.Abs(dirToTarget.x) + Mathf.Abs(dirToTarget.y), 1);
                    score += directionScore * edgeDirectionWeight;

                    // Factor 2: Pheromone avoidance
                    float pheromoneLevel = nextCell.pheromones[defaultEnemyTypeIndex];
                    if (pheromoneLevel > 0)
                    {
                        float pheromoneReduction = Mathf.Clamp01(pheromoneLevel / maxPheromoneStrength);
                        score -= pheromoneReduction * pheromoneAvoidanceWeight;
                    }

                    // Factor 3: Flow field alignment
                    if (ant.useFlowFieldForReturn)
                    {
                        Vector2 inverseFlow = -nextCell.flowDirection;
                        if (inverseFlow != Vector2.zero)
                        {
                            float flowDot = (inverseFlow.x * moveDir.x + inverseFlow.y * moveDir.y);
                            score += flowDot * flowFieldAlignmentWeight;
                        }
                    }
                    
                    // Store score
                    moveScores[nextPos] = score;
                }
            }
            
            // Choose best move
            if (possibleMoves.Count > 0)
            {
                // Sort by score
                possibleMoves.Sort((a, b) => moveScores[b].CompareTo(moveScores[a]));
                
                // Add some randomness
                int moveIndex = 0;
                if (possibleMoves.Count > 1 && Random.value > (1.0f - returnPathRandomness))
                {
                    moveIndex = Random.Range(1, Mathf.Min(3, possibleMoves.Count));
                }

                ant.position = possibleMoves[moveIndex];
                moved = true;
                
                // Debug logging
                if (showStructureDebug && 
                    gridController.GetCell(possibleMoves[moveIndex].x, possibleMoves[moveIndex].y).pheromones[defaultEnemyTypeIndex] > 1.0f)
                {
                    Debug.Log($"Ant avoided strong pheromone ({gridController.GetCell(ant.position.x, ant.position.y).pheromones[defaultEnemyTypeIndex]:F1}) " +
                     $"with score {moveScores[possibleMoves[moveIndex]]:F2}");
                }
            }
            
            // EMERGENCY STRATEGIES
            if (!moved)
            {
                // Flow field fallback
                if (ant.useFlowFieldForReturn)
                {
                    GridCell cell = gridController.GetCell(ant.position.x, ant.position.y);
                    if (cell != null && cell.flowDirection != Vector2.zero)
                    {
                        Vector2 inverseFlow = -cell.flowDirection;
                        Vector2Int flowMove = GetGridMoveFromDirection(inverseFlow);
                        Vector2Int newPosition = ant.position + flowMove;
                        
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
                
                // Direct path fallback
                if (!moved)
                {
                    Vector2Int direction = ant.targetPosition - ant.position;
                    if (direction.sqrMagnitude > 0)
                    {
                        Vector2Int moveDir = new Vector2Int(
                            Mathf.Clamp(direction.x, -1, 1),
                            Mathf.Clamp(direction.y, -1, 1)
                        );
                        
                        Vector2Int newPosition = ant.position + moveDir;
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
                
                // Random move fallback
                if (!moved)
                {
                    List<Vector2Int> validMoves = new List<Vector2Int>();
                    
                    for (int x = -1; x <= 1; x++)
                    {
                        for (int y = -1; y <= 1; y++)
                        {
                            if (x == 0 && y == 0) continue;
                            
                            Vector2Int checkPos = ant.position + new Vector2Int(x, y);
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
                    
                    if (validMoves.Count > 0)
                    {
                        ant.position = validMoves[Random.Range(0, validMoves.Count)];
                    }
                }
            }
        }
        
        // Public getters for debug/visualization
        public float GetTimeBeforeFlowIncrease() => timeBeforeFlowFieldIncrease;
    }
}
using System.Collections.Generic;
using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    /// <summary>
    /// Contains the core flow field calculation algorithms.
    /// This class is not a MonoBehaviour to facilitate unit testing and reuse.
    /// </summary>
    public class FlowFieldAlgorithm
    {
        private FlowFieldSettings settings;
        private System.Random random;
        
        // Maps used to store additional flow information
        public Dictionary<Vector2Int, float> FlowStrengthMap { get; private set; } = new Dictionary<Vector2Int, float>();
        public Dictionary<Vector2Int, float> StreamInfluenceMap { get; private set; } = new Dictionary<Vector2Int, float>();
        
        // Internal struct to represent a neighbor cell and its movement cost
        public struct NeighborInfo
        {
            public GridCell cell;
            public float cost;
            
            public NeighborInfo(GridCell cell, float cost)
            {
                this.cell = cell;
                this.cost = cost;
            }
        }
        
        public FlowFieldAlgorithm(FlowFieldSettings settings)
        {
            this.settings = settings;
            random = new System.Random(System.DateTime.Now.Millisecond);
        }
        
        public void GenerateFlowField(GridDataGenerator gridData, Vector2Int goal)
        {
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            // Clear flow maps
            FlowStrengthMap.Clear();
            StreamInfluenceMap.Clear();
            
            // 1. Reset all cells
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell != null)
                    {
                        cell.integrationCost = int.MaxValue;
                        cell.flowDirection = Vector2.zero;
                        FlowStrengthMap[new Vector2Int(x, y)] = 0f;
                    }
                }
            }
            
            // 2. Initialize queue with target cell
            Queue<GridCell> queue = new Queue<GridCell>();
            GridCell targetCell = gridData.GetCell(goal.x, goal.y);
            targetCell.integrationCost = 0;
            queue.Enqueue(targetCell);
            
            // 3. Dijkstra propagation with obstacle avoidance
            CalculateIntegrationField(queue, gridData);
            
            // 4. Check for unreachable cells (target enclosure detection)
            bool hasUnreachableCells = HasUnreachableCells(gridData);
            
            // If target is enclosed, generate a secondary flow field targeting the enclosure
            if (hasUnreachableCells)
            {
                HashSet<Vector2Int> borderObstacles = FindEnclosureBoundaryObstacles(gridData);
                
                // Only proceed if we found boundary obstacles
                if (borderObstacles.Count > 0)
                {
                    GenerateSecondaryFlowField(gridData, borderObstacles);
                }
            }
            
            // 5. Identify cells near obstacles for priority paths (original functionality)
            Dictionary<Vector2Int, float> obstaclePriorities = IdentifyObstaclePriorityAreas(gridData);
            
            // 6. Compute enhanced flow directions
            HashSet<Vector2Int> priorityStreamCells = CalculateFlowDirections(gridData, goal, obstaclePriorities);
            
            // 7. Process stream influence between priority paths and regular cells
            if (priorityStreamCells.Count > 0)
            {
                ProcessStreamInfluence(gridData, priorityStreamCells);
            }
        }
        
        private void CalculateIntegrationField(Queue<GridCell> queue, GridDataGenerator gridData)
        {
            while (queue.Count > 0)
            {
                GridCell current = queue.Dequeue();
                
                foreach (var neighborInfo in GetNeighborsWithCost(current, gridData))
                {
                    GridCell neighbor = neighborInfo.cell;
                    float moveCost = neighborInfo.cost;
                    
                    // Skip obstacles and occupied cells
                    if (neighbor != null && !neighbor.flags.isObstacle && !neighbor.flags.isOccupied)
                    {
                        int newCost = current.integrationCost + Mathf.RoundToInt(moveCost);
                        if (newCost < neighbor.integrationCost)
                        {
                            neighbor.integrationCost = newCost;
                            queue.Enqueue(neighbor);
                        }
                    }
                }
            }
        }
        
        private Dictionary<Vector2Int, float> IdentifyObstaclePriorityAreas(GridDataGenerator gridData)
        {
            Dictionary<Vector2Int, float> obstaclePriorities = new Dictionary<Vector2Int, float>();
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            // Find all obstacles and mark nearby cells with priority values
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell == null) continue;
                    
                    if (cell.flags.isObstacle || cell.flags.isOccupied)
                    {
                        // For each obstacle, mark cells in range with priority values
                        for (int dx = -settings.priorityPathRange; dx <= settings.priorityPathRange; dx++)
                        {
                            for (int dy = -settings.priorityPathRange; dy <= settings.priorityPathRange; dy++)
                            {
                                int nx = x + dx;
                                int ny = y + dy;
                                
                                // Skip out of bounds or the obstacle itself
                                if (nx < 0 || nx >= gridWidth || ny < 0 || ny >= gridHeight || (dx == 0 && dy == 0))
                                    continue;
                                    
                                GridCell nearbyCell = gridData.GetCell(nx, ny);
                                if (nearbyCell != null && !nearbyCell.flags.isObstacle && !nearbyCell.flags.isOccupied)
                                {
                                    Vector2Int key = new Vector2Int(nx, ny);
                                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                                    
                                    // Priority decreases with distance from obstacle
                                    float priority = 1f - Mathf.Clamp01(distance / settings.priorityPathRange);
                                    
                                    // Track the highest priority for this cell
                                    if (!obstaclePriorities.ContainsKey(key) || priority > obstaclePriorities[key])
                                    {
                                        obstaclePriorities[key] = priority;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            return obstaclePriorities;
        }
        
        private HashSet<Vector2Int> CalculateFlowDirections(
            GridDataGenerator gridData, 
            Vector2Int goal, 
            Dictionary<Vector2Int, float> obstaclePriorities)
        {
            HashSet<Vector2Int> priorityStreamCells = new HashSet<Vector2Int>();
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell == null || cell.integrationCost == int.MaxValue) 
                        continue;
                    
                    Vector2Int cellPos = new Vector2Int(x, y);
                    
                    // Get base flow direction from integration field
                    Vector2 baseDirection = GetBaseFlowDirection(cell, gridData);
                    
                    // Calculate direct path to target
                    Vector2 directPath = new Vector2(goal.x - x, goal.y - y);
                    if (directPath.magnitude > 0) directPath.Normalize();
                    
                    // Apply priority path strength if near obstacle
                    float priorityFactor = 0f;
                    Vector2 finalDirection;
                    
                    if (obstaclePriorities.TryGetValue(cellPos, out float priority))
                    {
                        priorityFactor = priority * settings.obstaclePriorityStrength;
                        
                        // Store the flow strength for visualization
                        FlowStrengthMap[cellPos] = priorityFactor;
                        
                        // For high priority paths (near obstacles), use pure pathfinding
                        if (priorityFactor > 0.5f)
                        {
                            finalDirection = baseDirection;
                            priorityStreamCells.Add(cellPos);
                        }
                        else
                        {
                            // For cells further from obstacles, gradually blend
                            float blendFactor = settings.directTargetInfluence * (1.0f - (priorityFactor / 0.5f));
                            finalDirection = Vector2.Lerp(baseDirection, directPath, blendFactor);
                        }
                    }
                    else
                    {
                        // Normal behavior for cells not near obstacles
                        finalDirection = Vector2.Lerp(baseDirection, directPath, settings.directTargetInfluence);
                    }
                    
                    // Ensure the direction is normalized
                    if (finalDirection.magnitude > 0)
                    {
                        finalDirection.Normalize();
                        
                        // Apply randomness if enabled
                        if (settings.directionRandomness > 0)
                        {
                            // Reduce randomness for priority paths
                            float adjustedRandomness = settings.directionRandomness * (1f - priorityFactor);
                            cell.flowDirection = AddRandomnessToDirection(finalDirection, adjustedRandomness);
                        }
                        else
                        {
                            cell.flowDirection = finalDirection;
                        }
                    }
                }
            }
            
            return priorityStreamCells;
        }
        
        private void ProcessStreamInfluence(GridDataGenerator gridData, HashSet<Vector2Int> priorityStreamCells)
        {
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            // Calculate influence for all cells based on proximity to streams
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    
                    // Skip cells that are already priority streams
                    if (priorityStreamCells.Contains(cellPos))
                        continue;
                    
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell == null || cell.integrationCost == int.MaxValue) 
                        continue;
                    
                    // Find the nearest priority stream and calculate influence
                    float minDistance = float.MaxValue;
                    Vector2Int nearestStream = Vector2Int.zero;
                    bool foundStream = false;
                    
                    foreach (var streamPos in priorityStreamCells)
                    {
                        float distance = Vector2.Distance(cellPos, streamPos);
                        if (distance < minDistance && distance <= settings.streamInfluenceRange)
                        {
                            minDistance = distance;
                            nearestStream = streamPos;
                            foundStream = true;
                        }
                    }
                    
                    if (foundStream)
                    {
                        // Calculate influence factor based on distance
                        float influenceFactor = 1.0f - Mathf.Clamp01(minDistance / settings.streamInfluenceRange);
                        influenceFactor *= settings.streamInfluenceStrength;
                        
                        // Store influence for visualization and flow adjustment
                        StreamInfluenceMap[cellPos] = influenceFactor;
                        
                        // Get the direction from the nearest priority stream
                        GridCell streamCell = gridData.GetCell(nearestStream.x, nearestStream.y);
                        if (streamCell != null && streamCell.flowDirection != Vector2.zero)
                        {
                            // Blend the cell's current direction with the priority stream direction
                            Vector2 interpolatedDirection = Vector2.Lerp(
                                cell.flowDirection,
                                streamCell.flowDirection,
                                influenceFactor
                            );
                            
                            // Update the cell's flow direction
                            if (interpolatedDirection.magnitude > 0)
                            {
                                interpolatedDirection.Normalize();
                                cell.flowDirection = interpolatedDirection;
                            }
                        }
                    }
                }
            }
        }
        
        // Helper methods
        
        public Vector2 GetBaseFlowDirection(GridCell cell, GridDataGenerator gridData)
        {
            GridCell lowest = cell;
            
            foreach (var neighborInfo in GetNeighborsWithCost(cell, gridData))
            {
                GridCell neighbor = neighborInfo.cell;
                if (neighbor != null && neighbor.integrationCost < lowest.integrationCost)
                    lowest = neighbor;
            }
            
            if (lowest != cell)
            {
                Vector2 direction = new Vector2(lowest.x - cell.x, lowest.y - cell.y);
                if (direction.magnitude > 0) direction.Normalize();
                return direction;
            }
            
            return Vector2.zero;
        }
        
        public Vector2 AddRandomnessToDirection(Vector2 baseDirection, float randomnessFactor)
        {
            if (baseDirection == Vector2.zero)
                return Vector2.zero;
            
            float randomAngle = ((float)random.NextDouble() * 2f - 1f) * settings.maxRandomAngle * randomnessFactor;
            
            float cos = Mathf.Cos(randomAngle * Mathf.Deg2Rad);
            float sin = Mathf.Sin(randomAngle * Mathf.Deg2Rad);
            Vector2 rotated = new Vector2(
                baseDirection.x * cos - baseDirection.y * sin,
                baseDirection.x * sin + baseDirection.y * cos
            );
            
            rotated.Normalize();
            return rotated;
        }
        
        public List<NeighborInfo> GetNeighborsWithCost(GridCell cell, GridDataGenerator gridData)
        {
            List<NeighborInfo> neighbors = new List<NeighborInfo>();
            
            // Cardinal directions
            Vector2Int[] cardinalDirections = new Vector2Int[]
            {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };
            
            // Diagonal directions
            Vector2Int[] diagonalDirections = new Vector2Int[]
            {
                new Vector2Int(1, 1), new Vector2Int(1, -1), 
                new Vector2Int(-1, 1), new Vector2Int(-1, -1)
            };
            
            // Add cardinal neighbors
            foreach (var dir in cardinalDirections)
            {
                int nx = cell.x + dir.x;
                int ny = cell.y + dir.y;
                
                if (nx >= 0 && nx < gridData.GetGridWidth() && ny >= 0 && ny < gridData.GetGridHeight())
                    neighbors.Add(new NeighborInfo(gridData.GetCell(nx, ny), 1f));
            }
            
            // Add diagonal neighbors if enabled
            if (settings.useDiagonalDirections)
            {
                foreach (var dir in diagonalDirections)
                {
                    int nx = cell.x + dir.x;
                    int ny = cell.y + dir.y;
                    
                    if (nx >= 0 && nx < gridData.GetGridWidth() && ny >= 0 && ny < gridData.GetGridHeight())
                    {
                        float cost = settings.useWeightedDiagonals ? 1.4f : 1f;
                        neighbors.Add(new NeighborInfo(gridData.GetCell(nx, ny), cost));
                    }
                }
            }
            
            return neighbors;
        }
        
        // Check if there are any unreachable cells in the grid
        private bool HasUnreachableCells(GridDataGenerator gridData)
        {
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied && 
                        cell.integrationCost == int.MaxValue)
                    {
                        return true; // Found at least one unreachable cell
                    }
                }
            }
            
            return false;
        }
        
        // Find obstacles that border unreachable areas
        private HashSet<Vector2Int> FindEnclosureBoundaryObstacles(GridDataGenerator gridData)
        {
            HashSet<Vector2Int> boundaryObstacles = new HashSet<Vector2Int>();
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    // Skip non-obstacles
                    GridCell cell = gridData.GetCell(x, y);
                    if (cell == null || (!cell.flags.isObstacle && !cell.flags.isOccupied))
                        continue;
                    
                    // Check if this obstacle has any unreachable neighbor
                    bool isBoundary = false;
                    foreach (var neighborInfo in GetNeighborsWithCost(cell, gridData))
                    {
                        GridCell neighbor = neighborInfo.cell;
                        if (neighbor != null && !neighbor.flags.isObstacle && !neighbor.flags.isOccupied && 
                            neighbor.integrationCost == int.MaxValue)
                        {
                            // This obstacle borders an unreachable area
                            isBoundary = true;
                            break;
                        }
                    }
                    
                    if (isBoundary)
                    {
                        boundaryObstacles.Add(new Vector2Int(x, y));
                    }
                }
            }
            
            return boundaryObstacles;
        }
        
        // Generate a secondary flow field with enclosure obstacles as targets
        private void GenerateSecondaryFlowField(GridDataGenerator gridData, HashSet<Vector2Int> boundaryObstacles)
        {
            int gridWidth = gridData.GetGridWidth();
            int gridHeight = gridData.GetGridHeight();
            
            // Use a temporary dictionary to track our secondary integration costs
            Dictionary<Vector2Int, int> secondaryCosts = new Dictionary<Vector2Int, int>();
            
            // Initialize queue with boundary obstacles as sources
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            foreach (var obstacle in boundaryObstacles)
            {
                secondaryCosts[obstacle] = 0;
                queue.Enqueue(obstacle);
            }
            
            // Dijkstra algorithm spreading from boundary obstacles
            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                int currentCost = secondaryCosts[current];
                
                // Get neighbors
                GridCell currentCell = gridData.GetCell(current.x, current.y);
                if (currentCell == null) continue;
                
                foreach (var neighborInfo in GetNeighborsWithCost(currentCell, gridData))
                {
                    GridCell neighbor = neighborInfo.cell;
                    if (neighbor == null) continue;
                    
                    Vector2Int neighborPos = new Vector2Int(neighbor.x, neighbor.y);
                    
                    // Skip obstacles (except the starting boundary ones)
                    if ((neighbor.flags.isObstacle || neighbor.flags.isOccupied) && 
                        !boundaryObstacles.Contains(neighborPos))
                        continue;
                    
                    int newCost = currentCost + Mathf.RoundToInt(neighborInfo.cost);
                    
                    // Check if this is a better path
                    if (!secondaryCosts.ContainsKey(neighborPos) || newCost < secondaryCosts[neighborPos])
                    {
                        secondaryCosts[neighborPos] = newCost;
                        queue.Enqueue(neighborPos);
                        
                        // Only update flow direction for unreachable cells
                        if (neighbor.integrationCost == int.MaxValue)
                        {
                            // Calculate flow direction toward the boundary obstacles
                            Vector2 direction = new Vector2(current.x - neighbor.x, current.y - neighbor.y);
                            if (direction.magnitude > 0)
                            {
                                direction.Normalize();
                                neighbor.flowDirection = direction;
                                neighbor.integrationCost = 1000000 + newCost; // Set a large but finite cost
                            }
                        }
                    }
                }
            }
        }
    }
}
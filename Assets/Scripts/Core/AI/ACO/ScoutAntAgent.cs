using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single virtual scout ant agent for ACO navigation.
/// Handles movement phases, target discovery, and pheromone laying.
/// </summary>
public class ScoutAntAgent
{
    public enum AntPhase { Initial, Snoop, Scouting, Return }

    public float moveSpeed = 5f;
    public float speedModifier = 1f; // 0 = instant, 1 = normal, >1 = slower
    public float lifetime = 5f;
    public float snoopDuration = 3f;
    public float scoutingDuration = 8f;
    public float pheromoneStrength = 1f;
    public int maxTargets = 3;

    private ScoutAntManager manager;
    private GridController gridController;
    private GridDataGenerator gridDataGenerator;
    private PheromoneManager pheromoneManager;

    private AntPhase phase = AntPhase.Initial;
    private float phaseTimer = 0f;
    private float timeAlive = 0f;
    public Vector2Int currentCell { get; private set; }
    private Vector2Int spawnCell;
    private List<Vector2Int> discoveredTargets = new List<Vector2Int>();
    private bool returning = false;
    private Queue<Vector2Int> returnPath = new Queue<Vector2Int>();

    public ScoutAntAgent(
        ScoutAntManager mgr,
        GridController gridCtrl,
        GridDataGenerator gridData,
        PheromoneManager pheroManager,
        Vector2Int spawn)
    {
        manager = mgr;
        gridController = gridCtrl;
        gridDataGenerator = gridData;
        pheromoneManager = pheroManager;
        ResetAgent(spawn);
    }

    public void ResetAgent(Vector2Int spawn)
    {
        spawnCell = spawn;
        currentCell = spawn;
        phase = AntPhase.Initial;
        phaseTimer = 0f;
        timeAlive = 0f;
        discoveredTargets.Clear();
        returning = false;
        returnPath.Clear();
    }

    public void AntUpdate(float deltaTime)
    {
        // Use extremely large delta when speedModifier is 0
        float effectiveDelta = speedModifier <= 0.01f ? float.MaxValue : deltaTime / speedModifier;
        timeAlive += deltaTime; // Still track real time for lifetime
        phaseTimer += deltaTime; // Still track real time for phase timing
        
        if (speedModifier <= 0.01f)
        {
            // For instant calculation, complete the entire phase at once
            switch (phase)
            {
                case AntPhase.Initial:
                    CompleteInitialPhase();
                    break;
                case AntPhase.Snoop:
                    CompleteSnoopPhase();
                    break;
                case AntPhase.Scouting:
                    CompleteScoutingPhase();
                    break;
                case AntPhase.Return:
                    CompleteReturnPhase();
                    break;
            }
        }
        else
        {
            // Normal step-by-step execution
            switch (phase)
            {
                case AntPhase.Initial:
                    InitialPhase(effectiveDelta);
                    break;
                case AntPhase.Snoop:
                    SnoopPhase(effectiveDelta);
                    break;
                case AntPhase.Scouting:
                    ScoutingPhase(effectiveDelta);
                    break;
                case AntPhase.Return:
                    ReturnPhase(effectiveDelta);
                    break;
            }
        }
        
        // End agent if lifetime exceeded
        if (timeAlive > lifetime)
        {
            // If the ant is still active but running out of lifetime, force it to return
            if (phase != AntPhase.Return) {
                BeginReturn();
            }
            // Only remove the ant if it's really out of time
            if (timeAlive > lifetime * 1.5f) {
                manager.OnAntFinished(this);
            }
        }
    }

    // --- PHASES ---

    private void InitialPhase(float effectiveDelta)
    {
        // Check all neighbors for isOwned before moving
        if (IsNeighborOwned(currentCell))
        {
            phase = AntPhase.Snoop;
            phaseTimer = 0f;
            return;
        }

        // Move along flow field toward player base
        Vector2Int nextCell = GetNextCellByFlowField(currentCell);
        MoveToCell(nextCell);

        // After moving, check again (in case we landed on an owned cell)
        if (IsPlayerTerritory(nextCell))
        {
            phase = AntPhase.Snoop;
            phaseTimer = 0f;
        }
    }

    private void SnoopPhase(float effectiveDelta)
    {
        int steps = speedModifier == 0f ? 10 : 1;
        for (int s = 0; s < steps; s++)
        {
            Vector2Int nextCell = GetBestSnoopNeighbor(currentCell);
            MoveToCell(nextCell);

            // Discover structures
            if (IsStructureCell(nextCell) && !discoveredTargets.Contains(nextCell))
            {
                discoveredTargets.Add(nextCell);
                if (discoveredTargets.Count >= maxTargets)
                {
                    BeginReturn();
                    return;
                }
            }
        }

        // After snoopDuration, go to scouting
        if (phaseTimer > snoopDuration)
        {
            phase = AntPhase.Scouting;
            phaseTimer = 0f;
        }
    }

    private void ScoutingPhase(float effectiveDelta)
    {
        // Add safety distance check to force return if too far from edge
        bool tooFarFromEdge = IsTooFarFromEdge(currentCell, 5); // Return when 5 cells or less from edge
        
        int steps = speedModifier == 0f ? 10 : 1;
        for (int s = 0; s < steps; s++)
        {
            Vector2Int nextCell = GetBestScoutingNeighbor(currentCell);
            MoveToCell(nextCell);

            if (IsStructureCell(nextCell) && !discoveredTargets.Contains(nextCell))
            {
                discoveredTargets.Add(nextCell);
                if (discoveredTargets.Count >= maxTargets)
                {
                    BeginReturn();
                    return;
                }
            }
        }

        // Return if spent too much time or too far from edge
        if (phaseTimer > scoutingDuration || tooFarFromEdge)
        {
            BeginReturn();
        }
    }

    private void BeginReturn()
    {
        phase = AntPhase.Return;
        phaseTimer = 0f;
        returning = true;
        
        // No longer create a fixed path - we'll use dynamic flow field navigation in reverse
        returnPath.Clear(); // Clear any existing path data
    }

    private void ReturnPhase(float effectiveDelta)
    {
        int steps = speedModifier == 0f ? 10 : 1;
        for (int s = 0; s < steps; s++)
        {
            // Lay pheromone on current cell
            LayPheromone(currentCell);
            
            // For discovered targets, lay pheromones in a small radius
            if (discoveredTargets.Contains(currentCell) && pheromoneManager != null)
            {
                foreach (var neighbor in GetNeighbors(currentCell))
                {
                    pheromoneManager.LayPheromone(neighbor, 0, pheromoneStrength * 1.5f);
                }
            }
            
            // Get next cell using reverse flow field but avoid saturated paths
            Vector2Int nextCell = GetReverseFlowFieldCell(currentCell);
            MoveToCell(nextCell);
            
            // If we reached an edge, we're done
            if (IsEdgeCell(nextCell))
            {
                returning = false;
                manager.OnAntFinished(this);
                return;
            }
        }
    }

    private void MoveToCell(Vector2Int nextCell)
    {
        currentCell = nextCell;
    }

    // --- NEIGHBOR CHECK ---

    private bool IsNeighborOwned(Vector2Int cell)
    {
        foreach (var n in GetNeighbors(cell))
        {
            if (IsPlayerTerritory(n))
                return true;
        }
        return false;
    }

    // --- Utility methods (same as before, but no Unity-specific code) ---
    private Vector2Int GetNextCellByFlowField(Vector2Int cell)
    {
        // Use flow field direction to pick next cell
        GridCell gc = gridDataGenerator.GetCell(cell.x, cell.y);
        Vector2 dir = gc.flowDirection.normalized;
        Vector2Int next = cell + new Vector2Int(Mathf.RoundToInt(dir.x), Mathf.RoundToInt(dir.y));
        if (gridController.IsValidCell(next.x, next.y))
            return next;
        return cell;
    }

    private Vector2Int GetBestSnoopNeighbor(Vector2Int cell)
    {
        // Prefer unvisited, owned, and FF-aligned neighbors
        float bestScore = float.MinValue;
        Vector2Int best = cell;

        foreach (var n in GetNeighbors(cell))
        {
            float score = 0f;
            if (!IsVisited(n)) score += 2f;
            if (IsPlayerTerritory(n)) score += 1f;
            score += Vector2.Dot(GetFlowFieldDirection(cell), ((Vector2)(n - cell)).normalized) * 0.5f;
            if (score > bestScore)
            {
                bestScore = score;
                best = n;
            }
        }
        return best;
    }

    private Vector2Int GetBestScoutingNeighbor(Vector2Int cell)
    {
        // Like snoop, but more FF influence
        float bestScore = float.MinValue;
        Vector2Int best = cell;

        foreach (var n in GetNeighbors(cell))
        {
            float score = 0f;
            if (!IsVisited(n)) score += 1f;
            score += Vector2.Dot(GetFlowFieldDirection(cell), ((Vector2)(n - cell)).normalized);
            if (score > bestScore)
            {
                bestScore = score;
                best = n;
            }
        }
        return best;
    }

    private Vector2 GetFlowFieldDirection(Vector2Int cell)
    {
        return gridDataGenerator.GetCell(cell.x, cell.y).flowDirection.normalized;
    }

    private bool IsVisited(Vector2Int cell) => false; // Placeholder

    private bool IsPlayerTerritory(Vector2Int cell)
    {
        return gridDataGenerator.GetCell(cell.x, cell.y).flags.isOwned;
    }

    private bool IsStructureCell(Vector2Int cell)
    {
        return gridDataGenerator.GetCell(cell.x, cell.y).placedObject != null;
    }

    private void LayPheromone(Vector2Int cell)
    {
        if (pheromoneManager != null)
        {
            if (discoveredTargets.Contains(cell))
            {
                // Lay stronger pheromone for discovered targets
                pheromoneManager.LayPheromoneSource(cell, 0, pheromoneStrength * 2.0f);
                Debug.Log($"Ant laying TARGET pheromone at {cell}, strength: {pheromoneStrength * 2.0f}");
            }
            else
            {
                // Lay normal pheromone for the path
                pheromoneManager.LayPheromoneSource(cell, 0, pheromoneStrength);
                if (Random.value < 0.1f) // Reduce log spam
                    Debug.Log($"Ant laying PATH pheromone at {cell}, strength: {pheromoneStrength}");
            }
            
            if (IsStructureCell(cell))
            {
                // Mark this structure to avoid redundant marking
                pheromoneManager.MarkStructure(cell);
            }
        }
        else
        {
            Debug.LogWarning("ScoutAnt attempted to lay pheromone but pheromoneManager is null");
        }
    }

    private Vector2Int FindNearestEdgeCell(Vector2Int from)
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int minDist = int.MaxValue;
        Vector2Int best = from;

        // Check all edge cells
        for (int x = 0; x < width; x++)
        {
            if (from.y < minDist)
            {
                minDist = Mathf.Abs(from.y - 0);
                best = new Vector2Int(x, 0);
            }
            if (Mathf.Abs(from.y - (height - 1)) < minDist)
            {
                minDist = Mathf.Abs(from.y - (height - 1));
                best = new Vector2Int(x, height - 1);
            }
        }
        for (int y = 0; y < height; y++)
        {
            if (Mathf.Abs(from.x - 0) < minDist)
            {
                minDist = Mathf.Abs(from.x - 0);
                best = new Vector2Int(0, y);
            }
            if (Mathf.Abs(from.x - (width - 1)) < minDist)
            {
                minDist = Mathf.Abs(from.x - (width - 1));
                best = new Vector2Int(width - 1, y);
            }
        }
        return best;
    }

    private Queue<Vector2Int> FindPath(Vector2Int start, Vector2Int goal)
    {
        // Simple BFS for now (replace with A* if needed)
        Queue<Vector2Int> path = new Queue<Vector2Int>();
        Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        frontier.Enqueue(start);
        cameFrom[start] = start;

        bool found = false;
        while (frontier.Count > 0)
        {
            var current = frontier.Dequeue();
            if (current == goal)
            {
                found = true;
                break;
            }
            foreach (var n in GetNeighbors(current))
            {
                if (!cameFrom.ContainsKey(n) && !gridDataGenerator.GetCell(n.x, n.y).flags.isObstacle)
                {
                    frontier.Enqueue(n);
                    cameFrom[n] = current;
                }
            }
        }

        if (!found)
            return path;

        // Reconstruct path
        List<Vector2Int> revPath = new List<Vector2Int>();
        var c = goal;
        while (c != start)
        {
            revPath.Add(c);
            c = cameFrom[c];
        }
        revPath.Reverse();
        foreach (var step in revPath)
            path.Enqueue(step);

        return path;
    }

    private List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = cell.x + dx[i];
            int ny = cell.y + dy[i];
            if (gridController.IsValidCell(nx, ny))
                neighbors.Add(new Vector2Int(nx, ny));
        }
        return neighbors;
    }

    private bool IsTooFarFromEdge(Vector2Int cell, int minDistance)
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        int distToLeft = cell.x;
        int distToRight = width - 1 - cell.x;
        int distToBottom = cell.y;
        int distToTop = height - 1 - cell.y;
        
        int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
        return minDist > minDistance;
    }

    // Add these new "Complete" methods that execute each phase instantly:

    private void CompleteInitialPhase()
    {
        // Find nearest owned cell by following flow field
        Vector2Int current = currentCell;
        bool foundOwned = false;
        int safety = 100; // Prevent infinite loops
        
        while (!foundOwned && safety > 0)
        {
            safety--;
            current = GetNextCellByFlowField(current);
            MoveToCell(current);
            
            if (IsPlayerTerritory(current) || IsNeighborOwned(current))
            {
                foundOwned = true;
                phase = AntPhase.Snoop;
                phaseTimer = 0f;
            }
        }
        
        // If we couldn't find owned territory, go to scouting anyway
        if (!foundOwned)
        {
            phase = AntPhase.Snoop;
            phaseTimer = 0f;
        }
    }

    private void CompleteSnoopPhase()
    {
        // Explore owned area and find structures
        int maxSteps = 30; // Limit exploration steps
        
        for (int i = 0; i < maxSteps; i++)
        {
            Vector2Int next = GetBestSnoopNeighbor(currentCell);
            if (next == currentCell) break; // No good moves left
            
            MoveToCell(next);
            
            if (IsStructureCell(next) && !discoveredTargets.Contains(next))
            {
                discoveredTargets.Add(next);
                if (discoveredTargets.Count >= maxTargets)
                {
                    BeginReturn();
                    return;
                }
            }
        }
        
        // Move to scouting phase
        phase = AntPhase.Scouting;
        phaseTimer = 0f;
    }

    private void CompleteScoutingPhase()
    {
        // Further exploration
        int maxSteps = 50; // Limit exploration steps
        
        for (int i = 0; i < maxSteps; i++)
        {
            // Check if we're too far from the edge and should return
            if (IsTooFarFromEdge(currentCell, 5))
            {
                BeginReturn();
                return;
            }
            
            Vector2Int next = GetBestScoutingNeighbor(currentCell);
            if (next == currentCell) break; // No good moves left
            
            MoveToCell(next);
            
            if (IsStructureCell(next) && !discoveredTargets.Contains(next))
            {
                discoveredTargets.Add(next);
                if (discoveredTargets.Count >= maxTargets)
                {
                    BeginReturn();
                    return;
                }
            }
        }
        
        // Begin return phase after exploration
        BeginReturn();
    }

    private void CompleteReturnPhase()
    {
        int maxSteps = 100; // Safety limit to prevent infinite loops
        while (returning && maxSteps > 0)
        {
            maxSteps--;
            
            // Lay pheromone at current location
            LayPheromone(currentCell);
            
            // For discovered targets, lay pheromones in a small radius
            if (discoveredTargets.Contains(currentCell) && pheromoneManager != null)
            {
                foreach (var neighbor in GetNeighbors(currentCell))
                {
                    pheromoneManager.LayPheromoneSource(neighbor, 0, pheromoneStrength * 1.5f);
                }
            }
            
            // Get next cell using reverse flow field
            Vector2Int nextCell = GetReverseFlowFieldCell(currentCell);
            MoveToCell(nextCell);
            
            // Check if we've reached the edge
            if (IsEdgeCell(nextCell))
            {
                // One final pheromone deposit at the edge
                LayPheromone(nextCell);
                returning = false;
                manager.OnAntFinished(this);
                break;
            }
        }
        
        // Force finish if we hit the step limit
        if (maxSteps <= 0 && returning)
        {
            returning = false;
            manager.OnAntFinished(this);
        }
    }

    // Method to get a cell in reverse flow field direction while avoiding saturated paths
    private Vector2Int GetReverseFlowFieldCell(Vector2Int cell)
    {
        List<Vector2Int> neighbors = GetNeighbors(cell);
        if (neighbors.Count == 0) return cell;
        
        float bestScore = float.MinValue;
        Vector2Int bestCell = cell;
        
        foreach (var n in neighbors)
        {
            // Calculate base score: edge proximity plus slight randomness
            float score = GetEdgeProximityScore(n) * 3.0f + Random.value * 0.2f;
            
            // Add pheromone avoidance factor - LOWER pheromone is BETTER
            if (pheromoneManager != null)
            {
                float pheromoneLevel = pheromoneManager.GetPheromone(n, 0);
                // Convert pheromone level to penalty (higher pheromone = lower score)
                float pheromonePenalty = pheromoneLevel * 2.0f; // Adjust multiplier for avoidance strength
                score -= pheromonePenalty;
                
                // Debug output for significant decisions
                if (pheromoneLevel > 1.0f && Random.value < 0.05f)
                    Debug.Log($"Ant avoiding high pheromone ({pheromoneLevel:F1}) at {n}, score penalty: {pheromonePenalty:F1}");
            }
            
            if (score > bestScore)
            {
                bestScore = score;
                bestCell = n;
            }
        }
        
        return bestCell;
    }

    // Helper to determine how close a cell is to any map edge
    private float GetEdgeProximityScore(Vector2Int cell)
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        // Calculate distance to each edge
        int distToLeft = cell.x;
        int distToRight = width - 1 - cell.x;
        int distToBottom = cell.y;
        int distToTop = height - 1 - cell.y;
        
        // Use the minimum distance to any edge
        int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
        
        // Convert to a 0-1 score where 1 is at the edge and 0 is furthest from any edge
        return 1.0f - Mathf.Clamp01((float)minDist / Mathf.Max(width, height) * 2.0f);
    }

    // Helper to check if a cell is on the map edge
    private bool IsEdgeCell(Vector2Int cell)
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        return cell.x == 0 || cell.x == width - 1 || cell.y == 0 || cell.y == height - 1;
    }
}

public class AntDebugHotkey : MonoBehaviour
{
    public ScoutAntManager scoutAntManager;

    void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A))
        {
            if (scoutAntManager != null)
                scoutAntManager.TriggerAnts();
        }
    }
}
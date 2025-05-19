using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages pheromone grid operations: diffusion, decay, and structure marking.
/// Integrates with the existing GridCell 2D array.
/// </summary>
public class PheromoneManager : MonoBehaviour
{
    [Header("References")]
    public GridDataGenerator gridDataGenerator;

    [Header("Pheromone Settings")]
    public float diffusionRate = 0.4f; // Increased from 0.2f for more spread
    public float decayRate = 0f;      // Keep at zero for persistence
    public int diffusionIterations = 3; // Increased from 1 for wider distribution
    [Tooltip("Distance in cells that pheromones will spread during distribution")]
    [Range(1, 15)]
    public int distributionRange = 3;
    [Tooltip("How strongly to equalize pheromone values (0=none, 1=fully equal)")]
    [Range(0, 1)]
    public float equalizationFactor = 0.3f;
    public bool enableAutoUpdate = false; // Added control flag

    // Registry of discovered structures to avoid redundant marking
    private HashSet<Vector2Int> markedStructures = new HashSet<Vector2Int>();

    private List<(Vector2Int cell, int type, float amount)> newPheromoneSources = new List<(Vector2Int, int, float)>();

    private void Awake()
    {
        if (gridDataGenerator == null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
    }

    private void Start()
    {
        // Comment out or remove the automatic updates
        // InvokeRepeating("UpdatePheromones", 1.0f, 0.5f);  // Call every 0.5 seconds
    }

    /// <summary>
    /// Call this periodically (e.g. every second) to diffuse and decay pheromones.
    /// </summary>
    public void UpdatePheromones()
    {
        for (int i = 0; i < diffusionIterations; i++)
            DiffusePheromones();
        DecayPheromones();
    }

    /// <summary>
    /// Lays pheromone of a given type at a cell.
    /// </summary>
    public void LayPheromone(Vector2Int cell, int type, float amount)
    {
        var gc = gridDataGenerator.GetCell(cell.x, cell.y);
        if (gc != null && type >= 0 && type < gc.pheromones.Length)
        {
            gc.pheromones[type] += amount;
        }
    }

    // Add this method to allow ants to register pheromone sources
    public void LayPheromoneSource(Vector2Int cell, int type, float amount)
    {
        newPheromoneSources.Add((cell, type, amount));
    }

    /// <summary>
    /// Returns the pheromone value for a given cell and type.
    /// </summary>
    public float GetPheromone(Vector2Int cell, int type)
    {
        var gc = gridDataGenerator.GetCell(cell.x, cell.y);
        if (gc != null && type >= 0 && type < gc.pheromones.Length)
            return gc.pheromones[type];
        return 0f;
    }

    /// <summary>
    /// Returns the pheromone array for a given cell.
    /// </summary>
    public float[] GetPheromones(Vector2Int cell)
    {
        var gc = gridDataGenerator.GetCell(cell.x, cell.y);
        return gc?.pheromones;
    }

    /// <summary>
    /// Mark a structure as discovered to avoid redundant marking.
    /// </summary>
    public void MarkStructure(Vector2Int cell)
    {
        markedStructures.Add(cell);
    }

    public bool IsStructureMarked(Vector2Int cell)
    {
        return markedStructures.Contains(cell);
    }

    public void ResetMarkedStructures()
    {
        markedStructures.Clear();
    }

    /// <summary>
    /// Diffuse pheromones to neighboring cells (simple blur).
    /// </summary>
    private void DiffusePheromones()
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int pheromoneTypes = gridDataGenerator.GetCell(0, 0).pheromones.Length;

        // Temporary buffer to store new values
        float[,,] buffer = new float[width, height, pheromoneTypes];

        // Copy current values
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int t = 0; t < pheromoneTypes; t++)
            buffer[x, y, t] = gridDataGenerator.GetCell(x, y).pheromones[t];

        // Diffuse
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            for (int t = 0; t < pheromoneTypes; t++)
            {
                float share = gc.pheromones[t] * diffusionRate / 4f;
                foreach (var n in GetNeighbors(x, y, width, height))
                {
                    buffer[n.x, n.y, t] += share;
                    buffer[x, y, t] -= share;
                }
            }
        }

        // Write back
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int t = 0; t < pheromoneTypes; t++)
            gridDataGenerator.GetCell(x, y).pheromones[t] = Mathf.Max(0f, buffer[x, y, t]);
    }

    /// <summary>
    /// Decay pheromones over time.
    /// </summary>
    private void DecayPheromones()
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int pheromoneTypes = gridDataGenerator.GetCell(0, 0).pheromones.Length;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int t = 0; t < pheromoneTypes; t++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            gc.pheromones[t] = Mathf.Max(0f, gc.pheromones[t] - decayRate);
        }
    }

    /// <summary>
    /// Get 4-way neighbors for diffusion.
    /// </summary>
    private IEnumerable<Vector2Int> GetNeighbors(int x, int y, int width, int height)
    {
        if (x > 0) yield return new Vector2Int(x - 1, y);
        if (x < width - 1) yield return new Vector2Int(x + 1, y);
        if (y > 0) yield return new Vector2Int(x, y - 1);
        if (y < height - 1) yield return new Vector2Int(x, y + 1);
    }

    /// <summary>
    /// Reset all pheromones in the grid (e.g. after battle).
    /// </summary>
    public void ResetAllPheromones()
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int pheromoneTypes = gridDataGenerator.GetCell(0, 0).pheromones.Length;

        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        for (int t = 0; t < pheromoneTypes; t++)
            gridDataGenerator.GetCell(x, y).pheromones[t] = 0f;

        ResetMarkedStructures();
    }

    // Optional: Add a visual debug toggle
    [Header("Debug")]
    public bool visualizeInScene = true;

    private void OnDrawGizmos()
    {
        if (!visualizeInScene || !Application.isPlaying || gridDataGenerator == null)
            return;
            
        // Very simple visualization - just to confirm pheromones exist
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var cell = gridDataGenerator.GetCell(x, y);
            if (cell != null && cell.pheromones[0] > 0.01f)
            {
                float intensity = Mathf.Clamp01(cell.pheromones[0] / 5.0f);
                Gizmos.color = new Color(1f, 0f, 1f, intensity * 0.5f);
                Vector3 pos = cell.worldPosition + Vector3.up * 0.05f;
                Gizmos.DrawCube(pos, new Vector3(0.8f, 0.01f, 0.8f));
            }
        }
    }

    /// <summary>
    /// Applies multiple diffusion passes to ensure wide pheromone distribution
    /// </summary>
    public void ApplyDiffusionOnly()
    {
        // Apply more diffusion passes to ensure wide coverage
        int extraDiffusionPasses = 5;
        for (int i = 0; i < diffusionIterations * extraDiffusionPasses; i++)
            DiffusePheromones();
    }

    /// <summary>
    /// Call this manually to clear all pheromones before a new run
    /// </summary>
    public void PrepareForNewRun()
    {
        ResetAllPheromones();
    }

    /// <summary>
    /// Applies wide distribution and equalizes pheromone concentrations
    /// to create more balanced, wider paths 
    /// </summary>
    public void ApplyEvenDistribution()
    {
        Debug.Log($"Starting even distribution with range: {distributionRange}");
        
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int pheromoneTypes = gridDataGenerator.GetCell(0, 0).pheromones.Length;
        
        // Create a temporary buffer for the new values
        float[,,] buffer = new float[width, height, pheromoneTypes];
        
        // Track affected cells for debugging
        int totalAffectedCells = 0;
        
        // Process each cell
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            if (gc == null) continue;
            
            for (int t = 0; t < pheromoneTypes; t++)
            {
                if (gc.pheromones[t] <= 0.01f) continue; // Skip cells with negligible pheromone
                
                // Get source pheromone value
                float sourceValue = gc.pheromones[t];
                totalAffectedCells++;
                
                // Distribute 70% of the pheromone to surrounding cells
                float valueToDistribute = sourceValue * 0.7f;
                
                // Keep 30% at the source
                buffer[x, y, t] += sourceValue * 0.3f;
                
                // Apply wide distribution based on range
                ApplyDistributionFromCell(x, y, t, valueToDistribute, buffer, width, height);
            }
        }
        
        // Apply the buffer values back to the grid
        ApplyBufferToGrid(buffer, width, height, pheromoneTypes);
        
        // Perform equalization step
        EqualizeValues(pheromoneTypes);
        
        Debug.Log($"Completed even distribution with range: {distributionRange}, affected {totalAffectedCells} source cells");
    }

    private void ApplyDistributionFromCell(int x, int y, int type, float valueToDistribute, 
                                          float[,,] buffer, int width, int height)
    {
        int range = distributionRange;
        var visited = new bool[width, height];
        var queue = new Queue<(int cx, int cy, int dist)>();
        queue.Enqueue((x, y, 0));
        visited[x, y] = true;

        // Collect all cells within range (excluding the source cell)
        var targets = new List<(int tx, int ty, int dist)>();
        while (queue.Count > 0)
        {
            var (cx, cy, dist) = queue.Dequeue();
            if (dist > 0) targets.Add((cx, cy, dist));
            if (dist >= range) continue;

            // 4-way neighbors
            foreach (var (nx, ny) in new (int, int)[] { (cx-1, cy), (cx+1, cy), (cx, cy-1), (cx, cy+1) })
            {
                if (nx >= 0 && nx < width && ny >= 0 && ny < height && !visited[nx, ny])
                {
                    visited[nx, ny] = true;
                    queue.Enqueue((nx, ny, dist + 1));
                }
            }
        }

        if (targets.Count == 0) return;

        // Distribute value evenly or with falloff
        float totalWeight = 0f;
        var weights = new List<float>();
        foreach (var (_, _, dist) in targets)
        {
            float falloff = 1f - (float)dist / (range + 0.001f);
            float w = Mathf.Max(0.01f, falloff * falloff); // quadratic falloff
            weights.Add(w);
            totalWeight += w;
        }

        for (int i = 0; i < targets.Count; i++)
        {
            var (tx, ty, _) = targets[i];
            float portion = valueToDistribute * (weights[i] / totalWeight);
            buffer[tx, ty, type] += portion;
        }
    }

    private void ApplyBufferToGrid(float[,,] buffer, int width, int height, int pheromoneTypes)
    {
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            if (gc == null) continue;
            
            for (int t = 0; t < pheromoneTypes; t++)
            {
                gc.pheromones[t] = buffer[x, y, t];
            }
        }
    }

    private void EqualizeValues(int pheromoneTypes)
    {
        // Find min/max values for each type
        float[] minValues = new float[pheromoneTypes];
        float[] maxValues = new float[pheromoneTypes];
        
        for (int t = 0; t < pheromoneTypes; t++)
        {
            minValues[t] = float.MaxValue;
            maxValues[t] = 0f;
        }
        
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        
        // Find min/max values
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            if (gc == null) continue;
            
            for (int t = 0; t < pheromoneTypes; t++)
            {
                if (gc.pheromones[t] > 0.01f)
                {
                    minValues[t] = Mathf.Min(minValues[t], gc.pheromones[t]);
                    maxValues[t] = Mathf.Max(maxValues[t], gc.pheromones[t]);
                }
            }
        }
        
        // Apply equalization
        for (int x = 0; x < width; x++)
        for (int y = 0; y < height; y++)
        {
            var gc = gridDataGenerator.GetCell(x, y);
            if (gc == null) continue;
            
            for (int t = 0; t < pheromoneTypes; t++)
            {
                if (gc.pheromones[t] <= 0.01f) continue;
                
                // Normalize and apply equalization curve
                float range = maxValues[t] - minValues[t];
                if (range > 0.01f)
                {
                    float normalizedValue = (gc.pheromones[t] - minValues[t]) / range;
                    float equalizedValue = Mathf.Pow(normalizedValue, 0.5f); // Square root for more even distribution
                    gc.pheromones[t] = minValues[t] + (equalizedValue * range);
                }
            }
        }
    }

    /// <summary>
    /// Spread pheromones from multiple sources using BFS for even distribution
    /// </summary>
    public void SpreadPheromonesBFS(int spreadRange = 5, float falloff = 0.7f)
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();

        foreach (var (sourceCell, type, amount) in newPheromoneSources)
        {
            var visited = new bool[width, height];
            var queue = new Queue<(Vector2Int cell, int dist)>();
            queue.Enqueue((sourceCell, 0));
            visited[sourceCell.x, sourceCell.y] = true;

            while (queue.Count > 0)
            {
                var (cell, dist) = queue.Dequeue();
                float value = amount * Mathf.Pow(falloff, dist);
                if (value < 0.01f) continue;

                var gc = gridDataGenerator.GetCell(cell.x, cell.y);
                if (gc != null && type >= 0 && type < gc.pheromones.Length)
                    gc.pheromones[type] += value;

                if (dist < spreadRange)
                {
                    foreach (var n in GetNeighbors(cell.x, cell.y, width, height))
                    {
                        if (!visited[n.x, n.y])
                        {
                            visited[n.x, n.y] = true;
                            queue.Enqueue((n, dist + 1));
                        }
                    }
                }
            }
        }

        newPheromoneSources.Clear();
    }

    // Add this method to ensure regular processing of pheromone sources
    public void Update()
    {
        // Process any registered pheromone sources
        if (newPheromoneSources.Count > 0 && enableAutoUpdate)
        {
            SpreadPheromonesBFS();
            DiffusePheromones();
        }
    }
}
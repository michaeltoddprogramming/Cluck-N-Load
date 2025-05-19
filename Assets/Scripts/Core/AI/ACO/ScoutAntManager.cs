using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the lifecycle and spawning of virtual scout ant agents for ACO navigation.
/// Handles event-driven triggers and staggered updates.
/// </summary>
public class ScoutAntManager : MonoBehaviour
{
    [Header("Ant Settings")]
    public int maxActiveAnts = 10;
    public float spawnDelay = 0.5f; // Delay between spawns when triggered

    [Header("Ant Movement")]
    [Tooltip("0 = instant calculation, 1 = normal speed, >1 = slower")]
    public float speedModifier = 1.0f;
    public float moveSpeed = 5.0f;
    public float lifetime = 20f;
    public float pheromoneStrength = 1.0f;

    [Header("Phase Durations")]
    public float snoopDuration = 3.0f;
    public float scoutingDuration = 8.0f;
    public int maxTargets = 3;

    [Header("References")]
    public GridController gridController;
    public GridDataGenerator gridDataGenerator;
    public PheromoneManager pheromoneManager; // Add this reference

    private List<ScoutAntAgent> activeAnts = new List<ScoutAntAgent>();
    private bool spawnPending = false;

    private void Awake()
    {
        if (gridController == null)
            gridController = FindObjectOfType<GridController>();
        if (gridDataGenerator == null)
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
        if (pheromoneManager == null)
            pheromoneManager = FindObjectOfType<PheromoneManager>();
    }

    /// <summary>
    /// Call this to trigger a new wave of scout ants (e.g. on building destruction or nightfall).
    /// </summary>
    public void TriggerAnts()
    {
        if (!spawnPending)
            StartCoroutine(SpawnAntsWithDelay());
    }

    private IEnumerator SpawnAntsWithDelay()
    {
        spawnPending = true;

        int antsToSpawn = Mathf.Min(maxActiveAnts - activeAnts.Count, maxActiveAnts);
        for (int i = 0; i < antsToSpawn; i++)
        {
            SpawnAntAtEdge();
            yield return new WaitForSeconds(spawnDelay);
        }

        spawnPending = false;
    }

    private void SpawnAntAtEdge()
    {
        Vector2Int spawnCell = GetRandomEdgeCell();
        var ant = new ScoutAntAgent(
            this, 
            gridController, 
            gridDataGenerator, 
            pheromoneManager, // Pass pheromone manager
            spawnCell
        );
        
        // Configure ant with manager settings
        ant.speedModifier = speedModifier;
        ant.moveSpeed = moveSpeed;
        ant.lifetime = lifetime;
        ant.snoopDuration = snoopDuration;
        ant.scoutingDuration = scoutingDuration;
        ant.pheromoneStrength = pheromoneStrength;
        ant.maxTargets = maxTargets;
        
        activeAnts.Add(ant);
    }

    private Vector2Int GetRandomEdgeCell()
    {
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int edge = Random.Range(0, 4);
        int x = 0, y = 0;
        switch (edge)
        {
            case 0: x = Random.Range(0, width); y = height - 1; break;
            case 1: x = Random.Range(0, width); y = 0; break;
            case 2: x = 0; y = Random.Range(0, height); break;
            case 3: x = width - 1; y = Random.Range(0, height); break;
        }
        return new Vector2Int(x, y);
    }

    public void OnAntFinished(ScoutAntAgent ant)
    {
        activeAnts.Remove(ant);
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        
        // If speedModifier is 0, process ALL ants COMPLETELY in this frame
        if (speedModifier == 0f)
        {
            // Process all ants to completion
            for (int i = activeAnts.Count - 1; i >= 0; i--)
            {
                // For instant calculation, we keep updating until the ant is done
                ScoutAntAgent ant = activeAnts[i];
                
                // Process the entire life of the ant at once
                while (i < activeAnts.Count && activeAnts.Contains(ant))
                {
                    ant.AntUpdate(9999f); // Use a large value to force completion
                }
            }
        }
        else
        {
            // Update all ants every frame to maintain consistent speed
            for (int i = 0; i < activeAnts.Count; i++)
            {
                activeAnts[i].AntUpdate(dt);
            }
        }

        // Listen for Shift + A to trigger ants
        if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(KeyCode.A))
        {
            TriggerAnts();
        }
    }

    public int ActiveAntCount => activeAnts.Count;

    // Optional: For debugging/visualization
    public List<ScoutAntAgent> GetActiveAnts() => activeAnts;

    /// <summary>
    /// Runs a complete cycle of ant scouting and pheromone laying with extensive distribution
    /// </summary>
    public void RunCompleteScoutingCycle()
    {
        // Clear old pheromones before starting
        if (pheromoneManager != null)
            pheromoneManager.PrepareForNewRun();
            
        // Run the ants to completion
        TriggerAnts();
        
        // Wait for ants to finish - in practice you'd need to check when all ants are done
        StartCoroutine(ApplyDiffusionAfterAntsComplete());
    }

    private IEnumerator ApplyDiffusionAfterAntsComplete()
    {
        // Wait until all ants complete their journey
        while (activeAnts.Count > 0)
        {
            yield return new WaitForSeconds(0.5f);
        }
        
        // Apply multiple diffusion passes for wide coverage
        if (pheromoneManager != null)
        {
            // First process all registered pheromone sources
            pheromoneManager.SpreadPheromonesBFS(5, 0.7f);
            
            // Then apply diffusion for wider coverage
            pheromoneManager.ApplyDiffusionOnly();
            
            // Apply final enhancement to ensure edge-to-structure connectivity
            EnhanceCriticalPaths();
        }
    }

    // Helper to ensure strong paths from discovered structures to map edges
    private void EnhanceCriticalPaths()
    {
        // This would run through detected structures and ensure paths to edges
        // are strongly marked - implementation depends on how you track discoveries
        if (pheromoneManager != null)
        {
            // Sample implementation - real one would use actual discovered structures
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            // Find cells with high pheromone levels (likely structures)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (pheromoneManager.GetPheromone(cell, 0) > 5.0f)
                    {
                        // Found a likely structure cell, enhance paths toward edges
                        EnhancePathToNearestEdge(cell);
                    }
                }
            }
        }
    }

    // Enhance pheromone path from a cell to nearest map edge
    private void EnhancePathToNearestEdge(Vector2Int startCell)
    {
        // Simple gradient descent toward nearest edge
        Vector2Int current = startCell;
        int width = gridDataGenerator.GetGridWidth();
        int height = gridDataGenerator.GetGridHeight();
        int maxSteps = Mathf.Max(width, height);
        float enhancementFactor = 3.0f;
        
        for (int step = 0; step < maxSteps; step++)
        {
            // Enhance current cell
            pheromoneManager.LayPheromone(current, 0, pheromoneStrength * enhancementFactor);
            
            // Find direction toward nearest edge
            int distToLeft = current.x;
            int distToRight = width - 1 - current.x;
            int distToBottom = current.y;
            int distToTop = height - 1 - current.y;
            
            int minDist = Mathf.Min(distToLeft, distToRight, distToBottom, distToTop);
            Vector2Int next = current;
            
            if (minDist == distToLeft) next = new Vector2Int(current.x - 1, current.y);
            else if (minDist == distToRight) next = new Vector2Int(current.x + 1, current.y);
            else if (minDist == distToBottom) next = new Vector2Int(current.x, current.y - 1);
            else next = new Vector2Int(current.x, current.y + 1);
            
            // Check if we're at the edge
            if (minDist == 0) break;
            
            // Move to next cell
            current = next;
            enhancementFactor *= 0.9f; // Gradually reduce enhancement as we move away
        }
    }
}
using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    public class PheromoneLayers
    {
        // References
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        
        // Pheromone settings
        private float pheromoneLayInterval = 0.2f;
        private float basePheromoneStrength = 1f;
        private bool scalePheromonesByStructures = true;
        private float maxPheromoneStrength = 5f;
        private int pheromoneSpreadRadius = 1;
        private float[] pheromoneSpreadFactors = new float[] { 1.0f, 0.5f, 0.25f }; // Center, adjacent, diagonal
        private int defaultEnemyTypeIndex = 0; // 0=regular, 1=fast, 2=strong
        private bool showStructureDebug = true;
        
        // Statistics
        private int totalPheromonesLaid = 0;
        private float averageStrength = 0f;
        private float highestStrength = 0f;
        private int cellsWithPheromones = 0;
        
        public PheromoneLayers(GridController gridController, 
                             GridDataGenerator gridDataGenerator, 
                             bool showDebug)
        {
            this.gridController = gridController;
            this.gridDataGenerator = gridDataGenerator;
            this.showStructureDebug = showDebug;
        }
        
        // Initialize settings from AntManager
        public void InitializeSettings(AntManager manager)
        {
            // Extract settings from the manager
            pheromoneLayInterval = manager.PheromoneLayInterval;
            basePheromoneStrength = manager.BasePheromoneStrength;
            scalePheromonesByStructures = manager.ScalePheromonesByStructures;
            maxPheromoneStrength = manager.MaxPheromoneStrength;
            pheromoneSpreadRadius = manager.PheromoneSpreadRadius;
            defaultEnemyTypeIndex = manager.DefaultEnemyTypeIndex;
            showStructureDebug = manager.ShowStructureDebug;
        }
        
        // Reset statistics
        public void ResetStatistics()
        {
            totalPheromonesLaid = 0;
            averageStrength = 0f;
            highestStrength = 0f;
            cellsWithPheromones = 0;
        }
        
        // Main pheromone laying method
        public void LayPheromones(VirtualAnt ant, float updateInterval)
        {
            // Only lay pheromones when returning and at specified interval
            ant.lastPheromoneTime += updateInterval;
            if (ant.lastPheromoneTime < pheromoneLayInterval)
                return;
                
            ant.lastPheromoneTime = 0f;
            
            if (!gridController.IsValidCell(ant.position.x, ant.position.y))
                return;
                
            // Calculate pheromone strength
            float strength = basePheromoneStrength;
            
            // Scale by structures found
            if (scalePheromonesByStructures && ant.discoveredStructures.Count > 0)
            {
                strength = Mathf.Min(
                    basePheromoneStrength * (1f + ant.discoveredStructures.Count * 0.5f), 
                    maxPheromoneStrength
                );
            }
            
            // Optimization: Skip diffusion for weak pheromones
            int effectiveRadius = strength > 2f ? pheromoneSpreadRadius : 
                                  strength > 1f ? Mathf.Min(1, pheromoneSpreadRadius) : 0;
            
            // Apply pheromones
            ApplyPheromonesWithDiffusion(ant.position, strength, defaultEnemyTypeIndex, effectiveRadius);
        }

        private void ApplyPheromonesWithDiffusion(Vector2Int center, float strength, int enemyType, int radius)
        {
            // Apply to center cell
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
            
            // Optimization: Pre-compute factor indices
            int maxDistFactor = pheromoneSpreadFactors.Length - 1;
            
            // Apply to neighboring cells
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    // Skip center
                    if (x == 0 && y == 0)
                        continue;
                        
                    Vector2Int neighborPos = new Vector2Int(center.x + x, center.y + y);
                    if (!gridController.IsValidCell(neighborPos.x, neighborPos.y))
                        continue;
                        
                    GridCell neighborCell = gridController.GetCell(neighborPos.x, neighborPos.y);
                    if (neighborCell == null)
                        continue;
                        
                    // Calculate distance factor
                    int distFactor = (Mathf.Abs(x) + Mathf.Abs(y) == 1) ? 1 : 2;  // 1 for adjacent, 2 for diagonal
                     
                    // Apply pheromone
                    if (distFactor <= maxDistFactor)
                    {
                        neighborCell.pheromones[enemyType] += strength * pheromoneSpreadFactors[distFactor];
                    }
                }
            }
            
            // Update statistics less frequently
            totalPheromonesLaid++;
            if (totalPheromonesLaid % 10 == 0)
            {
                averageStrength = ((averageStrength * (totalPheromonesLaid - 1)) + strength) / totalPheromonesLaid;
                highestStrength = Mathf.Max(highestStrength, strength);
            }
            
            // Count cells very infrequently
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
            int samplesPerAxis = 10;
            
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
            
            // Estimate total based on sampling
            float samplingRatio = (float)(samplesPerAxis * samplesPerAxis) / (width * height);
            cellsWithPheromones = Mathf.RoundToInt(count / samplingRatio);
        }
        
        // Public API
        public int GetTotalPheromonesLaid() => totalPheromonesLaid;
        public float GetAverageStrength() => averageStrength;
        public float GetHighestStrength() => highestStrength;
        public int GetCellsWithPheromones() => cellsWithPheromones;
    }
}
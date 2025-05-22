using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    [RequireComponent(typeof(MeshRenderer))]
    public class PheromoneVisualizer : MonoBehaviour
    {
        [Header("Visualization Settings")]
        [SerializeField] private bool showPheromones = true;
        [SerializeField] private float updateInterval = 0.5f;
        
        [Header("Pheromone Colors")]
        [SerializeField] private Color regularEnemyColor = new Color(1f, 0f, 0f, 0.5f); // Red
        [SerializeField] private Color fastEnemyColor = new Color(0f, 1f, 0f, 0.5f);    // Green
        [SerializeField] private Color strongEnemyColor = new Color(0f, 0f, 1f, 0.5f);  // Blue
        
        [Header("Intensity Settings")]
        [SerializeField] private float maxPheromoneIntensity = 5f;
        [SerializeField] private float minAlpha = 0.1f;
        [SerializeField] private float maxAlpha = 0.7f;
        
        // References
        private GridController gridController;
        private GridDataGenerator gridDataGenerator;
        private MeshRenderer meshRenderer;
        private Texture2D pheromoneTexture;
        private float updateTimer = 0f;
        
        private void Start()
        {
            gridController = FindObjectOfType<GridController>();
            gridDataGenerator = FindObjectOfType<GridDataGenerator>();
            meshRenderer = GetComponent<MeshRenderer>();
            
            if (gridController == null || gridDataGenerator == null || meshRenderer == null)
            {
                Debug.LogError("PheromoneVisualizer is missing required references");
                enabled = false;
                return;
            }
            
            // Create a new texture for the pheromone visualization
            CreatePheromoneTexture();
            
            // Apply it to the mesh
            ApplyTextureToMesh();
        }
        
        private void Update()
        {
            if (!showPheromones)
                return;
                
            updateTimer += Time.deltaTime;
            
            // Update visualization at specified interval
            if (updateTimer >= updateInterval)
            {
                UpdatePheromoneVisualization();
                updateTimer = 0f;
            }
        }
        
        private void CreatePheromoneTexture()
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            pheromoneTexture = new Texture2D(width, height);
            pheromoneTexture.filterMode = FilterMode.Point; // Sharp pixels for clear grid representation
            
            // Initialize to transparent
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }
            
            pheromoneTexture.SetPixels(pixels);
            pheromoneTexture.Apply();
        }
        
        private void ApplyTextureToMesh()
        {
            // Create a material that uses the texture
            Material material = new Material(Shader.Find("Unlit/Transparent"));
            material.mainTexture = pheromoneTexture;
            
            // Position the mesh correctly over the grid
            transform.position = new Vector3(
                gridDataGenerator.GetGridWidth() / 2f,
                0.05f, // Just above the ground
                gridDataGenerator.GetGridHeight() / 2f
            );
            
            // Scale the mesh to match the grid size
            transform.localScale = new Vector3(
                gridDataGenerator.GetGridWidth(),
                1f,
                gridDataGenerator.GetGridHeight()
            );
            
            // Apply the material
            meshRenderer.material = material;
        }
        
        // Add this method to force an immediate update
        public void ForceUpdate()
        {
            if (!enabled) return;
            UpdatePheromoneVisualization();
        }

        // Optimize the UpdatePheromoneVisualization method
        private void UpdatePheromoneVisualization()
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            // Skip every other cell to improve performance
            int skipFactor = 2;
            
            // Find highest value using sampling
            float highestValue = 0.1f;
            int sampleCount = 0;
            int maxSamples = 200;
            
            for (int x = 0; x < width; x += skipFactor)
            {
                for (int y = 0; y < height; y += skipFactor)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null)
                    {
                        for (int i = 0; i < cell.pheromones.Length; i++)
                        {
                            highestValue = Mathf.Max(highestValue, cell.pheromones[i]);
                        }
                    }
                    
                    sampleCount++;
                    if (sampleCount >= maxSamples)
                        break;
                }
                if (sampleCount >= maxSamples)
                    break;
            }
            
            // Cap the highest value
            highestValue = Mathf.Min(highestValue, maxPheromoneIntensity);
            
            Color[] pixels = new Color[width * height];
            
            // Process only cells with non-zero pheromones for performance
            for (int x = 0; x < width; x += skipFactor)
            {
                for (int y = 0; y < height; y += skipFactor)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell == null) continue;
                    
                    Color cellColor = Color.clear;
                    bool hasPheromones = false;
                    
                        // Process all pheromone types instead of limiting to defaultEnemyTypeIndex
                    for (int i = 0; i < cell.pheromones.Length; i++)
                    {
                        float pheromoneValue = cell.pheromones[i];
                        if (pheromoneValue > 0.01f)
                        {
                            hasPheromones = true;
                            float intensity = Mathf.Clamp01(pheromoneValue / highestValue);
                            float alpha = Mathf.Lerp(minAlpha, maxAlpha, intensity);
                            
                            Color pheromoneColor;
                            switch(i)
                            {
                                case 0: pheromoneColor = regularEnemyColor; break;
                                case 1: pheromoneColor = fastEnemyColor; break;
                                case 2: pheromoneColor = strongEnemyColor; break;
                                default: pheromoneColor = regularEnemyColor; break;
                            }
                            
                            pheromoneColor.a = alpha;
                            cellColor = BlendColors(cellColor, pheromoneColor);
                        }
                    }
                    
                    if (hasPheromones)
                    {
                        // Set this pixel
                        pixels[y * width + x] = cellColor;
                        
                        // Fill adjacent pixels with the same color (for the skipped cells)
                        for (int fillX = 0; fillX < skipFactor; fillX++)
                        {
                            for (int fillY = 0; fillY < skipFactor; fillY++)
                            {
                                int px = x + fillX;
                                int py = y + fillY;
                                if (px < width && py < height)
                                {
                                    pixels[py * width + px] = cellColor;
                                }
                            }
                        }
                    }
                }
            }
            
            pheromoneTexture.SetPixels(pixels);
            pheromoneTexture.Apply();
        }
        
        private Color BlendColors(Color baseColor, Color addedColor)
        {
            // If base is transparent, just return the added color
            if (baseColor.a < 0.01f)
                return addedColor;
                
            // Add the colors together, preserving alpha
            float resultAlpha = baseColor.a + addedColor.a * (1 - baseColor.a);
            if (resultAlpha < 0.01f)
                return Color.clear;
                
            float r = (baseColor.r * baseColor.a + addedColor.r * addedColor.a * (1 - baseColor.a)) / resultAlpha;
            float g = (baseColor.g * baseColor.a + addedColor.g * addedColor.a * (1 - baseColor.a)) / resultAlpha;
            float b = (baseColor.b * baseColor.a + addedColor.b * addedColor.a * (1 - baseColor.a)) / resultAlpha;
            
            return new Color(r, g, b, resultAlpha);
        }
        
        // Optional debug visualization for immediate feedback in scene view
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !showPheromones || gridController == null)
                return;
                
            int width = gridDataGenerator?.GetGridWidth() ?? 0;
            int height = gridDataGenerator?.GetGridHeight() ?? 0;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = gridController.GetCell(x, y);
                    if (cell != null)
                    {
                        Vector3 worldPos = gridController.GetCellCenterFromTexture(x, y);
                        worldPos.y = 0.05f;
                        
                        // Show pheromone levels
                        float totalStrength = 0f;
                        int strongestType = 0;
                        float strongestValue = 0f;
                        
                        for (int i = 0; i < cell.pheromones.Length; i++)
                        {
                            totalStrength += cell.pheromones[i];
                            if (cell.pheromones[i] > strongestValue)
                            {
                                strongestValue = cell.pheromones[i];
                                strongestType = i;
                            }
                        }
                        
                        if (totalStrength > 0.1f)
                        {
                            // Pick color based on dominant pheromone type
                            Color color;
                            switch (strongestType)
                            {
                                case 0: color = regularEnemyColor; break;
                                case 1: color = fastEnemyColor; break;
                                case 2: color = strongEnemyColor; break;
                                default: color = regularEnemyColor; break;
                            }
                            
                            // Scale alpha by strength
                            float normalizedStrength = Mathf.Clamp01(strongestValue / maxPheromoneIntensity);
                            color.a *= normalizedStrength;
                            
                            Gizmos.color = color;
                            
                            // Draw cube to represent pheromone
                            float size = 0.8f * normalizedStrength;
                            Gizmos.DrawCube(worldPos, new Vector3(size, 0.02f, size));
                        }
                    }
                }
            }
        }
    }
}
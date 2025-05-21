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
            pheromoneTexture.filterMode = FilterMode.Bilinear;
            
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
            Material material = new Material(Shader.Find("Unlit/Transparent"));
            material.mainTexture = pheromoneTexture;
            meshRenderer.material = material;
        }
        
        private void UpdatePheromoneVisualization()
        {
            int width = gridDataGenerator.GetGridWidth();
            int height = gridDataGenerator.GetGridHeight();
            
            Color[] pixels = new Color[width * height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    GridCell cell = gridDataGenerator.GetCell(x, y);
                    if (cell != null)
                    {
                        // Get pheromone values for the cell
                        float regularPheromone = cell.pheromones[0];
                        float fastPheromone = cell.pheromones[1];
                        float strongPheromone = cell.pheromones[2];
                        
                        // Blend colors based on pheromone values
                        Color cellColor = Color.clear;
                        
                        if (regularPheromone > 0)
                        {
                            float intensity = Mathf.Clamp01(regularPheromone / maxPheromoneIntensity);
                            float alpha = Mathf.Lerp(minAlpha, maxAlpha, intensity);
                            Color adjustedColor = regularEnemyColor;
                            adjustedColor.a = alpha;
                            cellColor = BlendColors(cellColor, adjustedColor);
                        }
                        
                        if (fastPheromone > 0)
                        {
                            float intensity = Mathf.Clamp01(fastPheromone / maxPheromoneIntensity);
                            float alpha = Mathf.Lerp(minAlpha, maxAlpha, intensity);
                            Color adjustedColor = fastEnemyColor;
                            adjustedColor.a = alpha;
                            cellColor = BlendColors(cellColor, adjustedColor);
                        }
                        
                        if (strongPheromone > 0)
                        {
                            float intensity = Mathf.Clamp01(strongPheromone / maxPheromoneIntensity);
                            float alpha = Mathf.Lerp(minAlpha, maxAlpha, intensity);
                            Color adjustedColor = strongEnemyColor;
                            adjustedColor.a = alpha;
                            cellColor = BlendColors(cellColor, adjustedColor);
                        }
                        
                        pixels[y * width + x] = cellColor;
                    }
                }
            }
            
            pheromoneTexture.SetPixels(pixels);
            pheromoneTexture.Apply();
        }
        
        private Color BlendColors(Color baseColor, Color addedColor)
        {
            // Simple additive blending with alpha
            if (baseColor.a < 0.01f)
                return addedColor;
                
            float alpha = baseColor.a + addedColor.a * (1 - baseColor.a);
            if (alpha < 0.01f)
                return Color.clear;
                
            float r = (baseColor.r * baseColor.a + addedColor.r * addedColor.a * (1 - baseColor.a)) / alpha;
            float g = (baseColor.g * baseColor.a + addedColor.g * addedColor.a * (1 - baseColor.a)) / alpha;
            float b = (baseColor.b * baseColor.a + addedColor.b * addedColor.a * (1 - baseColor.a)) / alpha;
            
            return new Color(r, g, b, alpha);
        }
    }
}
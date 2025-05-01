using System.Collections.Generic;
using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    /// <summary>
    /// Handles the visualization of flow fields for debugging.
    /// </summary>
    public class FlowFieldVisualizer : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private GridDataGenerator gridDataGenerator;
        [SerializeField] private FlowFieldSettings settings;
        [SerializeField] private FlowFieldTargetManager targetManager;
        
        // References to flow data
        private Dictionary<Vector2Int, float> flowStrengthMap;
        private Dictionary<Vector2Int, float> streamInfluenceMap;
        
        // Initialize with references to data sources
        public void Initialize(
            Dictionary<Vector2Int, float> flowStrength, 
            Dictionary<Vector2Int, float> streamInfluence)
        {
            flowStrengthMap = flowStrength;
            streamInfluenceMap = streamInfluence;
        }
        
        // This will be called by unity to draw debug gizmos
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || gridDataGenerator == null || 
                !gridDataGenerator.IsInitialized || !enabled)
                return;
                
            if (settings == null || targetManager == null)
                return;
                
            // Draw target point if enabled
            if (settings.visualizeTargetPoint)
            {
                DrawTargetPoint();
            }
            
            // Draw flow field if enabled
            if (settings.visualizeFlowField)
            {
                DrawFlowField();
            }
        }
        
        private void DrawTargetPoint()
        {
            Vector2Int target = targetManager.GetTargetCoordinates();
            if (targetManager.IsValidTarget(target))
            {
                GridCell targetCell = gridDataGenerator.GetCell(target.x, target.y);
                if (targetCell != null)
                {
                    Gizmos.color = settings.targetPointColor;
                    Gizmos.DrawSphere(targetCell.worldPosition, settings.arrowScale * 0.7f);
                    Gizmos.DrawWireCube(targetCell.worldPosition, new Vector3(1.2f, 0.1f, 1.2f));
                }
            }
        }
        
        private void DrawFlowField()
        {
            if (flowStrengthMap == null || streamInfluenceMap == null)
                return;
                
            int gridWidth = gridDataGenerator.GetGridWidth();
            int gridHeight = gridDataGenerator.GetGridHeight();
            
            for (int x = 0; x < gridWidth; x++)
            {
                for (int y = 0; y < gridHeight; y++)
                {
                    GridCell cell = gridDataGenerator.GetCell(x, y);
                    if (cell == null) continue;
                    
                    if (cell.flowDirection != Vector2.zero && cell.integrationCost != int.MaxValue)
                    {
                        DrawFlowArrow(cell, x, y);
                    }
                }
            }
        }
        
        private void DrawFlowArrow(GridCell cell, int x, int y)
        {
            Vector3 start = cell.worldPosition;
            Vector3 direction = new Vector3(cell.flowDirection.x, 0, cell.flowDirection.y);
            
            // Adjust arrow length based on flow strength for priority paths
            float strength = 1.0f;
            Vector2Int cellPos = new Vector2Int(x, y);
            if (flowStrengthMap.TryGetValue(cellPos, out float flowStrength))
            {
                // Make priority paths slightly longer
                strength = 1.0f + flowStrength * 0.5f;
            }
            
            Vector3 end = start + direction * settings.arrowScale * strength;
            
            // Calculate arrow color
            Color arrowColoring = CalculateArrowColor(cell, cellPos);
            
            Gizmos.color = arrowColoring;
            Gizmos.DrawLine(start, end);
            
            // Draw arrow head
            Vector3 right = Quaternion.Euler(0, 30, 0) * -direction * settings.arrowScale * 0.4f * strength;
            Vector3 left = Quaternion.Euler(0, -30, 0) * -direction * settings.arrowScale * 0.4f * strength;
            Gizmos.DrawLine(end, end + right);
            Gizmos.DrawLine(end, end + left);
            
            // Draw small circle at base for priority paths
            if (flowStrengthMap.TryGetValue(cellPos, out flowStrength) && flowStrength > 0.3f)
            {
                float circleSize = 0.1f + flowStrength * 0.1f;
                Gizmos.DrawWireSphere(start, circleSize);
            }
        }
        
        private Color CalculateArrowColor(GridCell cell, Vector2Int cellPos)
        {
            // Base color
            Color arrowColoring = settings.arrowColor;
            
            // Use angle-based color if randomness is enabled
            if (settings.directionRandomness > 0.0f)
            {
                float angle = Mathf.Atan2(cell.flowDirection.y, cell.flowDirection.x) * Mathf.Rad2Deg;
                angle = (angle + 360) % 360;
                float hue = angle / 360f;
                arrowColoring = Color.HSVToRGB(hue, 0.7f, 0.8f);
            }
            
            // Check if it's a priority path
            if (flowStrengthMap.TryGetValue(cellPos, out float flowStrength) && flowStrength > 0)
            {
                // Make priority paths whiter based on their strength
                arrowColoring = Color.Lerp(arrowColoring, settings.priorityPathColor, flowStrength);
            }
            // Check if it's influenced by a stream
            else if (streamInfluenceMap.TryGetValue(cellPos, out float influenceFactor) && influenceFactor > 0)
            {
                // Make influenced cells blend toward white based on influence factor
                arrowColoring = Color.Lerp(arrowColoring, settings.priorityPathColor, influenceFactor * 0.8f);
            }
            
            return arrowColoring;
        }
    }
}
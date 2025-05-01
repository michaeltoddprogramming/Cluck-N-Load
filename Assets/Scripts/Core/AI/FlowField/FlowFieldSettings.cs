using UnityEngine;

namespace FarmDefender.Core.AI.FlowField
{
    [CreateAssetMenu(fileName = "FlowFieldSettings", menuName = "FarmDefender/AI/Flow Field Settings")]
    public class FlowFieldSettings : ScriptableObject
    {
        [Header("Basic Flow Field Settings")]
        [Tooltip("Whether to use diagonal directions for movement")]
        public bool useDiagonalDirections = true;
        
        [Tooltip("Amount of randomness to add to flow directions (0 = none, 1 = max)")]
        [Range(0f, 1f)]
        public float directionRandomness = 0.1f;
        
        [Tooltip("Maximum angle for random direction variation in degrees")]
        [Range(0f, 180f)]
        public float maxRandomAngle = 45f;
        
        [Tooltip("Whether to use weighted diagonal paths (1.4x cost)")]
        public bool useWeightedDiagonals = true;

        [Header("Enhanced Flow Settings")]
        [Tooltip("Influence of direct path to target (0 = none, 1 = maximum)")]
        [Range(0f, 1f)]
        public float directTargetInfluence = 0.4f;
        
        [Tooltip("Strength of priority paths near obstacles")]
        [Range(0f, 1f)]
        public float obstaclePriorityStrength = 0.7f;
        
        [Tooltip("How many cells away from obstacles are affected by priority paths")]
        [Range(1, 5)]
        public int priorityPathRange = 2;

        [Header("Stream Influence Settings")]
        [Tooltip("How strongly other cells are influenced by priority streams")]
        [Range(0f, 1f)]
        public float streamInfluenceStrength = 0.5f;
        
        [Tooltip("Maximum distance for stream influence")]
        [Range(1, 10)]
        public int streamInfluenceRange = 4;

        [Header("Visualization Settings")]
        public bool visualizeFlowField = false;
        [Tooltip("Whether to draw the target center point")]
        public bool visualizeTargetPoint = true;
        public float arrowScale = 0.5f;
        public Color arrowColor = Color.blue;
        [Tooltip("Color for the target center point")]
        public Color targetPointColor = Color.red;
        [Tooltip("Color for priority paths")]
        public Color priorityPathColor = Color.white;
    }
}
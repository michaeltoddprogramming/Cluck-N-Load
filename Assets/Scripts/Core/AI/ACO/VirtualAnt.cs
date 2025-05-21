using UnityEngine;
using System.Collections.Generic;

namespace FarmDefender.Core.AI.ACO
{
    public class VirtualAnt
    {
        public Vector2Int position;
        public Vector2Int targetPosition;
        public float lifetime;
        public float lastPheromoneTime;
        public bool isReturning;
        public bool isInOwnedTerritory;
        public bool isSnooping;
        public Vector2Int lastDirection;
        public float currentFlowFieldInfluence;
        public float timeSinceLastStructureFound;
        public bool useFlowFieldForReturn;
        public int obstacleHitCount;
        
        // New properties for improved obstacle navigation
        public float flowFieldUseTimer;
        public float flowFieldUseDuration;
        
        public HashSet<Vector2Int> discoveredStructures = new HashSet<Vector2Int>();
        public List<Vector2Int> visitedCells = new List<Vector2Int>();
        
        public VirtualAnt(Vector2Int position, float lifetime)
        {
            this.position = position;
            this.lifetime = lifetime;
            this.lastPheromoneTime = 0f;
            this.isReturning = false;
            this.isInOwnedTerritory = false;
            this.isSnooping = false;
            this.lastDirection = Vector2Int.zero;
            this.targetPosition = Vector2Int.zero;
            this.currentFlowFieldInfluence = 1f;
            this.timeSinceLastStructureFound = 0f;
            this.useFlowFieldForReturn = false;
            this.obstacleHitCount = 0;
            this.flowFieldUseTimer = 0f;
            this.flowFieldUseDuration = 0.5f; // Default duration to use flow field (seconds)
        }
    }
}
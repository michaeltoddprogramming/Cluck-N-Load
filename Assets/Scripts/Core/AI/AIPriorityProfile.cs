using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AIPriorityProfile", menuName = "AI/AIPriorityProfile")]
public class AIPriorityProfile : ScriptableObject
{
    public AIAgent.EnemyType enemyType;
    public List<AITargetPriority> priorities = new List<AITargetPriority>();
}

[System.Serializable]
public struct AITargetPriority
{
    public AITargetType targetType;
    public int priority; // 1 = highest
}
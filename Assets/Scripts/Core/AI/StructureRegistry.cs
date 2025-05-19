using System.Collections.Generic;
using UnityEngine;

public static class StructureRegistry
{
    private static List<Structure> _allStructures = new List<Structure>();
    public static IReadOnlyList<Structure> AllStructures => _allStructures;

    public static void Register(Structure s)
    {
        if (!_allStructures.Contains(s))
            _allStructures.Add(s);
    }
    public static void Unregister(Structure s)
    {
        _allStructures.Remove(s);
    }

    public static Structure GetMainBuilding()
    {
        return _allStructures.Find(s => s.structureData.aiTargetType == AITargetType.MainBuilding);
    }

    public static Structure GetClosestStructureOfType(Vector3 pos, AITargetType type)
    {
        Structure closest = null;
        float closestDist = float.MaxValue;
        foreach (var s in _allStructures)
        {
            if (s.structureData.aiTargetType != type) continue;
            float dist = Vector3.Distance(pos, s.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = s;
            }
        }
        return closest;
    }
}
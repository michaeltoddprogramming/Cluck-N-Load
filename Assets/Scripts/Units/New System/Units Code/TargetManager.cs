using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    private List<MonoBehaviour> allTargets = new List<MonoBehaviour>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    public void RegisterTarget(MonoBehaviour target)
    {
        if (!allTargets.Contains(target))
            allTargets.Add(target);
    }

    public void UnregisterTarget(MonoBehaviour target)
    {
        allTargets.Remove(target);
    }

    public MonoBehaviour GetNearestAggroTargetOptimized(EnemyData data, Vector3 position)
    {
        MonoBehaviour nearest = null;
        float closestDist = float.MaxValue;

        // 1. Preferred attack type
        foreach (var col in GetTargetsOfType(data.AttType))
        {
            if (col == null) continue; // <-- skip destroyed objects

            if (col is ArmyUnit unit && unit.GetArmyType() == ArmyType.Sheep)
                continue;

            MonoBehaviour candidate = null;

            switch (data.AttType)
            {
                case AttType.Animals:
                    candidate = col as ArmyUnit;
                    break;
                case AttType.Resources:
                    if (col is CropStructure crop) candidate = crop;
                    else if (col is SiloStructure silo) candidate = silo;
                    break;
                case AttType.Defense:
                    candidate = col as DefenseStructure;
                    break;
                case AttType.Buildings:
                    if (col is CropStructure crop2) candidate = crop2;
                    else if (col is SiloStructure silo2) candidate = silo2;
                    else if (col is BarracksStructure barracks) candidate = barracks;
                    else if (col is AnimalStructure animal) candidate = animal;
                    break;
            }

            if (candidate != null && !IsTargetDead(candidate))
            {
                float dist = Vector3.Distance(position, candidate.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    nearest = candidate;
                }
            }
        }

        // 2. Fallback: any type
        if (nearest == null)
        {
            foreach (var col in GetAllTargets())
            {
                if (col == null) continue; // <-- skip destroyed

                if (col is DefenseStructure && data.AttType != AttType.Defense)
                    continue;

                if (!IsTargetDead(col))
                {
                    float dist = Vector3.Distance(position, col.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        nearest = col;
                    }
                }
            }
        }

        // 3. Farmhouse fallback
        if (nearest == null)
        {
            foreach (var s in GetAllStructures())
            {
                if (s == null) continue; // <-- skip destroyed

                if (s.GetType() == typeof(Structure) && !IsTargetDead(s))
                {
                    nearest = s;
                    break; // first farmhouse only
                }
            }
        }

        return nearest;
    }


    public List<MonoBehaviour> GetTargetsOfType(AttType attType)
    {
        return allTargets.Where(t =>
            (attType == AttType.Animals && t is ArmyUnit unit && unit.GetArmyType() != ArmyType.Sheep) ||
            (attType == AttType.Resources && (t is CropStructure || t is SiloStructure)) ||
            (attType == AttType.Defense && t is DefenseStructure) ||
            (attType == AttType.Buildings && (t is CropStructure || t is SiloStructure || t is BarracksStructure || t is AnimalStructure))
        ).ToList();
    }

    public List<Structure> GetAllStructures()
    {
        return allTargets.OfType<Structure>().ToList();
    }

    public List<MonoBehaviour> GetAllTargets()
    {
        return allTargets;
    }

    private bool IsTargetDead(MonoBehaviour target)
    {
        return target switch
        {
            ArmyUnit u => u.IsDead(),
            CropStructure u => u.IsDead(),
            SiloStructure u => u.IsDead(),
            DefenseStructure u => u.IsDead(),
            // Structure u => u.IsDead(),
            BarracksStructure u => u.IsDead(),
            AnimalStructure u => u.IsDead(),
            Structure u when u.GetType() == typeof(Structure) => u.IsDead(),
            _ => true // Assume dead if type unknown
        };
    }

}

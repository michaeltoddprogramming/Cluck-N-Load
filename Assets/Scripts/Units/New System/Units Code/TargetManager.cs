using UnityEngine;
using System.Collections.Generic;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance { get; private set; }

    // ========= OPTIMIZED DATA STRUCTURES =========
    // Separate lists by type - avoids LINQ filtering every frame
    private List<ArmyUnit> armyUnits = new List<ArmyUnit>();
    private List<CropStructure> cropStructures = new List<CropStructure>();
    private List<SiloStructure> siloStructures = new List<SiloStructure>();
    private List<DefenseStructure> defenseStructures = new List<DefenseStructure>();
    private List<BarracksStructure> barracksStructures = new List<BarracksStructure>();
    private List<AnimalStructure> animalStructures = new List<AnimalStructure>();
    private List<Structure> farmhouses = new List<Structure>();

    // Cache for nearest target queries - reduces repeated searches
    private Dictionary<int, CachedTargetResult> targetCache = new Dictionary<int, CachedTargetResult>();
    private const float CACHE_DURATION = 0.5f; // Re-search every 0.5 seconds

    // Periodic cleanup instead of checking nulls every frame
    private float lastCleanupTime;
    private const float CLEANUP_INTERVAL = 2f;

    private struct CachedTargetResult
    {
        public MonoBehaviour target;
        public float timestamp;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Update()
    {
        // Periodic cleanup of destroyed/dead targets
        if (Time.time - lastCleanupTime > CLEANUP_INTERVAL)
        {
            CleanupDeadTargets();
            lastCleanupTime = Time.time;
        }
    }

    // ========= REGISTRATION (Type-Based) =========
    public void RegisterTarget(MonoBehaviour target)
    {
        // Register to appropriate typed list
        switch (target)
        {
            case ArmyUnit unit:
                if (!armyUnits.Contains(unit)) armyUnits.Add(unit);
                break;
            case CropStructure crop:
                if (!cropStructures.Contains(crop)) cropStructures.Add(crop);
                break;
            case SiloStructure silo:
                if (!siloStructures.Contains(silo)) siloStructures.Add(silo);
                break;
            case DefenseStructure defense:
                if (!defenseStructures.Contains(defense)) defenseStructures.Add(defense);
                break;
            case BarracksStructure barracks:
                if (!barracksStructures.Contains(barracks)) barracksStructures.Add(barracks);
                break;
            case AnimalStructure animal:
                if (!animalStructures.Contains(animal)) animalStructures.Add(animal);
                break;
            case Structure farmhouse when farmhouse.GetType() == typeof(Structure):
                if (!farmhouses.Contains(farmhouse)) farmhouses.Add(farmhouse);
                break;
        }
    }

    public void UnregisterTarget(MonoBehaviour target)
    {
        // Remove from appropriate list
        switch (target)
        {
            case ArmyUnit unit: armyUnits.Remove(unit); break;
            case CropStructure crop: cropStructures.Remove(crop); break;
            case SiloStructure silo: siloStructures.Remove(silo); break;
            case DefenseStructure defense: defenseStructures.Remove(defense); break;
            case BarracksStructure barracks: barracksStructures.Remove(barracks); break;
            case AnimalStructure animal: animalStructures.Remove(animal); break;
            case Structure farmhouse: farmhouses.Remove(farmhouse); break;
        }

        // Clear any cached results for this enemy
        targetCache.Clear();
    }

    // ========= OPTIMIZED TARGET FINDING =========
    public MonoBehaviour GetNearestAggroTargetOptimized(EnemyData data, Vector3 position, int enemyID = 0)
    {
        // Check cache first (if enemy provides unique ID)
        if (enemyID != 0 && targetCache.TryGetValue(enemyID, out CachedTargetResult cached))
        {
            if (Time.time - cached.timestamp < CACHE_DURATION && 
                cached.target != null && 
                !IsTargetDead(cached.target))
            {
                return cached.target;
            }
        }

        MonoBehaviour nearest = null;
        float closestDistSqr = float.MaxValue; // Use squared distance (faster, no sqrt)

        // 1. Search preferred type first
        nearest = FindNearestInPreferredType(data.AttType, position, ref closestDistSqr);

        // 2. Fallback: search all types
        if (nearest == null)
        {
            nearest = FindNearestInAllTypes(data.AttType, position, ref closestDistSqr);
        }

        // 3. Final fallback: farmhouse
        if (nearest == null && farmhouses.Count > 0)
        {
            nearest = FindNearestInList(farmhouses, position);
        }

        // Cache result
        if (enemyID != 0 && nearest != null)
        {
            targetCache[enemyID] = new CachedTargetResult 
            { 
                target = nearest, 
                timestamp = Time.time 
            };
        }

        return nearest;
    }

    // ========= HELPER METHODS (No Allocations) =========
    private MonoBehaviour FindNearestInPreferredType(AttType attType, Vector3 position, ref float closestDistSqr)
    {
        MonoBehaviour nearest = null;

        switch (attType)
        {
            case AttType.Animals:
                // Search army units, exclude sheep
                for (int i = 0; i < armyUnits.Count; i++)
                {
                    var unit = armyUnits[i];
                    if (unit == null || unit.IsDead() || unit.GetArmyType() == ArmyType.Sheep)
                        continue;

                    float distSqr = (unit.transform.position - position).sqrMagnitude;
                    if (distSqr < closestDistSqr)
                    {
                        closestDistSqr = distSqr;
                        nearest = unit;
                    }
                }
                break;

            case AttType.Resources:
                // Search crops and silos
                nearest = CheckList(cropStructures, position, ref closestDistSqr, nearest);
                nearest = CheckList(siloStructures, position, ref closestDistSqr, nearest);
                break;

            case AttType.Defense:
                // Search defense structures
                nearest = CheckList(defenseStructures, position, ref closestDistSqr, nearest);
                break;

            case AttType.Buildings:
                // Search all building types
                nearest = CheckList(cropStructures, position, ref closestDistSqr, nearest);
                nearest = CheckList(siloStructures, position, ref closestDistSqr, nearest);
                nearest = CheckList(barracksStructures, position, ref closestDistSqr, nearest);
                nearest = CheckList(animalStructures, position, ref closestDistSqr, nearest);
                break;
        }

        return nearest;
    }

    private MonoBehaviour FindNearestInAllTypes(AttType preferredType, Vector3 position, ref float closestDistSqr)
    {
        MonoBehaviour nearest = null;

        // Check all lists except defense (unless preferred)
        nearest = CheckList(armyUnits, position, ref closestDistSqr, nearest);
        nearest = CheckList(cropStructures, position, ref closestDistSqr, nearest);
        nearest = CheckList(siloStructures, position, ref closestDistSqr, nearest);
        nearest = CheckList(barracksStructures, position, ref closestDistSqr, nearest);
        nearest = CheckList(animalStructures, position, ref closestDistSqr, nearest);

        if (preferredType == AttType.Defense)
        {
            nearest = CheckList(defenseStructures, position, ref closestDistSqr, nearest);
        }

        return nearest;
    }

    // Generic list checker - works for any type with IsDead()
    private MonoBehaviour CheckList<T>(List<T> list, Vector3 position, ref float closestDistSqr, MonoBehaviour currentNearest) where T : MonoBehaviour
    {
        for (int i = 0; i < list.Count; i++)
        {
            var target = list[i];
            if (target == null || IsTargetDead(target))
                continue;

            float distSqr = (target.transform.position - position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                currentNearest = target;
            }
        }
        return currentNearest;
    }

    private MonoBehaviour FindNearestInList<T>(List<T> list, Vector3 position) where T : MonoBehaviour
    {
        MonoBehaviour nearest = null;
        float closestDistSqr = float.MaxValue;

        for (int i = 0; i < list.Count; i++)
        {
            var target = list[i];
            if (target == null || IsTargetDead(target))
                continue;

            float distSqr = (target.transform.position - position).sqrMagnitude;
            if (distSqr < closestDistSqr)
            {
                closestDistSqr = distSqr;
                nearest = target;
            }
        }

        return nearest;
    }

    // ========= CLEANUP =========
    private void CleanupDeadTargets()
    {
        armyUnits.RemoveAll(u => u == null || u.IsDead());
        cropStructures.RemoveAll(c => c == null || c.IsDead());
        siloStructures.RemoveAll(s => s == null || s.IsDead());
        defenseStructures.RemoveAll(d => d == null || d.IsDead());
        barracksStructures.RemoveAll(b => b == null || b.IsDead());
        animalStructures.RemoveAll(a => a == null || a.IsDead());
        farmhouses.RemoveAll(f => f == null || f.IsDead());

        targetCache.Clear(); // Clear cache after cleanup
    }

    // ========= LEGACY COMPATIBILITY (Optional - Remove if not used elsewhere) =========
    public List<MonoBehaviour> GetAllTargets()
    {
        var all = new List<MonoBehaviour>();
        all.AddRange(armyUnits);
        all.AddRange(cropStructures);
        all.AddRange(siloStructures);
        all.AddRange(defenseStructures);
        all.AddRange(barracksStructures);
        all.AddRange(animalStructures);
        all.AddRange(farmhouses);
        return all;
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

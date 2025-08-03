using UnityEngine;
using System.Collections.Generic;
public class EnemyUnit : BaseUnit
{
    [SerializeField] private EnemyData data;
    private int currHealth;
    private float lastAttackTime = 0f;
    private MonoBehaviour currentTarget;
    private GridDataGenerator _gridDataGenerator;

    protected override void Awake()
    {
        base.Awake();
        currHealth = data.Health;
        _gridDataGenerator = FindObjectOfType<GridDataGenerator>();
    }

    protected override UnitData GetData() => data;

    public void Attack()
    {
        //apply cooldown
        if (Time.time < lastAttackTime + data.AttackCooldown)
        {
            return;
        }

        // if (currentTarget == null || currentTarget.IsDead())
        if (currentTarget == null || IsTargetDead(currentTarget))
        {
            var aggroThings = GetAggroThingsInRange();
            if (aggroThings.Count > 0)
                currentTarget = aggroThings[0] as MonoBehaviour; // TODO: Add smarter target selection later------------------------------------------------------------------
            else
                return; // No aggro things to attack
        }

        lastAttackTime = Time.time;
        PlaySound(data.AttackSound);

        DealDamage(currentTarget);

        // currentTarget.TakeDamage(data.AttackDamage);
    }

    // public List<BaseUnit> GetAggroThingsInRange(float range)
    // {
    //     return GridController.Instance.GetAggroThingsInRange(transform.position, data.AttackRange);


    //     // List<EnemyUnit> nearbyUnits = new List<EnemyUnit>();
    //     // Collider[] colliders = Physics.OverlapSphere(transform.position, range);

    //     // foreach (Collider collider in colliders)
    //     // {
    //     //     EnemyUnit unit = collider.GetComponent<EnemyUnit>();
    //     //     if (unit != null && unit != this)
    //     //     {
    //     //         nearbyUnits.Add(unit);
    //     //     }
    //     // }

    //     // return nearbyUnits;
    // }

    // public List<object> GetAggroThingsInRange()
    // {
    //     List<object> targets = new();
    //     GridController grid = GridController.Instance;

    //     Vector2Int center = grid.WorldToGridCoords(transform.position);
    //     int radius = data.AttackRange;

    //     for (int x = -radius; x <= radius; x++)
    //     {
    //         for (int y = -radius; y <= radius; y++)
    //         {
    //             Vector2Int check = center + new Vector2Int(x, y);
    //             if (!grid.IsValidCell(check.x, check.y)) continue;

    //             // Vector3 cellCenter = GridController.Instance.GridDataGenerator.GetWorldPositionFromGridCoords(check);

    //             Vector3 cellWorldPos = GridDataGenerator.GetWorldPositionFromGridCoords(check);
    //             Collider[] hits = Physics.OverlapSphere(cellWorldPos, grid.CellSize * 0.4f);

    //             foreach (Collider col in hits)
    //             {
    //                 switch (data.AttType)
    //                 {
    //                     case AttackType.Animals:
    //                         ArmyUnit army = col.GetComponent<ArmyUnit>();
    //                         if (army != null && !army.IsDead()) targets.Add(army);
    //                         break;

    //                     case AttackType.Resources:
    //                         if (col.GetComponent<CropStructure>() is var crop && crop != null) targets.Add(crop);
    //                         if (col.GetComponent<SiloStructure>() is var silo && silo != null) targets.Add(silo);
    //                         break;

    //                     case AttackType.Defense:
    //                         if (col.GetComponent<DefenseStructure>() is var def && def != null) targets.Add(def);
    //                         break;

    //                     case AttackType.Buildings:
    //                         AddIfFound<FarmHouseStructure>(col, targets);
    //                         AddIfFound<CropStructure>(col, targets);
    //                         AddIfFound<SiloStructure>(col, targets);
    //                         AddIfFound<BarrackStructure>(col, targets);
    //                         AddIfFound<AnimalStructure>(col, targets);
    //                         break;
    //                 }
    //             }
    //         }
    //     }

    //     return targets;

    //     void AddIfFound<T>(Collider col, List<object> list) where T : MonoBehaviour
    //     {
    //         T comp = col.GetComponent<T>();
    //         if (comp != null) list.Add(comp);
    //     }
    // }

    public List<object> GetAggroThingsInRange()
    {
        List<object> targets = new();
        GridController grid = GridController.Instance;

        // Get all cells within attack range (radius in grid cells)
        List<GridCell> cellsInRange = grid.GetCellsInRange(transform.position, data.AttackRange);

        foreach (GridCell cell in cellsInRange)
        {
            Vector3 cellWorldPos = cell.worldPosition;
            Collider[] hits = Physics.OverlapSphere(cellWorldPos, grid.GetCellSize() * 0.4f);

            foreach (Collider col in hits)
            {
                switch (data.AttType)
                {
                    case AttType.Animals:
                        ArmyUnit army = col.GetComponent<ArmyUnit>();
                        if (army != null && !army.IsDead()) targets.Add(army);
                        break;

                    case AttType.Resources:
                        if (col.GetComponent<CropStructure>() is var crop && crop != null) targets.Add(crop);
                        if (col.GetComponent<SiloStructure>() is var silo && silo != null) targets.Add(silo);
                        break;

                    case AttType.Defense:
                        // if (col.GetComponent<DefenseStructure>() is var def && def != null) targets.Add(def);
                        break;

                    case AttType.Buildings:
                        AddIfFound<FarmHouseStructure>(col, targets);
                        AddIfFound<CropStructure>(col, targets);
                        AddIfFound<SiloStructure>(col, targets);
                        AddIfFound<BarracksStructure>(col, targets);
                        AddIfFound<AnimalStructure>(col, targets);
                        break;
                }
            }
        }

        return targets;

        void AddIfFound<T>(Collider col, List<object> list) where T : MonoBehaviour
        {
            T comp = col.GetComponent<T>();
            if (comp != null) list.Add(comp);
        }
    }

    //     public List<object> GetAggroThingsInRange()
    // {
    //     List<object> targets = new();
    //     GridController grid = GridController.Instance;

    //     Vector2Int center = grid.WorldToGridCoords(transform.position);
    //     int radius = data.AttackRange;

    //     for (int x = -radius; x <= radius; x++)
    //     {
    //         for (int y = -radius; y <= radius; y++)
    //         {
    //             Vector2Int check = center + new Vector2Int(x, y);
    //             if (!grid.IsValidCell(check.x, check.y)) continue;

    //             Vector3 cellWorldPos = grid.gridDataGenerator.GetWorldPositionFromGridCoords(check);
    //             Collider[] hits = Physics.OverlapSphere(cellWorldPos, grid.CellSize * 0.4f);

    //             foreach (Collider col in hits)
    //             {
    //                 switch (data.AttType)
    //                 {
    //                     case AttackType.Animals:
    //                         ArmyUnit army = col.GetComponent<ArmyUnit>();
    //                         if (army != null && !army.IsDead()) targets.Add(army);
    //                         break;

    //                     case AttackType.Resources:
    //                         if (col.GetComponent<CropStructure>() is var crop && crop != null) targets.Add(crop);
    //                         if (col.GetComponent<SiloStructure>() is var silo && silo != null) targets.Add(silo);
    //                         break;

    //                     case AttackType.Defense:
    //                         if (col.GetComponent<DefenseStructure>() is var def && def != null) targets.Add(def);
    //                         break;

    //                     case AttackType.Buildings:
    //                         AddIfFound<FarmHouseStructure>(col, targets);
    //                         AddIfFound<CropStructure>(col, targets);
    //                         AddIfFound<SiloStructure>(col, targets);
    //                         AddIfFound<BarrackStructure>(col, targets);
    //                         AddIfFound<AnimalStructure>(col, targets);
    //                         break;
    //                 }
    //             }
    //         }
    //     }

    //     return targets;

    //     void AddIfFound<T>(Collider col, List<object> list) where T : MonoBehaviour
    //     {
    //         T comp = col.GetComponent<T>();
    //         if (comp != null) list.Add(comp);
    //     }
    // }

    public void TakeDamage(int damage)
    {
        if (currHealth <= 0 || currHealth - damage <= 0)
        {
            Die();
        }
        else
        {
            currHealth -= damage;
        }
    }

    public bool IsDead()
    {
        return currHealth <= 0;
    }

    private bool IsTargetDead(MonoBehaviour target)
    {
        return target switch
        {
            ArmyUnit u => u.IsDead(),
            CropStructure u => u.IsDead(),
            SiloStructure u => u.IsDead(),
            // DefenseStructure u => u.IsDead(),
            FarmHouseStructure u => u.IsDead(),
            BarracksStructure u => u.IsDead(),
            AnimalStructure u => u.IsDead(),
            _ => true // Assume dead if type unknown
        };
    }

    private void DealDamage(MonoBehaviour target)
    {
        switch (target)
        {
            case ArmyUnit u: u.TakeDamage(data.AttackDamage); break;
            case CropStructure u: u.TakeDamage(data.AttackDamage); break;
            case SiloStructure u: u.TakeDamage(data.AttackDamage); break;
            // case DefenseStructure u: u.TakeDamage(data.AttackDamage); break;
            case FarmHouseStructure u: u.TakeDamage(data.AttackDamage); break;
            case BarracksStructure u: u.TakeDamage(data.AttackDamage); break;
            case AnimalStructure u: u.TakeDamage(data.AttackDamage); break;
        }
    }

    // public void GetEdgePoints(int count)
    // {
    //     if (_gridDataGenerator == null) return;


    //     int width = _gridDataGenerator.GetGridWidth();
    //     int height = _gridDataGenerator.GetGridHeight();

    //     for (int i = 0; i < count; i++)
    //     {
    //         int edge = Random.Range(0, 4);
    //         int x, y;

    //         switch (edge)
    //         {
    //             case 0: // Top edge
    //                 x = Random.Range(0, width);
    //                 y = height - 1;
    //                 break;
    //             case 1: // Right edge
    //                 x = width - 1;
    //                 y = Random.Range(0, height);
    //                 break;
    //             case 2: // Bottom edge
    //                 x = Random.Range(0, width);
    //                 y = 0;
    //                 break;
    //             case 3: // Left edge
    //                 x = 0;
    //                 y = Random.Range(0, height);
    //                 break;
    //             default:
    //                 x = 0;
    //                 y = 0;
    //                 break;
    //         }

    //         GridCell cell = _gridDataGenerator.GetCell(x, y);
    //         if (cell != null && !cell.flags.isObstacle && !cell.flags.isOccupied)
    //         {
    //             Vector3 position = cell.worldPosition;

    //             if (edge == 0) position.z -= _edgeInset;
    //             else if (edge == 1) position.x -= _edgeInset;
    //             else if (edge == 2) position.z += _edgeInset;
    //             else if (edge == 3) position.x += _edgeInset;

    //             positions.Add(position);
    //         }
    //         else
    //         {
    //             i--;
    //         }
    //     }

    // }

    // public void SpawnEnemy()
    // {
    //     int spawnAmount = Random.Range(data.minSpawnAmount, data.maxSpawnAmount);

    //     for (int i = 0; i < spawnAmount; i++)
    //     {
    //         Vector3 spawnPos = GetRandomOutsidePosition();

    //         // Instantiate enemy prefab at spawnPos
    //         // Assuming you have a reference to an enemy prefab, e.g.:
    //         // GameObject enemyPrefab;

    //         GameObject enemyInstance = Instantiate(data.Prefab, spawnPos, Quaternion.identity);

    //         // Optional: register enemy with CombatManager or initialize it as needed
    //         EnemyUnit enemyUnit = enemyInstance.GetComponent<EnemyUnit>();
    //         if (enemyUnit != null)
    //         {
    //             CombatManager.Instance.RegisterUnit(enemyUnit);
    //         }
    //     }
    // }

    // private Vector3 GetRandomOutsidePosition()
    // {
    //     if (_gridDataGenerator == null) return Vector3.zero;

    //     float spawnInset = 5f;

    //     int width = _gridDataGenerator.GetGridWidth();
    //     int height = _gridDataGenerator.GetGridHeight();


    //     int side = Random.Range(0, 4); // 0 = top, 1 = right, 2 = bottom, 3 = left
    //     float x = 0f;
    //     float z = 0f;

    //     switch (side)
    //     {
    //         case 0: // Top
    //             x = Random.Range(0, width);
    //             z = height + spawnInset;
    //             break;
    //         case 1: // Right
    //             x = width + spawnInset;
    //             z = Random.Range(0, height);
    //             break;
    //         case 2: // Bottom
    //             x = Random.Range(0, width);
    //             z = height - spawnInset;
    //             break;
    //         case 3: // Left
    //             x = 0 - spawnInset;
    //             z = Random.Range(0, height);
    //             break;
    //     }

    //     float y = 0f; // or terrain.SampleHeight() if you have elevation
    //     return new Vector3(x, y, z);
    // }
    
    public void increaseAfterNight()
    {
        data.maxSpawnAmount += data.nightlySpawnMultiplier;
        data.minSpawnAmount += data.nightlySpawnMultiplier;
    }
    public void increaseAfterSeason()
    {
        data.maxSpawnAmount = (int)(data.maxSpawnAmount * data.seasonSpawnMultiplier);
        data.minSpawnAmount = (int)(data.minSpawnAmount * data.seasonSpawnMultiplier);

        data.nightlySpawnMultiplier = (int)(data.nightlySpawnMultiplier * data.seasonSpawnMultiplier);
    }
}
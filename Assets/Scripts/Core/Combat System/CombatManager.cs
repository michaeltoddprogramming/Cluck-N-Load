using System.Collections.Generic;
using UnityEngine;

public class CombatManager : MonoBehaviour
{
    private List<EnemyUnit> combatUnits = new List<EnemyUnit>();
    private bool isNight = false;

    private EnemyUnit enemyUnit;
    private SpawnUnits spawnUnits;


    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        enemyUnit = FindObjectOfType<EnemyUnit>();
        spawnUnits = FindObjectOfType<SpawnUnits>();
    }

    public void RegisterUnit(EnemyUnit unit)
    {
        if (!combatUnits.Contains(unit))
        {
            combatUnits.Add(unit);
        }
    }

    public void UnregisterUnit(EnemyUnit unit)
    {
        if (combatUnits.Contains(unit))
        {
            combatUnits.Remove(unit);
        }
    }

    private void Update()
    {
        foreach (ArmyUnit armyUnit in FindObjectsOfType<ArmyUnit>())
        {
            List<EnemyUnit> nearbyEnemies = armyUnit.GetNearbyEnemies();

            if (nearbyEnemies.Count > 0 && isNight)
            {
                // This assumes you're just triggering a generic attack.
                armyUnit.Attack();

                // Or if you want to target a specific enemy:
                // armyUnit.Attack(nearbyEnemies[0]);
            }
        }


        // foreach (EnemyUnit enemyUnit in FindObjectsOfType<EnemyUnit>())
        // {
        //     var nearbyAgro = enemyUnit.GetAggroThingsInRange();

        //     if (nearbyAgro.Count > 0 && isNight)
        //     {
        //         // This assumes you're just triggering a generic attack.
        //         enemyUnit.Attack();

        //         // Or if you want to target a specific enemy:
        //         // armyUnit.Attack(nearbyEnemies[0]);
        //     }
        // }



        // foreach (EnemyUnit unit in combatUnits)
        // {
        //     if (unit is EnemyUnit)
        //     {
        //         List<EnemyUnit> nearbyUnits = armyUnit.GetNearbyUnits(armyUnit.GetData().MovementSpeed); // Example range

        //         foreach (EnemyUnit nearbyUnit in nearbyUnits)
        //         {
        //             if (nearbyUnit is EnemyUnit) // Example filtering
        //             {
        //                 // Handle combat logic here
        //                 Debug.Log($"{armyUnit.name} found nearby unit: {nearbyUnit.name}");
        //             }
        //         }


        //         // Unit target = FindNearestEnemy(armyUnit);
        //         // if (target != null)
        //         // {
        //         //     armyUnit.Attack(target);
        //         // }
        //     }
        // }
    }

    public void StartCombat()
    {
        isNight = true;
        spawnUnits.SpawnEnemies();
    }
    public void StopCombat()
    {
        isNight = false;
        DestroyAllEnemies();
    }

    public void DestroyAllEnemies()
    {
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        foreach (EnemyUnit enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }

        combatUnits.Clear(); // Optional: clear tracked list if you're using one
    }

    public void increaseAfterNight()
    {
        if (enemyUnit != null)
        {
            enemyUnit.increaseAfterNight();
        }
    }

    public void increaseAfterSeason()
    {
        if (enemyUnit != null)
        {
            enemyUnit.increaseAfterSeason();
        }
    }
    
    
}
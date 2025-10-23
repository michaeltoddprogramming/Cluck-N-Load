using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private float maxTimeSpan = 30f;
    [SerializeField] private float minTimeSpan = 20f;
    [SerializeField] private float nightlyScale = 0.9f;
    [SerializeField] private float seasonScale = 0.5f;
    [SerializeField] private List<EnemyData> allEnemyData;
    private List<EnemyUnit> combatUnits = new List<EnemyUnit>();
    private bool isNight = false;
    private bool increaseAfterSeason1 = false;
    private bool increaseAfterNight1 = false;

    private EnemyUnit enemyUnit;
    public SpawnUnits spawnUnits;
    public int season;


    public static CombatManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        spawnUnits = FindObjectOfType<SpawnUnits>();
    }

    private void Start()
    {
        enemyUnit = FindObjectOfType<EnemyUnit>();
        // spawnUnits = FindObjectOfType<SpawnUnits>();
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

        // if (isNight && increaseAfterNight1)
        // {
        //     increaseAfterNight1 = false;
        //     enemyUnit.increaseAfterNight();
        // }

        // if (isNight && increaseAfterSeason1)
        // {
        //     increaseAfterSeason1 = false;
        //     enemyUnit.increaseAfterSeason();
        // }


        foreach (ArmyUnit armyUnit in FindObjectsOfType<ArmyUnit>())
        {
            List<EnemyUnit> nearbyEnemies = armyUnit.GetNearbyEnemies();

            if (nearbyEnemies.Count > 0 && isNight)
            {

                armyUnit.Attack();

                // }
                // if(armyUnit.ArmyType == Sheep)
                // This assumes you're just triggering a generic attack.

                // Or if you want to target a specific enemy:
                // armyUnit.Attack(nearbyEnemies[0]);

            }

            // if (nearbyEnemies.Count > 0 && isNight) // Only attack at night
            // {
            //     if (armyUnit is SheepUnit sheep)
            //     {
            //         // If it's a SheepUnit, let it handle its own attack logic
            //         sheep.Attack();
            //     }
            //     else
            //     {
            //         // All other ArmyUnits use the normal attack
            //         armyUnit.Attack();
            //     }
            // }


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
    }

    public void StartCombat()
    {
        if (increaseAfterNight1)
        {
            increaseAfterNight1 = false;
            afterNight();
        }

        if (increaseAfterSeason1)
        {
            increaseAfterSeason1 = false;
            afterSeason();
        }

        // Reset combat statistics for the new night
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.ResetStats();
        }

        isNight = true;
        // spawnUnits.SpawnEnemies();
        StartCoroutine(SpawnEnemies());
    }
    public void StopCombat()
    {
        isNight = false;
        EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        foreach (EnemyUnit enemy in enemies)
        {
            enemy.stopCombat();
        }
        // DestroyAllEnemies();
    }

    // public void DestroyAllEnemies()
    // {
    //     EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
    //     foreach (EnemyUnit enemy in enemies)
    //     {
    //         Destroy(enemy.gameObject);
    //     }

    //     combatUnits.Clear(); // Optional: clear tracked list if you're using one
    // }

    public void increaseAfterNight()
    {
        // increaseAfterNight1 = true;
        spawnUnits.increaseAfterNight();
    }

    private void afterNight()
    {
        // EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        // foreach (EnemyUnit enemy in enemies)
        // {
        //     // enemy.increaseAfterSeason();
        //     enemyUnit.increaseAfterNight();
        // }
        // foreach (EnemyData data in allEnemyData)
        // {
        //     data.maxSpawnAmount += data.nightlySpawnMultiplier;
        //     data.minSpawnAmount += data.nightlySpawnMultiplier;
        //     Debug.Log($"max: {data.maxSpawnAmount} min: {data.minSpawnAmount}************************************************");
        // }
    }

    public void increaseAfterSeason()
    {
        // increaseAfterSeason1 = true;        
        spawnUnits.increaseAfterSeason();
    }

    private void afterSeason()
    {
        // EnemyUnit[] enemies = FindObjectsOfType<EnemyUnit>();
        // foreach (EnemyUnit enemy in enemies)
        // {
        //     enemy.increaseAfterSeason();
        // }
        // data.maxSpawnAmount = (int)(data.maxSpawnAmount * data.seasonSpawnMultiplier);
        // data.minSpawnAmount = (int)(data.minSpawnAmount * data.seasonSpawnMultiplier);
        // Debug.Log($"max: {data.maxSpawnAmount} min: {data.minSpawnAmount}===============================================");

        // data.nightlySpawnMultiplier = (int)(data.nightlySpawnMultiplier * data.seasonSpawnMultiplier);
        // Debug.Log($"nightly things: {data.nightlySpawnMultiplier}=====================================================");

    }

    public void scaleTimeNightly()
    {
        maxTimeSpan *= nightlyScale;
        minTimeSpan /= nightlyScale;
    }

    public void scaleTimeBySeason()
    {
        maxTimeSpan *= seasonScale;
        minTimeSpan /= seasonScale;
    }

    private IEnumerator SpawnEnemies()
    {
        while (isNight)
        {
            spawnUnits.SpawnEnemies(season);

            float delay = Random.Range(minTimeSpan, maxTimeSpan);

            // Debug.Log($"Spawning enemies in {delay} seconds...-----------------------------------------------------");

            yield return new WaitForSeconds(delay);
        }
    }

    public void SetSeason(int newSeason)
    {
        Debug.Log($"Setting season to {newSeason}-------------------------------------------------------------------------------------------------------------------------------");
        season = newSeason;
    }


}
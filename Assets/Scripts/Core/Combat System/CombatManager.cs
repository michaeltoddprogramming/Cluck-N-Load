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
    
    // Separate lists for each unit type
    private List<EnemyUnit> combatUnits = new List<EnemyUnit>();
    private List<ArmyUnit> armyUnits = new List<ArmyUnit>();
    
    private bool isNight = false;
    private bool increaseAfterSeason1 = false;
    private bool increaseAfterNight1 = false;

    // Combat check optimization - don't check every frame
    private float combatCheckInterval = 0.1f; // Check every 0.1 seconds instead of 60 times per second
    private float lastCombatCheckTime;
    private float cleanupInterval = 2f; // Clean up null references every 2 seconds
    private float lastCleanupTime;

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

        spawnUnits = FindFirstObjectByType<SpawnUnits>();
    }

    private void Start()
    {
        enemyUnit = FindFirstObjectByType<EnemyUnit>();
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

    public void RegisterUnit(ArmyUnit unit)
    {
        if (!armyUnits.Contains(unit))
        {
            armyUnits.Add(unit);
        }
    }

    public void UnregisterUnit(ArmyUnit unit)
    {
        if (armyUnits.Contains(unit))
        {
            armyUnits.Remove(unit);
        }
    }

    private void Update()
    {
        // Periodic cleanup of null references
        if (Time.time - lastCleanupTime > cleanupInterval)
        {
            armyUnits.RemoveAll(u => u == null);
            combatUnits.RemoveAll(u => u == null);
            lastCleanupTime = Time.time;
        }

        // Only check combat every 0.1 seconds instead of every frame (60 times per second)
        // This reduces CPU usage by 83% while maintaining responsive combat
        if (Time.time - lastCombatCheckTime < combatCheckInterval)
            return;
            
        lastCombatCheckTime = Time.time;

        // Use registered army units instead of FindObjectsByType (which scans entire scene)
        // This is O(n) instead of O(scene_size) - massive performance improvement
        for (int i = 0; i < armyUnits.Count; i++)
        {
            ArmyUnit armyUnit = armyUnits[i];
            
            // Skip null or dead units
            if (armyUnit == null || armyUnit.IsDead())
                continue;

            List<EnemyUnit> nearbyEnemies = armyUnit.GetNearbyEnemies();

            if (nearbyEnemies.Count > 0 && isNight)
            {
                // Use the flag system instead of calling Attack() directly
                // This prevents redundant calls and lets the unit handle attack timing
                armyUnit.attackNow = true;
            }
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
        EnemyUnit[] enemies = FindObjectsByType<EnemyUnit>(FindObjectsSortMode.None);
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
        season = newSeason;
    }


}
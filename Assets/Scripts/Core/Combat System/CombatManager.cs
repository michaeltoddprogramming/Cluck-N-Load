using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class CombatManager : MonoBehaviour
{
    [SerializeField] private float maxTimeSpan = 20f;
    [SerializeField] private float minTimeSpan = 10f;
    [SerializeField] private float nightlyScale = 0.9f;
    [SerializeField] private float seasonScale = 0.5f;
    [SerializeField] private List<EnemyData> allEnemyData;
    private List<EnemyUnit> combatUnits = new List<EnemyUnit>();
    private List<ArmyUnit> cachedArmyUnits = new List<ArmyUnit>();
    private List<EnemyUnit> cachedEnemyUnits = new List<EnemyUnit>();
    private float lastCacheUpdate;
    private Coroutine spawnCoroutine;
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
        UpdateArmyUnitCache();
    }

    private void UpdateArmyUnitCache()
    {
        cachedArmyUnits.Clear();
        cachedArmyUnits.AddRange(FindObjectsOfType<ArmyUnit>());
        
        cachedEnemyUnits.Clear();
        cachedEnemyUnits.AddRange(FindObjectsOfType<EnemyUnit>());
        
        lastCacheUpdate = Time.time;
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
        // Update cache every second to avoid expensive FindObjectsOfType calls
        if (Time.time - lastCacheUpdate > 1f)
            UpdateArmyUnitCache();

        // Use cached list instead of FindObjectsOfType every frame
        foreach (ArmyUnit armyUnit in cachedArmyUnits)
        {
            List<EnemyUnit> nearbyEnemies = armyUnit.GetNearbyEnemies();

            if (nearbyEnemies.Count > 0 && isNight)
            {
                armyUnit.Attack();
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

        isNight = true;
        // spawnUnits.SpawnEnemies();
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
        spawnCoroutine = StartCoroutine(SpawnEnemies());
    }
    public void StopCombat()
    {
        isNight = false;
        // Use cached list instead of FindObjectsOfType
        foreach (EnemyUnit enemy in cachedEnemyUnits)
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

    private void OnDestroy()
    {
        if (spawnCoroutine != null)
            StopCoroutine(spawnCoroutine);
    }


}
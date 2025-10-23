using System.Collections.Generic;
using UnityEngine;

public class CombatStatistics : MonoBehaviour
{
    private static CombatStatistics _instance;
    public static CombatStatistics Instance
    {
        get
        {
            if (_instance == null)
            {
                // Create a new GameObject with CombatStatistics component
                GameObject obj = new GameObject("CombatStatistics");
                _instance = obj.AddComponent<CombatStatistics>();
                DontDestroyOnLoad(obj);
                
                // Initialize the instance
                _instance.Initialize();
            }
            return _instance;
        }
    }

    private void Initialize()
    {
        // Subscribe to events (moved from Start)
        if (GameEventManager.Instance != null)
        {
            GameEventManager.Instance.OnStructureDestroyed.AddListener(OnStructureDestroyed);
        }
    }

    // Army losses
    private Dictionary<string, int> armyUnitsLost = new Dictionary<string, int>();
    private int totalArmyUnitsLost = 0;

    // Structure losses
    private Dictionary<string, int> structuresLost = new Dictionary<string, int>();
    private int totalStructuresLost = 0;

    // Damage stats
    private int totalDamageDealt = 0;
    private int totalDamageTaken = 0;

    // Enemy defeats
    private Dictionary<string, int> enemiesDefeated = new Dictionary<string, int>();
    private int totalEnemiesDefeated = 0;

    private void Awake()
    {
        // This will only run if the script is manually added to a scene object
        // But we use lazy instantiation instead
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    public void ResetStats()
    {
        armyUnitsLost.Clear();
        totalArmyUnitsLost = 0;
        structuresLost.Clear();
        totalStructuresLost = 0;
        totalDamageDealt = 0;
        totalDamageTaken = 0;
        enemiesDefeated.Clear();
        totalEnemiesDefeated = 0;
    }

    public void RecordArmyUnitLoss(string unitType)
    {
        if (!armyUnitsLost.ContainsKey(unitType))
            armyUnitsLost[unitType] = 0;
        armyUnitsLost[unitType]++;
        totalArmyUnitsLost++;
    }

    private void OnStructureDestroyed(Structure structure)
    {
        string structureType = structure.GetType().Name.Replace("Structure", "");
        if (!structuresLost.ContainsKey(structureType))
            structuresLost[structureType] = 0;
        structuresLost[structureType]++;
        totalStructuresLost++;
    }

    public void RecordDamageDealt(int damage)
    {
        totalDamageDealt += damage;
    }

    public void RecordDamageTaken(int damage)
    {
        totalDamageTaken += damage;
    }

    public void RecordEnemyDefeated(string enemyType)
    {
        if (!enemiesDefeated.ContainsKey(enemyType))
            enemiesDefeated[enemyType] = 0;
        enemiesDefeated[enemyType]++;
        totalEnemiesDefeated++;
    }

    public string GenerateCombatReport()
    {
        List<string> reportLines = new List<string>();

        // Army losses
        if (totalArmyUnitsLost > 0)
        {
            reportLines.Add($"Army Losses: {totalArmyUnitsLost}");
            foreach (var kvp in armyUnitsLost)
            {
                reportLines.Add($"  • {kvp.Value} {kvp.Key}");
            }
        }

        // Structure losses
        if (totalStructuresLost > 0)
        {
            reportLines.Add($"Structures Lost: {totalStructuresLost}");
            foreach (var kvp in structuresLost)
            {
                reportLines.Add($"  • {kvp.Value} {kvp.Key}");
            }
        }

        // Combat effectiveness
        if (totalDamageDealt > 0 || totalEnemiesDefeated > 0)
        {
            reportLines.Add($"Army Performance:");
            if (totalDamageDealt > 0)
                reportLines.Add($"  • {totalDamageDealt} damage dealt");
            if (totalEnemiesDefeated > 0)
            {
                reportLines.Add($"  • {totalEnemiesDefeated} enemies defeated");
                foreach (var kvp in enemiesDefeated)
                {
                    reportLines.Add($"    - {kvp.Value} {kvp.Key}");
                }
            }
        }

        // Damage taken
        if (totalDamageTaken > 0)
        {
            reportLines.Add($"Damage Taken: {totalDamageTaken}");
        }

        // Victory/defeat status
        if (totalArmyUnitsLost == 0 && totalStructuresLost == 0)
        {
            reportLines.Insert(0, "PERFECT DEFENSE!");
        }
        else if (totalEnemiesDefeated > totalArmyUnitsLost + totalStructuresLost)
        {
            reportLines.Insert(0, "VICTORIOUS!");
        }
        else
        {
            reportLines.Insert(0, "BATTLE WORN");
        }

        return string.Join("\n", reportLines);
    }

    public bool HasCombatActivity()
    {
        bool hasActivity = totalArmyUnitsLost > 0 || totalStructuresLost > 0 ||
                          totalDamageDealt > 0 || totalDamageTaken > 0 || totalEnemiesDefeated > 0;
        return hasActivity;
    }
}
using UnityEngine;
using System.Collections.Generic;

public class BarracksStructure : Structure
{
    private string targetAnimalType;
    private List<GameObject> armyAnimalPrefabs;
    private GameObject flagPrefab;
    private float recruitmentRange;
    private int maxArmyAnimals;
    private int recruitmentCostPerAnimal;
    private float protectionRadius;
    private Color flagColor = Color.white;

    private AnimalStructure targetAnimalStructure;
    private List<GameObject> armyAnimals = new List<GameObject>();
    private GameObject flag;
    private Renderer flagRenderer;
    private Vector3 guardPosition;

    // Public getters
    public string TargetAnimalType => targetAnimalType;
    public int ArmyAnimalCount => armyAnimals.Count;
    public int MaxArmyAnimals => maxArmyAnimals;
    public Vector3 GetFlagPosition => flag != null ? flag.transform.position : transform.position + new Vector3(0, 2, 0);
    public float GetProtectionRadius => protectionRadius;
    public Color GetFlagColor => flagColor;
    public AnimalStructure GetTargetStructure => targetAnimalStructure;

    // Event to notify UI of changes
    public System.Action OnArmyChanged;

    protected override void Start()
    {
        base.Start();

        // Initialize from StructureData
        if (structureData != null)
        {
            if (structureData.type != StructureType.Barracks)
            {
                Debug.LogWarning($"{gameObject.name} has BarracksStructure script but StructureData.type is {structureData.type}, expected Barracks.");
            }
            targetAnimalType = structureData.targetAnimalType;
            armyAnimalPrefabs = structureData.armyAnimalPrefabs ?? new List<GameObject>();
            flagPrefab = structureData.flagPrefab;
            recruitmentRange = structureData.recruitmentRange;
            maxArmyAnimals = structureData.maxArmyAnimals;
            recruitmentCostPerAnimal = structureData.recruitmentCostPerAnimal;
            protectionRadius = structureData.protectionRadius;
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no StructureData assigned. Using default values.");
            targetAnimalType = "Chicken";
            armyAnimalPrefabs = new List<GameObject>();
            recruitmentRange = 4000f;
            maxArmyAnimals = 5;
            recruitmentCostPerAnimal = 50;
            protectionRadius = 5f;
        }

        InitializeFlag();
        FindTargetAnimalStructure();
    }

    private void InitializeFlag()
    {
        if (flagPrefab == null)
        {
            Debug.LogWarning($"{GetStructureName()} has no flag prefab assigned!");
            return;
        }

        flag = Instantiate(flagPrefab, transform.position + new Vector3(0, 2, 0), Quaternion.identity, transform);
        flagRenderer = flag.GetComponentInChildren<Renderer>();
        if (flagRenderer != null)
        {
            flagRenderer.material.color = flagColor;
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} flag prefab has no Renderer component!");
        }
        guardPosition = flag.transform.position;
    }

    public void SetFlagColor(Color newColor)
    {
        flagColor = newColor;
        if (flagRenderer != null)
        {
            flagRenderer.material.color = newColor;
            Debug.Log($"{GetStructureName()} flag color set to {newColor}");
        }
        UpdateArmyAnimalPositions();
    }

    private void FindTargetAnimalStructure()
    {
        AnimalStructure[] animalStructures = FindObjectsOfType<AnimalStructure>();
        float minDistance = float.MaxValue;
        AnimalStructure closestStructure = null;

        Debug.Log($"[BarracksStructure] Searching for {targetAnimalType} structures. Found {animalStructures.Length} AnimalStructures.");

        foreach (AnimalStructure structure in animalStructures)
        {
            if (structure == null || !structure.gameObject.activeInHierarchy) continue;

            string animalType = structure.GetAnimalType.ToString();
            if (animalType.Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
            {
                float distance = Vector3.Distance(transform.position, structure.transform.position);
                if (distance <= recruitmentRange && distance < minDistance)
                {
                    minDistance = distance;
                    closestStructure = structure;
                }
            }
        }

        targetAnimalStructure = closestStructure;
        if (targetAnimalStructure == null)
        {
            Debug.LogWarning($"{GetStructureName()} could not find a {targetAnimalType} structure within range ({recruitmentRange} units).");
        }
        else
        {
            Debug.Log($"{GetStructureName()} found target {targetAnimalType} structure: {targetAnimalStructure.GetStructureName()} at distance {minDistance:F2}");
        }
    }

    public bool CanRecruit(int amount)
    {
        if (targetAnimalStructure == null || MoneyManager.Instance == null) return false;
        if (armyAnimals.Count + amount > maxArmyAnimals) return false;
        if (!targetAnimalStructure.CanRecruit(amount)) return false;
        int totalCost = amount * recruitmentCostPerAnimal;
        return MoneyManager.Instance.CanAfford(totalCost);
    }

  public void RecruitAnimals(int amount)
{
    if (!CanRecruit(amount))
    {
        Debug.LogWarning($"Cannot recruit {amount} animals. CanRecruit check failed.");
        return;
    }

    int totalCost = amount * recruitmentCostPerAnimal;
    
    // Try to spend the money
    bool spentMoney = MoneyManager.Instance != null && MoneyManager.Instance.SpendMoney(totalCost);
    
    if (!spentMoney)
    {
        Debug.LogWarning($"Failed to spend {totalCost} gold for recruitment.");
        return;
    }
    
    // Reduce animals from the source structure
    targetAnimalStructure.RecruitAnimals(amount);
    
    Debug.Log($"Successfully spent {totalCost} gold and recruited {amount} animals from {targetAnimalStructure.GetStructureName()}");
    
    // Time to spawn army animals
    for (int i = 0; i < amount; i++)
    {
        GameObject prefab = GetArmyAnimalPrefab();
        if (prefab == null)
        {
            Debug.LogError($"No army animal prefab found for {targetAnimalType}!");
            continue;
        }

        // Position around the barracks in a circle
        float angle = 360f * (armyAnimals.Count % 8) / 8f; // 8 positions around the circle
        float radius = 2f; // 2 units away from barracks
        Vector3 spawnOffset = new Vector3(
            Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
            0,
            Mathf.Cos(angle * Mathf.Deg2Rad) * radius
        );
        
        Vector3 spawnPosition = transform.position + spawnOffset;
        spawnPosition.y = transform.position.y; // Same height as barracks
        
        // Instantiate and add to army
        GameObject armyAnimal = Instantiate(prefab, spawnPosition, Quaternion.identity);
        armyAnimals.Add(armyAnimal);

        // Set up the army animal
        ArmyAnimal armyAnimalScript = armyAnimal.GetComponent<ArmyAnimal>();
        if (armyAnimalScript != null)
        {
            armyAnimalScript.SetBarracks(this);
            armyAnimalScript.SetGuardPosition(guardPosition, protectionRadius);
        }
        else
        {
            Debug.LogError($"Army animal prefab does not have ArmyAnimal component!");
        }
    }

    Debug.Log($"{GetStructureName()} recruited {amount} army {targetAnimalType}s. Total army: {armyAnimals.Count}");
    
    // Notify listeners that the army has changed
    if (OnArmyChanged != null)
    {
        Debug.Log("Invoking OnArmyChanged event");
        OnArmyChanged.Invoke();
    }
    else
    {
        Debug.LogWarning("OnArmyChanged event is null!");
    }
} 

    public void PlaceFlag(Vector3 position)
    {
        if (flag != null)
        {
            flag.transform.position = position;
        }
        else
        {
            flag = Instantiate(flagPrefab, position, Quaternion.identity, transform);
            flagRenderer = flag.GetComponentInChildren<Renderer>();
            if (flagRenderer != null)
            {
                flagRenderer.material.color = flagColor;
            }
        }
        guardPosition = position;
        UpdateArmyAnimalPositions();
        Debug.Log($"{GetStructureName()} placed flag at {position}. Army guards this point.");
    }

    private void UpdateArmyAnimalPositions()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                ArmyAnimal armyAnimalScript = armyAnimal.GetComponent<ArmyAnimal>();
                if (armyAnimalScript != null)
                {
                    armyAnimalScript.SetGuardPosition(guardPosition, protectionRadius);
                }
            }
        }
    }

    private GameObject GetArmyAnimalPrefab()
    {
        foreach (GameObject prefab in armyAnimalPrefabs)
        {
            ArmyAnimal armyAnimal = prefab.GetComponent<ArmyAnimal>();
            if (armyAnimal != null && armyAnimal.AnimalType.ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }
        return armyAnimalPrefabs.Count > 0 ? armyAnimalPrefabs[0] : null;
    }

    private void OnDestroy()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null) Destroy(armyAnimal);
        }
        if (flag != null) Destroy(flag);
        base.OnDestroy();
    }

    public int GetRecruitmentCost()
    {
        return recruitmentCostPerAnimal;
    }
}
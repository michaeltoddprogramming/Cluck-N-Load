using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class BarracksStructure : Structure
{
    [SerializeField] private string targetAnimalType = "Chicken";
    [SerializeField] private List<GameObject> armyAnimalPrefabs;
    [SerializeField] private GameObject flagPrefab;
    [SerializeField] private float recruitmentRange = 4000f;
    [SerializeField] private int maxArmyAnimals = 5;
    [SerializeField] private int recruitmentCostPerAnimal = 50;
    [SerializeField] private float protectionRadius = 5f;
    [SerializeField] private Color flagColor = Color.white;
    [SerializeField] private float structureCheckInterval = 5f;

    [Header("Sound effects")]
    [SerializeField] private AudioSource flagPlaceSound;
    [SerializeField] private AudioSource flagPlaceSong;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip backgroundNoise;
    [SerializeField] private AudioSource audioSourceRecruit;
    [SerializeField] private AudioClip recruitSound;

    private AnimalStructure targetAnimalStructure;
    private List<GameObject> armyAnimals = new List<GameObject>();
    private GameObject flag;
    private Renderer flagRenderer;
    private Vector3 guardPosition;
    private NightManager nightManager;
    private bool isNightTime = false;
    private float nextStructureCheckTime;
    private float lastDayNightChangeTime;

    public string TargetAnimalType => targetAnimalType;
    public int ArmyAnimalCount => armyAnimals.Count;
    public int MaxArmyAnimals => maxArmyAnimals;
    public Vector3 GetFlagPosition => flag != null ? flag.transform.position : transform.position + new Vector3(0, 2, 0);
    public float GetProtectionRadius => protectionRadius;
    public Color GetFlagColor => flagColor;
    public AnimalStructure GetTargetStructure => targetAnimalStructure;

    public System.Action OnArmyChanged;

    [Header("Barrack Synergy")]
    public float synergyMinDist = 10f;
    public float synergyMaxDist = 20f;
    public float synergyDiscount = 0.8f;

    protected override void Start()
    {
        base.Start();
        if (structureData != null)
        {
            if (structureData.type != StructureType.Barracks)
            {
                Debug.LogWarning($"{gameObject.name} has BarracksStructure script but StructureData.type is {structureData.type}, expected Barracks.");
            }
            targetAnimalType = structureData.targetAnimalType ?? "Chicken";
            armyAnimalPrefabs = structureData.armyAnimalPrefabs ?? new List<GameObject>();
            flagPrefab = structureData.flagPrefab;
            recruitmentRange = structureData.recruitmentRange;
            maxArmyAnimals = structureData.maxArmyAnimals;
            recruitmentCostPerAnimal = structureData.recruitmentCostPerAnimal;
            protectionRadius = structureData.protectionRadius;
        }
        nightManager = NightManager.Instance ?? FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError($"{GetStructureName()} could not find NightManager!");
        }
        else
        {
            nightManager.RegisterBarracksStructure(this);
        }

        InitializeFlag();
        FindTargetAnimalStructure();
        nextStructureCheckTime = Time.time + structureCheckInterval;
    }

    private void Update()
    {
        if (targetAnimalStructure == null && Time.time >= nextStructureCheckTime)
        {
            FindTargetAnimalStructure();
            nextStructureCheckTime = Time.time + structureCheckInterval;
        }

        UpdateRecruitmentCostByDistance();
    }

    public void OnDayNightChanged(bool isNight)
    {
        if (Time.time - lastDayNightChangeTime < 0.1f)
        {
            Debug.LogWarning($"{GetStructureName()} Rapid OnDayNightChanged: isNight={isNight}, LastTime={lastDayNightChangeTime:F2}, CurrentTime={Time.time:F2}");
            return;
        }
        lastDayNightChangeTime = Time.time;
        isNightTime = isNight;
        Debug.Log($"{GetStructureName()} OnDayNightChanged: isNight={isNight}, Time={Time.time:F2}, Animals={armyAnimals.Count}");
        if (isNight)
        {
            DeployAnimals();
        }
        else
        {
            ReturnAnimalsToBarracks();
        }
    }

    private void DeployAnimals()
    {
        Debug.Log($"{GetStructureName()} DeployAnimals called at Time={Time.time:F2}");
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                armyAnimal.SetActive(true);
                ArmyAnimal animalScript = armyAnimal.GetComponent<ArmyAnimal>();
                if (animalScript != null)
                {
                    animalScript.SetTimeOfDay(true);
                    animalScript.MoveToFlag();
                    Debug.Log($"{GetStructureName()} Activated {armyAnimal.name}, moving to flag at {GetFlagPosition}");
                }
                else
                {
                    Debug.LogWarning($"{GetStructureName()} ArmyAnimal {armyAnimal.name} missing ArmyAnimal script!");
                }
            }
            else
            {
                Debug.LogWarning($"{GetStructureName()} Null armyAnimal in armyAnimals!");
            }
        }
        Debug.Log($"{GetStructureName()} Deployed {armyAnimals.Count} animals to guard the flag.");
    }

    private void ReturnAnimalsToBarracks()
    {
        Debug.Log($"{GetStructureName()} ReturnAnimalsToBarracks called at Time={Time.time:F2}");
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                armyAnimal.SetActive(false);
                Debug.Log($"{GetStructureName()} Deactivated {armyAnimal.name}");
            }
        }
        Debug.Log($"{GetStructureName()} Returned {armyAnimals.Count} animals to barracks.");
    }

    private void InitializeFlag()
    {
        if (flagPrefab == null)
        {
            Debug.LogError($"{GetStructureName()} has no flag prefab assigned!");
            return;
        }
        guardPosition = transform.position + new Vector3(0, 2, 0);
        flag = Instantiate(flagPrefab, guardPosition, Quaternion.identity, transform);
        flagRenderer = flag.GetComponentInChildren<Renderer>();
        if (flagRenderer != null)
        {
            flagRenderer.material.color = flagColor;
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()} flag prefab has no Renderer component!");
        }
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

    public void FindTargetAnimalStructure()
    {
        AnimalStructure[] animalStructures = FindObjectsOfType<AnimalStructure>();
        float minGridDistance = float.MaxValue;
        AnimalStructure closestStructure = null;

        GridController gridController = FindObjectOfType<GridController>();
        if (gridController == null)
        {
            Debug.LogWarning("No GridController found for FindTargetAnimalStructure.");
            targetAnimalStructure = null;
            return;
        }

        Vector2Int barracksCell = gridController.WorldToGridCoords(transform.position);

        foreach (AnimalStructure structure in animalStructures)
        {
            if (structure == null || !structure.gameObject.activeInHierarchy) continue;
            string animalType = structure.GetAnimalType.ToString();
            if (animalType.Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
            {
                Vector2Int animalCell = gridController.WorldToGridCoords(structure.transform.position);
                float gridDistance = Vector2Int.Distance(barracksCell, animalCell);
                if (gridDistance < minGridDistance)
                {
                    minGridDistance = gridDistance;
                    closestStructure = structure;
                }
            }
        }

        targetAnimalStructure = closestStructure;
        UpdateRecruitmentCostByDistance();

        if (targetAnimalStructure == null)
        {
            Debug.LogWarning($"{GetStructureName()} could not find a {targetAnimalType} structure in the scene.");
        }
        else
        {
            Debug.Log($"{GetStructureName()} found target {targetAnimalType} structure: {targetAnimalStructure.GetStructureName()} at grid distance {minGridDistance:F2}");
            OnArmyChanged?.Invoke();
        }
    }

    public bool CanRecruit(int amount)
    {
        if (targetAnimalStructure == null)
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot recruit - No target AnimalStructure found!");
            return false;
        }
        if (MoneyManager.Instance == null)
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot recruit - MoneyManager.Instance is null!");
            return false;
        }
        if (armyAnimals.Count + amount > maxArmyAnimals)
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot recruit - Army limit reached ({armyAnimals.Count}/{maxArmyAnimals})!");
            return false;
        }
        if (!targetAnimalStructure.CanRecruit(amount))
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot recruit - Target AnimalStructure cannot provide {amount} animals!");
            return false;
        }
        int totalCost = amount * recruitmentCostPerAnimal;
        if (!MoneyManager.Instance.CanAfford(totalCost))
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot recruit - Insufficient gold ({totalCost} needed)!");
            return false;
        }
        Debug.Log($"{GetStructureName()}: Can recruit {amount} animals.");
        return true;
    }

    public void RecruitAnimals(int amount)
    {
        if (!CanRecruit(amount))
        {
            Debug.LogWarning($"Cannot recruit {amount} animals. CanRecruit check failed.");
            return;
        }
        int totalCost = amount * recruitmentCostPerAnimal;
        if (!MoneyManager.Instance.SpendMoney(totalCost))
        {
            Debug.LogWarning($"Failed to spend {totalCost} gold for recruitment.");
            return;
        }
        Debug.Log($"Deducted {totalCost} gold for {amount} animals.");
        targetAnimalStructure.RecruitAnimals(amount);
        Debug.Log($"Recruited {amount} animals from {targetAnimalStructure.GetStructureName()}.");
        for (int i = 0; i < amount; i++)
        {
            GameObject prefab = GetArmyAnimalPrefab();
            if (prefab == null)
            {
                Debug.LogError($"No army animal prefab found for {targetAnimalType}!");
                continue;
            }
            float angle = 360f * (armyAnimals.Count % 8) / 8f;
            float radius = 2f;
            Vector3 spawnOffset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * radius,
                0,
                Mathf.Cos(angle * Mathf.Deg2Rad) * radius
            );
            Vector3 spawnPosition = transform.position + spawnOffset;
            spawnPosition.y = transform.position.y;
            GameObject armyAnimal = Instantiate(prefab, spawnPosition, Quaternion.identity);
            armyAnimals.Add(armyAnimal);
            Debug.Log($"Spawned animal {armyAnimal.name} at {spawnPosition} at Time={Time.time:F2}");
            ArmyAnimal armyAnimalScript = armyAnimal.GetComponent<ArmyAnimal>();
            if (armyAnimalScript != null)
            {
                // Set animalType explicitly to match targetAnimalType
                if (!armyAnimalScript.AnimalType.ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
                {
                    Debug.LogWarning($"Animal {armyAnimal.name} type {armyAnimalScript.AnimalType} does not match barracks target {targetAnimalType}");
                }
                armyAnimalScript.SetBarracks(this);
                armyAnimalScript.SetGuardPosition(guardPosition, protectionRadius);
                armyAnimalScript.SetTimeOfDay(isNightTime);
                if (!isNightTime)
                {
                    armyAnimal.SetActive(false);
                    Debug.Log($"Animal {armyAnimal.name} deactivated (daytime) at Time={Time.time:F2}");
                }
                else
                {
                    Debug.Log($"Animal {armyAnimal.name} activated (nighttime) at Time={Time.time:F2}");
                }
            }
            else
            {
                Debug.LogError($"Army animal prefab {armyAnimal.name} does not have ArmyAnimal component!");
            }
        }
        Debug.Log($"{GetStructureName()} recruited {amount} army {targetAnimalType}s. Total army: {armyAnimals.Count}");
        OnArmyChanged?.Invoke();
        UpdateRecruitmentCostByDistance();

        playRecruitSound();
    }

    public void PlaceFlag(Vector3 position)
    {
        flagPlacementSounds();

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
        Debug.Log($"{GetStructureName()} placed flag at {position}. Animals guard this point.");
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
                    if (isNightTime)
                    {
                        armyAnimalScript.MoveToFlag();
                    }
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
        if (nightManager != null)
        {
            nightManager.UnregisterBarracksStructure(this);
        }
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

    public void ClearBarracksArmy()
    {
        Debug.Log($"{GetStructureName()} ClearBarracksArmy called at Time={Time.time:F2}");
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                Debug.Log($"{GetStructureName()} Destroying {armyAnimal.name}");
                Destroy(armyAnimal);
            }
        }
        armyAnimals.Clear();
        OnArmyChanged?.Invoke();
        Debug.Log($"{GetStructureName()} cleared barracks army.");
    }

    public void ReturnAnimal(ArmyAnimal animal) // CHANGED: Renamed from ReturnChicken
    {
        if (animal == null || !armyAnimals.Contains(animal.gameObject))
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot return animal - Invalid or not in army at Time={Time.time:F2}");
            return;
        }
        armyAnimals.Remove(animal.gameObject);
        Debug.Log($"{GetStructureName()} Removed {animal.name} from army at Time={Time.time:F2}. Army size: {armyAnimals.Count}");
        Destroy(animal.gameObject);
        if (targetAnimalStructure != null)
        {
            targetAnimalStructure.AddAnimals(1);
            Debug.Log($"{GetStructureName()}: Returned 1 animal to {targetAnimalStructure.GetStructureName()} at Time={Time.time:F2}");
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()}: Cannot return animal - No target AnimalStructure found at Time={Time.time:F2}");
        }
        OnArmyChanged?.Invoke();
    }

    public static void UpdateAllNearbyChickenCoops()
    {
        foreach (var barrack in FindObjectsOfType<BarracksStructure>())
        {
            barrack.FindTargetAnimalStructure();
        }
    }

    public void flagPlacementSounds()
    {
        StartCoroutine(PlayFlagPlacementSoundsWithDelay());
    }

    private IEnumerator PlayFlagPlacementSoundsWithDelay()
    {
        if (flagPlaceSound != null)
            flagPlaceSound.Play();

        yield return new WaitForSeconds(0.5f);

        if (flagPlaceSong != null)
            flagPlaceSong.Play();
    }

    public void playBackgroundSound()
    {
        if (audioSource != null && backgroundNoise != null)
        {
            audioSource.clip = backgroundNoise;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    public void stopBackgroundSound()
    {
        if (audioSource != null && backgroundNoise != null && audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    public void playRecruitSound()
    {
        if (audioSourceRecruit != null && recruitSound != null)
        {
            audioSourceRecruit.clip = recruitSound;
            audioSourceRecruit.Play();
        }
    }

    private void UpdateRecruitmentCostByDistance()
    {
        if (targetAnimalStructure == null)
        {
            recruitmentCostPerAnimal = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
            return;
        }

        GridController gridController = FindObjectOfType<GridController>();
        if (gridController == null)
        {
            Debug.LogWarning("No GridController found for barracks synergy check.");
            recruitmentCostPerAnimal = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
            return;
        }

        Vector2Int barracksCell = gridController.WorldToGridCoords(transform.position);
        Vector2Int animalCell = gridController.WorldToGridCoords(targetAnimalStructure.transform.position);
        int gridDist = (int)Vector2Int.Distance(barracksCell, animalCell);

        int baseCost = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
        int minCost = (int)(baseCost * synergyDiscount);

        if (gridDist <= synergyMinDist)
        {
            recruitmentCostPerAnimal = baseCost;
        }
        else if (gridDist > synergyMinDist && gridDist <= synergyMaxDist)
        {
            recruitmentCostPerAnimal = minCost;
        }
        else
        {
            recruitmentCostPerAnimal = minCost;
        }
    }
}
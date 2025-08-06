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
    [SerializeField] private float structureCheckInterval = 10f;

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
    private bool isNightTime;
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
            targetAnimalType = structureData.targetAnimalType ?? "Chicken";
            armyAnimalPrefabs = structureData.armyAnimalPrefabs ?? new List<GameObject>();
            flagPrefab = structureData.flagPrefab;
            recruitmentRange = structureData.recruitmentRange;
            maxArmyAnimals = structureData.maxArmyAnimals;
            recruitmentCostPerAnimal = structureData.recruitmentCostPerAnimal;
            protectionRadius = structureData.protectionRadius;
        }
        nightManager = NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        nightManager?.RegisterBarracksStructure(this);

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
        if (Time.time - lastDayNightChangeTime < 0.1f) return;
        lastDayNightChangeTime = Time.time;
        isNightTime = isNight;
        if (isNight) DeployAnimals();
        else ReturnAnimalsToBarracks();
    }

    private void DeployAnimals()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                armyAnimal.SetActive(true);
                // ArmyAnimal animalScript = armyAnimal.GetComponent<ArmyAnimal>();
                ArmyUnit unit = armyAnimal.GetComponent<ArmyUnit>();

                if (unit != null)
                {
                    unit.SetTimeOfDay(true);
                    unit.MoveToFlag();
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
    }

    private void ReturnAnimalsToBarracks()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                armyAnimal.SetActive(true);  

                ArmyUnit unit = armyAnimal.GetComponent<ArmyUnit>();
                
                if (unit != null)
                {
                    unit.SetTimeOfDay(false);
                    unit.BackToBarracks();
                }
            }
        }
    }

    public void AfterBackToBarracks()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                armyAnimal.SetActive(false);  
            }
        }
    }

    private void InitializeFlag()
    {
        if (flagPrefab == null) return;
        guardPosition = transform.position + new Vector3(0, 2, 0);
        flag = Instantiate(flagPrefab, guardPosition, Quaternion.identity, transform);
        flagRenderer = flag.GetComponentInChildren<Renderer>();
        if (flagRenderer != null) flagRenderer.material.color = flagColor;
    }

    public void SetFlagColor(Color newColor)
    {
        flagColor = newColor;
        if (flagRenderer != null) flagRenderer.material.color = newColor;
        UpdateArmyAnimalPositions();
    }

    public void FindTargetAnimalStructure()
    {
        AnimalStructure[] animalStructures = FindObjectsByType<AnimalStructure>(FindObjectsSortMode.None);
        float minGridDistance = float.MaxValue;
        AnimalStructure closestStructure = null;
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            targetAnimalStructure = null;
            return;
        }

        Vector2Int barracksCell = gridController.WorldToGridCoords(transform.position);
        foreach (AnimalStructure structure in animalStructures)
        {
            if (structure == null || !structure.gameObject.activeInHierarchy) continue;
            if (structure.GetAnimalType.ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
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
    }

    public bool CanRecruit(int amount) => CanRecruitSilent(amount);

    private bool CanRecruitSilent(int amount)
    {
        if (targetAnimalStructure == null || MoneyManager.Instance == null || 
            armyAnimals.Count + amount > maxArmyAnimals || !targetAnimalStructure.CanRecruit(amount))
            return false;
        return true;
    }

    private bool CanRecruitWithLogging(int amount)
    {
        if (!CanRecruitSilent(amount) || !MoneyManager.Instance.CanAfford(amount * recruitmentCostPerAnimal))
            return false;
        return true;
    }

    public void RecruitAnimals(int amount)
    {
        if (!CanRecruitWithLogging(amount) || !MoneyManager.Instance.SpendMoney(amount * recruitmentCostPerAnimal)) return;
        targetAnimalStructure.RecruitAnimals(amount);
        for (int i = 0; i < amount; i++)
        {
            GameObject prefab = GetArmyAnimalPrefab();
            if (prefab == null) continue;
            float angle = 360f * (armyAnimals.Count % 8) / 8f;
            Vector3 spawnOffset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 2f, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * 2f);
            Vector3 spawnPosition = transform.position + spawnOffset;
            spawnPosition.y = transform.position.y;
            GameObject armyAnimal = Instantiate(prefab, spawnPosition, Quaternion.identity);
            armyAnimals.Add(armyAnimal);
            // ArmyAnimal armyAnimalScript = armyAnimal.GetComponent<ArmyAnimal>();
            ArmyUnit unit = armyAnimal.GetComponent<ArmyUnit>();

            if (unit != null)
            {
                // Set animalType explicitly to match targetAnimalType
                // if (!unit.AnimalType.ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
                // {
                //     Debug.LogWarning($"Animal {armyAnimal.name} type {armyAnimalScript.AnimalType} does not match barracks target {targetAnimalType}");
                // }
                unit.SetBarracks(this);
                unit.SetGuardPosition(guardPosition, protectionRadius);
                unit.SetTimeOfDay(isNightTime);
                if (!isNightTime)
                {
                    armyAnimal.SetActive(false);
                }
                else
                {
                    armyAnimal.SetActive(true);
                }
            }
            else
            {
                Debug.LogError($"Army animal prefab {armyAnimal.name} does not have ArmyAnimal component!");
            }
        }
        OnArmyChanged?.Invoke();
        UpdateRecruitmentCostByDistance();
        playRecruitSound();
        if (armyAnimals.Count >= 1) TutorialManager.Instance?.Trigger(TutorialTrigger.RecruitedFirstSoldiers);
    }

    public void PlaceFlag(Vector3 position)
    {
        flagPlacementSounds();
        if (flag != null) flag.transform.position = position;
        else
        {
            flag = Instantiate(flagPrefab, position, Quaternion.identity, transform);
            flagRenderer = flag.GetComponentInChildren<Renderer>();
            if (flagRenderer != null) flagRenderer.material.color = flagColor;
        }
        guardPosition = position;
        UpdateArmyAnimalPositions();
        if (armyAnimals.Count >= 1) TutorialManager.Instance?.Trigger(TutorialTrigger.PlacedFirstFlag);
    }

    private void UpdateArmyAnimalPositions()
    {
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                
                // ArmyAnimal armyAnimalScript = armyAnimal.GetComponent<ArmyAnimal>();
                ArmyUnit unit = armyAnimal.GetComponent<ArmyUnit>();

                if (unit != null)
                {
                    unit.SetGuardPosition(guardPosition, protectionRadius);
                    if (isNightTime)
                    {
                        unit.MoveToFlag();
                    }
                }
            }
        }
    }

private GameObject GetArmyAnimalPrefab()
    {
        foreach (GameObject prefab in armyAnimalPrefabs)
        {
            ArmyUnit armyAnimal = prefab.GetComponent<ArmyUnit>();
            if (armyAnimal != null && armyAnimal.GetType().ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
            {
                return prefab;
            }
        }
        return armyAnimalPrefabs.Count > 0 ? armyAnimalPrefabs[0] : null;
    }

    protected override void OnDestroy()
    {
        nightManager?.UnregisterBarracksStructure(this);
        foreach (GameObject armyAnimal in armyAnimals) if (armyAnimal != null) Destroy(armyAnimal);
        if (flag != null) Destroy(flag);
        base.OnDestroy();
    }

    public int GetRecruitmentCost() => recruitmentCostPerAnimal;

    public void ClearBarracksArmy()
    {
        foreach (GameObject armyAnimal in armyAnimals) if (armyAnimal != null) Destroy(armyAnimal);
        armyAnimals.Clear();
        OnArmyChanged?.Invoke();
    }

    // public void ReturnAnimal(ArmyAnimal animal) // CHANGED: Renamed from ReturnChicken
    // {
    //     if (animal == null || !armyAnimals.Contains(animal.gameObject))
    //     {
    //         Debug.LogWarning($"{GetStructureName()}: Cannot return animal - Invalid or not in army at Time={Time.time:F2}");
    //         return;
    //     }
    //     armyAnimals.Remove(animal.gameObject);
    //     Destroy(animal.gameObject);
    //     if (targetAnimalStructure != null)
    //     {
    //         targetAnimalStructure.AddAnimals(1);
    //     }
    //     else
    //     {
    //         Debug.LogWarning($"{GetStructureName()}: Cannot return animal - No target AnimalStructure found at Time={Time.time:F2}");
    //     }
    //     OnArmyChanged?.Invoke();
    // }

    public static void UpdateAllNearbyChickenCoops()
    {
        foreach (var barrack in FindObjectsByType<BarracksStructure>(FindObjectsSortMode.None))
            barrack.FindTargetAnimalStructure();
    }

    public void flagPlacementSounds() => StartCoroutine(PlayFlagPlacementSoundsWithDelay());

    private IEnumerator PlayFlagPlacementSoundsWithDelay()
    {
        if (flagPlaceSound != null) flagPlaceSound.Play();
        yield return new WaitForSeconds(0.5f);
        if (flagPlaceSong != null) flagPlaceSong.Play();
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
        if (audioSource != null && backgroundNoise != null && audioSource.isPlaying) audioSource.Stop();
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
        int baseCost = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
        if (targetAnimalStructure == null)
        {
            recruitmentCostPerAnimal = baseCost;
            return;
        }
        GridController gridController = FindFirstObjectByType<GridController>();
        if (gridController == null)
        {
            recruitmentCostPerAnimal = baseCost;
            return;
        }
        Vector2Int barracksCell = gridController.WorldToGridCoords(transform.position);
        Vector2Int animalCell = gridController.WorldToGridCoords(targetAnimalStructure.transform.position);
        int gridDist = (int)Vector2Int.Distance(barracksCell, animalCell);
        int minCost = (int)(baseCost * synergyDiscount);
        recruitmentCostPerAnimal = gridDist <= synergyMinDist ? baseCost : minCost;
    }

    public int GetMaxAnimalCount() => maxArmyAnimals;
    public int GetAnimalCount() => ArmyAnimalCount;
    public int GetAnimalRecruitPrice() => recruitmentCostPerAnimal;
    public string GetAnimalType() => targetAnimalType;
}
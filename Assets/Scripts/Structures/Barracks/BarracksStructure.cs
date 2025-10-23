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
    [SerializeField] private float structureCheckInterval = 2f;

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
    
    // OPTIMIZATION: Object pooling - keep units in memory but out of active hierarchy
    private static GameObject inactiveUnitsPool;
    private const string POOL_NAME = "[Inactive Army Units Pool]";
    
    // OPTIMIZATION: Cache GridController and throttle cost updates
    private GridController cachedGridController;
    private float lastCostUpdateTime = 0f;
    private const float COST_UPDATE_INTERVAL = 1.0f; // Update once per second (was 60/sec!)

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

    private List<GameObject> sheepUnits = new List<GameObject>();
    private List<GameObject> sheepFlags = new List<GameObject>();
    // private List<GameObject> sheep = new List<GameObject>();

    protected override void Start()
    {
        base.Start();
        
        // OPTIMIZATION: Create inactive pool if it doesn't exist
        EnsureInactivePoolExists();
        
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

        synergyMaxDist = structureData.synergyMaxDist;
        synergyMinDist = structureData.synergyMinDist;

        InitializeFlag();
        // Delay initial search to allow other structures to initialize
        StartCoroutine(DelayedInitialSearch(1f));  // Wait 1 second before first search
        nextStructureCheckTime = Time.time + structureCheckInterval;
    }

    private IEnumerator DelayedInitialSearch(float delay)
    {
        yield return new WaitForSeconds(delay);
        FindTargetAnimalStructure();
    }

    private void Update()
    {
        if (targetAnimalStructure == null && Time.time >= nextStructureCheckTime)
        {
            FindTargetAnimalStructure();
            nextStructureCheckTime = Time.time + structureCheckInterval;
        }
        
        // OPTIMIZATION: Throttle cost updates to once per second instead of 60/sec
        // Recruitment cost rarely changes (only when structures move)
        if (Time.time - lastCostUpdateTime >= COST_UPDATE_INTERVAL)
        {
            UpdateRecruitmentCostByDistance();
            lastCostUpdateTime = Time.time;
        }
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
                // OPTIMIZATION: Move unit back from pool to active scene
                armyAnimal.transform.SetParent(null); // Remove from pool, back to root
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
        // OPTIMIZATION: Move units to inactive pool instead of just SetActive(false)
        // This removes them from the active scene hierarchy, dramatically reducing overhead
        EnsureInactivePoolExists();
        
        foreach (GameObject armyAnimal in armyAnimals)
        {
            if (armyAnimal != null)
            {
                // Move to inactive pool (out of scene hierarchy)
                armyAnimal.transform.SetParent(inactiveUnitsPool.transform);
                armyAnimal.SetActive(false);
            }
        }
    }
    
    // OPTIMIZATION: Ensure the inactive pool exists
    private static void EnsureInactivePoolExists()
    {
        if (inactiveUnitsPool == null)
        {
            inactiveUnitsPool = GameObject.Find(POOL_NAME);
            if (inactiveUnitsPool == null)
            {
                inactiveUnitsPool = new GameObject(POOL_NAME);
                inactiveUnitsPool.SetActive(false); // Inactive parent = all children ignored by Unity!
                DontDestroyOnLoad(inactiveUnitsPool); // Persist across scenes
            }
        }
    }

    private void InitializeFlag()
    {
        if (flagPrefab == null) return;

        // Position flag directly on top of this specific structure
        Vector3 flagPosition = GetTopOfStructure();
        guardPosition = flagPosition;

        flag = Instantiate(flagPrefab, guardPosition, Quaternion.identity, transform);
        flagRenderer = flag.GetComponentInChildren<Renderer>();
        if (flagRenderer != null) flagRenderer.material.color = flagColor;
    }

    private float GetStructureHeight()
    {
        float height = 1f; // Default height
        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            height = renderer.bounds.size.y;
        }
        return height;
    }

    private Vector3 GetTopOfStructure()
    {
        // First try to use collider bounds which are often more accurate
        Collider structureCollider = GetComponent<Collider>();
        if (structureCollider != null)
        {
            // Use collider top surface - colliders are usually more precise for positioning
            return new Vector3(transform.position.x, structureCollider.bounds.max.y, transform.position.z);
        }

        // Fallback to renderer bounds
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            float highestY = float.MinValue;
            foreach (Renderer renderer in renderers)
            {
                if (renderer.bounds.max.y > highestY)
                {
                    highestY = renderer.bounds.max.y;
                }
            }
            return new Vector3(transform.position.x, highestY, transform.position.z);
        }

        // Final fallback to fixed height
        return transform.position + new Vector3(0, 2.5f, 0);
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
            Debug.LogWarning($"{GetStructureName()}: GridController not found, cannot search for animal structures.");
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
        if (targetAnimalStructure != null)
        {
        }
        else
        {
            Debug.LogWarning($"{GetStructureName()}: No suitable {targetAnimalType} found nearby.");
        }
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
        if (nightManager != null && nightManager.getIsPaused()) return;

        // Tutorial restriction: prevent recruiting more than 3 army animals during recruit_soldiers step
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive())
        {
            if (!TutorialManager.Instance.GetCompletedStepIds().Contains("recruit_soldiers"))
            {
                // Only allow recruiting if we won't exceed 3 army animals
                if (armyAnimals.Count + amount > 3)
                {
                    Debug.Log("Tutorial: Cannot recruit more than 3 army animals total. You need exactly 3!");
                    return;
                }
            }
        }

        UpdateRecruitmentCostByDistance();

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
                if (targetAnimalType == "Sheep")
                {
                    // Additional day check for sheep recruitment
                    NightManager nightManager = NightManager.Instance;
                    if (nightManager != null && !nightManager.IsDay)
                    {
                        Debug.LogWarning($"{GetStructureName()}: Cannot recruit sheep at night - flags cannot be placed.");
                        Destroy(armyAnimal);  // Clean up the spawned unit
                        continue;  // Skip this recruitment
                    }
                    sheepUnits.Add(armyAnimal);

                    // Set radius visibility based on current selection state
                    SheepUnit sheepUnit = armyAnimal.GetComponent<SheepUnit>();
                    if (sheepUnit != null)
                    {
                        sheepUnit.SetRadiusIndicatorVisibility(IsSelected());
                    }

                    // GameObject sheepFlag = Instantiate(flagPrefab, armyAnimal.transform.position + new Vector3(0, 2, 0), Quaternion.identity, transform);
                    // Renderer sheepFlagRenderer = sheepFlag.GetComponentInChildren<Renderer>();
                    // if (sheepFlagRenderer != null) sheepFlagRenderer.material.color = flagColor;

                    // sheepFlags.Add(sheepFlag);
                    unit.SetBarracks(this);
                    unit.SetGuardPosition(guardPosition, protectionRadius);
                    unit.SetTimeOfDay(isNightTime);

                    // sheepFlags.Add(armyAnimal.transform.position + new Vector3(0, 2, 0));
                    // sheepFlags[armyAnimals.Count - 1].add(transform.position + spawnOffset);
                    // unit.SetBarracks(this);
                    // unit.SetGuardPosition(sheepFlags[armyAnimal.Count - 1], protectionRadius);
                    // unit.SetTimeOfDay(isNightTime);
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
                // Set animalType explicitly to match targetAnimalType
                // if (!unit.AnimalType.ToString().Equals(targetAnimalType, System.StringComparison.OrdinalIgnoreCase))
                // {
                //     Debug.LogWarning($"Animal {armyAnimal.name} type {armyAnimalScript.AnimalType} does not match barracks target {targetAnimalType}");
                // }
            }
            else
            {
                Debug.LogError($"Army animal prefab {armyAnimal.name} does not have ArmyAnimal component!");
            }
        }
        // civilianSpawner?.RemoveAnimalsByChunk(amount);

        OnArmyChanged?.Invoke();
        // UpdateRecruitmentCostByDistance();
        playRecruitSound();
        if (armyAnimals.Count >= 1) TutorialManager.Instance?.Trigger(TutorialTrigger.RecruitedFirstSoldiers);
        if (armyAnimals.Count == 3) TutorialManager.Instance?.Trigger(TutorialTrigger.Recruited3ArmyAnimals);
    }

    public void PlaceFlag(Vector3 position)
    {
        flagPlacementSounds();

        if (targetAnimalType == "Sheep")
        {
            // Only add a new flag if we haven't placed enough flags yet
            if (sheepFlags.Count < sheepUnits.Count)
            {
                // Check if flag should be placed on top of the barracks structure
                Vector3 flagPosition = position;
                float distanceToBarracks = Vector3.Distance(new Vector3(position.x, 0, position.z),
                                                           new Vector3(transform.position.x, 0, transform.position.z));

                // If the flag is being placed very close to the barracks (within 2 units), put it on top of the structure
                if (distanceToBarracks < 2f)
                {
                    Vector3 topOfStructure = GetTopOfStructure();
                    flagPosition = new Vector3(transform.position.x, topOfStructure.y, transform.position.z);
                }

                GameObject sheepFlag = Instantiate(flagPrefab, flagPosition, Quaternion.identity, transform);
                Renderer sheepFlagRenderer = sheepFlag.GetComponentInChildren<Renderer>();
                if (sheepFlagRenderer != null) sheepFlagRenderer.material.color = flagColor;

                sheepFlags.Add(sheepFlag);
                ArmyUnit unit = sheepUnits[sheepFlags.Count - 1].GetComponent<ArmyUnit>();
                if (unit != null)
                {
                    unit.SetGuardPosition(flagPosition, protectionRadius);
                    if (isNightTime) unit.MoveToFlag();
                }
            }

            // Update each sheep to its corresponding flag
            // for (int i = 0; i < sheepUnits.Count; i++)
            // {
            //     ArmyUnit unit = sheepUnits[i].GetComponent<ArmyUnit>();
            //     if (unit != null && i < sheepFlags.Count)
            //     {
            //         unit.SetGuardPosition(sheepFlags[i].transform.position, protectionRadius);
            //         if (isNightTime)
            //         {
            //             unit.MoveToFlag();
            //         }
            //     }
            // }

        }
        else
        {
            // Check if flag is being placed on the barracks structure itself
            Vector3 flagPosition = position;
            float distanceToBarracks = Vector3.Distance(new Vector3(position.x, 0, position.z),
                                                       new Vector3(transform.position.x, 0, transform.position.z));

            // If the flag is being placed very close to the barracks (within 2 units), put it on top of the structure
            if (distanceToBarracks < 2f)
            {
                Vector3 topOfStructure = GetTopOfStructure();
                flagPosition = new Vector3(transform.position.x, topOfStructure.y, transform.position.z);
            }

            if (flag != null) flag.transform.position = flagPosition;
            else
            {
                flag = Instantiate(flagPrefab, flagPosition, Quaternion.identity, transform);
                flagRenderer = flag.GetComponentInChildren<Renderer>();
                if (flagRenderer != null) flagRenderer.material.color = flagColor;
            }
            guardPosition = flagPosition;
            UpdateArmyAnimalPositions();
        }
        if (armyAnimals.Count >= 1) TutorialManager.Instance?.Trigger(TutorialTrigger.PlacedFirstFlag);
    }

    private void UpdateArmyAnimalPositions()
    {
        if (targetAnimalType == "Sheep")
        {
            for (int k = 0; k < sheepUnits.Count; k++)
            {
                ArmyUnit unit = sheepUnits[k].GetComponent<ArmyUnit>();
                if (unit != null && k < sheepFlags.Count)
                {
                    unit.SetGuardPosition(sheepFlags[k].transform.position, protectionRadius);
                    if (isNightTime)
                    {
                        unit.MoveToFlag();
                    }
                }
            }
        }
        else
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

    public void OnAnimalDied(ArmyUnit unit)
    {
        if (unit == null) return;

        if (armyAnimals.Contains(unit.gameObject))
        {
            armyAnimals.Remove(unit.gameObject);

            if (targetAnimalType == "Sheep" && sheepUnits.Contains(unit.gameObject))
            {
                int index = sheepUnits.IndexOf(unit.gameObject);
                sheepUnits.RemoveAt(index);

                // Also remove the sheep flag if it exists for this unit
                if (index < sheepFlags.Count)
                {
                    GameObject flagToRemove = sheepFlags[index];
                    sheepFlags.RemoveAt(index);
                    if (flagToRemove != null) Destroy(flagToRemove);
                }
            }

            OnArmyChanged?.Invoke();
        }
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

    private void CreateArmyAnimal()
    {
        GameObject prefab = GetArmyAnimalPrefab();
        if (prefab == null) return;
        float angle = 360f * (armyAnimals.Count % 8) / 8f;
        Vector3 spawnOffset = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * 2f, 0, Mathf.Cos(angle * Mathf.Deg2Rad) * 2f);
        Vector3 spawnPosition = transform.position + spawnOffset;
        spawnPosition.y = transform.position.y;
        GameObject armyAnimal = Instantiate(prefab, spawnPosition, Quaternion.identity);
        armyAnimals.Add(armyAnimal);

        ArmyUnit unit = armyAnimal.GetComponent<ArmyUnit>();
        if (unit != null)
        {
            unit.SetBarracks(this);
            unit.SetGuardPosition(guardPosition, protectionRadius);
            unit.SetTimeOfDay(isNightTime);
            armyAnimal.SetActive(isNightTime);
        }
    }

    // private void UpdateRecruitmentCostByDistance()
    // {
    //     int baseCost = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
    //     if (targetAnimalStructure == null)
    //     {
    //         recruitmentCostPerAnimal = baseCost;
    //         return;
    //     }
    //     GridController gridController = FindFirstObjectByType<GridController>();
    //     if (gridController == null)
    //     {
    //         recruitmentCostPerAnimal = baseCost;
    //         return;
    //     }
    //     Vector2Int barracksCell = gridController.WorldToGridCoords(transform.position);
    //     Vector2Int animalCell = gridController.WorldToGridCoords(targetAnimalStructure.transform.position);
    //     int gridDist = (int)Vector2Int.Distance(barracksCell, animalCell);
    //     int minCost = (int)(baseCost * synergyDiscount);
    //     recruitmentCostPerAnimal = gridDist <= synergyMinDist ? baseCost : minCost;
    // }

    private void UpdateRecruitmentCostByDistance()
{
    int baseCost = structureData != null ? structureData.recruitmentCostPerAnimal : 50;
    if (targetAnimalStructure == null)
    {
        recruitmentCostPerAnimal = baseCost;
        return;
    }

    // OPTIMIZATION: Cache GridController to avoid expensive FindFirstObjectByType every call
    // GridController is a singleton that never changes, so find once and reuse
    if (cachedGridController == null)
    {
        cachedGridController = FindFirstObjectByType<GridController>();
    }
    
    if (cachedGridController == null)
    {
        recruitmentCostPerAnimal = baseCost;
        return;
    }

    Vector2Int barracksCell = cachedGridController.WorldToGridCoords(transform.position);
    Vector2Int animalCell = cachedGridController.WorldToGridCoords(targetAnimalStructure.transform.position);
    int gridDist = (int)Vector2Int.Distance(barracksCell, animalCell);

    // Apply discount only if distance is within min and max
    if (gridDist >= synergyMinDist && gridDist <= synergyMaxDist)
    {
        recruitmentCostPerAnimal = (int)(baseCost * synergyDiscount);
    }
    else
    {
        recruitmentCostPerAnimal = baseCost;
    }
}


    public int GetMaxAnimalCount() => maxArmyAnimals;
    public int GetAnimalCount() => ArmyAnimalCount;
    public int GetAnimalRecruitPrice() => recruitmentCostPerAnimal;
    public string GetAnimalType() => targetAnimalType;

    public void SpawnArmyAnimals(int count)
    {
        for (int i = 0; i < count; i++)
        {
            CreateArmyAnimal();
        }
    }

    // Add this method to BarracksStructure class
    public void CheatAddAnimals(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            RecruitAnimals(1);
        }
    }

    public int GetAvailableCivilians()
    {
        return targetAnimalStructure != null ? targetAnimalStructure.animalCount : 0;
    }

    public int GetMaxCivilians()
    {
        return targetAnimalStructure != null ? targetAnimalStructure.MaxAnimalCount : 0;
    }
    public override void Select()
    {
        base.Select();

        // Show radius indicators for sheep units when barracks is selected
        if (targetAnimalType == "Sheep")
        {
            SetSheepRadiusVisibility(true);
        }
    }

    public override void Deselect()
    {
        base.Deselect();

        // Hide radius indicators for sheep units when barracks is deselected
        if (targetAnimalType == "Sheep")
        {
            SetSheepRadiusVisibility(false);
        }
    }

    private void SetSheepRadiusVisibility(bool visible)
    {
        foreach (GameObject sheepUnit in sheepUnits)
        {
            if (sheepUnit != null)
            {
                SheepUnit sheep = sheepUnit.GetComponent<SheepUnit>();
                if (sheep != null)
                {
                    sheep.SetRadiusIndicatorVisibility(visible);
                }
            }
        }
    }

    // ===== SHEEP FLAG INDIVIDUAL MANAGEMENT =====
    /// <summary>
    /// Gets information about all sheep flags for UI display
    /// </summary>
    public List<SheepFlagInfo> GetSheepFlagInfo()
    {
        var flagInfoList = new List<SheepFlagInfo>();
        
        for (int i = 0; i < sheepFlags.Count && i < sheepUnits.Count; i++)
        {
            if (sheepFlags[i] != null && sheepUnits[i] != null)
            {
                flagInfoList.Add(new SheepFlagInfo
                {
                    flagIndex = i,
                    flagObject = sheepFlags[i],
                    sheepUnit = sheepUnits[i],
                    flagPosition = sheepFlags[i].transform.position,
                    sheepName = $"Sheep {i + 1}"
                });
            }
        }
        
        return flagInfoList;
    }

    /// <summary>
    /// Moves a specific sheep flag to a new position
    /// </summary>
    public bool MoveSheepFlag(int flagIndex, Vector3 newPosition)
    {
        Debug.Log($"[SHEEP FLAG] MoveSheepFlag called - flagIndex: {flagIndex}, newPosition: {newPosition}");
        Debug.Log($"[SHEEP FLAG] sheepFlags.Count: {sheepFlags.Count}, sheepUnits.Count: {sheepUnits.Count}");
        
        if (flagIndex < 0 || flagIndex >= sheepFlags.Count || sheepFlags[flagIndex] == null)
        {
            Debug.LogWarning($"[SHEEP FLAG] Invalid sheep flag index: {flagIndex}, sheepFlags.Count: {sheepFlags.Count}");
            return false;
        }

        // Check if it's daytime for sheep (same restrictions as placement)
        if (targetAnimalType == "Sheep")
        {
            NightManager nightManager = NightManager.Instance;
            if (nightManager != null && !nightManager.IsDay)
            {
                Debug.LogWarning("[SHEEP FLAG] Cannot move sheep flags at night");
                return false;
            }
        }
        
        Debug.Log($"[SHEEP FLAG] All checks passed, moving flag {flagIndex} to {newPosition}");

        // Move the flag
        Vector3 flagPosition = newPosition;
        float distanceToBarracks = Vector3.Distance(new Vector3(newPosition.x, 0, newPosition.z),
                                                   new Vector3(transform.position.x, 0, transform.position.z));

        // If moving very close to barracks, place on top
        if (distanceToBarracks < 2f)
        {
            Vector3 topOfStructure = GetTopOfStructure();
            flagPosition = new Vector3(transform.position.x, topOfStructure.y, transform.position.z);
            Debug.Log($"[SHEEP FLAG] Flag {flagIndex} placed on top of structure at {flagPosition}");
        }
        else
        {
            Debug.Log($"[SHEEP FLAG] Flag {flagIndex} placed at ground position {flagPosition}");
        }

        Vector3 oldPosition = sheepFlags[flagIndex].transform.position;
        sheepFlags[flagIndex].transform.position = flagPosition;
        Debug.Log($"[SHEEP FLAG] Flag {flagIndex} moved from {oldPosition} to {flagPosition}");

        // Update the corresponding sheep's guard position
        if (flagIndex < sheepUnits.Count && sheepUnits[flagIndex] != null)
        {
            Debug.Log($"[SHEEP FLAG] Updating guard position for sheep unit {flagIndex}");
            ArmyUnit unit = sheepUnits[flagIndex].GetComponent<ArmyUnit>();
            if (unit != null)
            {
                unit.SetGuardPosition(flagPosition, protectionRadius);
                Debug.Log($"[SHEEP FLAG] Guard position set for unit {flagIndex} at {flagPosition}");
                if (isNightTime)
                {
                    unit.MoveToFlag();
                    Debug.Log($"[SHEEP FLAG] Moving unit {flagIndex} to flag (night time)");
                }
            }
            else
            {
                Debug.LogWarning($"[SHEEP FLAG] No ArmyUnit component found for sheep {flagIndex}");
            }
        }
        else
        {
            Debug.LogWarning($"[SHEEP FLAG] No corresponding sheep unit for flag {flagIndex}");
        }

        Debug.Log($"[SHEEP FLAG] MoveSheepFlag completed successfully for flag {flagIndex}");
        return true;
    }

    /// <summary>
    /// Removes a specific sheep flag but keeps the sheep (sheep returns to main flag)
    /// </summary>
    public bool RemoveSheepFlag(int flagIndex)
    {
        if (flagIndex < 0 || flagIndex >= sheepFlags.Count)
        {
            return false;
        }

        // Remove the flag
        if (sheepFlags[flagIndex] != null)
        {
            Destroy(sheepFlags[flagIndex]);
        }
        sheepFlags.RemoveAt(flagIndex);

        // Move the sheep back to the main barracks flag position instead of deleting it
        if (flagIndex < sheepUnits.Count && sheepUnits[flagIndex] != null)
        {
            ArmyUnit unit = sheepUnits[flagIndex].GetComponent<ArmyUnit>();
            if (unit != null)
            {
                // Set sheep to use the main barracks flag position
                unit.SetGuardPosition(guardPosition, protectionRadius);
                if (isNightTime)
                {
                    unit.MoveToFlag();
                }
            }
        }

        OnArmyChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Gets the number of placed sheep flags
    /// </summary>
    public int GetSheepFlagCount()
    {
        return sheepFlags.Count;
    }

    /// <summary>
    /// Gets the maximum number of sheep flags that can be placed
    /// </summary>
    public int GetMaxSheepFlags()
    {
        return sheepUnits.Count;
    }

    /// <summary>
    /// Checks if more sheep flags can be placed
    /// </summary>
    public bool CanPlaceMoreSheepFlags()
    {
        return targetAnimalType == "Sheep" && sheepFlags.Count < sheepUnits.Count;
    }
}

/// <summary>
/// Data structure for sheep flag information
/// </summary>
[System.Serializable]
public struct SheepFlagInfo
{
    public int flagIndex;
    public GameObject flagObject;
    public GameObject sheepUnit;
    public Vector3 flagPosition;
    public string sheepName;
}
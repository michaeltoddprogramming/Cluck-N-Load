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
                Debug.Log($"{GetStructureName()}: Checking {structure.GetAnimalType} at distance {gridDistance}");
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
            Debug.Log($"{GetStructureName()}: Found target {targetAnimalStructure.GetAnimalType} at distance {minGridDistance}");
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
        OnArmyChanged?.Invoke();
        // UpdateRecruitmentCostByDistance();
        playRecruitSound();
        if (armyAnimals.Count >= 1) TutorialManager.Instance?.Trigger(TutorialTrigger.RecruitedFirstSoldiers);
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


}
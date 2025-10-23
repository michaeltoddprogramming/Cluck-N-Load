using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Events;


public class Structure : MonoBehaviour
{
    // Registration flag to prevent double registration
    private bool isRegisteredWithGameLoop = false;

    public StructureData StructureData => structureData; // Public property for external access
    // public StructureData StructureData;
    // public StructureDatabase StructureDatabase => structureDatabase;

    [Header("Structure Properties")]
    [Tooltip("Reference to structure data asset")]
    public StructureData structureData;
    [Tooltip("Current health of the structure")]
    [SerializeField] private int currentHealth;
    [Tooltip("Whether this structure can be damaged")]
    public bool isIndestructible = false;

    private bool allowSelectionAndUI = true;

    [Header("Grid Integration")]
    [Tooltip("Whether this structure blocks pathfinding")]
    public bool blocksPathfinding = true;
    [Tooltip("Whether grid cells should be marked as occupied")]
    public bool occupiesGridCells = true;
    [Tooltip("Whether to automatically register with grid on start")]
    public bool autoRegisterWithGrid = true;

    [Header("Destruction Settings")]
    [Tooltip("Whether to destroy GameObject when health reaches zero")]
    public bool destroyOnZeroHealth = true;
    [Tooltip("Delay before destroying GameObject")]
    public float destroyDelay = 0.5f;
    [Tooltip("Optional effect to spawn when destroyed")]
    public GameObject destructionEffectPrefab;

    [Header("Debug")]
    [Tooltip("Show debug information")]
    public bool showDebugInfo = false;

    [Header("UI")]
    [SerializeField] private GameObject healthBarPrefab;
    private GameObject healthBarInstance;
    private Slider healthBarSlider;
    private TextMeshProUGUI healthBarText;
    private CanvasGroup healthBarCanvasGroup;
    private Color healthyColor = new Color(0.2f, 1f, 0.2f);
    private Color midColor = new Color(1f, 0.9f, 0.2f);
    private Color dangerColor = new Color(1f, 0.2f, 0.2f);

    // Private references
    private GridController gridController;
    private List<Vector2Int> occupiedCells = new List<Vector2Int>();
    private bool hasRegisteredWithGrid = false;

    // Selection properties
    private GameObject selectionIndicator;
    private bool isSelected = false;



    // Events
    public delegate void StructureEvent(Structure structure);
    public event StructureEvent OnDamaged;
    public event StructureEvent OnDestroyed;
    public static UnityEvent OnAnyStructureDamaged = new UnityEvent();
    private bool hasTriggeredLowHealthEvent = false;
    public delegate void StructureDestroyedEventHandler(Structure destroyedStructure);
    public event StructureDestroyedEventHandler OnStructureDestroyed;

    // Health changed event for UI updates
    public event System.Action OnHealthChanged;

    // Wolf notifications
    // private static readonly List<Wolf> registeredWolves = new List<Wolf>();

    // public static void RegisterWolf(Wolf wolf)
    // {
    //     if (wolf != null && !registeredWolves.Contains(wolf))
    //         registeredWolves.Add(wolf);
    // }

    // public static void UnregisterWolf(Wolf wolf)
    // {
    //     registeredWolves.Remove(wolf);
    // }

    #region Unity Lifecycle

    public StructureData GetData()
    {
        return structureData;
    }

    private void Awake()
    {
        TargetManager.Instance.RegisterTarget(this);
        if (structureData != null)
        {
            currentHealth = structureData.health;
        }
        else
        {
            currentHealth = 100;
            Debug.LogWarning($"No StructureData assigned to {gameObject.name}", this);
        }

        selectionIndicator = transform.Find("SelectionIndicator")?.gameObject;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }

        OnHealthChanged += UpdateHealthBar;
    }

    private bool lastIsDayState = true;
    private bool hasInitialized = false;
    
    private void Update()
    {
        // Check for day/night transitions to update health bar visibility
        if (healthBarCanvasGroup != null && NightManager.Instance != null)
        {
            bool currentIsDayState = NightManager.Instance.IsDay;
            
            // Initialize the day state on first frame
            if (!hasInitialized)
            {
                lastIsDayState = currentIsDayState;
                hasInitialized = true;
                return;
            }
            
            // Only update when day/night actually changes
            if (currentIsDayState != lastIsDayState)
            {
                lastIsDayState = currentIsDayState;
                UpdateHealthBarVisibility(); // Use separate method for visibility updates
            }
        }
    }

    protected virtual void Start()
    {
        if (isRegisteredWithGameLoop)
        {
            Debug.LogWarning($"[Structure] Start() called but already registered: {gameObject.name}", this);
        }
        if (gameObject.name == "BuildGhost")
        {
            return;
        }

        gridController = FindFirstObjectByType<GridController>();

        if (gridController == null)
        {
            Debug.LogWarning("Grid controller not found! Structure won't register with the grid.", this);
            return;
        }

        // First, attempt to register the structure with the grid if requested
        if (autoRegisterWithGrid && gridController != null)
        {
            RegisterWithGrid();
        }

        // Only register with GameLoopManager if not already registered (prevents double registration)
        // and if the structure either does not occupy grid cells or has at least one valid occupied cell.
        bool hasValidGridOccupation = !occupiesGridCells || (occupiedCells != null && occupiedCells.Count > 0);
        if (!isRegisteredWithGameLoop && GameLoopManager.Instance != null && !GameLoopManager.Instance.IsStructureRegistered(this) && hasValidGridOccupation)
        {
            GameLoopManager.Instance.RegisterStructure(this);
            isRegisteredWithGameLoop = true;
        }

        if (GetComponent<Collider>() == null)
        {
            AddColliderToStructure();
        }

        // Instantiate health bar if prefab is assigned
        if (healthBarPrefab != null && healthBarInstance == null)
        {
            healthBarInstance = Instantiate(healthBarPrefab, transform);

            // Position the health bar above the structure based on its height
            var rect = healthBarInstance.GetComponent<RectTransform>();
            if (rect != null)
            {
                float structureHeight = GetStructureHeight();
                rect.localPosition = new Vector3(0, structureHeight + 1.5f, 0); // Add 1.5f buffer above structure
            }
            healthBarSlider = healthBarInstance.GetComponentInChildren<Slider>();
            healthBarText = healthBarInstance.GetComponentInChildren<TextMeshProUGUI>();
            healthBarCanvasGroup = healthBarInstance.GetComponentInChildren<CanvasGroup>();
            healthBarInstance.SetActive(false); // Start hidden
            
            // Set initial visibility based on current health and time of day
            UpdateHealthBar();
        }


    }

    private void UpdateHealthBar()
    {
        if (healthBarSlider != null)
        {
            float pct = Mathf.Clamp01((float)GetCurrentHealth() / GetMaxHealth());
            healthBarSlider.value = pct;
            healthBarSlider.fillRect.GetComponent<Image>().color =
                pct > 0.6f ? healthyColor : pct > 0.3f ? midColor : dangerColor;

            if (healthBarText != null)
                healthBarText.text = $"{GetCurrentHealth()} / {GetMaxHealth()}";

            // Show health bar based on health percentage and time of day
            if (healthBarCanvasGroup != null)
            {
                bool isDaytime = NightManager.Instance != null ? NightManager.Instance.IsDay : true;
                float healthPercent = (float)GetCurrentHealth() / GetMaxHealth();
                
                // During day: hide health bar if health is above 50%
                // During night: always show health bar if not at full health (for danger awareness)
                bool shouldShow = GetCurrentHealth() < GetMaxHealth() && 
                                 (!isDaytime || healthPercent <= 0.5f);
                
                // Smoothly fade health bar in/out
                FadeHealthBar(shouldShow);
            }

            // Animate feedback only if health bar is visible
            if (healthBarCanvasGroup != null && healthBarCanvasGroup.alpha > 0f)
            {
                LeanTween.cancel(healthBarInstance);
                LeanTween.alphaCanvas(healthBarCanvasGroup, 0.7f, 0.3f).setLoopPingPong(1);
                if (pct < 0.3f)
                {
                    LeanTween.moveLocalX(healthBarInstance, healthBarInstance.transform.localPosition.x + 0.1f, 0.1f)
                        .setLoopPingPong(2);
                }
            }

        }
        else
        {
            Debug.LogWarning($"HealthBarSlider is null for {gameObject.name}");
        }
    }

    // Optional: If something is calling OnEnable, ensure it doesn't re-register
    private void OnEnable()
    {
        // Do not register here; registration is handled in Start only
    }

    public void SetAllowSelectionAndUI(bool allow)
    {
        allowSelectionAndUI = allow;
        if (!allow && isSelected)
        {
            Deselect();
        }
    }

    protected virtual void OnDestroy()
    {
        if (hasRegisteredWithGrid && gridController != null)
        {
            TargetManager.Instance?.UnregisterTarget(this);
            UnregisterFromGrid();
        }
        // Moved UnregisterStructure to Die to avoid delay

        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }


    #endregion

    #region Grid Integration

    public void RegisterWithGrid()
    {
        if (gridController == null || !occupiesGridCells) return;
        if (gameObject.name == "BuildGhost") return;

        CalculateOccupiedCells();

        if (blocksPathfinding)
        {
            foreach (Vector2Int cellPos in occupiedCells)
            {
                if (gridController.IsValidCell(cellPos.x, cellPos.y))
                {
                    GridCell cell = gridController.GetCell(cellPos.x, cellPos.y);
                    if (cell != null)
                    {
                        cell.flags.isObstacle = true;
                    }
                }
            }
        }

        hasRegisteredWithGrid = true;
    }

    public void UnregisterFromGrid()
    {
        if (gridController == null || !hasRegisteredWithGrid || !occupiesGridCells) return;

        foreach (Vector2Int cellPos in occupiedCells)
        {
            if (gridController.IsValidCell(cellPos.x, cellPos.y))
            {
                gridController.SetCellOccupied(cellPos.x, cellPos.y, false);
                if (blocksPathfinding)
                {
                    GridCell cell = gridController.GetCell(cellPos.x, cellPos.y);
                    if (cell != null)
                    {
                        cell.flags.isObstacle = false;
                    }
                }
            }
        }

        hasRegisteredWithGrid = false;
    }

    private void CalculateOccupiedCells()
    {
        occupiedCells.Clear();
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds combinedBounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            combinedBounds.Encapsulate(renderers[i].bounds);
        }

        combinedBounds.Expand(-0.1f);
        Vector2Int bottomLeft = gridController.WorldToGridCoords(combinedBounds.min);
        Vector2Int topRight = gridController.WorldToGridCoords(combinedBounds.max);

        for (int x = bottomLeft.x; x <= topRight.x; x++)
        {
            for (int y = bottomLeft.y; y <= topRight.y; y++)
            {
                if (gridController.IsValidCell(x, y))
                {
                    occupiedCells.Add(new Vector2Int(x, y));
                }
            }
        }
    }

    #endregion

    #region Health and Damage

    public void TakeDamage(int amount)
    {
        if (isIndestructible) return;

        currentHealth -= amount;
        
        // Record damage taken for combat statistics
        if (CombatStatistics.Instance != null)
        {
            CombatStatistics.Instance.RecordDamageTaken(amount);
        }
        
        OnDamaged?.Invoke(this);
        OnHealthChanged?.Invoke(); // Trigger health changed event for UI

        if (!hasTriggeredLowHealthEvent && currentHealth <= (int)(GetMaxHealth() * 0.3f))
        {
            hasTriggeredLowHealthEvent = true;
            OnAnyStructureDamaged.Invoke();
        }

        if (currentHealth <= 0)
        {
            TargetManager.Instance?.UnregisterTarget(this);
            Die();
        }
    }

    // public virtual void Die()
    // {
    //     OnDestroyed?.Invoke(this);
    //     OnStructureDestroyed?.Invoke(this);

    //     // foreach (Wolf wolf in registeredWolves.ToList())
    //     // {
    //     //     if (wolf != null && wolf)
    //     //         wolf.OnTargetDestroyed(gameObject);
    //     // }

    //     if (destructionEffectPrefab != null)
    //     {
    //         Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
    //     }

    //     // Unregister from GameLoopManager immediately
    //     if (isRegisteredWithGameLoop && GameLoopManager.Instance != null)
    //     {
    //         GameLoopManager.Instance.UnregisterStructure(this);
    //         isRegisteredWithGameLoop = false;
    //     }
    //     UnregisterFromGrid();

    //     if (destroyOnZeroHealth)
    //     {
    //         Destroy(gameObject, destroyDelay);
    //     }
    // }

    public virtual void Die()
    {
        TargetManager.Instance.UnregisterTarget(this);
        OnDestroyed?.Invoke(this);
        OnStructureDestroyed?.Invoke(this);

        if (destructionEffectPrefab != null)
        {
            Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Remove from BuildingManager list
        BuildingManager.Instance?.removeBuilding(this.gameObject);

        if (isRegisteredWithGameLoop && GameLoopManager.Instance != null)
        {
            GameLoopManager.Instance.UnregisterStructure(this);
            isRegisteredWithGameLoop = false;
        }
        UnregisterFromGrid();

        if (destroyOnZeroHealth)
        {
            TargetManager.Instance?.UnregisterTarget(this);
            Destroy(gameObject, destroyDelay);
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth;
    }

    public int GetMaxHealth()
    {
        return structureData != null ? structureData.health : 100;
    }

    public bool IsDead()
    {
        return currentHealth <= 0;
    }

    public bool canBeRepaired()
    {
        return (float)currentHealth / GetMaxHealth() <= 0.3f;
    }

    public bool Repair()
    {
        if(!canBeRepaired())
        {
            return false;
        }

        currentHealth = GetMaxHealth();
        OnHealthChanged?.Invoke();

        if (hasTriggeredLowHealthEvent)
        {
            hasTriggeredLowHealthEvent = false;
            // OnAnyStructureDamaged.Invoke();
        }
        return true;
    }

    #endregion

    #region Utility

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

    private void UpdateHealthBarVisibility()
    {
        // Only update visibility logic without changing health values
        if (healthBarCanvasGroup != null)
        {
            bool isDaytime = NightManager.Instance != null ? NightManager.Instance.IsDay : true;
            float healthPercent = (float)GetCurrentHealth() / GetMaxHealth();
            
            // During day: hide health bar if health is above 50%
            // During night: always show health bar if not at full health (for danger awareness)
            bool shouldShow = GetCurrentHealth() < GetMaxHealth() && 
                             (!isDaytime || healthPercent <= 0.5f);
            
            Debug.Log($"[{gameObject.name}] Day/Night Change - Health: {GetCurrentHealth()}/{GetMaxHealth()} | IsDay: {isDaytime} | ShouldShow: {shouldShow}");
            
            // Smoothly fade health bar in/out
            FadeHealthBar(shouldShow);
        }
    }

    private void FadeHealthBar(bool shouldShow)
    {
        if (healthBarCanvasGroup == null || healthBarInstance == null) return;

        // Cancel any existing fade animations
        LeanTween.cancel(healthBarInstance);

        float targetAlpha = shouldShow ? 1f : 0f;
        float currentAlpha = healthBarCanvasGroup.alpha;

        // Always make sure the health bar is active when we need to show it
        if (shouldShow)
        {
            healthBarInstance.SetActive(true);
        }

        // Check if we need to animate or set immediately
        if (Mathf.Abs(currentAlpha - targetAlpha) > 0.01f)
        {
            // Smooth fade animation
            LeanTween.alphaCanvas(healthBarCanvasGroup, targetAlpha, 5f)
                .setEase(LeanTweenType.easeOutQuad)
                .setOnComplete(() => {
                    // Hide the GameObject when fully faded out to save performance
                    if (targetAlpha == 0f)
                    {
                        healthBarInstance.SetActive(false);
                    }
                });
        }
        else if (!shouldShow && currentAlpha == 0f)
        {
            // If already hidden, make sure GameObject is deactivated
            healthBarInstance.SetActive(false);
        }

        // Update interactability immediately
        healthBarCanvasGroup.interactable = shouldShow;
        healthBarCanvasGroup.blocksRaycasts = shouldShow;
    }

    public bool OccupiesCell(int x, int y)
    {
        return occupiedCells.Contains(new Vector2Int(x, y));
    }

    public bool GetAllowSelectionAndUI()
    {
        return allowSelectionAndUI;
    }

    public string GetStructureName()
    {
        return structureData != null ? structureData.structureName : gameObject.name;
    }

    public StructureType GetStructureType()
    {
        return structureData != null ? structureData.type : StructureType.Building;
    }

    public int GetCost()
    {
        return structureData != null ? structureData.cost : 0;
    }

    public int GetRepairCost()
    {
        return structureData != null ? structureData.RepairCost : 0;
    }

    #endregion

    #region Debug Visualization

    private void OnDrawGizmosSelected()
    {
        if (!showDebugInfo || !Application.isPlaying || gridController == null)
            return;

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        foreach (Vector2Int cell in occupiedCells)
        {
            if (gridController.IsValidCell(cell.x, cell.y))
            {
                Vector3 cellCenter = gridController.GetCellCenterFromTexture(cell.x, cell.y);
                float cellSize = gridController.GetCellSize();
                Gizmos.DrawCube(cellCenter, new Vector3(cellSize * 0.9f, 0.1f, cellSize * 0.9f));
            }
        }

        Renderer renderer = GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Bounds bounds = renderer.bounds;
            Vector3 top = new Vector3(bounds.center.x, bounds.max.y + 0.5f, bounds.center.z);
            float healthPct = (float)currentHealth / GetMaxHealth();
            float width = bounds.size.x;

            Gizmos.color = Color.gray;
            Gizmos.DrawCube(top, new Vector3(width, 0.1f, 0.1f));
            Gizmos.color = Color.green;
            Gizmos.DrawCube(
                top - new Vector3((width * (1 - healthPct)) / 2, 0, 0),
                new Vector3(width * healthPct, 0.1f, 0.1f)
            );
        }
    }

    #endregion

    #region Selection and Interaction

    public virtual void Select()
    {
        if (!allowSelectionAndUI) return;
        isSelected = true;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(true);
        }
    }

    public virtual void Deselect()
    {
        isSelected = false;
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }

        // Remove selection animation component if it exists
        SelectionAnimation selector = GetComponent<SelectionAnimation>();
        if (selector != null)
        {
            Destroy(selector);
        }

        Collider col = GetComponent<Collider>();
        if (col != null && !col.enabled)
        {
            col.enabled = true;
        }
    }

    private void AddColliderToStructure()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, combinedBounds.size.y / 2, 0);
            boxCollider.size = combinedBounds.size;
        }
        else
        {
            BoxCollider boxCollider = gameObject.AddComponent<BoxCollider>();
            boxCollider.center = new Vector3(0, 0.5f, 0);
            boxCollider.size = new Vector3(1, 1, 1);
        }
    }

    public bool IsSelected()
    {
        return isSelected;
    }

    public void ApplyDamage(int damage)
    {
        // Check god mode
        if (CheatManager.Instance != null && CheatManager.Instance.IsGodModeActive())
        {
            return;
        }

        currentHealth = Mathf.Max(0, currentHealth - damage);

        if (currentHealth <= 0)
        {
            OnStructureDestroyed?.Invoke(this); // Pass 'this' as the argument
        }
    }

    // Add cheat method
    public void CheatSetMaxHealth()
    {
        currentHealth = GetMaxHealth();
        UpdateHealthBar();
    }

    public void SetStructureType(StructureType type)
    {
        if (structureData != null)
        {
            structureData.type = type;
        }
    }

    #endregion

    public string GetDescription()
    {
        return structureData != null ? structureData.description : "No description available.";
    }
}
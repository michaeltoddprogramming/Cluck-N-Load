using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class StructureItemUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public Image icon;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public TextMeshProUGUI descriptionText;
    // public Button selectButton;
    public GameObject lockedOverlay;
    public NightManager nightManager;
    public UIHoverManager hoverManager;
    public Image moneyOverlay;

    public float magnatude = 7.5f;

    private float pulseScale = 1.2f;       // same scale as progress bar
    private float pulseDuration = 0.5f;    // ping-pong duration
    private bool isPulsing = false;


    private StructureData data;
    private BuildController buildController;

    // Public property to access the structure data
    public StructureData Data => data;

    // Track if item is locked due to day requirement
    private bool isLockedByDay = false;
    private bool isLockedByTutorial = false;  // NEW: Track tutorial lock state
    private bool previousLockedState = false;
    private bool isFirstUpdate = true;

    private void Start()
    {
        buildController = FindFirstObjectByType<BuildController>();
        nightManager = FindFirstObjectByType<NightManager>();
        hoverManager = FindFirstObjectByType<UIHoverManager>();
    }

    private void Update()
    {
        // UpdateAffordability();
        // Dynamically update lock state in case cheat is toggled or tutorial state changes
        if (data != null)
        {
            int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;
            bool unlockAllBuildsActive = CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive();
            isLockedByDay = !unlockAllBuildsActive && (data.unlockDay > currentDay);
            
            // Update tutorial lock state dynamically
            isLockedByTutorial = false;
            if (SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive())
            {
                if (SimplifiedTutorialManager.Instance.ShouldRestrictShopBuildings())
                {
                    isLockedByTutorial = !SimplifiedTutorialManager.Instance.IsBuildingAllowed(data.structureName);
                }
            }
        }

        bool isLocked = isLockedByDay || isLockedByTutorial;

        // Update locked overlay on first frame or when state changes
        if (lockedOverlay != null && (isFirstUpdate || previousLockedState != isLocked))
        {
            lockedOverlay.SetActive(isLocked);
            // Update the overlay text based on lock type
            if (isLocked)
            {
                var overlayText = lockedOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (overlayText != null)
                {
                    if (isLockedByTutorial)
                    {
                        overlayText.text = "Complete Tutorial Step";
                    }
                    else
                    {
                        overlayText.text = $"Unlocks on Day {data?.unlockDay ?? 0}";
                    }
                }
            }
            previousLockedState = isLocked;
            isFirstUpdate = false;
        }

        // Grayscale overlay logic (apply regardless of day lock status)
        Transform grayscaleOverlay = transform.Find("grayOverlay");
        if (grayscaleOverlay != null && nightManager != null)
        {
            if (nightManager.getIsPaused())
            {
                grayscaleOverlay.gameObject.SetActive(true);
            }
            else
            {
                grayscaleOverlay.gameObject.SetActive(false);
            }
        }
        else if (nightManager != null && !isLocked)
        {
            // For unlocked items without grayOverlay, apply pause effect directly to icon
            bool isPaused = nightManager.getIsPaused();
            if (isPaused && icon != null)
            {
                // Darken the icon when paused
                icon.color = new Color(0.4f, 0.4f, 0.4f, 0.7f);
            }
            else if (!isPaused && icon != null)
            {
                // Restore normal color when not paused
                UpdateAffordability(); // This will set the correct color based on affordability
            }
                 // This will set the correct color based on affordability
        }

        // Button interactability considers day lock, tutorial lock, affordability, and pause state
        // if (selectButton != null)
        // {
        //     bool isPaused = nightManager != null && nightManager.getIsPaused();
        //     selectButton.interactable = !isLockedByDay && !isLockedByTutorial && !isPaused &&
        //         (MoneyManager.Instance?.CanAfford(data?.cost ?? 0) ?? false);
        // }
    }

    public void Setup(StructureData structure)
    {
        if (structure == null)
        {
            Debug.LogError("StructureData is null!");
            return;
        }

        data = structure;

        if (icon != null)
            icon.sprite = structure.icon;

        if (nameText != null)
            nameText.text = structure.structureName;

        if (costText != null)
        {
            if(!MoneyManager.Instance.CanAfford(structure.cost))
            {
                costText.color = Color.red;
                costText.text = $"{structure.cost}";
            }
            else
            {
                costText.color = Color.white;
                costText.text = $"{structure.cost}";
            }
                // costText.text = $"{structure.cost}";
        }

        if (descriptionText != null)
            descriptionText.text = structure.description;

        int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;

        // Check if "Unlock All Buildings" cheat is active
        bool unlockAllBuildsActive = CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive();

        isLockedByDay = !unlockAllBuildsActive && (structure.unlockDay > currentDay); // Store the day lock state, but cheat overrides
        
        // NEW: Check if building is locked by tutorial restrictions
        isLockedByTutorial = false;
        if (SimplifiedTutorialManager.Instance != null && SimplifiedTutorialManager.Instance.IsTutorialActive())
        {
            if (SimplifiedTutorialManager.Instance.ShouldRestrictShopBuildings())
            {
                isLockedByTutorial = !SimplifiedTutorialManager.Instance.IsBuildingAllowed(structure.structureName);
                Debug.Log($"[StructureItemUI] {structure.structureName} tutorial locked: {isLockedByTutorial}");
            }
        }
        
        bool isLocked = isLockedByDay || isLockedByTutorial;
        
        // Set the locked overlay immediately in Setup
        if (lockedOverlay != null)
        {
            lockedOverlay.SetActive(isLocked);
        }
        
        previousLockedState = isLocked; // Initialize the previous state
        isFirstUpdate = false; // Mark that we've already set the initial state

        if (isLocked)
        {
            // if (selectButton != null)
            //     selectButton.interactable = false;
            if (icon != null)
                icon.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            if (lockedOverlay != null)
            {
                var unlockText = lockedOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (unlockText != null)
                {
                    if (isLockedByTutorial)
                    {
                        unlockText.text = "Complete Tutorial Step";
                    }
                    else
                    {
                        unlockText.text = $"Unlocks on Day {structure.unlockDay}";
                    }
                }
            }
            return;
        }

        // if (selectButton != null)
        // {
        //     selectButton.onClick.RemoveAllListeners();
        //     selectButton.onClick.AddListener(SelectStructure);
        // }

        UpdateAffordability();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    public void SelectStructure()
    {
        // Check if item is locked by day requirement or tutorial
        if (isLockedByDay || isLockedByTutorial)
        {
            PlayErrorSound();
            return;
        }

        if (nightManager != null && nightManager.getIsPaused())
        {
            PlayErrorSound();
            return;
        }
        
        if (data == null)
        {
            PlayErrorSound();
            return;
        }

        if(!MoneyManager.Instance.CanAfford(data.cost))
        {
            // ShakeCamera(magnatude, 0.2f);

            // Fade the money overlay in and out
            if (moneyOverlay != null)
            {
                moneyOverlay.gameObject.SetActive(true);
                moneyOverlay.canvasRenderer.SetAlpha(0f); // start invisible
                moneyOverlay.CrossFadeAlpha(1f, 0.25f, false); // fade in
                LeanTween.delayedCall(0.5f, () =>
                {
                    moneyOverlay.CrossFadeAlpha(0f, 0.25f, false); // fade out
                });
            }

            hoverManager.PlayErrorFeedbackForGameObject(true, this.gameObject);
            return;
        }

        // Success - play select sound and proceed
        PlaySelectSound();
        
        BuildController controller = FindFirstObjectByType<BuildController>();
        if (controller != null)
            controller.SetBuildTarget(data);
    }

    private void OnDestroy()
    {
        // if (selectButton != null)
        //     selectButton.onClick.RemoveAllListeners();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int newAmount) => UpdateAffordability();

    public void UpdateAffordability()
    {
        // if (data == null || selectButton == null || MoneyManager.Instance == null)
        if (data == null || MoneyManager.Instance == null)
        {
            // Debug.Log("here is the info after testing the affordability: " + data + selectButton + MoneyManager.Instance);
            Debug.Log("here is the info after testing the affordability: " + data + MoneyManager.Instance);
            return;
        }

        bool canAfford = MoneyManager.Instance.CanAfford(data.cost);
        bool isPaused = nightManager != null && nightManager.getIsPaused();
        bool isLocked = isLockedByDay || isLockedByTutorial;

        // Button interactability considers affordability, day lock, tutorial lock, and pause state
        // selectButton.interactable = canAfford && !isLockedByDay && !isLockedByTutorial && !isPaused;

        if (costText != null)
        {
            costText.color = canAfford ? Color.white : Color.red;
            costText.text = $"{data.cost}";
        }

        // Only update icon color if not paused and not locked (pause effects are handled in Update)
        if (icon != null && !isPaused && !isLocked)
            icon.color = canAfford ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.8f);

        // Notify BuildController to update ghost if affordability changed
        if (buildController != null)  // Use the cached reference
        {
            buildController.UpdateGhostAffordability(canAfford);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Show(data);

        onHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();

        onHoverExit();
    }

    private void PlaySelectSound()
    {
        // Use AudioManager for consistent audio playback
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySelectSound();
        }
    }
    
    private void PlayErrorSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayErrorSound();
        }
    }

    // public IEnumerator ShakeCameraCoroutine(float magnitude = 0.1f, float duration = 0.2f)
    // {
    //     Camera cam = Camera.main;
    //     if (cam == null) yield break;

    //     Vector3 originalPos = cam.transform.position;
    //     float elapsed = 0f;

    //     while (elapsed < duration)
    //     {
    //         float x = Random.Range(-magnitude, magnitude);
    //         float y = Random.Range(-magnitude, magnitude);

    //         cam.transform.position = originalPos + new Vector3(x, y, 0);

    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }

    //     cam.transform.position = originalPos; // restore
    // }

    // public void ShakeCamera(float magnitude = 0.1f, float duration = 0.2f)
    // {
    //     Camera cam = Camera.main;
    //     if (cam == null) return;

    //     Vector3 originalPos = cam.transform.position;

    //     LeanTween.value(cam.gameObject, 0f, 1f, duration)
    //         .setEase(LeanTweenType.easeShake)
    //         .setOnUpdate((float val) =>
    //         {
    //             float x = Random.Range(-magnitude, magnitude) * val;
    //             float y = Random.Range(-magnitude, magnitude) * val;
    //             cam.transform.position = originalPos + new Vector3(x, y, 0);
    //         })
    //         .setOnComplete(() =>
    //         {
    //             cam.transform.position = originalPos; // restore
    //         });
    // }

    public void onHover()
    {
        if(nightManager.getIsPaused())
        {
            hoverManager.ShowHoverOnGameObject(this.gameObject, "Freeze!", "Can’t build while the game’s paused.", false, new Vector2(0, 270));
        }
        else if (!MoneyManager.Instance.CanAfford(data.cost) && costText != null && !isPulsing)
        {
            isPulsing = true;

            RectTransform rt = costText.GetComponent<RectTransform>();
            if (rt != null)
                rt.pivot = new Vector2(0.5f, 0.5f);

            LeanTween.scale(costText.gameObject, Vector3.one * pulseScale, pulseDuration)
                .setEaseInOutSine()
                .setLoopPingPong();
        }
    }

    public void onHoverExit()
    {
        if(nightManager.getIsPaused())
        {
            hoverManager.HideHover();
        }
        else if (costText != null && isPulsing)
        {
            LeanTween.cancel(costText.gameObject);      // stop the animation
            costText.transform.localScale = Vector3.one; // reset scale
            isPulsing = false;
        }
    }

}

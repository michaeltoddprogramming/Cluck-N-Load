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
    public Button selectButton;
    public GameObject lockedOverlay;
    public NightManager nightManager;

    private StructureData data;
    private BuildController buildController;

    // Public property to access the structure data
    public StructureData Data => data;

    // Track if item is locked due to day requirement
    private bool isLockedByDay = false;

    private void Start()
    {
        buildController = FindFirstObjectByType<BuildController>();
        nightManager = FindFirstObjectByType<NightManager>();
    }

    private void Update()
    {
        // Dynamically update lock state in case cheat is toggled
        if (data != null)
        {
            int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;
            bool unlockAllBuildsActive = CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive();
            isLockedByDay = !unlockAllBuildsActive && (data.unlockDay > currentDay);
        }

        if (lockedOverlay != null)
        {
            // Only show overlay if locked by day requirement
            lockedOverlay.SetActive(isLockedByDay);
            // Update the overlay text for day lock
            if (lockedOverlay.activeInHierarchy && isLockedByDay)
            {
                var overlayText = lockedOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (overlayText != null)
                {
                    overlayText.text = $"Unlocks on Day {data?.unlockDay ?? 0}";
                }
            }
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
        else if (nightManager != null && !isLockedByDay)
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
        }

        // Button interactability considers day lock, affordability, and pause state
        if (selectButton != null)
        {
            bool isPaused = nightManager != null && nightManager.getIsPaused();
            selectButton.interactable = !isLockedByDay && !isPaused &&
                (MoneyManager.Instance?.CanAfford(data?.cost ?? 0) ?? false);
        }
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
            costText.text = $"{structure.cost}";

        if (descriptionText != null)
            descriptionText.text = structure.description;

        int currentDay = NightManager.Instance != null ? NightManager.Instance.Days : 0;

        // Check if "Unlock All Buildings" cheat is active
        bool unlockAllBuildsActive = CheatManager.Instance != null && CheatManager.Instance.IsUnlockAllBuildsActive();

        isLockedByDay = !unlockAllBuildsActive && (structure.unlockDay > currentDay); // Store the day lock state, but cheat overrides

        if (isLockedByDay)
        {
            if (selectButton != null)
                selectButton.interactable = false;
            if (icon != null)
                icon.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

            if (lockedOverlay != null)
            {
                var unlockText = lockedOverlay.GetComponentInChildren<TextMeshProUGUI>();
                if (unlockText != null)
                    unlockText.text = $"Unlocks on Day {structure.unlockDay}";
            }
            return;
        }

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => SelectStructure());
        }

        UpdateAffordability();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged += OnMoneyChanged;
    }

    public void SelectStructure()
    {

        if (nightManager != null && nightManager.getIsPaused())
        {
            Debug.Log("Cannot select structure while game is paused.");
            return;
        }

        if (data == null)
        {
            Debug.LogError("StructureData is null when selecting structure!");
            return;
        }

        BuildController controller = FindFirstObjectByType<BuildController>();
        if (controller != null)
            controller.SetBuildTarget(data);
    }

    private void OnDestroy()
    {
        if (selectButton != null)
            selectButton.onClick.RemoveAllListeners();

        if (MoneyManager.Instance != null)
            MoneyManager.Instance.OnMoneyChanged -= OnMoneyChanged;
    }

    private void OnMoneyChanged(int newAmount) => UpdateAffordability();

    public void UpdateAffordability()
    {
        if (data == null || selectButton == null || MoneyManager.Instance == null)
            return;

        bool canAfford = MoneyManager.Instance.CanAfford(data.cost);
        bool isPaused = nightManager != null && nightManager.getIsPaused();

        // Button interactability considers affordability, day lock, and pause state
        selectButton.interactable = canAfford && !isLockedByDay && !isPaused;

        if (costText != null)
        {
            costText.color = canAfford ? Color.white : Color.red;
            costText.text = canAfford ? $"{data.cost}" : $"{data.cost} (Cannot Afford!)";
        }

        // Only update icon color if not paused (pause effects are handled in Update)
        if (icon != null && !isPaused && !isLockedByDay)
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
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (ItemHoverPanel.Instance != null)
            ItemHoverPanel.Instance.Hide();
    }

    public void playErrorSound()
    {
        if (isLockedByDay)
        {
            AudioManager.Instance.PlayErrorSound();
        }
    }
}

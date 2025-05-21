using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CropStructureUI : BaseStructureUI
{
    [Header("UI Elements")]
    [SerializeField] private Button plantSunflowerButton;
    [SerializeField] private Button plantWheatButton;
    [SerializeField] private Button plantCarrotsButton;
    [SerializeField] private Button harvestButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI expectedYieldText; // New: Display expected yield
    [SerializeField] private TextMeshProUGUI inventoryText;     // New: Display inventory

    [SerializeField] private GameObject selectCropPanel; 



    private CropStructure cropStructure;
    private bool isCropStructure = false;
    private NightManager nightManager;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        isCropStructure = structure is CropStructure;
        if (isCropStructure)
        {
            cropStructure = (CropStructure)structure;
        }

        nightManager = FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError("NightManager not found in the scene! CropStructureUI requires NightManager to function.");
        }

        if (!isCropStructure)
        {
            Debug.LogWarning($"CropStructureUI used with non-crop structure: {structure.GetType().Name}");
            HideCropSpecificUI();
            return;
        }

        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Select Crop Panel is not assigned in the CropStructureUI prefab!");
        }


        SetupButtonListeners();
        UpdateUI();
    }
    
    public void ShowSelectCropPanel()
    {
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Select Crop Panel is not assigned in the CropStructureUI prefab!");
        }
    }

    private void SetupButtonListeners()
    {
        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.onClick.RemoveAllListeners();
            plantSunflowerButton.onClick.AddListener(() =>
            {
                cropStructure.Plant(CropStructure.CropType.Sunflower);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Plant Sunflower button is not assigned in the CropStructureUI prefab!");
            if (plantSunflowerButton != null) plantSunflowerButton.gameObject.SetActive(false);
        }

        if (plantWheatButton != null)
        {
            plantWheatButton.onClick.RemoveAllListeners();
            plantWheatButton.onClick.AddListener(() =>
            {
                cropStructure.Plant(CropStructure.CropType.Wheat);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Plant Wheat button is not assigned in the CropStructureUI prefab!");
            if (plantWheatButton != null) plantWheatButton.gameObject.SetActive(false);
        }

        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.onClick.RemoveAllListeners();
            plantCarrotsButton.onClick.AddListener(() =>
            {
                cropStructure.Plant(CropStructure.CropType.Carrots);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Plant Carrots button is not assigned in the CropStructureUI prefab!");
            if (plantCarrotsButton != null) plantCarrotsButton.gameObject.SetActive(false);
        }

        if (harvestButton != null)
        {
            harvestButton.onClick.RemoveAllListeners();
            harvestButton.onClick.AddListener(() =>
            {
                cropStructure.Harvest();
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Harvest button is not assigned in the CropStructureUI prefab!");
            if (harvestButton != null) harvestButton.gameObject.SetActive(false);
        }
    }

    protected override void Update()
    {
        base.Update();
        if (isCropStructure)
        {
            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (!isCropStructure || cropStructure == null || nightManager == null)
        {
            return;
        }

        bool isGrowing = cropStructure.IsGrowing;
        bool cropReady = cropStructure.CropReady;
        string cropName = cropStructure.CurrentCropType.ToString();

        // Update button visibility
        if (plantSunflowerButton != null)
            plantSunflowerButton.gameObject.SetActive(!isGrowing && !cropReady);
        if (plantWheatButton != null)
            plantWheatButton.gameObject.SetActive(!isGrowing && !cropReady);
        if (plantCarrotsButton != null)
            plantCarrotsButton.gameObject.SetActive(!isGrowing && !cropReady);
        if (harvestButton != null)
            harvestButton.gameObject.SetActive(cropReady);

        // Update status text and progress bar
        if (statusText != null)
        {
            if (cropReady)
            {
                statusText.text = $"Crop Plot: {cropName} ready to harvest!";
                statusText.color = Color.green;

                if (progressBar != null)
                    progressBar.gameObject.SetActive(false);
            }
            else if (isGrowing)
            {
                float progress = cropStructure.GrowthProgress;
                float totalTime = cropStructure.ProductionSettings.growthTime;

                float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
                float remainingHours = totalTime - progress;
                float completionHour = (currentHour + remainingHours) % 24f;
                int completionHourInt = Mathf.FloorToInt(completionHour);
                int completionMinuteInt = Mathf.CeilToInt((completionHour - completionHourInt) * 60f);

                if (completionMinuteInt == 60)
                {
                    completionHourInt = (completionHourInt + 1) % 24;
                    completionMinuteInt = 0;
                }

                statusText.text = $"Crop Plot: Growing {cropName}... (Finishes at {completionHourInt:D2}:{completionMinuteInt:D2})";
                statusText.color = Color.yellow;

                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(true);
                    progressBar.maxValue = totalTime;
                    progressBar.value = progress;
                }
            }
            else
            {
                statusText.text = "Crop Plot: Choose a crop to plant";
                statusText.color = Color.white;

                if (progressBar != null)
                    progressBar.gameObject.SetActive(false);
            }
        }

        // Update expected yield
        if (expectedYieldText != null)
        {
            if (isGrowing || cropReady)
            {
                int expectedYield = Mathf.RoundToInt(cropStructure.ProductionSettings.baseProductAmount * cropStructure.ProductionMultiplier);
                expectedYieldText.text = $"Expected Yield: {expectedYield} {cropName}";
                expectedYieldText.gameObject.SetActive(true);
            }
            else
            {
                expectedYieldText.gameObject.SetActive(false);
            }
        }

        // Update inventory display
        if (inventoryText != null)
        {
            inventoryText.text = $"Inventory: {InventoryManager.Instance.GetItemCount("Sunflower")} Sunflower, " +
                                $"{InventoryManager.Instance.GetItemCount("Wheat")} Wheat, " +
                                $"{InventoryManager.Instance.GetItemCount("Carrots")} Carrots";
        }
    }

    private void HideCropSpecificUI()
    {
        if (plantSunflowerButton != null) plantSunflowerButton.gameObject.SetActive(false);
        if (plantWheatButton != null) plantWheatButton.gameObject.SetActive(false);
        if (plantCarrotsButton != null) plantCarrotsButton.gameObject.SetActive(false);
        if (harvestButton != null) harvestButton.gameObject.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (statusText != null)
        {
            statusText.text = "Not a crop structure";
            statusText.color = Color.red;
        }
        if (expectedYieldText != null) expectedYieldText.gameObject.SetActive(false);
        if (inventoryText != null) inventoryText.gameObject.SetActive(false);
    }
}
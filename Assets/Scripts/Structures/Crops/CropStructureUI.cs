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
    [SerializeField] private GameObject selectCropPanel;
    [SerializeField] private GameObject plantingClose;
    [SerializeField] private TextMeshProUGUI cropStatusText;

    [Header("Dependencies")]
    [SerializeField] private NightManager nightManager;

    private CropStructure cropStructure;
    private bool isCropStructure = false;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        isCropStructure = structure is CropStructure;

        if (isCropStructure)
        {
            cropStructure = (CropStructure)structure;
        }
        else
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

        if (cropStatusText == null)
        {
            Debug.LogWarning("Crop Status Text is not assigned in the CropStructureUI prefab!");
        }

        if (nightManager == null)
        {
            nightManager = NightManager.Instance;
            if (nightManager == null)
            {
                nightManager = FindObjectOfType<NightManager>();
                if (nightManager == null)
                {
                    Debug.LogError("NightManager not found in scene!");
                }
            }
        }

        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.onClick.RemoveAllListeners();
            plantSunflowerButton.onClick.AddListener(() => plantCrop(0));
        }
        else
        {
            Debug.LogWarning("Plant Sunflower Button is not assigned!");
        }

        if (plantWheatButton != null)
        {
            plantWheatButton.onClick.RemoveAllListeners();
            plantWheatButton.onClick.AddListener(() => plantCrop(1));
        }
        else
        {
            Debug.LogWarning("Plant Wheat Button is not assigned!");
        }

        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.onClick.RemoveAllListeners();
            plantCarrotsButton.onClick.AddListener(() => plantCrop(2));
        }
        else
        {
            Debug.LogWarning("Plant Carrots Button is not assigned!");
        }

        if (harvestButton != null)
        {
            harvestButton.onClick.RemoveAllListeners();
            harvestButton.onClick.AddListener(harvestCrops);
        }
        else
        {
            Debug.LogWarning("Harvest Button is not assigned!");
        }

        if (plantingClose != null)
        {
            plantingClose.GetComponent<Button>()?.onClick.RemoveAllListeners();
            plantingClose.GetComponent<Button>()?.onClick.AddListener(closeSelectCropPanel);
        }
        else
        {
            Debug.LogWarning("Planting Close Button is not assigned!");
        }
    }

    public void ShowSelectCropPanel()
    {
        if (selectCropPanel != null && nightManager != null && nightManager.IsDay && cropStructure != null && !cropStructure.IsGrowing && !cropStructure.CropReady)
        {
            selectCropPanel.SetActive(true);
            UpdateUI();
            Debug.Log("ShowSelectCropPanel: Panel opened");
        }
        else
        {
            Debug.LogWarning($"Cannot show select crop panel: IsDay={nightManager?.IsDay}, Growing={cropStructure?.IsGrowing}, Ready={cropStructure?.CropReady}, PanelAssigned={selectCropPanel != null}");
            closeSelectCropPanel(); // Ensure panel closes if conditions aren't met
        }
    }

    public void closeSelectCropPanel()
    {
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(false);
            UpdateUI();
            Debug.Log("closeSelectCropPanel: Panel closed");
        }
        else
        {
            Debug.LogWarning("Select Crop Panel is not assigned!");
        }
    }

    public void plantCrop(int crop)
    {
        if (!isCropStructure || cropStructure == null)
        {
            Debug.LogWarning("Cannot plant crop: No valid CropStructure assigned.");
            return;
        }

        if (nightManager == null || !nightManager.IsDay)
        {
            Debug.LogWarning("Cannot plant crop: Planting is only allowed during the day!");
            closeSelectCropPanel();
            return;
        }

        CropStructure.CropType cropType;
        switch (crop)
        {
            case 0:
                cropType = CropStructure.CropType.Sunflower;
                break;
            case 1:
                cropType = CropStructure.CropType.Wheat;
                break;
            case 2:
                cropType = CropStructure.CropType.Carrots;
                break;
            default:
                Debug.LogWarning($"Invalid crop type selected: {crop}");
                return;
        }

        Debug.Log($"Attempting to plant {cropType} on {cropStructure.GetStructureName()}");
        cropStructure.Plant(cropType);
        closeSelectCropPanel();
        UpdateUI();
    }

    public void harvestCrops()
    {
        if (isCropStructure && cropStructure != null)
        {
            Debug.Log($"Attempting to harvest {cropStructure.CurrentCropType} on {cropStructure.GetStructureName()}");
            cropStructure.Harvest();
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("HarvestCrops called but no valid CropStructure found.");
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
            HideCropSpecificUI();
            return;
        }

        bool isGrowing = cropStructure.IsGrowing;
        bool cropReady = cropStructure.CropReady;
        bool canPlant = nightManager.IsDay && !isGrowing && !cropReady;

        // Update crop status text
        if (cropStatusText != null)
        {
            string status;
            if (cropReady)
            {
                status = $"{cropStructure.CurrentCropType}: Ready to harvest!";
                cropStatusText.color = Color.green;
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
                status = $"{cropStructure.CurrentCropType}: Growing...";
                cropStatusText.color = Color.yellow;
            }
            else
            {
                status = nightManager.IsDay ? "No crop planted" : "Cannot plant at night";
                cropStatusText.color = nightManager.IsDay ? Color.white : Color.red;
            }
            cropStatusText.text = status;
        }

        // Update button states
        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.interactable = canPlant;
            UpdateButtonVisual(plantSunflowerButton, canPlant, "Sunflower");
            Debug.Log($"SunflowerButton interactable: {canPlant}");
        }
        if (plantWheatButton != null)
        {
            plantWheatButton.interactable = canPlant;
            UpdateButtonVisual(plantWheatButton, canPlant, "Wheat");
            Debug.Log($"WheatButton interactable: {canPlant}");
        }
        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.interactable = canPlant;
            UpdateButtonVisual(plantCarrotsButton, canPlant, "Carrots");
            Debug.Log($"CarrotsButton interactable: {canPlant}");
        }
        if (harvestButton != null)
        {
            harvestButton.interactable = cropReady;
            UpdateButtonVisual(harvestButton, cropReady, "Harvest");
            Debug.Log($"HarvestButton interactable: {cropReady}");
        }
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(canPlant && selectCropPanel.activeSelf);
            Debug.Log($"SelectCropPanel active: {selectCropPanel.activeSelf}");
        }
        Debug.Log($"UpdateUI: canPlant={canPlant}, IsDay={nightManager.IsDay}, IsGrowing={isGrowing}, CropReady={cropReady}, Hours={nightManager.Hours}");
    }

           private void UpdateButtonVisual(Button button, bool isInteractable, string action)
        {
            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                // Handle harvest button differently from plant buttons
                if (action.Equals("Harvest"))
                {
                    // For the harvest button
                    if (isInteractable)
                    {
                        buttonText.text = "Harvest";
                    }
                    else
                    {
                        string reason;
                        if (cropStructure != null && cropStructure.IsGrowing)
                            reason = "Still Growing";
                        else if (cropStructure != null && cropStructure.CurrentCropType == CropStructure.CropType.None)
                            reason = "No Crop Planted";
                        else
                            reason = "Not Ready";
                        
                        buttonText.text = $"Harvest ({reason})";
                    }
                }
                else
                {
                    // For plant buttons
                    if (isInteractable)
                    {
                        buttonText.text = $"Plant {action}";
                    }
                    else
                    {
                        string reason;
                        if (nightManager != null && !nightManager.IsDay)
                            reason = "Night";
                        else if (cropStructure != null && cropStructure.IsGrowing)
                            reason = "Growing";
                        else if (cropStructure != null && cropStructure.CropReady)
                            reason = "Ready to Harvest";
                        else
                            reason = "Unavailable";
                        
                        buttonText.text = $"Plant {action} ({reason})";
                    }
                }
            }
        
            // Update button color to provide visual feedback
            ColorBlock colors = button.colors;
            colors.normalColor = isInteractable ? Color.white : Color.grey;
            button.colors = colors;
        }
    private void HideCropSpecificUI()
    {
        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.interactable = false;
            UpdateButtonVisual(plantSunflowerButton, false, "Sunflower");
        }
        if (plantWheatButton != null)
        {
            plantWheatButton.interactable = false;
            UpdateButtonVisual(plantWheatButton, false, "Wheat");
        }
        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.interactable = false;
            UpdateButtonVisual(plantCarrotsButton, false, "Carrots");
        }
        if (harvestButton != null)
        {
            harvestButton.interactable = false;
            UpdateButtonVisual(harvestButton, false, "Harvest");
        }
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(false);
        }
        if (cropStatusText != null)
        {
            cropStatusText.text = "No crop structure";
            cropStatusText.color = Color.red;
        }
    }
}
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

        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        if (plantSunflowerButton != null)
            plantSunflowerButton.onClick.AddListener(() => plantCrop(0));
        else
            Debug.LogWarning("Plant Sunflower Button is not assigned!");

        if (plantWheatButton != null)
            plantWheatButton.onClick.AddListener(() => plantCrop(1));
        else
            Debug.LogWarning("Plant Wheat Button is not assigned!");

        if (plantCarrotsButton != null)
            plantCarrotsButton.onClick.AddListener(() => plantCrop(2));
        else
            Debug.LogWarning("Plant Carrots Button is not assigned!");

        if (harvestButton != null)
            harvestButton.onClick.AddListener(harvestCrops);
        else
            Debug.LogWarning("Harvest Button is not assigned!");

        if (plantingClose != null)
            plantingClose.GetComponent<Button>()?.onClick.AddListener(closeSelectCropPanel);
        else
            Debug.LogWarning("Planting Close Button is not assigned!");
    }

    public void ShowSelectCropPanel()
    {
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(true);
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("Select Crop Panel is not assigned!");
        }
    }

    public void closeSelectCropPanel()
    {
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(false);
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
        if (!isCropStructure || cropStructure == null)
        {
            HideCropSpecificUI();
            return;
        }

        // Update crop status text
        if (cropStatusText != null)
        {
            string status = cropStructure.CurrentCropType == CropStructure.CropType.None
                ? "No crop planted"
                : $"{cropStructure.CurrentCropType} - {(cropStructure.CropReady ? "Ready to Harvest" : $"Growing (Progress: {cropStructure.GrowthProgress:F1})")}";
            cropStatusText.text = status;
        }

        // Show/hide harvest button based on crop readiness
        if (harvestButton != null)
        {
            harvestButton.gameObject.SetActive(cropStructure.CropReady);
        }

        // Show/hide plant buttons based on whether a crop is growing
        bool canPlant = !cropStructure.CropReady && !cropStructure.IsGrowing;
        if (plantSunflowerButton != null) plantSunflowerButton.gameObject.SetActive(canPlant);
        if (plantWheatButton != null) plantWheatButton.gameObject.SetActive(canPlant);
        if (plantCarrotsButton != null) plantCarrotsButton.gameObject.SetActive(canPlant);
    }

    private void HideCropSpecificUI()
    {
        if (plantSunflowerButton != null) plantSunflowerButton.gameObject.SetActive(false);
        if (plantWheatButton != null) plantWheatButton.gameObject.SetActive(false);
        if (plantCarrotsButton != null) plantCarrotsButton.gameObject.SetActive(false);
        if (harvestButton != null) harvestButton.gameObject.SetActive(false);
        if (selectCropPanel != null) selectCropPanel.SetActive(false);
        if (cropStatusText != null) cropStatusText.text = "No crop structure";
    }
}
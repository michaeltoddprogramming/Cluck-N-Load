using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CropStructureUI : BaseStructureUI
{
    [Header("UI Elements")]
    [SerializeField] private Button plantSunflowerButton;
    [SerializeField] private Button plantWheatButton;
    [SerializeField] private Button plantCarrotsButton;
    [SerializeField] private Button harvestButton;
    [SerializeField] private Button plantButton;
    [SerializeField] private GameObject selectCropPanel;
    [SerializeField] private GameObject plantingClose;
    [SerializeField] private TextMeshProUGUI statusText;
    // [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Image CropImage;
    [SerializeField] private Sprite SunflowerIcon;
    [SerializeField] private Sprite WheatIcon;
    [SerializeField] private Sprite carrotIcon;
    [SerializeField] private Sprite defaultIcon;

    [Header("Audio")]
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip plantSound;
    [SerializeField] private AudioClip harvestSound;
    [SerializeField] private float soundVolume = 0.7f;

    private AudioSource audioSource;
    private float lastErrorSoundTime;
    private CropStructure cropStructure;
    private bool isCropStructure;
    private char currCrop = 'N';

    public override void Initialize(Structure structure)
    {
        // statusText.gameObject.SetActive(false);
        plantButton.interactable = true;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
        base.Initialize(structure);

        isCropStructure = structure is CropStructure;
        if (isCropStructure) cropStructure = (CropStructure)structure;
        else
        {
            HideCropSpecificUI();
            return;
        }

        selectCropPanel?.SetActive(false);
        plantButton.interactable = true;
        nightManager = nightManager ?? NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        plantButton?.onClick.AddListener(() => { if (CanPlantCrops()) ShowSelectCropPanel(); else PlayErrorSound(); });
        plantSunflowerButton?.onClick.AddListener(() => TryPlantCrop(0));
        plantWheatButton?.onClick.AddListener(() => TryPlantCrop(1));
        plantCarrotsButton?.onClick.AddListener(() => TryPlantCrop(2));
        harvestButton?.onClick.AddListener(() => { if (CanHarvestCrops()) harvestCrops(); else PlayErrorSound(); });
        plantingClose?.GetComponent<Button>()?.onClick.AddListener(closeSelectCropPanel);
    }

    private void TryPlantCrop(int index)
    {
        if (!CanPlantCrops()) { PlayErrorSound(); return; }
        plantCrop(index);
    }

    private bool CanPlantCrops() => isCropStructure && cropStructure != null && nightManager?.IsDay == true && !cropStructure.IsGrowing && !cropStructure.CropReady;

    private bool CanHarvestCrops() => isCropStructure && cropStructure != null && nightManager?.IsDay == true && cropStructure.CropReady;

    public void ShowSelectCropPanel()
    {
        if (selectCropPanel != null && CanPlantCrops())
        {
            selectCropPanel.SetActive(true);
            plantButton.interactable = false;
            UpdateUI();
        }
        else
        {
            PlayErrorSound();
            closeSelectCropPanel();
        }
    }

    public void closeSelectCropPanel()
    {
        selectCropPanel?.SetActive(false);
        UpdateUI();
    }

    public void plantCrop(int crop)
    {
        if (!isCropStructure || cropStructure == null || nightManager?.IsDay != true)
        {
            PlayErrorSound();
            closeSelectCropPanel();
            return;
        }
        CropStructure.CropType cropType = crop switch
        {
            0 => CropStructure.CropType.Sunflower,
            1 => CropStructure.CropType.Wheat,
            2 => CropStructure.CropType.Carrots,
            _ => CropStructure.CropType.None
        };
        if (cropType == CropStructure.CropType.None)
        {
            PlayErrorSound();
            return;
        }
        cropStructure.Plant(cropType);
        PlaySound(plantSound);
        plantButton.interactable = false;
        closeSelectCropPanel();
        // Instant growth is now handled directly by CropStructure, not UI
        // else StartCoroutine(CloseUIAfterDelay(0.2f));
    }

    // Instant growth is now handled directly by CropStructure, not UI

    private IEnumerator CloseUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StructureUIManager.Instance?.HideStructureUI();
    }

    public void harvestCrops()
    {
        if (!isCropStructure || cropStructure == null) return;

        // Capture crop info BEFORE harvesting (since Harvest() resets CurrentCropType to None)
        CropStructure.CropType cropType = cropStructure.CurrentCropType;
        string cropName = cropType.ToString();
        if (string.IsNullOrEmpty(cropName) || cropName == "None")
        {
            cropName = "Crop"; // Fallback for uninitialized crop types
        }

        // Get singular form for proper pluralization
        string cropNameSingular = GetCropNameSingular(cropType);

        // Use the public properties instead of private fields
        int cropAmount = Mathf.RoundToInt(cropStructure.BaseCropHarvestAmount * cropStructure.CropHarvestMultiplier);
        int moneyGained = CalculateMoneyGain(cropType, cropAmount);

        string result = cropStructure.Harvest();

        switch (result)
        {
            case "space":
                statusText.color = Color.yellow;
                statusText.text = "Harvest unsuccessful: No space in silo";
                PlayErrorSound();
                // NEW: Show notification for silo space error
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowError("Silo Full!", "Build more silos or sell existing crops");
                }
                break;
            case "yes":
                statusText.color = Color.green;
                statusText.text = $"Harvest successful! +${moneyGained}";
                PlaySound(harvestSound);
                // NEW: Show success notification for harvest with boost info
                if (NotificationManager.Instance != null)
                {
                    // Handle plurals properly for crop names
                    string cropDisplayName = cropAmount == 1 ? cropNameSingular : cropName;
                    string baseMessage = $"{cropAmount} {cropDisplayName} harvested";
                    
                    Debug.Log($"[Crop Harvest] cropName: {cropName}, cropAmount: {cropAmount}, cropDisplayName: {cropDisplayName}, baseMessage: {baseMessage}");
                    
                    // Check if harvest multiplier is active
                    if (cropStructure.CropHarvestMultiplier > 1f)
                    {
                        int bonusPercent = Mathf.RoundToInt((cropStructure.CropHarvestMultiplier - 1f) * 100f);
                        baseMessage += $" • +{bonusPercent}% silo bonus!";
                        NotificationManager.ShowAchievement($"{cropName} Boosted!", baseMessage, 3f);
                    }
                    else
                    {
                        // NEW: Show error when no harvest boost is active - show harvest and missed potential
                        // Calculate what they could have earned with silo bonus (assuming 50% bonus)
                        int potentialCropAmount = Mathf.RoundToInt(cropAmount * 1.5f);
                        int missedCrops = potentialCropAmount - cropAmount;
                        string missedDisplayName = missedCrops == 1 ? GetCropNameSingular(cropType) : cropName;
                        NotificationManager.ShowError($"{cropName} No Boost!", $"{cropAmount} {cropDisplayName} harvested • Missing +{missedCrops} {missedDisplayName}!", 3f);
                    }
                }
                break;
            case "ready":
                statusText.color = Color.yellow;
                statusText.text = "Harvest unsuccessful: Crop not ready";
                PlayErrorSound();
                // NEW: Show warning for not ready
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowWarning("Not Ready Yet!", "Crops need more time to grow");
                }
                break;
            default:
                statusText.color = Color.yellow;
                statusText.text = "Harvest unsuccessful: Unknown issue";
                PlayErrorSound();
                // NEW: Show error for unknown issues
                if (NotificationManager.Instance != null)
                {
                    NotificationManager.ShowError("Harvest Failed!", "Something went wrong");
                }
                break;
        }
        plantButton.interactable = true;
        UpdateUI();
    }

    private int CalculateMoneyGain(CropStructure.CropType cropType, int amount)
    {
        int baseValue = cropType switch
        {
            CropStructure.CropType.Sunflower => 15,
            CropStructure.CropType.Wheat => 12,
            CropStructure.CropType.Carrots => 18,
            _ => 10
        };

        return amount * baseValue;
    }

    private void Update()
    {
        // Call base update to handle move button logic
        base.Update();
        UpdateUI();
    }

    private void UpdateUI()
    {
        // UpdateHealthBar();
        statusText.text = "slieduhrfehgfsiuedhfiusehfiuhref";
        setCropImage();
        if (!isCropStructure || cropStructure == null || nightManager == null)
        {
            HideCropSpecificUI();
            return;
        }
        bool isGrowing = cropStructure.IsGrowing;
        bool cropReady = cropStructure.CropReady;
        bool canPlant = nightManager.IsDay && !isGrowing && !cropReady;

        if (!canPlant)
        {
            plantButton.interactable = false;
        }
        else
        {
            plantButton.interactable = true;
        }


        if (statusText != null)
        {
            statusText.text = cropReady ? $"{cropStructure.CurrentCropType}: Ready to harvest!" : isGrowing ? "Growing..." : nightManager.IsDay ? "No crop planted" : "Cannot plant at night";
            statusText.color = cropReady ? Color.green : isGrowing ? Color.yellow : nightManager.IsDay ? Color.white : Color.yellow;
        }
        plantSunflowerButton.interactable = canPlant;
        plantWheatButton.interactable = canPlant;
        plantCarrotsButton.interactable = canPlant;
        harvestButton.interactable = cropReady;
        selectCropPanel.SetActive(canPlant && selectCropPanel.activeSelf);
    }

    private void setCropImage()
    {
        currCrop = cropStructure.GetCurrCrop();
        CropImage.sprite = currCrop switch
        {
            'S' => SunflowerIcon,
            'W' => WheatIcon,
            'C' => carrotIcon,
            _ => defaultIcon
        };
    }

    private void HideCropSpecificUI()
    {
        plantSunflowerButton.interactable = false;
        plantWheatButton.interactable = false;
        plantCarrotsButton.interactable = false;
        harvestButton.interactable = false;
        selectCropPanel?.SetActive(false);
        statusText.text = "No crop structure";
        statusText.color = Color.yellow;
    }

    private void PlayErrorSound()
    {
        if (Time.time - lastErrorSoundTime < 0.5f) return;
        lastErrorSoundTime = Time.time;
        audioSource?.PlayOneShot(errorSound);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null) audioSource.PlayOneShot(clip);
    }

    private string GetCropNameSingular(CropStructure.CropType cropType)
    {
        return cropType switch
        {
            CropStructure.CropType.Sunflower => "Sunflower",
            CropStructure.CropType.Wheat => "Wheat",
            CropStructure.CropType.Carrots => "Carrot",
            _ => "Crop"
        };
    }
}
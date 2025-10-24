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
    [SerializeField] private Slider cropGrowthBar;

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

    // public UIHoverManager hoverManager;
    // private float displayedGrowth = 0f; // Unused field

    private new void Start()
    {
        base.Start();
        // hoverManager = FindObjectOfType<UIHoverManager>();
    }
    
    // UI update optimization
    private const float UI_UPDATE_INTERVAL = 0.5f; // Update UI twice per second
    private float lastUIUpdate;

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

    // private bool CanPlantCrops() => isCropStructure && cropStructure != null && nightManager?.IsDay == true && !cropStructure.IsGrowing && !cropStructure.CropReady;
    private bool CanPlantCrops() => isCropStructure && cropStructure != null && nightManager?.IsDay == true && !cropStructure.CropReady;

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
        if (!isCropStructure || cropStructure == null || nightManager?.IsDay != true || nightManager.getIsPaused())
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
                        NotificationManager.ShowAchievement($"{cropName} Boosted!", baseMessage, 2f);
                    }
                    else
                    {
                        // NEW: Show error when no harvest boost is active - show harvest and missed potential
                        // Calculate what they could have earned with silo bonus (assuming 50% bonus)
                        int potentialCropAmount = Mathf.RoundToInt(cropAmount * 1.5f);
                        int missedCrops = potentialCropAmount - cropAmount;
                        string missedDisplayName = missedCrops == 1 ? GetCropNameSingular(cropType) : cropName;
                        NotificationManager.ShowError($"{cropName} No Boost!", $"{cropAmount} {cropDisplayName} harvested • Missing +{missedCrops} {missedDisplayName}!", 2f);
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

    protected override void Update()
    {
        // Call base update to handle move button logic
        base.Update();
        
        // Regular UI updates at intervals
        if (Time.time - lastUIUpdate > UI_UPDATE_INTERVAL)
        {
            UpdateUI();
            lastUIUpdate = Time.time;
        }

        if (cropStructure == null || nightManager == null || cropGrowthBar == null)
    {
        if (cropGrowthBar != null)
        {
            cropGrowthBar.value = 0f;
            cropGrowthBar.gameObject.SetActive(false);
        }
        return;
    }

    // Let the crop handle its growth
    cropStructure.TrackGrowth(nightManager);

    if (cropStructure.CropReady)
    {
        cropGrowthBar.value = 1f;
        cropGrowthBar.gameObject.SetActive(true);
    }
    else if (cropStructure.IsGrowing)
    {
        cropGrowthBar.value = cropStructure.GetGrowthProgress();
        cropGrowthBar.gameObject.SetActive(true);
    }
    else
    {
        cropGrowthBar.value = 0f;
        cropGrowthBar.gameObject.SetActive(false);
    }


        //this does work
        //  if (cropStructure == null || nightManager == null)
        // {
        //     if (cropGrowthBar != null)
        //     {
        //         cropGrowthBar.value = 0f;
        //         cropGrowthBar.gameObject.SetActive(false);
        //     }
        //     return;
        // }

        // bool isGrowing = cropStructure.IsGrowing;
        // bool cropReady = cropStructure.CropReady;

        // // Detect when crop is newly planted
        // if (isGrowing && !wasGrowing)
        // {
        //     // Crop just got planted - record the time
        //     plantedAtHour = nightManager.Hours + (nightManager.Minutes / 60f);
        //     cropGrowthProgress = 0f;
        // }

        // // Reset when crop is harvested or no longer growing
        // if (!isGrowing && wasGrowing)
        // {
        //     plantedAtHour = -1f;
        //     cropGrowthProgress = 0f;
        // }

        // wasGrowing = isGrowing;

        // // Update the progress bar
        // if (cropGrowthBar != null)
        // {
        //     if (cropReady)
        //     {
        //         // Crop is ready - show full bar
        //         cropGrowthBar.value = 1f;
        //         cropGrowthBar.gameObject.SetActive(true);
        //     }
        //     else if (isGrowing && plantedAtHour >= 0)
        //     {
        //         // Calculate current hour
        //         float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
                
        //         // Calculate hours elapsed since planting
        //         float hoursElapsed;
        //         if (currentHour >= plantedAtHour)
        //         {
        //             hoursElapsed = currentHour - plantedAtHour;
        //         }
        //         else
        //         {
        //             // Handle day wrap (planted late, now early next day)
        //             hoursElapsed = (24f - plantedAtHour) + currentHour;
        //         }

        //         // Crops grow from planting until 5 AM (start of day)
        //         // Calculate total hours needed based on when planted
        //         float targetHour = 5f; // 5 AM when day starts
        //         float totalHoursNeeded;
                
        //         if (plantedAtHour <= targetHour)
        //         {
        //             // Planted in morning, needs to grow until next morning
        //             totalHoursNeeded = (24f - plantedAtHour) + targetHour;
        //         }
        //         else
        //         {
        //             // Planted in afternoon/evening, grows until next morning
        //             totalHoursNeeded = (24f - plantedAtHour) + targetHour;
        //         }

        //         // Calculate progress as percentage
        //         float growthFraction = Mathf.Clamp01(hoursElapsed / totalHoursNeeded);
        //         cropGrowthBar.value = growthFraction;
        //         cropGrowthBar.gameObject.SetActive(true);
        //     }
        //     else
        //     {
        //         // No crop or not growing
        //         cropGrowthBar.value = 0f;
        //         cropGrowthBar.gameObject.SetActive(false);
        //     }
        // }
    }

    private void UpdateUI()
    {
        setCropImage();
        if (!isCropStructure || cropStructure == null || nightManager == null)
        {
            HideCropSpecificUI();
            return;
        }
        bool isGrowing = cropStructure.IsGrowing;
        bool cropReady = cropStructure.CropReady;
        bool isPaused = nightManager.getIsPaused();
        bool canPlant = nightManager.IsDay && !isPaused && !cropReady;

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
            statusText.text = cropReady ? $"{cropStructure.CurrentCropType}: Ready to harvest!" : isGrowing ? "Growing..." : isPaused ? "Game paused" : nightManager.IsDay ? "No crop planted" : "Cannot plant at night";
            statusText.color = cropReady ? Color.green : isGrowing ? Color.yellow : nightManager.IsDay ? Color.white : Color.yellow;
        }
        plantSunflowerButton.interactable = canPlant;
        plantWheatButton.interactable = canPlant;
        plantCarrotsButton.interactable = canPlant;
        harvestButton.interactable = cropReady && !isPaused;
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

    public void OnButtonHoverEnter(GameObject button)
    {
        // Debug.Log("Hover enter on button: " + button.name);
        if(hoverManager != null)
        {
            //when it is planed but not ready for harvest
            if(button == harvestButton.gameObject && cropStructure.IsGrowing && !cropStructure.CropReady)
            {
                hoverManager.ShowHover(harvestButton, "Patience, Farmer!", "Give it more time to grow!", true, new Vector2(-200, 0), cropGrowthBar.gameObject);
            }
            //when they have not planted yet
            else if(button == harvestButton.gameObject && !cropStructure.IsGrowing && !cropStructure.CropReady)
            {
                hoverManager.ShowHover(harvestButton, "Barren Land!", "Plant something first!", true, new Vector2(200, 0), plantButton.gameObject);
            }
            //when it is night and they can not plant
            else if(button == plantButton.gameObject && !nightManager.IsDay)
            {
                hoverManager.ShowHover(harvestButton, "Night Shift!", $"Farmer is asleep, try in the morning!", true, new Vector2(200, 0));
            }
            //when they try to move at night 
            else if(button == moveButton.gameObject && !nightManager.IsDay)
            {
                hoverManager.ShowHover(moveButton, "Sleeping!", $"Buildings can’t be moved at night!", true, new Vector2(200, 0));
            }
        }
    }

    public void OnButtonHoverExit()
    {
        if(hoverManager != null)
        {
            hoverManager.HideHover();
        }
    }

    public void OnButtonClick(GameObject button)
    {
        //when it is planed but not ready for harvest
        if(button == harvestButton.gameObject && cropStructure.IsGrowing && !cropStructure.CropReady)
        {
            hoverManager.PlayErrorFeedback(false, harvestButton);
        }
        //when they have not planted yet
        else if(button == harvestButton.gameObject && !cropStructure.IsGrowing &&!cropStructure.CropReady)
        {
            hoverManager.PlayErrorFeedback(false, harvestButton);
        }
        //when it is night and they can not plant
        else if(button == plantButton.gameObject && !nightManager.IsDay)
        {
            hoverManager.PlayErrorFeedback(false, plantButton);
        }
        //when they try to move at night 
        else if(button == moveButton.gameObject && !nightManager.IsDay)
        {
            hoverManager.PlayErrorFeedback(false, moveButton);
        }
    }
}
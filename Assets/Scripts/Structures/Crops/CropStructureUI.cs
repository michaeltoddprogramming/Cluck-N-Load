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
    [SerializeField] private TextMeshProUGUI cropStatusText;
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private Image CropImage;
    [SerializeField] private Sprite SunflowerIcon;
    [SerializeField] private Sprite WheatIcon;
    [SerializeField] private Sprite carrotIcon;
    [SerializeField] private Sprite defaultIcon;

    [Header("Dependencies")]
    [SerializeField] private NightManager nightManager;

    // Add these fields for audio
    [Header("Audio")]
    [SerializeField] private AudioClip errorSound;
    [SerializeField] private AudioClip plantSound;
    [SerializeField] private AudioClip harvestSound;
    [SerializeField] private float soundVolume = 0.7f;
    private AudioSource audioSource;
    private float lastErrorSoundTime;

    private CropStructure cropStructure;
    private bool isCropStructure = false;

    private char currCrop = 'N';

    public override void Initialize(Structure structure)
    {

        //make notification invisible
        notificationText.gameObject.SetActive(false);
        plantButton.interactable = true;

        // Setup audio source
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;

        base.Initialize(structure);

        isCropStructure = structure is CropStructure;

        if (isCropStructure)
        {
            cropStructure = (CropStructure)structure;
        }
        else
        {
            HideCropSpecificUI();
            return;
        }

        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(false);
            plantButton.interactable = true;
        }
        else
        {
        }

        if (cropStatusText == null)
        {
        }

        if (nightManager == null)
        {
            nightManager = NightManager.Instance;
            if (nightManager == null)
            {
                nightManager = FindFirstObjectByType<NightManager>();
                if (nightManager == null)
                {
                    Debug.LogError("NightManager not found in scene!");
                }
            }
        }

        SetupButtonListeners();
        UpdateUI();
    }

    private void Update()
    {
        UpdateUI();
    }

    // Add this method to play error sounds
    private void PlayErrorSound()
    {
        // Prevent sound spam by adding cooldown
        if (Time.time - lastErrorSoundTime < 0.5f)
            return;

        lastErrorSoundTime = Time.time;

        if (errorSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(errorSound);
        }
    }

    // Add method to play success sounds
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void SetupButtonListeners()
    {
        if (plantButton != null)
        {
            plantButton.onClick.RemoveAllListeners();
            plantButton.onClick.AddListener(() =>
            {
                // Check conditions before planting
                if (!CanPlantCrops())
                {
                    PlayErrorSound();
                    return;
                }
                ShowSelectCropPanel();
            });
        }
        else
        {
        }



        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.onClick.RemoveAllListeners();
            plantSunflowerButton.onClick.AddListener(() =>
            {
                // Check conditions before planting
                if (!CanPlantCrops())
                {
                    PlayErrorSound();
                    return;
                }
                plantCrop(0);
            });
        }
        else
        {
        }

        if (plantWheatButton != null)
        {
            plantWheatButton.onClick.RemoveAllListeners();
            plantWheatButton.onClick.AddListener(() =>
            {
                // Check conditions before planting
                if (!CanPlantCrops())
                {
                    PlayErrorSound();
                    return;
                }
                plantCrop(1);
            });
        }
        else
        {
        }

        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.onClick.RemoveAllListeners();
            plantCarrotsButton.onClick.AddListener(() =>
            {
                // Check conditions before planting
                if (!CanPlantCrops())
                {
                    PlayErrorSound();
                    return;
                }
                plantCrop(2);
            });
        }
        else
        {
        }

        if (harvestButton != null)
        {
            harvestButton.onClick.RemoveAllListeners();
            harvestButton.onClick.AddListener(() =>
            {
                // Check conditions before harvesting
                if (!CanHarvestCrops())
                {
                    PlayErrorSound();
                    return;
                }
                harvestCrops();
            });
        }
        else
        {
        }

        if (plantingClose != null)
        {
            plantingClose.GetComponent<Button>()?.onClick.RemoveAllListeners();
            plantingClose.GetComponent<Button>()?.onClick.AddListener(closeSelectCropPanel);
        }
        else
        {
        }
    }

    // Add helper method to check if crops can be planted
    private bool CanPlantCrops()
    {
        if (!isCropStructure || cropStructure == null)
            return false;

        if (nightManager == null || !nightManager.IsDay)
            return false;

        if (cropStructure.IsGrowing || cropStructure.CropReady)
            return false;

        return true;
    }

    // Add helper method to check if crops can be harvested
    private bool CanHarvestCrops()
    {
        if (!isCropStructure || cropStructure == null)
            return false;

        if (nightManager == null || !nightManager.IsDay)
            return false;

        if (!cropStructure.CropReady)
            return false;

        return true;
    }

    public void ShowSelectCropPanel()
    {
        bool canShowPanel = selectCropPanel != null &&
                           nightManager != null &&
                           nightManager.IsDay &&
                           cropStructure != null &&
                           !cropStructure.IsGrowing &&
                           !cropStructure.CropReady;

        if (canShowPanel)
        {
            selectCropPanel.SetActive(true);
            plantButton.interactable = false;
            UpdateUI();
            Debug.Log("Select crop panel shown=================================================================");
        }
        else
        {
            PlayErrorSound();
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
        }
        else
        {
        }
    }

    public void plantCrop(int crop)
    {
        if (!isCropStructure || cropStructure == null)
        {
            PlayErrorSound();
            return;
        }

        if (nightManager == null || !nightManager.IsDay)
        {
            Debug.LogWarning("Cannot plant crop: Planting is only allowed during the day!");
            PlayErrorSound();
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
                PlayErrorSound();
                return;
        }

        cropStructure.Plant(cropType);
        PlaySound(plantSound); // Play planting sound on success
        closeSelectCropPanel();

        // Close the entire structure UI after a short delay
        StartCoroutine(CloseUIAfterDelay(0.2f));
    }

    // Add this coroutine method to close the UI after a delay
    private IEnumerator CloseUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Use StructureUIManager to close the UI
        if (StructureUIManager.Instance != null)
        {
            StructureUIManager.Instance.HideStructureUI();
        }
    }

    public void harvestCrops()
    {
        if (isCropStructure && cropStructure != null)
        {
            string answer = cropStructure.Harvest();

            switch (answer)
            {
                case "space":
                    notificationText.gameObject.SetActive(true);
                    notificationText.color = Color.red;
                    notificationText.text = "Harvest unsuccessful: No space in silo";
                    PlayErrorSound();
                    break;
                case "yes":
                    notificationText.gameObject.SetActive(true);
                    notificationText.color = Color.green;
                    notificationText.text = "Harvest successful";
                    PlaySound(harvestSound); // Play harvest sound on success
                    break;
                case "ready":
                    notificationText.gameObject.SetActive(true);
                    notificationText.color = Color.red;
                    notificationText.text = "Harvest unsuccessful: Crop not ready";
                    PlayErrorSound();
                    break;
                default:
                    Debug.LogWarning($"Unexpected response from harvest: {answer}");
                    notificationText.gameObject.SetActive(true);
                    notificationText.color = Color.red;
                    notificationText.text = "Harvest unsuccessful: not sure why";
                    PlayErrorSound();
                    break;
            }
            UpdateUI();
        }
        else
        {
            Debug.LogWarning("HarvestCrops called but no valid CropStructure found.");
            PlayErrorSound();
        }
    }

    // Remove Update() method for better performance - using event-driven updates instead

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
                // float progress = cropStructure.GrowthProgress;
                // float totalTime = cropStructure.ProductionSettings.growthTime;
                // float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
                // float remainingHours = totalTime - progress;
                // float completionHour = (currentHour + remainingHours) % 24f;
                // int completionHourInt = Mathf.FloorToInt(completionHour);
                // int completionMinuteInt = Mathf.CeilToInt((completionHour - completionHourInt) * 60f);
                // if (completionMinuteInt == 60)
                // {
                // completionHourInt = (completionHourInt + 1) % 24;
                // completionMinuteInt = 0;
                // }
                // status = $"{cropStructure.CurrentCropType}: Growing...";
                status = $"Growing...";
                // cropStatusText.color = Color.yellow;
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
            UpdateButtonVisual(plantSunflowerButton, canPlant, "");
        }
        if (plantWheatButton != null)
        {
            plantWheatButton.interactable = canPlant;
            UpdateButtonVisual(plantWheatButton, canPlant, "");
        }
        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.interactable = canPlant;
            UpdateButtonVisual(plantCarrotsButton, canPlant, "");
        }
        if (harvestButton != null)
        {
            harvestButton.interactable = cropReady;
            UpdateButtonVisual(harvestButton, cropReady, "");
        }
        if (selectCropPanel != null)
        {
            selectCropPanel.SetActive(canPlant && selectCropPanel.activeSelf);
        }
    }

    private void UpdateButtonVisual(Button button, bool isInteractable, string action)
    {
        // Get the text component
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText == null) return;

        // Set appropriate text based on button state and type
        if (action.Equals("Harvest"))
        {
            if (isInteractable)
            {
                // buttonText.text = "";
            }
            else
            {
                // Similar to how BarracksStructureUI handles button text
                string reason = GetHarvestDisabledReason();
                // buttonText.text = $"Harvest\n<size=10>({reason})</size>";
            }
        }
        else
        {
            // Plant buttons
            if (isInteractable)
            {
                // buttonText.text = $"Plant {action}";
            }
            else
            {
                string reason = GetPlantDisabledReason();
                // buttonText.text = $"Plant {action}\n<size=10>({reason})</size>";
            }
        }

        // Set colors like the BarracksStructureUI does
        ColorBlock colors = button.colors;
        colors.normalColor = isInteractable ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;
    }

    // Helper methods to determine reasons
    private string GetHarvestDisabledReason()
    {
        if (nightManager != null && !nightManager.IsDay)
            return "Only During Day";
        if (cropStructure != null && cropStructure.IsGrowing)
            return "Still Growing";
        if (cropStructure != null && cropStructure.CurrentCropType == CropStructure.CropType.None)
            return "No Crop Planted";
        return "Not Ready";
    }

    private string GetPlantDisabledReason()
    {
        if (nightManager != null && !nightManager.IsDay)
            return "Only During Day";
        if (cropStructure != null && cropStructure.IsGrowing)
            return "Growing";
        if (cropStructure != null && cropStructure.CropReady)
            return "Harvest First";
        return "Unavailable";
    }

    private void setCropImage()
    {

        currCrop = cropStructure.GetCurrCrop();

        switch (currCrop)
        {
            case 'S':
                CropImage.sprite = SunflowerIcon;
                break;
            case 'W':
                CropImage.sprite = WheatIcon;
                break;
            case 'C':
                CropImage.sprite = carrotIcon;
                break;
            default:
                CropImage.sprite = defaultIcon; // No crop selected
                break;
        }
    }
    private void HideCropSpecificUI()
    {
        if (plantSunflowerButton != null)
        {
            plantSunflowerButton.interactable = false;
            UpdateButtonVisual(plantSunflowerButton, false, "");
        }
        if (plantWheatButton != null)
        {
            plantWheatButton.interactable = false;
            UpdateButtonVisual(plantWheatButton, false, "");
        }
        if (plantCarrotsButton != null)
        {
            plantCarrotsButton.interactable = false;
            UpdateButtonVisual(plantCarrotsButton, false, "");
        }
        if (harvestButton != null)
        {
            harvestButton.interactable = false;
            UpdateButtonVisual(harvestButton, false, "");
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

    public void checkStep5()
    {        
        TutorialManager.Instance.CheckStep5();        
    }
    public void checkHarvest()
    {        
        TutorialManager.Instance.CheckStep9();        
    }
}
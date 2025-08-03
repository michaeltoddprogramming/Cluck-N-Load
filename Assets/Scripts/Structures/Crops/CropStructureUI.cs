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
        notificationText.gameObject.SetActive(false);
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
        closeSelectCropPanel();
        if (TutorialManager.Instance?.IsTutorialActive() == true) StartCoroutine(DelayedInstantGrowForTutorial(0.5f));
        else StartCoroutine(CloseUIAfterDelay(0.2f));
    }

    private IEnumerator DelayedInstantGrowForTutorial(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (TutorialManager.Instance != null && cropStructure != null)
        {
            TutorialManager.Instance.Trigger(TutorialTrigger.PlantedCrop);
            yield return new WaitForSeconds(0.5f);
            cropStructure.ForceHarvestReadyForTutorial();
            UpdateUI();
            if (notificationText != null)
            {
                notificationText.gameObject.SetActive(true);
                notificationText.color = Color.green;
                notificationText.text = "Tutorial: Crop ready for harvest!";
            }
        }
    }

    private IEnumerator CloseUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        StructureUIManager.Instance?.HideStructureUI();
    }

    public void harvestCrops()
    {
        if (!isCropStructure || cropStructure == null) return;
        string result = cropStructure.Harvest();
        notificationText.gameObject.SetActive(true);
        switch (result)
        {
            case "space":
                notificationText.color = Color.red;
                notificationText.text = "Harvest unsuccessful: No space in silo";
                PlayErrorSound();
                break;
            case "yes":
                notificationText.color = Color.green;
                notificationText.text = "Harvest successful";
                PlaySound(harvestSound);
                break;
            case "ready":
                notificationText.color = Color.red;
                notificationText.text = "Harvest unsuccessful: Crop not ready";
                PlayErrorSound();
                break;
            default:
                notificationText.color = Color.red;
                notificationText.text = "Harvest unsuccessful: Unknown issue";
                PlayErrorSound();
                break;
        }
        UpdateUI();
    }

    private void Update() => UpdateUI();

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
        if (cropStatusText != null)
        {
            cropStatusText.text = cropReady ? $"{cropStructure.CurrentCropType}: Ready to harvest!" : isGrowing ? "Growing..." : nightManager.IsDay ? "No crop planted" : "Cannot plant at night";
            cropStatusText.color = cropReady ? Color.green : isGrowing ? Color.yellow : nightManager.IsDay ? Color.white : Color.red;
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
        cropStatusText.text = "No crop structure";
        cropStatusText.color = Color.red;
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
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalStructureUI : BaseStructureUI
{
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button buyAnimal;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button removeAnimal;
    [SerializeField] private TextMeshProUGUI animalAmountText;
    [SerializeField] private TextMeshProUGUI newAnimalAmount;
    [SerializeField] public Image animalIcon1;
    [SerializeField] public Image animalIcon2;
    [SerializeField] public Sprite cowIcon;
    [SerializeField] public Sprite chickenIcon;
    [SerializeField] public Sprite goatIcon;
    [SerializeField] public Sprite pigIcon;
    [SerializeField] public Sprite sheepIcon;
    [SerializeField] public AudioClip clickSound;

    private int newAnimalCount;
    private AnimalStructure animalStructure;
    private bool isAnimalStructure;
    private NightManager nightManager;

    private void Update() => UpdateUI();

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);
        isAnimalStructure = structure is AnimalStructure;
        if (isAnimalStructure)
        {
            animalStructure = (AnimalStructure)structure;
            animalStructure.OnAnimalCountChanged += UpdateUI;
            animalStructure.PlayBackgroundNoise();
        }
        nightManager = NightManager.Instance ?? FindFirstObjectByType<NightManager>();
        if (!isAnimalStructure)
        {
            HideAnimalSpecificUI();
            return;
        }
        SetupButtonListeners();
        UpdateUI();
    }

    private void SetupButtonListeners()
    {
        feedButton?.onClick.AddListener(() => { animalStructure.Feed(); UpdateUI(); });
        collectButton?.onClick.AddListener(() => { animalStructure.Collect(); UpdateUI(); });
        addAnimal?.onClick.AddListener(() => { animalChange(0); UpdateUI(); });
        removeAnimal?.onClick.AddListener(() => { animalChange(1); UpdateUI(); });
        buyAnimal?.onClick.AddListener(() => { BuyAnimals(); UpdateUI(); });
    }

    private void UpdateUI()
    {
        if (!isAnimalStructure || animalStructure == null || nightManager == null)
        {
            HideAnimalSpecificUI();
            return;
        }
        bool isProducing = animalStructure.IsProducing;
        bool productReady = animalStructure.ProductReady;
        int animalCount = animalStructure.AnimalCount;
        int maxAnimalCount = animalStructure.MaxAnimalCount;
        bool canFeed = nightManager.IsDay && !isProducing && !productReady && animalCount > 0;
        bool canCollect = productReady && nightManager.IsDay;
        bool canBuy = nightManager.IsDay && animalCount < maxAnimalCount;

        animalAmountText.text = $"{animalCount}/{maxAnimalCount}";
        newAnimalAmount.text = $"{newAnimalCount}";
        SetAnimalIcons();

        feedButton.interactable = canFeed;
        collectButton.interactable = canCollect;

        if (buyAnimal != null)
            buyAnimal.interactable = newAnimalCount > 0 && (newAnimalCount + animalCount) <= maxAnimalCount && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(newAnimalCount * animalStructure.ProductionSettings.costPerAnimal);

        addAnimal.interactable = (newAnimalCount + animalCount) < maxAnimalCount;
        removeAnimal.interactable = newAnimalCount > 0;

        if (progressBar != null && isProducing)
        {
            progressBar.gameObject.SetActive(true);
            progressBar.maxValue = animalStructure.ProductionSettings.productionTime;
            progressBar.value = animalStructure.ProductionProgress;
        }
        else if (progressBar != null) progressBar.gameObject.SetActive(false);

        UpdateStatusText(isProducing, productReady, animalCount);
    }

    private void SetAnimalIcons()
    {
        Sprite icon = animalStructure.GetAnimalType switch
        {
            AnimalStructure.AnimalType.Cow => cowIcon,
            AnimalStructure.AnimalType.Chicken => chickenIcon,
            AnimalStructure.AnimalType.Goat => goatIcon,
            AnimalStructure.AnimalType.Pig => pigIcon,
            AnimalStructure.AnimalType.Sheep => sheepIcon,
            _ => null
        };
        if (animalIcon1 != null) animalIcon1.sprite = icon;
        if (animalIcon2 != null) animalIcon2.sprite = icon;
    }

    private void UpdateStatusText(bool isProducing, bool productReady, int animalCount)
    {
        if (statusText != null)
        {
            if (productReady)
            {
                statusText.text = "Ready to collect!";
                statusText.color = Color.green;
            }
            else if (isProducing)
            {
                statusText.text = "Producing...";
                statusText.color = Color.white;
            }
            else if (animalCount <= 0)
            {
                statusText.text = "No animals!";
                statusText.color = Color.yellow;
            }
            else if (!nightManager.IsDay)
            {
                statusText.text = "Cannot feed at night";
                statusText.color = Color.yellow;
            }
            else
            {
                statusText.text = "Needs feeding";
                statusText.color = Color.white;
            }
        }
    }

    private void HideAnimalSpecificUI()
    {
        feedButton.interactable = false;
        collectButton.interactable = false;
        buyAnimal.interactable = false;
        addAnimal.interactable = false;
        removeAnimal.interactable = false;
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (statusText != null)
        {
            statusText.text = "Not an animal structure";
            statusText.color = Color.yellow;
        }
    }

    protected override void OnDestroy()
    {
        if (isAnimalStructure && animalStructure != null)
        {
            animalStructure.OnAnimalCountChanged -= UpdateUI;
            animalStructure.StopBackgroundNoise();
        }
        base.OnDestroy();
    }

    private void animalChange(int flag)
    {
        if (flag == 0) newAnimalCount++;
        else if (flag == 1 && newAnimalCount > 0) newAnimalCount--;
    }

    private void BuyAnimals()
    {
        if (newAnimalCount > 0 && MoneyManager.Instance != null)
        {
            animalStructure.BuyAnimals(newAnimalCount);
            newAnimalCount = 0;
        }
    }
}
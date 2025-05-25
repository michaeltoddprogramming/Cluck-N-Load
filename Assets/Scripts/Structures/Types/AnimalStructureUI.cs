using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalStructureUI : BaseStructureUI
{
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
    [SerializeField] private Button buyOneAnimalButton;
    [SerializeField] private Button buyFiveAnimalsButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private Slider progressBar;

    private AnimalStructure animalStructure;
    private bool isAnimalStructure = false;
    private NightManager nightManager;

    public override void Initialize(Structure structure)
    {
        base.Initialize(structure);

        isAnimalStructure = structure is AnimalStructure;
        if (isAnimalStructure)
        {
            animalStructure = (AnimalStructure)structure;
            animalStructure.OnAnimalCountChanged += UpdateUI;
            Debug.Log($"{structure.GetStructureName()} AnimalStructureUI: Subscribed to OnAnimalCountChanged");

            animalStructure.PlayBackgroundNoise();
        }

        nightManager = NightManager.Instance ?? FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError($"{structure.GetStructureName()} AnimalStructureUI: NightManager not found!");
        }

        if (!isAnimalStructure)
        {
            Debug.LogWarning($"AnimalStructureUI used with non-animal structure: {structure.GetType().Name}");
            HideAnimalSpecificUI();
            return;
        }

        animalStructure.PlayBackgroundNoise();
        SetupButtonListeners();
        UpdateUI();

        
    }

    // public void Start()
    // {
    //     animalStructure.PlayBackgroundNoise();
    // }

    private void SetupButtonListeners()
    {
        if (feedButton != null)
        {
            feedButton.onClick.RemoveAllListeners();
            feedButton.onClick.AddListener(() =>
            {
                animalStructure.Feed();
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Feed button not assigned in AnimalStructureUI prefab!");
        }

        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(() =>
            {
                animalStructure.Collect();
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Collect button not assigned in AnimalStructureUI prefab!");
        }

        if (buyOneAnimalButton != null)
        {
            buyOneAnimalButton.onClick.RemoveAllListeners();
            buyOneAnimalButton.onClick.AddListener(() =>
            {
                animalStructure.BuyAnimals(1);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Buy One Animal button not assigned in AnimalStructureUI prefab!");
        }

        if (buyFiveAnimalsButton != null)
        {
            buyFiveAnimalsButton.onClick.RemoveAllListeners();
            buyFiveAnimalsButton.onClick.AddListener(() =>
            {
                animalStructure.BuyAnimals(5);
                UpdateUI();
            });
        }
        else
        {
            Debug.LogWarning("Buy Five Animals button not assigned in AnimalStructureUI prefab!");
        }
    }

    protected override void Update()
    {
        base.Update();
        if (isAnimalStructure)
        {
            UpdateUI();
        }
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
        string structureName = $"{animalStructure.GetAnimalType} Structure";

        // Update buttons (BarracksStyle)
        if (feedButton != null)
        {
            feedButton.interactable = canFeed;
            UpdateButtonVisual(feedButton, canFeed, "Feed");
        }

        if (collectButton != null)
        {
            collectButton.interactable = canCollect;
            UpdateButtonVisual(collectButton, canCollect, "Collect");
        }

        if (buyOneAnimalButton != null)
        {
            buyOneAnimalButton.interactable = canBuy;
            UpdateButtonVisual(buyOneAnimalButton, canBuy, "Buy 1");
        }

        if (buyFiveAnimalsButton != null)
        {
            buyFiveAnimalsButton.interactable = canBuy;
            UpdateButtonVisual(buyFiveAnimalsButton, canBuy, "Buy 5");
        }

        // Update status text
        if (statusText != null)
        {
            string animalStatus = $"{structureName}: {animalCount}/{maxAnimalCount} {animalStructure.GetAnimalType}s";
            if (productReady)
            {
                statusText.text = $"{animalStatus}, Ready to collect!";
                statusText.color = nightManager.IsDay ? Color.green : Color.yellow;
                if (progressBar != null) progressBar.gameObject.SetActive(false);
            }
            else if (isProducing)
            {
                float progress = animalStructure.ProductionProgress;
                float totalTime = animalStructure.ProductionSettings.productionTime;
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
                statusText.text = $"{animalStatus}, Producing... (Ready at 05:00)";
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
                statusText.text = animalCount > 0 ? $"{animalStatus}, Needs feeding" : $"{animalStatus}, No animals!";
                statusText.color = animalCount > 0 ? (nightManager.IsDay ? Color.white : Color.red) : Color.red;
                if (progressBar != null) progressBar.gameObject.SetActive(false);
            }
        }
    }

    private void UpdateButtonVisual(Button button, bool isInteractable, string action)
    {
        TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        if (buttonText != null)
        {
            if (isInteractable)
            {
                buttonText.text = action;
            }
            else
            {
                string reason;
                if (action == "Feed")
                {
                    if (nightManager != null && !nightManager.IsDay)
                        reason = "Only During Day";
                    else if (animalStructure != null && animalStructure.IsProducing)
                        reason = "Already Fed";
                    else if (animalStructure != null && animalStructure.ProductReady)
                        reason = "Collect First";
                    else if (animalStructure != null && animalStructure.AnimalCount <= 0)
                        reason = "No Animals";
                    else if (animalStructure != null && InventoryManager.Instance != null)
                    {
                        string requiredFood = animalStructure.RequiredFood;
                        int requiredAmount = animalStructure.ProductionSettings.baseFoodRequired * animalStructure.AnimalCount;
                        if (!InventoryManager.Instance.HasItem(requiredFood, requiredAmount))
                            reason = $"Need {requiredAmount} {requiredFood}";
                        else
                            reason = "Unavailable";
                    }
                    else
                        reason = "Unavailable";
                    buttonText.text = $"{action}\n<size=9>({reason})</size>";
                }
                else if (action == "Collect")
                {
                    if (nightManager != null && !nightManager.IsDay)
                        reason = "Only During Day";
                    else if (animalStructure != null && !animalStructure.ProductReady)
                        reason = "Not Ready";
                    else
                        reason = "Unavailable";
                    buttonText.text = $"{action}\n<size=9>({reason})</size>";
                }
                else if (action.StartsWith("Buy"))
                {
                    if (nightManager != null && !nightManager.IsDay)
                        reason = "Only During Day";
                    else if (animalStructure != null && animalStructure.AnimalCount >= animalStructure.MaxAnimalCount)
                        reason = "Max Animals";
                    else if (animalStructure != null && MoneyManager.Instance != null)
                    {
                        int amount = action == "Buy 1" ? 1 : 5;
                        int totalCost = amount * animalStructure.ProductionSettings.costPerAnimal;
                        if (!MoneyManager.Instance.CanAfford(totalCost))
                            reason = $"Need {totalCost} Gold";
                        else
                            reason = "Unavailable";
                    }
                    else
                        reason = "Unavailable";
                    buttonText.text = $"{action}\n<size=9>({reason})</size>";
                }
                else
                {
                    buttonText.text = $"{action}\n<size=9>(Unavailable)</size>";
                }
            }
        }

        ColorBlock colors = button.colors;
        colors.normalColor = isInteractable ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        button.colors = colors;
    }

    private void HideAnimalSpecificUI()
    {
        if (feedButton != null)
        {
            feedButton.interactable = false;
            UpdateButtonVisual(feedButton, false, "Feed");
        }
        if (collectButton != null)
        {
            collectButton.interactable = false;
            UpdateButtonVisual(collectButton, false, "Collect");
        }
        if (buyOneAnimalButton != null)
        {
            buyOneAnimalButton.interactable = false;
            UpdateButtonVisual(buyOneAnimalButton, false, "Buy 1");
        }
        if (buyFiveAnimalsButton != null)
        {
            buyFiveAnimalsButton.interactable = false;
            UpdateButtonVisual(buyFiveAnimalsButton, false, "Buy 5");
        }
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (statusText != null)
        {
            statusText.text = "Not an animal structure";
            statusText.color = Color.red;
        }
    }

    private void OnDestroy()
    {
        if (isAnimalStructure && animalStructure != null)
        {
            animalStructure.OnAnimalCountChanged -= UpdateUI;
            animalStructure.StopBackgroundNoise();
        }        
    }
}
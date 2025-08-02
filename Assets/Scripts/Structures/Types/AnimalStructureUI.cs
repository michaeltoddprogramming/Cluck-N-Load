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
    [SerializeField] private Button addAnimal;
    [SerializeField] private Button removeAnimal;
    [SerializeField] private Button buyAnimal;
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

    private int newAnimalCount = 0;

    private AnimalStructure animalStructure;
    private bool isAnimalStructure = false;
    private NightManager nightManager;
    private MoneyManager moneyManager;

    // private bool fed = false;



    private float testProgressTimer = 0f;
    private float testFillDuration = 2f;
    private bool testFilling = false;

    private void Update()
    {
        if (testFilling && progressBar != null)
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
            // statusText.text = $"{animalStatus}, Producing... (Ready at 05:00)";
            // statusText.color = Color.yellow;
            if (progressBar != null)
            {
                progressBar.gameObject.SetActive(true);
                progressBar.maxValue = totalTime;
                progressBar.value = progress;
            }
        }

        UpdateUI();
    }

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
        if (nightManager == null)
        {
            Debug.LogError($"{structure.GetStructureName()} AnimalStructureUI: NightManager not found!");
        }

        if (!isAnimalStructure)
        {
            HideAnimalSpecificUI();
            return;
        }

        animalStructure.PlayBackgroundNoise();
        SetupButtonListeners();

        // Subscribe to day/night changes for UI updates
        if (nightManager != null)
        {
            // Note: You may need to add this event to NightManager
            // nightManager.OnDayNightChanged += UpdateUI;
        }

        UpdateUI();

        testProgressTimer = 0f;
        testFilling = true;
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
        }

        if (collectButton != null)
        {
            collectButton.onClick.RemoveAllListeners();
            collectButton.onClick.AddListener(() =>
            {
                animalStructure.Collect();

                // Notify tutorial system about product collection
                // TutorialConditionTracker tracker = FindFirstObjectByType<TutorialConditionTracker>();
                // if (tracker != null)
                // {
                //     tracker.OnProductCollected();
                // }

                // fed = false;
                UpdateUI();
            });
        }
        else
        {
        }
        if (addAnimal != null)
        {
            addAnimal.onClick.RemoveAllListeners();
            addAnimal.onClick.AddListener(() =>
            {
                animalChange(0);
                UpdateUI();
            });
        }
        else
        {
        }
        if (removeAnimal != null)
        {
            removeAnimal.onClick.RemoveAllListeners();
            removeAnimal.onClick.AddListener(() =>
            {
                animalChange(1);
                UpdateUI();
            });
        }
        else
        {
        }
        if (buyAnimal != null)
        {
            buyAnimal.onClick.RemoveAllListeners();
            buyAnimal.onClick.AddListener(() =>
            {
                BuyAnimals();
                UpdateUI();
            });
        }
        else
        {
        }
    }

    // Remove the Update method since we're using event-driven updates
    // This improves performance by not updating UI every frame

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

        //update animal count text
        if (animalAmountText != null)
        {
            animalAmountText.text = $"{animalCount}/{maxAnimalCount}";
            // animalAmountText.text = "piosaeurgof";
        }

        if (animalIcon1 != null)
        {
            if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Cow)
            {
                animalIcon1.sprite = cowIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Chicken)
            {
                animalIcon1.sprite = chickenIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Goat)
            {
                animalIcon1.sprite = goatIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Pig)
            {
                animalIcon1.sprite = pigIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Sheep)
            {
                animalIcon1.sprite = sheepIcon;
            }
        }

        if (animalIcon2 != null)
        {
            if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Cow)
            {
                animalIcon2.sprite = cowIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Chicken)
            {
                animalIcon2.sprite = chickenIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Goat)
            {
                animalIcon2.sprite = goatIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Pig)
            {
                animalIcon2.sprite = pigIcon;
            }
            else if (animalStructure.GetAnimalType == AnimalStructure.AnimalType.Sheep)
            {
                animalIcon2.sprite = sheepIcon;
            }
        }


        if (newAnimalAmount != null)
        {
            newAnimalAmount.text = $"{newAnimalCount}";
            // animalAmountText.text = "piosaeurgof";
        }

        // Update buttons (BarracksStyle)
        if (feedButton != null && canFeed && !isProducing && !productReady)
        {
            feedButton.interactable = true;
            // UpdateButtonVisual(feedButton, canFeed, "");
            // fed = true;
        }
        else if (feedButton != null && !canFeed)
        {
            feedButton.interactable = false;
            // UpdateButtonVisual(feedButton, false, "Feed");
        }

        if (collectButton != null && canCollect)
        {
            collectButton.interactable = true;
            // UpdateButtonVisual(collectButton, canCollect, "");
        }
        else if (collectButton != null && !canCollect)
        {
            collectButton.interactable = false;
            // UpdateButtonVisual(collectButton, false, "Collect");
        }

        if (buyAnimal != null)
        {
            Debug.Log($"new count: {newAnimalCount}, animal count: {animalCount}, max count: {maxAnimalCount}");
            if (newAnimalCount > 0 && (newAnimalCount + animalCount) <= maxAnimalCount && MoneyManager.Instance != null && MoneyManager.Instance.CanAfford(newAnimalCount * animalStructure.ProductionSettings.costPerAnimal))
            {
                buyAnimal.interactable = true;
            }
            else
            {
                buyAnimal.interactable = false;

                if ((newAnimalCount + animalCount) > maxAnimalCount)
                {
                    updateStatusText($"Cannot house more than {maxAnimalCount} animals!");
                }

                if (MoneyManager.Instance != null && !MoneyManager.Instance.CanAfford(newAnimalCount * animalStructure.ProductionSettings.costPerAnimal))
                {
                    updateStatusText($"Cannot afford {maxAnimalCount} many animals!");
                }
            }
        }

        if (addAnimal != null)
        {
            if ((newAnimalCount + animalCount) < maxAnimalCount)
            {
                addAnimal.interactable = true;
            }
            else
            {
                addAnimal.interactable = false;
            }
        }

        if (removeAnimal != null)
        {
            if (newAnimalCount > 0)
            {
                removeAnimal.interactable = true;
            }
            else
            {
                removeAnimal.interactable = false;
            }
        }


        // float progress = animalStructure.ProductionProgress;
        // float totalTime = animalStructure.ProductionSettings.productionTime;
        // float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
        // float remainingHours = totalTime - progress;
        // float completionHour = (currentHour + remainingHours) % 24f;
        // int completionHourInt = Mathf.FloorToInt(completionHour);
        // int completionMinuteInt = Mathf.CeilToInt((completionHour - completionHourInt) * 60f);
        // if (completionMinuteInt == 60)
        // {
        //     completionHourInt = (completionHourInt + 1) % 24;
        //     completionMinuteInt = 0;
        // }
        // // statusText.text = $"{animalStatus}, Producing... (Ready at 05:00)";
        // // statusText.color = Color.yellow;
        // if (progressBar != null)
        // {
        //     progressBar.gameObject.SetActive(true);
        //     progressBar.maxValue = totalTime;
        //     progressBar.value = progress;
        // }

        // if (buyOneAnimalButton != null)
        // {
        //     buyOneAnimalButton.interactable = canBuy;
        //     UpdateButtonVisual(buyOneAnimalButton, canBuy, "");
        // }

        // if (buyFiveAnimalsButton != null)
        // {
        //     buyFiveAnimalsButton.interactable = canBuy;
        //     UpdateButtonVisual(buyFiveAnimalsButton, canBuy, "");
        // }
    }

    private void UpdateButtonVisual(Button button, bool isInteractable, string action)
    {
        // TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
        // if (buttonText != null)
        // {
        //     if (isInteractable)
        //     {
        //         buttonText.text = action;
        //     }
        //     else
        //     {
        //         string reason;
        //         if (action == "Feed")
        //         {
        //             if (nightManager != null && !nightManager.IsDay)
        //                 reason = "Only During Day";
        //             else if (animalStructure != null && animalStructure.IsProducing)
        //                 reason = "Already Fed";
        //             else if (animalStructure != null && animalStructure.ProductReady)
        //                 reason = "Collect First";
        //             else if (animalStructure != null && animalStructure.AnimalCount <= 0)
        //                 reason = "No Animals";
        //             else if (animalStructure != null && InventoryManager.Instance != null)
        //             {
        //                 string requiredFood = animalStructure.RequiredFood;
        //                 int requiredAmount = animalStructure.ProductionSettings.baseFoodRequired * animalStructure.AnimalCount;
        //                 if (!InventoryManager.Instance.HasItem(requiredFood, requiredAmount))
        //                     reason = $"Need {requiredAmount} {requiredFood}";
        //                 else
        //                     reason = "Unavailable";
        //             }
        //             else
        //                 reason = "Unavailable";
        //             buttonText.text = $"{action}\n<size=9>({reason})</size>";
        //         }
        //         else if (action == "Collect")
        //         {
        //             if (nightManager != null && !nightManager.IsDay)
        //                 reason = "Only During Day";
        //             else if (animalStructure != null && !animalStructure.ProductReady)
        //                 reason = "Not Ready";
        //             else
        //                 reason = "Unavailable";
        //             buttonText.text = $"{action}\n<size=9>({reason})</size>";
        //         }
        //         else if (action.StartsWith("Buy"))
        //         {
        //             if (nightManager != null && !nightManager.IsDay)
        //                 reason = "Only During Day";
        //             else if (animalStructure != null && animalStructure.AnimalCount >= animalStructure.MaxAnimalCount)
        //                 reason = "Max Animals";
        //             else if (animalStructure != null && MoneyManager.Instance != null)
        //             {
        //                 int amount = action == "Buy 1" ? 1 : 5;
        //                 int totalCost = amount * animalStructure.ProductionSettings.costPerAnimal;
        //                 if (!MoneyManager.Instance.CanAfford(totalCost))
        //                     reason = $"Need {totalCost} Gold";
        //                 else
        //                     reason = "Unavailable";
        //             }
        //             else
        //                 reason = "Unavailable";
        //             buttonText.text = $"{action}\n<size=9>({reason})</size>";
        //         }
        //         else
        //         {
        //             buttonText.text = $"{action}\n<size=9>(Unavailable)</size>";
        //         }
        //     }
        // }

        // ColorBlock colors = button.colors;
        // colors.normalColor = isInteractable ? Color.white : new Color(0.7f, 0.7f, 0.7f);
        // colors.disabledColor = new Color(0.6f, 0.6f, 0.6f, 0.5f);
        // button.colors = colors;
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

    protected override void OnDestroy()
    {
        if (isAnimalStructure && animalStructure != null)
        {
            animalStructure.OnAnimalCountChanged -= UpdateUI;
            animalStructure.StopBackgroundNoise();
        }

        // Call base OnDestroy
        base.OnDestroy();
    }

    private void animalChange(int flag)
    {
        if (flag == 0)
        {
            newAnimalCount += 1;
        }
        else if (flag == 1 && newAnimalCount > 0)
        {
            newAnimalCount -= 1;
        }
    }

    private void BuyAnimals()
    {
        if (newAnimalCount > 0)
        {
            animalStructure.AddAnimals(newAnimalCount);
            newAnimalCount = 0;
        }
    }

    private void updateStatusText(string message)
    {
        // Update status text
        if (statusText != null)
        {
            string animalStatus = "";

            statusText.text = message;
            statusText.color = Color.red;



            // else
            // {
            //     // statusText.text = animalCount > 0 ? $"{animalStatus}, Needs feeding" : $"{animalStatus}, No animals!";
            //     // statusText.color = animalCount > 0 ? (nightManager.IsDay ? Color.white : Color.red) : Color.red;
            //     // if (progressBar != null) progressBar.gameObject.SetActive(false);
            // }
        }
    }
}
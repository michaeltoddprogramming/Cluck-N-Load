using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnimalStructureUI : BaseStructureUI
{
    [SerializeField] private Button feedButton;
    [SerializeField] private Button collectButton;
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
        }

        nightManager = FindObjectOfType<NightManager>();
        if (nightManager == null)
        {
            Debug.LogError("NightManager not found in the scene! AnimalStructureUI requires NightManager to function.");
        }

        if (!isAnimalStructure)
        {
            Debug.LogWarning($"AnimalStructureUI used with non-animal structure: {structure.GetType().Name}");
            HideAnimalSpecificUI();
            return;
        }

        UpdateUI();

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
            Debug.LogWarning("Feed button is not assigned in the AnimalStructureUI prefab!");
            if (feedButton != null) feedButton.gameObject.SetActive(false);
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
            Debug.LogWarning("Collect button is not assigned in the AnimalStructureUI prefab!");
            if (collectButton != null) collectButton.gameObject.SetActive(false);
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
            return;
        }

        bool isProducing = animalStructure.IsProducing;
        bool productReady = animalStructure.ProductReady;
        bool productionFinished = animalStructure.ProductionFinished;

        // Include animal type in the structure name
        string structureName = $"{animalStructure.GetAnimalType} Structure";

        if (feedButton != null)
            feedButton.gameObject.SetActive(!isProducing && !productReady && !productionFinished);

        if (collectButton != null)
            collectButton.gameObject.SetActive(productReady);

        if (statusText != null)
        {
            if (productReady)
            {
                statusText.text = $"{structureName}: Ready to collect!";
                statusText.color = Color.green;

                if (progressBar != null)
                    progressBar.gameObject.SetActive(false);
            }
            else if (productionFinished)
            {
                float currentHour = nightManager.Hours + (nightManager.Minutes / 60f);
                float hoursUntilNextDay = currentHour >= 5f ? (24f - currentHour + 5f) : (5f - currentHour);
                int wholeHours = Mathf.FloorToInt(hoursUntilNextDay);
                int minutes = Mathf.CeilToInt((hoursUntilNextDay - wholeHours) * 60f);
                if (minutes == 60)
                {
                    wholeHours += 1;
                    minutes = 0;
                }

                statusText.text = $"{structureName}: Waiting for new day (Done at 05:00, {wholeHours}h {minutes}m)";
                statusText.color = Color.yellow;

                if (progressBar != null)
                {
                    progressBar.gameObject.SetActive(true);
                    progressBar.maxValue = animalStructure.ProductionSettings.productionTime;
                    progressBar.value = animalStructure.ProductionSettings.productionTime;
                }
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

                statusText.text = $"{structureName}: Producing... (Finishes at {completionHourInt:D2}:{completionMinuteInt:D2})";
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
                statusText.text = $"{structureName}: Needs feeding";
                statusText.color = Color.white;

                if (progressBar != null)
                    progressBar.gameObject.SetActive(false);
            }
        }
    }

    private void HideAnimalSpecificUI()
    {
        if (feedButton != null) feedButton.gameObject.SetActive(false);
        if (collectButton != null) collectButton.gameObject.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (statusText != null)
        {
            statusText.text = "Not an animal structure";
            statusText.color = Color.red;
        }
    }
}
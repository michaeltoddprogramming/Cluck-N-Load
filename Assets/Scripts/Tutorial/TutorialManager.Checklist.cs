using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class TutorialManager
{
    [Header("Checklist UI")]
    public GameObject checklistPanel;
    public Transform checklistContainer;
    public GameObject checklistItemPrefab;

    private readonly Dictionary<string, string[]> categories = new Dictionary<string, string[]>
    {
        { "Farm Basics", new[] { "welcome", "camera_controls", "day_night_panel", "money_explanation", "time_controls", "season_bonuses" } },
        { "Building", new[] { "open_build_shop", "build_farmhouse", "build_crop_plot", "build_silo" } },
        { "Farming", new[] { "plant_first_crop", "harvest_first_crops" } },
        { "Animals", new[] { "build_chicken_coop", "build_chicken_barracks", "buy_chickens", "feed_chickens", "collect_eggs" } },
        { "Defense", new[] { "recruit_soldiers", "place_flag", "prepare_defense" } },
    };

    private List<string> completedStepIds = new List<string>();
    private string currentCategory = "Farm Basics";

    public void SetupChecklist()
    {
        if (checklistPanel != null)
            UpdateChecklistUI();
    }

    public void MarkStepComplete(string stepId)
    {
        if (string.IsNullOrEmpty(stepId) || completedStepIds.Contains(stepId))
            return;

        completedStepIds.Add(stepId);
        CheckCategoryProgress();
        UpdateChecklistUI();
        PlayCompletionFeedback();
    }

    private void CheckCategoryProgress()
    {
        if (!categories.TryGetValue(currentCategory, out string[] steps))
            return;

        bool categoryComplete = true;
        foreach (string step in steps)
            if (!completedStepIds.Contains(step))
            {
                categoryComplete = false;
                break;
            }

        if (categoryComplete)
            AdvanceToNextCategory();
    }

    private void AdvanceToNextCategory()
    {
        string[] allCategories = new string[categories.Count];
        categories.Keys.CopyTo(allCategories, 0);
        int currentIndex = System.Array.IndexOf(allCategories, currentCategory);
        if (currentIndex >= 0 && currentIndex < allCategories.Length - 1)
        {
            currentCategory = allCategories[currentIndex + 1];
            AnimateChecklistCategoryChange();
        }
    }

    private void UpdateChecklistUI()
    {
        foreach (Transform child in checklistContainer)
            Destroy(child.gameObject);

        AddCategoryHeader();
        AddCategorySteps();
    }

    private void AddCategoryHeader()
    {
        GameObject headerObj = Instantiate(checklistItemPrefab, checklistContainer);
        TextMeshProUGUI headerText = headerObj.GetComponentInChildren<TextMeshProUGUI>();
        if (headerText != null)
        {
            headerText.text = $"<b>{currentCategory}</b>";
            headerText.fontSize += 2;
            headerText.fontStyle = FontStyles.Bold;
            headerText.color = new Color(1f, 0.8f, 0.2f);
        }
        Toggle headerToggle = headerObj.GetComponentInChildren<Toggle>();
        if (headerToggle != null)
            headerToggle.gameObject.SetActive(false);
    }

    private void ApplyEnhancedToggleStyle(Toggle toggle, bool isCompleted)
    {
        if (toggle == null)
            return;

        toggle.isOn = isCompleted;
        toggle.interactable = false;
        Image checkmark = toggle.graphic as Image;
        if (checkmark != null)
        {
            checkmark.color = new Color(1f, 0.9f, 0.2f);
            if (isCompleted)
            {
                checkmark.transform.localScale = Vector3.zero;
                LeanTween.scale(checkmark.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack).setDelay(0.1f);
                LeanTween.rotateAroundLocal(checkmark.gameObject, Vector3.forward, 360f, 0.4f).setEase(LeanTweenType.easeOutCirc).setDelay(0.1f);
            }
        }
        Image background = toggle.GetComponent<Image>();
        if (background != null)
            background.color = isCompleted ? new Color(0.3f, 0.8f, 0.3f, 0.7f) : new Color(0.7f, 0.7f, 0.7f, 0.4f);
    }

    private void AddCategorySteps()
    {
        if (!categories.TryGetValue(currentCategory, out string[] stepIds))
            return;

        foreach (string stepId in stepIds)
        {
            TutorialStep step = steps.Find(s => s.stepId == stepId);
            if (step == null)
                continue;

            GameObject itemObj = Instantiate(checklistItemPrefab, checklistContainer);
            TextMeshProUGUI itemText = itemObj.GetComponentInChildren<TextMeshProUGUI>();
            if (itemText != null)
            {
                bool isCompleted = completedStepIds.Contains(stepId);
                itemText.text = step.title;
                itemText.color = isCompleted ? new Color(0.4f, 1f, 0.4f) : Color.white;
            }
            Toggle toggle = itemObj.GetComponentInChildren<Toggle>();
            if (toggle != null)
                ApplyEnhancedToggleStyle(toggle, completedStepIds.Contains(stepId));
        }
    }

    private void PlayCompletionFeedback()
    {
        if (keyPressSound != null)
            effectsAudioSource.PlayOneShot(keyPressSound, 0.7f);

        if (checklistPanel != null)
        {
            LeanTween.cancel(checklistPanel);
            LeanTween.scale(checklistPanel, Vector3.one * 1.05f, 0.2f).setEase(LeanTweenType.easeOutQuad).setOnComplete(() =>
            {
                LeanTween.scale(checklistPanel, Vector3.one, 0.3f).setEase(LeanTweenType.easeInOutQuad);
            });

            Image panelImage = checklistPanel.GetComponent<Image>();
            if (panelImage != null)
            {
                Color originalColor = panelImage.color;
                Color glowColor = new Color(1f, 1f, 0.7f, originalColor.a);
                LeanTween.value(checklistPanel, 0f, 1f, 0.2f).setOnUpdate((float val) =>
                {
                    panelImage.color = Color.Lerp(originalColor, glowColor, val);
                }).setLoopPingPong(1).setEase(LeanTweenType.easeInOutQuad);
            }
        }
        UpdateChecklistUI();
    }

    private void AnimateChecklistCategoryChange()
    {
        CanvasGroup canvasGroup = checklistPanel.GetComponent<CanvasGroup>() ?? checklistPanel.AddComponent<CanvasGroup>();
        LeanTween.cancel(checklistPanel);
        LeanTween.value(checklistPanel, 1f, 0f, 0.3f).setOnUpdate((float val) => { canvasGroup.alpha = val; }).setOnComplete(() =>
        {
            UpdateChecklistUI();
            LeanTween.value(checklistPanel, 0f, 1f, 0.3f).setOnUpdate((float val) => { canvasGroup.alpha = val; });
        });
    }

    public void ToggleChecklist()
    {
        if (checklistPanel != null)
        {
            bool newState = !checklistPanel.activeSelf;
            checklistPanel.SetActive(newState);
            if (newState)
            {
                checklistPanel.transform.localScale = Vector3.one * 0.9f;
                LeanTween.scale(checklistPanel, Vector3.one, 0.2f).setEase(LeanTweenType.easeOutBack);
            }
        }
    }
}
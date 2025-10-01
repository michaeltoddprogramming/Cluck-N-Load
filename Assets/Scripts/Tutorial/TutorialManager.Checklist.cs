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
        { "Farm Basics", new[] { "welcome", "melony_movement", "melony_zoom", "melony_rotate", "day_night_panel", "money_explanation", "time_controls", "season_bonuses", "enemy_indicator_tutorial" } },
        { "Building", new[] { "open_build_shop", "build_farmhouse", "build_crop_plot", "build_silo" } },
        { "Markets & Strategy", new[] { "price_panel_tutorial", "price_panel_explanation", "synergy_explanation" } },
        { "Farming", new[] { "plant_first_crop", "harvest_first_crops" } },
        { "Animals", new[] { "build_chicken_coop", "build_chicken_barracks", "buy_chickens", "feed_chickens", "collect_eggs" } },
        { "Defense", new[] { "recruit_soldiers", "build_first_hay_bale", "build_wall_chain", "place_flag", "prepare_defense" } },
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

        Debug.Log($"MarkStepComplete: Completed step '{stepId}'");
        completedStepIds.Add(stepId);
        CheckCategoryProgress();
        UpdateChecklistUI();
        PlayCompletionFeedback();

        // Enable skip button only after farmhouse is placed
        if (stepId == "build_farmhouse" && skipTutorialButton != null)
        {
            skipTutorialButton.gameObject.SetActive(true);
        }
    }

    private void CheckCategoryProgress()
    {
        Debug.Log($"CheckCategoryProgress: Current category = '{currentCategory}', Completed steps = [{string.Join(", ", completedStepIds)}]");
        
        if (!categories.TryGetValue(currentCategory, out string[] steps))
        {
            Debug.LogWarning($"No steps found for current category: {currentCategory}");
            return;
        }

        bool categoryComplete = true;
        foreach (string step in steps)
        {
            if (!completedStepIds.Contains(step))
            {
                categoryComplete = false;
                break;
            }
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
        Debug.Log($"UpdateChecklistUI: Current category = '{currentCategory}'");
        
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

    private void AddCategorySteps()
    {
        if (!categories.TryGetValue(currentCategory, out string[] stepIds))
        {
            Debug.LogWarning($"No steps found for category: {currentCategory}");
            return;
        }
        Debug.Log($"AddCategorySteps for '{currentCategory}': [{string.Join(", ", stepIds)}]");

        foreach (string stepId in stepIds)
        {
            Debug.Log($"Looking for step with ID: {stepId}");
            TutorialStep step = steps.Find(s => s.stepId == stepId);
            if (step == null)
            {
                Debug.LogWarning($"Could not find step with ID: {stepId}");
                continue;
            }
            Debug.Log($"Found step: {step.stepId} - {step.title}");

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
                // Skip animations during tutorial skip to prevent LeanTween overflow
                if (completedStepIds.Count > 10) // If we're completing many steps at once, skip animations
                {
                    checkmark.transform.localScale = Vector3.one;
                }
                else
                {
                    try
                    {
                        checkmark.transform.localScale = Vector3.zero;
                        LeanTween.scale(checkmark.gameObject, Vector3.one, 0.4f).setEase(LeanTweenType.easeOutBack).setDelay(0.1f);
                        LeanTween.rotateAroundLocal(checkmark.gameObject, Vector3.forward, 360f, 0.4f).setEase(LeanTweenType.easeOutCirc).setDelay(0.1f);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogWarning($"LeanTween animation failed: {e.Message}. Setting scale directly.");
                        checkmark.transform.localScale = Vector3.one;
                    }
                }
            }
        }

        Image background = toggle.GetComponent<Image>();
        if (background != null)
            background.color = isCompleted ? new Color(0.3f, 0.8f, 0.3f, 0.7f) : new Color(0.7f, 0.7f, 0.7f, 0.4f);
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

        LeanTween.value(checklistPanel, 1f, 0f, 0.3f).setOnUpdate((float val) =>
        {
            canvasGroup.alpha = val;
        }).setOnComplete(() =>
        {
            UpdateChecklistUI();
            LeanTween.value(checklistPanel, 0f, 1f, 0.3f).setOnUpdate((float val) =>
            {
                canvasGroup.alpha = val;
            });
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
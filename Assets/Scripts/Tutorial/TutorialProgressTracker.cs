using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class TutorialSaveData
{
    public bool tutorialCompleted;
    public bool tutorialEnabled;
    public List<string> completedStepIds;
    public List<int> completedConditions;
    public int currentStepIndex;
    public float tutorialStartTime;
    public float totalTutorialTime;

    public TutorialSaveData()
    {
        tutorialCompleted = false;
        tutorialEnabled = true;
        completedStepIds = new List<string>();
        completedConditions = new List<int>();
        currentStepIndex = 0;
        tutorialStartTime = 0f;
        totalTutorialTime = 0f;
    }
}

public class TutorialProgressTracker : MonoBehaviour
{
    [Header("Save Settings")]
    [SerializeField] private string saveKey = "TutorialProgress";
    [SerializeField] private bool autoSave = true;
    [SerializeField] private float autoSaveInterval = 30f;
    
    [Header("Analytics")]
    [SerializeField] private bool trackAnalytics = true;
    [SerializeField] private bool logProgressToConsole = false;
    
    private TutorialSaveData saveData;
    private Dictionary<string, float> stepStartTimes;
    private Dictionary<string, float> stepCompletionTimes;
    private float tutorialStartTime;
    private float lastAutoSaveTime;

    public System.Action<TutorialStep> OnStepStarted;
    public System.Action<TutorialStep, float> OnStepCompleted;
    public System.Action<float> OnTutorialCompleted;

    private void Awake()
    {
        stepStartTimes = new Dictionary<string, float>();
        stepCompletionTimes = new Dictionary<string, float>();
        
        LoadProgress();
        
        if (autoSave)
        {
            InvokeRepeating(nameof(AutoSave), autoSaveInterval, autoSaveInterval);
        }
    }

    private void Start()
    {
        // Subscribe to tutorial manager events
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnConditionCompleted += OnConditionCompleted;
            TutorialManager.Instance.OnTutorialCompleted += OnTutorialFinished;
        }

        tutorialStartTime = Time.time;
        saveData.tutorialStartTime = tutorialStartTime;
    }

    public void StartStep(TutorialStep step)
    {
        if (step == null) return;

        stepStartTimes[step.stepId] = Time.time;
        OnStepStarted?.Invoke(step);

        if (logProgressToConsole)
        {
            Debug.Log($"Tutorial Step Started: {step.stepId} - {step.title}");
        }

        if (trackAnalytics)
        {
            LogStepAnalytics(step, "started");
        }

        SaveProgress();
    }

    public void CompleteStep(TutorialStep step)
    {
        if (step == null) return;

        float completionTime = 0f;
        if (stepStartTimes.ContainsKey(step.stepId))
        {
            completionTime = Time.time - stepStartTimes[step.stepId];
            stepCompletionTimes[step.stepId] = completionTime;
        }

        if (!saveData.completedStepIds.Contains(step.stepId))
        {
            saveData.completedStepIds.Add(step.stepId);
        }

        OnStepCompleted?.Invoke(step, completionTime);

        if (logProgressToConsole)
        {
            Debug.Log($"Tutorial Step Completed: {step.stepId} - {step.title} (Time: {completionTime:F1}s)");
        }

        if (trackAnalytics)
        {
            LogStepAnalytics(step, "completed", completionTime);
        }

        SaveProgress();
    }

    public void OnConditionCompleted(TutorialCondition condition)
    {
        int conditionValue = (int)condition;
        if (!saveData.completedConditions.Contains(conditionValue))
        {
            saveData.completedConditions.Add(conditionValue);

            if (logProgressToConsole)
            {
                Debug.Log($"Tutorial Condition Met: {condition}");
            }

            if (trackAnalytics)
            {
                LogConditionAnalytics(condition);
            }
        }

        SaveProgress();
    }

    public void OnTutorialFinished()
    {
        saveData.tutorialCompleted = true;
        saveData.totalTutorialTime = Time.time - tutorialStartTime;

        OnTutorialCompleted?.Invoke(saveData.totalTutorialTime);

        if (logProgressToConsole)
        {
            Debug.Log($"Tutorial Completed! Total time: {saveData.totalTutorialTime:F1}s");
            LogTutorialSummary();
        }

        if (trackAnalytics)
        {
            LogTutorialCompletionAnalytics();
        }

        SaveProgress();
    }

    public void SaveProgress()
    {
        try
        {
            string jsonData = JsonUtility.ToJson(saveData, true);
            PlayerPrefs.SetString(saveKey, jsonData);
            PlayerPrefs.Save();
            
            lastAutoSaveTime = Time.time;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save tutorial progress: {e.Message}");
        }
    }

    public void LoadProgress()
    {
        try
        {
            if (PlayerPrefs.HasKey(saveKey))
            {
                string jsonData = PlayerPrefs.GetString(saveKey);
                saveData = JsonUtility.FromJson<TutorialSaveData>(jsonData);
            }
            else
            {
                saveData = new TutorialSaveData();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load tutorial progress: {e.Message}");
            saveData = new TutorialSaveData();
        }
    }

    public void ResetProgress()
    {
        saveData = new TutorialSaveData();
        stepStartTimes.Clear();
        stepCompletionTimes.Clear();
        
        PlayerPrefs.DeleteKey(saveKey);
        PlayerPrefs.Save();

        if (logProgressToConsole)
        {
            Debug.Log("Tutorial progress reset.");
        }
    }

    private void AutoSave()
    {
        if (Time.time - lastAutoSaveTime >= autoSaveInterval)
        {
            SaveProgress();
        }
    }

    private void LogStepAnalytics(TutorialStep step, string action, float duration = 0f)
    {
        if (!trackAnalytics) return;

        var analyticsData = new Dictionary<string, object>
        {
            ["step_id"] = step.stepId,
            ["step_title"] = step.title,
            ["action"] = action,
            ["timestamp"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        if (duration > 0)
        {
            analyticsData["duration"] = duration;
        }

        // Here you would send to your analytics service
        // For now, we'll just debug log
        Debug.Log($"Tutorial Analytics: {JsonUtility.ToJson(analyticsData)}");
    }

    private void LogConditionAnalytics(TutorialCondition condition)
    {
        if (!trackAnalytics) return;

        var analyticsData = new Dictionary<string, object>
        {
            ["condition"] = condition.ToString(),
            ["timestamp"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            ["tutorial_time_elapsed"] = Time.time - tutorialStartTime
        };

        Debug.Log($"Tutorial Condition Analytics: {JsonUtility.ToJson(analyticsData)}");
    }

    private void LogTutorialCompletionAnalytics()
    {
        if (!trackAnalytics) return;

        var analyticsData = new Dictionary<string, object>
        {
            ["total_duration"] = saveData.totalTutorialTime,
            ["steps_completed"] = saveData.completedStepIds.Count,
            ["conditions_met"] = saveData.completedConditions.Count,
            ["completion_timestamp"] = System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
        };

        Debug.Log($"Tutorial Completion Analytics: {JsonUtility.ToJson(analyticsData)}");
    }

    private void LogTutorialSummary()
    {
        Debug.Log("=== TUTORIAL SUMMARY ===");
        Debug.Log($"Total Time: {saveData.totalTutorialTime:F1} seconds");
        Debug.Log($"Steps Completed: {saveData.completedStepIds.Count}");
        Debug.Log($"Conditions Met: {saveData.completedConditions.Count}");
        
        if (stepCompletionTimes.Count > 0)
        {
            Debug.Log("Step Times:");
            foreach (var kvp in stepCompletionTimes)
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value:F1}s");
            }
        }
        
        Debug.Log("========================");
    }

    // Public getters
    public bool IsTutorialCompleted() => saveData.tutorialCompleted;
    public bool IsTutorialEnabled() => saveData.tutorialEnabled;
    public List<string> GetCompletedStepIds() => new List<string>(saveData.completedStepIds);
    public List<TutorialCondition> GetCompletedConditions() 
    {
        return saveData.completedConditions.Cast<TutorialCondition>().ToList();
    }
    public float GetTotalTutorialTime() => saveData.totalTutorialTime;
    public int GetCompletedStepsCount() => saveData.completedStepIds.Count;
    public float GetStepCompletionTime(string stepId)
    {
        return stepCompletionTimes.ContainsKey(stepId) ? stepCompletionTimes[stepId] : 0f;
    }

    // Public setters
    public void SetTutorialEnabled(bool enabled)
    {
        saveData.tutorialEnabled = enabled;
        SaveProgress();
    }

    public bool IsStepCompleted(string stepId)
    {
        return saveData.completedStepIds.Contains(stepId);
    }

    public bool IsConditionMet(TutorialCondition condition)
    {
        return saveData.completedConditions.Contains((int)condition);
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.OnConditionCompleted -= OnConditionCompleted;
            TutorialManager.Instance.OnTutorialCompleted -= OnTutorialFinished;
        }

        // Final save
        if (autoSave)
        {
            SaveProgress();
        }
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && autoSave)
        {
            SaveProgress();
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && autoSave)
        {
            SaveProgress();
        }
    }
}

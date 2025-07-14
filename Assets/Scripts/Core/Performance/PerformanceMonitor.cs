using UnityEngine;
using TMPro;

/// <summary>
/// Performance monitor for tracking FPS and memory usage
/// </summary>
public class PerformanceMonitor : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI fpsText;
    [SerializeField] private TextMeshProUGUI memoryText;
    [SerializeField] private bool showPerformanceUI = false;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f;
    [SerializeField] private bool enableProfiling = false;

    private float deltaTime = 0.0f;
    private float nextUpdate = 0.0f;
    private int frameCount = 0;
    private float fpsAccumulator = 0.0f;

    private void Start()
    {
        // Enable/disable performance UI based on settings
        if (fpsText != null) fpsText.gameObject.SetActive(showPerformanceUI);
        if (memoryText != null) memoryText.gameObject.SetActive(showPerformanceUI);

        // Enable profiling if requested
        if (enableProfiling)
        {
            Application.targetFrameRate = -1; // Uncap framerate for profiling
            QualitySettings.vSyncCount = 0;
        }
    }

    private void Update()
    {
        // Calculate delta time
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;
        fpsAccumulator += Time.unscaledDeltaTime;
        frameCount++;

        // Update UI at intervals
        if (Time.time >= nextUpdate && showPerformanceUI)
        {
            UpdatePerformanceUI();
            nextUpdate = Time.time + updateInterval;
        }

        // Check for performance toggle
        if (Input.GetKeyDown(KeyCode.F1))
        {
            TogglePerformanceUI();
        }
    }

    private void UpdatePerformanceUI()
    {
        if (frameCount == 0) return;

        // Calculate average FPS
        float averageFPS = frameCount / fpsAccumulator;
        float currentFPS = 1.0f / deltaTime;

        // Update FPS text
        if (fpsText != null)
        {
            Color fpsColor = GetFPSColor(currentFPS);
            fpsText.text = $"FPS: {currentFPS:F1} (Avg: {averageFPS:F1})";
            fpsText.color = fpsColor;
        }

        // Update memory text
        if (memoryText != null)
        {
            long memoryUsage = System.GC.GetTotalMemory(false);
            float memoryMB = memoryUsage / (1024f * 1024f);
            memoryText.text = $"Memory: {memoryMB:F1} MB";
        }

        // Reset accumulators
        frameCount = 0;
        fpsAccumulator = 0.0f;
    }

    private Color GetFPSColor(float fps)
    {
        if (fps >= 60f) return Color.green;
        if (fps >= 30f) return Color.yellow;
        return Color.red;
    }

    private void TogglePerformanceUI()
    {
        showPerformanceUI = !showPerformanceUI;
        
        if (fpsText != null) fpsText.gameObject.SetActive(showPerformanceUI);
        if (memoryText != null) memoryText.gameObject.SetActive(showPerformanceUI);

        }

    // Static methods for external use
    public static void LogPerformanceWarning(string message)
    {
        Debug.LogWarning($"[PERFORMANCE] {message}");
    }

    public static void StartProfiling(string profilingName)
    {
        if (Application.isEditor)
        {
            UnityEngine.Profiling.Profiler.BeginSample(profilingName);
        }
    }

    public static void EndProfiling()
    {
        if (Application.isEditor)
        {
            UnityEngine.Profiling.Profiler.EndSample();
        }
    }

    // Called by other systems to report performance metrics
    public void ReportCustomMetric(string metricName, float value)
    {
        if (enableProfiling)
        {
            }
    }

    private void OnDestroy()
    {
        // Clean up profiling
        if (enableProfiling && Application.isEditor)
        {
            UnityEngine.Profiling.Profiler.enabled = false;
        }
    }

    // Development helpers
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public void EditorOnlyPerformanceCheck()
    {
        if (deltaTime > 1f / 30f) // Below 30 FPS
        {
            Debug.LogWarning("Performance Warning: Frame time exceeded 33ms");
        }
    }
}

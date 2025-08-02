using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialManager
{
    void HandleRequiredInputDetection()
    {
        if (!waitingForStepToComplete || currentStepIndex < 0 || currentStepIndex >= steps.Count)
            return;

        var step = steps[currentStepIndex];
        if (step.requiredInputs == null || step.requiredInputs.Count == 0)
            return;

        foreach (KeyCode key in step.requiredInputs)
        {
            if (Input.GetKeyDown(key))
            {
                detectedInputs.Add(key);
                UpdateKeyIndicatorVisual(key, true);
            }
        }

        bool shouldAdvance = step.waitForAllInputs
            ? detectedInputs.Count >= step.requiredInputs.Count
            : detectedInputs.Count > 0;

        if (shouldAdvance)
            Trigger(TutorialTrigger.InputDetected);
    }

    void ShowKeyIndicators(List<KeyCode> keys)
    {
        ClearKeyIndicators();
        keyIndicatorMap.Clear();

        if (keyIndicatorPrefab == null || keyIndicatorContainer == null || keys == null || keys.Count == 0)
            return;

        foreach (KeyCode key in keys)
        {
            GameObject indicator = Instantiate(keyIndicatorPrefab, keyIndicatorContainer, false);
            indicator.name = $"Key_{key}";
            indicator.transform.localPosition = GetKeyPositionForLayout(key);

            string keyText = GetKeyDisplayName(key);

            TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = keyText;

            keyIndicatorMap[key] = indicator;
            keyIndicators.Add(indicator);
        }
    }

    void UpdateKeyIndicatorVisual(KeyCode key, bool pressed)
    {
        if (!keyIndicatorMap.TryGetValue(key, out GameObject indicator))
            return;
    
        Image background = indicator.GetComponentInChildren<Image>();
        if (background != null)
            background.color = pressed ? Color.green : Color.white;
    
        if (pressed && keyPressSound != null)
            effectsAudioSource.PlayOneShot(keyPressSound, 0.7f);
    
        if (pressed)
            LeanTween.scale(indicator, Vector3.one * 1.2f, 0.2f).setEasePunch();
    }

    string GetKeyDisplayName(KeyCode key)
    {
        if (key.ToString().StartsWith("Alpha"))
            return key.ToString().Substring(5); 
        
        switch (key)
        {
            case KeyCode.Mouse0: return "LC";
            case KeyCode.Mouse1: return "RC";
            case KeyCode.Mouse2: return "MWM"; 
            case KeyCode.Mouse3: return "MWU"; 
            case KeyCode.Mouse4: return "MWD"; 
            default: return key.ToString();
        }
    }

    void ClearKeyIndicators()
    {
        foreach (var obj in keyIndicators)
            Destroy(obj);
        keyIndicators.Clear();
    }

    Vector2 GetKeyPositionForLayout(KeyCode key)
    {
        float spacing = 120f; 
        return key switch
        {
            KeyCode.Mouse0 => new Vector2(spacing * 3, spacing),
            KeyCode.Mouse1 => new Vector2(spacing * 4, spacing),
            KeyCode.Mouse2 => new Vector2(spacing * 2.5f, 0),
            KeyCode.Mouse3 => new Vector2(spacing * 2, -spacing),
            KeyCode.Mouse4 => new Vector2(spacing * 3, -spacing),
            KeyCode.Alpha1 => new Vector2(-spacing * 0.5f, spacing * 2),
            KeyCode.Alpha2 => new Vector2(spacing * 0.5f, spacing * 2),
            KeyCode.W => new Vector2(0, spacing),
            KeyCode.A => new Vector2(-spacing, 0),
            KeyCode.S => new Vector2(0, 0),
            KeyCode.D => new Vector2(spacing, 0),
            KeyCode.Q => new Vector2(-spacing, spacing),
            KeyCode.E => new Vector2(spacing, spacing),
            _ => Vector2.zero
        };
    }
}
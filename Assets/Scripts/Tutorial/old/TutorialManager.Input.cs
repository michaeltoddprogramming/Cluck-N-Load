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
        
        // Handle Melony hunt input detection
        HandleMelonyInputDetection(step);
        
        if (step.requiredInputs == null || step.requiredInputs.Count == 0)
            return;

        foreach (KeyCode key in step.requiredInputs)
            if (Input.GetKeyDown(key))
            {
                detectedInputs.Add(key);
                UpdateKeyIndicatorVisual(key, true);
            }

        bool shouldAdvance = step.waitForAllInputs ? detectedInputs.Count >= step.requiredInputs.Count : detectedInputs.Count > 0;
        if (shouldAdvance)
            Trigger(TutorialTrigger.InputDetected);
    }
    
    void HandleMelonyInputDetection(TutorialStep step)
    {
        if (currentMelony == null) return;
        
        switch (step.stepId)
        {
            case "melony_movement":
                // Detect WASD and Q/E movement
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || 
                    Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D) ||
                    Input.GetKey(KeyCode.Q) || Input.GetKey(KeyCode.E))
                {
                    if (!detectedMelonyActions.Contains("movement"))
                    {
                        detectedMelonyActions.Add("movement");
                    }
                }
                break;
                
            case "melony_zoom":
                // Detect mouse wheel zoom OR 1/2 key zoom
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                bool keyZoom = Input.GetKey(KeyCode.Alpha1) || Input.GetKey(KeyCode.Alpha2);
                
                if (Mathf.Abs(scroll) > 0.01f || keyZoom)
                {
                    if (!detectedMelonyActions.Contains("zoom"))
                    {
                        detectedMelonyActions.Add("zoom");
                    }
                }
                break;
                
            case "melony_rotate":
                // Detect middle mouse button rotation
                if (Input.GetMouseButton(2) && 
                    (Mathf.Abs(Input.GetAxis("Mouse X")) > 0.01f || 
                     Mathf.Abs(Input.GetAxis("Mouse Y")) > 0.01f))
                {
                    if (!detectedMelonyActions.Contains("rotate"))
                    {
                        detectedMelonyActions.Add("rotate");
                    }
                }
                break;
        }
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
            
            // Check if this is a mouse button that should use an icon instead of text
            Sprite mouseIcon = GetMouseIconSprite(key);
            if (mouseIcon != null)
            {
                // Use the mouse icon sprite
                Image iconImage = indicator.GetComponentInChildren<Image>();
                if (iconImage != null)
                {
                    iconImage.sprite = mouseIcon;
                    iconImage.preserveAspect = true;
                    iconImage.raycastTarget = false; // Allow clicks to pass through
                    
                    // Use configurable scale for mouse icons
                    iconImage.transform.localScale = Vector3.one * mouseIconScale;
                }
                
                // Hide the text label for mouse buttons
                TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.gameObject.SetActive(false);
            }
            else
            {
                // Use text label for keyboard keys
                TextMeshProUGUI label = indicator.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                    label.text = GetKeyDisplayName(key);
                
                // Also disable raycast for keyboard key backgrounds
                Image backgroundImage = indicator.GetComponentInChildren<Image>();
                if (backgroundImage != null)
                    backgroundImage.raycastTarget = false;
            }
            
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

    Sprite GetMouseIconSprite(KeyCode key)
    {
        return key switch
        {
            KeyCode.Mouse0 => lmbIcon,        // Left Mouse Button
            KeyCode.Mouse1 => rmbIcon,        // Right Mouse Button  
            KeyCode.Mouse2 => mmbIcon,        // Middle Mouse Button
            KeyCode.Mouse3 => mmbUpIcon,      // Mouse Wheel Up
            KeyCode.Mouse4 => mmbDownIcon,    // Mouse Wheel Down
            _ => null // Not a mouse button, return null to use text
        };
    }

    string GetKeyDisplayName(KeyCode key)
    {
        if (key.ToString().StartsWith("Alpha"))
            return key.ToString().Substring(5);
        return key switch
        {
            KeyCode.Mouse0 => "LC",
            KeyCode.Mouse1 => "RC",
            KeyCode.Mouse2 => "MWM",
            KeyCode.Mouse3 => "MWU",
            KeyCode.Mouse4 => "MWD",
            _ => key.ToString()
        };
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
            // Mouse buttons - moved to the left (reduced X values)
            KeyCode.Mouse0 => new Vector2(spacing * 1.5f, spacing),      // Left mouse (was 3, now 1.5)
            KeyCode.Mouse1 => new Vector2(spacing * 2.5f, spacing),      // Right mouse (was 4, now 2.5)
            KeyCode.Mouse2 => new Vector2(spacing * 1f, 0),              // Middle mouse (was 2.5, now 1)
            KeyCode.Mouse3 => new Vector2(spacing * 0.5f, -spacing),     // Mouse wheel up (was 2, now 0.5)
            KeyCode.Mouse4 => new Vector2(spacing * 1.5f, -spacing),     // Mouse wheel down (was 3, now 1.5)
            
            // Number keys - moved down (reduced Y values)
            KeyCode.Alpha1 => new Vector2(-spacing * 0.5f, spacing * 1f),  // (was spacing * 2, now 1)
            KeyCode.Alpha2 => new Vector2(spacing * 0.5f, spacing * 1f),   // (was spacing * 2, now 1)
            
            // WASD keys - moved down (reduced Y values)
            KeyCode.W => new Vector2(0, 0),                              // (was spacing, now 0)
            KeyCode.A => new Vector2(-spacing, -spacing),                // (was 0, now -spacing)
            KeyCode.S => new Vector2(0, -spacing),                       // (was 0, now -spacing)
            KeyCode.D => new Vector2(spacing, -spacing),                 // (was 0, now -spacing)
            
            // Q and E keys - moved down (reduced Y values)
            KeyCode.Q => new Vector2(-spacing, 0),                       // (was spacing, now 0)
            KeyCode.E => new Vector2(spacing, 0),                        // (was spacing, now 0)
            
            _ => Vector2.zero
        };
    }
}
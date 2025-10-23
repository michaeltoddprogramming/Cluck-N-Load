using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialManager
{
    private void EnsureArrowExists()
    {
        if (tutorialArrow == null)
        {
            // Find the highest level canvas (usually the main UI canvas)
            Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            Canvas topCanvas = null;
            int highestSortingOrder = -1;
            
            foreach (Canvas canvas in allCanvases)
            {
                if (canvas.sortingOrder > highestSortingOrder || topCanvas == null)
                {
                    topCanvas = canvas;
                    highestSortingOrder = canvas.sortingOrder;
                }
            }
            
            // Create arrow as child of the top-level canvas to ensure it's always visible
            Transform parentTransform = topCanvas != null ? topCanvas.transform : tutorialPanel.transform;
            
            tutorialArrow = new GameObject("TutorialArrow");
            tutorialArrow.transform.SetParent(parentTransform, false);
            arrowRect = tutorialArrow.AddComponent<RectTransform>();
            
            // Consistent arrow size across devices
            arrowRect.sizeDelta = new Vector2(50, 50); // Slightly larger for better visibility
            
            // Create the arrow texture with consistent colors
            Texture2D texture = new Texture2D(50, 50);
            Color arrowColor = new Color(0f, 1f, 0.4f, 1f); // Bright green
            Color edgeColor = new Color(1f, 1f, 1f, 1f); // White edge
            
            // Create a better triangle shape (pointing down)
            for (int y = 0; y < 50; y++)
            {
                for (int x = 0; x < 50; x++)
                {
                    float centerX = 25f;
                    float centerY = 37f; // Move triangle higher in texture
                    
                    // Triangle pointing down - more defined shape
                    bool insideTriangle = (y <= centerY) && 
                                        (x >= centerX - (centerY - y) * 0.7f) && 
                                        (x <= centerX + (centerY - y) * 0.7f);
                    
                    // Edge detection for white outline
                    bool isEdge = false;
                    if (insideTriangle)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < 50 && ny >= 0 && ny < 50)
                                {
                                    bool neighborInside = (ny <= centerY) && 
                                                        (nx >= centerX - (centerY - ny) * 0.7f) && 
                                                        (nx <= centerX + (centerY - ny) * 0.7f);
                                    if (!neighborInside)
                                    {
                                        isEdge = true;
                                        break;
                                    }
                                }
                            }
                            if (isEdge) break;
                        }
                    }
                    
                    if (insideTriangle)
                        texture.SetPixel(x, y, isEdge ? edgeColor : arrowColor);
                    else
                        texture.SetPixel(x, y, Color.clear);
                }
            }
            texture.Apply();
            
            // Create sprite with consistent settings
            Sprite triangleSprite = Sprite.Create(texture, new Rect(0, 0, 50, 50), new Vector2(0.5f, 0.5f), 100f);
            Image arrowImage = tutorialArrow.AddComponent<Image>();
            arrowImage.sprite = triangleSprite;
            arrowImage.preserveAspect = true;
            arrowImage.color = Color.white;
            arrowImage.raycastTarget = false; // Allow clicks to pass through the arrow
            
            // Add Canvas component to ensure arrow appears on top of all UI
            Canvas arrowCanvas = tutorialArrow.AddComponent<Canvas>();
            arrowCanvas.overrideSorting = true;
            arrowCanvas.sortingOrder = 9999; // Very high fixed sorting order to ensure it's always on top
            arrowCanvas.sortingLayerName = "UI"; // Ensure it's on the UI layer
            
            // Add GraphicRaycaster to maintain proper UI interaction
            tutorialArrow.AddComponent<GraphicRaycaster>();
            
            // Ensure arrow starts hidden
            tutorialArrow.SetActive(false);
        }
    }

    // Ensure the arrow has the highest sorting order to appear on top of all UI
    private void EnsureArrowOnTop()
    {
        if (tutorialArrow == null) return;
        
        Canvas arrowCanvas = tutorialArrow.GetComponent<Canvas>();
        if (arrowCanvas == null) return;
        
        // Find the current highest sorting order
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        int highestSortingOrder = -1;
        
        foreach (Canvas canvas in allCanvases)
        {
            if (canvas != arrowCanvas && canvas.sortingOrder > highestSortingOrder)
            {
                highestSortingOrder = canvas.sortingOrder;
            }
        }
        
        // Set arrow sorting order to maximum to ensure it's always on top
        int newSortingOrder = Mathf.Max(highestSortingOrder + 200, 9999);
        if (arrowCanvas.sortingOrder < newSortingOrder)
        {
            arrowCanvas.sortingOrder = newSortingOrder;
        }
    }

    public void ShowArrowPointing(GameObject target, bool show)
    {
        if (target == null)
        {
            // If target is null, always hide the arrow
            HideArrow();
            return;
        }

        EnsureArrowExists();
        if (show)
        {
            // Ensure arrow has the highest sorting order when showing
            EnsureArrowOnTop();
            
            RectTransform targetRect = target.GetComponent<RectTransform>();
            Vector3 targetCenter;
            Vector3 screenPoint;
            
            if (targetRect != null)
            {
                // Handle UI elements
                Vector3[] targetCorners = new Vector3[4];
                targetRect.GetWorldCorners(targetCorners);
                targetCenter = (targetCorners[0] + targetCorners[1] + targetCorners[2] + targetCorners[3]) / 4f;
                
                // Convert target position to screen space, then to arrow's canvas space
                screenPoint = RectTransformUtility.WorldToScreenPoint(Camera.main, targetCenter);
            }
            else
            {
                // Handle world objects (structures)
                Renderer targetRenderer = target.GetComponent<Renderer>();
                if (targetRenderer != null)
                {
                    // Get the bounds of the structure and position arrow above it
                    Vector3 bounds = targetRenderer.bounds.center;
                    float structureHeight = targetRenderer.bounds.size.y;
                    targetCenter = bounds + new Vector3(0, structureHeight * 0.7f, 0); // Position above the structure
                }
                else
                {
                    // Fallback to transform position
                    targetCenter = target.transform.position + new Vector3(0, 2f, 0);
                }
                
                // Convert world position to screen space
                screenPoint = Camera.main.WorldToScreenPoint(targetCenter);
            }
            
            if (targetRect != null || screenPoint.z > 0) // Ensure world object is in front of camera
            {
                
                // Get the arrow's parent canvas for positioning
                Canvas arrowParentCanvas = tutorialArrow.GetComponentInParent<Canvas>();
                RectTransform arrowCanvasRect = arrowParentCanvas.GetComponent<RectTransform>();
                
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    arrowCanvasRect, 
                    screenPoint, 
                    arrowParentCanvas.worldCamera ?? Camera.main, 
                    out localPoint))
                {
                    // Position arrow below the target with consistent offset
                    float yOffset = targetRect != null ? -60 : -80; // Larger offset for world objects
                    Vector2 arrowPosition = localPoint + new Vector2(0, yOffset);
                    arrowRect.anchoredPosition = arrowPosition;
                }
                else
                {
                    Debug.LogWarning("[TutorialArrow] Failed to convert screen point to local point");
                    // Fallback positioning
                    arrowRect.position = targetCenter + new Vector3(0, -60, 0);
                }
                
                arrowRect.rotation = Quaternion.Euler(0, 0, 0); // Always point down
                tutorialArrow.SetActive(true);
                LeanTween.cancel(tutorialArrow);
                
                // Store the final positioned location AFTER positioning is complete
                Vector2 finalPos = arrowRect.anchoredPosition;
                
                // Consistent animation parameters
                LeanTween.scale(tutorialArrow, new Vector3(1.3f, 1.3f, 1.3f), 0.4f).setLoopPingPong().setEase(LeanTweenType.easeInOutBack);
                LeanTween.value(tutorialArrow, 0f, -15f, 0.5f).setLoopPingPong().setEase(LeanTweenType.easeInOutBack).setOnUpdate((float val) =>
                {
                    if (arrowRect != null)
                    {
                        arrowRect.anchoredPosition = finalPos + new Vector2(0, val);
                    }
                });
                
                // Consistent rotation wobble
                LeanTween.rotate(tutorialArrow, new Vector3(0, 0, 5f), 0.3f).setLoopPingPong().setEase(LeanTweenType.easeInOutSine);
            }
        }
        else
        {
            HideArrow();
        }
    }

    // Helper method for consistent arrow cleanup
    private void HideArrow()
    {
        if (tutorialArrow != null)
        {
            tutorialArrow.SetActive(false);
            LeanTween.cancel(tutorialArrow);
            // Reset arrow transform to prevent positioning issues
            if (arrowRect != null)
            {
                arrowRect.localScale = Vector3.one;
                arrowRect.localRotation = Quaternion.identity;
                arrowRect.anchoredPosition = Vector3.zero; // Reset anchored position
            }
        }
    }

    private IEnumerator TypeTextWithMumble(string text)
    {
        // IMMEDIATELY process colors BEFORE any typing starts
        // This ensures users never see raw color tags
        string processedText = ProcessTextForColors(text);
        
        // Aggressively ensure rich text support is enabled
        if (dialogueText != null)
        {
            dialogueText.richText = true;
            dialogueText.parseCtrlCharacters = true; // This is important for rich text
            dialogueText.SetAllDirty();
            dialogueText.ForceMeshUpdate();
            
            // Wait multiple frames to ensure everything is set up
            yield return null;
            yield return null;
        }
        
        // Parse the text to separate visible characters from rich text tags
        var textSegments = ParseRichText(processedText);
        
        dialogueText.text = "";
        if (mumbleAudioSource != null && mumbleClips != null && mumbleClips.Length > 0)
        {
            AudioClip randomMumble = mumbleClips[Random.Range(0, mumbleClips.Length)];
            mumbleAudioSource.clip = randomMumble;
            mumbleAudioSource.pitch = Random.Range(0.9f, 1.1f);
            mumbleAudioSource.volume = Random.Range(0.6f, 0.8f);
            mumbleAudioSource.loop = true;
            mumbleAudioSource.Play();
        }

        float nextCharTime = 0f;
        int segmentIndex = 0;
        int charIndexInSegment = 0;
        var displayedText = new System.Text.StringBuilder();
        bool wasMumblePlaying = false;

        while (segmentIndex < textSegments.Count)
        {
            // If the game is paused, wait until unpaused before continuing
            if (Time.timeScale == 0f)
            {
                if (mumbleAudioSource != null)
                {
                    wasMumblePlaying = mumbleAudioSource.isPlaying;
                    if (mumbleAudioSource.isPlaying)
                        mumbleAudioSource.Pause();
                }
                // Wait until unpaused
                while (Time.timeScale == 0f)
                {
                    yield return null;
                }
                // Resume audio only if it was playing before pause
                if (mumbleAudioSource != null && wasMumblePlaying && mumbleAudioSource.clip != null && !isMumblePaused)
                    mumbleAudioSource.UnPause();
            }

            var currentSegment = textSegments[segmentIndex];

            if (currentSegment.isTag)
            {
                // Add the entire tag at once (tags are invisible)
                displayedText.Append(currentSegment.text);
                dialogueText.text = displayedText.ToString();
                segmentIndex++;
                charIndexInSegment = 0;
            }
            else
            {
                // Type visible characters one by one
                nextCharTime += Time.unscaledDeltaTime;
                while (nextCharTime >= typeSpeed && charIndexInSegment < currentSegment.text.Length)
                {
                    displayedText.Append(currentSegment.text[charIndexInSegment]);
                    dialogueText.text = displayedText.ToString();
                    charIndexInSegment++;
                    nextCharTime -= typeSpeed;
                    
                    // Force update the text rendering periodically
                    if (charIndexInSegment % 3 == 0)
                    {
                        dialogueText.ForceMeshUpdate();
                    }
                }

                // Move to next segment when current segment is complete
                if (charIndexInSegment >= currentSegment.text.Length)
                {
                    segmentIndex++;
                    charIndexInSegment = 0;
                }
            }
            
            yield return null;
        }

        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
        {
            float startVolume = mumbleAudioSource.volume;
            float duration = 0.5f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                mumbleAudioSource.volume = Mathf.Lerp(startVolume, 0, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            mumbleAudioSource.Stop();
            mumbleAudioSource.volume = startVolume;
        }
    }

    void UpdateCharacterPortrait(TutorialStep step)
    {
        if (characterPortraitImage == null)
            return;

        characterPortraitImage.sprite = step.characterSprite;
        characterPortraitImage.gameObject.SetActive(step.characterSprite != null);
    }

    public void HighlightUI(GameObject target, bool enable)
    {
        if (target == null)
            return;

        LeanTween.cancel(target);
        ShowArrowPointing(target, enable);
        HighlightButtonDirectly(target, enable);
    }

    // New method for highlighting buttons without arrows (for animal/barracks UI)
    public void HighlightUIWithoutArrow(GameObject target, bool enable)
    {
        if (target == null)
            return;

        LeanTween.cancel(target);
        // Skip ShowArrowPointing - only highlight the button directly
        HighlightButtonDirectly(target, enable);
    }

    private void HighlightButtonDirectly(GameObject target, bool enable)
    {
        Outline outline = target.GetComponent<Outline>() ?? (enable ? target.AddComponent<Outline>() : null);
        if (outline != null)
        {
            outline.enabled = enable;
            if (enable)
            {
                outline.effectColor = new Color(0f, 1f, 0.4f, 1f); // Bright green to match arrow
                outline.effectDistance = new Vector2(4, 4); // Thicker outline
                
                // Pulsing effect with more vibrant colors
                LeanTween.value(target, 3f, 6f, 0.8f).setLoopPingPong().setEase(LeanTweenType.easeInOutSine).setOnUpdate((float val) =>
                {
                    outline.effectDistance = new Vector2(val, val);
                });
                
                // Color pulsing between bright green and bright cyan
                LeanTween.value(target, 0f, 1f, 1.0f).setLoopPingPong().setEase(LeanTweenType.easeInOutSine).setOnUpdate((float val) =>
                {
                    outline.effectColor = Color.Lerp(new Color(0f, 1f, 0.4f, 1f), new Color(0f, 0.8f, 1f, 1f), val);
                });
                
                // Gentle scale pulsing
                if (target.GetComponent<RectTransform>() != null)
                    LeanTween.scale(target, target.transform.localScale * 1.08f, 0.6f).setLoopPingPong().setEase(LeanTweenType.easeInOutQuad);
            }
            else
            {
                outline.effectDistance = new Vector2(4, 4);
                outline.effectColor = new Color(0f, 1f, 0.4f, 1f);
                if (target.GetComponent<RectTransform>() != null)
                    target.transform.localScale = Vector3.one;
            }
        }
    }

    private string ProcessTextForColors(string text)
    {
        // Convert named colors to more logical and readable hex colors
        string processedText = text;
        
        // Logical color scheme:
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=yellow>(.*?)</color>", "<color=#FFD700>$1</color>");    // Gold for important actions/clicks
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=green>(.*?)</color>", "<color=#32CD32>$1</color>");     // Lime green for farm/nature things
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=cyan>(.*?)</color>", "<color=#87CEEB>$1</color>");      // Sky blue (more readable) for UI/controls
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=magenta>(.*?)</color>", "<color=#FF69B4>$1</color>");   // Hot pink for Melony (character)
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=red>(.*?)</color>", "<color=#FF4444>$1</color>");       // Bright red for warnings/danger
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=blue>(.*?)</color>", "<color=#4169E1>$1</color>");      // Royal blue for time/night
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=orange>(.*?)</color>", "<color=#FF8C00>$1</color>");    // Dark orange for animals
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=white>(.*?)</color>", "<color=#F0F0F0>$1</color>");     // Off-white for visibility
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=brown>(.*?)</color>", "<color=#D2691E>$1</color>");     // Chocolate brown for buildings
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=gold>(.*?)</color>", "<color=#FFD700>$1</color>");      // Gold for money/rewards
        processedText = System.Text.RegularExpressions.Regex.Replace(processedText, @"<color=pink>(.*?)</color>", "<color=#FFB6C1>$1</color>");      // Light pink for pigs
        
        return processedText;
    }

    /// <summary>
    /// Represents a segment of text that can be either visible text or a rich text tag
    /// </summary>
    private struct TextSegment
    {
        public string text;
        public bool isTag;
        public bool isVisible;
    }

    /// <summary>
    /// Parses rich text into segments, separating visible characters from formatting tags
    /// </summary>
    private System.Collections.Generic.List<TextSegment> ParseRichText(string richText)
    {
        var segments = new System.Collections.Generic.List<TextSegment>();
        int currentIndex = 0;
        var currentText = new System.Text.StringBuilder();

        while (currentIndex < richText.Length)
        {
            if (richText[currentIndex] == '<')
            {
                // Save any accumulated visible text
                if (currentText.Length > 0)
                {
                    segments.Add(new TextSegment { text = currentText.ToString(), isTag = false, isVisible = true });
                    currentText.Clear();
                }

                // Find the end of the tag
                int tagEnd = richText.IndexOf('>', currentIndex);
                if (tagEnd != -1)
                {
                    string tag = richText.Substring(currentIndex, tagEnd - currentIndex + 1);
                    segments.Add(new TextSegment { text = tag, isTag = true, isVisible = false });
                    currentIndex = tagEnd + 1;
                }
                else
                {
                    // No closing bracket found, treat as regular character
                    currentText.Append(richText[currentIndex]);
                    currentIndex++;
                }
            }
            else
            {
                // Regular character
                currentText.Append(richText[currentIndex]);
                currentIndex++;
            }
        }

        // Add any remaining text
        if (currentText.Length > 0)
        {
            segments.Add(new TextSegment { text = currentText.ToString(), isTag = false, isVisible = true });
        }

        return segments;
    }
}
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
            tutorialArrow = new GameObject("TutorialArrow");
            tutorialArrow.transform.SetParent(tutorialPanel.transform, false);
            arrowRect = tutorialArrow.AddComponent<RectTransform>();
            arrowRect.sizeDelta = new Vector2(40, 40);
            Texture2D texture = new Texture2D(40, 40);
            Color arrowColor = new Color(0f, 1f, 0.4f, 1f); // Bright green
            Color edgeColor = new Color(1f, 1f, 1f, 1f); // White edge
            
            // Create a better triangle shape
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    float centerX = 20f;
                    float centerY = 30f;
                    
                    // Triangle pointing down
                    bool insideTriangle = (y <= centerY) && 
                                        (x >= centerX - (centerY - y) * 0.8f) && 
                                        (x <= centerX + (centerY - y) * 0.8f);
                    
                    // Edge detection for white outline
                    bool isEdge = false;
                    if (insideTriangle)
                    {
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dy = -1; dy <= 1; dy++)
                            {
                                int nx = x + dx, ny = y + dy;
                                if (nx >= 0 && nx < 40 && ny >= 0 && ny < 40)
                                {
                                    bool neighborInside = (ny <= centerY) && 
                                                        (nx >= centerX - (centerY - ny) * 0.8f) && 
                                                        (nx <= centerX + (centerY - ny) * 0.8f);
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
            Sprite triangleSprite = Sprite.Create(texture, new Rect(0, 0, 40, 40), new Vector2(0.5f, 0.5f), 100f);
            Image arrowImage = tutorialArrow.AddComponent<Image>();
            arrowImage.sprite = triangleSprite;
            arrowImage.preserveAspect = true;
            arrowImage.color = Color.white;
            arrowImage.raycastTarget = false; // Allow clicks to pass through the arrow
            tutorialArrow.SetActive(false);
        }
    }

    public void ShowArrowPointing(GameObject target, bool show)
    {
        if (target == null)
            return;

        EnsureArrowExists();
        if (show)
        {
            RectTransform targetRect = target.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                Vector3[] screenCorners = new Vector3[4];
                targetRect.GetWorldCorners(screenCorners);
                Vector3 targetCenter = (screenCorners[0] + screenCorners[1] + screenCorners[2] + screenCorners[3]) / 4f;
                Canvas canvas = targetRect.GetComponentInParent<Canvas>();
                RectTransform canvasRect = canvas.GetComponent<RectTransform>();
                Vector3[] canvasCorners = new Vector3[4];
                canvasRect.GetWorldCorners(canvasCorners);
                float screenWidth = canvasCorners[2].x - canvasCorners[0].x;
                float screenHeight = canvasCorners[2].y - canvasCorners[0].y;
                float normalizedX = (targetCenter.x - canvasCorners[0].x) / screenWidth;
                float normalizedY = (targetCenter.y - canvasCorners[0].y) / screenHeight;
                Vector3 arrowPosition = targetCenter + new Vector3(0, -50, 0);
                arrowRect.position = arrowPosition;
                arrowRect.rotation = Quaternion.Euler(0, 0, 0);
                tutorialArrow.SetActive(true);
                LeanTween.cancel(tutorialArrow);
                
                // More prominent bounce animation
                LeanTween.scale(tutorialArrow, new Vector3(1.3f, 1.3f, 1.3f), 0.4f).setLoopPingPong().setEase(LeanTweenType.easeInOutBack);
                LeanTween.move(tutorialArrow, arrowPosition + new Vector3(0, -15, 0), 0.5f).setLoopPingPong().setEase(LeanTweenType.easeInOutBack);
                
                // Add rotation wobble for extra attention
                LeanTween.rotate(tutorialArrow, new Vector3(0, 0, 5f), 0.3f).setLoopPingPong().setEase(LeanTweenType.easeInOutSine);
            }
        }
        else
        {
            tutorialArrow.SetActive(false);
            LeanTween.cancel(tutorialArrow);
        }
    }

    private IEnumerator TypeTextWithMumble(string text)
    {
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
        int charIndex = 0;
        bool wasMumblePlaying = false;
        while (charIndex < text.Length)
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

            nextCharTime += Time.unscaledDeltaTime;
            while (nextCharTime >= typeSpeed && charIndex < text.Length)
            {
                dialogueText.text += text[charIndex];
                charIndex++;
                nextCharTime -= typeSpeed;
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
}
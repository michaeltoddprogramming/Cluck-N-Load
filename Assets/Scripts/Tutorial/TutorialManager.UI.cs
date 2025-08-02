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
            Color arrowColor = new Color(1f, 0.8f, 0.2f, 0.95f);
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    // Triangle pointing up
                    float normX = (float)x / 39f;
                    float normY = (float)y / 39f;
                    if (normY > Mathf.Abs(normX - 0.5f) * 2)
                        texture.SetPixel(x, y, arrowColor);
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

            tutorialArrow.SetActive(false);
        }
    }
    
public void ShowArrowPointing(GameObject target, bool show)
{
    if (target == null) return;
    
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

            Vector3 arrowPosition;
            Quaternion arrowRotation;

            arrowPosition = targetCenter + new Vector3(0, -50, 0); 
            arrowRotation = Quaternion.Euler(0, 0, 0); 

            arrowRect.position = arrowPosition;
            arrowRect.rotation = arrowRotation;
            tutorialArrow.SetActive(true);
            LeanTween.cancel(tutorialArrow);
            LeanTween.scale(tutorialArrow, new Vector3(1.2f, 1.2f, 1.2f), 0.5f)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutQuad);

            Vector3 moveOffset = new Vector3(0, -10, 0);
            LeanTween.move(tutorialArrow, arrowPosition + moveOffset, 0.6f)
                .setLoopPingPong()
                .setEase(LeanTweenType.easeInOutQuad);
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
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        
        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
        {
            float startVolume = mumbleAudioSource.volume;
            float duration = 0.5f;
            float elapsed = 0f;
            
            while (elapsed < duration)
            {
                mumbleAudioSource.volume = Mathf.Lerp(startVolume, 0, elapsed/duration);
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

        if (step.characterSprite != null)
        {
            characterPortraitImage.sprite = step.characterSprite;
            characterPortraitImage.gameObject.SetActive(true);
        }
        else
            characterPortraitImage.gameObject.SetActive(false);
    }

      public void HighlightUI(GameObject target, bool enable)
    {
        if (target == null) return;
    
        LeanTween.cancel(target);

        ShowArrowPointing(target,enable);
        
        Outline outline = target.GetComponent<Outline>();
        if (outline == null && enable)
        {
            outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(1f, 0.8f, 0.2f, 1f);
            outline.effectDistance = new Vector2(3, 3);
        }
    
        if (outline != null)
        {
            outline.enabled = enable;
            
            if (enable)
            {

                

                LeanTween.value(target, 2f, 5f, 0.8f)
                    .setLoopPingPong()
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnUpdate((float val) => {
                        outline.effectDistance = new Vector2(val, val);
                    });
                    

                LeanTween.value(target, 0f, 1f, 1.2f)
                    .setLoopPingPong()
                    .setEase(LeanTweenType.easeInOutSine)
                    .setOnUpdate((float val) => {
                        outline.effectColor = Color.Lerp(
                            new Color(1f, 0.8f, 0.2f, 0.7f),  
                            new Color(1f, 1f, 0.5f, 1f),      
                            val
                        );
                    });
                    
                if (target.GetComponent<RectTransform>() != null)
                {
                    Vector3 originalScale = target.transform.localScale;
                    LeanTween.scale(target, originalScale * 1.05f, 0.5f)
                        .setLoopPingPong()
                        .setEase(LeanTweenType.easeInOutQuad);
                }
            }
            else
            {
                outline.effectDistance = new Vector2(3, 3);
                outline.effectColor = new Color(1f, 0.8f, 0.2f, 1f);
                if (target.GetComponent<RectTransform>() != null)
                {
                    target.transform.localScale = Vector3.one;
                }
            }
        }
    }

    IEnumerator AnimateOutline(Outline outline)
    {
        float time = 0f;
        float duration = 0.5f;
        Vector2 start = new(5, 5);
        Vector2 end = new(10, 10);
        Color startColor = Color.yellow;
        Color endColor = Color.cyan;

        while (outline.enabled)
        {
            time += Time.unscaledDeltaTime;
            float t = Mathf.PingPong(time / duration, 1f);
            outline.effectDistance = Vector2.Lerp(start, end, t);
            outline.effectColor = Color.Lerp(startColor, endColor, t);
            yield return null;
        }
    }
}
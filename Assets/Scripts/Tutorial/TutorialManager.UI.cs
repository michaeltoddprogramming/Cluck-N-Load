using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class TutorialManager
{
    private IEnumerator TypeTextWithMumble(string text)
    {
        dialogueText.text = "";
        
        if (mumbleAudioSource != null && mumbleClips != null && mumbleClips.Length > 0)
        {
            AudioClip randomMumble = mumbleClips[Random.Range(0, mumbleClips.Length)];
            mumbleAudioSource.clip = randomMumble;
            mumbleAudioSource.pitch = Random.Range(0.9f, 1.1f); // Slight random pitch variation
            mumbleAudioSource.volume = Random.Range(0.8f, 1.0f); // Slight random volume variation
            mumbleAudioSource.loop = true;
            mumbleAudioSource.Play();
        }
        
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSecondsRealtime(typeSpeed);
        }
        
        if (mumbleAudioSource != null && mumbleAudioSource.isPlaying)
            mumbleAudioSource.Stop();
        
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

    void HighlightUI(GameObject target, bool enable)
    {
        if (target == null) return;

        Outline outline = target.GetComponent<Outline>();
        if (outline == null && enable)
        {
            outline = target.AddComponent<Outline>();
            outline.effectColor = Color.yellow;
            outline.effectDistance = new Vector2(5, 5);
        }

        if (outline != null)
        {
            outline.enabled = enable;
            if (enable)
            {
                StopCoroutine("AnimateOutline");
                StartCoroutine(AnimateOutline(outline));
            }
            else
            {
                StopCoroutine("AnimateOutline");
                outline.effectDistance = new Vector2(5, 5);
                outline.effectColor = Color.yellow;
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
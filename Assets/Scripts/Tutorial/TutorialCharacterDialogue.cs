using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TutorialCharacterDialogue : MonoBehaviour
{
    [Header("Character Setup")]
    [SerializeField] private string characterName = "Old Pete";
    [SerializeField] private Sprite characterPortrait;
    [SerializeField] private AudioClip[] voiceClips;
    
    [Header("Animation Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private bool enablePortraitAnimation = true;
    [SerializeField] private float portraitBobAmount = 5f;
    [SerializeField] private float portraitBobSpeed = 2f;
    
    private AudioSource audioSource;
    private Vector3 originalPortraitPosition;
    private bool isTyping = false;
    private bool isPortraitAnimating = false;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.volume = 0.7f;
        audioSource.playOnAwake = false;
    }

    private void Start()
    {
        if (characterPortrait != null)
        {
            originalPortraitPosition = transform.position;
        }
    }

    public void PlayDialogue(string text, TextMeshProUGUI textComponent)
    {
        if (textComponent != null)
        {
            StartCoroutine(TypewriterEffect(text, textComponent));
        }
        
        PlayRandomVoiceClip();
        
        if (enablePortraitAnimation)
        {
            StartPortraitAnimation();
        }
    }

    private IEnumerator TypewriterEffect(string fullText, TextMeshProUGUI textComponent)
    {
        isTyping = true;
        textComponent.text = "";
        
        foreach (char c in fullText)
        {
            textComponent.text += c;
            
            // Play typing sound for certain characters
            if (c != ' ' && UnityEngine.Random.Range(0f, 1f) < 0.3f)
            {
                PlayTypingSound();
            }
            
            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }
        
        isTyping = false;
        StopPortraitAnimation();
    }

    private void PlayRandomVoiceClip()
    {
        if (voiceClips != null && voiceClips.Length > 0 && audioSource != null)
        {
            AudioClip randomClip = voiceClips[UnityEngine.Random.Range(0, voiceClips.Length)];
            audioSource.clip = randomClip;
            audioSource.pitch = UnityEngine.Random.Range(0.9f, 1.1f);
            audioSource.Play();
        }
    }

    private void PlayTypingSound()
    {
        // Simple typing sound using audio source pitch modulation
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.pitch = UnityEngine.Random.Range(1.8f, 2.2f);
            audioSource.volume = 0.1f;
            audioSource.PlayOneShot(audioSource.clip, 0.1f);
        }
    }

    private void StartPortraitAnimation()
    {
        if (!isPortraitAnimating)
        {
            StartCoroutine(PortraitBobAnimation());
        }
    }

    private void StopPortraitAnimation()
    {
        isPortraitAnimating = false;
    }

    private IEnumerator PortraitBobAnimation()
    {
        isPortraitAnimating = true;
        
        while (isPortraitAnimating && isTyping)
        {
            float newY = originalPortraitPosition.y + Mathf.Sin(Time.unscaledTime * portraitBobSpeed) * portraitBobAmount;
            transform.position = new Vector3(originalPortraitPosition.x, newY, originalPortraitPosition.z);
            
            yield return null;
        }
        
        // Return to original position
        transform.position = originalPortraitPosition;
    }

    public bool IsTyping()
    {
        return isTyping;
    }

    public void SkipTyping()
    {
        isTyping = false;
        StopAllCoroutines();
        StopPortraitAnimation();
    }
}

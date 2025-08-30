using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class MainMenuController : MonoBehaviour
{
    public Material skyboxMaterial;
    public float rotationSpeed = 10f;
    public GameObject saveSelectionMenu; 
    public GameObject optionsMenu; 
    public AudioSource backgroundMusic; 
    public AudioSource uiAudioSource;
    public AudioClip hoverSound;
    public AudioClip clickSound;

    private float currentRotation = 0f;

    void Start()
    {
        CleanupPersistentObjects();
        if (skyboxMaterial == null)
            skyboxMaterial = RenderSettings.skybox;
        StartCoroutine(AnimateSkyboxRotation());
        if (saveSelectionMenu != null)
            saveSelectionMenu.SetActive(false); // Ensure hidden at start

        if (backgroundMusic != null && !backgroundMusic.isPlaying)
        {
            backgroundMusic.Play();
            DontDestroyOnLoad(backgroundMusic.gameObject);
        }
    }

    private IEnumerator AnimateSkyboxRotation()
    {
        while (true)
        {
            currentRotation += rotationSpeed * Time.deltaTime;
            currentRotation %= 360f;
            skyboxMaterial.SetFloat("_Rotation", currentRotation); // correct property
            RenderSettings.skybox = skyboxMaterial;
            yield return null;
        }
    }

    public void OnPlayButtonClicked()
    {
        SceneTransitionManager.Instance.LoadSceneWithLoading("MainScene");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    public void OnContinueButtonClicked()
    {
        if (saveSelectionMenu != null)
            saveSelectionMenu.SetActive(true); // Show the menu
    }

    public void OnBackFromSaveSelection()
    {
        if (saveSelectionMenu != null)
            saveSelectionMenu.SetActive(false);
    }

    public void OnOptionsButtonClicked()
    {
        if (OptionsMenuController.Instance != null)
            OptionsMenuController.Instance.ShowMenu();
    }

    public void PlayHoverSound()
    {
        if (uiAudioSource != null && hoverSound != null)
        {
            uiAudioSource.PlayOneShot(hoverSound);
        }
    }

    public void PlayClickSound()
    {
        if (uiAudioSource != null && clickSound != null)
        {
            uiAudioSource.PlayOneShot(clickSound);
        }
    }

    private void CleanupPersistentObjects()
    {
        string[] persistentNames = {
            "OptionsCanvas 1(Clone)",
            "UIManager",
            "AudioManager",
            "GameLoopManager",
            "GameManager",
            "MoneyManager",
            "ShopUIManager",
            "LeanTween",
            "Debug Updater"
        };

        foreach (string objName in persistentNames)
        {
            var obj = GameObject.Find(objName);
            if (obj != null)
            {
                Destroy(obj);
            }
        }
    }
}
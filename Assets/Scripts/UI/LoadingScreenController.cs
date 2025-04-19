using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI pressKeyText; // New UI element for the prompt
    [SerializeField] private float minimumDisplayTime = 2f;

    private AsyncOperation sceneLoadOperation;
    private float timeElapsed = 0f;
    private bool isSceneReady = false;
    private bool waitingForKeyPress = false; // New flag to track if we're waiting for input

    void Start()
    {
        // Initialize progress bar and text
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        if (progressText != null)
        {
            progressText.text = "0%";
        }

        // Hide the "Press any key" text initially
        if (pressKeyText != null)
        {
            pressKeyText.gameObject.SetActive(false);
        }

        // Start loading the target scene
        string targetScene = PlayerPrefs.GetString("TargetScene", "MainScene");
        Debug.Log("Starting async load of scene: " + targetScene);

        sceneLoadOperation = SceneManager.LoadSceneAsync(targetScene);
        sceneLoadOperation.allowSceneActivation = false;
    }

    void Update()
    {
        timeElapsed += Time.deltaTime;

        if (sceneLoadOperation != null)
        {
            // Update progress
            float progress = Mathf.Clamp01(sceneLoadOperation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            if (progressText != null)
            {
                progressText.text = Mathf.RoundToInt(progress * 100) + "%";
            }

            // Check if the scene is ready to activate
            if (progress >= 1f)
            {
                isSceneReady = true;
            }

            // Once the scene is ready and minimum time has passed, show the prompt
            if (isSceneReady && timeElapsed >= minimumDisplayTime && !waitingForKeyPress)
            {
                ShowPressKeyPrompt();
            }

            // Check for any key press after showing the prompt
            if (waitingForKeyPress && Input.anyKeyDown)
            {
                ActivateScene();
            }
        }
    }

    void ShowPressKeyPrompt()
    {
        if (pressKeyText != null)
        {
            pressKeyText.gameObject.SetActive(true);
            pressKeyText.text = "Press any key to continue...";
        }
        waitingForKeyPress = true;
    }

    void ActivateScene()
    {
        if (sceneLoadOperation != null)
        {
            sceneLoadOperation.allowSceneActivation = true;
            Debug.Log("Scene activated: " + PlayerPrefs.GetString("TargetScene", "MainScene"));
        }
    }
}
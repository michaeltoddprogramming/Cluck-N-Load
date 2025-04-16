using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingScreenController : MonoBehaviour
{
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private float minimumDisplayTime = 2f; 

    private AsyncOperation sceneLoadOperation;
    private float timeElapsed = 0f;
    private bool isSceneReady = false;

    void Start()
    {
        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
        if (progressText != null)
        {
            progressText.text = "0%";
        }

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
            float progress = Mathf.Clamp01(sceneLoadOperation.progress / 0.9f);

            if (progressBar != null)
            {
                progressBar.value = progress;
            }
            if (progressText != null)
            {
                progressText.text = Mathf.RoundToInt(progress * 100) + "%";
            }

            if (progress >= 1f)
            {
                isSceneReady = true;
            }

            if (isSceneReady && timeElapsed >= minimumDisplayTime)
            {
                ActivateScene();
            }
        }
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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    [Header("Visuals")]
    public Material skyboxMaterial;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI pressKeyText;
    [SerializeField] private RectTransform chickenIcon;
    [SerializeField] private TextMeshProUGUI tipsText; // New: tips text UI

    [Header("Timing & Smoothness")]
    [SerializeField] private float minimumDisplayTime = 2f;
    [SerializeField] private float barSmoothSpeed = 5f;

    [Header("Skybox Animation")]
    [SerializeField] private float skyboxRotationSpeed = 0.3f;

    [Header("Chicken Animation")]
    [SerializeField] private float bobAmount = 5f;
    [SerializeField] private float bobSpeed = 4f;

    [Header("Tips Settings")]
    [SerializeField] private float tipChangeInterval = 4f; // How often to change tips
    [TextArea]
    [SerializeField] private string[] tipsList = new string[]
    {
        "Press SPACE to jump higher!",
        "You can sprint by holding SHIFT.",
        "Chickens love sunflower seeds!",
        "Remember to save your progress often.",
        "Enemies drop rare loot at night.",
        "Explore every corner for hidden treasures!"
    };

    [SerializeField] private Image fadeImage;
[SerializeField] private float fadeDuration = 1.5f;


    private AsyncOperation sceneLoadOperation;
    private float displayedProgress = 0f;
    private float timeElapsed = 0f;
    private bool waitingForKeyPress = false;
    private int assetsLoaded = 0;
    private int totalAssetsToLoad = 0;
    private float tipTimer = 0f;

    void Start()
    {
        if (skyboxMaterial != null)
            RenderSettings.skybox = skyboxMaterial;

        if (pressKeyText != null)
            pressKeyText.gameObject.SetActive(false);

        if (tipsText != null && tipsList.Length > 0)
            tipsText.text = tipsList[Random.Range(0, tipsList.Length)];

        StartCoroutine(LoadEverything());
    }

    void Update()
    {
        // Skybox subtle rotation
        if (skyboxMaterial != null)
            RenderSettings.skybox.SetFloat("_Rotation", Time.time * skyboxRotationSpeed);

        // Smooth progress bar animation
        if (progressBar != null)
            progressBar.value = Mathf.Lerp(progressBar.value, displayedProgress, Time.deltaTime * barSmoothSpeed);

        // Animate chicken bobbing
        if (chickenIcon != null)
        {
            float bobOffset = Mathf.Sin(Time.time * bobSpeed) * bobAmount;
            chickenIcon.anchoredPosition = new Vector2(chickenIcon.anchoredPosition.x, bobOffset);
        }

        // Rotate tips periodically
        if (!waitingForKeyPress && tipsText != null && tipsList.Length > 0)
        {
            tipTimer += Time.deltaTime;
            if (tipTimer >= tipChangeInterval)
            {
                tipTimer = 0f;
                tipsText.text = tipsList[Random.Range(0, tipsList.Length)];
            }
        }

        // Pulse "Press any key" text
        if (waitingForKeyPress && pressKeyText != null)
        {
            float alpha = Mathf.Abs(Mathf.Sin(Time.time * 2f));
            pressKeyText.color = new Color(pressKeyText.color.r, pressKeyText.color.g, pressKeyText.color.b, alpha);

            if (Input.anyKeyDown)
                ActivateScene();
        }
    }

    IEnumerator LoadEverything()
    {
        // Load assets from Resources
        GameObject[] prefabsToPreload = Resources.LoadAll<GameObject>("Prefabs");
        AudioClip[] musicToPreload = Resources.LoadAll<AudioClip>("Sounds");
        totalAssetsToLoad = prefabsToPreload.Length + musicToPreload.Length;

        foreach (var prefab in prefabsToPreload)
        {
            assetsLoaded++;
            UpdateProgress();
            yield return null;
        }

        foreach (var clip in musicToPreload)
        {
            assetsLoaded++;
            UpdateProgress();
            yield return null;
        }

        // Load target scene
        string targetScene = PlayerPrefs.GetString("TargetScene", "MainScene");
        sceneLoadOperation = SceneManager.LoadSceneAsync(targetScene);
        sceneLoadOperation.allowSceneActivation = false;

        while (!sceneLoadOperation.isDone)
        {
            float sceneProgress = Mathf.Clamp01(sceneLoadOperation.progress / 0.9f);
            UpdateProgress(sceneProgress);
            if (sceneProgress >= 1f)
            {
                break;
            }
            yield return null;
        }

        while (timeElapsed < minimumDisplayTime)
        {
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        ShowPressKeyPrompt();
    }

    void UpdateProgress(float sceneProgress = 0f)
    {
        float assetProgress = totalAssetsToLoad > 0 ? (float)assetsLoaded / totalAssetsToLoad : 1f;
        float combinedProgress = Mathf.Clamp01((assetProgress + sceneProgress) / 2f);

        displayedProgress = combinedProgress;

        if (progressText != null)
            progressText.text = Mathf.RoundToInt(combinedProgress * 100) + "%";
    }

    void ShowPressKeyPrompt()
    {
        if (pressKeyText != null)
        {
            pressKeyText.gameObject.SetActive(true);
            pressKeyText.text = "Press any key to continue... (Yes, even that one)";
        }
        waitingForKeyPress = true;
    }

void ActivateScene()
{
    if (sceneLoadOperation != null)
    {
        StartCoroutine(FadeAndActivate());
    }
}

IEnumerator FadeAndActivate()
{
    float elapsed = 0f;
    Color c = fadeImage.color;

    // Fade alpha from 0 → 1
    while (elapsed < fadeDuration)
    {
        elapsed += Time.deltaTime;
        float alpha = Mathf.Clamp01(elapsed / fadeDuration);
        fadeImage.color = new Color(c.r, c.g, c.b, alpha);
        yield return null;
    }

    // Finally allow the scene activation
    sceneLoadOperation.allowSceneActivation = true;
}

}

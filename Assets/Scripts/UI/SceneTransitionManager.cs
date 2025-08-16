using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("Fade Settings")]
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 1f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            SceneManager.sceneLoaded += OnSceneLoaded;

            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, 1); // Start fully black

            StartCoroutine(FadeIn()); // Fade in at the very beginning
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadSceneWithLoading(string targetScene)
    {
        StartCoroutine(FadeOutAndLoad(targetScene));
    }

    private IEnumerator FadeOutAndLoad(string targetScene)
    {
        // Fade to black
        yield return StartCoroutine(FadeOut());

        // Save target scene so LoadingScene knows what to load
        PlayerPrefs.SetString("TargetScene", targetScene);

        // Switch to loading scene
        SceneManager.LoadScene("LoadingScene");
    }

    private IEnumerator FadeOut()
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
    }

    private IEnumerator FadeIn()
    {
        float elapsed = 0f;
        Color c = fadeImage.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = 1f - Mathf.Clamp01(elapsed / fadeDuration);
            fadeImage.color = new Color(c.r, c.g, c.b, alpha);
            yield return null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (fadeImage != null)
        {
            // Ensure we start black (carry over from previous scene)
            fadeImage.color = new Color(0, 0, 0, 1);

            // Fade into the new scene
            StartCoroutine(FadeIn());
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
}

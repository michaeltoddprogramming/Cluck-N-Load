using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("SceneTransitionManager initialized: " + gameObject.name);
        }
        else
        {
            Destroy(gameObject);
            Debug.Log("Duplicate SceneTransitionManager destroyed: " + gameObject.name);
        }
    }

    public void LoadSceneWithLoading(string targetScene)
    {
        PlayerPrefs.SetString("TargetScene", targetScene);
        SceneManager.LoadScene("LoadingScene");
    }
}
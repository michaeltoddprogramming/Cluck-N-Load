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
            }
        else
        {
            Destroy(gameObject);
            }
    }

    public void LoadSceneWithLoading(string targetScene)
    {
        PlayerPrefs.SetString("TargetScene", targetScene);
        SceneManager.LoadScene("LoadingScene");
    }
}
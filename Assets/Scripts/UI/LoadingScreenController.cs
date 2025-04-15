using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreenController : MonoBehaviour
{
    void Start()
    {
        // Simulate a loading delay (you can replace this with actual async loading)
        Invoke("LoadTargetScene", 2f); // 2-second delay to show the loading screen
    }

    void LoadTargetScene()
    {
        string targetScene = PlayerPrefs.GetString("TargetScene", "MainScene");
        SceneManager.LoadScene(targetScene);
    }
}
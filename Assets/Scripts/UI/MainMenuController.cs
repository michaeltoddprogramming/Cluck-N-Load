using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour {
    public void OnPlayButtonClicked()
    {
        // Load the LoadingScene, which will then load MainScene
        SceneTransitionManager.Instance.LoadSceneWithLoading("MainScene");
    }

    public void OnQuitButtonClicked()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
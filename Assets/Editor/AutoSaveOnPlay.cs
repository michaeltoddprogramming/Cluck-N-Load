using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class AutoSaveOnPlay
{
    static AutoSaveOnPlay()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                UnityEngine.Debug.Log("Auto-saving before entering Play mode...");
                EditorSceneManager.SaveOpenScenes();
                AssetDatabase.SaveAssets();
            }
        };
    }
}

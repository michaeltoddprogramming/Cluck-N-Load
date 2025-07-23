using UnityEngine;
using UnityEngine.SceneManagement;

namespace UIAnimation.Addon
{
    public class LoadNextLevel : MonoBehaviour
    {
        public void LoadNewLevel(string Name)
        {
            SceneManager.LoadScene(Name);
        }
    }
}
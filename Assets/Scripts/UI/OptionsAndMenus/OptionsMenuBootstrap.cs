using UnityEngine;

public class OptionsMenuBootstrap : MonoBehaviour
{
    public GameObject optionsMenuPrefab; // Assign in Inspector

    void Awake()
    {
        if (OptionsMenuController.Instance == null)
            Instantiate(optionsMenuPrefab);
    }

        public void ShowMenu()
    {
        Debug.Log("Options menu shown");
        gameObject.SetActive(true);
    }
}
using UnityEngine;

public class Temp : MonoBehaviour
{
    // TutorialManager tutorialManager;
    // TutorialUIPrefab tutScript;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // tutorialManager = new TutorialManager();
        // tutScript = FindObjectOfType<TutorialUIPrefab>();

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void onShop()
    {
        TutorialManager.Instance.CheckStep2();
    }

    public void playControls()
    {
        TutorialManager.Instance.CheckStep7();
    }
}

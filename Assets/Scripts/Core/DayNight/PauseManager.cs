using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private ShopUIManager shopManager;
    private NightManager nightManager;
    // private ItemHoverPanel itemHoverPanel;
    private bool isPaused = false;
    // private static bool hasExplainedTimeControls = false;

    void Start()
    {
        nightManager = GetComponent<NightManager>();
        // itemHoverPanel = GetComponent<ItemHoverPanel>();

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Pause"))
        {
            if (isPaused)
            {
                playGame();
                // shopManager.enableShop();
                if (OptionsMenuController.Instance != null)
                    OptionsMenuController.Instance.HideMenu();
            }
            else
            {
                pauseGame();
                // shopManager.disableShop();
                if (OptionsMenuController.Instance != null)
                    OptionsMenuController.Instance.ShowMenu();
            }
        }
    }

    public void pausedOrPlay()
    {
        if (isPaused)
        {
            playGame();
        }
        else
        {
            pauseGame();
        }
    }

    public void pauseGame()
    {
        Debug.Log("Pausing game...");
        if (!isPaused)
        {
            nightManager.pauseTime();
            shopManager.disableShop();
            // itemHoverPanel.HideImmediate();
            ItemHoverPanel.Instance.HideImmediate();
            
            // Close price panel if it's open to prevent overlap with pause menu
            PricePanelUI pricePanel = FindFirstObjectByType<PricePanelUI>();
            if (pricePanel != null && pricePanel.gameObject.activeInHierarchy)
            {
                pricePanel.ClosePanel();
            }
            
            isPaused = true;
        }
    }

    public void playGame()
    {
        Debug.Log("Resuming game...");
        if (isPaused)
        {
            nightManager.playTime();
            nightManager.PlayEnableShop();
            isPaused = false;
        }
        // if (isPaused)
        // {
        //     Time.timeScale = 1;
        //     isPaused = false;

        // }
    }

    public bool getIsPaused()
    {
        Debug.Log("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!IsPaused called, returning: " + isPaused);
        return isPaused;
    }

    // private void TriggerTimeControlsExplained()
    // {
    //     if (!hasExplainedTimeControls)
    //     {
    //         hasExplainedTimeControls = true;
    //         if (TutorialManager.Instance != null)
    //             TutorialManager.Instance.OnConditionMet(TutorialCondition.TimeControlsExplained);
    //     }
    // }
}

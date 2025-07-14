using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PauseManager : MonoBehaviour
{
    [SerializeField] private ShopUIManager shopManager;
    private bool isPaused = false;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetButtonDown("Pause"))
        {
            if(isPaused)
            {
                playGame();
                shopManager.enableShop();
            }
            else
            {
                pauseGame();
                shopManager.disableShop();
            }        
        }
    }

    public void pausedOrPlay()
    {
        if(isPaused)
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
        if(!isPaused)
        {
            Time.timeScale = 0;
            isPaused = true;
        }
    }

    public void playGame()
    {
        if(isPaused)
        {
            Time.timeScale = 1;
            isPaused = false;
        }
    }
}

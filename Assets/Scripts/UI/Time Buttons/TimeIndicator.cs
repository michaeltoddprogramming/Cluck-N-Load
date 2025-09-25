using UnityEngine;
using UnityEngine.UI;

public class TimeIndicator : MonoBehaviour
{
    [SerializeField] private Image pause;
    [SerializeField] private Image play;
    [SerializeField] private Image fastForward;
    [SerializeField] private Sprite selectedPause;
    [SerializeField] private Sprite normalPause;
    [SerializeField] private Sprite selectedPlay;
    [SerializeField] private Sprite normalPlay;
    [SerializeField] private Sprite selectedFast;
    [SerializeField] private Sprite normalFast;

    public void exchangeTimeIcon(string change)
    {
        switch (change)
        {
            case "pause":
                pause.sprite = selectedPause;
                play.sprite = normalPlay;
                fastForward.sprite = normalFast;
                break;
            case "play":
                play.sprite = selectedPlay;
                pause.sprite = normalPause;
                fastForward.sprite = normalFast;
                break;
            case "fast":
                fastForward.sprite = selectedFast;
                play.sprite = normalPlay;
                pause.sprite = normalPause;
                break;
        }
    }
}

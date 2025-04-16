using UnityEngine;
using UnityEngine.UI;

public class DayNight : MonoBehaviour
{
    public Text dayNightText;
    private bool isDay = true;

    public void ToggleDayNight()
    {
        isDay = !isDay;
        dayNightText.text = isDay ? "Day" : "Night";
    }
}

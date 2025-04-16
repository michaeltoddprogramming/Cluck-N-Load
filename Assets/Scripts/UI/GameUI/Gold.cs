using UnityEngine;
using UnityEngine.UI;

public class Gold : MonoBehaviour
{
    public Text goldText;
    public int gold = 0;

    void Start()
    {
        UpdateGoldUI();
    }

    public void AddGold(int amount)
    {
        gold += amount;
        UpdateGoldUI();
    }

    public void SpendGold(int amount)
    {
        if (gold >= amount)
        {
            gold -= amount;
            UpdateGoldUI();
        }
    }

    void UpdateGoldUI()
    {
        goldText.text = "GOLD: " + gold.ToString();
    }
}

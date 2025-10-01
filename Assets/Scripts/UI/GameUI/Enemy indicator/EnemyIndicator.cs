using UnityEngine;

public class EnemyIndicator : MonoBehaviour
{
    [SerializeField] private GameObject wolf;
    [SerializeField] private GameObject raccoon;
    [SerializeField] private GameObject boar;
    [SerializeField] private GameObject bear;

     public void MakeWolfVisible()
    {
        wolf.SetActive(true);
        raccoon.SetActive(false);
        boar.SetActive(false);
        bear.SetActive(false);
    }
    public void MakeRacoonVisible()
    {
        raccoon.SetActive(true);
        wolf.SetActive(false);
        boar.SetActive(false);
        bear.SetActive(false);
    }
    public void MakeBoarVisible()
    {
        boar.SetActive(true);
        wolf.SetActive(false);
        raccoon.SetActive(false);
        bear.SetActive(false);
    }
    public void MakeBearVisible()
    {
        bear.SetActive(true);
        wolf.SetActive(false);
        raccoon.SetActive(false);
        boar.SetActive(false);
    }
    
    public void MakeAllEnemiesVisible()
    {
        Debug.Log("EnemyIndicator.MakeAllEnemiesVisible() called - setting all enemies active");
        wolf.SetActive(true);
        raccoon.SetActive(true);
        boar.SetActive(true);
        bear.SetActive(true);
        Debug.Log($"Enemy states: wolf={wolf.activeSelf}, raccoon={raccoon.activeSelf}, boar={boar.activeSelf}, bear={bear.activeSelf}");
    }
}

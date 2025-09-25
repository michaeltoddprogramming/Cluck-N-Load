using UnityEngine;

public class CoinRotator : MonoBehaviour
{
    [SerializeField] private float rotateSpeed = 180f;
    
    void Update()
    {
        transform.Rotate(0, 0, rotateSpeed * Time.deltaTime);
    }
}
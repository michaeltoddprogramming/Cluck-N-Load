// Add to any GameObject that exists in your scene
using UnityEngine;

public class CoinAnimationTester : MonoBehaviour 
{
    void Update() 
    {
        // Press C to test
        if (Input.GetKeyDown(KeyCode.C)) {
            Debug.Log("Testing coin animation");
            
            // Get camera position for test
            Vector3 testPos = Camera.main.transform.position + Camera.main.transform.forward * 5;
            
            // Test with MoneyManager
            if (MoneyManager.Instance != null) {
                Debug.Log("Using MoneyManager.AddMoney");
                MoneyManager.Instance.AddMoney(100, testPos);
            }
            // Direct test with CoinAnimation
            else if (CoinAnimation.Instance != null) {
                Debug.Log("Using CoinAnimation.PlayCoinAnimation directly");
                CoinAnimation.Instance.PlayCoinAnimation(testPos, 100);
            }
            else {
                Debug.LogError("No MoneyManager or CoinAnimation found!");
            }
        }
    }
}
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ProductionBoosts : MonoBehaviour
{
    private bool isBoosted = false;
    private float[] productsBoosted = new float[5] { 0, 0, 0, 0, 0 };

    public void SetBoosted(float[] amounts)
    {
        isBoosted = true;

        productsBoosted[0] = 0; // chicken
        productsBoosted[1] = 0; // cow
        productsBoosted[2] = 0; // sheep
        productsBoosted[3] = 0; // goat
        productsBoosted[4] = 0; // pig

        for (int k = 0; k < productsBoosted.Length; k++)
        {
            if (amounts[k] > 0)
            {
                // Debug.LogError($"Invalid amount for product {products[i]}: {amounts[i]}");
                productsBoosted[k] = amounts[k];

            }
            
            Debug.Log($"Here is the amount boosted for {productsBoosted[k]}: {productsBoosted[k]}");
        }
    }

    public float[] GetBoostedProducts()
    {
        if (!isBoosted)
        {
            Debug.LogWarning("No products are boosted.");
            return new float[0];
        }

        return productsBoosted;
    }
}
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class ProductionBoosts : MonoBehaviour
{
    private bool isBoosted = false;
    [SerializeField] private StructureData chicken;
    [SerializeField] private StructureData cow;
    [SerializeField] private StructureData sheep;
    [SerializeField] private StructureData goat;
    [SerializeField] private StructureData pig;
    [System.NonSerialized]
    private float[] productsBoosted;
    [System.NonSerialized]
    private int[] productPrices;

    void Awake()
    {
        // Initialize arrays to ensure they're not null/corrupted
        productsBoosted = new float[5] { 1f, 1f, 1f, 1f, 1f };
        productPrices = new int[5];
        
        // Initialize with fallback values in case StructureData is not assigned
        productPrices[0] = chicken?.moneyPerProduct ?? 50; // Chicken fallback
        productPrices[1] = cow?.moneyPerProduct ?? 100;    // Cow fallback
        productPrices[2] = sheep?.moneyPerProduct ?? 75;   // Sheep fallback
        productPrices[3] = goat?.moneyPerProduct ?? 80;    // Goat fallback
        productPrices[4] = pig?.moneyPerProduct ?? 90;     // Pig fallback

        Debug.Log($"ProductionBoosts initialized - Prices: [{productPrices[0]}, {productPrices[1]}, {productPrices[2]}, {productPrices[3]}, {productPrices[4]}]");
        Debug.Log($"ProductionBoosts initialized - Boosts: [{productsBoosted[0]}, {productsBoosted[1]}, {productsBoosted[2]}, {productsBoosted[3]}, {productsBoosted[4]}]");
    }

    public void SetBoosted(float[] amounts)
    {
        Debug.Log($"SetBoosted called with amounts: [{amounts[0]}, {amounts[1]}, {amounts[2]}, {amounts[3]}, {amounts[4]}]");
        Debug.Log($"SetBoosted call stack: {System.Environment.StackTrace}");
        Debug.Log($"Before SetBoosted - current productsBoosted: [{productsBoosted[0]}, {productsBoosted[1]}, {productsBoosted[2]}, {productsBoosted[3]}, {productsBoosted[4]}]");
        
        isBoosted = true;

        productsBoosted[0] = 1; // chicken
        productsBoosted[1] = 1; // cow
        productsBoosted[2] = 1; // sheep
        productsBoosted[3] = 1; // goat
        productsBoosted[4] = 1; // pig

        // Check if all amounts are zero - this might indicate a bug
        bool allZero = true;
        for (int i = 0; i < amounts.Length; i++)
        {
            if (amounts[i] != 0)
            {
                allZero = false;
                break;
            }
        }
        
        if (allZero)
        {
            Debug.LogError($"SetBoosted called with ALL ZERO values! This will break production. Converting to 1.0f. GameObject: {gameObject.name}");
        }

        for (int k = 0; k < productsBoosted.Length; k++)
        {
            if (amounts != null && k < amounts.Length)
            {
                // Don't allow zero values - treat them as 1.0f (normal production)
                if (amounts[k] > 0)
                {
                    productsBoosted[k] = amounts[k];
                    Debug.Log($"Set productsBoosted[{k}] = {amounts[k]}");
                }
                else
                {
                    productsBoosted[k] = 1.0f; // Force normal production instead of zero
                    Debug.Log($"Forced productsBoosted[{k}] = 1.0f (was {amounts[k]})");
                }
            }
            else
            {
                // If amounts array is too short, keep default value of 1
                Debug.Log($"Keeping default productsBoosted[{k}] = {productsBoosted[k]}");
            }
        }

        Debug.Log($"Final productsBoosted: [{productsBoosted[0]}, {productsBoosted[1]}, {productsBoosted[2]}, {productsBoosted[3]}, {productsBoosted[4]}]");
    }

    public float[] GetBoostedProducts()
    {
        Debug.Log($"GetBoostedProducts called on {this.gameObject.name} - isBoosted: {isBoosted}");
        
        if (!isBoosted)
        {
            Debug.LogWarning("No products are boosted, returning default multipliers.");
            return new float[5] { 1f, 1f, 1f, 1f, 1f }; // Return default multipliers instead of empty array
        }

        // Check if array got corrupted somehow
        bool hasZeros = false;
        for (int i = 0; i < productsBoosted.Length; i++)
        {
            if (productsBoosted[i] == 0)
            {
                hasZeros = true;
                productsBoosted[i] = 1.0f; // Fix it immediately
            }
        }
    
        Debug.Log($"GetBoostedProducts returning: [{productsBoosted[0]}, {productsBoosted[1]}, {productsBoosted[2]}, {productsBoosted[3]}, {productsBoosted[4]}]");
        return productsBoosted;
    }

    public int[] GetProductPrices()
    {
        return productPrices;
    }
}
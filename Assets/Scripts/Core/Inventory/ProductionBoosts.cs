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
    private float[] productsBoosted = new float[5] { 1, 1, 1, 1, 1 };
    private int[] productPrices = new int[5] { 10, 20, 18, 30, 15 };

    void Awake()
    {
        productPrices[0] = chicken.moneyPerProduct;
        productPrices[1] = cow.moneyPerProduct;
        productPrices[2] = sheep.moneyPerProduct;
        productPrices[3] = goat.moneyPerProduct;
        productPrices[4] = pig.moneyPerProduct;
    }

    public void SetBoosted(float[] amounts)
    {
        isBoosted = true;

        productsBoosted[0] = 1; // chicken
        productsBoosted[1] = 1; // cow
        productsBoosted[2] = 1; // sheep
        productsBoosted[3] = 1; // goat
        productsBoosted[4] = 1; // pig

        for (int k = 0; k < productsBoosted.Length; k++)
        {
            if (amounts[k] > 0)
            {
                // Debug.LogError($"Invalid amount for product {products[i]}: {amounts[i]}");
                productsBoosted[k] = amounts[k];

            }

            // Debug.Log($"Here is the amount boosted for {productsBoosted[k]}: {productsBoosted[k]}");
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

    public int[] GetProductPrices()
    {
        return productPrices;
    }
}
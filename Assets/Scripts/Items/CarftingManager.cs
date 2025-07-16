using System;
using System.Collections.Generic;
using UnityEngine;

public class CraftingManager : MonoBehaviour
{
    public List<RecipeData> recipes; // Populate this list in the Inspector with RecipeData ScriptableObjects

    public InventoryManager inventoryManager; // Assign in Inspector

    public bool CanCraft(RecipeData recipe)
    {
        if (inventoryManager == null) return false;

        foreach (RecipeData.Ingredient ingredient in recipe.ingredients)
        {
            if (!inventoryManager.HasItem(ingredient.item, ingredient.quantity))
            {
                return false;
            }
        }
        return true;
    }

    public bool Craft(RecipeData recipe)
    {
        if (!CanCraft(recipe))
        {
            Debug.LogWarning($"Cannot craft {recipe.product.item.name}: Missing ingredients.");
            return false;
        }

        // Consume ingredients
        foreach (RecipeData.Ingredient ingredient in recipe.ingredients)
        {
            inventoryManager.RemoveItem(ingredient.item, ingredient.quantity);
        }

        // Add product
        inventoryManager.AddItem(recipe.product.item, recipe.product.quantity);
        Debug.Log($"Successfully crafted {recipe.product.item.name}!");
        return true;
    }

    // We would typically have a UI method to display recipes and trigger Craft
    // public void OpenCraftingUI() { /* ... display recipes ... */ }
}
// RecipeData.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "HappyHarvest/Crafting/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Tooltip("A descriptive name for the crafted item.")]
        public string RecipeName;

        [System.Serializable]
        public struct Ingredient
        {
            [Tooltip("The item required as an ingredient.")]
            public Item Item;
            [Tooltip("The quantity of this item required.")]
            [Min(1)] public int Quantity;
        }

        [System.Serializable]
        public struct Product
        {
            [Tooltip("The item produced by this recipe.")]
            public Item Item;
            [Tooltip("The quantity of the item produced.")]
            [Min(1)] public int Quantity;
        }

        [Tooltip("List of ingredients required to craft this recipe.")]
        public List<Ingredient> Ingredients;
        [Tooltip("The item and quantity produced by this recipe.")]

        public string GetIngredientsString()
        {
            string s = "";
            foreach (var ing in Ingredients)
            {
                if (ing.Item != null)
                {
                    s += $"{ing.Quantity} {ing.Item.DisplayName}, ";
                }
            }
            // Remove trailing comma and space
            if (s.Length > 2)
            {
                s = s.Substring(0, s.Length - 2);
            }
            return s;
        }
    }
}
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewRecipe", menuName = "Scriptable Objects/Recipe Data")]
public class RecipeData : ScriptableObject
{
    public string recipeName;

    [System.Serializable]
    public struct Ingredient
    {
        public ItemData item;
        public int quantity;
    }

    [System.Serializable]
    public struct Product
    {
        public ItemData item;
        public int quantity;
    }

    public List<Ingredient> ingredients;
    public Product product;
}
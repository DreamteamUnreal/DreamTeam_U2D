// CraftingManager.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
	public class CraftingManager : MonoBehaviour
	{
		[Tooltip("List of all recipes known by this crafting manager.")]
		public List<RecipeData> KnownRecipes; // Assign your RecipeData assets here in the Inspector

		// This reference is likely already set via PlayerController
		// public InventorySystem PlayerInventory; // If not already linked via PlayerController

		private PlayerController m_Player; // Reference to the PlayerController

		private void Awake()
		{
			// Get reference to the PlayerController on the same GameObject
			m_Player = GetComponent<PlayerController>();
			if (m_Player == null)
			{
				Debug.LogError("CraftingManager: PlayerController not found on this GameObject. Crafting will not work.");
				enabled = false; // Disable script if essential component is missing
			}
			// If PlayerInventory was a separate field, you'd assign it here:
			// if (PlayerInventory == null) PlayerInventory = m_Player.Inventory;
		}

		/// <summary>
		/// Checks if the player has all the required ingredients for a given recipe.
		/// </summary>
		/// <param name="recipe">The RecipeData to check.</param>
		/// <returns>True if all ingredients are present in the player's inventory, false otherwise.</returns>
		public bool CanCraft(RecipeData recipe)
		{
			if (recipe == null || m_Player == null || m_Player.Inventory == null)
			{
				Debug.LogWarning("CraftingManager: Recipe, Player, or Player Inventory is null. Cannot check crafting ability.");
				return false;
			}

			foreach (RecipeData.Ingredient ingredient in recipe.Ingredients)
			{
				if (ingredient.Item == null)
				{
					Debug.LogWarning($"CraftingManager: Recipe '{recipe.RecipeName}' has a null ingredient item. Skipping check.");
					return false; // Or continue if you want to allow crafting with other valid ingredients
				}

				// Use the InventorySystem's HasItem method
				if (!m_Player.Inventory.HasItem(ingredient.Item, ingredient.Quantity))
				{
					Debug.Log($"Cannot craft {recipe.RecipeName}: Missing {ingredient.Quantity} {ingredient.Item.DisplayName}.");
					return false;
				}
			}
			return true;
		}
	}
}
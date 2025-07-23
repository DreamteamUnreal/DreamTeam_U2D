//CraftingStation.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
    public class CraftingStation : InteractiveObject
    {
        [Tooltip("The recipe that this specific crafting station can create.")]
        public RecipeData RecipeToCraft; // Assign a specific RecipeData asset here

        [Tooltip("Optional: If this station can craft multiple items, list them here.")]
        public List<RecipeData> MultipleRecipes; // For a more complex crafting table

        // You'd typically have a UI popup for crafting. For now, we'll just attempt to craft directly.
        // In a real game, InteractedWith() would open the UI, and the UI would call CraftingManager.Craft().

        public override void InteractedWith()
        {
            if (GameManager.Instance == null || GameManager.Instance.Player == null || GameManager.Instance.Player.CraftingManager == null)
            {
                Debug.LogWarning("CraftingStation: GameManager, Player, or Player's CraftingManager is null.");
                return;
            }

            // --- Simple Direct Crafting Example ---
            // If you only want this station to craft one specific item:
            if (RecipeToCraft != null)
            {
                Debug.Log($"Attempting to craft {RecipeToCraft.RecipeName} at {gameObject.name}...");
            }
            // --- More Complex: Open a Crafting UI ---
            // If you have a UI that shows multiple recipes, you'd do something like:
            // UIHandler.OpenCraftingUI(GameManager.Instance.Player.craftingManager.GetCraftableRecipes());
            // This would require a CraftingUIHandler and corresponding UI elements.
            else if (MultipleRecipes != null && MultipleRecipes.Count > 0)
            {
                Debug.Log("CraftingStation: This station supports multiple recipes. Implement UI to select one.");
            }
            else
            {
                Debug.LogWarning($"CraftingStation on {gameObject.name}: No RecipeToCraft or MultipleRecipes assigned.");
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f); // Visualize the station
            if (RecipeToCraft != null)
            {
#if UNITY_EDITOR
                UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Craft: {RecipeToCraft.RecipeName}");
#endif
            }
        }
    }
}
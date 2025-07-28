// Item.cs
using UnityEngine;

namespace HappyHarvest
{
	/// <summary>
	/// Base ScriptableObject for all items in the HappyHarvest game.
	/// Derived classes (e.g., Tool, Consumable, Seed, Product) will inherit from this.
	/// </summary>
	// You can add a CreateAssetMenu attribute here if you want to create generic Item assets directly
	// [CreateAssetMenu(fileName = "NewBaseItem", menuName = "HappyHarvest/Items/Base Item")]
	public class Item : ScriptableObject, IDatabaseEntry
	{
		[Tooltip("A unique identifier for this item. MUST BE UNIQUE. Used for saving/loading and lookup.")]
		public string ItemID;
		// --- Implement the IDatabaseEntry.ID property ---
		public string ID => ItemID; // IDatabaseEntry.ID now returns the ItemID

		public string Key => throw new System.NotImplementedException();

		[Tooltip("The name displayed in UI.")]
		public string DisplayName;

		[Tooltip("The sprite used for inventory icons and UI display.")]
		public Sprite ItemSprite;

		[Tooltip("The prefab instantiated when this item is equipped by the player (e.g., axe model).")]
		public GameObject VisualPrefab;

		[Tooltip("The name of the animator trigger on the player for using this item (e.g., 'UseAxe').")]
		public string PlayerAnimatorTriggerUse;

		[Tooltip("The price to buy this item from a shop.")]
		public int BuyPrice;

		[Tooltip("The maximum number of this item that can stack in one inventory slot.")]
		[Min(1)] // Minimum stack size is 1
		public int MaxStackSize = 99; // Default max stack size for most items

		/// <summary>
		/// Virtual method to check if this item can be used at a specific target cell.
		/// Override in derived classes (e.g., Tool, Seed) for specific logic.
		/// </summary>
		/// <param name="targetCell">The grid cell being targeted.</param>
		/// <returns>True if the item can be used, false otherwise.</returns>
		public virtual bool CanUse(Vector3Int targetCell)
		{
			// By default, a base item cannot be used on a cell
			return false;
		}

		/// <summary>
		/// Virtual method for general item usage (e.g., consuming a food item).
		/// Override in derived classes (e.g., Consumable).
		/// </summary>
		public virtual void Use()
		{
			Debug.Log($"Used generic item: {DisplayName}");
		}
	}
}
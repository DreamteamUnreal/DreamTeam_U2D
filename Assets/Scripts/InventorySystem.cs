// InventorySystem.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
	// InventorySlot class: Represents a single slot in the inventory
	// Marked [System.Serializable] so it can be seen and edited in the Inspector
	[System.Serializable]
	public class InventorySlot
	{
		[Tooltip("The Item ScriptableObject held in this slot.")]
		public Item Item;
		[Tooltip("The current quantity (stack size) of the item in this slot.")]
		[Min(0)] public int StackSize;

		// Constructor for convenience
		public InventorySlot(Item item, int stackSize)
		{
			Item = item;
			StackSize = stackSize;
		}

		// Clears the slot
		public void Clear()
		{
			Item = null;
			StackSize = 0;
		}

		// Checks if the slot is empty
		public bool IsEmpty()
		{
			return Item == null || StackSize <= 0;
		}
	}

	/// <summary>
	/// Manages the player's inventory, including adding, removing, equipping, and using items.
	/// </summary>
	public class InventorySystem : MonoBehaviour
	{
		// Define the fixed size of the inventory
		public const int InventorySize = 10; // Adjust this as needed for your UI layout

		[Tooltip("The array of inventory slots.")]
		public InventorySlot[] Entries = new InventorySlot[InventorySize];

		[Tooltip("The current index of the equipped item (-1 if nothing equipped).")]
		public int EquippedItemIdx = -1;

		// Public accessor for the currently equipped item
		public Item EquippedItem => EquippedItemIdx >= 0 && EquippedItemIdx < Entries.Length && !Entries[EquippedItemIdx].IsEmpty()
					? Entries[EquippedItemIdx].Item
					: null;

		// Called when the script is loaded or a value is changed in the Inspector
		private void OnValidate()
		{
			// Ensure the Entries array always matches InventorySize
			if (Entries.Length != InventorySize)
			{
				InventorySlot[] newEntries = new InventorySlot[InventorySize];
				for (int i = 0; i < Mathf.Min(Entries.Length, InventorySize); i++)
				{
					newEntries[i] = Entries[i];
				}
				// Initialize new slots if the array was expanded
				for (int i = Entries.Length; i < InventorySize; i++)
				{
					newEntries[i] = new InventorySlot(null, 0);
				}
				Entries = newEntries;
			}
		}

		// Initializes the inventory (e.g., fills empty slots)
		public void Init()
		{
			for (int i = 0; i < Entries.Length; i++)
			{
				if (Entries[i] == null)
				{
					Entries[i] = new InventorySlot(null, 0); // Ensure all slots are initialized
				}
			}
			// Equip the first valid item if nothing is equipped
			if (EquippedItemIdx == -1)
			{
				EquipNext();
			}
			UIHandler.UpdateInventory(this); // Update UI after initialization
		}

		// --- Core Inventory Logic ---

		/// <summary>
		/// Adds an item to the inventory. Handles stacking.
		/// </summary>
		/// <param name="newItem">The item to add.</param>
		/// <returns>True if the item was added, false if inventory is full.</returns>
		public bool AddItem(Item newItem)
		{
			if (newItem == null)
			{
				return false;
			}

			// Try to stack with existing items
			foreach (InventorySlot slot in Entries)
			{
				// Assuming Item has a MaxStackSize property
				if (slot.Item == newItem && slot.StackSize < newItem.MaxStackSize)
				{
					slot.StackSize++;
					UIHandler.UpdateInventory(this);
					return true;
				}
			}

			// Find an empty slot
			for (int i = 0; i < Entries.Length; i++)
			{
				if (Entries[i].IsEmpty()) // Empty slot
				{
					Entries[i] = new InventorySlot(newItem, 1); // Create new slot with 1 item
					UIHandler.UpdateInventory(this);
					return true;
				}
			}

			Debug.LogWarning($"Inventory full! Could not add {newItem.DisplayName}.");
			return false; // Inventory is full
		}

		/// <summary>
		/// Checks if the inventory contains a specified quantity of an item.
		/// </summary>
		/// <param name="itemToCheck">The item to check for.</param>
		/// <param name="requiredQuantity">The quantity needed.</param>
		/// <returns>True if the required quantity is present, false otherwise.</returns>
		public bool HasItem(Item itemToCheck, int requiredQuantity)
		{
			if (itemToCheck == null || requiredQuantity <= 0)
			{
				return false;
			}

			int currentCount = 0;
			foreach (InventorySlot slot in Entries)
			{
				if (slot.Item == itemToCheck) // Compare by Item reference
				{
					currentCount += slot.StackSize;
				}
			}
			return currentCount >= requiredQuantity;
		}

		/// <summary>
		/// Removes a specified quantity of an item from the inventory.
		/// </summary>
		/// <param name="itemToRemove">The item to remove.</param>
		/// <param name="quantityToRemove">The quantity to remove.</param>
		/// <returns>True if the items were successfully removed, false otherwise.</returns>
		public bool RemoveItem(Item itemToRemove, int quantityToRemove)
		{
			if (itemToRemove == null || quantityToRemove <= 0)
			{
				return false;
			}

			if (!HasItem(itemToRemove, quantityToRemove))
			{
				Debug.LogWarning($"InventorySystem: Not enough {itemToRemove.DisplayName} to remove {quantityToRemove}.");
				return false; // Ensure we have enough first
			}

			int remainingToRemove = quantityToRemove;

			// Iterate through slots and remove items
			for (int i = 0; i < Entries.Length && remainingToRemove > 0; i++)
			{
				InventorySlot slot = Entries[i];
				if (slot.Item == itemToRemove)
				{
					if (slot.StackSize <= remainingToRemove)
					{
						remainingToRemove -= slot.StackSize;
						slot.Clear(); // Clear the slot if all items are removed
					}
					else
					{
						slot.StackSize -= remainingToRemove;
						remainingToRemove = 0; // All removed
					}
				}
			}

			UIHandler.UpdateInventory(this); // Update UI after removal
			return remainingToRemove == 0; // Return true if all items were removed
		}

		/// <summary>
		/// Checks if a given item and quantity can fit into the inventory (considering stacking and empty slots).
		/// </summary>
		/// <param name="item">The item to check for.</param>
		/// <param name="count">The quantity to check for.</param>
		/// <returns>True if the items can fit, false otherwise.</returns>
		public bool CanFitItem(Item item, int count)
		{
			if (item == null || count <= 0)
			{
				return true;
			}

			int remainingCount = count;

			// First, try to fill existing stacks of the same item
			foreach (InventorySlot slot in Entries)
			{
				if (slot.Item == item && slot.StackSize < item.MaxStackSize)
				{
					int spaceInStack = item.MaxStackSize - slot.StackSize;
					int amountToFill = Mathf.Min(remainingCount, spaceInStack);
					remainingCount -= amountToFill;
					if (remainingCount <= 0)
					{
						return true; // All items fit
					}
				}
			}

			// If items still remain, check for empty slots
			int emptySlots = 0;
			foreach (InventorySlot slot in Entries)
			{
				if (slot.IsEmpty())
				{
					emptySlots++;
				}
			}

			// Calculate how many new full stacks are needed for the remaining items
			int newStacksNeeded = Mathf.CeilToInt((float)remainingCount / item.MaxStackSize);

			return emptySlots >= newStacksNeeded;
		}

		/// <summary>
		/// Removes a specific quantity of an item from a given inventory index.
		/// This is typically used for selling from a specific slot.
		/// </summary>
		/// <param name="inventoryIndex">The index of the slot.</param>
		/// <param name="count">The quantity to remove.</param>
		/// <returns>The actual count of items removed.</returns>
		public int Remove(int inventoryIndex, int count)
		{
			if (inventoryIndex < 0 || inventoryIndex >= Entries.Length || Entries[inventoryIndex].IsEmpty())
			{
				return 0;
			}

			InventorySlot slot = Entries[inventoryIndex];
			int actualRemoved = Mathf.Min(count, slot.StackSize);

			slot.StackSize -= actualRemoved;
			if (slot.StackSize <= 0)
			{
				slot.Clear();
			}

			UIHandler.UpdateInventory(this);
			return actualRemoved;
		}

		// --- Equipping Logic ---

		/// <summary>
		/// Equips the item at the specified index.
		/// </summary>
		/// <param name="index">The inventory slot index to equip.</param>
		public void EquipItem(int index)
		{
			if (index < 0 || index >= Entries.Length)
			{
				return;
			}

			EquippedItemIdx = index;
			UIHandler.UpdateInventory(this); // Update UI to highlight equipped item
			Debug.Log($"Equipped: {EquippedItem?.DisplayName ?? "Nothing"}");
		}

		/// <summary>
		/// Equips the next item in the inventory.
		/// </summary>
		public void EquipNext()
		{
			int originalIdx = EquippedItemIdx;
			int newIdx = EquippedItemIdx;

			// Find next non-empty slot
			do
			{
				newIdx = (newIdx + 1) % InventorySize;
				if (!Entries[newIdx].IsEmpty())
				{
					EquipItem(newIdx);
					return;
				}
			} while (newIdx != originalIdx); // Loop until we find a non-empty or return to start

			// If all slots are empty or only the currently equipped one is non-empty
			EquippedItemIdx = -1; // Nothing equipped
			UIHandler.UpdateInventory(this);
		}

		/// <summary>
		/// Equips the previous item in the inventory.
		/// </summary>
		public void EquipPrev()
		{
			int originalIdx = EquippedItemIdx;
			int newIdx = EquippedItemIdx;

			// Find previous non-empty slot
			do
			{
				newIdx = (newIdx - 1 + InventorySize) % InventorySize; // Handle negative wrap-around
				if (!Entries[newIdx].IsEmpty())
				{
					EquipItem(newIdx);
					return;
				}
			} while (newIdx != originalIdx);

			EquippedItemIdx = -1; // Nothing equipped
			UIHandler.UpdateInventory(this);
		}

		/// <summary>
		/// Uses the currently equipped item at a given target cell.
		/// </summary>
		/// <param name="targetCell">The target cell for item usage.</param>
#pragma warning disable IDE0060 // Remove unused parameter
		public void UseEquippedObject(Vector3Int targetCell)
#pragma warning restore IDE0060 // Remove unused parameter
		{
			if (EquippedItem == null)
			{
				return;
			}
			else
			{
				Debug.Log($"Equipped item {EquippedItem.DisplayName} has no defined usage logic.");
			}

			// After using, re-evaluate equipped item, especially if it was consumed
			if (EquippedItem == null || Entries[EquippedItemIdx].IsEmpty())
			{
				EquipNext(); // Try to equip the next item if current one is gone
			}
			UIHandler.UpdateInventory(this); // Always update UI after any changes
		}

		// --- Save/Load Logic ---

		/// <summary>
		/// Saves the current inventory state into a list of InventorySaveData.
		/// </summary>
		/// <param name="data">The list to populate with save data.</param>
		public void Save(ref List<InventorySaveData> data)
		{
			data.Clear();
			for (int i = 0; i < Entries.Length; i++)
			{
				InventorySlot slot = Entries[i];
				data.Add(new InventorySaveData
				{
					ItemID = slot.Item != null ? slot.Item.ItemID : "", // Save by ItemID
					StackSize = slot.StackSize
				});
			}
			data.Add(new InventorySaveData // Save equipped item index as a special entry
			{
				ItemID = "EQUIPPED_ITEM_INDEX", // Special ID
				StackSize = EquippedItemIdx
			});
		}

		/// <summary>
		/// Loads inventory state from a list of InventorySaveData.
		/// </summary>
		/// <param name="data">The list containing save data.</param>
		public void Load(List<InventorySaveData> data)
		{
			// Clear current inventory
			for (int i = 0; i < Entries.Length; i++)
			{
				Entries[i].Clear();
			}
			EquippedItemIdx = -1; // Reset equipped index

			int currentSlotIndex = 0;
			foreach (InventorySaveData entryData in data)
			{
				if (entryData.ItemID == "EQUIPPED_ITEM_INDEX")
				{
					EquippedItemIdx = entryData.StackSize; // StackSize used to store index
					continue;
				}

				// Find the actual Item ScriptableObject by its ItemID
				Item loadedItem = GameManager.Instance.ItemManager.GetItemByID(entryData.ItemID); // Assuming GameManager has an ItemManager
				if (loadedItem != null && currentSlotIndex < Entries.Length)
				{
					Entries[currentSlotIndex] = new InventorySlot(loadedItem, entryData.StackSize);
					currentSlotIndex++;
				}
				else if (loadedItem == null)
				{
					Debug.LogWarning($"InventorySystem: Item with ID '{entryData.ItemID}' not found during load. Skipping.");
				}
			}

			// After loading, ensure equipped item is valid and update UI
			if (EquippedItemIdx == -1 || EquippedItem == null)
			{
				EquipNext(); // Try to equip something if previous equipped is invalid
			}
			UIHandler.UpdateInventory(this);
			Debug.Log("Inventory loaded.");
		}
	}

	// You will need to define your InventorySaveData struct somewhere in your project.
	// Example:
	[System.Serializable]
	public struct InventorySaveData
	{
		public string ItemID;
		public int StackSize;
	}
}
// ItemManager.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // Required for .ToDictionary() if you use it, though I've used a foreach loop for clarity

namespace HappyHarvest
{
    /// <summary>
    /// Manages all Item ScriptableObjects in the game.
    /// It builds a dictionary for quick lookup of items by their unique ItemID,
    /// which is essential for saving/loading inventory.
    /// </summary>
    public class ItemManager : MonoBehaviour
    {
        [Tooltip("Drag all your Item ScriptableObjects (e.g., Apple, Flour, Axe, CarrotSeed, Pie) here in the Inspector.")]
        public List<Item> AllItems; // Assign all your Item ScriptableObjects here

        // Private dictionary for efficient lookup by ItemID
        private Dictionary<string, Item> m_ItemDictionary;

        void Awake()
        {
            InitializeItemDictionary();
        }

        // It's good practice to have a separate initialization method for clarity
        private void InitializeItemDictionary()
        {
            m_ItemDictionary = new Dictionary<string, Item>();

            if (AllItems == null || AllItems.Count == 0)
            {
                Debug.LogWarning("ItemManager: 'AllItems' list is empty or null. No items will be available for lookup. " +
                "Please populate the 'AllItems' list in the Inspector with your Item ScriptableObjects.");
                return;
            }

            foreach (Item item in AllItems)
            {
                if (item != null)
                {
                    if (!string.IsNullOrEmpty(item.ItemID))
                    {
                        if (!m_ItemDictionary.ContainsKey(item.ItemID))
                        {
                            m_ItemDictionary.Add(item.ItemID, item);
                        }
                        else
                        {
                            Debug.LogWarning($"ItemManager: Duplicate ItemID '{item.ItemID}' found for item '{item.DisplayName}'. " +
                            $"Only the first instance will be used. Please ensure all ItemIDs are unique.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"ItemManager: An item '{item.name}' in 'AllItems' list has an empty or null ItemID. " +
                        $"It will not be added to the lookup dictionary.");
                    }
                }
                else
                {
                    Debug.LogWarning("ItemManager: A null entry found in 'AllItems' list. Please remove it.");
                }
            }

            Debug.Log($"ItemManager: Initialized with {m_ItemDictionary.Count} unique items.");
        }

        /// <summary>
        /// Retrieves an Item ScriptableObject by its unique ItemID.
        /// This is the primary method for getting item references during loading or other lookups.
        /// </summary>
        /// <param name="itemID">The unique ID of the item to retrieve.</param>
        /// <returns>The Item ScriptableObject if found, otherwise null.</returns>
        public Item GetItemByID(string itemID)
        {
            // Ensure the dictionary is initialized before attempting to use it
            if (m_ItemDictionary == null || m_ItemDictionary.Count == 0)
            {
                // Attempt to re-initialize if it's null (e.g., if Awake order was off)
                // This is a fallback; proper Script Execution Order is preferred.
                InitializeItemDictionary();
                if (m_ItemDictionary == null || m_ItemDictionary.Count == 0)
                {
                    Debug.LogError($"ItemManager: Dictionary is not initialized or empty. Cannot get item by ID '{itemID}'.");
                    return null;
                }
            }

            if (m_ItemDictionary.TryGetValue(itemID, out Item item))
            {
                return item;
            }
            // Only warn if the itemID is not empty, as empty IDs are often intentional (e.g., empty inventory slots)
            if (!string.IsNullOrEmpty(itemID))
            {
                Debug.LogWarning($"ItemManager: Item with ID '{itemID}' not found in dictionary. " +
                "Ensure the ItemID is correct and the item is assigned to the 'AllItems' list in the Inspector.");
            }
            return null;
        }
    }
}
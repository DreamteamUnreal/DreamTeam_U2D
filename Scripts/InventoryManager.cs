using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public List<InventorySlot> inventorySlots = new List<InventorySlot>();
    public int maxSlots = 10; // Or dynamically managed

    public delegate void OnInventoryChanged();
    public event OnInventoryChanged onInventoryChangedCallback; // For UI updates

    [System.Serializable]
    public class InventorySlot
    {
        public ItemData itemData;
        public int quantity;
    }

    public bool AddItem(ItemData item, int amount = 1)
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.itemData == item)
            {
                slot.quantity += amount;
                onInventoryChangedCallback?.Invoke();
                return true;
            }
        }

        if (inventorySlots.Count < maxSlots)
        {
            inventorySlots.Add(new InventorySlot { itemData = item, quantity = amount });
            onInventoryChangedCallback?.Invoke();
            return true;
        }

        Debug.LogWarning("Inventory is full!");
        return false;
    }

    public bool RemoveItem(ItemData item, int amount = 1)
    {
        for (int i = 0; i < inventorySlots.Count; i++)
        {
            if (inventorySlots[i].itemData == item)
            {
                if (inventorySlots[i].quantity >= amount)
                {
                    inventorySlots[i].quantity -= amount;
                    if (inventorySlots[i].quantity <= 0)
                    {
                        inventorySlots.RemoveAt(i);
                    }
                    onInventoryChangedCallback?.Invoke();
                    return true;
                }
            }
        }
        return false; // Not enough items
    }

    public int GetItemQuantity(ItemData item)
    {
        foreach (InventorySlot slot in inventorySlots)
        {
            if (slot.itemData == item)
            {
                return slot.quantity;
            }
        }
        return 0;
    }

    public bool HasItem(ItemData item, int requiredQuantity)
    {
        return GetItemQuantity(item) >= requiredQuantity;
    }
}
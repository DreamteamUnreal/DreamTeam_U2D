using System;
using UnityEngine;

public class CollectibleItem : MonoBehaviour, Interactable
{
    public ItemData itemData; // ScriptableObject for item details
    public int quantity = 1;

    public void Interact(GameObject interactor)
    {
        PlayerController player = interactor.GetComponent<PlayerController>();
        if (player != null && player.inventoryManager != null)
        {
            player.inventoryManager.AddItem(itemData, quantity);
            Debug.Log($"Collected {quantity} {itemData.itemName}");
            Destroy(gameObject); // Remove item from scene
        }
    }
}
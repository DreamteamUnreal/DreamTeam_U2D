// CollectibleItem.cs
using UnityEngine;

namespace HappyHarvest
{
	// CollectibleItem should inherit from InteractiveObject, as your PlayerController
	// interacts with InteractiveObject components.
	public class CollectibleItem : InteractiveObject
	{
		[Tooltip("The ScriptableObject representing the item this collectible gives.")]
		public Item itemData; // This MUST be HappyHarvest.Item type
		[Tooltip("The quantity of the item this collectible gives.")]
		public int quantity = 1;

		public override void InteractedWith()
		{
			if (GameManager.Instance == null || GameManager.Instance.Player == null)
			{
				Debug.LogWarning("CollectibleItem: GameManager.Instance.Player is null. Cannot collect item.");
				return;
			}

			if (itemData == null)
			{
				Debug.LogWarning($"CollectibleItem on {gameObject.name}: itemData is null. Cannot add to inventory.");
				Destroy(gameObject);
				return;
			}

			// Your Player.AddItem takes a single Item object and adds 1 to stack.
			// So, we call it 'quantity' times.
			bool collectedAny = false;
			for (int i = 0; i < quantity; i++)
			{
				if (GameManager.Instance.Player.AddItem(itemData)) // Assuming AddItem adds 1 item to stack
				{
					collectedAny = true;
				}
				else
				{
					Debug.Log($"Player inventory is full. Could not collect all {itemData.DisplayName}.");
					break; // Stop if inventory is full
				}
			}

			if (collectedAny)
			{
				Debug.Log($"Collected {quantity} of {itemData.DisplayName} from {gameObject.name}");
				Destroy(gameObject); // Remove item from scene after collection
			}
			else
			{
				Debug.Log($"Failed to collect {itemData.DisplayName}. Inventory might be full or itemData null.");
			}
		}

		private void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawWireSphere(transform.position, 0.2f);
			if (itemData != null)
			{
				// To draw text in Gizmos, you need UnityEditor.Handles, which is editor-only.
				// This will cause errors if not wrapped in #if UNITY_EDITOR
#if UNITY_EDITOR
				UnityEditor.Handles.Label(transform.position + (Vector3.up * 0.5f), itemData.DisplayName);
#endif
			}
		}
	}
}
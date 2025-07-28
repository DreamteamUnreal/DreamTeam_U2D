// Consumable.cs
using UnityEngine;

namespace HappyHarvest
{
	// Make sure this inherits from your base Item class
	[CreateAssetMenu(fileName = "NewConsumable", menuName = "HappyHarvest/Items/Consumable")]
	public class Consumable : Item
	{
		[Tooltip("Amount of health/stamina restored when consumed.")]
		public int RestoreAmount = 10;

		// Override the generic Use method for consumables
		public override void Use()
		{
			Debug.Log($"Consumed {DisplayName}. Restored {RestoreAmount} health/stamina.");
			// Implement what happens when consumed (e.g., restore player health/stamina)
			// Example: GameManager.Instance.Player.RestoreHealth(RestoreAmount);
		}
	}
}
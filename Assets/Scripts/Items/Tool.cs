// Tool.cs
using UnityEngine;

namespace HappyHarvest
{
	// Make sure this inherits from your base Item class
	[CreateAssetMenu(fileName = "NewTool", menuName = "HappyHarvest/Items/Tool")]
	public class Tool : Item
	{
		[Tooltip("Does this tool get consumed (e.g., loses durability, or is a one-time use) when used?")]
		public bool ConsumeOnUse = false; // Example: true for a seed, false for an axe

		// Example: Override CanUse for tools that interact with specific cells
		public override bool CanUse(Vector3Int targetCell)
		{
			// Implement specific tool usage logic here (e.g., can only use axe on trees)
			// For now, a generic example:
			Debug.Log($"Checking if {DisplayName} can be used at {targetCell}");
			// Example: return GameManager.Instance.Terrain.CanInteract(targetCell, this);
			return true; // For now, assume a tool can always be used if equipped
		}

		// Example: Implement the actual usage effect
		public void Use(Vector3Int targetCell)
		{
			Debug.Log($"Using {DisplayName} at {targetCell}");
			// Implement what happens when the tool is used (e.g., chop tree, mine rock)
			// Example: GameManager.Instance.Terrain.Interact(targetCell, this);
		}
	}
}
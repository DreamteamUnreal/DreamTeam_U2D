// Seed.cs
using UnityEngine;

namespace HappyHarvest
{
    // Make sure this inherits from your base Item class
    [CreateAssetMenu(fileName = "NewSeed", menuName = "HappyHarvest/Items/Seed")]
    public class Seed : Item
    {
        [Tooltip("The prefab of the plant that grows from this seed.")]
        public GameObject PlantPrefab;

        // Override CanUse for planting logic
        public override bool CanUse(Vector3Int targetCell)
        {
            // Implement specific planting conditions (e.g., can only plant on tilled soil)
            // Example: return GameManager.Instance.Terrain.IsTilled(targetCell);
            Debug.Log($"Checking if {DisplayName} can be planted at {targetCell}");
            // For now, assume a seed can always be planted if equipped
            return true;
        }

        // Implement the planting action
        public void Use(Vector3Int targetCell)
        {
            Debug.Log($"Planting {DisplayName} at {targetCell}");
            // Example: GameManager.Instance.Terrain.PlantSeed(targetCell, PlantPrefab);
            // Instantiate the plant prefab at the cell center
            if (PlantPrefab != null && GameManager.Instance != null && GameManager.Instance.Terrain != null && GameManager.Instance.Terrain.Grid != null)
            {
                Vector3 worldPos = GameManager.Instance.Terrain.Grid.GetCellCenterWorld(targetCell);
                Instantiate(PlantPrefab, worldPos, Quaternion.identity);
            }
            else
            {
                Debug.LogWarning($"Seed: Cannot plant {DisplayName}. PlantPrefab, GameManager, Terrain, or Grid is null.");
            }
        }
    }
}
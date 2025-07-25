using UnityEngine;

namespace HappyHarvest
{
	[CreateAssetMenu(fileName = "WaterCan", menuName = "2D Farming/Items/Water Can")]
	public class WaterCan : Item
	{
		public override bool CanUse(Vector3Int target)
		{
			return GameManager.Instance.Terrain.IsTilled(target);
		}
	}
}
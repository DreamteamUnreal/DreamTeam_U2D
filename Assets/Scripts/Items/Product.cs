//Product.cs
using UnityEngine;

namespace HappyHarvest
{
	[CreateAssetMenu(menuName = "2D Farming/Items/Product")]
	public class Product : Item
	{
		public int SellPrice = 1;
		internal object transform;

		public override bool CanUse(Vector3Int target)
		{
			return true;
		}
	}
}

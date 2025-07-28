using UnityEngine;

namespace HappyHarvest
{
	[CreateAssetMenu(menuName = "2D Farming/Items/Basket")]
	public class Basket : Item
	{
		public override bool CanUse(Vector3Int target)
		{
			TerrainManager.CropData data = GameManager.Instance.Terrain.GetCropDataAt(target);
			if (!GameManager.Instance.Player.CanFitInInventory(data.GrowingCrop.Produce,
					data.GrowingCrop.ProductPerHarvest))
			{
				return false;
			}

			Crop product = GameManager.Instance.Terrain.HarvestAt(target);

			if (product != null)
			{
				for (int i = 0; i < product.ProductPerHarvest; ++i)
				{
					_ = GameManager.Instance.Player.AddItem(product.Produce);
				}
				return true;
			}
			return data != null && data.GrowingCrop != null && Mathf.Approximately(data.GrowthRatio, 1.0f);
		}
	}
}
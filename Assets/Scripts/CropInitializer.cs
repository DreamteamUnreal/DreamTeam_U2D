using System;
using UnityEngine;

namespace HappyHarvest
{
	/// <summary>
	/// Helper class for the demo purpose. Will auto plant the given crops at the given growth stage in the given cell.
	/// </summary>
	public class CropInitializer : MonoBehaviour
	{
		[Serializable]
		public struct CropInitData
		{
			public Vector2Int Cell;
			public Crop CropToPlant;
			public int StartingStage;
		}

		public CropInitData[] InitList;

		public void Start()
		{
			TerrainManager terrain = GameManager.Instance.Terrain;

			foreach (CropInitData initData in InitList)
			{
				Vector3Int target = (Vector3Int)initData.Cell;
				if (terrain.IsTillable(target) || terrain.IsTilled(target))
				{
					TerrainManager.CropData data = GameManager.Instance.Terrain.GetCropDataAt(target);
					if (data == null)
					{//we don't have a crop here yet so let's add the one we want to initialize.
						terrain.TillAt(target);
						terrain.WaterAt(target);
						terrain.PlantAt(target, initData.CropToPlant);

						terrain.OverrideGrowthStage(target, initData.StartingStage);
					}
				}
			}
		}
	}
}
using System;

namespace UnityEngine.Tilemaps
{
	[ExecuteInEditMode]
	public class TilePaletteHelper : MonoBehaviour
	{
		private Tilemap tilemap;
		private TileBase[] tiles;

#if UNITY_EDITOR
		private void Start()
		{
			tilemap = GetComponentInChildren<Tilemap>();
			tilemap.CompressBounds();
		}

		private void OnDrawGizmos()
		{
			if (tilemap == null)
			{
				return;
			}

			BoundsInt bounds = tilemap.cellBounds;
			int boundsSize = bounds.size.x * bounds.size.y * bounds.size.z;
			if (tiles == null || boundsSize != tiles.Length)
			{
				Array.Resize(ref tiles, boundsSize);
			}

			_ = tilemap.GetTilesBlockNonAlloc(bounds, tiles);

			int i = 0;
			foreach (Vector3Int position in bounds.allPositionsWithin)
			{
				TileBase tile = tiles[i++];
				if (tile == null)
				{
					continue;
				}

				if (tilemap.GetSprite(position) != null)
				{
					continue;
				}

				Vector3 localPosition = tilemap.CellToLocalInterpolated(position + tilemap.tileAnchor);
				Gizmos.DrawIcon(localPosition, TilePaletteIconsPreference.GetTexturePath(tile.GetType()));
			}
		}
#endif
	}
}
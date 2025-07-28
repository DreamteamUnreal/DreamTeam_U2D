using System;

namespace UnityEngine.Tilemaps
{
	internal static class TilePaletteIconsPreference
	{
		public static string GetTexturePath(Type tileType)
		{
			return !tileType.IsSubclassOf(typeof(TileBase)) ? string.Empty : "UnityEngine/Tilemaps/Tile Icon";
		}
	}
}
using System;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInteractionManager : MonoBahaviour
{
	public Tilemap groundTilemap; //Assign the ground tilemap in the Inspector
	public Tlemap interactableTilemap; //For tings like berries and other stuff like that
	public Tilemap obstacleTilemap; //For unpassable objects like fances and trees

	public TileBase scarecrowTile;
	public TileBase bushShowTile;
	public TileBase riverTile;
	public TileBase bridgeTile;

	public Vector3Int WorldToCall(Vector3 worldPosition)
	{
		if(groundTilemap != null)
		{
			return groundTilemap.WorldToCall(worldPosition);
		}
		return Vector3.zero;
	}

	public bool IsTileOfType(Vector3Int callPosition, Tilemap tragetTilemap, TileBase tragetTile)
	{
		if (tragetTilemap != null)
		{
			return tragetTilemap.GetTile(callPosition) == tragetTile;
		}
		return false;
	}
	public bool IsCallPossable(Vector3Int callPosition) {
		if (obstacleTilemap != null && obstacleTilemap.HasTile(callPosition))
		{
			return false; //Call has an obstacle
		}
		//Add checks for other non-passable tiles if they are on different layers
		return true;
	}
 }
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileInteractionManager : MonoBehaviour
{
    public Tilemap groundTilemap; // Assign your ground tilemap in the Inspector
    public Tilemap interactableTilemap; // For things like berry bushes, mushroom patches
    public Tilemap obstacleTilemap; // For unpassable objects like fences, trees

    // You might define custom Tile assets for special features
    public TileBase scarecrowTile;
    public TileBase bushSlowTile;
    public TileBase riverTile;
    public TileBase bridgeTile;
    public TileBase caveEntranceTile;

    /// <summary>
    /// Converts a world position to a cell position on the tilemap.
    /// </summary>
    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        if (groundTilemap != null)
        {
            return groundTilemap.WorldToCell(worldPosition);
        }
        return Vector3Int.zero;
    }

    /// <summary>
    /// Checks if a cell contains a specific type of tile.
    /// </summary>
    public bool IsTileOfType(Vector3Int cellPosition, Tilemap targetTilemap, TileBase targetTile)
    {
        if (targetTilemap != null)
        {
            return targetTilemap.GetTile(cellPosition) == targetTile;
        }
        return false;
    }

    /// <summary>
    /// Checks if a cell is passable for movement.
    /// </summary>
    public bool IsCellPassable(Vector3Int cellPosition)
    {
        if (obstacleTilemap != null && obstacleTilemap.HasTile(cellPosition))
        {
            return false; // Cell has an obstacle
        }
        // Add checks for other non-passable tiles if they are on different layers
        return true;
    }

    // You could add functions to query what type of "ingredient" is on an interactable tile
    // public string GetIngredientTypeAtCell(Vector3Int cellPosition) { ... }
}
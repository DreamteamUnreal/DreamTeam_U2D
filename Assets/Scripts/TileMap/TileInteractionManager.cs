// TileInteractionManager.cs
using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic; // Added for List

public class TileInteractionManager : MonoBehaviour
{
    public Tilemap groundTilemap; // Assign ground tilemap in the Inspector
    public Tilemap interactableTilemap; // For things like berry bushes, mushroom patches
    public Tilemap obstacleTilemap; // For unpassable objects like fences, trees

    // Might define custom Tile assets for special features
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
    /// Converts a cell position to a world position (center of the cell).
    /// </summary>
    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        if (groundTilemap != null)
        {
            return groundTilemap.GetCellCenterWorld(cellPosition);
        }
        return Vector3.zero;
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
        // Might add checks for other non-passable tiles if they are on different layers
        // For example, if a "deep water" tile is on groundTilemap and is impassable:
        // if (groundTilemap.GetTile(cellPosition) == deepWaterTile) return false;

        // Also check if the cell is within the bounds of your main tilemap
        if (groundTilemap != null && !groundTilemap.HasTile(cellPosition))
        {
            return false; // Not a valid ground tile
        }

        return true;
    }

    /// <summary>
    /// Gets all valid neighboring cells for pathfinding.
    /// </summary>
    public List<Vector3Int> GetNeighborCells(Vector3Int cell)
    {
        List<Vector3Int> neighbors = new List<Vector3Int>();

        // Check 8 directions (including diagonals for smoother movement, adjust if only cardinal is desired)
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0) continue; // Skip self

                Vector3Int neighborCell = new Vector3Int(cell.x + x, cell.y + y, cell.z);

                if (IsCellPassable(neighborCell))
                {
                    neighbors.Add(neighborCell);
                }
            }
        }
        return neighbors;
    }
}
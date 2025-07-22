using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;

    public Tilemap tilemap;
    public Tilemap obstacleTilemap;
    public float cellSize = 1f;

    private GridNode[,] grid;
    private int width, height;
    private Vector3Int origin;  // ÆðµãÆ«ÒÆ£¨cellBounds.min£©

    private void Awake()
    {
        Instance = this;
        GenerateGridFromTilemap();
    }

    void GenerateGridFromTilemap()
    {
        BoundsInt bounds = tilemap.cellBounds;
        origin = bounds.min;
        width = bounds.size.x;
        height = bounds.size.y;

        grid = new GridNode[width, height];

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
            {
                Vector3Int cellPos = new Vector3Int(origin.x + x, origin.y + y, 0);
                Vector2Int gridPos = new Vector2Int(x, y);

                bool walkable = !obstacleTilemap.HasTile(cellPos);
                grid[x, y] = new GridNode(gridPos, walkable);
            }
    }

    public GridNode GetNode(Vector2Int gridPos)
    {
        if (gridPos.x >= 0 && gridPos.y >= 0 && gridPos.x < width && gridPos.y < height)
            return grid[gridPos.x, gridPos.y];
        return null;
    }

    public Vector2 GridToWorld(Vector2Int gridPos)
    {
        Vector3Int cell = new Vector3Int(origin.x + gridPos.x, origin.y + gridPos.y, 0);
        return tilemap.GetCellCenterWorld(cell);
    }

    public Vector2Int WorldToGrid(Vector2 worldPos)
    {
        Vector3Int cell = tilemap.WorldToCell(worldPos);
        return new Vector2Int(cell.x - origin.x, cell.y - origin.y);
    }
}

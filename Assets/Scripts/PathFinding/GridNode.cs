using UnityEngine;

public class GridNode
{
    public Vector2Int Position;
    public bool Walkable;
    public int GCost;
    public int HCost;
    public GridNode Parent;

    public int FCost => GCost + HCost;

    public GridNode(Vector2Int pos, bool walkable)
    {
        Position = pos;
        Walkable = walkable;
    }
}
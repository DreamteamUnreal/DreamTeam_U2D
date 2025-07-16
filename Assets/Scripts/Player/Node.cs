// Node.cs
using UnityEngine;

public class Node
{
    public Vector3Int gridPosition; // The cell coordinates of this node
    public int gCost;               // Cost from the start node to this node
    public int hCost;               // Heuristic cost from this node to the end node
    public int fCost               // Total cost (gCost + hCost)
    {
        get { return gCost + hCost; }
    }
    public Node parent;             // The node that came before this one in the path

    public Node(Vector3Int _gridPosition, int _gCost, int _hCost, Node _parent)
    {
        gridPosition = _gridPosition;
        gCost = _gCost;
        hCost = _hCost;
        parent = _parent;
    }
}
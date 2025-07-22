using System.Collections.Generic;
using UnityEngine;

public class GridPathfinder : MonoBehaviour
{
    public static List<GridNode> FindPath(Vector2Int start, Vector2Int end)
    {
        GridManager grid = GridManager.Instance;

        GridNode startNode = grid.GetNode(start);
        GridNode endNode = grid.GetNode(end);

        if (startNode == null || endNode == null || !startNode.Walkable || !endNode.Walkable)
            return null;

        List<GridNode> openSet = new List<GridNode>();
        HashSet<GridNode> closedSet = new HashSet<GridNode>();

        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            GridNode current = openSet[0];
            for (int i = 1; i < openSet.Count; i++)
            {
                if (openSet[i].FCost < current.FCost ||
                    (openSet[i].FCost == current.FCost && openSet[i].HCost < current.HCost))
                {
                    current = openSet[i];
                }
            }

            openSet.Remove(current);
            closedSet.Add(current);

            if (current == endNode)
                return RetracePath(startNode, endNode);

            foreach (GridNode neighbor in GetNeighbors(current))
            {
                if (!neighbor.Walkable || closedSet.Contains(neighbor))
                    continue;

                int newCost = current.GCost + GetDistance(current, neighbor);
                if (newCost < neighbor.GCost || !openSet.Contains(neighbor))
                {
                    neighbor.GCost = newCost;
                    neighbor.HCost = GetDistance(neighbor, endNode);
                    neighbor.Parent = current;

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }
        }

        return null;
    }

    static List<GridNode> GetNeighbors(GridNode node)
    {
        List<GridNode> neighbors = new List<GridNode>();
        Vector2Int[] directions = {
            Vector2Int.up, Vector2Int.down,
            Vector2Int.left, Vector2Int.right
        };

        foreach (var dir in directions)
        {
            GridNode neighbor = GridManager.Instance.GetNode(node.Position + dir);
            if (neighbor != null)
                neighbors.Add(neighbor);
        }

        return neighbors;
    }

    static int GetDistance(GridNode a, GridNode b)
    {
        return Mathf.Abs(a.Position.x - b.Position.x) + Mathf.Abs(a.Position.y - b.Position.y);
    }

    static List<GridNode> RetracePath(GridNode start, GridNode end)
    {
        List<GridNode> path = new List<GridNode>();
        GridNode current = end;

        while (current != start)
        {
            path.Add(current);
            current = current.Parent;
        }

        path.Reverse();
        return path;
    }
}

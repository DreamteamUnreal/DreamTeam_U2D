// Pathfinding.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq; // For OrderBy

public class Pathfinding : MonoBehaviour
{
    public TileInteractionManager tileManager; // Assign in Inspector

    void Awake()
    {
        if (tileManager == null)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            tileManager = FindObjectOfType<TileInteractionManager>();
#pragma warning restore CS0618 // Type or member is obsolete
            if (tileManager == null)
            {
                Debug.LogError("Pathfinding: TileInteractionManager not found in scene!");
            }
        }
    }

    /// <summary>
    /// Finds a path from startCell to targetCell using the A* algorithm.
    /// </summary>
    /// <returns>A list of Vector3Int cells representing the path, or null if no path found.</returns>
    public List<Vector3Int> FindPath(Vector3Int startCell,
									Vector3Int targetCell)
	{
		if (tileManager != null)
		{
			// Check if start or target cells are impassable
			if (!tileManager.IsCellPassable(startCell) || !tileManager.IsCellPassable(targetCell))
			{
				Debug.LogWarning($"Pathfinding: Start ({startCell}) or Target ({targetCell}) cell is impassable.");
				return null;
			}

			// --- A* Algorithm Implementation ---
			List<Node> openSet = new List<Node>(); // Nodes to be evaluated
			HashSet<Vector3Int> closedSet = new HashSet<Vector3Int>(); // Nodes already evaluated

			// Dictionary to store the best path to a node (gridPosition -> Node)
			Dictionary<Vector3Int, Node> allNodes = new Dictionary<Vector3Int, Node>();

			Node startNode = new Node(startCell, 0, GetDistance(startCell, targetCell), null);
			openSet.Add(startNode);
			allNodes.Add(startNode.gridPosition, startNode);

			while (openSet.Count > 0)
			{
				// Get the node with the lowest F cost from the open set
				Node currentNode = openSet.OrderBy(node => node.fCost).First(); // Simple but less efficient than a Priority Queue

				openSet.Remove(currentNode);
				closedSet.Add(currentNode.gridPosition);

				// If we reached the target, reconstruct and return the path
				if (currentNode.gridPosition == targetCell)
				{
					return ReconstructPath(currentNode);
				}

				// Explore neighbors
				foreach (Vector3Int neighborCell in tileManager.GetNeighborCells(currentNode.gridPosition))
				{
					if (closedSet.Contains(neighborCell))
					{
						continue; // Already evaluated
					}

					int newGCost = currentNode.gCost + GetDistance(currentNode.gridPosition, neighborCell); // Cost to move to neighbor

					Node neighborNode;
					bool neighborExists = allNodes.TryGetValue(neighborCell, out neighborNode);

					if (!neighborExists || newGCost < neighborNode.gCost)
					{
						// Found a better path to this neighbor, or it's a new neighbor
						if (!neighborExists)
						{
							neighborNode = new Node(neighborCell, newGCost, GetDistance(neighborCell, targetCell), currentNode);
							allNodes.Add(neighborCell, neighborNode);
						}
						else
						{
							neighborNode.gCost = newGCost;
							neighborNode.parent = currentNode;
						}

						if (!openSet.Contains(neighborNode))
						{
							openSet.Add(neighborNode);
						}
					}
				}
			}

			Debug.LogWarning($"Pathfinding: No path found from {startCell} to {targetCell}.");
			return null; // No path found
		}

		Debug.LogError("Pathfinding: TileManager is not assigned!");
		return null;
	}

	/// <summary>
	/// Calculates the Manhattan distance (heuristic) between two cells.
	/// </summary>
	private int GetDistance(Vector3Int cellA, Vector3Int cellB)
    {
        int distX = Mathf.Abs(cellA.x - cellB.x);
        int distY = Mathf.Abs(cellA.y - cellB.y);

        // For diagonal movement, use a combination of straight and diagonal costs
        // If 8-directional movement is allowed, diagonal cost is sqrt(2) * 10 = 14
        // Straight cost is 10
        // return 14 * Mathf.Min(distX, distY) + 10 * Mathf.Abs(distX - distY); // For 8-directional
        return distX + distY; // For 4-directional (Manhattan) or simpler 8-directional
    }

    /// <summary>
    /// Reconstructs the path from the end node back to the start node.
    /// </summary>
    private List<Vector3Int> ReconstructPath(Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;

        while (currentNode != null)
        {
            path.Add(currentNode.gridPosition);
            currentNode = currentNode.parent;
        }
        path.Reverse(); // Path is built backwards, so reverse it
        return path;
    }
}
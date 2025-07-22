using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitMover : MonoBehaviour
{
    public float moveSpeed = 2f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector2 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int targetGrid = GridManager.Instance.WorldToGrid(mouseWorld);
            Vector2Int currentGrid = GridManager.Instance.WorldToGrid(transform.position);

            if (targetGrid == currentGrid)
                return;

            List<GridNode> path = GridPathfinder.FindPath(currentGrid, targetGrid);
            if (path != null && path.Count > 0)
            {
                StopAllCoroutines(); // 防止多个协程并行
                StartCoroutine(MoveAlongPath(path));
            }
        }
    }

    IEnumerator MoveAlongPath(List<GridNode> path)
    {
        foreach (GridNode node in path)
        {
            Vector2 target = GridManager.Instance.GridToWorld(node.Position);
            while (Vector2.Distance(transform.position, target) > 0.01f)
            {
                transform.position = Vector2.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
                yield return null;
            }

            transform.position = target; // Snap to exact
        }
    }
}

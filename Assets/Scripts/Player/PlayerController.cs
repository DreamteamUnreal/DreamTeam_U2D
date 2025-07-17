// PlayerController.cs
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic; // Added for List

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float stoppingDistance = 0.1f; // How close player needs to be to a target cell center

    private Rigidbody2D rb;
    private Pathfinding pathfinding; // Reference to the Pathfinding script
    private TileInteractionManager tileManager; // Reference to the TileInteractionManager

    public InventoryManager inventoryManager; // Assign in Inspector
    public CraftingManager craftingManager;   // Assign in Inspector

    private Interactable currentInteractable;

    private List<Vector3Int> currentPath; // The path the player is currently following
    private int pathIndex;                // Current index in the path

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
#pragma warning disable CS0618 // Type or member is obsolete
		Pathfinding pathfinding1 = FindObjectOfType<Pathfinding>();
#pragma warning restore CS0618 // Type or member is obsolete
		pathfinding = pathfinding1;
#pragma warning disable CS0618 // Type or member is obsolete
		tileManager = FindObjectOfType<TileInteractionManager>();
#pragma warning restore CS0618 // Type or member is obsolete

		if (pathfinding == null) Debug.LogError("PlayerController: Pathfinding script not found!");
        if (tileManager == null) Debug.LogError("PlayerController: TileInteractionManager not found!");
    }

    void FixedUpdate()
    {
        FollowPath();
    }

    // --- Input System Callbacks ---
    // Remove OnMove since we're using click-to-move
    // public void OnMove(InputAction.CallbackContext context) { ... }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (currentInteractable != null)
            {
                currentInteractable.Interact(this.gameObject);
            }
            else
            {
                // Logic for interacting with a crafting station if near it
                // craftingManager.OpenCraftingUI(); // Example
            }
        }
    }

    public void OnUseSpecialFeature(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Special Feature Used!");
            // Example: Find a Scarecrow and activate it (replace with more robust system)
            // Scarecrow scarecrow = FindObjectOfType<Scarecrow>();
            // if (scarecrow != null) scarecrow.ActivateScarecrow();
        }
    }

    // New: Handle mouse click for movement
    public void OnClickToMove(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            // Get mouse position in world coordinates
            Vector2 mouseWorldPos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            
            // Convert world position to cell position
            Vector3Int targetCell = tileManager.WorldToCell(mouseWorldPos);

            // Get player's current cell position
            Vector3Int playerCurrentCell = tileManager.WorldToCell(transform.position);

            // Find path using A*
            currentPath = pathfinding.FindPath(playerCurrentCell, targetCell);

            if (currentPath != null && currentPath.Count > 0)
            {
                pathIndex = 0; // Start at the beginning of the new path
            }
        }
    }

    private void FollowPath()
    {
        if (currentPath == null || pathIndex >= currentPath.Count)
        {
			// No path or path finished, stop movement
#pragma warning disable CS0618 // Type or member is obsolete
			rb.velocity = Vector2.zero;
#pragma warning restore CS0618 // Type or member is obsolete
			return;
        }

        Vector3Int targetCell = currentPath[pathIndex];
        Vector3 targetWorldPos = tileManager.CellToWorld(targetCell);

        // Calculate direction to the next point in the path
        Vector2 direction = (targetWorldPos - transform.position).normalized;

		// Move the player
#pragma warning disable CS0618 // Type or member is obsolete
		rb.velocity = direction * moveSpeed;
#pragma warning restore CS0618 // Type or member is obsolete

		// Check if player has reached the current target cell
		if (Vector2.Distance(transform.position, targetWorldPos) < stoppingDistance)
        {
            pathIndex++; // Move to the next point in the path
            if (pathIndex >= currentPath.Count)
            {
				// Reached end of path
#pragma warning disable CS0618 // Type or member is obsolete
				rb.velocity = Vector2.zero;
#pragma warning restore CS0618 // Type or member is obsolete
				currentPath = null; // Clear path
            }
        }
    }

    // --- Interaction Trigger (Same as before) ---
    void OnTriggerEnter2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null)
        {
            currentInteractable = interactable;
            // Optionally, show UI prompt
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && currentInteractable == interactable)
        {
            currentInteractable = null;
            // Optionally, hide UI prompt
        }
    }

    // Helper for debugging: Draw the path in the editor
    void OnDrawGizmos()
    {
        if (currentPath != null && tileManager != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < currentPath.Count; i++)
            {
                Gizmos.DrawSphere(tileManager.CellToWorld(currentPath[i]), 0.2f);
                if (i < currentPath.Count - 1)
                {
                    Gizmos.DrawLine(tileManager.CellToWorld(currentPath[i]), tileManager.CellToWorld(currentPath[i+1]));
                }
            }
        }
    }
}
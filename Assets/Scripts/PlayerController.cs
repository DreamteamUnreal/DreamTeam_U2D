using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBhaviour
{
	public float moveSpeed = 5f;
	private Rigidbody2D rb;
	private Vector2 moveInput;

	public InventoryManager inventoryManager; //Assign in Inspector
	public CraftingManager craftingManager; //Assign in Inspector

	private Interactable currentInteractable;

	void Awake()
	{
		rb = GetComponent<Rigidbody2D>();
	}

	public void OnMove(InputAction.CallbackContext context)
	{
		moveInput = context.ReadValue<Vector2>();
	}
    
	public void OnInteract(InputAction.CallbackContext context)
    {
        if (context.performed)
		{
			if (currentInteractable != null)
			{
				currentInteractable.Interact(this.gameObject);
			}
			else {
			}
		}
    }

    public void OnUseSpecialFeature(InputAction.CallbackContext context)
    {
        if (context.performed)
		{
			Debug.Log("Special Feature Used!");
		}
    }

	void OnTriggerEnter2D(Collider2D other)
	{
		Interactable interactable = other.GetComponent<Interactable>();
		if (interactable != null)
		{
			currentInteractable = interactable;
		}
	}

    void OnTriggerExit2D(Collider2D other)
    {
        Interactable interactable = other.GetComponent<Interactable>();
        if (interactable != null && currentInteractable == interactable)
        {
            currentInteractable = null;
        }
    }
}
//PlayerController.cs
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace HappyHarvest
{
	public class PlayerController : MonoBehaviour
	{
		public InputActionAsset InputAction;
		public float Speed = 4.0f;

		public SpriteRenderer Target;
		public Transform ItemAttachBone;

		public int Coins
		{
			get => m_Coins;
			set
			{
				m_Coins = value;
				UIHandler.UpdateCoins(Coins);
			}
		}

		// Pathfinding and Tilemap references
		private Pathfinding m_Pathfinding;
		private TileInteractionManager m_TileManager;
		private List<Vector3Int> m_CurrentPath; // The path the player is currently following
		private int m_PathIndex;                // Current index in the path
		public float StoppingDistance = 0.1f; // How close player needs to be to a target cell center
		private readonly InputAction m_ClickToMoveAction; // New Action for click-to-move input

		[field: SerializeField]
		public InventorySystem Inventory { get; }
		public Animator Animator { get; private set; }

		// --- THIS IS THE CRUCIAL PROPERTY THAT MUST BE PRESENT ---
		public CraftingManager CraftingManager { get; private set; } // <--- THIS IS IT!

		[SerializeField]
		private int m_Coins = 10;
		private Rigidbody2D m_Rigidbody;
		private InputAction m_MoveAction;
		private InputAction m_NextItemAction;
		private InputAction m_PrevItemAction;
		private InputAction m_UseItemAction;

		private Vector3 m_CurrentWorldMousePos;
		private Vector2 m_CurrentLookDirection;
		private Vector3Int m_CurrentTarget;

		private TargetMarker m_TargetMarker;

		private bool m_HasTarget = false;
		private bool m_IsOverUI = false;

		private bool m_CanControl = true;
		private InteractiveObject m_CurrentInteractiveTarget = null;
#pragma warning disable IDE0052 // Remove unread private members
		private readonly Collider2D[] m_CollidersCache = new Collider2D[8];
#pragma warning restore IDE0052 // Remove unread private members

		// Changed dictionary key from Item to string (ItemID) as per previous fix
		private readonly Dictionary<string, ItemInstance> m_ItemVisualInstance = new();

		private readonly int m_DirXHash = Animator.StringToHash("DirX");
		private readonly int m_DirYHash = Animator.StringToHash("DirY");
		private readonly int m_SpeedHash = Animator.StringToHash("Speed");

		private void Awake()
		{
			m_Rigidbody = GetComponent<Rigidbody2D>();
			Animator = GetComponentInChildren<Animator>();
			m_TargetMarker = Target.GetComponent<TargetMarker>();
			m_TargetMarker.Hide();

			gameObject.transform.SetParent(null);
			DontDestroyOnLoad(gameObject);

			// Get references to Pathfinding and TileInteractionManager
#pragma warning disable CS0618 // Type or member is obsolete
			m_Pathfinding = FindObjectOfType<Pathfinding>();
#pragma warning restore CS0618 // Type or member is obsolete
#pragma warning disable CS0618 // Type or member is obsolete
			m_TileManager = FindObjectOfType<TileInteractionManager>();
#pragma warning restore CS0618 // Type or member is obsolete

			if (m_Pathfinding == null)
			{
				Debug.LogError("PlayerController: Pathfinding script not found in scene!");
			}

			if (m_TileManager == null)
			{
				Debug.LogError("PlayerController: TileInteractionManager not found in scene!");
			}

			// --- NEW: Get CraftingManager reference ---
			// CraftingManager should be on the same GameObject as PlayerController
			CraftingManager = GetComponent<CraftingManager>();
			if (CraftingManager == null)
			{
				Debug.LogError("PlayerController: CraftingManager component not found on this GameObject! Crafting will not work.");
			}
		}

		private void Start()
		{
			m_MoveAction = InputAction.FindAction("Gameplay/Move");
			m_MoveAction.Enable();

			m_NextItemAction = InputAction.FindAction("Gameplay/EquipNext");
			m_PrevItemAction = InputAction.FindAction("Gameplay/EquipPrev");

			m_NextItemAction.Enable();
			m_NextItemAction.performed += context =>
			{
				ToggleToolVisual(false);
				Inventory.EquipNext();
				ToggleToolVisual(true);
			};

			m_PrevItemAction.Enable();
			m_PrevItemAction.performed += context =>
			{
				ToggleToolVisual(false);
				Inventory.EquipPrev();
				ToggleToolVisual(true);
			};

			m_UseItemAction = InputAction.FindAction("Gameplay/Use");
			m_UseItemAction.Enable();

			m_UseItemAction.performed += context => UseObject();

			m_CurrentLookDirection = Vector2.right;

			Inventory.Init();

			foreach (InventorySlot entry in Inventory.Entries)
			{
				if (entry.Item != null)
				{
					CreateItemVisual(entry.Item);
				}
			}
			ToggleToolVisual(true);

			UIHandler.UpdateInventory(Inventory);
			UIHandler.UpdateCoins(m_Coins);
		}

		[System.Obsolete]
		private void Update()
		{
			m_IsOverUI = EventSystem.current.IsPointerOverGameObject();
			m_CurrentInteractiveTarget = null;
			m_HasTarget = false;

			if (!IsMouseOverGameWindow())
			{
				UIHandler.ChangeCursor(UIHandler.CursorType.System);
				return;
			}

			if (!m_CanControl || m_IsOverUI)
			{
				if (m_IsOverUI)
				{
					UIHandler.ChangeCursor(UIHandler.CursorType.Interact);
				}

				return;
			}

			m_CurrentWorldMousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
			Collider2D overlapCol = Physics2D.OverlapPoint(m_CurrentWorldMousePos, 1 << 31); // Layer 31 for InteractiveObjects

			if (overlapCol != null)
			{
				m_CurrentInteractiveTarget = overlapCol.GetComponent<InteractiveObject>();
				m_HasTarget = false; // Player is targeting an interactive object, not a tile
				UIHandler.ChangeCursor(UIHandler.CursorType.Interact);
				return;
			}

			UIHandler.ChangeCursor(UIHandler.CursorType.Normal);

			Grid grid = GameManager.Instance.Terrain?.Grid;

			if (grid != null)
			{
				Vector3Int currentCell = grid.WorldToCell(transform.position);
				Vector3Int pointedCell = grid.WorldToCell(m_CurrentWorldMousePos);

				currentCell.z = 0;
				pointedCell.z = 0;

				Vector3Int toTarget = pointedCell - currentCell;

				if (Mathf.Abs(toTarget.x) > 1)
				{
					toTarget.x = (int)Mathf.Sign(toTarget.x);
				}

				if (Mathf.Abs(toTarget.y) > 1)
				{
					toTarget.y = (int)Mathf.Sign(toTarget.y);
				}

				m_CurrentTarget = currentCell + toTarget;
				Target.transform.position = GameManager.Instance.Terrain.Grid.GetCellCenterWorld(m_CurrentTarget);

				if (Inventory.EquippedItem != null && Inventory.EquippedItem.CanUse(m_CurrentTarget))
				{
					m_HasTarget = true;
					m_TargetMarker.Activate();
				}
				else
				{
					m_TargetMarker.Hide();
				}
			}

			if (Keyboard.current != null)
			{
				if (Keyboard.current.f5Key.wasPressedThisFrame)
				{
					SaveSystem.Save();
				}
				else if (Keyboard.current.f9Key.wasPressedThisFrame)
				{
					SaveSystem.Load();
				}
			}
			else
			{
				Debug.LogWarning("Keyboard.current is null. Input system might not be initialized yet.");
			}
		}

		private void UseObject()
		{
			if (m_IsOverUI)
			{
				return;
			}

			if (m_CurrentInteractiveTarget != null)
			{
				m_CurrentInteractiveTarget.InteractedWith();
				return;
			}

			// Only allow item use if an item is equipped AND it either doesn't need a target, or a valid target exists
			// You need to add a 'NeedTarget()' method to your Item.cs if you want to use this.
			// public virtual bool NeedTarget() { return false; } in Item.cs
			// and override in Tool/Seed etc.
			// For now, I'll comment out the NeedTarget() check to avoid compilation errors if it's missing.
			if (Inventory.EquippedItem != null /* && m_Inventory.EquippedItem.NeedTarget() */ && !m_HasTarget)
			{
				return;
			}

			UseItem();
		}

		private void FixedUpdate()
		{
			Vector2 move = m_MoveAction.ReadValue<Vector2>();
			FollowPath(); // This handles pathfinding movement

			// If not actively following a path, allow direct input movement
			if (m_CurrentPath == null || m_CurrentPath.Count == 0)
			{
				if (move != Vector2.zero)
				{
					SetLookDirectionFrom(move);
				}
				else
				{
					if (IsMouseOverGameWindow())
					{
						Vector3 posToMouse = m_CurrentWorldMousePos - transform.position;
						SetLookDirectionFrom(posToMouse);
					}
				}

				Vector2 movement = move * Speed;
				float speed = movement.sqrMagnitude;

				Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
				Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
				Animator.SetFloat(m_SpeedHash, speed);

				m_Rigidbody.MovePosition(m_Rigidbody.position + (movement * Time.deltaTime));
			}
			// If following a path, FollowPath() already handles movement and animation parameters
		}

		private bool IsMouseOverGameWindow()
		{
			return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y);
		}

		private void SetLookDirectionFrom(Vector2 direction)
		{
			m_CurrentLookDirection = Mathf.Abs(direction.x) > Mathf.Abs(direction.y)
				? direction.x > 0 ? Vector2.right : Vector2.left
				: direction.y > 0 ? Vector2.up : Vector2.down;
		}

		public bool CanFitInInventory(Item item, int count)
		{
			return Inventory.CanFitItem(item, count);
		}

		public bool AddItem(Item newItem)
		{
			CreateItemVisual(newItem);
			return Inventory.AddItem(newItem);
		}

		public void SellItem(int inventoryIndex, int count)
		{
			if (inventoryIndex < 0 || inventoryIndex >= Inventory.Entries.Length)
			{
				return;
			}

			Item item = Inventory.Entries[inventoryIndex].Item;

			if (item == null || item is not Product product) // Ensure 'Product' class exists and inherits from 'Item'
			{
				return;
			}

			int actualCount = Inventory.Remove(inventoryIndex, count);

			m_Coins += actualCount * product.SellPrice;
			UIHandler.UpdateCoins(m_Coins);
		}

		public bool BuyItem(Item item)
		{
			if (item.BuyPrice > m_Coins)
			{
				return false;
			}

			m_Coins -= item.BuyPrice;
			UIHandler.UpdateCoins(m_Coins);
			_ = AddItem(item);
			return true;
		}

		public void ChangeEquipItem(int index)
		{
			ToggleToolVisual(false);
			Inventory.EquipItem(index);
			ToggleToolVisual(true);
		}

		public void ToggleControl(bool canControl)
		{
			m_CanControl = canControl;
			if (canControl)
			{
				m_ClickToMoveAction?.Enable();
				m_MoveAction?.Enable();
				m_NextItemAction?.Enable();
				m_PrevItemAction?.Enable();
				m_UseItemAction?.Enable();
			}
			else
			{
				StopMovement();
				m_ClickToMoveAction?.Disable();
				m_MoveAction?.Disable();
				m_NextItemAction?.Disable();
				m_PrevItemAction?.Disable();
				m_UseItemAction?.Disable();
			}
		}

		public void UseItem()
		{
			if (Inventory.EquippedItem == null)
			{
				return;
			}

			Item previousEquipped = Inventory.EquippedItem;

			Inventory.UseEquippedObject(m_CurrentTarget);

			// Use ItemID as key for m_ItemVisualInstance dictionary
			if (previousEquipped != null && m_ItemVisualInstance.ContainsKey(previousEquipped.ItemID))
			{
				ItemInstance visual = m_ItemVisualInstance[previousEquipped.ItemID];

				if (visual.Animator != null)
				{
					if (!visual.Instance.activeInHierarchy)
					{
						Transform current = visual.Instance.transform;
						while (current != null)
						{
							current.gameObject.SetActive(true);
							current = current.parent;
						}
					}

					visual.Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
					visual.Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
					visual.Animator.SetTrigger("Use");
				}
			}

			if (Inventory.EquippedItem == null)
			{
				if (previousEquipped != null)
				{
					_ = StartCoroutine(DelayedObjectDisable(previousEquipped));
				}
			}
		}

		private IEnumerator DelayedObjectDisable(Item item)
		{
			yield return new WaitForSeconds(1.0f);
			ToggleVisualExplicit(false, item);
		}

		public void Save(ref PlayerSaveData data)
		{
			data.Position = m_Rigidbody.position;
			data.Coins = m_Coins;
			data.Inventory = new List<InventorySaveData>();
			Inventory.Save(ref data.Inventory);
		}

		public void Load(PlayerSaveData data)
		{
			m_Coins = data.Coins;
			Inventory.Load(data.Inventory);

			m_Rigidbody.position = data.Position;
		}

		private void ToggleToolVisual(bool enable)
		{
			// Use ItemID as key for m_ItemVisualInstance dictionary
			if (Inventory.EquippedItem != null && m_ItemVisualInstance.TryGetValue(Inventory.EquippedItem.ItemID, out ItemInstance itemVisual))
			{
				itemVisual.Instance.SetActive(enable);
			}
		}

		private void ToggleVisualExplicit(bool enable, Item item)
		{
			// Use ItemID as key for m_ItemVisualInstance dictionary
			if (item != null && m_ItemVisualInstance.TryGetValue(item.ItemID, out ItemInstance itemVisual))
			{
				itemVisual.Instance.SetActive(enable);
			}
		}

		private void CreateItemVisual(Item item)
		{
			// Use ItemID as key for m_ItemVisualInstance dictionary
			if (item.VisualPrefab != null && !m_ItemVisualInstance.ContainsKey(item.ItemID))
			{
				GameObject newVisual = Instantiate(item.VisualPrefab, ItemAttachBone, false);
				newVisual.SetActive(false);

				m_ItemVisualInstance[item.ItemID] = new ItemInstance()
				{
					Instance = newVisual,
					Animator = newVisual.GetComponentInChildren<Animator>(),
					AnimatorHash = Animator.StringToHash(item.PlayerAnimatorTriggerUse)
				};
			}
		}

		private void FollowPath()
		{
			if (m_CurrentPath == null || m_PathIndex >= m_CurrentPath.Count)
			{
				StopMovement();
				return;
			}

			Vector3Int targetCell = m_CurrentPath[m_PathIndex];
			Vector3 targetWorldPos = m_TileManager.CellToWorld(targetCell);
			targetWorldPos.z = transform.position.z;

			Vector2 direction = (targetWorldPos - transform.position).normalized;

			m_Rigidbody.MovePosition(m_Rigidbody.position + (direction * Speed * Time.deltaTime));

			float currentSpeedMagnitude = direction.magnitude > 0.01f ? Speed : 0;
			Animator.SetFloat(m_SpeedHash, currentSpeedMagnitude);

			if (direction != Vector2.zero)
			{
				SetLookDirectionFrom(direction);
			}

			Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
			Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);

			if (Vector2.Distance(transform.position, targetWorldPos) < StoppingDistance)
			{
				m_PathIndex++;
				if (m_PathIndex >= m_CurrentPath.Count)
				{
					StopMovement();
				}
			}
		}

		private void StopMovement()
		{
#pragma warning disable CS0618 // Type or member is obsolete
			m_Rigidbody.velocity = Vector2.zero;
#pragma warning restore CS0618 // Type or member is obsolete
			m_CurrentPath = null;
			m_PathIndex = 0;
			Animator.SetFloat(m_SpeedHash, 0);
		}

		private void OnDrawGizmos()
		{
			if (m_CurrentPath != null && m_TileManager != null)
			{
				Gizmos.color = Color.blue;
				for (int i = 0; i < m_CurrentPath.Count; i++)
				{
					Gizmos.DrawSphere(m_TileManager.CellToWorld(m_CurrentPath[i]), 0.2f);
					if (i < m_CurrentPath.Count - 1)
					{
						Gizmos.DrawLine(m_TileManager.CellToWorld(m_CurrentPath[i]), m_TileManager.CellToWorld(m_CurrentPath[i + 1]));
					}
				}
			}
		}
	}

	internal class ItemInstance
	{
		public GameObject Instance;
		public Animator Animator;
		public int AnimatorHash;
	}

	[System.Serializable]
	public struct PlayerSaveData
	{
		public Vector3 Position;
		public int Coins;
		public List<InventorySaveData> Inventory;
	}
}
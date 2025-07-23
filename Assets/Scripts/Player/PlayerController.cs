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
        private InputAction m_ClickToMoveAction; // New Action for click-to-move input

        public InventorySystem Inventory => m_Inventory;
        public Animator Animator => m_Animator;

        // --- NEW: CraftingManager Reference ---
        // This will be used by CraftingStation to access the crafting logic.
        // It should be assigned in Awake or Start.
        public CraftingManager craftingManager { get; private set; } // Public property for external access

        [SerializeField]
        private int m_Coins = 10;

        [SerializeField]
        private InventorySystem m_Inventory;

        private Rigidbody2D m_Rigidbody;

        private Vector3 m_SpawnPosition;

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

        private Animator m_Animator;

        private InteractiveObject m_CurrentInteractiveTarget = null;
        private Collider2D[] m_CollidersCache = new Collider2D[8];

        // Changed dictionary key from Item to string (ItemID) as per previous fix
        private Dictionary<string, ItemInstance> m_ItemVisualInstance = new();

        private int m_DirXHash = Animator.StringToHash("DirX");
        private int m_DirYHash = Animator.StringToHash("DirY");
        private int m_SpeedHash = Animator.StringToHash("Speed");

        void Awake()
        {
            m_Rigidbody = GetComponent<Rigidbody2D>();
            m_Animator = GetComponentInChildren<Animator>();
            m_TargetMarker = Target.GetComponent<TargetMarker>();
            m_TargetMarker.Hide();

            gameObject.transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            // Get references to Pathfinding and TileInteractionManager
            // Suppressing obsolete warnings, but consider updating to newer FindObjectByType if possible
#pragma warning disable CS0618
            m_Pathfinding = FindObjectOfType<Pathfinding>();
            m_TileManager = FindObjectOfType<TileInteractionManager>();
#pragma warning restore CS0618

            if (m_Pathfinding == null) Debug.LogError("PlayerController: Pathfinding script not found in scene!");
            if (m_TileManager == null) Debug.LogError("PlayerController: TileInteractionManager not found in scene!");

            // --- NEW: Get CraftingManager reference ---
            // CraftingManager should be on the same GameObject as PlayerController
            craftingManager = GetComponent<CraftingManager>();
            if (craftingManager == null)
            {
                Debug.LogError("PlayerController: CraftingManager component not found on this GameObject! Crafting will not work.");
            }
        }

        void Start()
        {
            m_MoveAction = InputAction.FindAction("Gameplay/Move");
            m_MoveAction.Enable();

            m_NextItemAction = InputAction.FindAction("Gameplay/EquipNext");
            m_PrevItemAction = InputAction.FindAction("Gameplay/EquipPrev");

            m_NextItemAction.Enable();
            m_NextItemAction.performed += context =>
            {
                ToggleToolVisual(false);
                m_Inventory.EquipNext();
                ToggleToolVisual(true);
            };

            m_PrevItemAction.Enable();
            m_PrevItemAction.performed += context =>
            {
                ToggleToolVisual(false);
                m_Inventory.EquipPrev();
                ToggleToolVisual(true);
            };

            m_UseItemAction = InputAction.FindAction("Gameplay/Use");
            m_UseItemAction.Enable();

            m_UseItemAction.performed += context => UseObject();

            m_CurrentLookDirection = Vector2.right;

            m_Inventory.Init();

            foreach (var entry in m_Inventory.Entries)
            {
                if (entry.Item != null)
                    CreateItemVisual(entry.Item);
            }
            ToggleToolVisual(true);

            UIHandler.UpdateInventory(m_Inventory);
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
                if (m_IsOverUI) UIHandler.ChangeCursor(UIHandler.CursorType.Interact);
                return;
            }

            m_CurrentWorldMousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            var overlapCol = Physics2D.OverlapPoint(m_CurrentWorldMousePos, 1 << 31); // Layer 31 for InteractiveObjects

            if (overlapCol != null)
            {
                m_CurrentInteractiveTarget = overlapCol.GetComponent<InteractiveObject>();
                m_HasTarget = false; // Player is targeting an interactive object, not a tile
                UIHandler.ChangeCursor(UIHandler.CursorType.Interact);
                return;
            }

            UIHandler.ChangeCursor(UIHandler.CursorType.Normal);

            var grid = GameManager.Instance.Terrain?.Grid;

            if (grid != null)
            {
                var currentCell = grid.WorldToCell(transform.position);
                var pointedCell = grid.WorldToCell(m_CurrentWorldMousePos);

                currentCell.z = 0;
                pointedCell.z = 0;

                var toTarget = pointedCell - currentCell;

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

                if (m_Inventory.EquippedItem != null && m_Inventory.EquippedItem.CanUse(m_CurrentTarget))
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

        void UseObject()
        {
            if (m_IsOverUI)
                return;

            if (m_CurrentInteractiveTarget != null)
            {
                m_CurrentInteractiveTarget.InteractedWith();
                return;
            }
            UseItem();
        }

        void FixedUpdate()
        {
            var move = m_MoveAction.ReadValue<Vector2>();
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

                var movement = move * Speed;
                var speed = movement.sqrMagnitude;

                m_Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
                m_Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
                m_Animator.SetFloat(m_SpeedHash, speed);

                m_Rigidbody.MovePosition(m_Rigidbody.position + movement * Time.deltaTime);
            }
            // If following a path, FollowPath() already handles movement and animation parameters
        }

        bool IsMouseOverGameWindow()
        {
            return !(0 > Input.mousePosition.x || 0 > Input.mousePosition.y || Screen.width < Input.mousePosition.x || Screen.height < Input.mousePosition.y);
        }

        void SetLookDirectionFrom(Vector2 direction)
        {
            if (Mathf.Abs(direction.x) > Mathf.Abs(direction.y))
            {
                m_CurrentLookDirection = direction.x > 0 ? Vector2.right : Vector2.left;
            }
            else
            {
                m_CurrentLookDirection = direction.y > 0 ? Vector2.up : Vector2.down;
            }
        }

        public bool CanFitInInventory(Item item, int count)
        {
            return m_Inventory.CanFitItem(item, count);
        }

        public bool AddItem(Item newItem)
        {
            CreateItemVisual(newItem);
            return m_Inventory.AddItem(newItem);
        }

        public void SellItem(int inventoryIndex, int count)
        {
            if (inventoryIndex < 0 || inventoryIndex >= Inventory.Entries.Length) // Corrected boundary check
                return;

            var item = Inventory.Entries[inventoryIndex].Item;

            // Ensure 'Product' class exists and inherits from 'Item' and has 'SellPrice'
            if (item == null || !(item is Product product))
                return;

            int actualCount = m_Inventory.Remove(inventoryIndex, count);

            m_Coins += actualCount * product.SellPrice;
            UIHandler.UpdateCoins(m_Coins);
            UIHandler.PlayBuySellSound(transform.position);
        }

        public bool BuyItem(Item item)
        {
            if (item.BuyPrice > m_Coins)
            {
                return false;
            }

            m_Coins -= item.BuyPrice;
            UIHandler.UpdateCoins(m_Coins);
            UIHandler.PlayBuySellSound(transform.position);
            AddItem(item);
            return true;
        }

        public void ChangeEquipItem(int index)
        {
            ToggleToolVisual(false);
            m_Inventory.EquipItem(index);
            ToggleToolVisual(true);
        }

        public void ToggleControl(bool canControl)
        {
            m_CanControl = canControl;
            if (canControl)
            {
                m_ClickToMoveAction?.Enable(); // Use null-conditional operator for safety
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
            if (m_Inventory.EquippedItem == null)
                return;

            var previousEquipped = m_Inventory.EquippedItem;

            m_Inventory.UseEquippedObject(m_CurrentTarget);

            // Use ItemID as key for m_ItemVisualInstance dictionary
            if (previousEquipped != null && m_ItemVisualInstance.ContainsKey(previousEquipped.ItemID))
            {
                var visual = m_ItemVisualInstance[previousEquipped.ItemID];

                // Animator.SetTrigger(visual.AnimatorHash); // This line is redundant if visual.Animator is used below

                if (visual.Animator != null)
                {
                    if (!visual.Instance.activeInHierarchy)
                    {
                        var current = visual.Instance.transform;
                        while (current != null)
                        {
                            current.gameObject.SetActive(true);
                            current = current.parent;
                        }
                    }

                    visual.Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
                    visual.Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);
                    visual.Animator.SetTrigger("Use"); // Trigger the use animation on the item visual
                }
            }

            if (m_Inventory.EquippedItem == null)
            {
                if (previousEquipped != null)
                {
                    StartCoroutine(DelayedObjectDisable(previousEquipped));
                }
            }
        }

        IEnumerator DelayedObjectDisable(Item item)
        {
            yield return new WaitForSeconds(1.0f);
            ToggleVisualExplicit(false, item);
        }

        public void Save(ref PlayerSaveData data)
        {
            data.Position = m_Rigidbody.position;
            data.Coins = m_Coins;
            data.Inventory = new List<InventorySaveData>();
            m_Inventory.Save(ref data.Inventory);
        }

        public void Load(PlayerSaveData data)
        {
            m_Coins = data.Coins;
            m_Inventory.Load(data.Inventory);

            m_Rigidbody.position = data.Position;
        }

        void ToggleToolVisual(bool enable)
        {
            // Use ItemID as key for m_ItemVisualInstance dictionary
            if (m_Inventory.EquippedItem != null && m_ItemVisualInstance.TryGetValue(m_Inventory.EquippedItem.ItemID, out var itemVisual))
            {
                itemVisual.Instance.SetActive(enable);
            }
        }

        void ToggleVisualExplicit(bool enable, Item item)
        {
            // Use ItemID as key for m_ItemVisualInstance dictionary
            if (item != null && m_ItemVisualInstance.TryGetValue(item.ItemID, out var itemVisual))
            {
                itemVisual.Instance.SetActive(enable);
            }
        }

        void CreateItemVisual(Item item)
        {
            // Use ItemID as key for m_ItemVisualInstance dictionary
            if (item.VisualPrefab != null && !m_ItemVisualInstance.ContainsKey(item.ItemID))
            {
                var newVisual = Instantiate(item.VisualPrefab, ItemAttachBone, false);
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

            m_Rigidbody.MovePosition(m_Rigidbody.position + direction * Speed * Time.deltaTime);

            float currentSpeedMagnitude = direction.magnitude > 0.01f ? Speed : 0;
            m_Animator.SetFloat(m_SpeedHash, currentSpeedMagnitude);

            if (direction != Vector2.zero)
            {
                SetLookDirectionFrom(direction);
            }

            m_Animator.SetFloat(m_DirXHash, m_CurrentLookDirection.x);
            m_Animator.SetFloat(m_DirYHash, m_CurrentLookDirection.y);

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
#pragma warning disable CS0618
            m_Rigidbody.velocity = Vector2.zero;
#pragma warning restore CS0618
            m_CurrentPath = null;
            m_PathIndex = 0;
            m_Animator.SetFloat(m_SpeedHash, 0);
        }

        void OnDrawGizmos()
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

    class ItemInstance
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
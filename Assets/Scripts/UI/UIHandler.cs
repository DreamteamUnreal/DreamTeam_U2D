// UIHandler.cs
using System.Collections.Generic;
using Template2DCommon;
using UnityEngine;
using UnityEngine.UIElements; // For VisualElement, Label, Button, etc.
using Cursor = UnityEngine.Cursor; // To avoid ambiguity with System.Windows.Forms.Cursor if present

namespace HappyHarvest
{
	/// <summary>
	/// Handles everything related to the main gameplay UI. Will retrieve all the UI Element and contains various static
	/// functions that updates/change the UI so they can be called from any other class interacting with the UI.
	/// </summary>
	public class UIHandler : MonoBehaviour
	{
		protected static UIHandler s_Instance; // Singleton instance

		public enum CursorType
		{
			Normal,
			Interact,
			System
		}

		[Header("Cursor")]
		public Texture2D NormalCursor;
		public Texture2D InteractCursor;

		[Header("UI Document")]
		public VisualTreeAsset MarketEntryTemplate; // UXML template for market items

		[Header("Sounds")]
		public AudioClip MarketSellSound;

		protected UIDocument m_Document;

		// UI Element references
		protected List<VisualElement> m_InventorySlots;
		protected List<Label> m_ItemCountLabels;
		protected Label m_CointCounter;
		protected VisualElement m_MarketPopup;
		protected ScrollView m_MarketContentScrollview; // Changed to ScrollView for clarity
		protected Label m_TimerLabel;
		protected Button m_BuyButton;
		protected Button m_SellButton;

		protected bool m_HaveFocus = true;
		protected CursorType m_CurrentCursorType;

		protected SettingMenu m_SettingMenu;
		protected WarehouseUI m_WarehouseUI;

		// Fade to black helper
		protected VisualElement m_Blocker;
		protected System.Action m_FadeFinishClbk;

		// Weather icons
		private Label m_SunLabel;
		private Label m_RainLabel;
		private Label m_ThunderLabel;

		private void Awake()
		{
			s_Instance = this; // Set singleton instance

			m_Document = GetComponent<UIDocument>();

			// CRITICAL: Check if UIDocument is found and its root is valid
			if (m_Document == null)
			{
				Debug.LogError("UIHandler: UIDocument component not found on this GameObject. UI will not function. Please add a UIDocument component.");
				enabled = false; // Disable this component to prevent further errors
				return;
			}
			if (m_Document.rootVisualElement == null)
			{
				Debug.LogError("UIHandler: UIDocument's rootVisualElement is null. Is the UXML asset assigned to the UIDocument's 'Source Asset' field?");
				enabled = false;
				return;
			}

			// Query UI elements by name
			m_InventorySlots = m_Document.rootVisualElement.Query<VisualElement>("InventoryEntry").ToList();
			m_ItemCountLabels = m_Document.rootVisualElement.Query<Label>("ItemCount").ToList();

			// Setup Inventory Slot click handlers
			for (int i = 0; i < m_InventorySlots.Count; ++i)
			{
				int i1 = i; // Capture loop variable for closure
							// Ensure Clickable class is from UnityEngine.UIElements or Template2DCommon
							// If Template2DCommon is not used, replace with standard UIElements click registration:
							// m_InventorySlots[i].RegisterCallback<ClickEvent>(evt => { /* ... */ });
				m_InventorySlots[i].AddManipulator(new Clickable(() =>
				{
					if (GameManager.Instance != null && GameManager.Instance.Player != null)
					{
						GameManager.Instance.Player.ChangeEquipItem(i1);
					}
					else
					{
						Debug.LogWarning("UIHandler: GameManager or Player is null when attempting to change equip item. Check Script Execution Order.");
					}
				}));
			}

			// Assert inventory slot count matches expected size
			Debug.Assert(m_InventorySlots.Count == InventorySystem.InventorySize,
				$"UIHandler: Not enough inventory slots in the UXML ({m_InventorySlots.Count}) for InventorySystem size ({InventorySystem.InventorySize}).");

			m_CointCounter = m_Document.rootVisualElement.Q<Label>("CoinAmount");
			if (m_CointCounter == null)
			{
				Debug.LogWarning("UIHandler: 'CoinAmount' label not found in UXML.");
			}

			m_MarketPopup = m_Document.rootVisualElement.Q<VisualElement>("MarketPopup");
			if (m_MarketPopup != null)
			{
				Button closeButton = m_MarketPopup.Q<Button>("CloseButton");
				if (closeButton != null)
				{
					closeButton.clicked += CloseMarket;
				}
				else
				{
					Debug.LogWarning("UIHandler: 'CloseButton' not found in 'MarketPopup'.");
				}

				m_MarketPopup.visible = false; // Initially hide market popup

				m_BuyButton = m_MarketPopup.Q<Button>("BuyButton");
				if (m_BuyButton != null)
				{
					m_BuyButton.clicked += ToggleToBuy;
				}
				else
				{
					Debug.LogWarning("UIHandler: 'BuyButton' not found in 'MarketPopup'.");
				}

				m_SellButton = m_MarketPopup.Q<Button>("SellButton");
				if (m_SellButton != null)
				{
					m_SellButton.clicked += ToggleToSell;
				}
				else
				{
					Debug.LogWarning("UIHandler: 'SellButton' not found in 'MarketPopup'.");
				}

				m_MarketContentScrollview = m_MarketPopup.Q<ScrollView>("ContentScrollView");
				if (m_MarketContentScrollview == null)
				{
					Debug.LogWarning("UIHandler: 'ContentScrollView' not found in 'MarketPopup'.");
				}
			}
			else
			{
				Debug.LogWarning("UIHandler: 'MarketPopup' VisualElement not found in UXML.");
			}

			m_TimerLabel = m_Document.rootVisualElement.Q<Label>("Timer");
			if (m_TimerLabel == null)
			{
				Debug.LogWarning("UIHandler: 'Timer' label not found in UXML.");
			}

			// Initialize SettingMenu
			m_SettingMenu = new SettingMenu(m_Document.rootVisualElement);
			m_SettingMenu.OnOpen += () => { if (GameManager.Instance != null) { GameManager.Instance.Pause(); } };
			m_SettingMenu.OnClose += () => { if (GameManager.Instance != null) { GameManager.Instance.Resume(); } };

			// Initialize WarehouseUI
			VisualElement warehousePopup = m_Document.rootVisualElement.Q<VisualElement>("WarehousePopup");
			if (warehousePopup != null)
			{
				if (MarketEntryTemplate == null)
				{
					Debug.LogError("UIHandler: MarketEntryTemplate is null. WarehouseUI cannot be initialized without it.");
				}
				else
				{
					m_WarehouseUI = new WarehouseUI(warehousePopup, MarketEntryTemplate);
				}
			}
			else
			{
				Debug.LogWarning("UIHandler: 'WarehousePopup' not found in UXML. Warehouse UI might not function.");
			}

			// Initialize Blocker for fade effects
			m_Blocker = m_Document.rootVisualElement.Q<VisualElement>("Blocker");
			if (m_Blocker != null)
			{
				m_Blocker.style.opacity = 1.0f; // Start fully opaque for initial fade-in
				m_Blocker.RegisterCallback<TransitionEndEvent>(evt => { m_FadeFinishClbk?.Invoke(); });
			}
			else
			{
				Debug.LogWarning("UIHandler: 'Blocker' VisualElement not found in UXML. Fade effects will not work.");
			}

			// Initialize Weather Labels and their click handlers
			m_SunLabel = m_Document.rootVisualElement.Q<Label>("SunLabel");
			if (m_SunLabel != null)
			{
				m_SunLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Sun); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'SunLabel' not found in UXML.");
			}

			m_RainLabel = m_Document.rootVisualElement.Q<Label>("RainLabel");
			if (m_RainLabel != null)
			{
				m_RainLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Rain); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'RainLabel' not found in UXML.");
			}

			m_ThunderLabel = m_Document.rootVisualElement.Q<Label>("ThunderLabel");
			if (m_ThunderLabel != null)
			{
				m_ThunderLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Thunder); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'ThunderLabel' not found in UXML.");
			}
		}

		private void Start()
		{
			// Perform initial fade from black after all Awake methods have run
			// and GameManager.Instance is guaranteed to be set.
			if (m_Blocker != null)
			{
				FadeFromBlack(() => { Debug.Log("Initial fade from black complete."); });
			}

			// Initial UI updates that rely on GameManager/Player
			if (GameManager.Instance != null && GameManager.Instance.Player != null)
			{
				UpdateInventory_Internal(GameManager.Instance.Player.Inventory);
				UpdateCoins_Internal(GameManager.Instance.Player.Coins);
			}
			else
			{
				Debug.LogWarning("UIHandler Start: GameManager.Instance or Player is null. Initial UI updates skipped.");
			}

			// Initial weather icon update
			if (GameManager.Instance?.WeatherSystem != null)
			{
				UpdateWeatherIcons(GameManager.Instance.WeatherSystem.CurrentWeather);
			}

			// Initial scene loaded check for weather UI visibility
			SceneLoaded();
		}

		private void Update()
		{
			// The critical line: add null check for GameManager.Instance here
			if (GameManager.Instance != null && m_TimerLabel != null)
			{
				m_TimerLabel.text = GameManager.Instance.CurrentTimeAsString();
			}
			// No warning here, as it would spam the console if GameManager isn't ready early on.
		}

		private void OnApplicationFocus(bool hasFocus)
		{
			m_HaveFocus = hasFocus;
			if (!hasFocus)
			{
				Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
			}
			else
			{
				ChangeCursor(m_CurrentCursorType);
			}
		}

		// --- Static Public Methods for External Calls ---

		/// <summary>
		/// Updates the inventory display in the UI. Call this whenever the player's inventory changes.
		/// </summary>
		/// <param name="system">The InventorySystem instance to display.</param>
		public static void UpdateInventory(InventorySystem system)
		{
			if (s_Instance != null)
			{
				s_Instance.UpdateInventory_Internal(system);
			}
			else
			{
				Debug.LogWarning("UIHandler.UpdateInventory: s_Instance is null. UI update skipped.");
			}
		}

		/// <summary>
		/// Updates the coin counter in the UI. Call this whenever the player's coin amount changes.
		/// </summary>
		/// <param name="amount">The new coin amount.</param>
		public static void UpdateCoins(int amount)
		{
			if (s_Instance != null && s_Instance.m_CointCounter != null)
			{
				s_Instance.UpdateCoins_Internal(amount);
			}
			else
			{
				Debug.LogWarning("UIHandler.UpdateCoins: s_Instance or m_CointCounter is null. Coin update skipped.");
			}
		}

		/// <summary>
		/// Opens the market UI.
		/// </summary>
		public static void OpenMarket()
		{
			if (s_Instance != null)
			{
				s_Instance.OpenMarket_Internal();
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Pause();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenMarket: s_Instance is null. Market cannot be opened.");
			}
		}

		/// <summary>
		/// Closes the market UI.
		/// </summary>
		public static void CloseMarket()
		{
			if (s_Instance != null && s_Instance.m_MarketPopup != null)
			{
				if (SoundManager.Instance != null)
				{
					SoundManager.Instance.PlayUISound();
				}

				s_Instance.m_MarketPopup.visible = false;
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Resume();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.CloseMarket: s_Instance or m_MarketPopup is null. Market cannot be closed.");
			}
		}

		/// <summary>
		/// Opens the warehouse UI.
		/// </summary>
		public static void OpenWarehouse()
		{
			if (s_Instance != null && s_Instance.m_WarehouseUI != null)
			{
				s_Instance.m_WarehouseUI.Open();
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenWarehouse: s_Instance or m_WarehouseUI is null. Warehouse cannot be opened.");
			}
		}

		/// <summary>
		/// Changes the mouse cursor icon.
		/// </summary>
		/// <param name="cursorType">The type of cursor to display.</param>
		public static void ChangeCursor(CursorType cursorType)
		{
			if (s_Instance == null)
			{
				return; // Cannot change cursor if UIHandler not initialized
			}

			if (s_Instance.m_HaveFocus)
			{
				switch (cursorType)
				{
					case CursorType.Interact:
						Cursor.SetCursor(s_Instance.InteractCursor, Vector2.zero, CursorMode.Auto);
						break;
					case CursorType.Normal:
						Cursor.SetCursor(s_Instance.NormalCursor, Vector2.zero, CursorMode.Auto);
						break;
					case CursorType.System:
						Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
						break;
					default:
						break;
				}
			}
			s_Instance.m_CurrentCursorType = cursorType;
		}

		/// <summary>
		/// Updates the visibility of weather icons in the UI based on the current weather.
		/// </summary>
		/// <param name="currentWeather">The current weather type.</param>
		public static void UpdateWeatherIcons(WeatherSystem.WeatherType currentWeather)
		{
			if (s_Instance == null)
			{
				return; // Cannot update icons if UIHandler not initialized
			}

			// Null checks for labels before accessing EnableInClassList
			s_Instance.m_SunLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Sun);
			s_Instance.m_RainLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Rain);
			s_Instance.m_ThunderLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Thunder);

			s_Instance.m_SunLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Sun);
			s_Instance.m_RainLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Rain);
			s_Instance.m_ThunderLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Thunder);
		}

		/// <summary>
		/// Called when a new scene is loaded. Adjusts UI visibility based on scene content (e.g., weather system presence).
		/// </summary>
		public static void SceneLoaded()
		{
			if (s_Instance == null)
			{
				return; // Cannot update UI if UIHandler not initialized
			}

			// We hide the weather control if there is no weather system in that scene
			// Add null check for m_SunLabel.parent
			if (s_Instance.m_SunLabel != null && s_Instance.m_SunLabel.parent != null)
			{
				s_Instance.m_SunLabel.parent.style.display =
					GameManager.Instance.WeatherSystem == null ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}

		/// <summary>
		/// Fades the screen to black.
		/// </summary>
		/// <param name="onFinished">Callback executed when the fade is complete.</param>
		public static void FadeToBlack(System.Action onFinished)
		{
			if (s_Instance == null || s_Instance.m_Blocker == null)
			{
				Debug.LogWarning("UIHandler.FadeToBlack: s_Instance or m_Blocker is null. Fade skipped.");
				onFinished?.Invoke(); // Call callback immediately if fade cannot happen
				return;
			}

			s_Instance.m_FadeFinishClbk = onFinished;
			// FIX: Directly assign List<TimeValue> to transitionDuration
			s_Instance.m_Blocker.style.transitionDuration = new List<TimeValue> { new(0.5f, TimeUnit.Second) };
			s_Instance.m_Blocker.style.opacity = 1.0f;
			Debug.Log("UIHandler: Fading to black.");
		}

		/// <summary>
		/// Fades the screen from black.
		/// </summary>
		/// <param name="onFinished">Callback executed when the fade is complete.</param>
		public static void FadeFromBlack(System.Action onFinished)
		{
			if (s_Instance == null || s_Instance.m_Blocker == null)
			{
				Debug.LogWarning("UIHandler.FadeFromBlack: s_Instance or m_Blocker is null. Fade skipped.");
				onFinished?.Invoke(); // Call callback immediately if fade cannot happen
				return;
			}

			s_Instance.m_FadeFinishClbk = onFinished;
			// FIX: Directly assign List<TimeValue> to transitionDuration
			s_Instance.m_Blocker.style.transitionDuration = new List<TimeValue> { new(0.5f, TimeUnit.Second) };
			s_Instance.m_Blocker.style.opacity = 0.0f;
			Debug.Log("UIHandler: Fading from black.");
		}

		// --- Internal Helper Methods ---
		private void OpenMarket_Internal()
		{
			if (m_MarketPopup == null)
			{
				return;
			}

			m_MarketPopup.visible = true;
			ToggleToSell(); // Open the Sell Tab by default
			if (GameManager.Instance != null && GameManager.Instance.Player != null)
			{
				GameManager.Instance.Player.ToggleControl(false);
			}
		}

		private void ToggleToSell()
		{
			if (m_SellButton == null || m_BuyButton == null || m_MarketContentScrollview == null || GameManager.Instance == null || GameManager.Instance.Player == null)
			{
				return;
			}

			m_SellButton.AddToClassList("activeButton");
			m_BuyButton.RemoveFromClassList("activeButton");
			m_SellButton.SetEnabled(false);
			m_BuyButton.SetEnabled(true);

			m_MarketContentScrollview.contentContainer.Clear();

			for (int i = 0; i < GameManager.Instance.Player.Inventory.Entries.Length; ++i)
			{
				Item item = GameManager.Instance.Player.Inventory.Entries[i].Item;
				if (item == null)
				{
					continue;
				}

				// Ensure MarketEntryTemplate is assigned in the Inspector
				if (MarketEntryTemplate == null)
				{
					Debug.LogError("UIHandler: MarketEntryTemplate is null. Cannot generate market entries.");
					return;
				}

				TemplateContainer clone = MarketEntryTemplate.CloneTree();

				clone.Q<Label>("ItemName").text = item.DisplayName;
				clone.Q<VisualElement>("ItemIcone").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = clone.Q<Button>("ActionButton");

				if (item is Product product)
				{
					int count = GameManager.Instance.Player.Inventory.Entries[i].StackSize;
					button.text = $"Sell {count} for {product.SellPrice * count}";

					int i1 = i; // Capture for closure
					button.clicked += () =>
					{
						if (GameManager.Instance != null && GameManager.Instance.Player != null)
						{
							GameManager.Instance.Player.SellItem(i1, count);
							// Remove this entry after selling (assuming it's fully sold)
							m_MarketContentScrollview.contentContainer.Remove(clone.contentContainer);
							// Re-toggle to sell to refresh list if partially sold items remain
							ToggleToSell();
						}
					};
				}
				else
				{
					button.SetEnabled(false);
					button.text = "Cannot Sell";
				}
				m_MarketContentScrollview.Add(clone.contentContainer);
			}
		}

		private void ToggleToBuy()
		{
			if (m_SellButton == null || m_BuyButton == null || m_MarketContentScrollview == null || GameManager.Instance == null || GameManager.Instance.Player == null)
			{
				return;
			}

			m_SellButton.RemoveFromClassList("activeButton");
			m_BuyButton.AddToClassList("activeButton");
			m_BuyButton.SetEnabled(false);
			m_SellButton.SetEnabled(true);

			m_MarketContentScrollview.contentContainer.Clear();

			for (int i = 0; i < GameManager.Instance.MarketEntries.Length; ++i)
			{
				Item item = GameManager.Instance.MarketEntries[i];
				if (item == null)
				{
					continue;
				}

				if (MarketEntryTemplate == null)
				{
					Debug.LogError("UIHandler: MarketEntryTemplate is null. Cannot generate market entries.");
					return;
				}

				TemplateContainer clone = MarketEntryTemplate.CloneTree();

				clone.Q<Label>("ItemName").text = item.DisplayName;
				clone.Q<VisualElement>("ItemIcone").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = clone.Q<Button>("ActionButton");

				if (GameManager.Instance.Player.Coins >= item.BuyPrice && GameManager.Instance.Player.CanFitInInventory(item, 1))
				{
					button.text = $"Buy 1 for {item.BuyPrice}";
					int i1 = i; // Capture for closure
					button.clicked += () =>
					{
						if (GameManager.Instance != null && GameManager.Instance.Player != null)
						{
							if (GameManager.Instance.Player.BuyItem(item))
							{
								// If bought, refresh the buy list to update affordability/capacity
								ToggleToBuy();
							}
						}
					};
					button.SetEnabled(true);
				}
				else
				{
					button.SetEnabled(false);
					if (GameManager.Instance.Player.Coins < item.BuyPrice)
					{
						button.text = $"Cannot afford ({item.BuyPrice})";
					}
					else if (!GameManager.Instance.Player.CanFitInInventory(item, 1))
					{
						button.text = "Inventory Full";
					}
				}
				m_MarketContentScrollview.Add(clone.contentContainer);
			}
		}

		private void UpdateCoins_Internal(int amount)
		{
			if (m_CointCounter != null)
			{
				m_CointCounter.text = amount.ToString();
			}
		}

		private void UpdateInventory_Internal(InventorySystem system)
		{
			if (system == null || m_InventorySlots == null || m_ItemCountLabels == null)
			{
				return;
			}

			for (int i = 0; i < system.Entries.Length; ++i)
			{
				// Ensure index is within bounds for UI elements
				if (i >= m_InventorySlots.Count || i >= m_ItemCountLabels.Count)
				{
					Debug.LogWarning($"UIHandler: Inventory slot index {i} out of bounds for UI elements. Check UXML 'InventoryEntry' count.");
					break;
				}

				Item item = system.Entries[i].Item;
				m_InventorySlots[i][0].style.backgroundImage =
					item == null ? new StyleBackground((Sprite)null) : new StyleBackground(item.ItemSprite);

				if (item == null || system.Entries[i].StackSize < 2)
				{
					m_ItemCountLabels[i].style.visibility = Visibility.Hidden;
				}
				else
				{
					m_ItemCountLabels[i].style.visibility = Visibility.Visible;
					m_ItemCountLabels[i].text = system.Entries[i].StackSize.ToString();
				}

				if (system.EquippedItemIdx == i)
				{
					m_InventorySlots[i].AddToClassList("equipped");
				}
				else
				{
					m_InventorySlots[i].RemoveFromClassList("equipped");
				}
			}
		}
	}
}
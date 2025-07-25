//UIHandler.cs
using System.Collections.Generic;
using Template2DCommon;
using UnityEngine;
using UnityEngine.UIElements;
using Cursor = UnityEngine.Cursor;

namespace HappyHarvest
{
	/// <summary>
	/// Handle everything related to the main gameplay UI. Will retrieve all the UI Element and contains various static
	/// functions that updates/change the UI so they can be called from any other class interacting with the UI.
	/// </summary>
	public class UIHandler : MonoBehaviour
	{
		protected static UIHandler s_Instance;

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
		public VisualTreeAsset MarketEntryTemplate;

		[Header("Sounds")]
		public AudioClip MarketSellSound;

		protected UIDocument m_Document;

		protected List<VisualElement> m_InventorySlots;
		protected List<Label> m_ItemCountLabels;

		protected Label m_CointCounter;

		protected VisualElement m_MarketPopup;
		protected VisualElement m_MarketContentScrollview;

		protected Label m_TimerLabel;

		protected Button m_BuyButton;
		protected Button m_SellButton;

		protected bool m_HaveFocus = true;
		protected CursorType m_CurrentCursorType;

		protected SettingMenu m_SettingMenu;
		protected WarehouseUI m_WarehouseUI;

		// Fade to balck helper
		protected VisualElement m_Blocker;
		protected System.Action m_FadeFinishClbk;

		private Label m_SunLabel;
		private Label m_RainLabel;
		private Label m_ThunderLabel;

		private void Awake()
		{
			s_Instance = this;

			m_Document = GetComponent<UIDocument>();

			// Recommended: Null check m_Document here as well, if it might not be present
			if (m_Document == null)
			{
				Debug.LogError("UIHandler: UIDocument component not found on this GameObject. UI will not function.");
				enabled = false; // Disable this component to prevent further errors
				return;
			}

			// Recommended: Null check rootVisualElement, similar to MainMenuHandler
			if (m_Document.rootVisualElement == null)
			{
				Debug.LogError("UIHandler: UIDocument's rootVisualElement is null. Is the UXML asset assigned?");
				enabled = false;
				return;
			}

			m_InventorySlots = m_Document.rootVisualElement.Query<VisualElement>("InventoryEntry").ToList();
			m_ItemCountLabels = m_Document.rootVisualElement.Query<Label>("ItemCount").ToList();

			for (int i = 0; i < m_InventorySlots.Count; ++i)
			{
				int i1 = i;
				m_InventorySlots[i].AddManipulator(new Clickable(() =>
				{
					// Add null check for GameManager.Instance here too, as it's accessed within Awake
					if (GameManager.Instance != null && GameManager.Instance.Player != null)
					{
						GameManager.Instance.Player.ChangeEquipItem(i1);
					}
					else
					{
						Debug.LogWarning("UIHandler: GameManager or Player is null when attempting to change equip item.");
					}
				}));
			}

			Debug.Assert(m_InventorySlots.Count == InventorySystem.InventorySize,
				"Not enough items slots in the UI for inventory");

			m_CointCounter = m_Document.rootVisualElement.Q<Label>("CoinAmount");

			m_MarketPopup = m_Document.rootVisualElement.Q<VisualElement>("MarketPopup");
			// Add null check for m_MarketPopup before accessing its children
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
				m_MarketPopup.visible = false;

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
				Debug.LogWarning("UIHandler: 'MarketPopup' not found in UXML.");
			}

			m_TimerLabel = m_Document.rootVisualElement.Q<Label>("Timer");
			if (m_TimerLabel == null)
			{
				Debug.LogWarning("UIHandler: 'Timer' label not found in UXML.");
			}

			m_SettingMenu = new SettingMenu(m_Document.rootVisualElement);
			m_SettingMenu.OnOpen += () => { if (GameManager.Instance != null) { GameManager.Instance.Pause(); } };
			m_SettingMenu.OnClose += () => { if (GameManager.Instance != null) { GameManager.Instance.Resume(); } };

			// Add null check for WarehousePopup if it's optional
			VisualElement warehousePopup = m_Document.rootVisualElement.Q<VisualElement>("WarehousePopup");
			if (warehousePopup != null)
			{
				m_WarehouseUI = new WarehouseUI(warehousePopup, MarketEntryTemplate);
			}
			else
			{
				Debug.LogWarning("UIHandler: 'WarehousePopup' not found in UXML. Warehouse UI might not function.");
			}

			m_Blocker = m_Document.rootVisualElement.Q<VisualElement>("Blocker");
			if (m_Blocker != null)
			{
				m_Blocker.style.opacity = 1.0f;
				// It's generally safer to put Schedule calls in Start or on an enabled event
				// This is being executed later, so it's probably okay, but ensure m_Blocker is not null
				m_Blocker.schedule.Execute(() => { FadeFromBlack(() => { }); }).ExecuteLater(500);

				m_Blocker.RegisterCallback<TransitionEndEvent>(evt =>
				{
					m_FadeFinishClbk?.Invoke();
				});
			}
			else
			{
				Debug.LogWarning("UIHandler: 'Blocker' VisualElement not found in UXML.");
			}

			m_SunLabel = m_Document.rootVisualElement.Q<Label>("SunLabel");
			m_RainLabel = m_Document.rootVisualElement.Q<Label>("RainLabel");
			m_ThunderLabel = m_Document.rootVisualElement.Q<Label>("ThunderLabel");

			// Add null checks for weather labels before adding manipulators
			if (m_SunLabel != null)
			{
				m_SunLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Sun); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'SunLabel' not found.");
			}

			if (m_RainLabel != null)
			{
				m_RainLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Rain); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'RainLabel' not found.");
			}

			if (m_ThunderLabel != null)
			{
				m_ThunderLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Thunder); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'ThunderLabel' not found.");
			}
		}

		private void Update()
		{
			// The critical line: add null check for GameManager.Instance here
			if (GameManager.Instance != null && m_TimerLabel != null)
			{
				m_TimerLabel.text = GameManager.Instance.CurrentTimeAsString();
			}
			else
			{
				// This warning will tell if GameManager is not ready or if m_TimerLabel was not found
				// Debug.LogWarning("UIHandler Update: GameManager.Instance or m_TimerLabel is null. Cannot update timer.");
			}
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

		//Need to be called by the player everytime the inventory change.
		public static void UpdateInventory(InventorySystem system)
		{
			s_Instance.UpdateInventory_Internal(system);
		}

		public static void UpdateCoins(int amount)
		{
			s_Instance.UpdateCoins_Internal(amount);
		}

		public static void OpenMarket()
		{
			s_Instance.OpenMarket_Internal();
			GameManager.Instance.Pause();
		}

		public static void CloseMarket()
		{
			SoundManager.Instance.PlayUISound();
			s_Instance.m_MarketPopup.visible = false;
			GameManager.Instance.Resume();
		}

		public static void OpenWarehouse()
		{
			s_Instance.m_WarehouseUI.Open();
		}

		public static void ChangeCursor(CursorType cursorType)
		{
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

		public static void UpdateWeatherIcons(WeatherSystem.WeatherType currentWeather)
		{
			s_Instance.m_SunLabel.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Sun);
			s_Instance.m_RainLabel.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Rain);
			s_Instance.m_ThunderLabel.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Thunder);

			s_Instance.m_SunLabel.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Sun);
			s_Instance.m_RainLabel.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Rain);
			s_Instance.m_ThunderLabel.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Thunder);
		}

		public static void SceneLoaded()
		{
			//we hide the weather control if there is no weather sytsem in that scene
			s_Instance.m_SunLabel.parent.style.display =
				GameManager.Instance.WeatherSystem == null ? DisplayStyle.None : DisplayStyle.Flex;
		}

		private void OpenMarket_Internal()
		{
			m_MarketPopup.visible = true;

			//we open the Sell Tab by default
			ToggleToSell();

			GameManager.Instance.Player.ToggleControl(false);
		}

		private void ToggleToSell()
		{
			m_SellButton.AddToClassList("activeButton");
			m_BuyButton.RemoveFromClassList("activeButton");

			m_SellButton.SetEnabled(false);
			m_BuyButton.SetEnabled(true);

			//clear all the existing entry. A good target for optimization if profiling show bad perf in UI is to pool
			//instead of delete/recreate entries
			m_MarketContentScrollview.contentContainer.Clear();

			for (int i = 0; i < GameManager.Instance.Player.Inventory.Entries.Length; ++i)
			{
				Item item = GameManager.Instance.Player.Inventory.Entries[i].Item;
				if (item == null)
				{
					continue;
				}

				TemplateContainer clone = MarketEntryTemplate.CloneTree();

				clone.Q<Label>("ItemName").text = item.DisplayName;
				clone.Q<VisualElement>("ItemIcone").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = clone.Q<Button>("ActionButton");

				if (item is Product product)
				{
					int count = GameManager.Instance.Player.Inventory.Entries[i].StackSize;
					button.text = $"Sell {count} for {product.SellPrice * count}";

					int i1 = i;
					button.clicked += () =>
					{
						GameManager.Instance.Player.SellItem(i1, count);
						//we remove this entry, we just sold it.
						m_MarketContentScrollview.contentContainer.Remove(clone.contentContainer);
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
			m_SellButton.RemoveFromClassList("activeButton");
			m_BuyButton.AddToClassList("activeButton");

			m_BuyButton.SetEnabled(false);
			m_SellButton.SetEnabled(true);

			//clear all the existing entry. A good target for optimization if profiling show bad perf in UI is to pool
			//instead of delete/recreate entries
			m_MarketContentScrollview.contentContainer.Clear();

			for (int i = 0; i < GameManager.Instance.MarketEntries.Length; ++i)
			{
				Item item = GameManager.Instance.MarketEntries[i];

				TemplateContainer clone = MarketEntryTemplate.CloneTree();

				clone.Q<Label>("ItemName").text = item.DisplayName;
				clone.Q<VisualElement>("ItemIcone").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = clone.Q<Button>("ActionButton");

				if (GameManager.Instance.Player.Coins >= item.BuyPrice)
				{
					button.text = $"Buy 1 for {item.BuyPrice}";
					int i1 = i;
					button.clicked += () =>
					{
						if (GameManager.Instance.Player.BuyItem(item))
						{
							if (GameManager.Instance.Player.Coins < item.BuyPrice)
							{
								button.text = $"Cannot afford cost of {item.BuyPrice}";
								button.SetEnabled(false);
							}
						}
					};
					button.SetEnabled(true);
				}
				else
				{
					button.text = $"Cannot afford cost of {item.BuyPrice}";
					button.SetEnabled(false);
				}

				m_MarketContentScrollview.Add(clone.contentContainer);
			}
		}

		public static void PlayBuySellSound(Vector3 location)
		{
			SoundManager.Instance.PlaySFXAt(location, s_Instance.MarketSellSound, false);
		}

		public static void FadeToBlack(System.Action onFinished)
		{
			s_Instance.m_FadeFinishClbk = onFinished;

			s_Instance.m_Blocker.schedule.Execute(() =>
			{
				s_Instance.m_Blocker.style.opacity = 1.0f;
			}).ExecuteLater(10);
		}

		public static void FadeFromBlack(System.Action onFinished)
		{
			s_Instance.m_FadeFinishClbk = onFinished;

			s_Instance.m_Blocker.schedule.Execute(() =>
			{
				s_Instance.m_Blocker.style.opacity = 0.0f;
			}).ExecuteLater(10);
		}

		private void UpdateCoins_Internal(int amount)
		{
			m_CointCounter.text = amount.ToString();
		}

		private void UpdateInventory_Internal(InventorySystem system)
		{
			for (int i = 0; i < system.Entries.Length; ++i)
			{
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
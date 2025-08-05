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
	/// This updated version includes fixes for UI Toolkit binding issues, adds a crafting menu,
	/// and a game over screen.
	/// </summary>
	public class UIHandler : MonoBehaviour
	{
		// Changed to public for easier debugging of the singleton instance
		public static UIHandler Instance { get; private set; } // Singleton instance

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
		// These are the UXML and USS assets you will link in the Inspector
		public VisualTreeAsset MarketEntryTemplate;
		public VisualTreeAsset CraftingEntryTemplate; // Ensure this is assigned in Inspector if used

		[Header("Sounds")]
		public AudioClip MarketSellSound;
		public AudioClip PieCraftSound; // New sound for pie crafting

		protected UIDocument m_Document;

		protected List<VisualElement> m_InventorySlots;
		protected List<Label> m_ItemCountLabels;
		protected Label m_CointCounter;
		protected VisualElement m_MarketPopup;
		protected ScrollView m_MarketContentScrollview;
		protected Label m_TimerLabel;
		protected Button m_BuyButton;
		protected Button m_SellButton;

		protected bool m_HaveFocus = true;
		protected CursorType m_CurrentCursorType;

		protected VisualElement m_SettingMenu;
		protected VisualElement m_GameOverScreen;
		protected VisualElement m_CraftingPopup;
		protected ScrollView m_CraftingScrollView;

		protected WarehouseUI m_WarehouseUI; // Assuming this is a custom class

		protected VisualElement m_Blocker;
		protected System.Action m_FadeFinishClbk;

		private Label m_SunLabel;
		private Label m_RainLabel;
		private Label m_ThunderLabel;
		private Button m_SettingsButton; // Button to open the settings menu (e.g., "MenuButton" in UXML)

		private void Awake()
		{
			// Ensure only one instance exists
			if (Instance != null && Instance != this)
			{
				Debug.LogWarning("UIHandler: Destroying duplicate UIHandler instance.");
				Destroy(gameObject);
				return;
			}
			Instance = this; // Assign singleton instance

			m_Document = GetComponent<UIDocument>();

			if (m_Document == null)
			{
				Debug.LogError("UIHandler: UIDocument component not found on this GameObject. UI will not function.");
				enabled = false;
				return;
			}

			// Important: Check rootVisualElement after a short delay if it's not immediately available
			// This can sometimes happen if the UXML parsing isn't fully complete in the same frame as Awake.
			// However, the Script Execution Order is the more robust fix.
			if (m_Document.rootVisualElement == null)
			{
				Debug.LogError("UIHandler: UIDocument's rootVisualElement is null in Awake. Is the UXML asset assigned to the UIDocument's 'Source Asset' field? Disabling UIHandler.");
				enabled = false;
				return;
			}

			// Query UI elements
			m_InventorySlots = m_Document.rootVisualElement.Query<VisualElement>("InventoryEntry").ToList();
			m_ItemCountLabels = m_Document.rootVisualElement.Query<Label>("ItemCount").ToList();

			// Setup Inventory Slot click handlers
			for (int i = 0; i < m_InventorySlots.Count; ++i)
			{
				int i1 = i;
				// Assuming 'Clickable' is from Template2DCommon. If not, replace with .RegisterCallback<ClickEvent>
				m_InventorySlots[i].AddManipulator(new Clickable(() =>
				{
					if (GameManager.Instance != null && GameManager.Instance.Player != null)
					{
						GameManager.Instance.Player.ChangeEquipItem(i1);
					}
					else
					{
						Debug.LogWarning("UIHandler: GameManager or Player is null when changing equip item.");
					}
				}));
			}

			// Check inventory slot count consistency
			// This check should ideally be done after InventorySystem is fully initialized if its size is dynamic.
			// For now, assuming InventorySystem.InventorySize is a reliable constant.
			if (InventorySystem.InventorySize != m_InventorySlots.Count)
			{
				Debug.LogWarning($"UIHandler: Inventory system size ({InventorySystem.InventorySize}) does not match the number of 'InventoryEntry' elements in UXML ({m_InventorySlots.Count}). Please ensure they match.");
			}

			m_CointCounter = m_Document.rootVisualElement.Q<Label>("CoinAmount");
			if (m_CointCounter == null)
			{
				Debug.LogWarning("UIHandler: 'CoinAmount' label not found.");
			}

			// Market Popup setup
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

				m_MarketPopup.style.display = DisplayStyle.None; // Hide by default

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
				Debug.LogWarning("UIHandler: 'Timer' label not found.");
			}

			// Settings Menu setup
			m_SettingMenu = m_Document.rootVisualElement.Q<VisualElement>("SettingMenu");
			if (m_SettingMenu != null)
			{
				// Assuming a button named "MenuButton" or "SettingsButton" in your main HUD to open this menu
				m_SettingsButton = m_Document.rootVisualElement.Q<Button>("MenuButton"); // Or "SettingsButton"
				if (m_SettingsButton != null)
				{
					m_SettingsButton.clicked += OpenSettingMenu;
				}
				else
				{
					Debug.LogWarning("UIHandler: 'MenuButton' (or 'SettingsButton') not found in UXML. Cannot open settings menu.");
				}

				m_SettingMenu.Q<Button>("ResumeButton").clicked += CloseSettingMenu;
				m_SettingMenu.Q<Button>("ExitButton").clicked += () => { Application.Quit(); };
				m_SettingMenu.Q<Button>("GameOverButton").clicked += OpenGameOverScreen; // Link Game Over button
				m_SettingMenu.style.display = DisplayStyle.None; // Hide by default
			}
			else
			{
				Debug.LogWarning("UIHandler: 'SettingMenu' not found in UXML.");
			}

			// Crafting Popup setup
			m_CraftingPopup = m_Document.rootVisualElement.Q<VisualElement>("CraftingPopup");
			if (m_CraftingPopup != null)
			{
				m_CraftingPopup.Q<Button>("CloseButton").clicked += CloseCraftingMenu;
				m_CraftingScrollView = m_CraftingPopup.Q<ScrollView>("CraftingScrollView");
				m_CraftingPopup.style.display = DisplayStyle.None; // Hide by default
																   // m_Document.rootVisualElement.Q<Button>("CraftingButton").clicked += OpenCraftingMenu;
			}
			else
			{
				Debug.LogWarning("UIHandler: 'CraftingPopup' not found in UXML.");
			}

			// Game Over Screen setup
			m_GameOverScreen = m_Document.rootVisualElement.Q<VisualElement>("GameOverScreen");
			if (m_GameOverScreen != null)
			{
				m_GameOverScreen.Q<Button>("RestartButton").clicked += () =>
				{
					// Logic to restart the game, e.g., reload the current scene
					UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
				};
				m_GameOverScreen.style.display = DisplayStyle.None; // Hide by default
			}
			else
			{
				Debug.LogWarning("UIHandler: 'GameOverScreen' not found in UXML.");
			}


			// Warehouse UI setup (assuming WarehouseUI is a class that takes a VisualElement and template)
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

			// Blocker setup
			m_Blocker = m_Document.rootVisualElement.Q<VisualElement>("Blocker");
			if (m_Blocker != null)
			{
				m_Blocker.style.opacity = 1.0f; // Start fully opaque for initial fade-in
				m_Blocker.RegisterCallback<TransitionEndEvent>(evt => { m_FadeFinishClbk?.Invoke(); });
			}
			else
			{
				Debug.LogWarning("UIHandler: 'Blocker' not found in UXML. Fade effects will not work.");
			}

			// Weather labels setup
			m_SunLabel = m_Document.rootVisualElement.Q<Label>("SunLabel");
			if (m_SunLabel != null)
			{
				m_SunLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Sun); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'SunLabel' not found.");
			}

			m_RainLabel = m_Document.rootVisualElement.Q<Label>("RainLabel");
			if (m_RainLabel != null)
			{
				m_RainLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Rain); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'RainLabel' not found.");
			}

			m_ThunderLabel = m_Document.rootVisualElement.Q<Label>("ThunderLabel");
			if (m_ThunderLabel != null)
			{
				m_ThunderLabel.AddManipulator(new Clickable(() => { if (GameManager.Instance?.WeatherSystem != null) { GameManager.Instance.WeatherSystem.ChangeWeather(WeatherSystem.WeatherType.Thunder); } }));
			}
			else
			{
				Debug.LogWarning("UIHandler: 'ThunderLabel' not found.");
			}
		}

		private void Start()
		{
			// Perform initial fade from black after all Awake methods have run
			// Check s_Instance explicitly before using it in a static call, though it should be set by now.
			if (Instance != null && Instance.m_Blocker != null)
			{
				FadeFromBlack(() => { Debug.Log("Initial fade from black complete."); });
			}
			else
			{
				// This warning indicates the Script Execution Order is likely incorrect for UIHandler.
				Debug.LogWarning("UIHandler Start: Cannot perform initial fade. UIHandler.s_Instance or m_Blocker is null. Check Script Execution Order.");
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
			if (GameManager.Instance != null && m_TimerLabel != null)
			{
				m_TimerLabel.text = GameManager.Instance.CurrentTimeAsString();
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

		// --- Static Public Methods for External Calls ---

		public static void UpdateInventory(InventorySystem system)
		{
			if (Instance != null)
			{
				Instance.UpdateInventory_Internal(system);
			}
			else
			{
				Debug.LogWarning("UIHandler.UpdateInventory: s_Instance is null. UI update skipped.");
			}
		}

		public static void UpdateCoins(int amount)
		{
			if (Instance != null && Instance.m_CointCounter != null)
			{
				Instance.UpdateCoins_Internal(amount);
			}
			else
			{
				Debug.LogWarning("UIHandler.UpdateCoins: s_Instance or m_CointCounter is null. Coin update skipped.");
			}
		}

		public static void OpenMarket()
		{
			if (Instance != null)
			{
				Instance.OpenMarket_Internal();
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

		public static void CloseMarket()
		{
			if (Instance != null && Instance.m_MarketPopup != null)
			{
				if (SoundManager.Instance != null)
				{
					SoundManager.Instance.PlayUISound();
				}

				Instance.m_MarketPopup.style.display = DisplayStyle.None;
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

		public static void OpenWarehouse()
		{
			if (Instance != null && Instance.m_WarehouseUI != null)
			{
				Instance.m_WarehouseUI.Open();
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenWarehouse: s_Instance or m_WarehouseUI is null. Warehouse cannot be opened.");
			}
		}

		public static void OpenCraftingMenu()
		{
			if (Instance != null && Instance.m_CraftingPopup != null)
			{
				Instance.OpenCraftingMenu_Internal();
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Pause();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenCraftingMenu: s_Instance or m_CraftingPopup is null. Crafting menu cannot be opened.");
			}
		}

		public static void CloseCraftingMenu()
		{
			if (Instance != null && Instance.m_CraftingPopup != null)
			{
				Instance.m_CraftingPopup.style.display = DisplayStyle.None;
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Resume();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.CloseCraftingMenu: s_Instance or m_CraftingPopup is null. Crafting menu cannot be closed.");
			}
		}

		public static void OpenSettingMenu()
		{
			if (Instance != null && Instance.m_SettingMenu != null)
			{
				Instance.m_SettingMenu.style.display = DisplayStyle.Flex;
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Pause();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenSettingMenu: s_Instance or m_SettingMenu is null. Settings menu cannot be opened.");
			}
		}

		public static void CloseSettingMenu()
		{
			if (Instance != null && Instance.m_SettingMenu != null)
			{
				Instance.m_SettingMenu.style.display = DisplayStyle.None;
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Resume();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.CloseSettingMenu: s_Instance or m_SettingMenu is null. Settings menu cannot be closed.");
			}
		}

		public static void OpenGameOverScreen()
		{
			if (Instance != null && Instance.m_GameOverScreen != null)
			{
				Instance.m_GameOverScreen.style.display = DisplayStyle.Flex;
				if (GameManager.Instance != null)
				{
					GameManager.Instance.Pause();
				}
			}
			else
			{
				Debug.LogWarning("UIHandler.OpenGameOverScreen: s_Instance or m_GameOverScreen is null. Game Over screen cannot be opened.");
			}
		}

		public static void ChangeCursor(CursorType cursorType)
		{
			if (Instance == null)
			{
				return;
			}

			if (Instance.m_HaveFocus)
			{
				switch (cursorType)
				{
					case CursorType.Interact:
						Cursor.SetCursor(Instance.InteractCursor, Vector2.zero, CursorMode.Auto);
						break;
					case CursorType.Normal:
						Cursor.SetCursor(Instance.NormalCursor, Vector2.zero, CursorMode.Auto);
						break;
					case CursorType.System:
						Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
						break;
					default:
						break;
				}
			}
			Instance.m_CurrentCursorType = cursorType;
		}

		public static void UpdateWeatherIcons(WeatherSystem.WeatherType currentWeather)
		{
			if (Instance == null)
			{
				return;
			}

			Instance.m_SunLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Sun);
			Instance.m_RainLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Rain);
			Instance.m_ThunderLabel?.EnableInClassList("on-button", currentWeather == WeatherSystem.WeatherType.Thunder);

			Instance.m_SunLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Sun);
			Instance.m_RainLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Rain);
			Instance.m_ThunderLabel?.EnableInClassList("off-button", currentWeather != WeatherSystem.WeatherType.Thunder);
		}

		public static void SceneLoaded()
		{
			if (Instance == null)
			{
				return;
			}

			if (Instance.m_SunLabel != null && Instance.m_SunLabel.parent != null)
			{
				Instance.m_SunLabel.parent.style.display =
					GameManager.Instance.WeatherSystem == null ? DisplayStyle.None : DisplayStyle.Flex;
			}
		}

		public static void FadeToBlack(System.Action onFinished)
		{
			if (Instance == null || Instance.m_Blocker == null)
			{
				Debug.LogWarning("UIHandler.FadeToBlack: s_Instance or m_Blocker is null. Fade skipped.");
				onFinished?.Invoke();
				return;
			}

			Instance.m_FadeFinishClbk = onFinished;
			Instance.m_Blocker.style.transitionDuration = new List<TimeValue> { new(0.5f, TimeUnit.Second) };
			Instance.m_Blocker.style.opacity = 1.0f;
			Debug.Log("UIHandler: Fading to black.");
		}

		public static void FadeFromBlack(System.Action onFinished)
		{
			if (Instance == null || Instance.m_Blocker == null)
			{
				Debug.LogWarning("UIHandler.FadeFromBlack: s_Instance or m_Blocker is null. Fade skipped.");
				onFinished?.Invoke();
				return;
			}

			Instance.m_FadeFinishClbk = onFinished;
			Instance.m_Blocker.style.transitionDuration = new List<TimeValue> { new(0.5f, TimeUnit.Second) };
			Instance.m_Blocker.style.opacity = 0.0f;
			Debug.Log("UIHandler: Fading from black.");
		}

		// --- Internal Helper Methods ---

		private void OpenMarket_Internal()
		{
			if (m_MarketPopup == null)
			{
				return;
			}

			m_MarketPopup.style.display = DisplayStyle.Flex;
			ToggleToSell();
			if (GameManager.Instance != null && GameManager.Instance.Player != null)
			{
				GameManager.Instance.Player.ToggleControl(false);
			}
		}

		private void OpenCraftingMenu_Internal()
		{
			if (m_CraftingPopup == null)
			{
				return;
			}

			m_CraftingPopup.style.display = DisplayStyle.Flex;
			PopulateCraftingMenu();
			if (GameManager.Instance != null && GameManager.Instance.Player != null)
			{
				GameManager.Instance.Player.ToggleControl(false);
			}
		}

		private void PopulateCraftingMenu()
		{
			if (m_CraftingScrollView == null)
			{
				return;
			}

			m_CraftingScrollView.contentContainer.Clear();

			if (CraftingEntryTemplate != null)
			{
				if (GameManager.Instance?.Player?.CraftingManager?.KnownRecipes != null)
				{
					foreach (RecipeData recipe in GameManager.Instance.Player.CraftingManager.KnownRecipes)
					{
						TemplateContainer recipeClone = CraftingEntryTemplate.CloneTree();
						recipeClone.Q<Label>("RecipeName").text = recipe.RecipeName;

						VisualElement ingredientsContainer = recipeClone.Q<VisualElement>("IngredientsContainer");
						if (ingredientsContainer != null)
						{
							ingredientsContainer.Clear();
							foreach (RecipeData.Ingredient ingredient in recipe.Ingredients)
							{
								Label ingredientLabel = new($"{ingredient.Quantity}x {ingredient.Item?.DisplayName ?? "Unknown Item"}");
								ingredientLabel.style.color = Color.white;
								ingredientsContainer.Add(ingredientLabel);
							}
						}

						VisualElement productContainer = recipeClone.Q<VisualElement>("ProductContainer");
						productContainer?.Clear();


						Button craftButton = recipeClone.Q<Button>("CraftButton");
						craftButton.text = "Craft";

						if (GameManager.Instance.Player.CraftingManager.CanCraft(recipe))
						{
							craftButton.SetEnabled(true);
							craftButton.clicked += () =>
							{
								if (GameManager.Instance?.Player != null)
								{
									if (GameManager.Instance.Player.CraftingManager.CanCraft(recipe))
									{
										Debug.Log($"Successfully crafted {recipe.RecipeName}!");
										if (SoundManager.Instance != null && PieCraftSound != null)
										{
											SoundManager.Instance.PlaySFXAt(PieCraftSound, Vector3.zero);
										}

										PopulateCraftingMenu();
									}
									else
									{
										Debug.LogWarning($"Failed to craft {recipe.RecipeName}. Check inventory and recipe conditions.");
									}
								}
							};
						}
						else
						{
							craftButton.SetEnabled(false);
							craftButton.text = "Cannot Craft";
						}

						m_CraftingScrollView.Add(recipeClone);
					}
				}
				else
				{
					Debug.LogWarning("UIHandler: CraftingManager or its KnownRecipes list is null. Cannot populate crafting menu.");
				}
			}
			else
			{
				Debug.LogError("UIHandler: CraftingEntryTemplate is null. Cannot populate crafting menu.");
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

				if (MarketEntryTemplate == null)
				{
					Debug.LogError("UIHandler: MarketEntryTemplate is null. Cannot generate market entries.");
					return;
				}

				TemplateContainer clone = MarketEntryTemplate.CloneTree();

				clone.Q<Label>("ItemName").text = item.DisplayName;
				clone.Q<VisualElement>("ItemIcon").style.backgroundImage = new StyleBackground(item.ItemSprite);


				Button button = clone.Q<Button>("ActionButton");

				if (item is Product product)
				{
					int count = GameManager.Instance.Player.Inventory.Entries[i].StackSize;
					button.text = $"Sell {count} for {product.SellPrice * count}";

					int i1 = i;
					button.clicked += () =>
					{
						if (GameManager.Instance != null && GameManager.Instance.Player != null)
						{
							GameManager.Instance.Player.SellItem(i1, count);
							ToggleToSell();
						}
					};
				}
				else
				{
					button.SetEnabled(false);
					button.text = "Cannot Sell";
				}
				m_MarketContentScrollview.Add(clone);
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
				clone.Q<VisualElement>("ItemIcon").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = clone.Q<Button>("ActionButton");

				if (GameManager.Instance.Player.Coins >= item.BuyPrice && GameManager.Instance.Player.CanFitInInventory(item, 1))
				{
					button.text = $"Buy 1 for {item.BuyPrice}";
					int i1 = i;
					button.clicked += () =>
					{
						if (GameManager.Instance != null && GameManager.Instance.Player != null)
						{
							if (GameManager.Instance.Player.BuyItem(item))
							{
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
				m_MarketContentScrollview.Add(clone);
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
				if (i >= m_InventorySlots.Count || i >= m_ItemCountLabels.Count)
				{
					Debug.LogWarning($"UIHandler: Inventory slot index {i} out of bounds for UI elements. Check UXML 'InventoryEntry' count.");
					break;
				}

				Item item = system.Entries[i].Item;
				VisualElement itemIconElement = m_InventorySlots[i].Q<VisualElement>("ItemIcon");
				if (itemIconElement != null)
				{
					itemIconElement.style.backgroundImage =
						item == null ? new StyleBackground((Sprite)null) : new StyleBackground(item.ItemSprite);
				}
				else
				{
					Debug.LogWarning($"UIHandler: 'ItemIcon' VisualElement not found inside InventoryEntry {i}.");
				}


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
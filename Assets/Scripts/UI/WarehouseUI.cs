//WerehouseUI.cs
using Template2DCommon;
using UnityEngine.UIElements;

namespace HappyHarvest
{
	/// <summary>
	/// Handle the WarehouseUI that handle storing/retrieving object.
	/// </summary>
	public class WarehouseUI
	{
		private readonly VisualElement m_Root;

		private readonly VisualTreeAsset m_EntryTemplate;

		private readonly Button m_StoreButton;
		private readonly Button m_RetrieveButton;

		private readonly ScrollView m_Scrollview;

		public WarehouseUI(VisualElement root, VisualTreeAsset entryTemplate)
		{
			m_Root = root;

			m_EntryTemplate = entryTemplate;

			m_StoreButton = m_Root.Q<Button>("StoreButton");
			m_StoreButton.clicked += OpenStore;

			m_RetrieveButton = m_Root.Q<Button>("RetrieveButton");
			m_RetrieveButton.clicked += OpenRetrieve;

			m_Root.Q<Button>("CloseButton").clicked += Close;

			m_Scrollview = m_Root.Q<ScrollView>("ContentScrollView");
		}

		public void Open()
		{
			m_Root.visible = true;
			GameManager.Instance.Pause();
			SoundManager.Instance.PlayUISound();
		}

		public void Close()
		{
			m_Root.visible = false;
			GameManager.Instance.Resume();
		}

		private void OpenStore()
		{
			m_StoreButton.AddToClassList("activeButton");
			m_RetrieveButton.RemoveFromClassList("activeButton");

			m_StoreButton.SetEnabled(false);
			m_RetrieveButton.SetEnabled(true);

			m_Scrollview.contentContainer.Clear();

			PlayerController player = GameManager.Instance.Player;

			for (int i = 0; i < player.Inventory.Entries.Length; ++i)
			{
				InventorySlot invEntry = player.Inventory.Entries[i];
				Item item = invEntry.Item;

				if (item == null)
				{
					continue;
				}

				TemplateContainer entry = m_EntryTemplate.CloneTree();
				entry.Q<Label>("ItemName").text = item.DisplayName;
				entry.Q<VisualElement>("ItemIcone").style.backgroundImage = new StyleBackground(item.ItemSprite);

				Button button = entry.Q<Button>("ActionButton");
				int i1 = i;
				button.clicked += () =>
				{
					_ = player.Inventory.Remove(i1, invEntry.StackSize);
					m_Scrollview.contentContainer.Remove(entry);
				};

				button.text = "Store";

				m_Scrollview.contentContainer.Add(entry);
			}
		}

		private void OpenRetrieve()
		{
			m_RetrieveButton.AddToClassList("activeButton");
			m_StoreButton.RemoveFromClassList("activeButton");

			m_RetrieveButton.SetEnabled(false);
			m_StoreButton.SetEnabled(true);

			m_Scrollview.contentContainer.Clear();
			_ = GameManager.Instance.Player.Inventory;
		}
	}
}

//SettingMenu.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Template2DCommon
{
	public class SettingMenu
	{
		public System.Action OnClose;
		public System.Action OnOpen;

		private readonly VisualElement m_Root;
		private readonly Button m_OpenMenu;

		private readonly Button m_CloseButton;
		private readonly Button m_QuitButton;

		private readonly DropdownField m_ResolutionDropdown;
		private readonly Toggle m_FullscreenToggle;

		private readonly Slider m_MainVolumeSlider;
		private readonly Slider m_BGMVolumeSlider;
		private readonly Slider m_SFXVolumeSlider;

		private readonly List<Resolution> m_AvailableResolutions;

		public SettingMenu(VisualElement root)
		{
			m_Root = root.Q<VisualElement>("SettingMenu");
			m_OpenMenu = root.Q<Button>("OpenSettingMenuButton");

			m_CloseButton = m_Root.Q<Button>("CloseButton");
			m_QuitButton = m_Root.Q<Button>("QuitButton");

			m_ResolutionDropdown = m_Root.Q<DropdownField>("ResolutionDropdown");
			m_FullscreenToggle = m_Root.Q<Toggle>("FullscreenToggle");

			m_MainVolumeSlider = m_Root.Q<Slider>("MainVolume");
			m_BGMVolumeSlider = m_Root.Q<Slider>("MusicVolume");
			m_SFXVolumeSlider = m_Root.Q<Slider>("SFXVolume");

			_ = m_MainVolumeSlider.RegisterValueChangedCallback(evt =>
			{
				SoundManager.Instance.Sound.MainVolume = evt.newValue;
				SoundManager.Instance.UpdateVolume();
			});
			_ = m_BGMVolumeSlider.RegisterValueChangedCallback(evt =>
			{
				SoundManager.Instance.Sound.BGMVolume = evt.newValue;
				SoundManager.Instance.UpdateVolume();
			});
			_ = m_SFXVolumeSlider.RegisterValueChangedCallback(evt =>
			{
				SoundManager.Instance.Sound.SFXVolume = evt.newValue;
				SoundManager.Instance.UpdateVolume();
			});

			m_Root.visible = false;

			m_OpenMenu.clicked += () =>
			{
				if (m_Root.visible)
				{
					Close();
				}
				else
				{
					Open();
				}
			};

			m_CloseButton.clicked += Close;
			m_QuitButton.clicked += Application.Quit;

			//fill resolution dropdown
			m_AvailableResolutions = new List<Resolution>();

			List<string> resEntries = new();
			foreach (Resolution resolution in Screen.resolutions)
			{
				//if we already have a resolution with same width & height, we skip.
				if (m_AvailableResolutions.FindIndex(r => r.width == resolution.width && r.height == resolution.height) != -1)
				{
					continue;
				}

				string resName = resolution.width + "x" + resolution.height;
				resEntries.Add(resName);
				m_AvailableResolutions.Add(resolution);

			}

			m_ResolutionDropdown.choices = resEntries;

			_ = m_ResolutionDropdown.RegisterValueChangedCallback(evt =>
			{
				if (m_ResolutionDropdown.index == -1)
				{
					return;
				}

				Resolution res = m_AvailableResolutions[m_ResolutionDropdown.index];
				Screen.SetResolution(res.width, res.height, m_FullscreenToggle.value);
			});

			m_FullscreenToggle.value = Screen.fullScreen;
			_ = m_FullscreenToggle.RegisterValueChangedCallback(evt =>
			{
				Screen.fullScreen = evt.newValue;
			});
		}

		private void Open()
		{
			m_MainVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.Sound.MainVolume);
			m_BGMVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.Sound.BGMVolume);
			m_SFXVolumeSlider.SetValueWithoutNotify(SoundManager.Instance.Sound.SFXVolume);

			string currentRes = Screen.width + "x" + Screen.height;
			m_ResolutionDropdown.label = currentRes;
			m_ResolutionDropdown.SetValueWithoutNotify(currentRes);

			m_Root.visible = true;
			OnOpen.Invoke();
		}

		private void Close()
		{
			SoundManager.Instance.PlayUISound();
			SoundManager.Instance.Save();
			m_Root.visible = false;
			OnClose.Invoke();
		}
	}
}
//DayCycleHandler.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace HappyHarvest
{
	[DefaultExecutionOrder(10)] // Ensures it runs early, but after GameManager (if GameManager is <10)
	public class DayCycleHandler : MonoBehaviour
	{
		public Transform LightsRoot;

		[Header("Day Light")]
		public Light2D DayLight;
		public Gradient DayLightGradient;

		[Header("Night Light")]
		public Light2D NightLight;
		public Gradient NightLightGradient;

		[Header("Ambient Light")]
		public Light2D AmbientLight;
		public Gradient AmbientLightGradient;

		[Header("RimLights")]
		public Light2D SunRimLight;
		public Gradient SunRimLightGradient;
		public Light2D MoonRimLight;
		public Gradient MoonRimLightGradient;

		[Tooltip("The angle 0 = upward, going clockwise to 1 along the day")]
		public AnimationCurve ShadowAngle;
		[Tooltip("The scale of the normal shadow length (0 to 1) along the day")]
		public AnimationCurve ShadowLength;

		private readonly List<ShadowInstance> m_Shadows = new();
		private readonly List<LightInterpolator> m_LightBlenders = new();

		private void Awake()
		{
			if (GameManager.Instance != null)
			{
				Debug.LogError("DayCycleHandler: GameManager.Instance is null in Awake! Check Script Execution Order. This DayCycleHandler will not be registered.");
			}
		}

		public void Tick()
		{
			// Ensure GameManager.Instance is not null before accessing CurrentDayRatio
			if (GameManager.Instance != null)
			{
				UpdateLight(GameManager.Instance.CurrentDayRatio);
			}
			else
			{
				// Debug.LogWarning("DayCycleHandler: GameManager.Instance is null in Tick. Skipping light update.");
			}
		}

		public void UpdateLight(float ratio)
		{
			if (DayLight != null)
			{
				DayLight.color = DayLightGradient.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("DayLight is not assigned in DayCycleHandler.");
			}

			if (NightLight != null)
			{
				NightLight.color = NightLightGradient.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("NightLight is not assigned in DayCycleHandler.");
			}

			if (AmbientLight != null)
			{
				AmbientLight.color = AmbientLightGradient.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("AmbientLight is not assigned in DayCycleHandler.");
			}

			if (SunRimLight != null)
			{
				SunRimLight.color = SunRimLightGradient.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("SunRimLight is not assigned in DayCycleHandler.");
			}

			if (MoonRimLight != null)
			{
				MoonRimLight.color = MoonRimLightGradient.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("MoonRimLight is not assigned in DayCycleHandler.");
			}

			if (LightsRoot != null)
			{
				LightsRoot.rotation = Quaternion.Euler(0, 0, 360.0f * ratio);
			}
			else
			{
				Debug.LogWarning("LightsRoot is not assigned in DayCycleHandler. Cannot rotate lights.");
			}

			UpdateShadow(ratio);
		}

		private void UpdateShadow(float ratio)
		{
			float currentShadowAngle = 0;
			if (ShadowAngle != null)
			{
				currentShadowAngle = ShadowAngle.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("ShadowAngle AnimationCurve is not assigned in DayCycleHandler.");
			}

			float currentShadowLength = 0;
			if (ShadowLength != null)
			{
				currentShadowLength = ShadowLength.Evaluate(ratio);
			}
			else
			{
				Debug.LogWarning("ShadowLength AnimationCurve is not assigned in DayCycleHandler.");
			}

			while (currentShadowAngle > 1.0f)
			{
				currentShadowAngle -= 1.0f;
			}

			foreach (ShadowInstance shadow in m_Shadows)
			{
				if (shadow != null && shadow.transform != null)
				{
					Transform t = shadow.transform;
					t.eulerAngles = new Vector3(0, 0, currentShadowAngle * 360.0f);
					t.localScale = new Vector3(1, 1f * shadow.BaseLength * currentShadowLength, 1);
				}
			}
		}

		// --- SAVE/LOAD IMPLEMENTATIONS ---
		/// <summary>
		/// Saves the current state of the DayCycleHandler to the provided save data struct.
		/// </summary>
		/// <param name="data">The DayCycleHandlerSaveData struct to populate.</param>
		public void Save(ref DayCycleHandlerSaveData data)
		{
			// Assuming GameManager.Instance.CurrentDayRatio is the authoritative time
			if (GameManager.Instance != null)
			{
				data.TimeOfTheDay = GameManager.Instance.CurrentDayRatio;
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.Save: GameManager.Instance is null. Cannot save current day ratio.");
				data.TimeOfTheDay = 0.0f; // Default to 0 if GameManager isn't available
			}
			Debug.Log($"DayCycleHandler: Saved time {data.TimeOfTheDay}");
		}

		/// <summary>
		/// Loads the state of the DayCycleHandler from the provided save data struct.
		/// </summary>
		/// <param name="data">The DayCycleHandlerSaveData struct to load from.</param>
		public void Load(DayCycleHandlerSaveData data)
		{
			if (GameManager.Instance != null)
			{
				GameManager.Instance.CurrentDayRatio = data.TimeOfTheDay;
				UpdateLight(GameManager.Instance.CurrentDayRatio); // Immediately update lights based on loaded time
				Debug.Log($"DayCycleHandler: Loaded time {data.TimeOfTheDay}");
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.Load: GameManager.Instance is null. Cannot load day ratio.");
			}
		}

		// --- FIXED REGISTER/UNREGISTER METHODS ---
		public static void RegisterShadow(ShadowInstance shadow)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DayCycleHandler instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
				if (instance != null) { instance.m_Shadows.Add(shadow); }
				return;
			}
#endif
			if (GameManager.Instance != null && GameManager.Instance.DayCycleHandler != null)
			{
				GameManager.Instance.DayCycleHandler.m_Shadows.Add(shadow);
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.RegisterShadow: GameManager.Instance or DayCycleHandler is null. Shadow not registered at runtime.");
			}
		}

		public static void UnregisterShadow(ShadowInstance shadow)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DayCycleHandler instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
				if (instance != null) { instance.m_Shadows.Remove(shadow); }
				return;
			}
#endif
			if (GameManager.Instance != null && GameManager.Instance.DayCycleHandler != null)
			{
				GameManager.Instance.DayCycleHandler.m_Shadows.Remove(shadow);
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.UnregisterShadow: GameManager.Instance or DayCycleHandler is null. Shadow not unregistered at runtime.");
			}
		}

		public static void RegisterLightBlender(LightInterpolator interpolator)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DayCycleHandler instance = FindFirstObjectByType<DayCycleHandler>();
				if (instance != null) { instance.m_LightBlenders.Add(interpolator); }
				return;
			}
#endif
			if (GameManager.Instance != null && GameManager.Instance.DayCycleHandler != null)
			{
				GameManager.Instance.DayCycleHandler.m_LightBlenders.Add(interpolator);
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.RegisterLightBlender: GameManager.Instance or DayCycleHandler is null. LightInterpolator not registered at runtime.");
			}
		}

		public static void UnregisterLightBlender(LightInterpolator interpolator)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				DayCycleHandler instance = FindFirstObjectByType<DayCycleHandler>();
				if (instance != null) { instance.m_LightBlenders.Remove(interpolator); }
				return;
			}
#endif
			if (GameManager.Instance != null && GameManager.Instance.DayCycleHandler != null)
			{
				GameManager.Instance.DayCycleHandler.m_LightBlenders.Remove(interpolator);
			}
			else
			{
				Debug.LogWarning("DayCycleHandler.UnregisterLightBlender: GameManager.Instance or DayCycleHandler is null. LightInterpolator not unregistered at runtime.");
			}
		}
	}
#pragma warning disable IDE0060 // Remove unused parameter
	public class LightInterpolator : MonoBehaviour { public void SetRatio(float ratio) { /* ... */ } /* ... */ }
#pragma warning restore IDE0060 // Remove unused parameter
	[System.Serializable]
	public struct DayCycleHandlerSaveData
	{
		public float TimeOfTheDay;
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(DayCycleHandler))]
	public class DayCycleEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			DayCycleHandler handler = (DayCycleHandler)target;

			GUILayout.Space(10);
			GUILayout.Label("Editor Preview Controls", EditorStyles.boldLabel);

			// Allow changing CurrentDayRatio in editor for preview
			float newRatio = EditorGUILayout.Slider("Current Day Ratio", GameManager.Instance.CurrentDayRatio, 0f, 1f);
			if (newRatio != GameManager.Instance.CurrentDayRatio)
			{
				GameManager.Instance.CurrentDayRatio = newRatio;
				handler.UpdateLight(newRatio); // Immediately update lights in editor
			}

			EditorGUILayout.LabelField("Time:", GameManager.GetTimeAsString(GameManager.Instance.CurrentDayRatio));
		}
	}
#endif
}
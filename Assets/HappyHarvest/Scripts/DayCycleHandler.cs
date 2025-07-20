// HappyHarvest/DayCycleHandler.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

namespace HappyHarvest
{
    [DefaultExecutionOrder(10)]
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

        private List<ShadowInstance> m_Shadows = new();
        private List<LightInterpolator> m_LightBlenders = new();

        private void Awake()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.DayCycleHandler = this;
            }
            else
            {
                Debug.LogError("DayCycleHandler: GameManager.Instance is null in Awake! Check Script Execution Order. This DayCycleHandler will not be registered.");
            }
        }

        // ... (Tick, UpdateLight, UpdateShadow methods) ...

        // --- FIXED REGISTER/UNREGISTER METHODS ---

        public static void RegisterShadow(ShadowInstance shadow)
        {
            // This part runs ONLY IN EDITOR, when Application.isPlaying is false
            // (e.g., for previewing in the scene view without running the game)
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
                if (instance != null)
                {
                    instance.m_Shadows.Add(shadow);
                }
                return; // Return early for editor-only case
            }
#endif
            // This part runs at RUNTIME (both in Editor and in Builds)
            if (GameManager.Instance != null && GameManager.Instance.DayCycleHandler != null)
            {
                GameManager.Instance.DayCycleHandler.m_Shadows.Add(shadow);
            }
            else
            {
                // This might happen if GameManager/DayCycleHandler isn't set up yet,
                // which suggests a script execution order issue for runtime.
                Debug.LogWarning("DayCycleHandler.RegisterShadow: GameManager.Instance or DayCycleHandler is null. Shadow not registered at runtime.");
            }
        }

        public static void UnregisterShadow(ShadowInstance shadow)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                var instance = GameObject.FindFirstObjectByType<DayCycleHandler>();
                if (instance != null)
                {
                    instance.m_Shadows.Remove(shadow);
                }
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
                var instance = FindFirstObjectByType<DayCycleHandler>();
                if (instance != null)
                {
                    instance.m_LightBlenders.Add(interpolator);
                }
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
                var instance = FindFirstObjectByType<DayCycleHandler>();
                if (instance != null)
                {
                    instance.m_LightBlenders.Remove(interpolator);
                }
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

        public void Tick()
        {
            // Ensure GameManager.Instance is not null before accessing CurrentDayRatio
            if (GameManager.Instance != null)
            {
                UpdateLight(GameManager.Instance.CurrentDayRatio);
            }
            else
            {
                // If GameManager is null, we can't get the ratio, so skip update or use a default
                // Debug.LogWarning("DayCycleHandler: GameManager.Instance is null in Tick. Skipping light update.");
            }
        }

        public void UpdateLight(float ratio)
        {
            // Add null checks for all Light2D references before using them
            if (DayLight != null)
            {
                DayLight.color = DayLightGradient.Evaluate(ratio);
            }
            else
            {
                Debug.LogWarning("DayLight is not assigned in DayCycleHandler. UpdateLight cannot set its color.");
            }

            if (NightLight != null)
            {
                NightLight.color = NightLightGradient.Evaluate(ratio);
            }
            else
            {
                Debug.LogWarning("NightLight is not assigned in DayCycleHandler. UpdateLight cannot set its color.");
            }

            // Removed UNITY_EDITOR checks as they are now redundant with the direct null checks
            if (AmbientLight != null)
            {
                AmbientLight.color = AmbientLightGradient.Evaluate(ratio);
            }
            else
            {
                Debug.LogWarning("AmbientLight is not assigned in DayCycleHandler. UpdateLight cannot set its color.");
            }

            if (SunRimLight != null)
            {
                SunRimLight.color = SunRimLightGradient.Evaluate(ratio);
            }
            else
            {
                Debug.LogWarning("SunRimLight is not assigned in DayCycleHandler. UpdateLight cannot set its color.");
            }

            if (MoonRimLight != null)
            {
                MoonRimLight.color = MoonRimLightGradient.Evaluate(ratio);
            }
            else
            {
                Debug.LogWarning("MoonRimLight is not assigned in DayCycleHandler. UpdateLight cannot set its color.");
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

        void UpdateShadow(float ratio)
        {
            // Ensure AnimationCurves are not null before evaluating
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
                currentShadowAngle -= 1.0f;

            foreach (var shadow in m_Shadows)
            {
                if (shadow != null && shadow.transform != null) // Add null check for shadow itself
                {
                    var t = shadow.transform;
                    t.eulerAngles = new Vector3(0, 0, currentShadowAngle * 360.0f);
                    t.localScale = new Vector3(1, 1f * shadow.BaseLength * currentShadowLength, 1);
                }
            }

            foreach (var handler in m_LightBlenders)
            {
                if (handler != null) // Add null check for handler
                {
                    handler.SetRatio(ratio);
                }
            }
        }

        internal void Load(DayCycleHandlerSaveData timeSaveData)
        {
            throw new NotImplementedException();
        }

        internal object Save(ref DayCycleHandlerSaveData timeSaveData)
        {
            throw new NotImplementedException();
        }

        // ... rest of your code ...
    }

    // Ensure ShadowInstance and LightInterpolator classes are defined elsewhere in your project
    // Example:
    // public class ShadowInstance : MonoBehaviour { public float BaseLength; /* ... */ }
    // public class LightInterpolator : MonoBehaviour { public void SetRatio(float ratio); /* ... */ }

    [System.Serializable]
    public struct DayCycleHandlerSaveData
    {
        public float TimeOfTheDay;
    }

#if UNITY_EDITOR
    // ... DayCycleEditor code ...
#endif

}
// GameManager.cs
using UnityEngine;
using System.Collections.Generic; // For List if needed, and Item[] for MarketEntries

namespace HappyHarvest
{
    /// <summary>
    /// The central singleton manager for the HappyHarvest game.
    /// It provides global access to key game systems and manages their lifecycle.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Singleton Instance: Provides global access to the GameManager.
        // It is privately set only within its own Awake method.
        public static GameManager Instance { get; private set; }

        // --- References to other Game Systems ---
        // These properties allow other scripts to easily access major game components
        // via GameManager.Instance.PropertyName. They are set in Awake.

        public PlayerController Player { get; private set; }
        public PieScoreManager PieScoreManager { get; private set; }
        public DayCycleHandler DayCycleHandler { get; private set; }
        public ItemManager ItemManager { get; private set; }
        public TerrainManager Terrain { get; private set; } // Assuming you have a TerrainManager for grid/tilemap access
        public WeatherSystem WeatherSystem { get; private set; } // For weather control

        [Header("Market Settings")]
        [Tooltip("List of items available for purchase in the market.")]
        public Item[] MarketEntries; // Assign your buyable Item ScriptableObjects here

        // --- NEW: Property for Loaded Scene Data ---
        // This will hold the save data for the currently loaded scene.
        // It's private set because GameManager itself will manage loading/setting this.
        public SceneSaveData LoadedSceneData { get; private set; }
        // Public property to expose the current day ratio (0.0 to 1.0)
        // This would typically be managed by DayCycleHandler and exposed through a method or property.
        // For simplicity, let's assume DayCycleHandler updates this or GameManager manages it.
        // If DayCycleHandler has a public float CurrentRatio, you can remove this.
        [Tooltip("The current progress of the day (0.0 = start of day, 1.0 = end of day).")]
        public float CurrentDayRatio = 0.0f; // This should ideally be driven by DayCycleHandler

		[System.Obsolete]
		void Awake()
        {
            // --- Singleton Initialization ---
            // Ensures only one instance of GameManager exists across scenes.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject); // Destroy duplicate instances
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject); // Keep GameManager alive between scene loads

            // --- Find and Assign Sub-Managers/Systems ---
            // GameManager takes responsibility for finding and linking its sub-systems.
            // This relies on these sub-systems being present in the scene and having their Awake methods run.
            // Ensure Script Execution Order is set correctly for these dependencies.

            // ItemManager: Essential for loading items by ID (used by InventorySystem)
            ItemManager = FindObjectOfType<ItemManager>();
            if (ItemManager == null)
            {
                Debug.LogError("GameManager: ItemManager not found in the scene! Item loading will likely fail. " +
                "Please ensure an 'ItemManager' GameObject with the 'ItemManager.cs' script is in your scene.");
            }

            // PlayerController: The main player character script
            Player = FindObjectOfType<PlayerController>();
            if (Player == null)
            {
                Debug.LogWarning("GameManager: PlayerController not found in scene. Player-related functions may not work. " +
                "Ensure your Player GameObject has 'PlayerController.cs' and is in the scene.");
            }

            // PieScoreManager: Manages game scoring and rounds
            PieScoreManager = FindObjectOfType<PieScoreManager>();
            if (PieScoreManager == null)
            {
                Debug.LogWarning("GameManager: PieScoreManager not found in scene. Scoring functions may not work. " +
                "Ensure a 'PieScoreManager' GameObject with the 'PieScoreManager.cs' script is in your scene.");
            }

            // DayCycleHandler: Manages day/night cycle and lighting
            DayCycleHandler = FindObjectOfType<DayCycleHandler>();
            if (DayCycleHandler == null)
            {
                Debug.LogWarning("GameManager: DayCycleHandler not found in scene. Day/Night cycle will not function. " +
                "Ensure a 'DayCycleHandler' GameObject with the 'DayCycleHandler.cs' script is in your scene.");
            }

			// TerrainManager: Manages grid, tilemaps, and terrain interactions
#pragma warning disable CS0618 // Type or member is obsolete
			Terrain = FindObjectOfType<TerrainManager>();
#pragma warning restore CS0618 // Type or member is obsolete
			if (Terrain == null)
            {
                Debug.LogWarning("GameManager: TerrainManager not found in scene. Terrain interactions (e.g., planting) may not work. " +
                "Ensure a 'TerrainManager' GameObject with the 'TerrainManager.cs' script is in your scene.");
            }

			// WeatherSystem: Manages weather effects
#pragma warning disable CS0618 // Type or member is obsolete
			WeatherSystem = FindObjectOfType<WeatherSystem>();
#pragma warning restore CS0618 // Type or member is obsolete
			if (WeatherSystem == null)
            {
                Debug.LogWarning("GameManager: WeatherSystem not found in scene. Weather control will not function. " +
                "Ensure a 'WeatherSystem' GameObject with the 'WeatherSystem.cs' script is in your scene.");
            }

            Debug.Log("GameManager Initialized.");
        }

        void Update()
        {
            DayCycleHandler.Tick(); // Call the DayCycleHandler's tick
        }

        // --- NEW: Methods to set/get LoadedSceneData (used by SaveSystem) ---
        public void SetLoadedSceneData(SceneSaveData data)
        {
            LoadedSceneData = data;
            Debug.Log("GameManager: Loaded scene data has been set.");
            // Here, you would typically apply the loaded scene data to your scene objects.
            // For example, iterate through InteractiveObjects and restore their states.
            // This logic would be more complex and depend on how you identify and manage scene objects.
        }

        public SceneSaveData GetCurrentSceneData()
        {
            // This method would collect the current state of the scene to be saved.
            // For example, iterate through active InteractiveObjects and collect their data.
            SceneSaveData currentData = new SceneSaveData();
            // currentData.InteractiveObjects = new List<InteractiveObjectSaveData>();
            // foreach (var obj in FindObjectsOfType<InteractiveObject>()) { /* collect data */ }
            return currentData;
        }

        /// <summary>
        /// Provides the current game time as a formatted string (e.g., "08:30 AM").
        /// This would typically get the time from DayCycleHandler or an internal time system.
        /// </summary>
        /// <returns>Formatted time string.</returns>
        public string CurrentTimeAsString()
        {
            // This is a placeholder. You'd get the actual time from your DayCycleHandler
            // or a dedicated TimeSystem.
            // Example: if DayCycleHandler has a public float CurrentTimeRatio;
            // float totalMinutesInDay = 24 * 60;
            // int currentMinutes = Mathf.FloorToInt(CurrentDayRatio * totalMinutesInDay);
            // int hours = currentMinutes / 60;
            // int minutes = currentMinutes % 60;
            // return $"{hours:00}:{minutes:00}";

            // For now, return a static string or link to DayCycleHandler's actual time if it has one
            if (DayCycleHandler != null)
            {
                // Assuming DayCycleHandler has a way to give the current time ratio or formatted time
                // For example, if DayCycleHandler.Tick() updates CurrentDayRatio, you can use that.
                // Or if DayCycleHandler has a public string GetFormattedTime() method.
                // For now, let's use a simple conversion of CurrentDayRatio
                float totalHours = 24.0f;
                int hours = Mathf.FloorToInt(CurrentDayRatio * totalHours);
                int minutes = Mathf.FloorToInt((CurrentDayRatio * totalHours * 60) % 60);
                return $"{hours:00}:{minutes:00}";
            }
            return "00:00"; // Default if DayCycleHandler is not available
        }

        /// <summary>
        /// Helper to get formatted time string for UI elements (e.g. for DayCycleEditor)
        /// </summary>
        /// <param name="ratio">The time ratio (0-1) to convert.</param>
        /// <returns>Formatted time string (e.g., "06:00 AM").</returns>
        public static string GetTimeAsString(float ratio)
        {
            float totalHours = 24.0f;
            int hours = Mathf.FloorToInt(ratio * totalHours);
            int minutes = Mathf.FloorToInt((ratio * totalHours * 60) % 60);

            string amPm = "AM";
            if (hours >= 12)
            {
                amPm = "PM";
                if (hours > 12) hours -= 12;
            }
            if (hours == 0) hours = 12; // 00:xx becomes 12:xx AM

            return $"{hours:00}:{minutes:00} {amPm}";
        }


        /// <summary>
        /// Pauses the game by setting Time.timeScale to 0.
        /// </summary>
        public void Pause()
        {
            Time.timeScale = 0;
            Debug.Log("Game Paused");
        }

        /// <summary>
        /// Resumes the game by setting Time.timeScale to 1.
        /// </summary>
        public void Resume()
        {
            Time.timeScale = 1;
            Debug.Log("Game Resumed");
        }

        // You might add methods here for:
        // - Scene loading/unloading
        // - Game state management (e.g., game over, main menu, gameplay)
        // - Global event dispatching
        // - Saving/Loading game progress (orchestrating SaveSystem)
    }
}
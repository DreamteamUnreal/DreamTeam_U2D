//GameManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement; // Required for SceneManager

namespace HappyHarvest
{
    /// <summary>
    /// The central singleton manager for the HappyHarvest game.
    /// It provides global access to key game systems and manages their lifecycle.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // Singleton Instance: Provides global access to the GameManager.
        public static GameManager Instance { get; private set; }

        // --- References to other Game Systems ---
        public PlayerController Player { get; private set; }
        public PieScoreManager PieScoreManager { get; private set; }
        public DayCycleHandler DayCycleHandler { get; private set; }
        public ItemManager ItemManager { get; private set; }
        public TerrainManager Terrain { get; private set; }
        public WeatherSystem WeatherSystem { get; private set; }
        public CropDatabase CropDatabase { get; private set; }

        public Tilemap WalkSurfaceTilemap { get; set; }
        public SceneData CurrentSceneDataComponent { get; set; } // This is the MonoBehaviour component in the scene

        // --- SpawnPoint Management ---
        private List<SpawnPoint> m_SpawnPoints = new();
        public int LastSpawnIndex = 0; // Stores the spawn index for the next scene load

        // --- Day Event Handling ---
        private List<DayEventHandler> m_DayEventHandlers = new();
        // This struct helps track the state of each DayEvent (whether its OnEvent was triggered)
        private struct DayEventState
        {
            public DayEventHandler Handler;
            public int EventIndex;
            public bool WasInRange;
        }
        private List<DayEventState> m_DayEventStates = new();


        // --- Existing Properties ---
        [Header("Market Settings")]
        public Item[] MarketEntries;
        [Tooltip("The current progress of the day (0.0 = start of day, 1.0 = end of day).")]
        public float CurrentDayRatio = 0.0f; // This should ideally be driven by DayCycleHandler

		// Removed [System.Obsolete] as Awake is now fully functional
		[System.Obsolete]
		void Awake()
        {
            // --- Singleton Initialization ---
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // --- Find and Assign Sub-Managers/Systems ---
            // Ensure Script Execution Order is set correctly for these dependencies.

            ItemManager = FindObjectOfType<ItemManager>();
            if (ItemManager == null) Debug.LogError("GameManager: ItemManager not found in the scene!");

            Player = FindObjectOfType<PlayerController>();
            if (Player == null) Debug.LogWarning("GameManager: PlayerController not found in scene.");

            PieScoreManager = FindObjectOfType<PieScoreManager>();
            if (PieScoreManager == null) Debug.LogWarning("GameManager: PieScoreManager not found in scene.");

            DayCycleHandler = FindObjectOfType<DayCycleHandler>();
            if (DayCycleHandler == null) Debug.LogWarning("GameManager: DayCycleHandler not found in scene.");

            Terrain = FindObjectOfType<TerrainManager>();
            if (Terrain == null) Debug.LogWarning("GameManager: TerrainManager not found in scene.");

            WeatherSystem = FindObjectOfType<WeatherSystem>();
            if (WeatherSystem == null) Debug.LogWarning("GameManager: WeatherSystem not found in scene.");

            CropDatabase = FindObjectOfType<CropDatabase>();
            if (CropDatabase == null) Debug.LogError("GameManager: CropDatabase not found in the scene!");

            Debug.Log("GameManager Initialized.");
        }

        void Update()
        {
            // Advance Day Cycle
            DayCycleHandler?.Tick();

            // Handle Day Events
            HandleDayEvents();
        }

        // --- Day Event Management Methods ---
        public void RegisterEventHandler(DayEventHandler handler)
        {
            if (!m_DayEventHandlers.Contains(handler))
            {
                m_DayEventHandlers.Add(handler);
                // Initialize event states for the new handler
                for (int i = 0; i < handler.Events.Length; i++)
                {
                    m_DayEventStates.Add(new DayEventState
                    {
                        Handler = handler,
                        EventIndex = i,
                        WasInRange = handler.Events[i].IsInRange(CurrentDayRatio)
                    });
                }
                Debug.Log($"GameManager: Registered DayEventHandler {handler.name}");
            }
        }

        public void RemoveEventHandler(DayEventHandler handler)
        {
            if (m_DayEventHandlers.Contains(handler))
            {
                m_DayEventHandlers.Remove(handler);
                // Remove associated states
                m_DayEventStates.RemoveAll(state => state.Handler == handler);
                Debug.Log($"GameManager: Unregistered DayEventHandler {handler.name}");
            }
        }

        private void HandleDayEvents()
        {
            // Iterate through a copy to avoid issues if handlers unregister themselves during iteration
            foreach (var state in m_DayEventStates.ToList()) // .ToList() creates a copy
            {
                if (state.Handler == null || state.EventIndex >= state.Handler.Events.Length)
                {
                    // Handler or event no longer valid, will be removed on next unregister or scene load
                    continue;
                }

                var dayEvent = state.Handler.Events[state.EventIndex];
                bool isInRange = dayEvent.IsInRange(CurrentDayRatio);

                if (isInRange && !state.WasInRange)
                {
                    // Event just entered range
                    dayEvent.OnEvents?.Invoke();
                    // Update state in the list directly (need to find it)
                    int index = m_DayEventStates.FindIndex(s => s.Handler == state.Handler && s.EventIndex == state.EventIndex);
                    if (index != -1) m_DayEventStates[index] = new DayEventState { Handler = state.Handler, EventIndex = state.EventIndex, WasInRange = true };
                }
                else if (!isInRange && state.WasInRange)
                {
                    // Event just left range
                    dayEvent.OffEvent?.Invoke();
                    // Update state in the list directly
                    int index = m_DayEventStates.FindIndex(s => s.Handler == state.Handler && s.EventIndex == state.EventIndex);
                    if (index != -1) m_DayEventStates[index] = new DayEventState { Handler = state.Handler, EventIndex = state.EventIndex, WasInRange = false };
                }
            }
        }

        // --- SpawnPoint Management Methods ---
        public void RegisterSpawn(SpawnPoint spawnPoint)
        {
            if (!m_SpawnPoints.Contains(spawnPoint))
            {
                m_SpawnPoints.Add(spawnPoint);
                Debug.Log($"GameManager: Registered SpawnPoint {spawnPoint.name} (Index: {spawnPoint.SpawnIndex})");
            }
        }

        public void UnregisterSpawn(SpawnPoint spawnPoint)
        {
            if (m_SpawnPoints.Contains(spawnPoint))
            {
                m_SpawnPoints.Remove(spawnPoint);
                Debug.Log($"GameManager: Unregistered SpawnPoint {spawnPoint.name} (Index: {spawnPoint.SpawnIndex})");
            }
        }

        /// <summary>
        /// Spawns the player at a specific spawn point based on its index in the current scene.
        /// </summary>
        /// <param name="spawnIndex">The index of the desired spawn point.</param>
        public void SpawnPlayerAt(int spawnIndex)
        {
            SpawnPoint targetSpawn = m_SpawnPoints.FirstOrDefault(sp => sp.SpawnIndex == spawnIndex);

            if (targetSpawn != null)
            {
                targetSpawn.SpawnHere();
                LastSpawnIndex = spawnIndex;
                Debug.Log($"GameManager: Player spawned at index {spawnIndex}.");
            }
            else
            {
                Debug.LogWarning($"GameManager: SpawnPoint with index {spawnIndex} not found in current scene. Player not moved.");
                if (m_SpawnPoints.Count > 0)
                {
                    m_SpawnPoints[0].SpawnHere();
                    LastSpawnIndex = m_SpawnPoints[0].SpawnIndex;
                    Debug.Log($"GameManager: Player spawned at default first spawn point (Index: {m_SpawnPoints[0].SpawnIndex}).");
                }
                else
                {
                    Debug.LogError("GameManager: No SpawnPoints found in the scene at all!");
                }
            }
        }

		/// <summary>
		/// Initiates a scene transition. Saves current scene data, loads the new scene,
		/// and spawns the player at the target spawn point in the new scene.
		/// </summary>
		/// <param name="targetSceneBuildIndex">The build index of the scene to load.</param>
		/// <param name="targetSpawnIndex">The index of the spawn point in the target scene.</param>
		[System.Obsolete]
		public void MoveTo(int targetSceneBuildIndex, int targetSpawnIndex)
        {
            // 1. Save current scene's data before leaving it
            SaveSystem.SaveCurrentSceneDataToLookup();

            // 2. Store the target spawn index for the next scene
            LastSpawnIndex = targetSpawnIndex;

            // 3. Subscribe to sceneLoaded event for post-load actions
            SceneManager.sceneLoaded += OnSceneLoadedAfterMoveTo;

            // 4. Load the new scene
            SceneManager.LoadScene(targetSceneBuildIndex, LoadSceneMode.Single);

            Debug.Log($"GameManager: Moving to scene build index {targetSceneBuildIndex}, targeting spawn index {targetSpawnIndex}.");
        }

        /// <summary>
        /// Callback for SceneManager.sceneLoaded after a scene transition initiated by MoveTo.
        /// </summary>
        private void OnSceneLoadedAfterMoveTo(Scene scene, LoadSceneMode mode)
        {
            // Unsubscribe immediately to prevent multiple calls
            SceneManager.sceneLoaded -= OnSceneLoadedAfterMoveTo;

            if (Instance == null)
            {
                Debug.LogError("GameManager.OnSceneLoadedAfterMoveTo: GameManager.Instance is null. Cannot complete scene transition.");
                return;
            }

            // 1. Load the scene-specific data for the newly loaded scene
            SaveSystem.LoadCurrentSceneDataFromLookup();

            // 2. Spawn the player at the designated spawn point in the new scene
            Instance.SpawnPlayerAt(Instance.LastSpawnIndex);

            Debug.Log($"GameManager: Scene '{scene.name}' loaded. Player spawned at LastSpawnIndex {Instance.LastSpawnIndex}.");
        }


        // --- Existing Time and UI Helper Methods ---
        public string CurrentTimeAsString()
        {
            if (DayCycleHandler != null)
            {
                float totalHours = 24.0f;
                int hours = Mathf.FloorToInt(CurrentDayRatio * totalHours);
                int minutes = Mathf.FloorToInt((CurrentDayRatio * totalHours * 60) % 60);
                return $"{hours:00}:{minutes:00}";
            }
            return "00:00";
        }

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
            if (hours == 0) hours = 12;

            return $"{hours:00}:{minutes:00} {amPm}";
        }

        public void Pause()
        {
            Time.timeScale = 0;
            Debug.Log("Game Paused");
        }

        public void Resume()
        {
            Time.timeScale = 1;
            Debug.Log("Game Resumed");
        }
    }
}
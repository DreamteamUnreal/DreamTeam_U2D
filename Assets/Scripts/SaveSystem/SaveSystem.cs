//SaveSystem.cs
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq; // Required for ToList()

namespace HappyHarvest
{
    public static class SaveSystem
    {
        // The main save data structure that gets serialized to JSON
        private static SaveData s_CurrentData = new SaveData();

        // Runtime lookup for scene-specific data. This is NOT directly serialized.
        private static Dictionary<string, SceneData> s_ScenesDataLookup = new Dictionary<string, SceneData>();

        // Path where the save file will be stored
        private static string SaveFilePath => Application.persistentDataPath + "/save.sav";

        // --- Main Save Data Structure ---
        [System.Serializable]
        public struct SaveData
        {
            public PlayerSaveData PlayerData;
            public DayCycleHandlerSaveData TimeSaveData;
            // This now correctly holds a list of the nested SceneData struct
            public List<SceneData> AllScenesData; // Changed from SaveData[] ScenesData;
        }

        // --- Nested Scene Data Structure (for saving individual scene states) ---
        [System.Serializable]
        public struct SceneData // This struct is defined within SaveSystem
        {
            public string UniqueSceneName;
            public TerrainDataSave TerrainData; // Data specific to the terrain
            // Add other scene-specific data here, e.g.:
            public List<InteractiveObjectSaveData> InteractiveObjectsData; // For other scene objects
        }

		/// <summary>
		/// Saves the current game state (player, time, all collected scene data) to a file.
		/// </summary>
		[System.Obsolete]
		public static void Save()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("SaveSystem: GameManager.Instance is null. Cannot save game.");
                return;
            }

            // 1. Save Player Data
            GameManager.Instance.Player.Save(ref s_CurrentData.PlayerData);

            // 2. Save Day Cycle Data
            // Ensure DayCycleHandler.Save is void and doesn't return object
            GameManager.Instance.DayCycleHandler.Save(ref s_CurrentData.TimeSaveData);

            // 3. Save Current Scene's Data before writing to file
            // Make sure the current scene's data is captured and added to the lookup
            SaveCurrentSceneDataToLookup();

            // 4. Convert the runtime dictionary of scene data to a serializable list
            s_CurrentData.AllScenesData = s_ScenesDataLookup.Values.ToList();

            // 5. Serialize and Write to File
            string json = JsonUtility.ToJson(s_CurrentData, true); // 'true' for pretty print
            File.WriteAllText(SaveFilePath, json);

            Debug.Log($"Game Saved to: {SaveFilePath}");
        }

        /// <summary>
        /// Loads the game state from a file. This will trigger a scene reload.
        /// </summary>
        public static void Load()
        {
            if (!File.Exists(SaveFilePath))
            {
                Debug.LogWarning($"SaveSystem: No save file found at {SaveFilePath}. Cannot load.");
                return;
            }

            string content = File.ReadAllText(SaveFilePath);
            s_CurrentData = JsonUtility.FromJson<SaveData>(content);

            // Populate the runtime scene data lookup from the loaded data
            s_ScenesDataLookup.Clear();
            if (s_CurrentData.AllScenesData != null)
            {
                foreach (var sceneData in s_CurrentData.AllScenesData)
                {
                    s_ScenesDataLookup[sceneData.UniqueSceneName] = sceneData;
                }
            }

            // Subscribe to sceneLoaded BEFORE loading the scene
            SceneManager.sceneLoaded += OnSceneLoadedAfterLoadGame;

            // Load the scene that was active when the game was saved (or a default starting scene)
            // You might want to save the last active scene's name/index in s_CurrentData.
            // For now, let's reload the current active scene, and then apply data.
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex, LoadSceneMode.Single);

            Debug.Log($"Game Loaded from: {SaveFilePath}");
        }

        /// <summary>
        /// Callback method for when a scene finishes loading after a Load() call.
        /// </summary>
        private static void OnSceneLoadedAfterLoadGame(Scene scene, LoadSceneMode mode)
        {
            // Unsubscribe immediately to prevent multiple calls
            SceneManager.sceneLoaded -= OnSceneLoadedAfterLoadGame;

            if (GameManager.Instance == null)
            {
                Debug.LogError("SaveSystem: GameManager.Instance is null after scene load. Cannot apply loaded data.");
                return;
            }

            // Apply Player Data
            GameManager.Instance.Player.Load(s_CurrentData.PlayerData);

            // Apply Day Cycle Data
            GameManager.Instance.DayCycleHandler.Load(s_CurrentData.TimeSaveData);

            // Apply the loaded scene-specific data for the just-loaded scene
            LoadCurrentSceneDataFromLookup();

            // --- Spawn Player at Last Spawn Index (if applicable) ---
            // This is crucial for player position after scene load
            // GameManager.Instance.SpawnPlayerAt(GameManager.Instance.LastSpawnIndex);
            // Or if you saved player position directly, GameManager.Instance.Player.transform.position = s_CurrentData.PlayerData.Position;
            // The Player.Load(s_CurrentData.PlayerData) should handle this.
            // If the player isn't moving to the correct spot, ensure Player.Load correctly sets its position.
        }

		/// <summary>
		/// Collects the current scene's data and adds/updates it in the runtime lookup dictionary.
		/// Call this before saving the entire game state.
		/// </summary>
		[System.Obsolete]
		public static void SaveCurrentSceneDataToLookup()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentSceneDataComponent == null)
            {
                Debug.LogWarning("SaveSystem.SaveCurrentSceneDataToLookup: GameManager or CurrentSceneDataComponent is null. Cannot save current scene data.");
                return;
            }

            string currentSceneName = GameManager.Instance.CurrentSceneDataComponent.UniqueSceneName;
            TerrainDataSave terrainSaveData = new TerrainDataSave();
            List<InteractiveObjectSaveData> interactiveObjectSaveDatas = new List<InteractiveObjectSaveData>();

            // Save Terrain Data
            if (GameManager.Instance.Terrain != null)
            {
                GameManager.Instance.Terrain.Save(ref terrainSaveData);
            }
            else
            {
                Debug.LogWarning("SaveSystem.SaveCurrentSceneDataToLookup: TerrainManager is null. Terrain data will not be saved for this scene.");
            }

            // Save Interactive Objects Data
            // You need to ensure your InteractiveObjects have a way to provide their save data.
            // Example: Add a virtual method `public virtual InteractiveObjectSaveData GetSaveData()` to InteractiveObject.
            foreach (var obj in Object.FindObjectsOfType<InteractiveObject>())
            {
                // You would implement a method on InteractiveObject to get its specific save data
                // For now, using a placeholder, but this needs to be properly implemented per object type
                interactiveObjectSaveDatas.Add(new InteractiveObjectSaveData
                {
                    InstanceID = obj.name, // Use a truly unique ID here if names are not unique
                    Position = obj.transform.position,
                    IsActive = obj.gameObject.activeSelf,
                    CurrentState = "default" // Replace with actual state from obj
                });
            }


            // Create the SceneData entry
            SceneData sceneDataToSave = new SceneData
            {
                UniqueSceneName = currentSceneName,
                TerrainData = terrainSaveData,
                InteractiveObjectsData = interactiveObjectSaveDatas
            };

            // Add or update the entry in the runtime dictionary
            s_ScenesDataLookup[currentSceneName] = sceneDataToSave;

            Debug.Log($"SaveSystem: Saved data for current scene '{currentSceneName}' to lookup.");
        }

        /// <summary>
        /// Loads and applies scene-specific data for the currently active scene from the runtime lookup dictionary.
        /// Call this after a scene has loaded.
        /// </summary>
        public static void LoadCurrentSceneDataFromLookup()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentSceneDataComponent == null)
            {
                Debug.LogWarning("SaveSystem.LoadCurrentSceneDataFromLookup: GameManager or CurrentSceneDataComponent is null. Cannot load scene data.");
                return;
            }

            string currentSceneName = GameManager.Instance.CurrentSceneDataComponent.UniqueSceneName;

            if (s_ScenesDataLookup.TryGetValue(currentSceneName, out var loadedSceneData))
            {
                // Load Terrain Data
                if (GameManager.Instance.Terrain != null)
                {
                    GameManager.Instance.Terrain.Load(loadedSceneData.TerrainData);
                }
                else
                {
                    Debug.LogWarning("SaveSystem.LoadCurrentSceneDataFromLookup: TerrainManager is null. Terrain data will not be loaded for this scene.");
                }

                // Load Interactive Objects Data
                // You need to implement how each InteractiveObject loads its state from its save data.
                // This typically involves iterating through loadedSceneData.InteractiveObjectsData
                // and finding the corresponding objects in the scene to call their Load methods.
                foreach (var loadedObjectData in loadedSceneData.InteractiveObjectsData)
                {
                    // Find the object in the scene (e.g., by unique ID)
                    GameObject sceneObject = GameObject.Find(loadedObjectData.InstanceID); // Again, use unique IDs
                    if (sceneObject != null)
                    {
                        InteractiveObject interactiveObj = sceneObject.GetComponent<InteractiveObject>();
                        if (interactiveObj != null)
                        {
                            // Assuming InteractiveObject has a virtual Load method
                            // interactiveObj.Load(loadedObjectData); // You need to implement this
                            interactiveObj.gameObject.SetActive(loadedObjectData.IsActive);
                            // Apply other specific states from loadedObjectData
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"SaveSystem.LoadCurrentSceneDataFromLookup: Object with InstanceID '{loadedObjectData.InstanceID}' not found in current scene. Skipping.");
                    }
                }

                Debug.Log($"SaveSystem: Loaded and applied data for current scene '{currentSceneName}'.");
            }
            else
            {
                Debug.LogWarning($"SaveSystem: No saved data found for scene '{currentSceneName}'. Initializing fresh.");
                // If no save data for this scene, ensure it's in a default state.
                // You might want to call Terrain.Init() or similar here.
            }
        }
    }
}
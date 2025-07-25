// SceneSaveData.cs
using System.Collections.Generic;
using UnityEngine;

namespace HappyHarvest
{
    /// <summary>
    /// A serializable struct to hold all save data pertaining to a specific game scene.
    /// This would typically include the states of objects within that scene.
    /// </summary>
    [System.Serializable]
    public struct SceneSaveData
    {
        // Example: List of save data for interactive objects in this scene.
        // You would populate this list with data collected from your InteractiveObject instances.
        public List<InteractiveObjectSaveData> InteractiveObjects;

        // --- ADD THIS LINE ---
        [Tooltip("A unique name for the scene, usually its file name.")]
        public string UniqueSceneName; // <--- ADD THIS LINE
        // Might want to save the current scene's name or build index for verification
        public string SceneName;
        public int SceneBuildIndex;

        // Add any other scene-specific data that needs to be saved here.
        // For instance:
        // public List<Vector3> DroppedItemPositions;
        // public List<string> DroppedItemIDs;
    }

    /// <summary>
    /// A serializable struct to hold save data for an individual InteractiveObject.
    /// Would typically assign a unique ID to each persistent InteractiveObject in your scene.
    /// </summary>
    [System.Serializable]
    public struct InteractiveObjectSaveData
    {
        // A unique identifier for this specific interactive object instance in the scene.
        // This could be its GameObject.name, a GUID, or a custom ID assigned in the Editor.
        public string InstanceID;

        // Example state data for an interactive object:
        public Vector3 Position; // Current position (if it can move)
        public bool IsActive;    // Whether the object is active/enabled
        public string CurrentState; // E.g., "Tilled", "Planted", "Empty" for a farm plot
        // Add more fields here to save the specific state of your interactive objects.
        // For example, for a FarmPlot:
        // public Item PlantedSeedType; // The type of seed planted
        // public float GrowthProgress; // How far along the plant is in its growth cycle
        // public bool IsWatered;
    }
}
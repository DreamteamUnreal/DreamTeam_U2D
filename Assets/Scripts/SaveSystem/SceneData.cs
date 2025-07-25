//SceneData.cs
using UnityEngine;

namespace HappyHarvest
{
	/// <summary>
	/// Required in every scene. This defines the unique name of the scene, used in the save system to identify the scene.
	/// This means the scene can be moved, renamed or its build id changed and saves won't break.
	/// </summary>
	public class SceneData : MonoBehaviour
	{
		public string UniqueSceneName;

		private void OnEnable()
		{
			// --- CHANGE THIS LINE ---
			// It should now set the CurrentSceneDataComponent property
			if (GameManager.Instance != null)
			{
				GameManager.Instance.CurrentSceneDataComponent = this;
			}
			else
			{
				Debug.LogWarning($"SceneData on {gameObject.name}: GameManager.Instance is null. Cannot register scene data.");
			}
		}

		private void OnDisable()
		{
			// --- CHANGE THIS LINE ---
			// It should now clear the CurrentSceneDataComponent property
			if (GameManager.Instance?.CurrentSceneDataComponent == this)
			{
				GameManager.Instance.CurrentSceneDataComponent = null;
			}
		}
	}
}
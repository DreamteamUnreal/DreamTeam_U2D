//SceneTransition.cs
using UnityEngine;

namespace HappyHarvest
{
	[RequireComponent(typeof(Collider2D))]
	public class SceneTransition : MonoBehaviour
	{
		public int TargetSceneBuildIndex;
		public int TargetSpawnIndex;

		[System.Obsolete]
		private void OnTriggerEnter2D(Collider2D col)
		{
			GameManager.Instance.MoveTo(TargetSceneBuildIndex, TargetSpawnIndex);
		}
	}
}
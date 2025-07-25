using UnityEngine;

namespace HappyHarvest
{
	public class StartAnimation : MonoBehaviour
	{
		public Animation Animation;

		public void Trigger()
		{
			_ = Animation.Play();
		}
	}
}
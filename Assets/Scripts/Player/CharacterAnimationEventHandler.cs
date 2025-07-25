using UnityEngine;

namespace HappyHarvest
{
	/// <summary>
	/// This allows to lock and unlock controlling the Player Controller. This is used by Animation Event to stop moving
	/// during e.g. a tool animation. This is added on the same object that have the Animation Controller so it can
	/// receive animation event.
	/// </summary>
	public class CharacterAnimationEventHandler : MonoBehaviour
	{
#pragma warning disable IDE0052 // Remove unread private members
		private PlayerController m_Controller;
#pragma warning restore IDE0052 // Remove unread private members

		private void Awake()
		{
			//Get In Parent as this is on the GameObject with the animator and the controller is at the root of the
			//character.
			m_Controller = GetComponentInParent<PlayerController>();
		}
	}
}

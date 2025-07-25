using System;
using System.Collections.Generic;
using Template2DCommon;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

namespace HappyHarvest
{
	/// <summary>
	/// Handle playing a random step sound during walk animation. This need to be on the same GameObject of the player
	/// with the Animator as it need to receive the PlayStepSound events from the walking animation.
	/// Contains a list of pairing of list of tiles to list of audio clip, so it can play different clip based on the
	/// tile under the player.
	/// Note : the Tilemap that is checked for which tile is walked on need a WalkableSurface component on it.
	/// </summary>
	public class StepSoundHandler : MonoBehaviour
	{
		[Serializable]
		public class TileSoundMapping
		{
			public TileBase[] Tiles;
			public AudioClip[] StepSounds;
		}

		public AudioClip[] DefaultStepSounds;
		public TileSoundMapping[] SoundMappings;

		private readonly Dictionary<TileBase, AudioClip[]> m_Mapping = new();

		private void Start()
		{
			foreach (TileSoundMapping mapping in SoundMappings)
			{
				foreach (TileBase tile in mapping.Tiles)
				{
					m_Mapping[tile] = mapping.StepSounds;
				}
			}
		}

		//This is called by animation event on the walking animation of the character.
		public void PlayStepSound()
		{
			Vector3Int underCell = GameManager.Instance.WalkSurfaceTilemap.WorldToCell(transform.position);
			TileBase tile = GameManager.Instance.WalkSurfaceTilemap.GetTile(underCell);

			SoundManager.Instance.PlaySFXAt(transform.position,
				(tile != null && m_Mapping.ContainsKey(tile))
					? GetRandomEntry(m_Mapping[tile])
					: GetRandomEntry(DefaultStepSounds), false);
		}

		private AudioClip GetRandomEntry(AudioClip[] clips)
		{
			return clips[Random.Range(0, clips.Length)];
		}
	}
}
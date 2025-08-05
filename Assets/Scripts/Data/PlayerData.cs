using UnityEngine;

public class PlayerData : MonoBehaviour
{
	// Start is called once before the first execution of Update after the MonoBehaviour is created
	public static PlayerData Instance { get; private set; }

	public int score = 0;  // across all levels

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
			DontDestroyOnLoad(gameObject);  // survive scene loads
		}
		else
		{
			Destroy(gameObject); // only one allowed
		}
	}

	public void ResetAll()
	{
		score = 0;
	}
}

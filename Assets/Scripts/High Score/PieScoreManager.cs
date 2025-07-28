// PieScoreManager.cs
using TMPro; // Make sure TextMeshPro is imported in the project if not already
using UnityEngine;
using Random = UnityEngine.Random; // Explicitly use UnityEngine.Random to avoid ambiguity with System.Random

namespace HappyHarvest // Add the HappyHarvest namespace
{
	public class PieScoreManager : MonoBehaviour
	{
		// Change to private static, and provide a public accessor via GameManager
		// This makes it consistent with how other managers are accessed (e.g., GameManager.Instance.PieScoreManager)
		// If absolutely need a direct static instance, ensure its Awake runs early.
		// For now, I'll assume GameManager.Instance will hold the reference.
		// public static PieScoreManager Instance; // Remove this if GameManager holds the reference

		[Header("UI")]
		public TMP_Text currentPiesText;
		public TMP_Text currentRoundText;
		public TMP_Text numPiesText;
		public TMP_Text numRoundsText;
		public TMP_Text playerScoreText;
		public TMP_Text highScoreText;

		[Header("Pie Settings")]
		public int piesMade = 0;
		public int piesRequired = 5; // Start with a default requirement
		public int totalPiesMade = 0;
		public int currentRounds = 1;
		public int playerScore = 0;
		public int roundNum = 0;

		[Header("HighScore Settings")]
		private int highScore = 0;

		private void Awake()
		{
			// Register with GameManager.Instance
			// Ensure GameManager's Awake runs BEFORE PieScoreManager's Awake
			// by setting GameManager's Script Execution Order to a lower value (e.g., -200)
			if (GameManager.Instance != null)
			{
				Debug.LogError("PieScoreManager: GameManager.Instance is null in Awake! Check Script Execution Order.");
				// Optionally destroy this GameObject if GameManager is essential and not found
				// Destroy(gameObject);
			}
		}

		private void Start()
		{
			// Loads the high score
			highScore = PlayerPrefs.GetInt("HighPieScore", 0);

			// Start from round 1, and deadline is round 2
			currentRounds = 1;
			roundNum = currentRounds + 1; // This means the UI will show "Round 1 / Round 2" initially

			UpdateUI();
		}

		public void AddPie()
		{
			piesMade++;
			totalPiesMade++;

			if (piesMade > piesRequired)
			{
				int bonus = 80 + Random.Range(50, 91); // 80 + random 5090
				playerScore += bonus;
				Debug.Log($"Bonus Pie! +{bonus} points");
			}
			else
			{
				playerScore += 80;
				Debug.Log("+80 points");
			}

			// High score update
			if (playerScore > highScore)
			{
				highScore = playerScore;
				PlayerPrefs.SetInt("HighPieScore", highScore);
				PlayerPrefs.Save();
			}

			UpdateUI();
		}

		private void StartNewRound()
		{
			piesMade = 0;
			piesRequired += 2; // Increase difficulty each round
			currentRounds++;

			roundNum = currentRounds + 1; // Update for UI

			Debug.Log($"Starting new round: {currentRounds}. Pies required: {piesRequired}");
			UpdateUI();
		}

		public void EndRound()
		{
			CheckRoundResult();
		}

		public void RestartGame()
		{
			piesMade = 0;
			playerScore = 0;
			currentRounds = 1;
			roundNum = currentRounds + 1;
			piesRequired = 5; // Reset to initial requirement
			totalPiesMade = 0;

			// Reload high score on restart
			highScore = PlayerPrefs.GetInt("HighPieScore", 0);

			UpdateUI();
			Debug.Log("Game has been reset.");
		}

		private void CheckRoundResult()
		{
			if (piesMade < piesRequired)
			{
				Debug.Log("Game Over - not enough pies!");

				// Save a new high score if achieved (using totalPiesMade for high score)
				if (totalPiesMade > highScore)
				{
					highScore = totalPiesMade;
					PlayerPrefs.SetInt("HighPieScore", highScore);
					PlayerPrefs.Save();
					Debug.Log($"New High Score: {highScore}");
				}
				// Might want to trigger a "Game Over" UI screen here
				// GameManager.Instance.ShowGameOverScreen(); // Example
			}
			else
			{
				Debug.Log("Round Complete!");
				StartNewRound(); // Automatically start next round if successful
			}
		}

		private void UpdateUI()
		{
			// Add null checks for all TMP_Text references
			if (currentPiesText != null)
			{
				currentPiesText.text = $"{piesMade}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: currentPiesText is null.");
			}

			if (currentRoundText != null)
			{
				currentRoundText.text = $"{currentRounds}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: currentRoundText is null.");
			}

			if (numPiesText != null)
			{
				numPiesText.text = $"{piesRequired}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: numPiesText is null.");
			}

			if (numRoundsText != null)
			{
				numRoundsText.text = $"{roundNum}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: numRoundsText is null.");
			}

			if (playerScoreText != null)
			{
				playerScoreText.text = $"{playerScore}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: playerScoreText is null.");
			}

			if (highScoreText != null)
			{
				highScoreText.text = $"{highScore}";
			}
			else
			{
				Debug.LogWarning("PieScoreManager: highScoreText is null.");
			}
		}
	}
}
using TMPro;
using UnityEngine;

namespace HappyHarvest
{
    public class PieScoreManager : MonoBehaviour
	{
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

		private int highScore = 0;

        [Header("References")]
        [SerializeField] private GameOverUI gameOverUIManager;


        private int PlayerScore
        {
            get => PlayerData.Instance.score;
            set => PlayerData.Instance.score = value;
        }

        // I took this out I didnt have it before I dont see a need for it. Keeping it just in case 
        /*private void Awake()
		{
			if (GameManager.Instance != null)
			{
				Debug.LogError("PieScoreManager: GameManager.Instance is null in Awake! Check Script Execution Order.");
				// Optionally destroy this GameObject if GameManager is essential and not found
				// Destroy(gameObject);
			}
		}*/

        private void Start()
		{
			// Loads the high score
			highScore = PlayerPrefs.GetInt("HighPieScore", 0);
			currentRounds = 1; // Current round the player will start on
			roundNum = currentRounds + 1; // roundNum is the currentRounds + 1 meaning the next round

			

			UpdateUI(); // Update the UI
		}

		// AddPie You will only need to call this function where the pies are made
		public void AddPie()
		{
			piesMade++;
			totalPiesMade++;

			if (piesMade > piesRequired)
			{
				int bonus = 80 + Random.Range(50, 91); // 80 + random 5091
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

		// Do not worry about start new round :) 
		private void StartNewRound()
		{
			piesMade = 0;
			piesRequired += 2; // Increase difficulty each round
			currentRounds++;

			roundNum = currentRounds + 1; // Update for UI

			Debug.Log($"Starting new round: {currentRounds}. Pies required: {piesRequired}");
			UpdateUI();
		}

		// Call EndRound at the end of the round how ever it ends
		public void EndRound()
		{
			CheckRoundResult();
		}
		
		// RestartGame is a function the sets everything back to round one
		// We can call this at game over or restart
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

		// CheckRoundResult checks the round no need to change
		private void CheckRoundResult()
		{
            if (piesMade < piesRequired)
            {
                if (gameOverUIManager != null)
                {
                    gameOverUIManager.ShowGameOver(playerScore, highScore, totalPiesMade);
                }
                else
                {
                    Debug.LogWarning("GameOverUIManager is not assigned in PieScoreManager!");
                }

                // Save high score if achieved
                if (totalPiesMade > highScore)
                {
                    highScore = totalPiesMade;
                    PlayerPrefs.SetInt("HighPieScore", highScore);
                    PlayerPrefs.Save();
                    Debug.Log($"New High Score: {highScore}");
                }
            }
            else
            {
                Debug.Log("Round Complete!");
                StartNewRound();
            }
        }


		// UpdateUI just updates the UI
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
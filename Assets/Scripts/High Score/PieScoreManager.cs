//PieScoreManager.cs
using TMPro;
using UnityEngine;

public class PieScoreManager : MonoBehaviour
{

    public static PieScoreManager Instance;

    [Header("UI")]
    public TMP_Text currentPiesText;
    public TMP_Text currentRoundText;
    public TMP_Text numPiesText; 
    public TMP_Text numRoundsText;
    public TMP_Text playerScoreText;
    public TMP_Text highScoreText;

    [Header("Pie Settings")]
    public int piesMade = 0;
    public int piesRequired = 0;
    public int totalPiesMade = 0;
    public int currentRounds = 1;
    public int playerScore = 0;
    public int roundNum = 0;

    [Header("HighScore Settings")]
    private int highScore = 0;

    private void Awake()
    {
        // Singleton setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //loads the high score
        highScore = PlayerPrefs.GetInt("HighPieScore", 0);

        // Start from round 1, and deadline is round 2
        currentRounds = 1;
        roundNum = currentRounds + 1;

        UpdateUI();
    }

    public void AddPie()
    {
        piesMade++;
        totalPiesMade++;

        if (piesMade > piesRequired)
        {
            int bonus = 80 + Random.Range(50, 91); // 80 + random 50�90
            playerScore += bonus;
            Debug.Log($"Bonus Pie! +{bonus} points");
        }
        else
        {
            playerScore += 80;
            Debug.Log("+80 points");
        }

        // high score update
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
        piesRequired += 2;
        currentRounds++;

        roundNum = currentRounds + 1;

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
        piesRequired = 5; 
        totalPiesMade = 0;

        UpdateUI();
        Debug.Log("Game has been reset.");
    }

    private void CheckRoundResult()
    {
        if (piesMade < piesRequired)
        {
            Debug.Log("Game Over � not enough pies!");
            
            // save a new high score if achieved
            if (totalPiesMade > highScore)
            {
                highScore = totalPiesMade;
                PlayerPrefs.SetInt("HighPieScore", highScore);
                PlayerPrefs.Save();
            }
        }

        else
        {
            Debug.Log("Round Complete!");
            piesRequired += 2; // Increase difficulty each round
            StartNewRound();
        }
    }

    private void UpdateUI()
    {
        if (currentPiesText != null)
            currentPiesText.text = $"{piesMade}";

        if (currentRoundText != null)
            currentRoundText.text = $"{currentRounds}";

        if (numPiesText != null)
            numPiesText.text = $"{piesRequired}";

        if (numRoundsText != null)
            numRoundsText.text = $"{roundNum}";
        if (playerScoreText != null)
            playerScoreText.text = $"{playerScore}";

        if (highScoreText != null)
            highScoreText.text = $"{highScore}";
    }

}

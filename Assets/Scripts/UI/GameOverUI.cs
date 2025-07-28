using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
	public GameObject normalHighScoreUI;
	public GameObject lostUIPanel;
	public TMP_Text finalScoreText;
	public TMP_Text highScoreText;
	public TMP_Text totalPiesText;

	private void Start()
	{
		if (lostUIPanel != null)
		{
			lostUIPanel.SetActive(false);
		}
	}

	public void ShowGameOver(int finalScore, int highScore, int totalPies)
	{
		if (lostUIPanel != null)
		{
			lostUIPanel.SetActive(true);

			if (normalHighScoreUI != null)
			{
				normalHighScoreUI.SetActive(false); // Hides the HUD UI
			}

			if (finalScoreText != null)
			{
				finalScoreText.text = $" {finalScore}";
			}

			if (highScoreText != null)
			{
				highScoreText.text = $" {highScore}";
			}

			if (totalPiesText != null)
			{
				totalPiesText.text = $" {totalPies}";
			}
		}
	}

	public void OnRestartButton()
	{
		Scene currentScene = SceneManager.GetActiveScene();

		if (normalHighScoreUI != null)
		{
			normalHighScoreUI.SetActive(true); // Show HUD again
		}

		if (currentScene.name == "Lvl_L1")
		{
			PlayerData.Instance.ResetAll();
			SceneManager.LoadScene("Lvl_L1");
		}
		else
		{
			SceneManager.LoadScene(currentScene.buildIndex);
		}
	}

	public void OnMainMenu()
	{
		SceneManager.LoadScene("MainMenu");
	}
	public void OnQuitButton()
	{
		Application.Quit();
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false; // For stopping Play mode in Unity Editor
#endif
	}
}

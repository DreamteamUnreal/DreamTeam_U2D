// Oven.cs
using UnityEngine;
using HappyHarvest;

public class Oven : InteractiveObject // Or whatever your crafting station is
{
	public Item pieItemDefinition; // Assign your "Pie" Item ScriptableObject here
	public PieScoreManager pieScore; // This lets you use my pieScoreManager script :)
	public override void InteractedWith()
	{
		// Call AddPie() on the PieScoreManager
		if (GameManager.Instance != null)
		{
			pieScore.AddPie(); // One pie will be added to the ui as well as the score
			Debug.Log("A pie has been made!");
		}
		else
		{
			Debug.LogWarning("Oven: PieScoreManager not found. Pie not added to score.");
		}
	}
}
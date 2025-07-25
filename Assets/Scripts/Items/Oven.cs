// Oven.cs
using UnityEngine;
using HappyHarvest;

public class Oven : InteractiveObject // Or whatever your crafting station is
{
	public Item pieItemDefinition; // Assign your "Pie" Item ScriptableObject here

	public override void InteractedWith()
	{
		// Call AddPie() on the PieScoreManager
		if (GameManager.Instance != null && GameManager.Instance.PieScoreManager != null)
		{
			GameManager.Instance.PieScoreManager.AddPie();
			Debug.Log("A pie has been made!");
		}
		else
		{
			Debug.LogWarning("Oven: PieScoreManager not found. Pie not added to score.");
		}
	}
}
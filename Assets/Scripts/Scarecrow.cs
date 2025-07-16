using System;
using UnityEngine;

public class Scarecrow : MonoBehaviour, Interactable
{
    public float scareRadius = 10f;
    public float scareDuration = 3f; // How long animals are scared
    public bool canBeUsed = true; // For single-use as per Level 1 design

    public void Interact(GameObject interactor)
    {
        if (canBeUsed)
        {
            Debug.Log("Scarecrow activated!");
            ScareAnimals();
            canBeUsed = false; // Use once
            // Optionally, change appearance or play animation
        }
        else
        {
            Debug.Log("Scarecrow already used!");
        }
    }

    private void ScareAnimals()
    {
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, scareRadius);
        foreach (Collider2D hitCollider in hitColliders)
        {
            AnimalAI animal = hitCollider.GetComponent<AnimalAI>();
            if (animal != null)
            {
                animal.FleeInstantly();
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, scareRadius);
    }
}
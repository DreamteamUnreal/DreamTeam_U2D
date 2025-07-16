using UnityEngine;
using System.Collections; // For coroutines

public class AnimalAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float roamRadius = 5f; // How far it roams from its spawn point
    public float chaseSpeedMultiplier = 1.5f; // When chasing player/ingredients
    public float ingredientStealRadius = 0.5f; // How close to an ingredient to 'steal'
    public float playerDetectionRadius = 3f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private Transform playerTransform; // Assign player in Inspector or find dynamically

    public enum AnimalState { Roaming, ChasingIngredient, Fleeing, AttackingPlayer }
    public AnimalState currentState = AnimalState.Roaming;

    // References to any "stealable" items in the scene
    private CollectibleItem currentTargetItem;

    void Start()
    {
        startPosition = transform.position;
        playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform; // Ensure your player has the "Player" tag
        StartCoroutine(Roam());
    }

    void Update()
    {
        // Simple state machine logic
        switch (currentState)
        {
            case AnimalState.Roaming:
                // Check for player or ingredients
                if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) < playerDetectionRadius)
                {
                    // Transition to a different state based on animal type (flee or attack)
                    // For squirrels, they might flee
                    currentState = AnimalState.Fleeing;
                    StartCoroutine(FleeFromPlayer());
                }
                else if (FindNearbyIngredient())
                {
                    currentState = AnimalState.ChasingIngredient;
                    StartCoroutine(ChaseIngredient());
                }
                break;
            case AnimalState.ChasingIngredient:
                // Handled in coroutine
                break;
            case AnimalState.Fleeing:
                // Handled in coroutine
                break;
                // Add more states for other animals (e.g., AttackingPlayer for Bear)
        }
    }

    IEnumerator Roam()
    {
        while (currentState == AnimalState.Roaming)
        {
            Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
            targetPosition = startPosition + new Vector3(randomOffset.x, randomOffset.y, 0);

            // Move towards target
            while (Vector2.Distance(transform.position, targetPosition) > 0.1f && currentState == AnimalState.Roaming)
            {
                transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
                yield return null;
            }
            yield return new WaitForSeconds(Random.Range(1f, 3f)); // Wait a bit before moving again
        }
    }

    IEnumerator ChaseIngredient()
    {
        while (currentState == AnimalState.ChasingIngredient && currentTargetItem != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, currentTargetItem.transform.position, moveSpeed * chaseSpeedMultiplier * Time.deltaTime);

            if (Vector2.Distance(transform.position, currentTargetItem.transform.position) < ingredientStealRadius)
            {
                // Simple 'steal': remove item and reset state
                Debug.Log($"{gameObject.name} stole {currentTargetItem.itemData.itemName}!");
                Destroy(currentTargetItem.gameObject);
                currentTargetItem = null;
                currentState = AnimalState.Roaming;
                StartCoroutine(Roam());
                yield break;
            }
            yield return null;
        }
        // If target item is gone or state changed, revert to roaming
        if (currentState == AnimalState.ChasingIngredient)
        {
            currentState = AnimalState.Roaming;
            StartCoroutine(Roam());
        }
    }

    IEnumerator FleeFromPlayer()
    {
        float fleeDuration = 2f; // How long to flee
        float fleeSpeed = moveSpeed * 2f; // Faster when fleeing
        float timer = 0f;

        while (timer < fleeDuration && currentState == AnimalState.Fleeing && playerTransform != null)
        {
            Vector2 fleeDirection = (transform.position - playerTransform.position).normalized;
            transform.position = Vector2.MoveTowards(transform.position, (Vector2)transform.position + fleeDirection * 10f, fleeSpeed * Time.deltaTime);
            timer += Time.deltaTime;
            yield return null;
        }
        // After fleeing, return to roaming
        currentState = AnimalState.Roaming;
        StartCoroutine(Roam());
    }


    // Simple check for closest collectible
    private bool FindNearbyIngredient()
    {
        // For a real game, you'd want more efficient searching (e.g., Physics2D.OverlapCircleAll)
#pragma warning disable CS0618 // Type or member is obsolete
        CollectibleItem[] allItems = FindObjectsOfType<CollectibleItem>();
#pragma warning restore CS0618 // Type or member is obsolete
        float closestDistance = Mathf.Infinity;
        CollectibleItem potentialTarget = null;

        foreach (CollectibleItem item in allItems)
        {
            float dist = Vector2.Distance(transform.position, item.transform.position);
            if (dist < closestDistance)
            {
                closestDistance = dist;
                potentialTarget = item;
            }
        }

        if (potentialTarget != null && closestDistance < playerDetectionRadius * 1.5f) // Animals look further for food
        {
            currentTargetItem = potentialTarget;
            return true;
        }
        return false;
    }

    // Function for Scarecrow to call
    public void FleeInstantly()
    {
        StopAllCoroutines(); // Stop current behavior
        currentState = AnimalState.Fleeing;
        StartCoroutine(FleeFromPlayer()); // Reuse flee logic
    }
}
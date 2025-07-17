// CameraFollow.cs
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Tooltip("The Transform of the player GameObject to follow.")]
    public Transform target; // Assign your Player GameObject's Transform here in the Inspector

    [Tooltip("The offset from the player's position.")]
    public Vector3 offset = new Vector3(0f, 0f, -10f); // Adjust this if your camera's Z is different, or you want an X/Y offset

    [Tooltip("How smoothly the camera follows the player. 0 for instant follow.")]
    [Range(0f, 1f)] // Create a slider in the Inspector from 0 to 1
    public float smoothSpeed = 0.125f; // A small value makes it smooth, 0 for no smoothing

    // LateUpdate is called once per frame, after all Update functions have been called.
    // This ensures the player has moved for the current frame before the camera tries to follow.
    void LateUpdate()
    {
        if (target == null)
        {
            Debug.LogWarning("CameraFollow: Target (Player) not assigned! Please assign the player's Transform in the Inspector.");
            return;
        }

        // Calculate the desired camera position based on the target's position and the offset
        Vector3 desiredPosition = target.position + offset;

        // Use Lerp for smooth camera movement
        // Lerp interpolates between two points. It's usually good to use Time.deltaTime
        // if you want framerate-independent smoothing, but for camera follow,
        // a fixed smoothSpeed often gives a more consistent feel.
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);

        // Apply the smoothed position to the camera
        transform.position = smoothedPosition;

        // Optional: If you want to snap the camera to pixel grid for pixel art games:
        // transform.position = new Vector3(Mathf.Round(smoothedPosition.x), Mathf.Round(smoothedPosition.y), smoothedPosition.z);
    }
}
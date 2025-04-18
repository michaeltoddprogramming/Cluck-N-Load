using UnityEngine;

public class DistanceBasedSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float maxDistance = 10f;  
    [SerializeField] private float minDistance = 1f;   

    private void Update()
    {
        // Calculate the distance between the camera and the object
        float distance = Vector3.Distance(cameraTransform.position, transform.position);

        // If the distance is less than the maxDistance, adjust the volume based on distance
        if (distance < maxDistance)
        {
            // Calculate a volume level based on the distance (between 0 and 1)
            float volume = Mathf.Clamp01(1 - (distance / maxDistance)); 
            audioSource.volume = volume;  // Set the volume based on the calculated value

            // Ensure the sound plays if it's close enough
            if (!audioSource.isPlaying)
            {
                audioSource.Play();
            }
        }
        else
        {
            // Stop the sound when the camera is too far
            if (audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }
    }
}

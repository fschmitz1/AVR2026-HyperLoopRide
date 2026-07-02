using UnityEngine;

public class TubePassSoundPlayer : MonoBehaviour
{
    [Header("References")]
    public HyperloopJoystickController hyperloopController;
    public AudioSource audioSource;

    [Header("Sound")]
    public AudioClip[] passBySounds;
    public float volume = 1f;

    [Header("Speed Pitch")]
    public float minSpeed = 0f;
    public float maxSpeed = 500f;
    public float minPitch = 0.8f;
    public float maxPitch = 1.8f;

    [Header("Cooldown")]
    public float minTimeBetweenSounds = 0.08f;

    [Header("Debug")]
    public float currentSpeed;
    public float currentPitch;

    private float lastPlayTime = -999f;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
    }

    public void PlayPassSound()
    {
        if (audioSource == null)
            return;

        if (passBySounds == null || passBySounds.Length == 0)
            return;

        if (Time.time - lastPlayTime < minTimeBetweenSounds)
            return;

        float speed = 0f;

        if (hyperloopController != null)
            speed = Mathf.Abs(hyperloopController.currentSpeed);

        currentSpeed = speed;

        float t = Mathf.InverseLerp(minSpeed, maxSpeed, speed);

        currentPitch = Mathf.Lerp(minPitch, maxPitch, t);
        audioSource.pitch = currentPitch;

        AudioClip clip = passBySounds[Random.Range(0, passBySounds.Length)];
        audioSource.PlayOneShot(clip, volume);

        lastPlayTime = Time.time;
    }
}
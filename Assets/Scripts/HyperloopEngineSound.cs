using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class HyperloopEngineSound : MonoBehaviour
{
    [Header("References")]
    public HyperloopJoystickController hyperloopController;
    public AudioSource audioSource;

    [Header("Speed Mapping")]
    public float minSpeedToHear = 0.2f;
    public float speedForMaxSound = 500f;

    [Header("Volume")]
    [Range(0f, 1f)]
    public float minVolume = 0f;

    [Range(0f, 1f)]
    public float maxVolume = 0.8f;

    public float volumeSmoothSpeed = 5f;

    [Header("Pitch")]
    public float minPitch = 0.75f;
    public float maxPitch = 1.8f;
    public float pitchSmoothSpeed = 4f;

    [Header("Boost Extra")]
    public bool boostMakesSoundStronger = true;
    public float boostVolumeMultiplier = 1.15f;
    public float boostPitchMultiplier = 1.1f;

    [Header("Debug")]
    public float speed;
    public float speed01;
    public float targetVolume;
    public float targetPitch;

    private void Awake()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0f;
    }

    private void Update()
    {
        if (hyperloopController == null || audioSource == null)
            return;

        speed = Mathf.Abs(hyperloopController.currentSpeed);

        bool shouldPlay = speed > minSpeedToHear;

        if (!shouldPlay)
        {
            targetVolume = 0f;
            targetPitch = minPitch;

            audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * volumeSmoothSpeed);
            audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * pitchSmoothSpeed);

            if (audioSource.volume < 0.01f && audioSource.isPlaying)
                audioSource.Stop();

            return;
        }

        if (!audioSource.isPlaying)
            audioSource.Play();

        speed01 = Mathf.Clamp01(speed / speedForMaxSound);

        targetVolume = Mathf.Lerp(minVolume, maxVolume, speed01);
        targetPitch = Mathf.Lerp(minPitch, maxPitch, speed01);

        if (boostMakesSoundStronger && hyperloopController.boostModeActive)
        {
            targetVolume *= boostVolumeMultiplier;
            targetPitch *= boostPitchMultiplier;
        }

        targetVolume = Mathf.Clamp01(targetVolume);

        audioSource.volume = Mathf.Lerp(audioSource.volume, targetVolume, Time.deltaTime * volumeSmoothSpeed);
        audioSource.pitch = Mathf.Lerp(audioSource.pitch, targetPitch, Time.deltaTime * pitchSmoothSpeed);
    }
}
using UnityEngine;

public class SpeedLinesController : MonoBehaviour
{
    [Header("References")]
    public HyperloopJoystickController hyperloopController;
    public ParticleSystem leftLines;
    public ParticleSystem rightLines;
    public ParticleSystem topLines;

    [Header("Activation")]
    public bool onlyWhenBoostLaunched = true;
    public float minSpeedToShow = 5f;

    [Header("Emission")]
    public float minEmission = 0f;
    public float maxEmission = 120f;

    [Header("Particle Settings")]
    public float minParticleSpeed = 6f;
    public float maxParticleSpeed = 25f;

    [Header("Speed Mapping")]
    public float speedForMaxEffect = 100f;

    private void Update()
    {
        if (hyperloopController == null)
            return;

        float speed = Mathf.Abs(hyperloopController.currentSpeed);

        bool shouldShow = speed >= minSpeedToShow;

        if (onlyWhenBoostLaunched)
            shouldShow = shouldShow && hyperloopController.boostLaunchConfirmed;

        UpdateSystem(leftLines, speed, shouldShow);
        UpdateSystem(rightLines, speed, shouldShow);
        UpdateSystem(topLines, speed, shouldShow);
    }

    private void UpdateSystem(ParticleSystem ps, float speed, bool shouldShow)
    {
        if (ps == null)
            return;

        var emission = ps.emission;
        var main = ps.main;

        if (!shouldShow)
        {
            emission.rateOverTime = 0f;

            if (ps.isPlaying)
                ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            return;
        }

        if (!ps.isPlaying)
            ps.Play();

        float t = Mathf.Clamp01(speed / speedForMaxEffect);

        emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, t);
        main.startSpeed = Mathf.Lerp(minParticleSpeed, maxParticleSpeed, t);
    }
}
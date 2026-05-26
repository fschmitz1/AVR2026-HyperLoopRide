using UnityEngine;
using TMPro;

public class SpeedDisplay : MonoBehaviour
{
    [Header("References")]
    public HyperloopJoystickController hyperloopController;
    public TMP_Text speedText;

    [Header("Display")]
    public string unit = "m/s";
    public float multiplier = 1f;
    public int decimalPlaces = 1;
    public bool showAbsoluteSpeed = true;

    private void Awake()
    {
        if (speedText == null)
            speedText = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (hyperloopController == null || speedText == null)
            return;

        float speed = hyperloopController.currentSpeed * multiplier;

        if (showAbsoluteSpeed)
            speed = Mathf.Abs(speed);

        speedText.text = speed.ToString("F" + decimalPlaces) + " " + unit;
    }
}
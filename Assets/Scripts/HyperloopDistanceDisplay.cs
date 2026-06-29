using UnityEngine;
using TMPro;

public class HyperloopRouteDisplay : MonoBehaviour
{
    [Header("World References")]
    public Transform hyperloop;
    public Transform stationStart;
    public Transform stationTarget;

    [Header("UI References")]
    public RectTransform startPointUI;
    public RectTransform endPointUI;
    public RectTransform hyperloopIconUI;
    public TMP_Text distanceText;

    [Header("Display")]
    public string distancePrefix = "Reststrecke:";
    public int distanceDecimals = 1;
    public bool clampProgress = true;

    [Header("Debug")]
    public float totalDistance;
    public float progress01;
    public float remainingDistanceMeters;
    public float remainingDistanceKm;

    private Vector3 routeDirection;

    private void Start()
    {
        RecalculateRoute();
        UpdateDisplay();
    }

    private void Update()
    {
        UpdateDisplay();
    }

    public void RecalculateRoute()
    {
        if (stationStart == null || stationTarget == null)
            return;

        Vector3 start = stationStart.position;
        Vector3 target = stationTarget.position;

        Vector3 routeVector = target - start;
        totalDistance = routeVector.magnitude;

        if (totalDistance > 0.0001f)
            routeDirection = routeVector.normalized;
        else
            routeDirection = Vector3.forward;
    }

    private void UpdateDisplay()
    {
        if (hyperloop == null || stationStart == null || stationTarget == null)
            return;

        if (startPointUI == null || endPointUI == null || hyperloopIconUI == null || distanceText == null)
            return;

        if (totalDistance <= 0.0001f)
            RecalculateRoute();

        Vector3 start = stationStart.position;
        Vector3 current = hyperloop.position;

        // Projektion des Hyperloops auf die Fahrtrichtung
        float distanceFromStartAlongRoute = Vector3.Dot(current - start, routeDirection);

        progress01 = totalDistance > 0.0001f
            ? distanceFromStartAlongRoute / totalDistance
            : 0f;

        if (clampProgress)
            progress01 = Mathf.Clamp01(progress01);

        remainingDistanceMeters = Mathf.Max(0f, totalDistance - (progress01 * totalDistance));
        remainingDistanceKm = remainingDistanceMeters / 1000f;

        // Icon auf UI-Linie bewegen
        Vector2 startPos = startPointUI.anchoredPosition;
        Vector2 endPos = endPointUI.anchoredPosition;

        hyperloopIconUI.anchoredPosition = Vector2.Lerp(startPos, endPos, progress01);

        // Distanztext
        distanceText.text = distancePrefix + " " + remainingDistanceKm.ToString("F" + distanceDecimals) + " km";
    }
}
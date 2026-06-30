using UnityEngine;
using TMPro;

public class HyperloopLoopRouteDisplay : MonoBehaviour
{
    [Header("Route Manager")]
    public HyperloopRouteLoopTeleporter routeTeleporter;

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
    public float progress01;
    public float remainingDistanceKm;

    private void Update()
    {
        if (routeTeleporter == null)
            return;

        if (startPointUI == null || endPointUI == null || hyperloopIconUI == null || distanceText == null)
            return;

        progress01 = routeTeleporter.progress01;
        remainingDistanceKm = routeTeleporter.remainingDistanceKm;

        if (clampProgress)
            progress01 = Mathf.Clamp01(progress01);

        Vector2 startPos = startPointUI.anchoredPosition;
        Vector2 endPos = endPointUI.anchoredPosition;

        hyperloopIconUI.anchoredPosition = Vector2.Lerp(startPos, endPos, progress01);

        distanceText.text =
            distancePrefix + " " +
            remainingDistanceKm.ToString("F" + distanceDecimals) +
            " km";
    }
}
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class DisableRayWhileDirectGrab : MonoBehaviour
{
    [Header("Interactors")]
    [SerializeField] private XRDirectInteractor directInteractor;
    [SerializeField] private XRRayInteractor rayInteractor;

    [Header("Optional")]
    [SerializeField] private LineRenderer rayLineRenderer;
    [SerializeField] private Behaviour rayLineVisual;

    private void Reset()
    {
        directInteractor = GetComponent<XRDirectInteractor>();
        rayInteractor = GetComponent<XRRayInteractor>();
        rayLineRenderer = GetComponent<LineRenderer>();
        rayLineVisual = GetComponent<Behaviour>();
    }

    private void OnEnable()
    {
        if (directInteractor)
        {
            directInteractor.selectEntered.AddListener(OnDirectGrabStarted);
            directInteractor.selectExited.AddListener(OnDirectGrabEnded);
        }
    }

    private void OnDisable()
    {
        if (directInteractor)
        {
            directInteractor.selectEntered.RemoveListener(OnDirectGrabStarted);
            directInteractor.selectExited.RemoveListener(OnDirectGrabEnded);
        }
    }

    private void OnDirectGrabStarted(SelectEnterEventArgs args)
    {
        SetRayEnabled(false);
    }

    private void OnDirectGrabEnded(SelectExitEventArgs args)
    {
        // Einen kleinen Moment warten, damit das Loslassen nicht sofort als Teleport zählt.
        Invoke(nameof(EnableRayAgain), 0.1f);
    }

    private void EnableRayAgain()
    {
        SetRayEnabled(true);
    }

    private void SetRayEnabled(bool enabled)
    {
        if (rayInteractor)
            rayInteractor.enabled = enabled;

        if (rayLineRenderer)
            rayLineRenderer.enabled = enabled;

        if (rayLineVisual)
            rayLineVisual.enabled = enabled;
    }
}
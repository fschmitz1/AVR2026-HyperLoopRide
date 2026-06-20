using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
public class KartonCloseManager : MonoBehaviour
{
    [Header("Box")]
    [SerializeField] private Rigidbody boxRigidbody;
    [SerializeField] private XRGrabInteractable boxGrabInteractable;

    [Header("Flaps")]
    [SerializeField] private KartonKlappeLock[] flaps;

    [Header("After Closing")]
    [SerializeField] private bool useGravityWhenClosed = true;

    private bool boxIsClosed;

    private void Reset()
    {
        boxRigidbody = GetComponent<Rigidbody>();
        boxGrabInteractable = GetComponent<XRGrabInteractable>();
    }

    private void Awake()
    {
        if (!boxRigidbody) boxRigidbody = GetComponent<Rigidbody>();
        if (!boxGrabInteractable) boxGrabInteractable = GetComponent<XRGrabInteractable>();

        boxRigidbody.isKinematic = true;
        boxRigidbody.useGravity = false;

        if (boxGrabInteractable)
        {
            boxGrabInteractable.enabled = false;
        }
    }

    public void NotifyFlapLocked(KartonKlappeLock flap)
    {
        if (boxIsClosed)
            return;

        if (AllFlapsLocked())
        {
            MakeBoxMovable();
        }
    }

    private bool AllFlapsLocked()
    {
        foreach (KartonKlappeLock flap in flaps)
        {
            if (!flap || !flap.IsLocked)
            {
                return false;
            }
        }

        return true;
    }

    private void MakeBoxMovable()
    {
        boxIsClosed = true;

        if (boxRigidbody)
        {
            boxRigidbody.isKinematic = false;
            boxRigidbody.useGravity = true;
            boxRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            boxRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }

        if (boxGrabInteractable)
        {
            boxGrabInteractable.enabled = true;
        }

        Debug.Log("Alle 4 Klappen sind geschlossen. Karton kann jetzt aufgehoben werden. Gravity ist aktiv.");
    }
}
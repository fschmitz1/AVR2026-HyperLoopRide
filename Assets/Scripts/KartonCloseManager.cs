using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class KartonCloseManager : MonoBehaviour
{
    [Header("Opened Box")]
    [SerializeField] private GameObject openedBoxObject;

    [Header("Closed Box Replacement")]
    [SerializeField] private GameObject closedBoxObject;
    [SerializeField] private Rigidbody closedBoxRigidbody;
    [SerializeField] private XRGrabInteractable closedBoxGrabInteractable;

    [Header("Flaps")]
    [SerializeField] private KartonKlappeLock[] flaps;

    [Header("Close Requirement")]
    [SerializeField] private int requiredLockedFlaps = 4;

    [Header("Physics")]
    [SerializeField] private bool useGravityWhenClosed = true;

    private bool boxIsClosed;

    private void Reset()
    {
        openedBoxObject = gameObject;
    }

    private void Awake()
    {
        if (!openedBoxObject)
            openedBoxObject = gameObject;

        if (closedBoxObject)
        {
            closedBoxObject.SetActive(false);

            if (!closedBoxRigidbody)
                closedBoxRigidbody = closedBoxObject.GetComponent<Rigidbody>();

            if (!closedBoxGrabInteractable)
                closedBoxGrabInteractable = closedBoxObject.GetComponent<XRGrabInteractable>();
        }

        if (closedBoxRigidbody)
        {
            closedBoxRigidbody.isKinematic = true;
            closedBoxRigidbody.useGravity = false;
        }

        if (closedBoxGrabInteractable)
        {
            closedBoxGrabInteractable.enabled = false;
        }
    }

    public void NotifyFlapLocked(KartonKlappeLock flap)
    {
        if (boxIsClosed)
            return;

        int lockedCount = CountLockedFlaps();
        int assignedCount = CountAssignedFlaps();

        Debug.Log($"Karton Status: {lockedCount}/{requiredLockedFlaps} Klappen geschlossen. Zugewiesen: {assignedCount}");

        if (assignedCount < requiredLockedFlaps)
        {
            Debug.LogWarning($"Im KartonCloseManager sind nur {assignedCount} Klappen eingetragen. Benötigt: {requiredLockedFlaps}.");
            return;
        }

        if (lockedCount >= requiredLockedFlaps)
        {
            ReplaceWithClosedBox();
        }
    }

    private int CountAssignedFlaps()
    {
        int count = 0;

        foreach (KartonKlappeLock flap in flaps)
        {
            if (flap)
                count++;
        }

        return count;
    }

    private int CountLockedFlaps()
    {
        int count = 0;

        foreach (KartonKlappeLock flap in flaps)
        {
            if (flap && flap.IsLocked)
                count++;
        }

        return count;
    }

    private void ReplaceWithClosedBox()
    {
        if (boxIsClosed)
            return;

        boxIsClosed = true;

        if (!closedBoxObject)
        {
            Debug.LogError("Closed Box Object ist nicht zugewiesen.");
            return;
        }

        // Geschlossene Box an exakt dieselbe Stelle setzen.
        closedBoxObject.transform.SetPositionAndRotation(
            openedBoxObject.transform.position,
            openedBoxObject.transform.rotation
        );

        closedBoxObject.transform.localScale = openedBoxObject.transform.localScale;

        // Closed Box aktivieren.
        closedBoxObject.SetActive(true);

        if (!closedBoxRigidbody)
            closedBoxRigidbody = closedBoxObject.GetComponent<Rigidbody>();

        if (!closedBoxGrabInteractable)
            closedBoxGrabInteractable = closedBoxObject.GetComponent<XRGrabInteractable>();

        if (closedBoxRigidbody)
        {
            closedBoxRigidbody.isKinematic = false;
            closedBoxRigidbody.useGravity = useGravityWhenClosed;
            closedBoxRigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            closedBoxRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

#if UNITY_6000_0_OR_NEWER
            closedBoxRigidbody.linearVelocity = Vector3.zero;
#else
            closedBoxRigidbody.velocity = Vector3.zero;
#endif

            closedBoxRigidbody.angularVelocity = Vector3.zero;
            closedBoxRigidbody.WakeUp();
        }

        if (closedBoxGrabInteractable)
        {
            closedBoxGrabInteractable.enabled = true;
        }

        // Offene Box deaktivieren.
        openedBoxObject.SetActive(false);

        Debug.Log("Offener Karton wurde durch geschlossenen Karton ersetzt.");
    }
}
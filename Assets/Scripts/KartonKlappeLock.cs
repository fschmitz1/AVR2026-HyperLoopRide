using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(HingeJoint))]
[RequireComponent(typeof(XRGrabInteractable))]
public class KartonKlappeLock : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HingeJoint hinge;
    [SerializeField] private XRGrabInteractable grabInteractable;
    [SerializeField] private Rigidbody boxBody;
    [SerializeField] private KartonCloseManager closeManager;

    [Header("Close Detection")]
    [SerializeField] private float closedAngle = 0f;
    [SerializeField] private float closeTolerance = 12f;

    [Header("Lock Settings")]
    [SerializeField] private bool disableGrabAfterLock = true;

    public bool IsLocked { get; private set; }

    private Rigidbody rb;
    private FixedJoint fixedJoint;

    private void Reset()
    {
        hinge = GetComponent<HingeJoint>();
        grabInteractable = GetComponent<XRGrabInteractable>();
        rb = GetComponent<Rigidbody>();
    }

    private void Awake()
    {
        if (!hinge) hinge = GetComponent<HingeJoint>();
        if (!grabInteractable) grabInteractable = GetComponent<XRGrabInteractable>();

        rb = GetComponent<Rigidbody>();

        if (!boxBody && hinge && hinge.connectedBody)
        {
            boxBody = hinge.connectedBody;
        }
    }

    private void OnEnable()
    {
        if (grabInteractable)
        {
            grabInteractable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (grabInteractable)
        {
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        if (IsLocked)
            return;

        if (!hinge)
            return;

        Debug.Log($"{name} released. Hinge angle: {hinge.angle}");

        if (IsNearClosed())
        {
            LockFlap();
        }
    }

    private bool IsNearClosed()
    {
        float difference = Mathf.Abs(Mathf.DeltaAngle(hinge.angle, closedAngle));
        return difference <= closeTolerance;
    }

    public void LockFlap()
    {
        if (IsLocked)
            return;

        IsLocked = true;

        StopRigidbodyMotion();

        if (hinge)
        {
            Destroy(hinge);
            hinge = null;
        }

        fixedJoint = gameObject.AddComponent<FixedJoint>();
        fixedJoint.connectedBody = boxBody;
        fixedJoint.enableCollision = false;
        fixedJoint.breakForce = Mathf.Infinity;
        fixedJoint.breakTorque = Mathf.Infinity;
        fixedJoint.massScale = 1f;
        fixedJoint.connectedMassScale = 1f;

        if (grabInteractable && disableGrabAfterLock)
        {
            grabInteractable.enabled = false;
        }

        if (closeManager)
        {
            closeManager.NotifyFlapLocked(this);
        }

        Debug.Log($"{name} locked.");
    }

    private void StopRigidbodyMotion()
    {
        if (!rb)
            return;

#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = Vector3.zero;
#else
        rb.velocity = Vector3.zero;
#endif

        rb.angularVelocity = Vector3.zero;
    }
}
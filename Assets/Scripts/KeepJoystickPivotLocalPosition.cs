using UnityEngine;

[DefaultExecutionOrder(32000)]
public class KeepJoystickPivotLocalPosition : MonoBehaviour
{
    [Header("Lock")]
    public bool useCurrentLocalPositionOnAwake = true;
    public Vector3 lockedLocalPosition;

    [Header("Rigidbody")]
    public Rigidbody rb;
    public bool syncRigidbody = true;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        if (useCurrentLocalPositionOnAwake)
            lockedLocalPosition = transform.localPosition;
    }

    private void LateUpdate()
    {
        RestoreLocalPosition();
    }

    private void RestoreLocalPosition()
    {
        transform.localPosition = lockedLocalPosition;

        if (syncRigidbody && rb != null)
        {
            rb.position = transform.position;
            rb.transform.position = transform.position;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
using UnityEngine;

[DefaultExecutionOrder(31000)]
public class KeepLocalPositionWithParent : MonoBehaviour
{
    [Header("Lock")]
    public bool lockLocalPosition = true;
    public bool lockLocalScale = true;

    [Header("Rigidbody")]
    public Rigidbody rb;
    public bool zeroVelocity = true;

    [Header("Debug")]
    public Vector3 startLocalPosition;
    public Vector3 startLocalScale;

    private void Awake()
    {
        if (rb == null)
            rb = GetComponent<Rigidbody>();

        startLocalPosition = transform.localPosition;
        startLocalScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (lockLocalPosition)
            transform.localPosition = startLocalPosition;

        if (lockLocalScale)
            transform.localScale = startLocalScale;

        if (rb != null)
        {
            rb.position = transform.position;

            if (zeroVelocity)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }
}
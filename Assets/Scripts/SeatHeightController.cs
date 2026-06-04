using UnityEngine;

public class SeatHeightController : MonoBehaviour
{
    [Header("Joystick")]
    public VRJoystickLever heightJoystick;

    [Header("Seat")]
    public Transform seatToMove;

    [Header("Player")]
    public Transform xrOrigin;
    public Transform playerHead;
    public BoxCollider seatArea;
    public bool onlyMovePlayerWhenOnSeat = true;

    [Header("Movement")]
    public Vector3 worldMoveDirection = Vector3.up;
    public float moveSpeed = 0.4f;

    [Tooltip("Wie weit der Sitz von der Startposition nach unten fahren darf.")]
    public float minOffset = -0.2f;

    [Tooltip("Wie weit der Sitz von der Startposition nach oben fahren darf.")]
    public float maxOffset = 0.5f;

    public bool invertInput = false;

    [Header("Objects With Rigidbody That Must Ride Along")]
    public Rigidbody[] carriedRigidbodies;

    [Header("Deadzone")]
    [Range(0f, 1f)]
    public float deadzone = 0.1f;

    [Header("Debug")]
    public float currentInput;
    public float currentOffset;

    private void Awake()
    {
        if (seatToMove == null)
            seatToMove = transform;

        if (worldMoveDirection.sqrMagnitude < 0.0001f)
            worldMoveDirection = Vector3.up;

        worldMoveDirection.Normalize();
    }

    private void Update()
    {
        if (heightJoystick == null || seatToMove == null)
            return;

        float input = heightJoystick.value;

        if (invertInput)
            input *= -1f;

        if (Mathf.Abs(input) < deadzone)
            input = 0f;

        currentInput = input;

        float wantedOffset = currentOffset + input * moveSpeed * Time.deltaTime;
        wantedOffset = Mathf.Clamp(wantedOffset, minOffset, maxOffset);

        float deltaOffset = wantedOffset - currentOffset;
        currentOffset = wantedOffset;

        Vector3 movement = worldMoveDirection * deltaOffset;

        MoveSeat(movement);
    }

    private void MoveSeat(Vector3 movement)
    {
        if (movement.sqrMagnitude <= 0.0000001f)
            return;

        Vector3[] oldRigidbodyWorldPositions = new Vector3[carriedRigidbodies.Length];

        for (int i = 0; i < carriedRigidbodies.Length; i++)
        {
            if (carriedRigidbodies[i] != null)
                oldRigidbodyWorldPositions[i] = carriedRigidbodies[i].transform.position;
        }

        seatToMove.position += movement;

        for (int i = 0; i < carriedRigidbodies.Length; i++)
        {
            Rigidbody rb = carriedRigidbodies[i];

            if (rb == null)
                continue;

            Vector3 targetWorldPosition = oldRigidbodyWorldPositions[i] + movement;

            rb.position = targetWorldPosition;
            rb.transform.position = targetWorldPosition;
        }

        if (ShouldMovePlayer())
        {
            xrOrigin.position += movement;
        }
    }

    private bool ShouldMovePlayer()
    {
        if (!onlyMovePlayerWhenOnSeat)
            return xrOrigin != null;

        if (xrOrigin == null || playerHead == null || seatArea == null)
            return false;

        Vector3 closestPoint = seatArea.ClosestPoint(playerHead.position);
        return Vector3.Distance(closestPoint, playerHead.position) < 0.01f;
    }
}
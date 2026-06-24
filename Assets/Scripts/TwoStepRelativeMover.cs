using System.Collections;
using UnityEngine;

public class TwoStepRelativeMover : MonoBehaviour
{
    [Header("Object To Move")]
    public Transform objectToMove;

    [Header("Step 1: Relative movement")]
    public Vector3 step1LocalPositionOffset;
    public Vector3 step1LocalRotationOffset;
    public float step1Duration = 1f;

    [Header("Pause")]
    public float pauseDuration = 0.3f;

    [Header("Step 2: Relative movement")]
    public Vector3 step2LocalPositionOffset;
    public Vector3 step2LocalRotationOffset;
    public float step2Duration = 1f;

    [Header("Player Ride Along Optional")]
    public bool movePlayerWithObject = false;
    public Transform xrOrigin;
    public Transform playerHead;
    public BoxCollider rideArea;
    public bool onlyMovePlayerWhenInside = true;
    public bool rotatePlayerWithObject = true;

    [Header("Objects With Rigidbody That Must Ride Along")]
    public Rigidbody[] carriedRigidbodies;

    [Header("Smoothing")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("State")]
    public bool isOpen;
    public bool isMoving;

    [Header("Debug")]
    public Vector3 currentPositionOffset;
    public Quaternion currentRotationOffset = Quaternion.identity;

    private Coroutine moveRoutine;

    private void Awake()
    {
        if (objectToMove == null)
            objectToMove = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        currentPositionOffset = Vector3.zero;
        currentRotationOffset = Quaternion.identity;
    }

    public void OpenDoor()
    {
        if (isMoving || isOpen)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        PlaySound(openSound);
        moveRoutine = StartCoroutine(OpenRoutine());
    }

    public void CloseDoor()
    {
        if (isMoving || !isOpen)
            return;

        if (moveRoutine != null)
            StopCoroutine(moveRoutine);

        PlaySound(closeSound);
        moveRoutine = StartCoroutine(CloseRoutine());
    }

    public void ToggleDoor()
    {
        if (isMoving)
            return;

        if (isOpen)
            CloseDoor();
        else
            OpenDoor();
    }

    private IEnumerator OpenRoutine()
    {
        isMoving = true;

        yield return MoveToRelativeOffset(
            step1LocalPositionOffset,
            Quaternion.Euler(step1LocalRotationOffset),
            step1Duration
        );

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveToRelativeOffset(
            step2LocalPositionOffset,
            Quaternion.Euler(step2LocalRotationOffset),
            step2Duration
        );

        isOpen = true;
        isMoving = false;
    }

    private IEnumerator CloseRoutine()
    {
        isMoving = true;

        yield return MoveToRelativeOffset(
            step1LocalPositionOffset,
            Quaternion.Euler(step1LocalRotationOffset),
            step2Duration
        );

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveToRelativeOffset(
            Vector3.zero,
            Quaternion.identity,
            step1Duration
        );

        isOpen = false;
        isMoving = false;
    }

    private IEnumerator MoveToRelativeOffset(Vector3 targetPositionOffset, Quaternion targetRotationOffset, float duration)
    {
        Vector3 startPositionOffset = currentPositionOffset;
        Quaternion startRotationOffset = currentRotationOffset;

        Vector3 previousPositionOffset = startPositionOffset;
        Quaternion previousRotationOffset = startRotationOffset;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = movementCurve.Evaluate(t);

            Vector3 desiredPositionOffset = Vector3.Lerp(startPositionOffset, targetPositionOffset, curvedT);
            Quaternion desiredRotationOffset = Quaternion.Slerp(startRotationOffset, targetRotationOffset, curvedT);

            Vector3 deltaPositionOffset = desiredPositionOffset - previousPositionOffset;
            Quaternion deltaRotationOffset = desiredRotationOffset * Quaternion.Inverse(previousRotationOffset);

            ApplyRelativeDelta(deltaPositionOffset, deltaRotationOffset);

            previousPositionOffset = desiredPositionOffset;
            previousRotationOffset = desiredRotationOffset;

            currentPositionOffset = desiredPositionOffset;
            currentRotationOffset = desiredRotationOffset;

            yield return null;
        }

        Vector3 finalDeltaPositionOffset = targetPositionOffset - previousPositionOffset;
        Quaternion finalDeltaRotationOffset = targetRotationOffset * Quaternion.Inverse(previousRotationOffset);

        ApplyRelativeDelta(finalDeltaPositionOffset, finalDeltaRotationOffset);

        currentPositionOffset = targetPositionOffset;
        currentRotationOffset = targetRotationOffset;
    }

    private void ApplyRelativeDelta(Vector3 deltaLocalPosition, Quaternion deltaLocalRotation)
    {
        if (objectToMove == null)
            return;

        bool playerWasInside = ShouldMovePlayer();

        Matrix4x4 beforeMatrix = objectToMove.localToWorldMatrix;
        Quaternion beforeWorldRotation = objectToMove.rotation;

        Vector3[] oldRigidbodyPositions = new Vector3[carriedRigidbodies.Length];
        Quaternion[] oldRigidbodyRotations = new Quaternion[carriedRigidbodies.Length];

        for (int i = 0; i < carriedRigidbodies.Length; i++)
        {
            if (carriedRigidbodies[i] == null)
                continue;

            oldRigidbodyPositions[i] = carriedRigidbodies[i].transform.position;
            oldRigidbodyRotations[i] = carriedRigidbodies[i].transform.rotation;
        }

        Vector3 oldXrOriginPosition = Vector3.zero;
        Quaternion oldXrOriginRotation = Quaternion.identity;

        if (xrOrigin != null)
        {
            oldXrOriginPosition = xrOrigin.position;
            oldXrOriginRotation = xrOrigin.rotation;
        }

        objectToMove.localPosition += deltaLocalPosition;
        objectToMove.localRotation = objectToMove.localRotation * deltaLocalRotation;

        Matrix4x4 afterMatrix = objectToMove.localToWorldMatrix;
        Quaternion afterWorldRotation = objectToMove.rotation;

        Matrix4x4 deltaMatrix = afterMatrix * beforeMatrix.inverse;
        Quaternion worldRotationDelta = afterWorldRotation * Quaternion.Inverse(beforeWorldRotation);

        for (int i = 0; i < carriedRigidbodies.Length; i++)
        {
            Rigidbody rb = carriedRigidbodies[i];

            if (rb == null)
                continue;

            Vector3 targetPosition = deltaMatrix.MultiplyPoint3x4(oldRigidbodyPositions[i]);
            Quaternion targetRotation = worldRotationDelta * oldRigidbodyRotations[i];

            rb.position = targetPosition;
            rb.rotation = targetRotation;

            rb.transform.position = targetPosition;
            rb.transform.rotation = targetRotation;
        }

        if (movePlayerWithObject && playerWasInside && xrOrigin != null)
        {
            xrOrigin.position = deltaMatrix.MultiplyPoint3x4(oldXrOriginPosition);

            if (rotatePlayerWithObject)
                xrOrigin.rotation = worldRotationDelta * oldXrOriginRotation;
        }
    }

    private bool ShouldMovePlayer()
    {
        if (!movePlayerWithObject)
            return false;

        if (!onlyMovePlayerWhenInside)
            return xrOrigin != null;

        if (xrOrigin == null || playerHead == null || rideArea == null)
            return false;

        Vector3 localPoint = rideArea.transform.InverseTransformPoint(playerHead.position);

        Vector3 center = rideArea.center;
        Vector3 halfSize = rideArea.size * 0.5f;

        float margin = 0.2f;

        bool insideX = localPoint.x >= center.x - halfSize.x - margin &&
                       localPoint.x <= center.x + halfSize.x + margin;

        bool insideY = localPoint.y >= center.y - halfSize.y - margin &&
                       localPoint.y <= center.y + halfSize.y + margin;

        bool insideZ = localPoint.z >= center.z - halfSize.z - margin &&
                       localPoint.z <= center.z + halfSize.z + margin;

        return insideX && insideY && insideZ;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
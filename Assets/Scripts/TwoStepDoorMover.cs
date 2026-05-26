using System.Collections;
using UnityEngine;

public class TwoStepDoorMover : MonoBehaviour
{
    [Header("Door Object")]
    public Transform door;

    [Header("Step 1: Door moves out")]
    public Vector3 step1LocalPosition;
    public Vector3 step1LocalRotation;
    public float step1Duration = 1f;

    [Header("Pause")]
    public float pauseDuration = 0.3f;

    [Header("Step 2: Door moves up")]
    public Vector3 step2LocalPosition;
    public Vector3 step2LocalRotation;
    public float step2Duration = 1f;

    [Header("Smoothing")]
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("State")]
    public bool isOpen;
    public bool isMoving;

    private Vector3 closedLocalPosition;
    private Quaternion closedLocalRotation;
    private Coroutine moveRoutine;

    private void Awake()
    {
        if (door == null)
            door = transform;

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        closedLocalPosition = door.localPosition;
        closedLocalRotation = door.localRotation;
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

        yield return MoveTo(
            step1LocalPosition,
            Quaternion.Euler(step1LocalRotation),
            step1Duration
        );

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveTo(
            step2LocalPosition,
            Quaternion.Euler(step2LocalRotation),
            step2Duration
        );

        isOpen = true;
        isMoving = false;
    }

    private IEnumerator CloseRoutine()
    {
        isMoving = true;

        PlaySound(closeSound);

        yield return MoveTo(
            step1LocalPosition,
            Quaternion.Euler(step1LocalRotation),
            step2Duration
        );

        yield return new WaitForSeconds(pauseDuration);

        yield return MoveTo(
            closedLocalPosition,
            closedLocalRotation,
            step1Duration
        );

        isOpen = false;
        isMoving = false;
    }

    private IEnumerator MoveTo(Vector3 targetLocalPosition, Quaternion targetLocalRotation, float duration)
    {
        Vector3 startPosition = door.localPosition;
        Quaternion startRotation = door.localRotation;

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = Mathf.Clamp01(elapsed / duration);
            float curvedT = movementCurve.Evaluate(t);

            door.localPosition = Vector3.Lerp(startPosition, targetLocalPosition, curvedT);
            door.localRotation = Quaternion.Slerp(startRotation, targetLocalRotation, curvedT);

            yield return null;
        }

        door.localPosition = targetLocalPosition;
        door.localRotation = targetLocalRotation;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}
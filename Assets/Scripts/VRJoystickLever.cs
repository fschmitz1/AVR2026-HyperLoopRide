using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRJoystickLever : MonoBehaviour
{
    public enum RotationAxis
    {
        LocalX,
        LocalZ
    }

    [Header("References")]
    public XRGrabInteractable grabInteractable;
    public Transform pivot;

    [Header("Rotation")]
    public RotationAxis rotationAxis = RotationAxis.LocalX;
    public float minAngle = -30f;
    public float maxAngle = 30f;
    public bool invert = false;

    [Header("Return")]
    public bool returnToCenter = true;
    public float returnSpeed = 8f;

    [Header("Button Visual")]
    public Transform buttonVisual;
    public Vector3 buttonPressedLocalOffset = new Vector3(0f, 0f, -0.01f);
    public float buttonMoveSpeed = 20f;

    [Header("Button Input")]
    public InputActionReference primaryButtonAction;
    public InputActionReference secondaryButtonAction;

    [Header("Button Audio")]
    public AudioSource buttonAudioSource;
    public AudioClip buttonClickSound;

    [Header("Output")]
    [Range(-1f, 1f)]
    public float value;
    public bool buttonPressed;

    private Transform handTransform;
    private bool isGrabbed;

    private Quaternion startLocalRotation;
    private Vector3 buttonStartLocalPosition;

    private float grabStartHandAngle;
    private float grabStartLeverAngle;

    private bool wasButtonPressed;

    private void Awake()
    {
        if (grabInteractable == null)
            grabInteractable = GetComponent<XRGrabInteractable>();

        if (pivot == null)
            pivot = transform;

        startLocalRotation = pivot.localRotation;

        if (buttonVisual != null)
            buttonStartLocalPosition = buttonVisual.localPosition;
    }

    private void OnEnable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.AddListener(OnGrabbed);
            grabInteractable.selectExited.AddListener(OnReleased);
        }

        if (primaryButtonAction != null)
            primaryButtonAction.action.Enable();

        if (secondaryButtonAction != null)
            secondaryButtonAction.action.Enable();
    }

    private void OnDisable()
    {
        if (grabInteractable != null)
        {
            grabInteractable.selectEntered.RemoveListener(OnGrabbed);
            grabInteractable.selectExited.RemoveListener(OnReleased);
        }

        if (primaryButtonAction != null)
            primaryButtonAction.action.Disable();

        if (secondaryButtonAction != null)
            secondaryButtonAction.action.Disable();
    }

    private void Update()
    {
        
        UpdateLever();
        UpdateButton();

    }

    private void UpdateLever()
    {
        float targetAngle = GetCurrentLeverAngle();

        if (isGrabbed && handTransform != null)
        {
            float currentHandAngle = GetHandAngle();
            float handDelta = Mathf.DeltaAngle(grabStartHandAngle, currentHandAngle);

            if (invert)
                handDelta *= -1f;

            targetAngle = grabStartLeverAngle + handDelta;
        }
        else if (returnToCenter)
        {
            targetAngle = Mathf.Lerp(targetAngle, 0f, Time.deltaTime * returnSpeed);
        }

        targetAngle = Mathf.Clamp(targetAngle, minAngle, maxAngle);
        ApplyLeverAngle(targetAngle);

        value = Mathf.InverseLerp(minAngle, maxAngle, targetAngle) * 2f - 1f;
    }

    private void UpdateButton()
    {
        bool primaryPressed = primaryButtonAction != null && primaryButtonAction.action.IsPressed();
        bool secondaryPressed = secondaryButtonAction != null && secondaryButtonAction.action.IsPressed();

        buttonPressed = isGrabbed && (primaryPressed || secondaryPressed);

        if (buttonPressed && !wasButtonPressed)
        {
            if (buttonAudioSource != null && buttonClickSound != null)
                buttonAudioSource.PlayOneShot(buttonClickSound);
        }

        wasButtonPressed = buttonPressed;

        if (buttonVisual == null)
            return;

        Vector3 targetPosition = buttonPressed
            ? buttonStartLocalPosition + buttonPressedLocalOffset
            : buttonStartLocalPosition;

        buttonVisual.localPosition = Vector3.Lerp(
            buttonVisual.localPosition,
            targetPosition,
            Time.deltaTime * buttonMoveSpeed
        );
    }

    private void OnGrabbed(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        handTransform = args.interactorObject.transform;

        grabStartHandAngle = GetHandAngle();
        grabStartLeverAngle = GetCurrentLeverAngle();
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isGrabbed = false;
        handTransform = null;
        buttonPressed = false;
        wasButtonPressed = false;
    }

    private float GetHandAngle()
    {
        if (handTransform == null)
            return 0f;

        Vector3 handPositionInParentSpace = pivot.parent != null
            ? pivot.parent.InverseTransformPoint(handTransform.position)
            : handTransform.position;

        Vector3 directionInParentSpace = handPositionInParentSpace - pivot.localPosition;

        Vector3 directionInNeutralPivotSpace =
            Quaternion.Inverse(startLocalRotation) * directionInParentSpace;

        if (rotationAxis == RotationAxis.LocalX)
            return Mathf.Atan2(directionInNeutralPivotSpace.z, directionInNeutralPivotSpace.y) * Mathf.Rad2Deg;

        return -Mathf.Atan2(directionInNeutralPivotSpace.x, directionInNeutralPivotSpace.y) * Mathf.Rad2Deg;
    }

    private float GetCurrentLeverAngle()
    {
        Quaternion relativeRotation = Quaternion.Inverse(startLocalRotation) * pivot.localRotation;
        Vector3 euler = relativeRotation.eulerAngles;

        float angle = rotationAxis == RotationAxis.LocalX ? euler.x : euler.z;

        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    private void ApplyLeverAngle(float angle)
    {
        Quaternion axisRotation = rotationAxis == RotationAxis.LocalX
            ? Quaternion.AngleAxis(angle, Vector3.right)
            : Quaternion.AngleAxis(angle, Vector3.forward);

        pivot.localRotation = startLocalRotation * axisRotation;
    }
}
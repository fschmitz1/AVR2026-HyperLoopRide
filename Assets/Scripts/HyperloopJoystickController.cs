using UnityEngine;

public class HyperloopJoystickController : MonoBehaviour
{
    [Header("Joystick")]
    public VRJoystickLever joystick;

    [Header("Player")]
    public Transform xrOrigin;
    public Transform playerHead;

    [Header("Normal Movement")]
    public Vector3 localMoveDirection = Vector3.forward;
    public float maxSpeed = 8f;
    public float acceleration = 2f;
    public float deceleration = 4f;
    public bool invertInput = true;

    [Header("Boost Movement")]
    public bool boostModeActive;
    public float boostMultiplier = 10f;

    [Header("Boost Start Sequence")]
    public float joystickForwardThreshold = 0.7f;
    public float requiredForwardHoldTime = 3f;
    public bool waitingForJoystickForward;
    public bool boostReadyToLaunch;
    public bool boostLaunchConfirmed;
    public float forwardHoldTimer;

    [Header("Cruise Control")]
    public bool cruiseControlEnabled;
    public float cruiseSpeed;
    public float minCruiseSpeed = 0.1f;

    [Header("Player Ride Detection")]
    public bool onlyMovePlayerWhenInside = true;
    public BoxCollider rideArea;

    [Header("Objects With Rigidbody That Must Ride Along")]
    public Rigidbody[] carriedRigidbodies;

    [Header("Deadzone")]
    [Range(0f, 1f)]
    public float deadzone = 0.1f;

    [Header("Debug")]
    public float currentSpeed;
    public float currentInput;
    public float currentTargetSpeed;

    private bool wasCruiseButtonPressed;

    private void LateUpdate()
    {
        if (joystick == null)
            return;

        float input = joystick.value;

        if (invertInput)
            input *= -1f;

        if (Mathf.Abs(input) < deadzone)
            input = 0f;

        currentInput = input;

        HandleCruiseControlButton();

        if (boostModeActive && !boostLaunchConfirmed)
        {
            HandleBoostStartSequence(input);
            SlowDownWhileWaiting();
            return;
        }

        float activeMaxSpeed = boostModeActive ? maxSpeed * boostMultiplier : maxSpeed;
        float activeAcceleration = boostModeActive ? acceleration * boostMultiplier : acceleration;
        float activeDeceleration = boostModeActive ? deceleration * boostMultiplier : deceleration;

        float joystickTargetSpeed = input * activeMaxSpeed;

        float targetSpeed = cruiseControlEnabled
            ? cruiseSpeed
            : joystickTargetSpeed;

        currentTargetSpeed = targetSpeed;

        float speedChangeRate = Mathf.Abs(targetSpeed) > Mathf.Abs(currentSpeed)
            ? activeAcceleration
            : activeDeceleration;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            targetSpeed,
            speedChangeRate * Time.deltaTime
        );

        if (Mathf.Approximately(currentSpeed, 0f))
            return;

        Vector3 movement =
            transform.TransformDirection(localMoveDirection.normalized)
            * currentSpeed
            * Time.deltaTime;

        MoveHyperloop(movement);
    }

    public void PowerButtonPressed()
    {
        if (!boostModeActive)
        {
            ActivateBoostMode();
            return;
        }

        if (boostModeActive && boostReadyToLaunch && !boostLaunchConfirmed)
        {
            ConfirmBoostLaunch();
            return;
        }

        if (boostModeActive && boostLaunchConfirmed)
        {
            DeactivateBoostMode();
            return;
        }
    }

    private void ActivateBoostMode()
    {
        boostModeActive = true;

        waitingForJoystickForward = true;
        boostReadyToLaunch = false;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;

        currentSpeed = 0f;
    }

    private void ConfirmBoostLaunch()
    {
        boostLaunchConfirmed = true;
        waitingForJoystickForward = false;
        boostReadyToLaunch = false;
    }

    private void DeactivateBoostMode()
    {
        boostModeActive = false;

        waitingForJoystickForward = false;
        boostReadyToLaunch = false;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;
    }

    private void HandleBoostStartSequence(float input)
    {
        if (!waitingForJoystickForward)
            return;

        if (input >= joystickForwardThreshold)
        {
            forwardHoldTimer += Time.deltaTime;

            if (forwardHoldTimer >= requiredForwardHoldTime)
            {
                boostReadyToLaunch = true;
                waitingForJoystickForward = false;
            }
        }
        else
        {
            forwardHoldTimer = 0f;
        }
    }

    private void SlowDownWhileWaiting()
    {
        currentTargetSpeed = 0f;

        currentSpeed = Mathf.MoveTowards(
            currentSpeed,
            0f,
            deceleration * Time.deltaTime
        );

        if (Mathf.Approximately(currentSpeed, 0f))
            return;

        Vector3 movement =
            transform.TransformDirection(localMoveDirection.normalized)
            * currentSpeed
            * Time.deltaTime;

        MoveHyperloop(movement);
    }

    private void HandleCruiseControlButton()
    {
        if (boostModeActive)
            return;

        bool cruiseButtonPressed = joystick.buttonPressed;

        if (cruiseButtonPressed && !wasCruiseButtonPressed)
        {
            if (!cruiseControlEnabled)
            {
                if (Mathf.Abs(currentSpeed) >= minCruiseSpeed)
                {
                    cruiseSpeed = currentSpeed;
                    cruiseControlEnabled = true;
                }
            }
            else
            {
                cruiseControlEnabled = false;
                cruiseSpeed = 0f;
            }
        }

        wasCruiseButtonPressed = cruiseButtonPressed;
    }

    private void MoveHyperloop(Vector3 movement)
    {
        Vector3[] oldRigidbodyWorldPositions = new Vector3[carriedRigidbodies.Length];

        for (int i = 0; i < carriedRigidbodies.Length; i++)
        {
            if (carriedRigidbodies[i] != null)
                oldRigidbodyWorldPositions[i] = carriedRigidbodies[i].transform.position;
        }

        transform.position += movement;

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
        if (!onlyMovePlayerWhenInside)
            return xrOrigin != null;

        if (xrOrigin == null || playerHead == null || rideArea == null)
            return false;

        Vector3 closestPoint = rideArea.ClosestPoint(playerHead.position);
        return Vector3.Distance(closestPoint, playerHead.position) < 0.01f;
    }
}
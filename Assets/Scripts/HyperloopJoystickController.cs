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

    [Header("Emergency Brake")]
    public bool emergencyBraking;
    public float emergencyBrakeDuration = 1f;
    public float emergencyBrakeTimer;
    private float emergencyBrakeStartSpeed;

    [Header("Boost Start Sequence")]
    public float joystickForwardThreshold = 0.7f;
    public float requiredForwardHoldTime = 3f;
    public bool waitingForJoystickForward;
    public bool boostLaunchConfirmed;
    public float forwardHoldTimer;

    [Header("Boost Countdown Lights")]
    public GameObject countdownLight1;
    public GameObject countdownLight2;
    public GameObject countdownLight3;

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

    private void Start()
    {
        SetCountdownLights(false, false, false);
    }

    private void LateUpdate()
    {
        if (emergencyBraking)
        {
            UpdateEmergencyBrake();
            return;
        }

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
            UpdateCountdownLights();
            SlowDownWhileWaiting();
            return;
        }

        UpdateCountdownLights();

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
        if (emergencyBraking)
            return;

        if (boostModeActive)
        {
            if (Mathf.Abs(currentSpeed) > 0.01f)
            {
                StartEmergencyBrake();
                return;
            }

            DeactivateBoostMode();
            return;
        }

        ActivateBoostMode();
    }

    private void StartEmergencyBrake()
    {
        emergencyBraking = true;
        emergencyBrakeTimer = 0f;
        emergencyBrakeStartSpeed = currentSpeed;

        boostModeActive = false;
        waitingForJoystickForward = false;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;
        currentTargetSpeed = 0f;

        SetCountdownLights(false, false, false);
    }

    private void UpdateEmergencyBrake()
    {
        emergencyBrakeTimer += Time.deltaTime;

        float duration = Mathf.Max(0.01f, emergencyBrakeDuration);
        float t = Mathf.Clamp01(emergencyBrakeTimer / duration);

        currentSpeed = Mathf.Lerp(emergencyBrakeStartSpeed, 0f, t);
        currentTargetSpeed = 0f;

        if (!Mathf.Approximately(currentSpeed, 0f))
        {
            Vector3 movement =
                transform.TransformDirection(localMoveDirection.normalized)
                * currentSpeed
                * Time.deltaTime;

            MoveHyperloop(movement);
        }

        if (t >= 1f)
        {
            currentSpeed = 0f;
            currentTargetSpeed = 0f;
            emergencyBraking = false;
        }
    }

    private void ActivateBoostMode()
    {
        boostModeActive = true;
        waitingForJoystickForward = true;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;

        currentSpeed = 0f;
        currentTargetSpeed = 0f;

        SetCountdownLights(false, false, false);
    }

    private void ConfirmBoostLaunch()
    {
        boostLaunchConfirmed = true;
        waitingForJoystickForward = false;
        forwardHoldTimer = requiredForwardHoldTime;

        SetCountdownLights(false, false, true);
    }

    private void DeactivateBoostMode()
    {
        boostModeActive = false;
        waitingForJoystickForward = false;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;

        SetCountdownLights(false, false, false);
    }

    private void HandleBoostStartSequence(float input)
    {
        if (!waitingForJoystickForward)
            return;

        if (Mathf.Abs(input) >= joystickForwardThreshold)
        {
            forwardHoldTimer += Time.deltaTime;

            if (forwardHoldTimer >= requiredForwardHoldTime)
            {
                ConfirmBoostLaunch();
            }
        }
        else
        {
            forwardHoldTimer = 0f;
        }
    }

    private void UpdateCountdownLights()
    {
        if (!boostModeActive)
        {
            SetCountdownLights(false, false, false);
            return;
        }

        if (boostLaunchConfirmed)
        {
            SetCountdownLights(false, false, true);
            return;
        }

        bool light1On = forwardHoldTimer >= 1f;
        bool light2On = forwardHoldTimer >= 2f;
        bool light3On = forwardHoldTimer >= 3f;

        SetCountdownLights(light1On, light2On, light3On);
    }

    private void SetCountdownLights(bool light1, bool light2, bool light3)
    {
        if (countdownLight1 != null)
            countdownLight1.SetActive(light1);

        if (countdownLight2 != null)
            countdownLight2.SetActive(light2);

        if (countdownLight3 != null)
            countdownLight3.SetActive(light3);
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
        bool playerWasInside = ShouldMovePlayer();

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

        if (playerWasInside && xrOrigin != null)
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
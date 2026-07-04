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

    [Header("Boost Countdown Audio")]
    public AudioSource countdownAudioSource;
    public AudioClip countdownNegativeSound1;
    public AudioClip countdownNegativeSound2;
    public AudioClip countdownPositiveSound;
    public float countdownSoundVolume = 1f;

    [Header("Cruise Control")]
    public bool cruiseControlEnabled;
    public float cruiseSpeed;
    public float minCruiseSpeed = 0.1f;

    [Header("Player Ride Detection")]
    public bool onlyMovePlayerWhenInside = true;
    public BoxCollider rideArea;

    [Header("Station Auto Stop")]
    public bool stationAutoStopEnabled = true;
    public Transform stationStopTarget;

    [Tooltip("Radius um das Stop-Target. Bei hoher Geschwindigkeit lieber 10-20 nehmen.")]
    public float stationStopRadius = 10f;

    [Tooltip("Wenn aktiv, springt der Hyperloop exakt auf die Target-Position.")]
    public bool snapToStationStopTarget = true;

    [Tooltip("Wenn aktiv, kann der Hyperloop nach Erreichen der Station nicht versehentlich weiterfahren.")]
    public bool lockMovementAfterStationStop = true;

    public bool stationStopReached;

    [Header("Objects With Rigidbody That Must Ride Along")]
    public Rigidbody[] carriedRigidbodies;

    [Header("Deadzone")]
    [Range(0f, 1f)]
    public float deadzone = 0.1f;

    [Header("Debug")]
    public float currentSpeed;
    public float currentInput;
    public float currentTargetSpeed;
    public float distanceToStationStopTarget;

    private bool wasCruiseButtonPressed;
    private int lastCountdownSoundStep;

    private void Start()
    {
        SetCountdownLights(false, false, false);
        lastCountdownSoundStep = 0;
    }

    private void LateUpdate()
    {
        if (stationStopReached && lockMovementAfterStationStop)
        {
            currentInput = 0f;
            currentSpeed = 0f;
            currentTargetSpeed = 0f;
            return;
        }

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

        MoveHyperloopOrStopAtStation(movement);
    }

    public void PowerButtonPressed()
    {
        if (stationStopReached && lockMovementAfterStationStop)
            return;

        if (emergencyBraking)
            return;

        if (boostModeActive)
        {
            // Wenn Boost bereits gestartet ist und der Hyperloop fährt:
            // Klick = Tempomat an/aus
            if (boostLaunchConfirmed && Mathf.Abs(currentSpeed) > minCruiseSpeed)
            {
                if (!cruiseControlEnabled)
                {
                    cruiseSpeed = currentSpeed;
                    cruiseControlEnabled = true;
                    currentTargetSpeed = cruiseSpeed;
                }
                else
                {
                    cruiseControlEnabled = false;
                    cruiseSpeed = 0f;
                }

                return;
            }

            // Wenn Boostmodus aktiv ist, aber noch nicht gestartet wurde:
            // Knopf deaktiviert den Boostmodus wieder.
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

        lastCountdownSoundStep = 0;

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

            MoveHyperloopOrStopAtStation(movement);
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

        stationStopReached = false;
        lastCountdownSoundStep = 0;

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

        lastCountdownSoundStep = 0;

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
            lastCountdownSoundStep = 0;
        }
    }

    private void UpdateCountdownLights()
    {
        if (!boostModeActive)
        {
            SetCountdownLights(false, false, false);
            lastCountdownSoundStep = 0;
            return;
        }

        if (boostLaunchConfirmed)
        {
            SetCountdownLights(false, false, true);
            PlayCountdownSoundStepIfNeeded(3);
            return;
        }

        int currentStep = 0;

        if (forwardHoldTimer >= 1f)
            currentStep = 1;

        if (forwardHoldTimer >= 2f)
            currentStep = 2;

        if (forwardHoldTimer >= 3f)
            currentStep = 3;

        bool light1On = currentStep >= 1;
        bool light2On = currentStep >= 2;
        bool light3On = currentStep >= 3;

        SetCountdownLights(light1On, light2On, light3On);

        if (currentStep < lastCountdownSoundStep)
            lastCountdownSoundStep = currentStep;

        PlayCountdownSoundStepIfNeeded(currentStep);
    }

    private void PlayCountdownSoundStepIfNeeded(int step)
    {
        if (step <= 0)
            return;

        if (step <= lastCountdownSoundStep)
            return;

        lastCountdownSoundStep = step;

        AudioClip clipToPlay = null;

        if (step == 1)
            clipToPlay = countdownNegativeSound1;
        else if (step == 2)
            clipToPlay = countdownNegativeSound2;
        else if (step == 3)
            clipToPlay = countdownPositiveSound;

        if (countdownAudioSource != null && clipToPlay != null)
        {
            countdownAudioSource.PlayOneShot(clipToPlay, countdownSoundVolume);
        }
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

        MoveHyperloopOrStopAtStation(movement);
    }

    private void HandleCruiseControlButton()
    {
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

    private void MoveHyperloopOrStopAtStation(Vector3 movement)
    {
        if (ShouldStopAtStation(movement, out Vector3 movementUntilStop))
        {
            if (movementUntilStop.sqrMagnitude > 0.0000001f)
                MoveHyperloop(movementUntilStop);

            CompleteStationStop();
            return;
        }

        MoveHyperloop(movement);
    }

    private bool ShouldStopAtStation(Vector3 plannedMovement, out Vector3 movementUntilStop)
    {
        movementUntilStop = plannedMovement;

        if (!stationAutoStopEnabled)
            return false;

        if (stationStopTarget == null)
            return false;

        if (stationStopReached)
            return false;

        Vector3 currentPosition = transform.position;
        Vector3 targetPosition = stationStopTarget.position;

        distanceToStationStopTarget = Vector3.Distance(currentPosition, targetPosition);

        if (distanceToStationStopTarget <= stationStopRadius)
        {
            movementUntilStop = snapToStationStopTarget
                ? targetPosition - currentPosition
                : Vector3.zero;

            return true;
        }

        if (plannedMovement.sqrMagnitude <= 0.0000001f)
            return false;

        Vector3 movementDirection = plannedMovement.normalized;
        float movementDistance = plannedMovement.magnitude;

        Vector3 toTarget = targetPosition - currentPosition;

        float distanceAlongMovement = Vector3.Dot(toTarget, movementDirection);

        if (distanceAlongMovement < -stationStopRadius)
            return false;

        if (distanceAlongMovement > movementDistance + stationStopRadius)
            return false;

        Vector3 closestPointOnThisFrame =
            currentPosition +
            movementDirection * Mathf.Clamp(distanceAlongMovement, 0f, movementDistance);

        float closestDistanceToTarget =
            Vector3.Distance(closestPointOnThisFrame, targetPosition);

        if (closestDistanceToTarget > stationStopRadius)
            return false;

        if (snapToStationStopTarget)
        {
            movementUntilStop = targetPosition - currentPosition;
        }
        else
        {
            float stopDistance = Mathf.Clamp(distanceAlongMovement, 0f, movementDistance);
            movementUntilStop = movementDirection * stopDistance;
        }

        return true;
    }

    private void CompleteStationStop()
    {
        currentSpeed = 0f;
        currentInput = 0f;
        currentTargetSpeed = 0f;

        boostModeActive = false;
        waitingForJoystickForward = false;
        boostLaunchConfirmed = false;
        forwardHoldTimer = 0f;

        cruiseControlEnabled = false;
        cruiseSpeed = 0f;

        emergencyBraking = false;
        emergencyBrakeTimer = 0f;
        emergencyBrakeStartSpeed = 0f;

        stationStopReached = true;
        lastCountdownSoundStep = 0;

        SetCountdownLights(false, false, false);
    }

    public void ResetStationStop()
    {
        stationStopReached = false;
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
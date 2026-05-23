using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class XRJoystickButton : MonoBehaviour
{
    [Header("References")]
    public XRSimpleInteractable interactable;
    public Transform buttonVisual;

    [Header("Movement")]
    public Vector3 pressedLocalOffset = new Vector3(0f, 0f, -0.02f);
    public float pressSpeed = 20f;

    [Header("State")]
    public bool isPressed;

    private Vector3 startLocalPosition;

    private void Awake()
    {
        if (interactable == null)
            interactable = GetComponent<XRSimpleInteractable>();

        if (buttonVisual == null)
            buttonVisual = transform;

        startLocalPosition = buttonVisual.localPosition;
    }

    private void OnEnable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.AddListener(OnPressed);
            interactable.selectExited.AddListener(OnReleased);
        }
    }

    private void OnDisable()
    {
        if (interactable != null)
        {
            interactable.selectEntered.RemoveListener(OnPressed);
            interactable.selectExited.RemoveListener(OnReleased);
        }
    }

    private void Update()
    {
        Vector3 targetPosition = isPressed
            ? startLocalPosition + pressedLocalOffset
            : startLocalPosition;

        buttonVisual.localPosition = Vector3.Lerp(
            buttonVisual.localPosition,
            targetPosition,
            Time.deltaTime * pressSpeed
        );
    }

    private void OnPressed(SelectEnterEventArgs args)
    {
        isPressed = true;
        Debug.Log("Joystick button pressed");
    }

    private void OnReleased(SelectExitEventArgs args)
    {
        isPressed = false;
        Debug.Log("Joystick button released");
    }
}
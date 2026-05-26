using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class PhysicalPushButton : MonoBehaviour
{
    [Header("Button")]
    public Transform buttonVisual;

    [Tooltip("Wohin sich der Button sichtbar bewegt, wenn er gedrückt wird.")]
    public Vector3 pressedLocalOffset = new Vector3(0.03f, 0f, 0f);

    [Tooltip("Aus welcher lokalen Richtung die Hand den Button eindrückt.")]
    public Vector3 pressDetectionDirectionLocal = new Vector3(-1f, 0f, 0f);

    [Tooltip("Wie weit die Hand in Detection-Richtung gehen muss, um den Button komplett zu drücken.")]
    public float pressDistance = 0.03f;

    [Header("Hand Detection")]
    public LayerMask handLayerMask = ~0;

    [Header("Movement")]
    public float pressSpeed = 20f;
    public float returnSpeed = 12f;

    [Header("Press State")]
    [Range(0f, 1f)]
    public float pressThreshold = 0.8f;

    [Range(0f, 1f)]
    public float pressAmount;

    public bool isPressed;

    [Header("Events")]
    public UnityEvent onPressed;
    public UnityEvent onReleased;

    private Vector3 startLocalPosition;
    private readonly HashSet<Collider> touchingColliders = new HashSet<Collider>();

    private void Awake()
    {
        if (buttonVisual == null)
            buttonVisual = transform;

        startLocalPosition = buttonVisual.localPosition;
    }

    private void Update()
    {
        float targetPressAmount = CalculateTargetPressAmount();

        float speed = targetPressAmount > pressAmount ? pressSpeed : returnSpeed;

        pressAmount = Mathf.MoveTowards(
            pressAmount,
            targetPressAmount,
            speed * Time.deltaTime
        );

        buttonVisual.localPosition = startLocalPosition + pressedLocalOffset * pressAmount;

        bool currentlyPressed = pressAmount >= pressThreshold;

        if (currentlyPressed && !isPressed)
        {
            isPressed = true;
            onPressed.Invoke();
        }
        else if (!currentlyPressed && isPressed)
        {
            isPressed = false;
            onReleased.Invoke();
        }
    }

    private float CalculateTargetPressAmount()
    {
        if (pressDetectionDirectionLocal.sqrMagnitude <= 0.0001f)
            return 0f;

        if (pressDistance <= 0.0001f)
            return 0f;

        Vector3 detectionDirection = pressDetectionDirectionLocal.normalized;
        float strongestPress = 0f;

        touchingColliders.RemoveWhere(collider => collider == null || !collider.enabled);

        foreach (Collider handCollider in touchingColliders)
        {
            Vector3 handWorldPosition = handCollider.bounds.center;

            Vector3 handLocalPosition;

            if (buttonVisual.parent != null)
                handLocalPosition = buttonVisual.parent.InverseTransformPoint(handWorldPosition);
            else
                handLocalPosition = handWorldPosition;

            Vector3 localDelta = handLocalPosition - startLocalPosition;

            float distanceAlongPressDirection = Vector3.Dot(localDelta, detectionDirection);

            float amount = Mathf.Clamp01(distanceAlongPressDirection / pressDistance);

            if (amount > strongestPress)
                strongestPress = amount;
        }

        return strongestPress;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsInLayerMask(other.gameObject.layer, handLayerMask))
            return;

        touchingColliders.Add(other);
    }

    private void OnTriggerExit(Collider other)
    {
        touchingColliders.Remove(other);
    }

    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }
}
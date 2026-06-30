using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(31000)]
public class HyperloopRouteLoopTeleporter : MonoBehaviour
{
    [Header("References")]
    public HyperloopJoystickController hyperloopController;
    public Transform hyperloop;
    public Transform xrOrigin;

    [Header("Teleport Markers")]
    public Transform teleportFromPoint;
    public Transform teleportToPoint;

    [Header("Virtual Route")]
    public float totalRouteDistanceMeters = 42000f;
    public float travelledDistanceMeters;

    [Tooltip("Wenn die Reststrecke kleiner als dieser Wert ist, wird nicht mehr zurückteleportiert. Dann fährt der Hyperloop normal Richtung Station 2 weiter.")]
    public float finalExitDistanceMeters = 800f;

    [Header("Teleport Trigger")]
    public bool loopTeleportEnabled = true;

    [Tooltip("Wenn der Hyperloop näher als diese Distanz am Teleport From Point ist, wird teleportiert.")]
    public float teleportTriggerRadius = 3f;

    public float minimumSecondsBetweenTeleports = 0.5f;

    [Header("Objects With Rigidbody That Must Teleport Along")]
    public Rigidbody[] extraRigidbodiesToMove;

    [Header("Rigidbody")]
    public bool zeroRigidbodyVelocityOnTeleport = false;

    [Header("Debug")]
    public float remainingDistanceMeters;
    public float remainingDistanceKm;
    public float progress01;
    public int loopCount;
    public float distanceToTeleportPoint;
    public bool routeFinished;

    private float lastTeleportTime = -999f;

    private void LateUpdate()
    {
        if (hyperloopController == null || hyperloop == null || teleportFromPoint == null || teleportToPoint == null)
            return;

        UpdateVirtualDistance();

        if (!loopTeleportEnabled || routeFinished)
            return;

        if (remainingDistanceMeters <= finalExitDistanceMeters)
            return;

        if (Time.time - lastTeleportTime < minimumSecondsBetweenTeleports)
            return;

        distanceToTeleportPoint = Vector3.Distance(hyperloop.position, teleportFromPoint.position);

        if (distanceToTeleportPoint <= teleportTriggerRadius)
        {
            TeleportToTargetTunnel();
            loopCount++;
            lastTeleportTime = Time.time;
        }
    }

    private void UpdateVirtualDistance()
    {
        float speed = Mathf.Abs(hyperloopController.currentSpeed);

        if (!routeFinished)
            travelledDistanceMeters += speed * Time.deltaTime;

        if (travelledDistanceMeters >= totalRouteDistanceMeters)
        {
            travelledDistanceMeters = totalRouteDistanceMeters;
            routeFinished = true;
        }

        remainingDistanceMeters = Mathf.Max(0f, totalRouteDistanceMeters - travelledDistanceMeters);
        remainingDistanceKm = remainingDistanceMeters / 1000f;

        progress01 = totalRouteDistanceMeters > 0f
            ? travelledDistanceMeters / totalRouteDistanceMeters
            : 0f;

        progress01 = Mathf.Clamp01(progress01);
    }

    private void TeleportToTargetTunnel()
    {
        Vector3 delta = teleportToPoint.position - teleportFromPoint.position;

        if (delta.sqrMagnitude <= 0.000001f)
            return;

        List<Rigidbody> rigidbodiesToMove = GetRigidbodiesToMove();

        Vector3[] oldRigidbodyPositions = new Vector3[rigidbodiesToMove.Count];
        Quaternion[] oldRigidbodyRotations = new Quaternion[rigidbodiesToMove.Count];

        for (int i = 0; i < rigidbodiesToMove.Count; i++)
        {
            Rigidbody rb = rigidbodiesToMove[i];

            if (rb == null)
                continue;

            oldRigidbodyPositions[i] = rb.transform.position;
            oldRigidbodyRotations[i] = rb.transform.rotation;
        }

        hyperloop.position += delta;

        if (xrOrigin != null)
            xrOrigin.position += delta;

        for (int i = 0; i < rigidbodiesToMove.Count; i++)
        {
            Rigidbody rb = rigidbodiesToMove[i];

            if (rb == null)
                continue;

            Vector3 newPosition = oldRigidbodyPositions[i] + delta;

            rb.position = newPosition;
            rb.rotation = oldRigidbodyRotations[i];

            rb.transform.position = newPosition;
            rb.transform.rotation = oldRigidbodyRotations[i];

            if (zeroRigidbodyVelocityOnTeleport)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        Physics.SyncTransforms();
    }

    private List<Rigidbody> GetRigidbodiesToMove()
    {
        List<Rigidbody> result = new List<Rigidbody>();

        if (hyperloopController != null && hyperloopController.carriedRigidbodies != null)
        {
            for (int i = 0; i < hyperloopController.carriedRigidbodies.Length; i++)
            {
                Rigidbody rb = hyperloopController.carriedRigidbodies[i];

                if (rb != null && !result.Contains(rb))
                    result.Add(rb);
            }
        }

        if (extraRigidbodiesToMove != null)
        {
            for (int i = 0; i < extraRigidbodiesToMove.Length; i++)
            {
                Rigidbody rb = extraRigidbodiesToMove[i];

                if (rb != null && !result.Contains(rb))
                    result.Add(rb);
            }
        }

        return result;
    }

    public void ResetRoute()
    {
        travelledDistanceMeters = 0f;
        remainingDistanceMeters = totalRouteDistanceMeters;
        remainingDistanceKm = remainingDistanceMeters / 1000f;
        progress01 = 0f;
        loopCount = 0;
        routeFinished = false;
        lastTeleportTime = -999f;
    }
}
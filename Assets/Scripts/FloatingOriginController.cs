using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[DefaultExecutionOrder(30000)]
public class FloatingOriginController : MonoBehaviour
{
    [Header("Reference")]
    public Transform focusTransform;

    [Header("Objects To Shift")]
    public Transform[] rootsToShift;

    [Header("Protected Subtrees")]
    [Tooltip("Hier z.B. Bedienungsfläche eintragen, NICHT den Joystick_Pivot selbst.")]
    public Transform[] protectedSubtrees;

    [Tooltip("Floating Origin wartet, solange in einem Protected Subtree gerade ein XRGrabInteractable gegriffen wird.")]
    public bool waitWhileProtectedObjectIsGrabbed = true;

    [Header("Settings")]
    public float shiftThreshold = 1000f;
    public bool keepYAtZero = true;

    [Header("Debug")]
    public Vector3 totalWorldOffset;
    public float currentDistanceFromOrigin;
    public bool waitingBecauseProtectedObjectIsGrabbed;
    public string grabbedObjectName;
    public int lastProtectedCount;

    private void LateUpdate()
    {
        if (focusTransform == null)
            return;

        Vector3 focusPosition = focusTransform.position;

        if (keepYAtZero)
            focusPosition.y = 0f;

        currentDistanceFromOrigin = focusPosition.magnitude;

        if (currentDistanceFromOrigin < shiftThreshold)
        {
            waitingBecauseProtectedObjectIsGrabbed = false;
            grabbedObjectName = "";
            return;
        }

        if (waitWhileProtectedObjectIsGrabbed && IsAnyProtectedObjectGrabbed())
        {
            waitingBecauseProtectedObjectIsGrabbed = true;
            return;
        }

        waitingBecauseProtectedObjectIsGrabbed = false;
        grabbedObjectName = "";

        ShiftWorld(focusPosition);
    }

    private void ShiftWorld(Vector3 offset)
    {
        List<ProtectedState> protectedStates = SaveAndDetachProtectedSubtrees();

        for (int i = 0; i < rootsToShift.Length; i++)
        {
            Transform root = rootsToShift[i];

            if (root == null)
                continue;

            root.position -= offset;
        }

        RestoreProtectedSubtrees(protectedStates);

        totalWorldOffset += offset;

        Physics.SyncTransforms();
    }

    private List<ProtectedState> SaveAndDetachProtectedSubtrees()
    {
        List<ProtectedState> states = new List<ProtectedState>();

        if (protectedSubtrees == null)
        {
            lastProtectedCount = 0;
            return states;
        }

        for (int i = 0; i < protectedSubtrees.Length; i++)
        {
            Transform t = protectedSubtrees[i];

            if (t == null)
                continue;

            ProtectedState state = new ProtectedState
            {
                transform = t,
                parent = t.parent,
                siblingIndex = t.GetSiblingIndex(),
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale
            };

            states.Add(state);

            // Wichtig:
            // Kurz lösen, damit der Floating-Origin-Shift dieses Teil nicht direkt verzieht.
            t.SetParent(null, true);
        }

        lastProtectedCount = states.Count;
        return states;
    }

    private void RestoreProtectedSubtrees(List<ProtectedState> states)
    {
        for (int i = 0; i < states.Count; i++)
        {
            ProtectedState state = states[i];

            if (state.transform == null)
                continue;

            state.transform.SetParent(state.parent, false);
            state.transform.SetSiblingIndex(state.siblingIndex);

            state.transform.localPosition = state.localPosition;
            state.transform.localRotation = state.localRotation;
            state.transform.localScale = state.localScale;

            Rigidbody[] rigidbodies = state.transform.GetComponentsInChildren<Rigidbody>(true);

            for (int j = 0; j < rigidbodies.Length; j++)
            {
                Rigidbody rb = rigidbodies[j];

                if (rb == null)
                    continue;

                rb.position = rb.transform.position;
                rb.rotation = rb.transform.rotation;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }
    }

    private bool IsAnyProtectedObjectGrabbed()
    {
        grabbedObjectName = "";

        if (protectedSubtrees == null)
            return false;

        for (int i = 0; i < protectedSubtrees.Length; i++)
        {
            Transform subtree = protectedSubtrees[i];

            if (subtree == null)
                continue;

            XRGrabInteractable[] grabs = subtree.GetComponentsInChildren<XRGrabInteractable>(true);

            for (int j = 0; j < grabs.Length; j++)
            {
                XRGrabInteractable grab = grabs[j];

                if (grab == null)
                    continue;

                if (grab.isSelected)
                {
                    grabbedObjectName = grab.name;
                    return true;
                }
            }
        }

        return false;
    }

    public Vector3 GetRealWorldPosition(Vector3 currentUnityPosition)
    {
        return currentUnityPosition + totalWorldOffset;
    }

    private struct ProtectedState
    {
        public Transform transform;
        public Transform parent;
        public int siblingIndex;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }
}
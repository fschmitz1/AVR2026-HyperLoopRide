using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class SocketVisualSwapWhileMoving : MonoBehaviour
{
    [Header("Socket")]
    public XRSocketInteractor socketInteractor;
    public Transform socketAttachTransform;

    [Header("Hyperloop")]
    public HyperloopJoystickController hyperloopController;
    public float speedThreshold = 0.1f;

    [Header("Fake / Fixed Visual Object")]
    public GameObject fixedVisualObject;

    [Header("Real Object Handling")]
    public bool keepRealObjectAtSocket = true;
    public bool makeRealRigidbodyKinematicWhileSocketed = true;
    public bool disableRealRenderersWhileMoving = true;

    [Header("Debug")]
    public bool isSocketed;
    public bool isUsingFixedVisual;
    public Transform realObject;
    public Rigidbody realRigidbody;

    private Renderer[] realRenderers;
    private bool oldIsKinematic;
    private bool oldUseGravity;

    private void Awake()
    {
        if (socketInteractor == null)
            socketInteractor = GetComponent<XRSocketInteractor>();

        if (socketAttachTransform == null)
            socketAttachTransform = transform;

        if (fixedVisualObject != null)
            fixedVisualObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (socketInteractor == null)
            return;

        socketInteractor.selectEntered.AddListener(OnSocketed);
        socketInteractor.selectExited.AddListener(OnUnsocketed);
    }

    private void OnDisable()
    {
        if (socketInteractor == null)
            return;

        socketInteractor.selectEntered.RemoveListener(OnSocketed);
        socketInteractor.selectExited.RemoveListener(OnUnsocketed);
    }

    private void LateUpdate()
    {
        if (!isSocketed || realObject == null)
            return;

        bool hyperloopIsMoving = IsHyperloopMoving();

        if (hyperloopIsMoving && !isUsingFixedVisual)
        {
            UseFixedVisual();
        }
        else if (!hyperloopIsMoving && isUsingFixedVisual)
        {
            UseRealObject();
        }

        if (keepRealObjectAtSocket && socketAttachTransform != null)
        {
            realObject.SetPositionAndRotation(
                socketAttachTransform.position,
                socketAttachTransform.rotation
            );

            if (realRigidbody != null)
            {
                realRigidbody.position = socketAttachTransform.position;
                realRigidbody.rotation = socketAttachTransform.rotation;
                realRigidbody.linearVelocity = Vector3.zero;
                realRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private void OnSocketed(SelectEnterEventArgs args)
    {
        isSocketed = true;

        realObject = args.interactableObject.transform;
        realRigidbody = realObject.GetComponent<Rigidbody>();

        if (realRigidbody == null)
            realRigidbody = realObject.GetComponentInParent<Rigidbody>();

        if (realRigidbody != null)
        {
            realObject = realRigidbody.transform;

            oldIsKinematic = realRigidbody.isKinematic;
            oldUseGravity = realRigidbody.useGravity;

            if (makeRealRigidbodyKinematicWhileSocketed)
            {
                realRigidbody.isKinematic = true;
                realRigidbody.useGravity = false;
            }

            realRigidbody.linearVelocity = Vector3.zero;
            realRigidbody.angularVelocity = Vector3.zero;
        }

        realRenderers = realObject.GetComponentsInChildren<Renderer>(true);

        UseRealObject();
    }

    private void OnUnsocketed(SelectExitEventArgs args)
    {
        UseRealObject();

        if (realRigidbody != null)
        {
            realRigidbody.isKinematic = oldIsKinematic;
            realRigidbody.useGravity = oldUseGravity;
            realRigidbody.linearVelocity = Vector3.zero;
            realRigidbody.angularVelocity = Vector3.zero;
        }

        isSocketed = false;
        isUsingFixedVisual = false;

        realObject = null;
        realRigidbody = null;
        realRenderers = null;
    }

    private bool IsHyperloopMoving()
    {
        if (hyperloopController == null)
            return false;

        return Mathf.Abs(hyperloopController.currentSpeed) > speedThreshold;
    }

    private void UseFixedVisual()
    {
        isUsingFixedVisual = true;

        if (fixedVisualObject != null)
            fixedVisualObject.SetActive(true);

        if (disableRealRenderersWhileMoving)
            SetRealRenderersVisible(false);
    }

    private void UseRealObject()
    {
        isUsingFixedVisual = false;

        if (socketAttachTransform != null && realObject != null)
        {
            realObject.SetPositionAndRotation(
                socketAttachTransform.position,
                socketAttachTransform.rotation
            );
        }

        SetRealRenderersVisible(true);

        if (fixedVisualObject != null)
            fixedVisualObject.SetActive(false);
    }

    private void SetRealRenderersVisible(bool visible)
    {
        if (realRenderers == null)
            return;

        for (int i = 0; i < realRenderers.Length; i++)
        {
            if (realRenderers[i] != null)
                realRenderers[i].enabled = visible;
        }
    }
}
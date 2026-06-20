using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[RequireComponent(typeof(XRSocketInteractor))]
public class SocketTrigger : MonoBehaviour
{
    [Header("Socket")]
    [SerializeField] private XRSocketInteractor socketInteractor;

    [Header("Optional Filter")]
    [SerializeField] private GameObject requiredObject;
    [SerializeField] private string requiredTag;

    [Header("Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;

    [Header("Events")]
    [SerializeField] private UnityEvent onObjectPlaced;
    [SerializeField] private UnityEvent onObjectRemoved;

    private bool hasTriggered;

    private void Reset()
    {
        socketInteractor = GetComponent<XRSocketInteractor>();
    }

    private void Awake()
    {
        if (!socketInteractor)
        {
            socketInteractor = GetComponent<XRSocketInteractor>();
        }
    }

    private void OnEnable()
    {
        socketInteractor.selectEntered.AddListener(OnSocketObjectPlaced);
        socketInteractor.selectExited.AddListener(OnSocketObjectRemoved);
    }

    private void OnDisable()
    {
        socketInteractor.selectEntered.RemoveListener(OnSocketObjectPlaced);
        socketInteractor.selectExited.RemoveListener(OnSocketObjectRemoved);
    }

    private void OnSocketObjectPlaced(SelectEnterEventArgs args)
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        GameObject placedObject = args.interactableObject.transform.gameObject;

        if (!IsAllowedObject(placedObject))
            return;

        hasTriggered = true;

        Debug.Log($"{placedObject.name} wurde in Socket {name} abgelegt.");

        onObjectPlaced.Invoke();
    }

    private void OnSocketObjectRemoved(SelectExitEventArgs args)
    {
        GameObject removedObject = args.interactableObject.transform.gameObject;

        if (!IsAllowedObject(removedObject))
            return;

        Debug.Log($"{removedObject.name} wurde aus Socket {name} entfernt.");

        onObjectRemoved.Invoke();
    }

    private bool IsAllowedObject(GameObject obj)
    {
        if (requiredObject && obj != requiredObject)
            return false;

        if (!string.IsNullOrEmpty(requiredTag) && !obj.CompareTag(requiredTag))
            return false;

        return true;
    }
}
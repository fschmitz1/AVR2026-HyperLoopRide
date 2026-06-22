using UnityEngine;

public class SocketObjectSwitcher : MonoBehaviour
{
    [Header("Objects To Disable")]
    [SerializeField] private GameObject firstObjectToDisable;
    [SerializeField] private GameObject secondObjectToDisable;

    [Header("Object To Enable")]
    [SerializeField] private GameObject objectToEnable;

    [Header("Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;
    [SerializeField] private bool copyTransformFromFirstDisabledToEnabled = false;

    private bool hasTriggered;

    public void SwitchObjects()
    {
        if (triggerOnlyOnce && hasTriggered)
            return;

        hasTriggered = true;

        if (objectToEnable && firstObjectToDisable && copyTransformFromFirstDisabledToEnabled)
        {
            objectToEnable.transform.SetPositionAndRotation(
                firstObjectToDisable.transform.position,
                firstObjectToDisable.transform.rotation
            );

            objectToEnable.transform.localScale = firstObjectToDisable.transform.localScale;
        }

        if (objectToEnable)
        {
            objectToEnable.SetActive(true);
        }

        if (firstObjectToDisable)
        {
            firstObjectToDisable.SetActive(false);
        }

        if (secondObjectToDisable)
        {
            secondObjectToDisable.SetActive(false);
        }

        Debug.Log("SocketObjectSwitcher: Zwei Objekte wurden deaktiviert und ein Objekt wurde aktiviert.");
    }
}
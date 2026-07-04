using UnityEngine;

public class TubePassTrigger : MonoBehaviour
{
    [Header("Sound Player")]
    public TubePassSoundPlayer soundPlayer;

    [Header("Trigger Filter")]
    public Transform hyperloopRoot;

    [Header("Debug")]
    public bool wasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (hyperloopRoot != null)
        {
            if (!other.transform.IsChildOf(hyperloopRoot) && other.transform != hyperloopRoot)
                return;
        }

        if (soundPlayer != null)
        {
            soundPlayer.PlayPassSound();
            wasTriggered = true;
        }
    }
}
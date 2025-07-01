using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class AreaTrigger : MonoBehaviour
{
    [SerializeField] private string m_areaName;
    private const string kPlayerTag = "Player";

    private bool m_hasEntered = false;

    /*------------------------------------------------------------------------
    | --- OnTriggerEnter: Called when the player enters the trigger area --- |
    ------------------------------------------------------------------------*/
    private void OnTriggerEnter(Collider other)
    {
        if (!m_hasEntered && other.CompareTag(kPlayerTag))
        {
            Debug.Log($"Entering area: {m_areaName}");
            m_hasEntered = true;
        }
    }

    /*----------------------------------------------------------------------
    | --- OnTriggerExit: Called when the player exits the trigger area --- |
    ----------------------------------------------------------------------*/
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(kPlayerTag))
        {
            m_hasEntered = false;
        }
    }
}
using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    public class PreservedObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject m_preservedObject;

        static bool m_hasSpawned = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            if (!m_hasSpawned)
            {
                SpawnPreservedObjects();
                m_hasSpawned = true;
            }
        }

        /*----------------------------------------------------------------------------------------------------------------------------------
        | --- SpawnPreservedObjects: Instantiate a Collection of Preserved GameObjects that does not get Destroyed upon Loading Scenes --- |
        ----------------------------------------------------------------------------------------------------------------------------------*/
        private void SpawnPreservedObjects()
        {
            GameObject gameObject = Instantiate(m_preservedObject);
            DontDestroyOnLoad(gameObject);
        }
    }
}
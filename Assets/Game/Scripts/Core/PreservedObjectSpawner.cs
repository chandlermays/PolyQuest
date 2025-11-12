using UnityEngine;
//---------------------------------

namespace PolyQuest.Core
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Spawns and preserves a GameObject across scene loads, ensuring it is instantiated once. *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Instantiates a specified GameObject if it has not already been spawned.                *
     *      - Marks the spawned object to persist between scene loads using DontDestroyOnLoad.       *
     *      - Prevents duplicate preserved objects by tracking spawn state with a static flag.       *
     * --------------------------------------------------------------------------------------------- */
    public class PreservedObjectSpawner : MonoBehaviour
    {
        [SerializeField] private GameObject m_preservedObject;

        private static bool m_hasSpawned = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_preservedObject, nameof(m_preservedObject));

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
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;
//---------------------------------

namespace PolyQuest
{
    public class Portal : MonoBehaviour
    {
        [Header("Portal Settings")]
        [SerializeField] private SceneField m_sceneField;
        [SerializeField] private Transform m_spawnPoint;

        private static readonly List<Portal> s_portals = new();

        private GameObject m_player;
        private const string kPlayerTag = "Player";

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            s_portals.Add(this);
        }

        /*--------------------------------------------------------
        | --- OnDisable: Called when the object is destroyed --- |
        --------------------------------------------------------*/
        private void OnDestroy()
        {
            s_portals.Remove(this);
        }

        /*------------------------------------------------------------------------------
        | --- OnTriggerEnter: Called when the Collider 'other' enters this trigger --- |
        ------------------------------------------------------------------------------*/
        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(kPlayerTag))
            {
                m_player = other.gameObject;
                StartCoroutine(SceneTransition());
            }
        }

        /*-------------------------------------------------------------
        | --- SceneTransition: Coroutine to Transition the Scenes --- |
        -------------------------------------------------------------*/
        private IEnumerator SceneTransition()
        {
            DontDestroyOnLoad(gameObject);

            // These are amongst the "PerservedObjects" in the game that get created upon loading scenes
            TransitionFade fade = FindObjectOfType<TransitionFade>();
            SaveLoadController saveLoadController = FindObjectOfType<SaveLoadController>();

            PlayerController playerController = m_player.GetComponent<PlayerController>();
            playerController.enabled = false;

            yield return fade.FadeOut();

            saveLoadController.Save();

            yield return SceneManager.LoadSceneAsync(m_sceneField.SceneName);

            // Cannot use cached player, have to find it again in the new scene
            PlayerController newPlayerController = GameObject.FindWithTag(kPlayerTag).GetComponent<PlayerController>();
            newPlayerController.enabled = false;

            saveLoadController.Load();

            Portal destination = GetDestination();
            UpdatePlayer(destination);

            saveLoadController.Save();

            yield return fade.Wait();
            yield return fade.FadeIn();

            newPlayerController.enabled = true;
            Destroy(gameObject);
        }

        /*--------------------------------------------------------------------
        | --- GetDestination: Returns the Portal the Player is headed to --- |
        --------------------------------------------------------------------*/
        private Portal GetDestination()
        {
            foreach (Portal portal in s_portals)
            {
                if (portal != this)
                {
                    return portal;
                }
            }

            return null;
        }

        /*-------------------------------------------------------------------------------------
        | --- UpdatePlayer: Update the Player's Position to the Destination's Spawn Point --- |
        -------------------------------------------------------------------------------------*/
        private void UpdatePlayer(Portal destination)
        {
            // Retrieve the player object again in the new scene
            m_player = GameObject.FindWithTag(kPlayerTag);
            if (m_player == null)
            {
                Debug.LogError("Player object not found in the new scene!");
                return;
            }

            NavMeshAgent agent = m_player.GetComponent<NavMeshAgent>();
            agent.enabled = false;
            m_player.transform.SetPositionAndRotation(destination.m_spawnPoint.position, destination.m_spawnPoint.rotation);
            agent.enabled = true;
        }
    }
}
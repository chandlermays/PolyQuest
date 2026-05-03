/*---------------------------
File: Portal.cs
Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.Saving;
using PolyQuest.PCG;

namespace PolyQuest.SceneManagement
{
    /* --------------------------------------------------------------------------------------------
     * Role: Handles scene transitions via in-game portals, managing player transfer and spawn.    *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Detects when the player enters a portal trigger.                                     *
     *      - Initiates scene transitions and manages fade effects during travel.                  *
     *      - Coordinates saving and loading game state before and after scene changes.            *
     *      - Finds the corresponding destination portal and updates the player's position.        *
     *      - Ensures player control and navigation are properly managed during transitions.       *
     * ------------------------------------------------------------------------------------------- */

    public enum PortalID
    {
        A,
        B,
        C,
        D,
        E
    }

    public class Portal : MonoBehaviour
    {
        [Header("Portal Settings")]
        [SerializeField] private SceneField m_sceneField;
        [SerializeField] private Transform m_spawnPoint;
        [SerializeField] private PortalID m_portalID;

        private static readonly List<Portal> s_portals = new();

        private GameObject m_player;
        private const string kPlayerTag = "Player";

        public string SceneName => m_sceneField.SceneName;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_sceneField, nameof(m_sceneField));
            Utilities.CheckForNull(m_spawnPoint, nameof(m_spawnPoint));
        }

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
            // DontDestroyOnLoad only works for root GameObjects or components on root GameObjects
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            PlayerController playerController = m_player.GetComponent<PlayerController>();
            playerController.enabled = false;

            yield return TransitionFade.Instance.FadeOut();

            // Checkpoint before scene transition
            SaveManager.Instance.Save();

            yield return SceneManager.LoadSceneAsync(m_sceneField.SceneName);

            // Cannot use cached player, have to find it again in the new scene
            PlayerController newPlayerController = GameObject.FindWithTag(kPlayerTag).GetComponent<PlayerController>();
            newPlayerController.enabled = false;

            SaveManager.Instance.Load();

            // If the scene has a LevelBuilder, wait until the level is fully generated before trying to find the destination portal
            LevelBuilder levelBuilder = FindFirstObjectByType<LevelBuilder>();
            if (levelBuilder != null)
            {
                yield return WaitForLevelReady(levelBuilder);
            }

            Portal destination = GetDestination();
#if UNITY_EDITOR
            if (destination == null)
            {
                Debug.LogError($"Portal: No destination found matching PortalID '{m_portalID}' in scene '{m_sceneField.SceneName}'.");
                Destroy(gameObject);
                yield break;
            }
#endif
            UpdatePlayer(destination);
            
            // Checkpoint after scene transition
            SaveManager.Instance.Save();

            yield return TransitionFade.Instance.Wait();
            yield return TransitionFade.Instance.FadeIn();

            newPlayerController.enabled = true;
            Destroy(gameObject);
        }

        /*----------------------------------------------------------------------------------------------------
        | --- WaitForLevelReady: Coroutine to wait for the LevelBuilder to complete generating the level --- | 
        ----------------------------------------------------------------------------------------------------*/
        private IEnumerator WaitForLevelReady(LevelBuilder levelBuilder)
        {
            bool levelReady = false;
            levelBuilder.OnLevelGenerated += () => levelReady = true;

            if (!levelBuilder.IsGenerated)
            {
                yield return new WaitUntil(() => levelReady);
            }
        }

        /*--------------------------------------------------------------------
        | --- GetDestination: Returns the Portal the Player is headed to --- |
        --------------------------------------------------------------------*/
        private Portal GetDestination()
        {
            Scene activeScene = SceneManager.GetActiveScene();

            foreach (Portal portal in s_portals)
            {
                // skip self cannot be destination
                if (portal == this)
                    continue;

                // filter portals in the active scene; again, source cannot be destination
                if (portal.gameObject.scene != activeScene)
                    continue;

                // find the connecting portal with the same portalID
                if (portal.m_portalID == m_portalID)
                    return portal;
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
#if UNITY_EDITOR
            if (m_player == null)
            {
                Debug.LogError("Player object not found in the new scene!");
                return;
            }
#endif
            NavMeshAgent agent = m_player.GetComponent<NavMeshAgent>();
            agent.enabled = false;
            m_player.transform.SetPositionAndRotation(destination.m_spawnPoint.position, destination.m_spawnPoint.rotation);
            agent.enabled = true;
        }
    }
}
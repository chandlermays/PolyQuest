/*---------------------------
File: RespawnComponent.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
using System.Collections;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Components
{
    public class RespawnComponent : EntityComponent
    {
        [SerializeField] private float m_respawnDelay = 1.5f;

        private HealthComponent m_healthComponent;

        private bool m_isRespawning = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        protected override void Awake()
        {
            base.Awake();

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_healthComponent.OnDeath += Respawn;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_healthComponent.OnDeath -= Respawn;
        }

        /*-----------------------------------------------
        | --- Respawn: Called to respawn the entity --- |
        -----------------------------------------------*/
        private void Respawn()
        {
            if (m_isRespawning)
                return;

            m_isRespawning = true;
            StartCoroutine(StartRespawn());
        }

        /*---------------------------------------------------------------
        | --- StartRespawn: Coroutine to handle the respawn process --- |
        ---------------------------------------------------------------*/
        private IEnumerator StartRespawn()
        {
            if (m_respawnDelay > 0f)
                yield return new WaitForSeconds(m_respawnDelay);

            SaveManager.Instance.RespawnCheckpoint();
            m_isRespawning = false;
        }
    }
}
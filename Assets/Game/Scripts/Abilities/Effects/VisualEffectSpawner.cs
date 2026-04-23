/*---------------------------
File: VisualEffectSpawner.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Effects/New Visual Effect Spawner", fileName = "New Visual Effect Spawner")]
    public class VisualEffectSpawner : EffectStrategy
    {
        [SerializeField] private GameObject m_spawnedPrefab;
        [SerializeField] private float m_duration = 3f;

        /*------------------------------------------------------------------- 
        | --- StartEffect: Spawns the visual effect at the target point --- |
        -------------------------------------------------------------------*/
        public override void StartEffect(AbilityConfig config, Action onComplete)
        {
            config.StartCoroutine(Effect(config, onComplete));
        }

        /*------------------------------------------------------------------------- 
        | --- Effect: Coroutine that handles the effect spawning and duration --- |
        -------------------------------------------------------------------------*/
        private IEnumerator Effect(AbilityConfig config, Action onComplete)
        {
            GameObject instance = Instantiate(m_spawnedPrefab, config.TargetPoint, m_spawnedPrefab.transform.rotation);
            if (m_duration > 0)
            {
                yield return new WaitForSeconds(m_duration);
                Destroy(instance);
            }
            onComplete?.Invoke();
        }
    }
}
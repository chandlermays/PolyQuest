using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.UI.Core;
using PolyQuest.Input;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Targeting/New Delayed Click Strategy", fileName = "New Delayed Click Strategy")]
    public class DelayedClick : TargetingStrategy
    {
        [SerializeField] private LayerMask m_groundLayer;
        [SerializeField] private float m_areaOfEffectRadius;
        [SerializeField] private GameObject m_areaRadiusPrefab;

        private GameObject m_areaRadiusInstance;
        private const int kMaxDistance = 1000;

        /*------------------------------------------------------------------------- 
        | --- StartTargeting: Initiates the targeting process for the ability --- |
        -------------------------------------------------------------------------*/
        public override void StartTargeting(AbilityConfig config, Action onComplete)
        {
            PlayerController playerController = config.User.GetComponent<PlayerController>();
            playerController.StartCoroutine(Targeting(config, playerController, onComplete));
        }

        /*----------------------------------------------------------------- 
        | --- Targeting: Coroutine that manages the targeting process --- |
        -----------------------------------------------------------------*/
        private IEnumerator Targeting(AbilityConfig config, PlayerController playerController, Action onComplete)
        {
            playerController.enabled = false;

            if (m_areaRadiusInstance == null)
            {
                m_areaRadiusInstance = Instantiate(m_areaRadiusPrefab, Vector3.zero, Quaternion.identity);
            }
            else
            {
                m_areaRadiusInstance.SetActive(true);
            }

            m_areaRadiusInstance.transform.localScale = new Vector3(m_areaOfEffectRadius, 1, m_areaOfEffectRadius);

            playerController.SetCursor(CursorSettings.CursorType.kTargeting);

            PolyQuestInputActions inputActions = InputManager.Instance.InputActions;

            while (!config.IsCancelled)
            {
                if (inputActions.Gameplay.Cancel.WasPressedThisFrame())
                {
                    config.Cancel();
                    playerController.SetCursor(CursorSettings.CursorType.kNone);
                    break;
                }

                if (Physics.Raycast(playerController.GetCursorRay(), out RaycastHit raycastHit, kMaxDistance, m_groundLayer))
                {
                    m_areaRadiusInstance.transform.position = raycastHit.point;

                    if (inputActions.Gameplay.Interact.WasPressedThisFrame())
                    {
                        // Wait until the mouse button is released
                        while (inputActions.Gameplay.Interact.IsPressed())
                        {
                            if (inputActions.Gameplay.Cancel.WasPressedThisFrame())
                            {
                                config.Cancel();
                                playerController.SetCursor(CursorSettings.CursorType.kNone);
                                break;
                            }
                            yield return null;
                        }
                        if (config.IsCancelled)
                            break;

                        config.TargetPoint = raycastHit.point;
                        config.Targets = CollectTargetsInRadius(raycastHit.point);
                        break;
                    }
                }
                yield return null;
            }

            m_areaRadiusInstance.SetActive(false);
            playerController.enabled = true;
            onComplete?.Invoke();
        }

        /*-------------------------------------------------------------------------------------- 
        | --- CollectTargetsInRadius: Gathers all targets within the area of effect radius --- |
        --------------------------------------------------------------------------------------*/
        private IEnumerable<GameObject> CollectTargetsInRadius(Vector3 point)
        {
            RaycastHit[] hits = Physics.SphereCastAll(point, m_areaOfEffectRadius, Vector3.up, 0);
            foreach (var hit in hits)
            {
                yield return hit.collider.gameObject;
            }
        }
    }
}
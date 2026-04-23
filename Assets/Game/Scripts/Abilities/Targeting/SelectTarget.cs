/*---------------------------
File: SelectTarget.cs
Author: Chandler Mays
----------------------------*/
using System;
using System.Collections;
using UnityEngine;
//---------------------------------
using PolyQuest.Player;
using PolyQuest.Input;

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Targeting/New Select Target Strategy", fileName = "New Select Target Strategy")]
    public class SelectTarget : TargetingStrategy
    {
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
            PolyQuestInputActions inputActions = InputManager.Instance.InputActions;

            playerController.BeginTargeting(
                onTargetSelected: (target, point) =>
                {
                    config.SetSingleTarget(target);
                    config.TargetPoint = point;
                },
                onCancelled: () =>
                {
                    config.Cancel();
                }
            );

            while (!config.IsCancelled)
            {
                if (inputActions.Gameplay.Cancel.WasPressedThisFrame())
                {
                    config.Cancel();
                    break;
                }

                if (config.TargetPoint != Vector3.zero)
                    break;

                yield return null;
            }

            playerController.EndTargeting();
            onComplete?.Invoke();
        }
    }
}
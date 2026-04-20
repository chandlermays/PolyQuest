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

// NOTE: The communication between SelectTarget and PlayerController is not working very well.
// You can select an enemy as the target and the ability will be cast on them, but in the process
// of targeting, the player can still highlight other entities with the cursor, which can lead to confusion.
// A better approach might be to have the PlayerController enter a "targeting mode" where it only highlights
// valid targets for the ability, and ignores other entities. This would require some refactoring of the PlayerController
// to support this mode, and the SelectTarget strategy would need to communicate with the PlayerController to enable it.
// While on the subject, it's possible that PlayerController should be divided into multiple components to make it easier.

// April 20th: At the time of re-reading this comment, I believe I've already taken care of this concern. Check it and remove if so.

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
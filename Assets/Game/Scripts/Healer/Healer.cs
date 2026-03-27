using PolyQuest.Abilities;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;
using PolyQuest.Input;
using PolyQuest.Player;
using PolyQuest.UI.Core;
using UnityEngine;

namespace PolyQuest.Healing
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Marks an NPC as a Healer and handles initiating healing interactions via raycast.       *
     *                                                                                               *
     * Responsibilities:                                                                             *
     *      - Implements IRaycastable to allow the player to interact with the NPC via raycast.      *
     *      - Sets the cursor to the heal icon when hovered.                                         *
     *      - Directs the player's HealerInteractor to approach and receive healing.                 *
     * --------------------------------------------------------------------------------------------- */
    public class Healer : MonoBehaviour, IRaycastable
    {
        [SerializeField] private EffectStrategy m_healEffect;
        private Outline m_outline;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_outline = GetComponent<Outline>();
            Utilities.CheckForNull(m_outline, nameof(m_outline));
        }

        /*------------------------------------------------------------------
        | --- HealInteractor: Fully replenishes the interactor's health --- |
        ------------------------------------------------------------------*/
        public void Heal(HealerInteractor healerInteractor)
        {
            GameObject player = healerInteractor.gameObject;

            healerInteractor.GetComponent<HealthComponent>().FullyReplenishHealth();

            if (m_healEffect != null)
            {
                AbilityConfig config = new(player)
                {
                    TargetPoint = player.transform.position
                };
                config.SetSingleTarget(player);
                m_healEffect.StartEffect(config, null);
            }
        }

        /*------------------------------------------------------------
        | --- GetCursorType: Returns the Cursor Type for Healing --- |
        ------------------------------------------------------------*/
        public CursorSettings.CursorType GetCursorType()
        {
            return CursorSettings.CursorType.kHealer;
        }

        /*--------------------------------------------------------------------------
        | --- HandleRaycast: The Behavior of the Raycast for Initiating Healing --- |
        --------------------------------------------------------------------------*/
        public bool HandleRaycast(PlayerController playerController)
        {
            if (InputManager.Instance.InputActions.Gameplay.Interact.WasPressedThisFrame())
            {
                playerController.GetComponent<HealerInteractor>().SetTargetHealer(this);
            }

            return true;
        }

        public void ToggleHighlight(bool highlight)
        {
            m_outline.enabled = highlight;
        }
    }
}
using System;
using UnityEngine;
//---------------------------------
using PolyQuest.Components;
using PolyQuest.Core;

namespace PolyQuest.Shops
{
    public class ShopInteractor : MonoBehaviour, IAction
    {
        private Shop m_activeShop;
        private Shop m_targetShop;

        private const float kProximityThreshold = 3.0f;

        private MovementComponent m_movementComponent;
        private ActionManager m_actionManager;

        public Shop ActiveShop => m_activeShop;

        public event Action OnActiveShopChanged;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_movementComponent = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movementComponent, nameof(m_movementComponent));

            m_actionManager = GetComponent<ActionManager>();
            Utilities.CheckForNull(m_actionManager, nameof(m_actionManager));
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (m_targetShop == null)
                return;

            if (Vector3.Distance(m_targetShop.transform.position, transform.position) > kProximityThreshold)
            {
                m_movementComponent.MoveTo(m_targetShop.transform.position);
            }
            else
            {
                m_movementComponent.Stop();
                SetActiveShop(m_targetShop);
                m_targetShop = null;
            }
        }

        /*-------------------------------------------------------------------------------------------
        | --- SetActiveShop: Sets the active shop that the player is currently interacting with --- |
        -------------------------------------------------------------------------------------------*/
        public void SetActiveShop(Shop shop)
        {
            if (m_activeShop != null)
            {
                m_activeShop.SetShopInteractor(null);
            }

            m_activeShop = shop;

            if (m_activeShop != null)
            {
                m_activeShop.SetShopInteractor(this);
            }

            OnActiveShopChanged?.Invoke();
        }

        /*-----------------------------------------------------------------------------
        | --- SetTargetShop: Sets the target shop for the player to interact with --- |
        -----------------------------------------------------------------------------*/
        public void SetTargetShop(Shop shop)
        {
            m_targetShop = shop;
            m_actionManager.StartAction(this);
        }

        /*------------------------------------------------------------------------
        | --- Cancel: Cancels the current shop interaction or movement to it --- |
        ------------------------------------------------------------------------*/
        public void Cancel()
        {
            m_targetShop = null;
        }
    }
}
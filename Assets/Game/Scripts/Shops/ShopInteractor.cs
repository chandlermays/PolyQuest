using System;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Shops
{
    public class ShopInteractor : MonoBehaviour
    {
        private Shop m_activeShop;

        public Shop ActiveShop => m_activeShop;

        public event Action OnActiveShopChanged;

        /*----------------------------------------------------------------------
        | --- SetActiveShop: Sets the currently active shop for the player --- |
        ----------------------------------------------------------------------*/
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
    }
}
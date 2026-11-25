/*---------------------------
| File: CurrencyItem.cs
| Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Pickups
{
    [CreateAssetMenu(menuName = "PolyQuest/Items/Currency Item", fileName = "New Currency Item")]
    public class CurrencyItem : InventoryItem
    {
        /*-------------------------------------------------------------------------------------
        | --- OnPickup: Currency goes to the wallet, so it's always taken, even when full --- |
        -------------------------------------------------------------------------------------*/
        public override bool OnPickup(Inventory inventory, int quantity)
        {
            Wallet wallet = inventory.GetComponent<Wallet>();
            wallet.UpdateSilver(quantity);
            return true;
        }
    }
}
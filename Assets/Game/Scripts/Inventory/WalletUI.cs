using TMPro;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Inventories
{
    public class WalletUI : MonoBehaviour
    {
        [SerializeField] private Wallet m_wallet;
        [SerializeField] private TextMeshProUGUI m_amountText;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_wallet, nameof(m_wallet));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_wallet.OnWalletUpdated += RefreshUI;
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            m_wallet.OnWalletUpdated -= RefreshUI;
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            RefreshUI();
        }

        /*-------------------------------------------------------------------------------
        | --- RefreshUI: Updates the wallet UI to reflect the current silver amount --- |
        -------------------------------------------------------------------------------*/
        private void RefreshUI()
        {
            m_amountText.text = m_wallet.CurrentSilver.ToString();
        }
    }
}
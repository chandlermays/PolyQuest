using TMPro;
using UnityEngine;

namespace PolyQuest.UI
{
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] private GameObject m_labelRoot;
        [SerializeField] private TextMeshProUGUI m_labelText;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_labelRoot, nameof(m_labelRoot));
            Utilities.CheckForNull(m_labelText, nameof(m_labelText));
            Hide();
        }

        /*----------------------------------------------------
        | --- SetLabel: Sets the text of the world label --- |
        ----------------------------------------------------*/
        public void SetLabel(string text)
        {
            m_labelText.text = text;
        }

        /*------------------------------------------------------------
        | --- Toggle: Controls the visibility of the world label --- |
        ------------------------------------------------------------*/
        public void Toggle(bool visible)
        {
            if (visible)
            {
                Show();
            }
            else
            {
                Hide();
            }
        }

        /*---------------------------------------------------------------
        | --- Show/Hide: Controls the visibility of the world label --- |
        ---------------------------------------------------------------*/
        private void Show() => m_labelRoot.SetActive(true);
        private void Hide() => m_labelRoot.SetActive(false);
    }
}
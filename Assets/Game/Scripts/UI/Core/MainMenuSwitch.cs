using UnityEngine;
//---------------------------------

namespace PolyQuest.UI.Core
{
    public class MainMenuSwitch : MonoBehaviour
    {
        [SerializeField] private GameObject m_initialMenu;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            SwitchTo(m_initialMenu);
        }

        /*-----------------------------------------------------------------
        | --- SwitchTo: Switches the active menu to the specified one --- |
        -----------------------------------------------------------------*/
        public void SwitchTo(GameObject menu)
        {
            if (menu.transform.parent != transform)
                return;

            foreach (Transform child in transform)
            {
                child.gameObject.SetActive(child.gameObject == menu);
            }
        }
    }
}
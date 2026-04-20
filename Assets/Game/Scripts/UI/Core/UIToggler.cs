/*---------------------------
File: UIToggler.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
using UnityEngine.InputSystem;
//---------------------------------

namespace PolyQuest.UI.Core
{
    /* -------------------------------------------------------------------------------
     * Role: Toggles the visibility of a UI element in response to a key press.       *
     *                                                                                *
     * Responsibilities:                                                              *
     *      - Listens for a specified key input to toggle a UI GameObject on or off,  *
     *        such as opening an Inventory menu, Pause menu, or Quest tab.            *
     * ------------------------------------------------------------------------------ */
    public class UIToggler : MonoBehaviour
    {
        [SerializeField] private GameObject m_uiPrefab;
        [SerializeField] private InputActionReference m_toggleAction;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_uiPrefab, nameof(m_uiPrefab));
            Utilities.CheckForNull(m_toggleAction, nameof(m_toggleAction));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_toggleAction.action.Enable();
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_toggleAction.action.Disable();
        }

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_uiPrefab.SetActive(false);
        }

        /*---------------------------------------
        | --- Update: Called once per frame --- |
        ---------------------------------------*/
        private void Update()
        {
            if (m_toggleAction.action.WasPressedThisFrame())
            {
                ToggleUI();
            }
        }

        /*--------------------------------------------
        | --- ToggleUI: Toggle the UI visibility --- |
        --------------------------------------------*/
        public void ToggleUI()
        {
            m_uiPrefab.SetActive(!m_uiPrefab.activeSelf);
        }
    }
}
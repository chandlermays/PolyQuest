using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.UI.Core
{
    /* --------------------------------------------------------------------------------------------
     * Role: Provides a dialog interface for splitting item stacks by entering a numeric value.   *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Displays a dialog for numeric input to split item stacks.                            *
     *      - Validates user input to ensure it's within valid range.                              *
     *      - Invokes callbacks when split is confirmed or cancelled.                              *
     *      - Can be shown/hidden dynamically during gameplay.                                     *
     * ------------------------------------------------------------------------------------------- */
    public class ItemSplitDialog : MonoBehaviour
    {
        [SerializeField] private GameObject m_dialogPanel;
        [SerializeField] private TMP_InputField m_quantityInputField;
        [SerializeField] private TextMeshProUGUI m_titleText;
        [SerializeField] private Button m_confirmButton;
        [SerializeField] private Button m_cancelButton;

        private int m_maxQuantity;
        private Action<int> m_onConfirm;
        private Action m_onCancel;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.AddListener(OnConfirmClicked);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.AddListener(OnCancelClicked);
            }

            if (m_quantityInputField != null)
            {
                m_quantityInputField.onValueChanged.AddListener(OnInputValueChanged);
            }

            Hide();
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            if (m_confirmButton != null)
            {
                m_confirmButton.onClick.RemoveListener(OnConfirmClicked);
            }

            if (m_cancelButton != null)
            {
                m_cancelButton.onClick.RemoveListener(OnCancelClicked);
            }

            if (m_quantityInputField != null)
            {
                m_quantityInputField.onValueChanged.RemoveListener(OnInputValueChanged);
            }
        }

        /*------------------------------------------------------------------
        | --- Show: Display the dialog with specified max quantity --- |
        ------------------------------------------------------------------*/
        public void Show(int maxQuantity, Action<int> onConfirm, Action onCancel = null)
        {
            m_maxQuantity = maxQuantity;
            m_onConfirm = onConfirm;
            m_onCancel = onCancel;

            if (m_titleText != null)
            {
                m_titleText.text = $"Split Stack (Max: {maxQuantity})";
            }

            if (m_quantityInputField != null)
            {
                // Default to half the stack
                int defaultValue = Mathf.Max(1, maxQuantity / 2);
                m_quantityInputField.text = defaultValue.ToString();
                m_quantityInputField.ActivateInputField();
                m_quantityInputField.Select();
            }

            if (m_dialogPanel != null)
            {
                m_dialogPanel.SetActive(true);
            }
        }

        /*------------------------------------------------------------------
        | --- Hide: Hide the dialog --- |
        ------------------------------------------------------------------*/
        public void Hide()
        {
            if (m_dialogPanel != null)
            {
                m_dialogPanel.SetActive(false);
            }

            m_onConfirm = null;
            m_onCancel = null;
        }

        /*------------------------------------------------------------------
        | --- OnConfirmClicked: Handle confirm button click --- |
        ------------------------------------------------------------------*/
        private void OnConfirmClicked()
        {
            if (m_quantityInputField != null && int.TryParse(m_quantityInputField.text, out int quantity))
            {
                // Clamp the quantity to valid range
                quantity = Mathf.Clamp(quantity, 1, m_maxQuantity);

                m_onConfirm?.Invoke(quantity);
            }

            Hide();
        }

        /*------------------------------------------------------------------
        | --- OnCancelClicked: Handle cancel button click --- |
        ------------------------------------------------------------------*/
        private void OnCancelClicked()
        {
            m_onCancel?.Invoke();
            Hide();
        }

        /*------------------------------------------------------------------
        | --- OnInputValueChanged: Validate input as user types --- |
        ------------------------------------------------------------------*/
        private void OnInputValueChanged(string value)
        {
            if (m_confirmButton == null)
                return;

            // Enable/disable confirm button based on valid input
            bool isValid = int.TryParse(value, out int quantity) && quantity > 0 && quantity <= m_maxQuantity;
            m_confirmButton.interactable = isValid;
        }
    }
}

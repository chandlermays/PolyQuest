using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.Attributes
{
    public class AttributeUI : MonoBehaviour
    {
        [SerializeField] private Attributes m_attributes;
        [SerializeField] private Attribute m_attributeType;
        [SerializeField] private TextMeshProUGUI m_valueText;
        [SerializeField] private Button m_minusButton;
        [SerializeField] private Button m_plusButton;

        private int m_value;

        private void Start()
        {
            m_minusButton.onClick.AddListener(() => AdjustValue(-1));
            m_plusButton.onClick.AddListener(() => AdjustValue(1));
        }

        private void Update()
        {
            m_minusButton.interactable = m_attributes.CanAssignPoints(m_attributeType, -1);
            m_plusButton.interactable = m_attributes.CanAssignPoints(m_attributeType, 1);

            m_valueText.text = m_attributes.GetPoints(m_attributeType).ToString();
        }

        public void AdjustValue(int amount)
        {
            m_attributes.AssignPoints(m_attributeType, amount);
        }
    }
}
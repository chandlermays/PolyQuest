using TMPro;
using UnityEngine;
using UnityEngine.UI;
//---------------------------------

namespace PolyQuest.Attributes
{
    public class AttributeUI : MonoBehaviour
    {
        [SerializeField] private Attributes m_attributes;
        [SerializeField] private TextMeshProUGUI m_availablePoints;
        [SerializeField] private Button m_confirmButton;

        private void Start()
        {
            m_confirmButton.onClick.AddListener(m_attributes.Confirm);
        }
    }
}
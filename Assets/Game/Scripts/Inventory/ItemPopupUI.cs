using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.UI
{
    public class ItemPopupUI : MonoBehaviour
    {
        [SerializeField] private Inventory m_inventory;
        [SerializeField] private GameObject m_itemPopupUI;
        [SerializeField] private int m_maxItemPopups = 5;
        [SerializeField] private float m_popupDuration = 2f;

        private readonly Queue<GameObject> m_activePopups = new();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_inventory, nameof(m_inventory));
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_inventory.OnItemAdded += ShowPopup;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_inventory.OnItemAdded -= ShowPopup;
        }

        /*-----------------------------------------------------------------
        | --- ShowPopup: Instantiate and display an item pickup popup --- |
        -----------------------------------------------------------------*/
        private void ShowPopup(InventoryItem item, int quantity)
        {
            GameObject popup = Instantiate(m_itemPopupUI, transform);

            // Update the popup to display the item's information
            popup.GetComponent<ItemPopupSlotUI>().Setup(item, quantity);

            m_activePopups.Enqueue(popup);

            if (m_activePopups.Count > m_maxItemPopups)
            {
                Destroy(m_activePopups.Dequeue());
            }

            StartCoroutine(FadeAndDestroyPopup(popup));
        }

        /*-----------------------------------------------------------------------
        | --- FadeAndDestroyPopup: Fade out and destroy the popup over time --- |
        -----------------------------------------------------------------------*/
        private IEnumerator FadeAndDestroyPopup(GameObject popup)
        {
            yield return new WaitForSeconds(m_popupDuration);
            if (popup == null)
                yield break;

            CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
            for (float elapsedTime = 0f; elapsedTime < 1f; elapsedTime += Time.deltaTime)
            {
                canvasGroup.alpha = 1f - elapsedTime;
                yield return null;
            }

            Destroy(popup);
        }
    }
}
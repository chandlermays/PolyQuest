using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;

namespace PolyQuest.UI.Core
{
    public class AreaZoneUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI m_areaNameText;
        [SerializeField] private CanvasGroup m_areaCanvasGroup;
        [SerializeField] private float m_fadeDuration = 0.5f;
        [SerializeField] private float m_displayDuration = 3f;

        private Coroutine m_displayCoroutine;
        private readonly Stack<string> m_zoneStack = new Stack<string>();

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_areaCanvasGroup.alpha = 0f;
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            AreaTrigger.OnAreaEntered += HandleAreaEntered;
            AreaTrigger.OnAreaExited += HandleAreaExited;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            AreaTrigger.OnAreaEntered -= HandleAreaEntered;
            AreaTrigger.OnAreaExited -= HandleAreaExited;
        }

        /*-------------------------------------------------------------------------------
        | --- HandleAreaEntered: Activates the UI when the player enters a new area --- |
        -------------------------------------------------------------------------------*/
        private void HandleAreaEntered(string areaName)
        {
            m_zoneStack.Push(areaName);
            ShowZone(areaName);
        }

        /*-----------------------------------------------------------------------------------
        | --- HandleAreaExited: Restores the previous zone UI when the player exits one --- |
        -----------------------------------------------------------------------------------*/
        private void HandleAreaExited(string areaName)
        {
            if (m_zoneStack.Count > 0 && m_zoneStack.Peek() == areaName)
                m_zoneStack.Pop();

            if (m_zoneStack.Count > 0)
                ShowZone(m_zoneStack.Peek());
        }

        /*---------------------------------------------------------------------
        | --- ShowZone: Triggers the display routine for a given zone name --- |
        ---------------------------------------------------------------------*/
        private void ShowZone(string areaName)
        {
            m_areaNameText.text = areaName;

            if (m_displayCoroutine != null)
                StopCoroutine(m_displayCoroutine);

            m_displayCoroutine = StartCoroutine(DisplayRoutine());
        }

        /*---------------------------------------------------------------------------------------
        | --- DisplayRoutine: Fades in the UI, holds for a duration, then fades it back out --- |
        ---------------------------------------------------------------------------------------*/
        private IEnumerator DisplayRoutine()
        {
            yield return StartCoroutine(FadeTo(1f));
            yield return new WaitForSeconds(m_displayDuration);
            yield return StartCoroutine(FadeTo(0f));
            m_displayCoroutine = null;
        }

        /*-----------------------------------------------------------------------
        | --- FadeTo: Lerps the CanvasGroup alpha to the given target value --- |
        -----------------------------------------------------------------------*/
        private IEnumerator FadeTo(float target)
        {
            float start = m_areaCanvasGroup.alpha;
            float elapsed = 0f;

            while (elapsed < m_fadeDuration)
            {
                elapsed += Time.deltaTime;
                m_areaCanvasGroup.alpha = Mathf.Lerp(start, target, elapsed / m_fadeDuration);
                yield return null;
            }

            m_areaCanvasGroup.alpha = target;
        }
    }
}
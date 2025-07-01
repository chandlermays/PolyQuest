using UnityEngine;
using System.Collections;
//---------------------------------

namespace PolyQuest
{
    public class TransitionFade : MonoBehaviour
    {
        [Header("Fade Settings")]
        [SerializeField] private float m_fadeDuration = 2f;
        [SerializeField] private float m_waitDuration = 1f;

        private CanvasGroup m_canvasGroup;
        private Coroutine m_currentActiveFade = null;

        /*---------------------------------------------------------------
        | --- Awae: Called when the script instance is being loaded --- |
        ---------------------------------------------------------------*/
        private void Awake()
        {
            m_canvasGroup = GetComponent<CanvasGroup>();
        }

        /*----------------------------------------------------
        | --- ResetFade: Set the Fade's Alpha value to 1 --- |
        ----------------------------------------------------*/
        public void ResetFade()
        {
            m_canvasGroup.alpha = 1;
        }

        /*--------------------------------------------------------------------
        | --- FadeOut: Coroutine for Fading Out for the Scene Transition --- |
        --------------------------------------------------------------------*/
        public Coroutine FadeOut()
        {
            return Fade(1, m_fadeDuration);
        }

        /*------------------------------------------------------------------
        | --- FadeIn: Coroutine for Fading In for the Scene Transition --- |
        ------------------------------------------------------------------*/
        public Coroutine FadeIn()
        {
            return Fade(0, m_fadeDuration);
        }

        /*--------------------------------------------------------------------
        | --- Fade: Coroutine for Fading In/Out for the Scene Transition --- |
        --------------------------------------------------------------------*/
        public Coroutine Fade(float target, float time)
        {
            if (m_currentActiveFade != null)
            {
                StopCoroutine(m_currentActiveFade);
            }
            m_currentActiveFade = StartCoroutine(FadeRoutine(target, time));
            return m_currentActiveFade;
        }

        /*------------------------------------------------------------------------
        | --- FadeRoutine: Coroutine to Fade In/Out for the Scene Transition --- |
        ------------------------------------------------------------------------*/
        private IEnumerator FadeRoutine(float target, float time)
        {
            while (!Mathf.Approximately(m_canvasGroup.alpha, target))
            {
                m_canvasGroup.alpha = Mathf.MoveTowards(m_canvasGroup.alpha, target, Time.unscaledDeltaTime / time);
                yield return null;
            }
        }

        /*------------------------------------------------------------------------------------------------
        | --- Wait: Coroutine to Wait a Specified Duration in between Fades for the Scene Transition --- |
        ------------------------------------------------------------------------------------------------*/
        public IEnumerator Wait()
        {
            yield return new WaitForSeconds(m_waitDuration);
        }
    }
}
using System.Collections;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private Experience m_experience;
        [SerializeField] private float m_fadeDuration = 0.5f;
        [SerializeField] private float m_holdDuration = 2f;

        private CanvasGroup m_canvasGroup;
        private Coroutine m_activeCoroutine;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_experience, nameof(m_experience));

            m_canvasGroup = GetComponent<CanvasGroup>();
            Utilities.CheckForNull(m_canvasGroup, nameof(m_canvasGroup));

            Hide();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_experience.OnLevelUp += HandleLevelUp;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_experience.OnLevelUp -= HandleLevelUp;
        }

        /*----------------------------------
        | --- Hide: Hide the UI object --- |
        ----------------------------------*/
        private void Hide()
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;
        }

        /*----------------------------------------------------------
        | --- HandleLevelUp: Initiate the level-up UI sequence --- |
        ----------------------------------------------------------*/
        private void HandleLevelUp()
        {
            if (m_activeCoroutine != null)
            {
                StopCoroutine(m_activeCoroutine);
            }

            m_activeCoroutine = StartCoroutine(PlayLevelUpSequence());
        }

        /*---------------------------------------------------------------------------------
        | --- PlayLevelUpSequence: Fade in, hold, then fade out the level-up UI panel --- |
        ---------------------------------------------------------------------------------*/
        private IEnumerator PlayLevelUpSequence()
        {
            yield return StartCoroutine(Fade(0f, 1f));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(1f, 0f));

            Hide();
            m_activeCoroutine = null;
        }

        /*-----------------------------------------------------------------------
        | --- Fade: Lerp the CanvasGroup alpha between two values over time --- |
        -----------------------------------------------------------------------*/
        private IEnumerator Fade(float from, float to)
        {
            m_canvasGroup.alpha = from;

            for (float elapsed = 0f; elapsed < m_fadeDuration; elapsed += Time.deltaTime)
            {
                m_canvasGroup.alpha = Mathf.Lerp(from, to, elapsed / m_fadeDuration);
                yield return null;
            }

            m_canvasGroup.alpha = to;
        }
    }
}
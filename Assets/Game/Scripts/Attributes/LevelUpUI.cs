using System.Collections;
using UnityEngine;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.UI
{
    public class LevelUpUI : MonoBehaviour
    {
        [SerializeField] private Experience m_experience;
        [SerializeField] private CanvasGroup m_instructionText;
        [SerializeField] private float m_fadeDuration = 0.5f;
        [SerializeField] private float m_holdDuration = 2f;
        [SerializeField] private float m_instructionDelay = 0.3f;
        [SerializeField] private float m_instructionDuration = 0.5f;

        private CanvasGroup m_canvasGroup;
        private Coroutine m_activeCoroutine;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_experience, nameof(m_experience));
            Utilities.CheckForNull(m_instructionText, nameof(m_instructionText));

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

            m_instructionText.alpha = 0f;
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
            Hide();

            Coroutine instructionSequence = StartCoroutine(PlayInstructionSequence());
            yield return StartCoroutine(Fade(m_canvasGroup, 0f, 1f, m_fadeDuration));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(m_canvasGroup, 1f, 0f, m_fadeDuration));

            yield return instructionSequence;

            Hide();
            m_activeCoroutine = null;
        }

        /*----------------------------------------------------------------------------------
        | --- PlayInstructionSequence: Fade the text in and out after an initial delay --- |
        ----------------------------------------------------------------------------------*/
        private IEnumerator PlayInstructionSequence()
        {
            yield return new WaitForSeconds(m_instructionDelay);
            yield return StartCoroutine(Fade(m_instructionText, 0f, 1f, m_instructionDuration));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(m_instructionText, 1f, 0f, m_instructionDuration));
        }

        /*-----------------------------------------------------------------------
        | --- Fade: Lerp the CanvasGroup alpha between two values over time --- |
        -----------------------------------------------------------------------*/
        private IEnumerator Fade(CanvasGroup target, float from, float to, float duration)
        {
            target.alpha = from;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                target.alpha = Mathf.Lerp(from, to, elapsed / duration);
                yield return null;
            }

            target.alpha = to;
        }
    }
}
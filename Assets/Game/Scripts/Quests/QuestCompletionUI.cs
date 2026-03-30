using System.Collections;
using UnityEngine;
using TMPro;
//---------------------------------
using PolyQuest.Quests;

namespace PolyQuest.UI
{
    public class QuestCompletionUI : MonoBehaviour
    {
        [SerializeField] private QuestManager m_questManager;
        [SerializeField] private TextMeshProUGUI m_questTitle;
        [SerializeField] private CanvasGroup m_titleCanvasGroup;
        [SerializeField] private float m_fadeDuration = 0.5f;
        [SerializeField] private float m_holdDuration = 3f;
        [SerializeField] private float m_titleDelay = 0.3f;
        [SerializeField] private float m_titleFadeDuration = 0.5f;

        private CanvasGroup m_canvasGroup;
        private Coroutine m_activeCoroutine;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_questManager, nameof(m_questManager));
            Utilities.CheckForNull(m_questTitle, nameof(m_questTitle));
            Utilities.CheckForNull(m_titleCanvasGroup, nameof(m_titleCanvasGroup));

            m_canvasGroup = GetComponent<CanvasGroup>();
            Utilities.CheckForNull(m_canvasGroup, nameof(m_canvasGroup));

            Hide();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_questManager.OnQuestCompleted += HandleQuestCompleted;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_questManager.OnQuestCompleted -= HandleQuestCompleted;
        }

        /*---------------------------------
        | --- Hide: Hide the UI panel --- |
        ---------------------------------*/
        private void Hide()
        {
            m_canvasGroup.alpha = 0f;
            m_canvasGroup.interactable = false;
            m_canvasGroup.blocksRaycasts = false;

            m_titleCanvasGroup.alpha = 0f;
        }

        /*-------------------------------------------------------------------------
        | --- HandleQuestStarted: Initiate the quest completion UI sequence --- |
        -------------------------------------------------------------------------*/
        private void HandleQuestCompleted(Quest quest)
        {
            if (m_activeCoroutine != null)
            {
                StopCoroutine(m_activeCoroutine);
            }

            m_questTitle.text = quest.Title;
            m_activeCoroutine = StartCoroutine(PlayQuestCompleteSequence());
        }

        /*--------------------------------------------------------------------------------------------
        | --- PlayQuestStartedSequence: Fade in, hold, then fade out the quest completion panel --- |
        --------------------------------------------------------------------------------------------*/
        private IEnumerator PlayQuestCompleteSequence()
        {
            Hide();

            Coroutine titleSequence = StartCoroutine(PlayTitleSequence());
            yield return StartCoroutine(Fade(m_canvasGroup, 0f, 1f, m_fadeDuration));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(m_canvasGroup, 1f, 0f, m_fadeDuration));

            yield return titleSequence;

            Hide();
            m_activeCoroutine = null;
        }

        /*--------------------------------------------------------------------
        | --- PlayTitleSequence: Fade the title in and out after a delay --- |
        --------------------------------------------------------------------*/
        private IEnumerator PlayTitleSequence()
        {
            yield return new WaitForSeconds(m_titleDelay);
            yield return StartCoroutine(Fade(m_titleCanvasGroup, 0f, 1f, m_titleFadeDuration));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(m_titleCanvasGroup, 1f, 0f, m_titleFadeDuration));
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
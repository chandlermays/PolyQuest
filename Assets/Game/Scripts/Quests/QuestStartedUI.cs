/*---------------------------
| File: QuestStartedUI.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
//---------------------------------
using PolyQuest.Dialogues;
using PolyQuest.Quests;

namespace PolyQuest.UI
{
    public class QuestStartedUI : MonoBehaviour
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

        private PlayerDialogueHandler m_dialogueHandler;            // Notices wait while a conversation is on screen
        private readonly Queue<Quest> m_pendingQuests = new();

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

            m_dialogueHandler = m_questManager.GetComponent<PlayerDialogueHandler>();

            Hide();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            m_questManager.OnQuestStarted += HandleQuestStarted;
            if (m_dialogueHandler != null)
                m_dialogueHandler.OnDialogueEnded += ShowPending;
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            m_questManager.OnQuestStarted -= HandleQuestStarted;
            if (m_dialogueHandler != null)
                m_dialogueHandler.OnDialogueEnded -= ShowPending;
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

        /*-------------------------------------------------------------------------------------------
        | --- HandleQuestStarted: Shows the notice now, or once the conversation on screen ends --- |
        -------------------------------------------------------------------------------------------*/
        private void HandleQuestStarted(Quest quest)
        {
            m_pendingQuests.Enqueue(quest);
            ShowPending();
        }

        /*---------------------------------------------------------------------------------------------
        | --- ShowPending: Starts showing the waiting notices, unless a conversation is on screen --- |
        ---------------------------------------------------------------------------------------------*/
        private void ShowPending()
        {
            if (m_activeCoroutine != null || m_pendingQuests.Count == 0 || IsDialogueActive())
                return;

            m_activeCoroutine = StartCoroutine(PlayPendingNotices());
        }

        /*------------------------------------------------------------------------------------------
        | --- PlayPendingNotices: Shows each waiting notice in turn, pausing for conversations --- |
        ------------------------------------------------------------------------------------------*/
        private IEnumerator PlayPendingNotices()
        {
            while (m_pendingQuests.Count > 0 && !IsDialogueActive())
            {
                m_questTitle.text = m_pendingQuests.Dequeue().Title;
                yield return StartCoroutine(PlayQuestStartedSequence());
            }

            m_activeCoroutine = null;
        }

        /*-------------------------------------------------------------------------
        | --- IsDialogueActive: Whether a conversation is on screen right now --- |
        -------------------------------------------------------------------------*/
        private bool IsDialogueActive() => m_dialogueHandler != null && m_dialogueHandler.IsActive();

        /*----------------------------------------------------------------------------------------
        | --- PlayQuestStartedSequence: Fade in, hold, then fade out the quest started panel --- |
        ----------------------------------------------------------------------------------------*/
        private IEnumerator PlayQuestStartedSequence()
        {
            Hide();

            Coroutine titleSequence = StartCoroutine(PlayTitleSequence());
            yield return StartCoroutine(Fade(m_canvasGroup, 0f, 1f, m_fadeDuration));
            yield return new WaitForSeconds(m_holdDuration);
            yield return StartCoroutine(Fade(m_canvasGroup, 1f, 0f, m_fadeDuration));

            yield return titleSequence;

            Hide();
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
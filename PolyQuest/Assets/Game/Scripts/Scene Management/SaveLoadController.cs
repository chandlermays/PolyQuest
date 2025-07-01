using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest
{
    public class SaveLoadController : MonoBehaviour
    {
        private const string kDefaultSaveFile = "PolyQuestSaveFile";

        /* --- Key Bindings --- */
        [SerializeField] private KeyCode kSaveKey = KeyCode.F5;
        [SerializeField] private KeyCode kLoadKey = KeyCode.F9;

        private SaveSystem m_saveSystem;

        /*-----------------------------------------------------
        | --- Start: Called before the first frame update --- |
        -----------------------------------------------------*/
        private void Start()
        {
            m_saveSystem = GetComponent<SaveSystem>();

            TransitionFade transitionFade = FindObjectOfType<TransitionFade>();

            transitionFade.ResetFade();
            StartCoroutine(m_saveSystem.LoadLastScene(kDefaultSaveFile));
            transitionFade.FadeIn();
        }

        /*-----------------------------------------
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            if (Input.GetKeyDown(kSaveKey))
            {
                Save();
            }
            if (Input.GetKeyDown(kLoadKey))
            {
                Load();
            }
        }

        /*--------------------------------------------------------
        | --- Save: Perform the Action of Saving to the File --- |
        --------------------------------------------------------*/
        public void Save()
        {
            m_saveSystem.Save(kDefaultSaveFile);
        }

        /*---------------------------------------------------------
        | --- Load: Perform the Action of Loading from a File --- |
        ---------------------------------------------------------*/
        public void Load()
        {
            m_saveSystem.Load(kDefaultSaveFile);
        }
    }
}
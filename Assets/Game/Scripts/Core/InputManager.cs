using UnityEngine;

namespace PolyQuest.Input
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }
        public PolyQuestInputActions InputActions { get; private set; }

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            InputActions = new PolyQuestInputActions();
        }

        /*---------------------------------------------------------------------
        | --- OnEnable: Called when the object becomes enabled and active --- |
        ---------------------------------------------------------------------*/
        private void OnEnable()
        {
            InputActions.Enable();
        }

        /*---------------------------------------------------------------------------
        | --- OnDisable: Called when the behaviour becomes disabled or inactive --- |
        ---------------------------------------------------------------------------*/
        private void OnDisable()
        {
            InputActions.Disable();
        }

        /*--------------------------------------------------------------------
        | --- OnDestroy: Called when the MonoBehaviour will be destroyed --- |
        --------------------------------------------------------------------*/
        private void OnDestroy()
        {
            InputActions?.Dispose();
        }
    }
}
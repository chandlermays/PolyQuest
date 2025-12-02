using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
//---------------------------------
using PolyQuest.Core;
using PolyQuest.Components;
using PolyQuest.Inventories;
using PolyQuest.UI.Core;
using PolyQuest.Dialogues;

namespace PolyQuest.Player
{
    /* ----------------------------------------------------------------------------------------------
     * Role: Responsible for controlling the player's actions, input, and interactions in the game. *
     *                                                                                              *
     * Responsibilities:                                                                            *
     *      - Processes player input and translates it into movement, combat, and interactions.     *
     *      - Handles navigation, including raycasting and NavMesh movement.                        *
     *      - Manages player interaction with UI and in-world interactable objects.                 *
     *      - Updates the mouse cursor based on context (UI, movement, combat, etc.).               *
     *      - Coordinates with movement, combat, and health components.                             *
     * -------------------------------------------------------------------------------------------- */

    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private CursorSettings m_cursorSettings;
        private CursorSettings.CursorType m_currentCursorType;
        private bool m_isDraggingUI = false;

        private Camera m_mainCamera;
        private MovementComponent m_movement;
        private HealthComponent m_healthComponent;
        private Actions m_actions;
        private PlayerDialogueHandler m_dialogueHandler;

        [SerializeField] private float m_maxNavMeshDistance = 1f;
        private readonly RaycastHit[] m_raycasts = new RaycastHit[16];

        [SerializeField] private int m_numberOfActionSlots = 6;

        private bool m_isInDialogue = false;

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            Utilities.CheckForNull(m_cursorSettings, nameof(m_cursorSettings));

            m_mainCamera = Camera.main;
            Utilities.CheckForNull(m_mainCamera, nameof(m_mainCamera));

            m_movement = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_movement, nameof(m_movement));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));

            m_actions = GetComponent<Actions>();
            Utilities.CheckForNull(m_actions, nameof(m_actions));

            m_dialogueHandler = GetComponent<PlayerDialogueHandler>();
            Utilities.CheckForNull(m_dialogueHandler, nameof(m_dialogueHandler));
        }

        private void OnEnable()
        {
            m_dialogueHandler.OnDialogueStarted += HandleDialogueStarted;
            m_dialogueHandler.OnDialogueEnded += HandleDialogueEnded;
        }

        private void OnDisable()
        {
            m_dialogueHandler.OnDialogueStarted -= HandleDialogueStarted;
            m_dialogueHandler.OnDialogueEnded -= HandleDialogueEnded;
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Do nothing if you're dead
            if (m_healthComponent.IsDead)
            {
                SetCursor(CursorSettings.CursorType.kNone);
                return;
            }

            ProcessEvents();
        }

        /*---------------------------------------------------- 
        | --- ProcessEvents: Handle any Events via Input --- |
        ----------------------------------------------------*/
        private void ProcessEvents()
        {
            if (HandleUI())
                return;

            if (!m_isInDialogue)
            {
                if (HandleAbilities())
                    return;

                if (HandleInteractable())
                    return;

                if (HandleMovement())
                    return;
            }

            // If no action was taken, set the Cursor to normal
            SetCursor(CursorSettings.CursorType.kNone);
        }

        /*-----------------------------------------------------------------------
        | --- HandleAbilities: Use the Ability associated with Action Slots --- |
        -----------------------------------------------------------------------*/
        private bool HandleAbilities()
        {
            for (int i = 0; i < m_numberOfActionSlots; ++i)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    if (m_actions.Use(gameObject, i))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /*---------------------------------------------------------
        | --- HandleUI: Perform the action associated with UI --- |
        ---------------------------------------------------------*/
        private bool HandleUI()
        {
            if (Input.GetMouseButtonUp(0))
            {
                m_isDraggingUI = false;
            }

            if (EventSystem.current.IsPointerOverGameObject())
            {
                if (Input.GetMouseButtonDown(0))
                {
                    m_isDraggingUI = true;
                }
                SetCursor(CursorSettings.CursorType.kNone);
                return true;
            }

            if (m_isDraggingUI)
            {
                return true;
            }

            return false;
        }

        /*------------------------------------------------------------------------------
        | --- HandleInteractable: Perform the action associated with Interactables --- |
        ------------------------------------------------------------------------------*/
        private bool HandleInteractable()
        {
            int hits = Physics.RaycastNonAlloc(GetCursorRay(), m_raycasts);
            for (int i = 0; i < hits; ++i)
            {
                RaycastHit hit = m_raycasts[i];

                // Ignore self
                if (hit.transform.gameObject == gameObject)
                {
                    continue;
                }

                IRaycastable[] raycastables = hit.transform.GetComponents<IRaycastable>();
                foreach (IRaycastable raycastable in raycastables)
                {
                    if (raycastable.HandleRaycast(this))
                    {
                        SetCursor(raycastable.GetCursorType());
                        return true;
                    }
                }
            }
            return false;
        }

        /*--------------------------------------------------------------------- 
        | --- HandleMovement: Perform the action associated with Movement --- |
        ---------------------------------------------------------------------*/
        private bool HandleMovement()
        {
            if (!Input.GetMouseButton(0)) return false;

            if (RaycastNavMesh(out Vector3 target))
            {
                if (!m_movement.CanMoveTo(target)) return false;

                m_movement.StartMoveAction(target);
                SetCursor(CursorSettings.CursorType.kMovement);
                return true;
            }
            return false;
        }

        /*------------------------------------------------------------------------------- 
        | --- RaycastNavMesh: Raycast to the NavMesh and return the target position --- |
        -------------------------------------------------------------------------------*/
        private bool RaycastNavMesh(out Vector3 target)
        {
            target = Vector3.zero;

            // Perform a raycast from the camera to the world
            if (!Physics.Raycast(GetCursorRay(), out RaycastHit hit))
                return false;

            // Check if the hit point is valid on the NavMesh
            if (!NavMesh.SamplePosition(hit.point, out NavMeshHit navMeshHit, m_maxNavMeshDistance, NavMesh.AllAreas))
                return false;

            // Assign the valid NavMesh position to the target
            target = navMeshHit.position;

            return true;
        }

        private void HandleDialogueStarted()
        {
            m_isInDialogue = true;
        }

        private void HandleDialogueEnded()
        {
            m_isInDialogue = false;
        }

        /*---------------------------------------------------------------------- 
        | --- GetCursorRay: Get the Ray from the Camera through the Cursor --- |
        ----------------------------------------------------------------------*/
        public Ray GetCursorRay()
        {
            return m_mainCamera.ScreenPointToRay(Input.mousePosition);
        }

        /*-------------------------------------------------------------------
        | --- SetCursor: Sets the Cursor based on the given Cursor Type --- |
        -------------------------------------------------------------------*/
        public void SetCursor(CursorSettings.CursorType type)
        {
            if (m_currentCursorType == type)
                return;

            m_currentCursorType = type;

            CursorSettings.CursorMapping mapping = m_cursorSettings.GetCursorMapping(m_currentCursorType);
            if (mapping.m_texture != null)
            {
                Cursor.SetCursor(mapping.m_texture, mapping.Hotspot, CursorMode.Auto);
            }
        }
    }
}
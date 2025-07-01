using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;
//---------------------------------

namespace PolyQuest
{
    /*---------------------------------------------------------- 
    | --- Responsible for Controlling the Player's Actions --- |
    ----------------------------------------------------------*/
    public class PlayerController : MonoBehaviour
    {
        /* --- Cursor Settings --- */
        [SerializeField] private CursorSettings m_cursorSettings;
        private CursorSettings.CursorType m_currentCursorType;

        /* --- References --- */
        private Camera m_mainCamera;
        private MovementComponent m_playerMovement;
        private CombatComponent m_combatComponent;
        private HealthComponent m_healthComponent;

        /* --- Navigation --- */
        [SerializeField] private float m_maxNavMeshDistance = 1f;
        private readonly RaycastHit[] m_raycasts = new RaycastHit[16];

        /*----------------------------------------------------------------
        | --- Awake: Called when the script instance is being loaded --- |
        ----------------------------------------------------------------*/
        private void Awake()
        {
            m_mainCamera = Camera.main;

            m_playerMovement = GetComponent<MovementComponent>();
            Utilities.CheckForNull(m_playerMovement, nameof(m_playerMovement));

            m_combatComponent = GetComponent<CombatComponent>();
            Utilities.CheckForNull(m_combatComponent, nameof(m_combatComponent));

            m_healthComponent = GetComponent<HealthComponent>();
            Utilities.CheckForNull(m_healthComponent, nameof(m_healthComponent));
        }

        /*----------------------------------------- 
        | --- Update: Called upon every frame --- |
        -----------------------------------------*/
        private void Update()
        {
            // Do nothing if you're dead
            if (m_healthComponent.IsDead())
            {
                SetCursor(CursorSettings.CursorType.kNormal);
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

            if (HandleInteractable())
                return;

            if (HandleMovement())
                return;

            // If no action was taken, set the Cursor to normal
            SetCursor(CursorSettings.CursorType.kNormal);
        }

        /*---------------------------------------------------------
        | --- HandleUI: Perform the Action associated with UI --- |
        ---------------------------------------------------------*/
        private bool HandleUI()
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                SetCursor(CursorSettings.CursorType.kNormal);
                return true;
            }
            return false;
        }

        /*---------------------------------------------------------------------------------------------
        | --- HandleInteractable: Perform the Action associated with Interactables (IRaycastable) --- |
        ---------------------------------------------------------------------------------------------*/
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

        /*----------------------------------------------------------------------------- 
        | --- HandleMovement: Perform the Action associated with Movement (Input) --- |
        -----------------------------------------------------------------------------*/
        private bool HandleMovement()
        {
            if (!Input.GetMouseButton(0)) return false;

            if (RaycastNavMesh(out Vector3 target))
            {
                if (!m_playerMovement.CanMoveTo(target)) return false;

                m_playerMovement.StartMoveAction(target);
                SetCursor(CursorSettings.CursorType.kMovement);
                return true;
            }
            return false;
        }

        /*-------------------------------------------------------------------------- 
        | --- RaycastNavMesh: Check if the Raycast hits the NavMesh (Movement) --- |
        --------------------------------------------------------------------------*/
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

        /*---------------------------------------------------------------------------------- 
        | --- GetCursorRay: Returns a Ray going from the Camera through a Screen Point --- |
        ----------------------------------------------------------------------------------*/
        private Ray GetCursorRay()
        {
            return m_mainCamera.ScreenPointToRay(Input.mousePosition);
        }

        /*--------------------------------------------------------------------------------- 
        | --- SetCursor: Change the Current Cursor Type to the Associated Action (UI) --- |
        ---------------------------------------------------------------------------------*/
        private void SetCursor(CursorSettings.CursorType type)
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
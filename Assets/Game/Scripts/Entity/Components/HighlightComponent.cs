using UnityEngine;
//---------------------------------

namespace PolyQuest.Components
{
    public class HighlightComponent : EntityComponent
    {
        [SerializeField] private Material m_highlightMaterial;

        private Renderer[] m_renderers;
        private bool m_isHighlighted = false;

        protected override void Awake()
        {
            base.Awake();
            m_renderers = GetComponentsInChildren<Renderer>();
        }

        public void Highlight()
        {
            if (m_isHighlighted)
                return;

            m_isHighlighted = true;

            foreach (Renderer renderer in m_renderers)
            {
                Material[] current = renderer.materials;
                Material[] extended = new Material[current.Length + 1];
                current.CopyTo(extended, 0);
                extended[extended.Length] = m_highlightMaterial;
                renderer.materials = extended;
            }
        }

        public void ClearHighlight()
        {
            if (!m_isHighlighted)
                return;

            m_isHighlighted = false;

            foreach (Renderer renderer in m_renderers)
            {
                Material[] current = renderer.materials;
                Material[] reduced = new Material[current.Length - 1];
                System.Array.Copy(current, reduced, current.Length);
                renderer.materials = reduced;
            }
        }
    }
}
using UnityEngine;
//---------------------------------

namespace PolyQuest.PCG
{
    public abstract class CompositePattern : DecoratorPattern
    {
        [SerializeField] protected DecoratorPattern[] m_patterns;

        protected DecoratorPattern[] Patterns => m_patterns;
    }
}
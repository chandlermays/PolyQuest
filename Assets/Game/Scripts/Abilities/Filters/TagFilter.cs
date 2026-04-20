/*---------------------------
File: TagFilter.cs
Author: Chandler Mays
----------------------------*/
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    [CreateAssetMenu(menuName = "PolyQuest/Abilities/Filters/Tag", fileName = "New Tag Filter")]
    public class TagFilter : FilteringStrategy
    {
        [SerializeField] private string m_filteredTag;

        /*-------------------------------------------------------------
        | --- Filter: Returns only targets with the specified tag --- |
        -------------------------------------------------------------*/
        public override IEnumerable<GameObject> Filter(IEnumerable<GameObject> targets)
        {
            foreach (GameObject target in targets)
            {
                if (target.CompareTag(m_filteredTag))
                {
                    yield return target;
                }
            }
        }
    }
}
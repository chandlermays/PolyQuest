using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------
using PolyQuest.Core;

namespace PolyQuest.Abilities
{
    public class AbilityConfig : IAction
    {
        private GameObject m_user;
        private Vector3 m_targetPoint;
        private IEnumerable<GameObject> m_targets;
        private bool m_isCancelled = false;

        public GameObject User => m_user;
        public Vector3 TargetPoint
        {
            get => m_targetPoint;
            set => m_targetPoint = value;
        }
        public IEnumerable<GameObject> Targets
        {
            get => m_targets;
            set => m_targets = value;
        }
        public bool IsCancelled => m_isCancelled;

        public AbilityConfig(GameObject user)
        {
            m_user = user;
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            m_user.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }

        public void SetSingleTarget(GameObject target)
        {
            m_targets = SingleTarget(target);
        }

        public void Cancel()
        {
            m_isCancelled = true;
        }

        private static IEnumerable<GameObject> SingleTarget(GameObject target)
        {
            yield return target;
        }
    }
}
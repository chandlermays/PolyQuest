using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Abilities
{
    public class AbilityConfig
    {
        private GameObject m_user;
        private Vector3 m_targetPoint;
        private IEnumerable<GameObject> m_targets;

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

        public AbilityConfig(GameObject user)
        {
            m_user = user;
        }

        public void StartCoroutine(IEnumerator coroutine)
        {
            m_user.GetComponent<MonoBehaviour>().StartCoroutine(coroutine);
        }
    }
}
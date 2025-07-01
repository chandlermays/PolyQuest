using UnityEngine;
//---------------------------------

namespace PolyQuest
{
    [System.Serializable]
    public class SceneField
    {
        [SerializeField] private Object m_sceneAsset;
        [SerializeField] private string m_sceneName = "";

        public string SceneName
        {
            get { return m_sceneName; }
        }

        // Makes it work with the existing Unity methods (LoadLevel/LoadScene)
        public static implicit operator string(SceneField sceneField)
        {
            return sceneField.SceneName;
        }
    }
}
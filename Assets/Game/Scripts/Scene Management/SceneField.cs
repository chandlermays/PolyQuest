/*---------------------------
File: SceneField.cs
Author: Chandler Mays
----------------------------*/
using UnityEngine;
//---------------------------------

namespace PolyQuest.SceneManagement
{
    /* --------------------------------------------------------------------------------------------
     * Role: Serializable reference to a Unity scene, usable in the editor and at runtime.         *
     *                                                                                             *
     * Responsibilities:                                                                           *
     *      - Stores a reference to a scene asset and its name for use in scripts and inspectors.  *
     *      - Provides access to the scene name for loading or comparison purposes.                *
     *      - Supports implicit conversion to string for compatibility with Unity scene APIs.      *
     * ------------------------------------------------------------------------------------------- */
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
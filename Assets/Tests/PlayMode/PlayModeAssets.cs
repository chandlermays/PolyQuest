/*---------------------------
| File: PlayModeAssets.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
using UnityEngine;
//---------------------------------

namespace PolyQuest.Tests
{
    // Loads real project assets (prefabs) by path. Play Mode tests only run inside the Editor, so AssetDatabase is available.
    internal static class PlayModeAssets
    {
        public static T Load<T>(string path) where T : Object
        {
#if UNITY_EDITOR
            T asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(path);
#else
            T asset = null;
#endif
            Assert.That(asset, Is.Not.Null, $"Asset not found at '{path}'. Was it moved or renamed?");
            return asset;
        }
    }
}
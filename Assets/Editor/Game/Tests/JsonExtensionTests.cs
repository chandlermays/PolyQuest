/*---------------------------
| File: JsonExtensionTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    public class JsonExtensionTests
    {
        [Test]
        [Description("Vector3.ToToken writes an object with exactly x, y and z as floats.")]
        public void ToToken_WritesXYZ_AsFloats()
        {
            JObject token = (JObject)new Vector3(1.5f, -2f, 3.25f).ToToken();

            Assert.That(token.Count, Is.EqualTo(3));
            Assert.That(token["x"].Value<float>(), Is.EqualTo(1.5f));
            Assert.That(token["y"].Value<float>(), Is.EqualTo(-2f));
            Assert.That(token["z"].Value<float>(), Is.EqualTo(3.25f));
        }

        [Test]
        [Description("ToToken followed by ToVector3 returns the original vector.")]
        public void RoundTrip_PreservesTheVector()
        {
            Vector3 original = new(12.3456f, -0.001f, 9999.5f);

            Vector3 restored = original.ToToken().ToVector3();

            Assert.That(restored, Is.EqualTo(original));
        }
    }
}
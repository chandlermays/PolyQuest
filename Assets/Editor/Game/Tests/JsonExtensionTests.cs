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
        public void ToToken_WritesXYZ_AsFloats()
        {
            JObject token = (JObject)new Vector3(1.5f, -2f, 3.25f).ToToken();

            Assert.That(token.Count, Is.EqualTo(3));
            Assert.That(token["x"].Value<float>(), Is.EqualTo(1.5f));
            Assert.That(token["y"].Value<float>(), Is.EqualTo(-2f));
            Assert.That(token["z"].Value<float>(), Is.EqualTo(3.25f));
        }

        [Test]
        public void RoundTrip_PreservesTheVector()
        {
            Vector3 original = new(12.3456f, -0.001f, 9999.5f);

            Vector3 restored = original.ToToken().ToVector3();

            Assert.That(restored, Is.EqualTo(original));
        }

        [Test]
        public void ToVector3_MissingKeys_DefaultToZero()
        {
            JToken partial = new JObject { ["x"] = 4f };

            Assert.That(partial.ToVector3(), Is.EqualTo(new Vector3(4f, 0f, 0f)));
        }

        [Test]
        public void ToVector3_EmptyObject_IsZero()
        {
            Assert.That(new JObject().ToVector3(), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ToVector3_NonObjectToken_IsZero()
        {
            Assert.That(new JValue(5).ToVector3(), Is.EqualTo(Vector3.zero));
            Assert.That(JValue.CreateNull().ToVector3(), Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void ToVector3_IgnoresUnrelatedKeys()
        {
            JToken token = new JObject { ["x"] = 1f, ["y"] = 2f, ["z"] = 3f, ["w"] = 99f };

            Assert.That(token.ToVector3(), Is.EqualTo(new Vector3(1f, 2f, 3f)));
        }
    }
}
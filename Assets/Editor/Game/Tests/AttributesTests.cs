/*---------------------------
| File: AttributesTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Attribute = PolyQuest.Attributes.Attribute;
//---------------------------------
using PolyQuest.Attributes;

namespace PolyQuest.Tests
{
    // Attribute points: the points available per level, assigning, confirming, the stat pipeline, the save data and the
    // attribute panel. The attribute settings are made in the test, so the Player's real numbers stay tuning data.
    public class AttributesTests
    {
        private const float kTolerance = 1e-4f;

        private InventoryTestKit m_kit;
        private BaseStats m_stats;
        private Attributes.Attributes m_attributes;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            (m_stats, m_attributes) = NewPlayer("Player");
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* --- Helpers --- */

        // Points per level: 4, 8, 12. Strength: +2 damage per point. Vitality: +10% health per point.
        private (BaseStats, Attributes.Attributes) NewPlayer(string name)
        {
            PlayerProgression progression = m_kit.Track(ScriptableObject.CreateInstance<PlayerProgression>());
            SerializedTestUtil.SetIntArray(progression, "m_maxHealthAmounts", 100, 110, 121);
            SerializedTestUtil.SetIntArray(progression, "m_damageAmounts", 10, 10, 10);
            SerializedTestUtil.SetIntArray(progression, "m_maxExperienceAmounts", 100, 250, 475);
            SerializedTestUtil.SetIntArray(progression, "m_maxAttributePoints", 4, 8, 12);

            GameObject player = m_kit.NewObject(name);
            BaseStats stats = player.AddComponent<BaseStats>();
            SerializedTestUtil.SetObject(stats, "m_baseProgression", progression);
            SerializedTestUtil.SetBool(stats, "m_useStatModifiers", true);

            Attributes.Attributes attributes = player.AddComponent<Attributes.Attributes>();
            SerializedObject so = new SerializedObject(attributes);
            SerializedProperty configs = so.FindProperty("m_attributeModifiers");
            configs.arraySize = 2;
            SetConfig(configs.GetArrayElementAtIndex(0), Attribute.kStrength, Stat.kDamage, additive: 2f, percentage: 0f);
            SetConfig(configs.GetArrayElementAtIndex(1), Attribute.kVitality, Stat.kHealth, additive: 0f, percentage: 10f);
            so.ApplyModifiedPropertiesWithoutUndo();
            InventoryTestKit.InvokeAwake(attributes);
            return (stats, attributes);
        }

        private static void SetConfig(SerializedProperty config, Attribute attribute, Stat stat, float additive, float percentage)
        {
            config.FindPropertyRelative("m_attribute").enumValueIndex = (int)attribute;
            config.FindPropertyRelative("m_stat").enumValueIndex = (int)stat;
            config.FindPropertyRelative("m_additiveBonus").floatValue = additive;
            config.FindPropertyRelative("m_percentageBonus").floatValue = percentage;
        }

        /* ============================== Points available ============================== */

        [Test]
        [Description("The points remaining are the level's total minus the confirmed and pending points.")]
        public void RemainingPoints_AreTheLevelsTotal_MinusConfirmedAndPending()
        {
            Assert.That(m_attributes.GetRemainingPoints(), Is.EqualTo(4));

            m_attributes.AssignPoints(Attribute.kStrength, 1);
            m_attributes.Confirm();
            m_attributes.AssignPoints(Attribute.kVitality, 2);

            Assert.That(m_attributes.GetRemainingPoints(), Is.EqualTo(1));
        }

        [Test]
        [Description("Leveling up adds that level's points to those remaining.")]
        public void LevelingUp_AddsThatLevelsPoints()
        {
            m_attributes.AssignPoints(Attribute.kStrength, 3);
            m_attributes.Confirm();

            m_stats.LevelUp();

            Assert.That(m_attributes.GetRemainingPoints(), Is.EqualTo(5));
        }

        /* ============================== Assigning ============================== */

        [Test]
        [Description("Points can be assigned up to the number remaining and no further; a refused assignment changes nothing.")]
        public void Assigning_StopsAtTheRemainingPoints()
        {
            m_attributes.AssignPoints(Attribute.kStrength, 3);

            Assert.That(m_attributes.CanAssignPoints(Attribute.kVitality, 2), Is.False);
            m_attributes.AssignPoints(Attribute.kVitality, 2);

            Assert.That(m_attributes.GetPendingPoints(Attribute.kVitality), Is.EqualTo(0));
            Assert.That(m_attributes.CanAssignPoints(Attribute.kVitality, 1), Is.True);
        }

        [Test]
        [Description("Removing a pending point gives it back, and pending points never go below 0.")]
        public void RemovingPendingPoints_GivesThemBack_NotBelowZero()
        {
            m_attributes.AssignPoints(Attribute.kStrength, 2);

            m_attributes.AssignPoints(Attribute.kStrength, -1);
            Assert.That(m_attributes.GetPendingPoints(Attribute.kStrength), Is.EqualTo(1));
            Assert.That(m_attributes.GetRemainingPoints(), Is.EqualTo(3));

            m_attributes.AssignPoints(Attribute.kStrength, -1);
            Assert.That(m_attributes.CanAssignPoints(Attribute.kStrength, -1), Is.False);
            m_attributes.AssignPoints(Attribute.kStrength, -1);
            Assert.That(m_attributes.GetPendingPoints(Attribute.kStrength), Is.EqualTo(0));
            Assert.That(m_attributes.GetRemainingPoints(), Is.EqualTo(4));
        }

        /* ============================== Confirming and stats ============================== */

        [Test]
        [Description("Pending points change no stat until they are confirmed.")]
        public void PendingPoints_ChangeNoStat_UntilConfirmed()
        {
            float damage = m_stats.GetDamage();
            float health = m_stats.GetHealth();

            m_attributes.AssignPoints(Attribute.kStrength, 2);
            m_attributes.AssignPoints(Attribute.kVitality, 1);

            Assert.That(m_stats.GetDamage(), Is.EqualTo(damage).Within(kTolerance));
            Assert.That(m_stats.GetHealth(), Is.EqualTo(health).Within(kTolerance));
        }

        [Test]
        [Description("Confirming moves the pending points into the attributes, applies their bonuses to the stats, and raises OnStatModified once.")]
        public void Confirm_AppliesThePoints_ToTheStats()
        {
            int notifications = 0;
            m_stats.OnStatModified += () => ++notifications;
            m_attributes.AssignPoints(Attribute.kStrength, 2);
            m_attributes.AssignPoints(Attribute.kVitality, 1);

            m_attributes.Confirm();

            Assert.That(m_attributes.GetAssignedPoints(Attribute.kStrength), Is.EqualTo(2));
            Assert.That(m_attributes.GetPendingPoints(Attribute.kStrength), Is.EqualTo(0));
            Assert.That(m_stats.GetDamage(), Is.EqualTo(10f + 2f * 2f).Within(kTolerance));
            Assert.That(m_stats.GetHealth(), Is.EqualTo(100f * 1.10f).Within(kTolerance));
            Assert.That(notifications, Is.EqualTo(1));
        }

        /* ============================== Saving ============================== */

        [Test]
        [Description("Capturing and restoring into a fresh Player restores the confirmed points and their bonuses; pending points aren't saved.")]
        public void CaptureThenRestore_RestoresConfirmedPoints_NotPendingOnes()
        {
            m_attributes.AssignPoints(Attribute.kStrength, 2);
            m_attributes.Confirm();
            m_attributes.AssignPoints(Attribute.kVitality, 1);
            (BaseStats freshStats, Attributes.Attributes fresh) = NewPlayer("FreshPlayer");

            fresh.RestoreState(m_attributes.CaptureState());

            Assert.That(fresh.GetAssignedPoints(Attribute.kStrength), Is.EqualTo(2));
            Assert.That(fresh.GetTotalPoints(Attribute.kVitality), Is.EqualTo(0));
            Assert.That(freshStats.GetDamage(), Is.EqualTo(14f).Within(kTolerance));
        }

        [Test]
        [Description("A saved entry with an unknown attribute name or a value that isn't a number is skipped, and the rest load.")]
        public void RestoreState_UnreadableEntries_AreSkipped_TheRestLoad()
        {
            m_attributes.RestoreState(new JObject
            {
                ["kStrength"] = 2,
                ["kCharisma"] = 3,
                ["kVitality"] = "lots",
                ["kMagic"] = 1,
            });

            Assert.That(m_attributes.GetAssignedPoints(Attribute.kStrength), Is.EqualTo(2));
            Assert.That(m_attributes.GetAssignedPoints(Attribute.kMagic), Is.EqualTo(1));
            Assert.That(m_attributes.GetAssignedPoints(Attribute.kVitality), Is.EqualTo(0));
        }

        [Test]
        [Description("Attribute save data that isn't an object is ignored: the confirmed points are unchanged.")]
        public void RestoreState_NotAnObject_IsIgnored()
        {
            m_attributes.AssignPoints(Attribute.kStrength, 2);
            m_attributes.Confirm();

            m_attributes.RestoreState(new JArray(1, 2));
            m_attributes.RestoreState(new JValue("strength"));

            Assert.That(m_attributes.GetAssignedPoints(Attribute.kStrength), Is.EqualTo(2));
        }

        /* ============================== The attribute panel ============================== */

        private AttributeListingUI Listing(Attribute attribute, out Button minus, out Button plus, out TextMeshProUGUI value)
        {
            GameObject row = m_kit.NewObject(attribute + " Row");
            minus = m_kit.NewObject("Minus").AddComponent<Button>();
            plus = m_kit.NewObject("Plus").AddComponent<Button>();
            value = m_kit.NewObject("Value").AddComponent<TextMeshProUGUI>();
            AttributeListingUI listing = row.AddComponent<AttributeListingUI>();
            SerializedTestUtil.SetObject(listing, "m_attributes", m_attributes);
            SerializedTestUtil.SetEnum(listing, "m_attributeType", attribute);
            SerializedTestUtil.SetObject(listing, "m_minusButton", minus);
            SerializedTestUtil.SetObject(listing, "m_plusButton", plus);
            SerializedTestUtil.SetObject(listing, "m_valueText", value);
            Invoke(listing, "Start");
            return listing;
        }

        private static void Invoke(Component component, string method)
            => component.GetType().GetMethod(method, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
                        .Invoke(component, null);

        [Test]
        [Description("An attribute row's + and - buttons add and remove a pending point, and its value shows the confirmed plus pending points.")]
        public void AttributeRow_Buttons_AssignAndRemove_AndTheValueShowsThePoints()
        {
            AttributeListingUI row = Listing(Attribute.kStrength, out Button minus, out Button plus, out TextMeshProUGUI value);

            plus.onClick.Invoke();
            plus.onClick.Invoke();
            minus.onClick.Invoke();
            Invoke(row, "Update");

            Assert.That(m_attributes.GetPendingPoints(Attribute.kStrength), Is.EqualTo(1));
            Assert.That(value.text, Is.EqualTo("1"));
        }

        [Test]
        [Description("An attribute row's + is enabled only while a point remains, and - only while the attribute has a pending point.")]
        public void AttributeRow_Buttons_AreEnabledOnlyWhenTheChangeIsAllowed()
        {
            AttributeListingUI row = Listing(Attribute.kStrength, out Button minus, out Button plus, out _);

            Invoke(row, "Update");
            Assert.That(minus.interactable, Is.False, "no pending point to remove");
            Assert.That(plus.interactable, Is.True);

            m_attributes.AssignPoints(Attribute.kStrength, 4);
            Invoke(row, "Update");
            Assert.That(minus.interactable, Is.True);
            Assert.That(plus.interactable, Is.False, "no points remaining");
        }

        [Test]
        [Description("The attribute panel's Confirm button confirms the pending points, and its text shows the points remaining.")]
        public void AttributePanel_Confirm_ConfirmsThePoints_AndShowsThePointsRemaining()
        {
            Button confirm = m_kit.NewObject("Confirm").AddComponent<Button>();
            TextMeshProUGUI available = m_kit.NewObject("Available").AddComponent<TextMeshProUGUI>();
            AttributeUI panel = m_kit.NewObject("AttributePanel").AddComponent<AttributeUI>();
            SerializedTestUtil.SetObject(panel, "m_attributes", m_attributes);
            SerializedTestUtil.SetObject(panel, "m_availablePoints", available);
            SerializedTestUtil.SetObject(panel, "m_confirmButton", confirm);
            Invoke(panel, "Start");
            m_attributes.AssignPoints(Attribute.kStrength, 3);

            confirm.onClick.Invoke();
            Invoke(panel, "Update");

            Assert.That(m_attributes.GetAssignedPoints(Attribute.kStrength), Is.EqualTo(3));
            Assert.That(available.text, Does.Contain("1"));
        }
    }
}
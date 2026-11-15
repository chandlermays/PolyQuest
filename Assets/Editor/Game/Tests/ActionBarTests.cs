/*---------------------------
| File: ActionBarTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Tests
{
    // The action bar's outcomes that survive the redesign: using consumables and non-consumables, one slot per ability,
    // and the save data. Stack limits and how pickups reach the bar aren't tested; they change with the redesign.
    public class ActionBarTests
    {
        private InventoryTestKit m_kit;
        private GameObject m_player;
        private Actions m_actions;
        private ActionItem m_potion;
        private ActionItem m_fireball;

        [SetUp]
        public void SetUp()
        {
            m_kit = new InventoryTestKit();
            m_player = m_kit.NewObject("Player");
            m_kit.NewInventory(m_player, 4);
            m_actions = m_player.AddComponent<Actions>();
            InventoryTestKit.InvokeAwake(m_actions);

            m_potion = m_kit.Item<ActionItem>("test-potion", stackable: true);
            SerializedTestUtil.SetBool(m_potion, "m_isConsumable", true);
            m_fireball = m_kit.Item<ActionItem>("test-fireball");
        }

        [TearDown]
        public void TearDown()
        {
            m_kit.Dispose();
        }

        /* ============================== Using ============================== */

        [Test]
        [Description("Using a consumable uses up one, and the slot empties when the last one is used.")]
        public void UsingAConsumable_UsesUpOne_AndTheSlotEmptiesAtZero()
        {
            m_actions.AddActionItem(m_potion, 0, 2);

            m_actions.Use(m_player, 0);
            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(1));

            m_actions.Use(m_player, 0);
            Assert.That(m_actions.GetActionItem(0), Is.Null);
            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(0));
        }

        [Test]
        [Description("Using a non-consumable keeps it in its slot.")]
        public void UsingANonConsumable_KeepsItInItsSlot()
        {
            m_actions.AddActionItem(m_fireball, 1, 1);

            m_actions.Use(m_player, 1);

            Assert.That(m_actions.GetActionItem(1), Is.SameAs(m_fireball));
            Assert.That(m_actions.GetQuantity(1), Is.EqualTo(1));
        }

        /* ============================== One slot per ability ============================== */

        [Test]
        [Description("An ability can be in only one slot: placing one that's already on the bar moves it to the new slot.")]
        public void PlacingAnAbilityAlreadyOnTheBar_MovesIt()
        {
            m_actions.AddActionItem(m_fireball, 0, 1);

            m_actions.AddActionItem(m_fireball, 3, 1);

            Assert.That(m_actions.GetActionItem(0), Is.Null);
            Assert.That(m_actions.GetActionItem(3), Is.SameAs(m_fireball));
        }

        /* ============================== Saving ============================== */

        [Test]
        [Description("Capturing the bar and restoring it into a fresh one restores each slot's item and quantity.")]
        public void CaptureThenRestore_RestoresEachSlot()
        {
            m_actions.AddActionItem(m_potion, 0, 3);
            m_actions.AddActionItem(m_fireball, 2, 1);
            GameObject other = m_kit.NewObject("FreshPlayer");
            m_kit.NewInventory(other, 4);
            Actions fresh = other.AddComponent<Actions>();
            InventoryTestKit.InvokeAwake(fresh);

            fresh.RestoreState(m_actions.CaptureState());

            Assert.That(fresh.GetActionItem(0), Is.SameAs(m_potion));
            Assert.That(fresh.GetQuantity(0), Is.EqualTo(3));
            Assert.That(fresh.GetActionItem(2), Is.SameAs(m_fireball));
            Assert.That(fresh.GetActionItem(1), Is.Null);
        }

        [Test]
        [Description("Saved slots with an unknown item or that can't be read are skipped, and the rest load.")]
        public void RestoreState_UnknownOrUnreadableSlots_AreSkipped_TheRestLoad()
        {
            m_actions.AddActionItem(m_potion, 0, 3);
            JObject saved = (JObject)m_actions.CaptureState();
            JToken goodSlot = saved["0"];
            JObject damaged = new JObject
            {
                ["0"] = goodSlot,
                ["1"] = new JObject { ["ItemID"] = "no-such-item", ["Quantity"] = 1 },
                ["first"] = goodSlot,
                ["2"] = "lots",
            };

            Assert.DoesNotThrow(() => m_actions.RestoreState(damaged));

            Assert.That(m_actions.GetActionItem(0), Is.SameAs(m_potion));
            Assert.That(m_actions.GetQuantity(0), Is.EqualTo(3));
            Assert.That(m_actions.GetActionItem(1), Is.Null);
            Assert.That(m_actions.GetActionItem(2), Is.Null);
        }
    }
}
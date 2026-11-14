/*---------------------------
| File: QuestIntegrationTests.cs
| Author: Chandler Mays
----------------------------*/
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
//---------------------------------
using PolyQuest.Pickups;
using PolyQuest.Quests;
using PolyQuest.Saving;

namespace PolyQuest.Tests
{
    // The real Player prefab with the shipped quests, the real SavingSystem and a throwaway save slot.
    public class QuestIntegrationTests
    {
        private const string kHeirloom = "A Family Heirloom";
        private const string kBanditCamp = "Clear the Bandit Camp";

        private PlayerRig m_rig;

        [SetUp]
        public void SetUp()
        {
            m_rig = new PlayerRig();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (Pickup pickup in Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None))
                Object.DestroyImmediate(pickup.gameObject);

            m_rig.Dispose();
        }

        /* --- Helpers --- */

        private QuestManager Quests => m_rig.Player.GetComponent<QuestManager>();

        private static Quest LoadQuest(string name)
        {
            Quest quest = Quest.GetByName(name);
            Assert.That(quest, Is.Not.Null, $"fixture: the '{name}' quest asset is missing from Resources");
            return quest;
        }

        private void CompleteAll(Quest quest)
        {
            foreach (QuestObjective objective in quest.Objectives)
                Quests.CompleteObjective(quest, objective);
        }

        /* ============================== Rewards ============================== */

        [UnityTest]
        [Description("Completing a quest saves the completion together with its reward: loading that save into a fresh Player shows the quest completed and the silver paid.")]
        public IEnumerator CompletingAQuest_SavesTheRewardWithTheCompletion()
        {
            yield return null;
            Quest heirloom = LoadQuest(kHeirloom);
            int silverReward = heirloom.Rewards.Where(r => r.Item is CurrencyItem).Sum(r => r.Amount);
            Assert.That(silverReward, Is.GreaterThan(0), "fixture: the quest must reward silver");
            int silverBefore = m_rig.Wallet.CurrentSilver;
            Quests.AddQuest(heirloom);

            CompleteAll(heirloom);

            m_rig.SpawnPlayer();
            yield return null;
            SaveManager.Instance.Load();

            Assert.That(Quests.IsQuestCompleted(heirloom), Is.True);
            Assert.That(m_rig.Wallet.CurrentSilver, Is.EqualTo(silverBefore + silverReward));
        }

        [UnityTest]
        [Description("An item reward that doesn't fit in a full inventory is dropped in the world for the Player, not lost.")]
        public IEnumerator ItemRewardThatDoesNotFit_IsDroppedInTheWorld()
        {
            yield return null;
            Quest banditCamp = LoadQuest(kBanditCamp);
            Quest.Reward itemReward = banditCamp.Rewards.First(r => r.Item != null && !(r.Item is CurrencyItem));
            for (int guard = 0; m_rig.Inventory.TryAddToAvailableSlot(m_rig.Equipable); ++guard)
                Assert.That(guard, Is.LessThan(m_rig.Inventory.Size), "fixture: filling the inventory is not converging");
            Assert.That(m_rig.QuantityOf(itemReward.Item), Is.EqualTo(0), "fixture: the Player must not already hold the reward item");
            Quests.AddQuest(banditCamp);

            CompleteAll(banditCamp);

            Pickup dropped = Object.FindObjectsByType<Pickup>(FindObjectsSortMode.None).SingleOrDefault(p => p.Item == itemReward.Item);
            Assert.That(dropped, Is.Not.Null, "the reward is in neither the inventory nor the world");
            Assert.That(dropped.Quantity, Is.EqualTo(itemReward.Amount));
        }
    }
}
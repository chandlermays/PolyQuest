/*---------------------------
| File: WalletTests.cs
| Author: Chandler Mays
----------------------------*/
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
//---------------------------------
using PolyQuest.Inventories;

namespace PolyQuest.Tests
{
    // Silver: adding and spending, the update event and the save data.
    public class WalletTests
    {
        private GameObject m_player;
        private Wallet m_wallet;
        private int m_updates;

        [SetUp]
        public void SetUp()
        {
            m_player = new GameObject("Player");
            m_wallet = m_player.AddComponent<Wallet>();
            m_updates = 0;
            m_wallet.OnWalletUpdated += () => ++m_updates;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_player);
        }

        [Test]
        [Description("Adding and spending silver changes the balance and raises OnWalletUpdated each time.")]
        public void AddingAndSpending_ChangeTheBalance_AndRaiseOnWalletUpdated()
        {
            m_wallet.UpdateSilver(50);
            m_wallet.UpdateSilver(-20);

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(30));
            Assert.That(m_updates, Is.EqualTo(2));
        }

        [Test]
        [Description("Capturing the balance and restoring it into a fresh wallet restores it and raises OnWalletUpdated.")]
        public void CaptureThenRestore_RestoresTheBalance()
        {
            m_wallet.UpdateSilver(75);
            Wallet fresh = m_player.AddComponent<Wallet>();
            int freshUpdates = 0;
            fresh.OnWalletUpdated += () => ++freshUpdates;

            fresh.RestoreState(m_wallet.CaptureState());

            Assert.That(fresh.CurrentSilver, Is.EqualTo(75));
            Assert.That(freshUpdates, Is.EqualTo(1));
        }

        [Test]
        [Description("Wallet save data that can't be read is ignored: the balance is unchanged.")]
        public void RestoreState_Unreadable_IsIgnored()
        {
            m_wallet.UpdateSilver(40);

            m_wallet.RestoreState(new JValue("lots"));
            m_wallet.RestoreState(new JObject { ["silver"] = 10 });
            m_wallet.RestoreState(new JArray(1, 2));

            Assert.That(m_wallet.CurrentSilver, Is.EqualTo(40));
        }
    }
}
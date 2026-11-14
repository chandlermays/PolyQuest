/*---------------------------
| File: FactionTests.cs
| Author: Chandler Mays
----------------------------*/
using NUnit.Framework;
//---------------------------------
using PolyQuest.Components;
using UnityEngine;

namespace PolyQuest.Tests
{
    // Who is hostile to whom. Player = the hero, NPC = the guards, Enemy = the bandits, Neutral = no side.
    public class FactionTests
    {
        private GameObject m_go;

        [SetUp]
        public void SetUp()
        {
            m_go = new GameObject("Faction");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(m_go);
        }

        [TestCase(Faction.kPlayer, Faction.kPlayer, false)]
        [TestCase(Faction.kPlayer, Faction.kNPC, false)]
        [TestCase(Faction.kPlayer, Faction.kEnemy, true)]
        [TestCase(Faction.kPlayer, Faction.kNeutral, false)]
        [TestCase(Faction.kNPC, Faction.kPlayer, false)]
        [TestCase(Faction.kNPC, Faction.kNPC, false)]
        [TestCase(Faction.kNPC, Faction.kEnemy, true)]
        [TestCase(Faction.kNPC, Faction.kNeutral, false)]
        [TestCase(Faction.kEnemy, Faction.kPlayer, true)]
        [TestCase(Faction.kEnemy, Faction.kNPC, true)]
        [TestCase(Faction.kEnemy, Faction.kEnemy, false)]
        [TestCase(Faction.kEnemy, Faction.kNeutral, false)]
        [TestCase(Faction.kNeutral, Faction.kPlayer, false)]
        [TestCase(Faction.kNeutral, Faction.kNPC, false)]
        [TestCase(Faction.kNeutral, Faction.kEnemy, false)]
        [TestCase(Faction.kNeutral, Faction.kNeutral, false)]
        [Description("Player and Enemy are hostile to each other, as are NPC and Enemy. Player and NPC are allies, no faction is hostile to itself, and Neutral is hostile to no one.")]
        public void IsHostileTo(Faction mine, Faction theirs, bool hostile)
        {
            FactionComponent faction = EntityTestKit.SetFaction(m_go, mine);

            Assert.That(faction.IsHostileTo(theirs), Is.EqualTo(hostile));
        }
    }
}
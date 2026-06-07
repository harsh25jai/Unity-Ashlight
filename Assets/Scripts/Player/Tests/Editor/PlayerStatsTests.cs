using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Player.Tests
{
    public class PlayerStatsTests
    {
        private PlayerStats _playerStats;

        [SetUp]
        public void SetUp()
        {
            _playerStats = ScriptableObject.CreateInstance<PlayerStats>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerStats);
        }

        [Test]
        public void GetModifiedValue_ReturnsBaseValueWithoutModifiers()
        {
            Assert.AreEqual(100f, _playerStats.GetModifiedValue(100f));
        }

        [Test]
        public void GetModifiedValue_AppliesSingleMultiplier()
        {
            _playerStats.AddModifier("speed-boots", 1.5f);

            Assert.AreEqual(150f, _playerStats.GetModifiedValue(100f));
        }

        [Test]
        public void GetModifiedValue_StacksMultipleMultipliers()
        {
            _playerStats.AddModifier("buff-a", 2f);
            _playerStats.AddModifier("buff-b", 0.5f);

            Assert.AreEqual(100f, _playerStats.GetModifiedValue(100f));
        }

        [Test]
        public void RemoveModifier_RestoresUnmodifiedValue()
        {
            _playerStats.AddModifier("buff", 2f);
            _playerStats.RemoveModifier("buff");

            Assert.AreEqual(100f, _playerStats.GetModifiedValue(100f));
        }

        [Test]
        public void ModifiedMoveSpeed_UsesDefaultBaseValue()
        {
            Assert.AreEqual(4f, _playerStats.ModifiedMoveSpeed);
        }
    }
}

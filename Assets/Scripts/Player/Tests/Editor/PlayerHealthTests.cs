using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Player.Tests
{
    public class PlayerHealthTests
    {
        private PlayerStats _playerStats;
        private GameObject _playerObject;
        private PlayerHealth _playerHealth;

        [SetUp]
        public void SetUp()
        {
            _playerStats = ScriptableObject.CreateInstance<PlayerStats>();
            _playerObject = new GameObject("Player");
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();

            var statsField = typeof(PlayerHealth).GetField(
                "_playerStats",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            statsField?.SetValue(_playerHealth, _playerStats);
            _playerHealth.enabled = true;
            _playerHealth.ResetToFullHealth();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerObject);
            Object.DestroyImmediate(_playerStats);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _playerHealth.TakeDamage(25f);

            Assert.AreEqual(75f, _playerHealth.CurrentHealth);
        }

        [Test]
        public void TakeDamage_TriggersDeathWithoutDestroyingObject()
        {
            bool deathTriggered = false;
            _playerHealth.OnDeath.AddListener(() => deathTriggered = true);

            _playerHealth.TakeDamage(100f);

            Assert.IsTrue(deathTriggered);
            Assert.IsTrue(_playerHealth.IsDead);
            Assert.IsNotNull(_playerObject);
        }

        [Test]
        public void Heal_RestoresHealthUpToMax()
        {
            _playerHealth.TakeDamage(40f);
            _playerHealth.Heal(15f);

            Assert.AreEqual(75f, _playerHealth.CurrentHealth);
        }

        [Test]
        public void TakeDamage_IsBlockedDuringInvincibility()
        {
            _playerHealth.TakeDamage(10f);
            _playerHealth.TakeDamage(10f);

            Assert.AreEqual(90f, _playerHealth.CurrentHealth);
        }
    }
}

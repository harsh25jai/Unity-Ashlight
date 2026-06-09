using Ashlight.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Player.Tests
{
    public class PlayerHealthTests
    {
        private GameObject _playerObject;
        private GameObject _torchObject;
        private HolyTorch _holyTorch;
        private PlayerHealth _playerHealth;

        [SetUp]
        public void SetUp()
        {
            _playerObject = new GameObject("Player");
            _torchObject = new GameObject("Torch");
            _torchObject.transform.SetParent(_playerObject.transform);

            Light pointLight = _torchObject.AddComponent<Light>();
            pointLight.type = LightType.Point;

            GameObject spotObject = new GameObject("SpotLight");
            spotObject.transform.SetParent(_torchObject.transform);
            Light spotLight = spotObject.AddComponent<Light>();
            spotLight.type = LightType.Spot;

            _holyTorch = _torchObject.AddComponent<HolyTorch>();
            _playerHealth = _playerObject.AddComponent<PlayerHealth>();

            var torchField = typeof(PlayerHealth).GetField(
                "holyTorch",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            torchField?.SetValue(_playerHealth, _holyTorch);
            _playerHealth.enabled = true;
            _playerHealth.Revive();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void TakeDamage_ReducesCurrentHealth()
        {
            _playerHealth.TakeDamage(25f);

            Assert.AreEqual(75f, _playerHealth.CurrentHealth);
        }

        [Test]
        public void TakeDamage_SyncsTorchHealthModifier()
        {
            _playerHealth.TakeDamage(50f);

            Assert.AreEqual(0.5f, _holyTorch.HealthModifier, 0.001f);
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
        public void HealthPercent_ReturnsNormalizedValue()
        {
            _playerHealth.TakeDamage(25f);

            Assert.AreEqual(0.75f, _playerHealth.HealthPercent, 0.001f);
        }
    }
}

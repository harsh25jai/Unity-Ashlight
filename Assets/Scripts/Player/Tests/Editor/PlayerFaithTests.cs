using Ashlight.Systems;
using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Player.Tests
{
    public class PlayerFaithTests
    {
        private GameObject _playerObject;
        private GameObject _torchObject;
        private HolyTorch _holyTorch;
        private PlayerFaith _playerFaith;

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
            _playerFaith = _playerObject.AddComponent<PlayerFaith>();

            var torchField = typeof(PlayerFaith).GetField(
                "holyTorch",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            torchField?.SetValue(_playerFaith, _holyTorch);
            _playerFaith.enabled = true;
            _playerFaith.ResetToSavedFaith(PlayerFaith.DefaultMaxFaith);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_playerObject);
        }

        [Test]
        public void TakeDamage_ReducesCurrentFaith()
        {
            _playerFaith.TakeDamage(25f);

            Assert.AreEqual(75f, _playerFaith.CurrentFaith);
        }

        [Test]
        public void TakeDamage_SyncsTorchFaithModifier()
        {
            _playerFaith.TakeDamage(50f);

            Assert.AreEqual(0.5f, _holyTorch.HealthModifier, 0.001f);
        }

        [Test]
        public void TakeDamage_TriggersDepletionWithoutDestroyingObject()
        {
            bool depletedTriggered = false;
            _playerFaith.OnFaithDepleted.AddListener(() => depletedTriggered = true);

            _playerFaith.TakeDamage(100f);

            Assert.IsTrue(depletedTriggered);
            Assert.IsTrue(_playerFaith.IsDepleted);
            Assert.IsNotNull(_playerObject);
        }

        [Test]
        public void RestoreFaith_RestoresUpToMax()
        {
            _playerFaith.TakeDamage(40f);
            _playerFaith.RestoreFaith(15f);

            Assert.AreEqual(75f, _playerFaith.CurrentFaith);
        }

        [Test]
        public void FaithPercent_ReturnsNormalizedValue()
        {
            _playerFaith.TakeDamage(25f);

            Assert.AreEqual(0.75f, _playerFaith.FaithPercent, 0.001f);
        }
    }
}

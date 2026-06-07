using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Systems.Tests
{
    public class HolyWaterInventoryTests
    {
        private HolyWaterInventory _inventory;

        [SetUp]
        public void SetUp()
        {
            _inventory = ScriptableObject.CreateInstance<HolyWaterInventory>();
            _inventory.ResetToFull();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_inventory);
        }

        [Test]
        public void Spend_ReturnsFalseWhenInsufficient()
        {
            Assert.IsFalse(_inventory.Spend(150f));
            Assert.AreEqual(100f, _inventory.Current);
        }

        [Test]
        public void Spend_ReducesCurrentAndClampsToZero()
        {
            Assert.IsTrue(_inventory.Spend(100f));

            Assert.AreEqual(0f, _inventory.Current);
        }

        [Test]
        public void Replenish_ClampsToMaxCapacity()
        {
            _inventory.Spend(50f);
            _inventory.Replenish(100f);

            Assert.AreEqual(100f, _inventory.Current);
        }

        [Test]
        public void FillPercent_ReturnsNormalizedValue()
        {
            _inventory.Spend(50f);

            Assert.AreEqual(0.5f, _inventory.FillPercent, 0.001f);
        }
    }
}

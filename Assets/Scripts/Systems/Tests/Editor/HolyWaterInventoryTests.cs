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
            _inventory.ResetToDefault();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_inventory);
        }

        [Test]
        public void AddBottle_FailsWhenCapacityIsZero()
        {
            Assert.IsFalse(_inventory.AddBottle());
            Assert.AreEqual(0, _inventory.CurrentBottles);
        }

        [Test]
        public void IncreaseCapacity_AllowsPickupUpToAbsoluteMax()
        {
            _inventory.IncreaseCapacity(2);
            Assert.IsTrue(_inventory.AddBottle());
            Assert.IsTrue(_inventory.AddBottle());
            Assert.IsFalse(_inventory.AddBottle());
            Assert.AreEqual(2, _inventory.CurrentBottles);
        }

        [Test]
        public void ConsumeBottle_FiresCountChange()
        {
            _inventory.IncreaseCapacity(1);
            _inventory.AddBottle();

            Assert.IsTrue(_inventory.ConsumeBottle());
            Assert.AreEqual(0, _inventory.CurrentBottles);
        }

        [Test]
        public void BreakBottle_ReducesCountWithoutConsumeEventOverlap()
        {
            int brokenCount = 0;
            _inventory.OnBottleBroken.AddListener(() => brokenCount++);
            _inventory.IncreaseCapacity(1);
            _inventory.AddBottle();

            Assert.IsTrue(_inventory.BreakBottle());
            Assert.AreEqual(1, brokenCount);
            Assert.AreEqual(0, _inventory.CurrentBottles);
        }

        [Test]
        public void BottleFillPercent_ReturnsNormalizedValue()
        {
            _inventory.SetState(2, 4);

            Assert.AreEqual(0.5f, _inventory.BottleFillPercent, 0.001f);
        }

        [Test]
        public void IncreaseCapacity_ClampsToAbsoluteMax()
        {
            _inventory.IncreaseCapacity(10);

            Assert.AreEqual(HolyWaterInventory.AbsoluteMaxBottles, _inventory.MaxBottles);
        }
    }
}

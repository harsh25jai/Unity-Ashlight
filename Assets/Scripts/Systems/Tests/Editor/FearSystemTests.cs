using NUnit.Framework;

namespace Ashlight.Systems.Tests
{
    public class FearSystemTests
    {
        [Test]
        public void CalculateFearFromDistance_ReturnsZeroBeyondFifteenUnits()
        {
            Assert.AreEqual(0f, FearSystem.CalculateFearFromDistance(15f), 0.001f);
            Assert.AreEqual(0f, FearSystem.CalculateFearFromDistance(30f), 0.001f);
        }

        [Test]
        public void CalculateFearFromDistance_ReturnsOneWithinOneUnit()
        {
            Assert.AreEqual(1f, FearSystem.CalculateFearFromDistance(1f), 0.001f);
            Assert.AreEqual(1f, FearSystem.CalculateFearFromDistance(0.5f), 0.001f);
        }

        [Test]
        public void CalculateFearFromDistance_InterpolatesBetweenThresholds()
        {
            float midFear = FearSystem.CalculateFearFromDistance(8f);
            Assert.Greater(midFear, 0f);
            Assert.Less(midFear, 1f);
        }
    }
}

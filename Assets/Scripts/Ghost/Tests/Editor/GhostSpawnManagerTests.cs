using Ashlight.Environment;
using NUnit.Framework;

namespace Ashlight.Ghost.Tests
{
    public class GhostSpawnManagerTests
    {
        [Test]
        public void GetMaxActiveGhostsForPhase_ReturnsExpectedLimits()
        {
            Assert.AreEqual(3, GhostSpawnManager.GetMaxActiveGhostsForPhase(DayNightPhase.Day));
            Assert.AreEqual(6, GhostSpawnManager.GetMaxActiveGhostsForPhase(DayNightPhase.Night_Early));
            Assert.AreEqual(int.MaxValue, GhostSpawnManager.GetMaxActiveGhostsForPhase(DayNightPhase.Night_Deep));
        }
    }
}

using Ashlight.Environment;
using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class HUDManagerTests
    {
        [Test]
        public void FormatHealthDisplay_RoundsUpAndShowsMax()
        {
            Assert.AreEqual("86 / 100", HUDManager.FormatHealthDisplay(85.2f, 100f));
            Assert.AreEqual("1 / 100", HUDManager.FormatHealthDisplay(0.4f, 100f));
        }

        [Test]
        public void FormatNightCount_ShowsNightNumber()
        {
            Assert.AreEqual("Night 3", HUDManager.FormatNightCount(3));
        }

        [Test]
        public void FormatTorchFuel_ShowsPercentage()
        {
            Assert.IsTrue(HUDManager.FormatTorchFuel(0.85f).Contains("85%"));
        }

        [Test]
        public void FormatNightPhase_MapsKnownPhases()
        {
            Assert.AreEqual("Day", HUDManager.FormatNightPhase(DayNightPhase.Day));
            Assert.AreEqual("Night — Deep", HUDManager.FormatNightPhase(DayNightPhase.Night_Deep));
        }
    }
}

using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class UpgradeStationUITests
    {
        [Test]
        public void FormatUpgradeFailureReason_MapsKnownCodes()
        {
            Assert.AreEqual("INSUFFICIENT FAITH", UpgradeStationUI.FormatUpgradeFailureReason("insufficient_faith"));
            Assert.AreEqual("LOCKED", UpgradeStationUI.FormatUpgradeFailureReason("prerequisite_missing"));
            Assert.AreEqual("OWNED", UpgradeStationUI.FormatUpgradeFailureReason("already_purchased"));
        }
    }
}

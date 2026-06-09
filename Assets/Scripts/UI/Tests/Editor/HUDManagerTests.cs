using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class HUDManagerTests
    {
        [Test]
        public void FormatHealthDisplay_RoundsUpAndShowsMax()
        {
            Assert.AreEqual("85 / 100", HUDManager.FormatHealthDisplay(85.2f, 100f));
            Assert.AreEqual("1 / 100", HUDManager.FormatHealthDisplay(0.4f, 100f));
        }
    }
}

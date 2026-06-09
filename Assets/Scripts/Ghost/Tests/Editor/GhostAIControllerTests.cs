using NUnit.Framework;

namespace Ashlight.Ghost.Tests
{
    public class GhostAIControllerTests
    {
        [Test]
        public void ShouldRetreatFromLight_UsesConfiguredThreshold()
        {
            Assert.IsTrue(GhostAIController.ShouldRetreatFromLight(0.8f, 0.7f));
            Assert.IsFalse(GhostAIController.ShouldRetreatFromLight(0.6f, 0.7f));
        }
    }
}

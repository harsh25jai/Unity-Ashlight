using NUnit.Framework;

namespace Ashlight.Ghost.Tests
{
    public class GhostAIControllerTests
    {
        [Test]
        public void ShouldRetreatFromLight_UsesLightResistanceThreshold()
        {
            Assert.IsTrue(GhostAIController.ShouldRetreatFromLight(0.6f, 0.5f));
            Assert.IsFalse(GhostAIController.ShouldRetreatFromLight(0.4f, 0.5f));
        }
    }
}

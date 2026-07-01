using NUnit.Framework;

namespace Ashlight.Ghost.Tests
{
    public class GrabberGhostTests
    {
        [Test]
        public void GrabberGhost_TypeExists()
        {
            Assert.NotNull(typeof(GrabberGhost));
        }

        [Test]
        public void GhostState_IncludesGrabbing()
        {
            Assert.IsTrue(System.Enum.IsDefined(typeof(GhostState), GhostState.Grabbing));
        }
    }
}

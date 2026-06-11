using Ashlight.Ghost;
using NUnit.Framework;

namespace Ashlight.UI.Tests
{
    public class GhostHealthBarTests
    {
        [Test]
        public void GhostState_CombatStates_AreDefinedForHealthBarVisibility()
        {
            Assert.AreNotEqual(GhostState.Stalk, GhostState.Idle);
            Assert.AreNotEqual(GhostState.Chase, GhostState.Wander);
            Assert.AreNotEqual(GhostState.Attack, GhostState.Idle);
            Assert.AreNotEqual(GhostState.Retreat, GhostState.Wander);
            Assert.AreNotEqual(GhostState.Recharge, GhostState.Idle);
        }
    }
}

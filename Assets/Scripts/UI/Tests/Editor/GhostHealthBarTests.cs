using Ashlight.Ghost;
using NUnit.Framework;
using UnityEngine;

namespace Ashlight.UI.Tests
{
    public class GhostHealthBarTests
    {
        [Test]
        public void ShouldShowForState_IsHiddenDuringIdleAndWander()
        {
            Assert.IsFalse(GhostHealthBar.ShouldShowForState(GhostState.Idle));
            Assert.IsFalse(GhostHealthBar.ShouldShowForState(GhostState.Wander));
        }

        [Test]
        public void ShouldShowForState_IsVisibleDuringCombatStates()
        {
            Assert.IsTrue(GhostHealthBar.ShouldShowForState(GhostState.Stalk));
            Assert.IsTrue(GhostHealthBar.ShouldShowForState(GhostState.Chase));
            Assert.IsTrue(GhostHealthBar.ShouldShowForState(GhostState.Attack));
            Assert.IsTrue(GhostHealthBar.ShouldShowForState(GhostState.Retreat));
            Assert.IsTrue(GhostHealthBar.ShouldShowForState(GhostState.Recharge));
        }

        [Test]
        public void GetColorForHealthPercent_UsesThresholdColors()
        {
            Color green = GhostHealthBar.GetColorForHealthPercent(0.8f);
            Color yellow = GhostHealthBar.GetColorForHealthPercent(0.45f);
            Color red = GhostHealthBar.GetColorForHealthPercent(0.1f);

            Assert.Greater(green.g, green.r);
            Assert.Greater(yellow.r, red.r);
            Assert.Less(red.g, green.g);
        }
    }
}

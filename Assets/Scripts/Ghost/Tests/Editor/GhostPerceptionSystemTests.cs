using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Ghost.Tests
{
    public class GhostPerceptionSystemTests
    {
        [Test]
        public void EvaluatePerception_ReturnsUnawareOutsideRange()
        {
            Vector3 ghostPosition = Vector3.zero;
            Vector3 playerPosition = new Vector3(20f, 0f, 0f);

            PerceptionLevel level = GhostPerceptionSystem.EvaluatePerception(
                ghostPosition,
                Vector3.forward,
                playerPosition,
                detectionRange: 10f,
                lightResistance: 0.5f,
                torchFuelPercent: 0f);

            Assert.AreEqual(PerceptionLevel.Unaware, level);
        }

        [Test]
        public void EvaluatePerception_ReturnsDetectedWhenCloseAndInArc()
        {
            Vector3 ghostPosition = Vector3.zero;
            Vector3 playerPosition = new Vector3(2f, 0f, 0f);

            PerceptionLevel level = GhostPerceptionSystem.EvaluatePerception(
                ghostPosition,
                Vector3.forward,
                playerPosition,
                detectionRange: 10f,
                lightResistance: 0f,
                torchFuelPercent: 0f);

            Assert.AreEqual(PerceptionLevel.Detected, level);
        }

        [Test]
        public void EvaluatePerception_ReducesRangeWhenTorchFuelIsHigh()
        {
            Vector3 ghostPosition = Vector3.zero;
            Vector3 playerPosition = new Vector3(8f, 0f, 0f);

            PerceptionLevel withoutTorch = GhostPerceptionSystem.EvaluatePerception(
                ghostPosition,
                Vector3.forward,
                playerPosition,
                detectionRange: 10f,
                lightResistance: 0.5f,
                torchFuelPercent: 0f);

            PerceptionLevel withTorch = GhostPerceptionSystem.EvaluatePerception(
                ghostPosition,
                Vector3.forward,
                playerPosition,
                detectionRange: 10f,
                lightResistance: 0.5f,
                torchFuelPercent: 0.8f);

            Assert.AreNotEqual(withoutTorch, withTorch);
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Environment.Tests
{
    public class IsometricCameraControllerTests
    {
        [Test]
        public void ClampPositionToBounds_ClampsAxesOutsideRange()
        {
            var min = new Vector3(-10f, 0f, -10f);
            var max = new Vector3(10f, 20f, 10f);
            var outside = new Vector3(100f, -5f, 0f);

            Vector3 result = IsometricCameraController.ClampPositionToBounds(outside, min, max);

            Assert.AreEqual(new Vector3(10f, 0f, 0f), result);
        }

        [Test]
        public void ClampPositionToBounds_PreservesPositionInsideRange()
        {
            var min = new Vector3(-10f, 0f, -10f);
            var max = new Vector3(10f, 20f, 10f);
            var inside = new Vector3(3f, 12f, -4f);

            Vector3 result = IsometricCameraController.ClampPositionToBounds(inside, min, max);

            Assert.AreEqual(inside, result);
        }

        [Test]
        public void ClampPositionToBounds_HandlesInvertedMinMax()
        {
            var min = new Vector3(10f, 20f, 10f);
            var max = new Vector3(-10f, 0f, -10f);
            var position = new Vector3(100f, 15f, -100f);

            Vector3 result = IsometricCameraController.ClampPositionToBounds(position, min, max);

            Assert.AreEqual(new Vector3(-10f, 15f, -10f), result);
        }
    }
}

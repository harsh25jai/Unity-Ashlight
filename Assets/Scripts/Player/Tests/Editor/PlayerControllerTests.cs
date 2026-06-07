using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Player.Tests
{
    public class PlayerControllerTests
    {
        [Test]
        public void ConvertToIsometricDirection_RotatesForwardInputBy45Degrees()
        {
            Vector3 result = PlayerController.ConvertToIsometricDirection(Vector2.up);

            Assert.AreEqual(0.70710677f, result.x, 0.001f);
            Assert.AreEqual(0f, result.y, 0.001f);
            Assert.AreEqual(0.70710677f, result.z, 0.001f);
        }

        [Test]
        public void ConvertToIsometricDirection_ReturnsZeroForNoInput()
        {
            Vector3 result = PlayerController.ConvertToIsometricDirection(Vector2.zero);

            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void ConvertToIsometricDirection_NormalizesDiagonalInput()
        {
            Vector2 diagonal = new Vector2(1f, 1f);
            Vector3 result = PlayerController.ConvertToIsometricDirection(diagonal);

            Assert.AreEqual(1f, result.magnitude, 0.001f);
        }
    }
}

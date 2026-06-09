using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Environment.Tests
{
    public class ChurchSafeZoneTests
    {
        [Test]
        public void GhostRepelRadius_IsTwentyUnits()
        {
            Vector3 origin = Vector3.zero;
            Vector3 inside = new Vector3(15f, 0f, 0f);
            Vector3 outside = new Vector3(25f, 0f, 0f);

            Assert.LessOrEqual(Vector3.Distance(origin, inside), 20f);
            Assert.Greater(Vector3.Distance(origin, outside), 20f);
        }
    }
}

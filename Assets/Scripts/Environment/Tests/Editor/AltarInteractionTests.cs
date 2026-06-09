using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Environment.Tests
{
    public class AltarInteractionTests
    {
        [Test]
        public void CooldownRemaining_IsZeroWhenReady()
        {
            AltarInteraction altar = new GameObject(nameof(AltarInteraction)).AddComponent<AltarInteraction>();

            try
            {
                Assert.IsTrue(altar.IsBlessingReady);
                Assert.AreEqual(0f, altar.CooldownRemaining);
            }
            finally
            {
                Object.DestroyImmediate(altar.gameObject);
            }
        }
    }
}

using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Ghost.Tests
{
    public class GhostTypeDefinitionTests
    {
        [Test]
        public void DefaultValues_MatchDesignDefaults()
        {
            GhostTypeDefinition definition = ScriptableObject.CreateInstance<GhostTypeDefinition>();

            Assert.AreEqual(3f, definition.MoveSpeed, 0.001f);
            Assert.AreEqual(10f, definition.Damage, 0.001f);
            Assert.AreEqual(10f, definition.DetectionRange, 0.001f);
            Assert.AreEqual(1.5f, definition.AttackRange, 0.001f);
            Assert.AreEqual(5f, definition.RetreatSpeed, 0.001f);

            Object.DestroyImmediate(definition);
        }
    }
}

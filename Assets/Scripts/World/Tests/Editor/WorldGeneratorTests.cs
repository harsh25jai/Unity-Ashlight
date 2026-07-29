using NUnit.Framework;
using UnityEngine;

namespace Ashlight.World.Tests
{
    public class WorldGeneratorTests
    {
        [Test]
        public void GetChunkSeed_IsDeterministicForSameInputs()
        {
            WorldGenerator worldGenerator = CreateWorldGenerator(masterSeed: 12345, randomise: false);

            try
            {
                int first = worldGenerator.GetChunkSeed(new Vector2Int(2, 5));
                int second = worldGenerator.GetChunkSeed(new Vector2Int(2, 5));

                Assert.AreEqual(first, second);
            }
            finally
            {
                Object.DestroyImmediate(worldGenerator.gameObject);
            }
        }

        [Test]
        public void GetChurchRule_ReturnsMediumPresetByDefault()
        {
            WorldGenerator worldGenerator = CreateWorldGenerator(masterSeed: 1, randomise: false);

            try
            {
                ChurchPlacementRule rule = worldGenerator.GetChurchRule();

                Assert.AreEqual(6, rule.minDistanceFromStart);
                Assert.AreEqual(8, rule.maxDistanceFromStart);
                Assert.AreEqual(5, rule.spacingBetweenChurches);
            }
            finally
            {
                Object.DestroyImmediate(worldGenerator.gameObject);
            }
        }

        [Test]
        public void WorldToChunkCoord_UsesTwentyUnitGrid()
        {
            WorldGenerator worldGenerator = CreateWorldGenerator(masterSeed: 1, randomise: false);

            try
            {
                Vector2Int coord = worldGenerator.WorldToChunkCoord(new Vector3(41f, 0f, -19f));

                Assert.AreEqual(new Vector2Int(2, -1), coord);
            }
            finally
            {
                Object.DestroyImmediate(worldGenerator.gameObject);
            }
        }

        private static WorldGenerator CreateWorldGenerator(int masterSeed, bool randomise)
        {
            GameObject worldObject = new GameObject(nameof(WorldGenerator));
            WorldGenerator worldGenerator = worldObject.AddComponent<WorldGenerator>();

            var masterSeedField = typeof(WorldGenerator).GetField(
                "masterSeed",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            masterSeedField?.SetValue(worldGenerator, masterSeed);

            var randomiseField = typeof(WorldGenerator).GetField(
                "randomiseSeedOnNewGame",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            randomiseField?.SetValue(worldGenerator, randomise);

            var awakeMethod = typeof(WorldGenerator).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            awakeMethod?.Invoke(worldGenerator, null);

            return worldGenerator;
        }
    }
}

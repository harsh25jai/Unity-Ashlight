using NUnit.Framework;
using UnityEngine;

namespace Ashlight.World.Tests
{
    public class ChunkLoaderTests
    {
        [Test]
        public void ChunkCoordToWorldOrigin_MatchesWorldGeneratorGrid()
        {
            GameObject worldObject = new GameObject(nameof(WorldGenerator));
            WorldGenerator worldGenerator = worldObject.AddComponent<WorldGenerator>();

            try
            {
                InvokeAwake(worldGenerator);
                Vector3 origin = worldGenerator.ChunkCoordToWorldOrigin(new Vector2Int(3, -2));

                Assert.AreEqual(new Vector3(60f, 0f, -40f), origin);
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
            }
        }

        [Test]
        public void WorldToChunkCoord_PlayerSpawn_IsOriginChunk()
        {
            GameObject worldObject = new GameObject(nameof(WorldGenerator));
            WorldGenerator worldGenerator = worldObject.AddComponent<WorldGenerator>();

            try
            {
                InvokeAwake(worldGenerator);
                Vector3 playerSpawn = new Vector3(0.35356f, 0.24749f, 0.35356f);

                Assert.AreEqual(Vector2Int.zero, worldGenerator.WorldToChunkCoord(playerSpawn));
            }
            finally
            {
                Object.DestroyImmediate(worldObject);
            }
        }

        private static void InvokeAwake(WorldGenerator generator)
        {
            var method = typeof(WorldGenerator).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            method?.Invoke(generator, null);
        }
    }
}

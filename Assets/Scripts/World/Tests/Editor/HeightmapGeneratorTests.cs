using NUnit.Framework;
using UnityEngine;

namespace Ashlight.World.Tests
{
    public class HeightmapGeneratorTests
    {
        [Test]
        public void Generate_ReturnsTwentyOneByTwentyOneGrid()
        {
            float[,] heightMap = HeightmapGenerator.Generate(Vector2Int.zero, 42);

            Assert.AreEqual(ChunkData.HeightMapResolution, heightMap.GetLength(0));
            Assert.AreEqual(ChunkData.HeightMapResolution, heightMap.GetLength(1));
        }

        [Test]
        public void Generate_IsDeterministicForSameSeed()
        {
            float[,] first = HeightmapGenerator.Generate(new Vector2Int(2, 3), 999);
            float[,] second = HeightmapGenerator.Generate(new Vector2Int(2, 3), 999);

            Assert.AreEqual(first[5, 7], second[5, 7]);
        }

        [Test]
        public void SmoothChunkEdges_BlendsSharedNorthEdge()
        {
            var heightMap = new float[ChunkData.HeightMapResolution, ChunkData.HeightMapResolution];
            var north = new float[ChunkData.HeightMapResolution, ChunkData.HeightMapResolution];

            heightMap[4, 20] = 10f;
            north[4, 0] = 2f;

            HeightmapGenerator.SmoothChunkEdges(heightMap, north, null, null, null);

            Assert.AreEqual(6f, heightMap[4, 20]);
            Assert.AreEqual(6f, north[4, 0]);
        }

        [Test]
        public void ClampSlopes_ReducesAdjacentHeightSteps()
        {
            var heightMap = new float[ChunkData.HeightMapResolution, ChunkData.HeightMapResolution];
            heightMap[5, 5] = 0f;
            heightMap[6, 5] = 2f;

            float[,] clamped = HeightmapGenerator.ClampSlopes(heightMap, maxHeightDelta: 0.3f);

            Assert.LessOrEqual(Mathf.Abs(clamped[6, 5] - clamped[5, 5]), 0.3f + 0.001f);
        }

        [Test]
        public void IsTooSteep_ReturnsTrueOnSteepGradient()
        {
            var heightMap = new float[ChunkData.HeightMapResolution, ChunkData.HeightMapResolution];
            for (int z = 0; z < ChunkData.HeightMapResolution; z++)
            {
                for (int x = 0; x < ChunkData.HeightMapResolution; x++)
                {
                    heightMap[x, z] = x * 3f;
                }
            }

            Assert.IsTrue(HeightmapGenerator.IsTooSteep(heightMap, 10, 10, 35f));
        }
    }
}

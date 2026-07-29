using NUnit.Framework;
using UnityEngine;

namespace Ashlight.World.Tests
{
    public class TerrainChunkUnityTests
    {
        [Test]
        public void Initialise_CreatesTerrainWithExpectedResolution()
        {
            var data = new ChunkData
            {
                chunkCoord = Vector2Int.zero,
                heightScale = 8f,
                heightMap = HeightmapGenerator.Generate(Vector2Int.zero, 101)
            };

            GameObject chunkObject = new GameObject("Chunk_Test");
            TerrainChunkUnity terrainChunk = chunkObject.AddComponent<TerrainChunkUnity>();

            try
            {
                terrainChunk.Initialise(data, null);

                Assert.IsNotNull(terrainChunk.UnityTerrain);
                Assert.AreEqual(33, terrainChunk.UnityTerrain.terrainData.heightmapResolution);
                Assert.AreEqual(
                    new Vector3(20f, ChunkData.DefaultHeightScale, 20f),
                    terrainChunk.UnityTerrain.terrainData.size);
                Assert.IsTrue(terrainChunk.HasStandardTerrainMetrics());
            }
            finally
            {
                Object.DestroyImmediate(chunkObject);
            }
        }

        [Test]
        public void GetHeightAtWorldPos_ReturnsSampledTerrainHeight()
        {
            var data = new ChunkData
            {
                chunkCoord = Vector2Int.zero,
                heightScale = 8f,
                heightMap = HeightmapGenerator.Generate(Vector2Int.zero, 101)
            };

            GameObject chunkObject = new GameObject("Chunk_Test");
            TerrainChunkUnity terrainChunk = chunkObject.AddComponent<TerrainChunkUnity>();

            try
            {
                terrainChunk.Initialise(data, null);
                float height = terrainChunk.GetHeightAtWorldPos(10f, 10f);

                Assert.GreaterOrEqual(height, 0f);
            }
            finally
            {
                Object.DestroyImmediate(chunkObject);
            }
        }
    }
}

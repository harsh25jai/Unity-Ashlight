using System;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Thread-safe heightmap generation using layered Perlin noise.
    /// </summary>
    public static class HeightmapGenerator
    {
        private const int Resolution = ChunkData.HeightMapResolution - 1;
        private const int VertexCount = ChunkData.HeightMapResolution;
        private const float ChunkWorldSize = ChunkData.ChunkSize;
        private const float MaxOctaveSum = 1.875f;
        private const float StartFlattenRadius = 25f;

        /// <summary>
        /// Generates a 21×21 heightmap for a chunk coordinate.
        /// </summary>
        /// <param name="chunkCoord">Chunk grid coordinate.</param>
        /// <param name="masterSeed">World master seed.</param>
        /// <param name="heightScale">Maximum height variation multiplier.</param>
        /// <param name="flattenForStart">Whether to flatten the starting area.</param>
        /// <returns>Height samples indexed by [x, z].</returns>
        public static float[,] Generate(
            Vector2Int chunkCoord,
            int masterSeed,
            float heightScale = ChunkData.DefaultHeightScale,
            bool flattenForStart = false)
        {
            var heightMap = new float[VertexCount, VertexCount];

            float seedOffsetX = masterSeed % 10000 * 0.1f;
            float seedOffsetZ = masterSeed / 10000f * 0.1f;
            float chunkWorldX = chunkCoord.x * ChunkWorldSize;
            float chunkWorldZ = chunkCoord.y * ChunkWorldSize;

            for (int z = 0; z < VertexCount; z++)
            {
                for (int x = 0; x < VertexCount; x++)
                {
                    float wx = chunkWorldX + x + seedOffsetX;
                    float wz = chunkWorldZ + z + seedOffsetZ;

                    float height = 0f;

                    float largeScaleFrequency = 0.008f;
                    float largeScaleSample = PerlinNoise(
                        wx * largeScaleFrequency,
                        wz * largeScaleFrequency);
                    height += largeScaleSample * 1.4f;

                    float amplitude = 1f;
                    float frequency = 0.05f;
                    const float persistence = 0.5f;
                    float detailSum = 0f;

                    for (int octave = 0; octave < 4; octave++)
                    {
                        detailSum += PerlinNoise(wx * frequency, wz * frequency) * amplitude;
                        amplitude *= persistence;
                        frequency *= 2f;
                    }

                    height += detailSum * 0.6f;

                    const float NewMaxSum = 1.4f + (MaxOctaveSum * 0.6f);
                    height = height / NewMaxSum * heightScale;

                    if (flattenForStart)
                    {
                        float dx = x - 10f;
                        float dz = z - 10f;
                        float distFromChunkCentre = MathF.Sqrt(dx * dx + dz * dz);
                        float flattenFactor = SmoothStep(0f, 1f, distFromChunkCentre / StartFlattenRadius);
                        height *= flattenFactor;
                    }

                    heightMap[x, z] = height;
                }
            }

            return heightMap;
        }

        /// <summary>
        /// Applies neighbour averaging to soften local height variation.
        /// </summary>
        /// <param name="heightMap">Source height samples.</param>
        /// <param name="iterations">Number of smoothing passes.</param>
        /// <returns>Smoothed height samples.</returns>
        public static float[,] SmoothHeightmap(float[,] heightMap, int iterations = 1)
        {
            if (heightMap == null || iterations <= 0)
            {
                return heightMap;
            }

            int size = heightMap.GetLength(0);
            float[,] result = (float[,])heightMap.Clone();

            for (int pass = 0; pass < iterations; pass++)
            {
                float[,] next = (float[,])result.Clone();

                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        float sum = 0f;
                        int count = 0;

                        for (int dx = -1; dx <= 1; dx++)
                        {
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                int sampleX = x + dx;
                                int sampleZ = z + dz;
                                if (sampleX < 0 || sampleZ < 0 || sampleX >= size || sampleZ >= size)
                                {
                                    continue;
                                }

                                sum += result[sampleX, sampleZ];
                                count++;
                            }
                        }

                        next[x, z] = count > 0 ? sum / count : result[x, z];
                    }
                }

                result = next;
            }

            return result;
        }

        /// <summary>
        /// Relaxes adjacent height differences that exceed a walkable step limit.
        /// </summary>
        /// <param name="heightMap">Source height samples.</param>
        /// <param name="maxHeightDelta">Maximum allowed height step between neighbours.</param>
        /// <returns>Height samples with clamped slopes.</returns>
        public static float[,] ClampSlopes(float[,] heightMap, float maxHeightDelta = 0.35f)
        {
            if (heightMap == null)
            {
                return heightMap;
            }

            int size = heightMap.GetLength(0);
            float[,] result = (float[,])heightMap.Clone();

            for (int pass = 0; pass < 3; pass++)
            {
                for (int x = 0; x < size; x++)
                {
                    for (int z = 0; z < size; z++)
                    {
                        if (x < size - 1)
                        {
                            float diff = result[x + 1, z] - result[x, z];
                            if (Mathf.Abs(diff) > maxHeightDelta)
                            {
                                float excess = (Mathf.Abs(diff) - maxHeightDelta) * 0.5f * Mathf.Sign(diff);
                                result[x + 1, z] -= excess;
                                result[x, z] += excess;
                            }
                        }

                        if (z < size - 1)
                        {
                            float diff = result[x, z + 1] - result[x, z];
                            if (Mathf.Abs(diff) > maxHeightDelta)
                            {
                                float excess = (Mathf.Abs(diff) - maxHeightDelta) * 0.5f * Mathf.Sign(diff);
                                result[x, z + 1] -= excess;
                                result[x, z] += excess;
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Blends shared edge heights between adjacent chunk heightmaps.
        /// </summary>
        /// <param name="heightMap">Heightmap being stitched.</param>
        /// <param name="northNeighbour">Chunk to the north, or null.</param>
        /// <param name="eastNeighbour">Chunk to the east, or null.</param>
        /// <param name="southNeighbour">Chunk to the south, or null.</param>
        /// <param name="westNeighbour">Chunk to the west, or null.</param>
        public static void SmoothChunkEdges(
            float[,] heightMap,
            float[,] northNeighbour,
            float[,] eastNeighbour,
            float[,] southNeighbour,
            float[,] westNeighbour)
        {
            if (heightMap == null)
            {
                return;
            }

            if (northNeighbour != null)
            {
                for (int x = 0; x < VertexCount; x++)
                {
                    float blended = (heightMap[x, Resolution] + northNeighbour[x, 0]) * 0.5f;
                    heightMap[x, Resolution] = blended;
                    northNeighbour[x, 0] = blended;
                }
            }

            if (eastNeighbour != null)
            {
                for (int z = 0; z < VertexCount; z++)
                {
                    float blended = (heightMap[Resolution, z] + eastNeighbour[0, z]) * 0.5f;
                    heightMap[Resolution, z] = blended;
                    eastNeighbour[0, z] = blended;
                }
            }

            if (southNeighbour != null)
            {
                for (int x = 0; x < VertexCount; x++)
                {
                    float blended = (heightMap[x, 0] + southNeighbour[x, Resolution]) * 0.5f;
                    heightMap[x, 0] = blended;
                    southNeighbour[x, Resolution] = blended;
                }
            }

            if (westNeighbour != null)
            {
                for (int z = 0; z < VertexCount; z++)
                {
                    float blended = (heightMap[0, z] + westNeighbour[Resolution, z]) * 0.5f;
                    heightMap[0, z] = blended;
                    westNeighbour[Resolution, z] = blended;
                }
            }
        }

        /// <summary>
        /// Returns the slope angle in degrees at a heightmap sample.
        /// </summary>
        /// <param name="heightMap">Height samples.</param>
        /// <param name="x">X sample index.</param>
        /// <param name="z">Z sample index.</param>
        /// <returns>Slope angle in degrees.</returns>
        public static float GetSlopeAngle(float[,] heightMap, int x, int z)
        {
            x = Math.Clamp(x, 1, Resolution - 1);
            z = Math.Clamp(z, 1, Resolution - 1);

            float dx = heightMap[x + 1, z] - heightMap[x - 1, z];
            float dz = heightMap[x, z + 1] - heightMap[x, z - 1];
            float gradient = MathF.Sqrt(dx * dx + dz * dz) * 0.5f;
            return MathF.Atan(gradient) * (180f / MathF.PI);
        }

        /// <summary>
        /// Returns whether a heightmap sample exceeds a walkable slope limit.
        /// </summary>
        /// <param name="heightMap">Height samples.</param>
        /// <param name="x">X sample index.</param>
        /// <param name="z">Z sample index.</param>
        /// <param name="maxSlopeDegrees">Maximum walkable slope.</param>
        /// <returns>True when the slope is too steep.</returns>
        public static bool IsTooSteep(
            float[,] heightMap,
            int x,
            int z,
            float maxSlopeDegrees = 35f)
        {
            return GetSlopeAngle(heightMap, x, z) > maxSlopeDegrees;
        }

        private static float SmoothStep(float edge0, float edge1, float value)
        {
            float t = Math.Clamp((value - edge0) / (edge1 - edge0), 0f, 1f);
            return t * t * (3f - 2f * t);
        }

        private static float PerlinNoise(float x, float y)
        {
            int x0 = (int)MathF.Floor(x);
            int y0 = (int)MathF.Floor(y);
            int x1 = x0 + 1;
            int y1 = y0 + 1;

            float sx = Fade(x - x0);
            float sy = Fade(y - y0);

            float n00 = DotGrid(x0, y0, x, y);
            float n10 = DotGrid(x1, y0, x, y);
            float n01 = DotGrid(x0, y1, x, y);
            float n11 = DotGrid(x1, y1, x, y);

            float ix0 = Lerp(n00, n10, sx);
            float ix1 = Lerp(n01, n11, sx);
            return Lerp(ix0, ix1, sy);
        }

        private static float Fade(float t)
        {
            return t * t * t * (t * (t * 6f - 15f) + 10f);
        }

        private static float Lerp(float a, float b, float t)
        {
            return a + t * (b - a);
        }

        private static float DotGrid(int gridX, int gridY, float x, float y)
        {
            int hash = Hash(gridX, gridY) & 3;
            float gradientX;
            float gradientY;

            switch (hash)
            {
                case 0:
                    gradientX = 1f;
                    gradientY = 0f;
                    break;
                case 1:
                    gradientX = -1f;
                    gradientY = 0f;
                    break;
                case 2:
                    gradientX = 0f;
                    gradientY = 1f;
                    break;
                default:
                    gradientX = 0f;
                    gradientY = -1f;
                    break;
            }

            float dx = x - gridX;
            float dy = y - gridY;
            return gradientX * dx + gradientY * dy;
        }

        private static int Hash(int x, int y)
        {
            int hash = x * 374761393 + y * 668265263;
            hash = (hash ^ (hash >> 13)) * 1274126177;
            return hash ^ (hash >> 16);
        }
    }
}

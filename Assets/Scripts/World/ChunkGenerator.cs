using System;
using System.Collections.Generic;
using Ashlight.Environment;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Thread-safe procedural generator that fills <see cref="ChunkData"/> shells.
    /// </summary>
    public class ChunkGenerator
    {
        private const int HeightMapResolution = ChunkData.HeightMapResolution;
        private const float ChunkSize = WorldGenerator.ChunkSize;
        private const float MaxSlopeDegrees = 35f;
        private const float LargeTreeMinDistance = 2.5f;
        private const float MediumTreeMinDistance = 1.5f;
        private const float ChurchExclusionRadius = 5f;

        private readonly WorldGenerator _world;

        /// <summary>
        /// Creates a generator bound to world-level rules.
        /// </summary>
        /// <param name="world">Active world generator.</param>
        public ChunkGenerator(WorldGenerator world)
        {
            _world = world;
        }

        /// <summary>
        /// Generates and returns populated chunk data for a coordinate.
        /// Safe to call from a background thread — no Unity API usage.
        /// </summary>
        /// <param name="coord">Chunk grid coordinate.</param>
        /// <returns>Fully populated chunk data.</returns>
        public ChunkData Generate(Vector2Int coord)
        {
            var data = new ChunkData
            {
                chunkCoord = coord,
                chunkSeed = _world.GetChunkSeed(coord),
                heightMap = new float[HeightMapResolution, HeightMapResolution]
            };

            var rng = new System.Random(data.chunkSeed);

            GenerateHeightMap(data);
            DetermineBiome(data);
            DetermineChurchSpawn(data, rng);
            PlaceTrees(data, rng);
            PlaceGroundScatter(data, rng);
            PlaceShadowPockets(data, rng);
            DetermineGhostSpawnPoints(data, rng);

            data.isGenerated = true;
            return data;
        }

        private void GenerateHeightMap(ChunkData data)
        {
            bool flattenForStart = data.chunkCoord == Vector2Int.zero
                || (Math.Abs(data.chunkCoord.x) <= 1 && Math.Abs(data.chunkCoord.y) <= 1);

            data.heightMap = HeightmapGenerator.Generate(
                data.chunkCoord,
                _world.MasterSeed,
                data.heightScale,
                flattenForStart);
            data.heightMap = HeightmapGenerator.SmoothHeightmap(data.heightMap, iterations: 2);
            data.heightMap = HeightmapGenerator.ClampSlopes(data.heightMap, maxHeightDelta: 0.45f);
            ClampHeightmapToScale(data);
        }

        private static void ClampHeightmapToScale(ChunkData data)
        {
            if (data?.heightMap == null)
            {
                return;
            }

            int width = data.heightMap.GetLength(0);
            int height = data.heightMap.GetLength(1);

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    data.heightMap[x, z] = Math.Clamp(data.heightMap[x, z], 0f, data.heightScale);
                }
            }
        }

        private void DetermineBiome(ChunkData data)
        {
            _world.GetBiomeAtChunk(data.chunkCoord, out BiomeType biomeType, out float biomeBlend);
            data.biomeType = biomeType;
            data.biomeBlend = biomeBlend;
        }

        private void PlaceTrees(ChunkData data, System.Random rng)
        {
            float density = _world.GetTreeDensity(data.chunkCoord);
            if (density <= 0.001f)
            {
                return;
            }

            int largeTarget = Math.Min(3, Math.Max(0, (int)MathF.Round(density * 2f)));
            int mediumTarget = Math.Min(12, Math.Max(0, (int)MathF.Round(density * 12f)));

            Vector3 origin = _world.ChunkCoordToWorldOrigin(data.chunkCoord);
            var largeCandidates = new List<Vector2>();
            var mediumCandidates = new List<Vector2>();

            int attempts = Math.Max(largeTarget + mediumTarget, 1) * 30;
            for (int i = 0; i < attempts; i++)
            {
                float localX = NextFloat(rng) * ChunkSize;
                float localZ = NextFloat(rng) * ChunkSize;
                int mapX = Math.Clamp((int)MathF.Round(localX), 1, HeightMapResolution - 2);
                int mapZ = Math.Clamp((int)MathF.Round(localZ), 1, HeightMapResolution - 2);

                if (HeightmapGenerator.IsTooSteep(data.heightMap, mapX, mapZ, MaxSlopeDegrees))
                {
                    continue;
                }

                if (IsNearChurch(data, origin, localX, localZ, ChurchExclusionRadius))
                {
                    continue;
                }

                var point = new Vector2(localX, localZ);
                if (largeCandidates.Count < largeTarget && IsPoissonValid(point, largeCandidates, LargeTreeMinDistance))
                {
                    largeCandidates.Add(point);
                    continue;
                }

                if (mediumCandidates.Count < mediumTarget && IsPoissonValid(point, mediumCandidates, MediumTreeMinDistance))
                {
                    mediumCandidates.Add(point);
                }
            }

            foreach (Vector2 point in largeCandidates)
            {
                float y = SampleHeight(data, point.x, point.y);
                data.largTreePositions.Add(origin + new Vector3(point.x, y, point.y));
            }

            foreach (Vector2 point in mediumCandidates)
            {
                float y = SampleHeight(data, point.x, point.y);
                data.mediumTreePositions.Add(origin + new Vector3(point.x, y, point.y));
            }
        }

        private void PlaceGroundScatter(ChunkData data, System.Random rng)
        {
            Vector3 origin = _world.ChunkCoordToWorldOrigin(data.chunkCoord);
            int rockCount = rng.Next(2, 7);

            for (int i = 0; i < rockCount; i++)
            {
                float localX = NextFloat(rng) * ChunkSize;
                float localZ = NextFloat(rng) * ChunkSize;
                int mapX = Math.Clamp((int)MathF.Round(localX), 1, HeightMapResolution - 2);
                int mapZ = Math.Clamp((int)MathF.Round(localZ), 1, HeightMapResolution - 2);

                if (HeightmapGenerator.GetSlopeAngle(data.heightMap, mapX, mapZ) < 12f)
                {
                    continue;
                }

                float y = SampleHeight(data, localX, localZ);
                data.rockPositions.Add(origin + new Vector3(localX, y, localZ));
            }

            foreach (Vector3 largeTree in data.largTreePositions)
            {
                int rootCount = rng.Next(2, 5);
                for (int i = 0; i < rootCount; i++)
                {
                    float angle = NextFloat(rng) * MathF.PI * 2f;
                    float distance = 0.6f + NextFloat(rng) * 1.8f;
                    Vector3 offset = new Vector3(MathF.Cos(angle) * distance, 0f, MathF.Sin(angle) * distance);
                    data.rootPositions.Add(largeTree + offset);
                }
            }

            int bushCount = rng.Next(3, 9);
            for (int i = 0; i < bushCount; i++)
            {
                float localX = NextFloat(rng) * ChunkSize;
                float localZ = NextFloat(rng) * ChunkSize;
                if (IsNearExistingTree(data, origin, localX, localZ, 1.2f))
                {
                    continue;
                }

                float y = SampleHeight(data, localX, localZ);
                data.bushPositions.Add(origin + new Vector3(localX, y, localZ));
            }
        }

        private void DetermineGhostSpawnPoints(ChunkData data, System.Random rng)
        {
            if (data.chunkCoord == Vector2Int.zero)
            {
                return;
            }

            if (data.shadowPocketCentres.Count == 0)
            {
                return;
            }

            int spawnCount = rng.Next(3, 9);
            for (int i = 0; i < spawnCount; i++)
            {
                Vector3 pocket = data.shadowPocketCentres[rng.Next(data.shadowPocketCentres.Count)];
                float angle = NextFloat(rng) * MathF.PI * 2f;
                float radius = 1f + NextFloat(rng) * 3f;
                Vector3 candidate = pocket + new Vector3(MathF.Cos(angle) * radius, 0f, MathF.Sin(angle) * radius);

                if (IsNearChurchWorld(data, candidate, ChurchExclusionRadius))
                {
                    continue;
                }

                data.ghostSpawnPoints.Add(candidate);
            }
        }

        private void DetermineChurchSpawn(ChunkData data, System.Random rng)
        {
            ChurchPlacementRule rule = _world.GetChurchRule();
            int distanceFromStart = data.chunkCoord.y;

            if (distanceFromStart < rule.minDistanceFromStart || distanceFromStart > rule.maxDistanceFromStart)
            {
                return;
            }

            if (_world.HasChurchWithinSpacing(data.chunkCoord, rule.spacingBetweenChurches))
            {
                return;
            }

            if (rng.NextDouble() > 0.35d)
            {
                return;
            }

            if (!TryFindFlatChurchPosition(data, rule.clearingRadius, rng, out Vector3 worldPosition))
            {
                return;
            }

            ChurchType churchType = RollChurchType(rng);
            var spawn = new ChurchSpawnData
            {
                worldPosition = worldPosition,
                churchType = churchType,
                churchID = $"church_{data.chunkCoord.x}_{data.chunkCoord.y}_{churchType}",
                isActivated = false
            };

            data.churchSpawns.Add(spawn);
            _world.RegisterChurchChunk(data.chunkCoord);
        }

        private void PlaceShadowPockets(ChunkData data, System.Random rng)
        {
            if (data.largTreePositions.Count + data.mediumTreePositions.Count < 3)
            {
                return;
            }

            int pocketCount = rng.Next(1, 4);
            var treePoints = new List<Vector3>(data.largTreePositions);
            treePoints.AddRange(data.mediumTreePositions);

            for (int pocketIndex = 0; pocketIndex < pocketCount; pocketIndex++)
            {
                int clusterSize = Math.Min(treePoints.Count, rng.Next(3, 7));
                Vector3 centroid = Vector3.zero;
                for (int i = 0; i < clusterSize; i++)
                {
                    centroid += treePoints[rng.Next(treePoints.Count)];
                }

                centroid /= clusterSize;
                data.shadowPocketCentres.Add(centroid);
            }
        }

        private ChurchType RollChurchType(System.Random rng)
        {
            double roll = rng.NextDouble() * 100d;
            return _world.Difficulty switch
            {
                GameDifficulty.Easy when roll < 40d => ChurchType.Candle,
                GameDifficulty.Easy when roll < 70d => ChurchType.HolyWater,
                GameDifficulty.Easy when roll < 90d => ChurchType.SimpleAltar,
                GameDifficulty.Easy => ChurchType.Priest,
                GameDifficulty.Medium when roll < 30d => ChurchType.SimpleAltar,
                GameDifficulty.Medium when roll < 60d => ChurchType.Candle,
                GameDifficulty.Medium when roll < 85d => ChurchType.HolyWater,
                GameDifficulty.Medium => ChurchType.Priest,
                GameDifficulty.Hard when roll < 50d => ChurchType.SimpleAltar,
                GameDifficulty.Hard when roll < 80d => ChurchType.Candle,
                GameDifficulty.Hard when roll < 95d => ChurchType.HolyWater,
                _ => ChurchType.Priest
            };
        }

        private bool TryFindFlatChurchPosition(
            ChunkData data,
            float clearingRadius,
            System.Random rng,
            out Vector3 worldPosition)
        {
            Vector3 origin = _world.ChunkCoordToWorldOrigin(data.chunkCoord);
            worldPosition = origin;

            for (int attempt = 0; attempt < 24; attempt++)
            {
                float localX = clearingRadius + NextFloat(rng) * (ChunkSize - clearingRadius * 2f);
                float localZ = clearingRadius + NextFloat(rng) * (ChunkSize - clearingRadius * 2f);
                int mapX = Math.Clamp((int)MathF.Round(localX), 1, HeightMapResolution - 2);
                int mapZ = Math.Clamp((int)MathF.Round(localZ), 1, HeightMapResolution - 2);

                if (HeightmapGenerator.GetSlopeAngle(data.heightMap, mapX, mapZ) > 10f)
                {
                    continue;
                }

                if (!IsAreaFlat(data, mapX, mapZ, clearingRadius))
                {
                    continue;
                }

                float y = SampleHeight(data, localX, localZ);
                worldPosition = origin + new Vector3(localX, y, localZ);
                return true;
            }

            return false;
        }

        private static bool IsAreaFlat(ChunkData data, int centerX, int centerZ, float radius)
        {
            float centerHeight = data.heightMap[centerX, centerZ];
            int sampleRadius = Math.Max(1, (int)MathF.Round(radius * 0.25f));

            for (int z = centerZ - sampleRadius; z <= centerZ + sampleRadius; z++)
            {
                for (int x = centerX - sampleRadius; x <= centerX + sampleRadius; x++)
                {
                    if (x < 0 || z < 0 || x >= HeightMapResolution || z >= HeightMapResolution)
                    {
                        return false;
                    }

                    if (MathF.Abs(data.heightMap[x, z] - centerHeight) > 0.75f)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static bool IsNearChurch(
            ChunkData data,
            Vector3 chunkOrigin,
            float localX,
            float localZ,
            float radius)
        {
            float radiusSquared = radius * radius;
            foreach (ChurchSpawnData church in data.churchSpawns)
            {
                float dx = church.worldPosition.x - (chunkOrigin.x + localX);
                float dz = church.worldPosition.z - (chunkOrigin.z + localZ);
                if (dx * dx + dz * dz <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNearChurchWorld(ChunkData data, Vector3 worldPosition, float radius)
        {
            float radiusSquared = radius * radius;
            foreach (ChurchSpawnData church in data.churchSpawns)
            {
                float dx = church.worldPosition.x - worldPosition.x;
                float dz = church.worldPosition.z - worldPosition.z;
                if (dx * dx + dz * dz <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsNearExistingTree(
            ChunkData data,
            Vector3 chunkOrigin,
            float localX,
            float localZ,
            float radius)
        {
            float radiusSquared = radius * radius;

            foreach (Vector3 tree in data.largTreePositions)
            {
                float dx = tree.x - (chunkOrigin.x + localX);
                float dz = tree.z - (chunkOrigin.z + localZ);
                if (dx * dx + dz * dz <= radiusSquared)
                {
                    return true;
                }
            }

            foreach (Vector3 tree in data.mediumTreePositions)
            {
                float dx = tree.x - (chunkOrigin.x + localX);
                float dz = tree.z - (chunkOrigin.z + localZ);
                if (dx * dx + dz * dz <= radiusSquared)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPoissonValid(Vector2 candidate, List<Vector2> accepted, float minDistance)
        {
            float minDistanceSquared = minDistance * minDistance;
            for (int i = 0; i < accepted.Count; i++)
            {
                Vector2 delta = accepted[i] - candidate;
                if (delta.sqrMagnitude < minDistanceSquared)
                {
                    return false;
                }
            }

            return true;
        }

        private static float SampleHeight(ChunkData data, float localX, float localZ)
        {
            int x = Math.Clamp((int)MathF.Round(localX), 0, HeightMapResolution - 1);
            int z = Math.Clamp((int)MathF.Round(localZ), 0, HeightMapResolution - 1);
            return data.heightMap[x, z];
        }

        private static float NextFloat(System.Random rng)
        {
            return (float)rng.NextDouble();
        }
    }
}

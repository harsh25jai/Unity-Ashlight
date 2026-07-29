using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Game difficulty presets that influence church spacing and visibility.
    /// </summary>
    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }

    /// <summary>
    /// Church placement spacing and clearing rules for a difficulty preset.
    /// </summary>
    [Serializable]
    public class ChurchPlacementRule
    {
        public int minDistanceFromStart;
        public int maxDistanceFromStart;
        public int spacingBetweenChurches;
        public float clearingRadius;
        public float visibilityRange;
    }

    /// <summary>
    /// Deterministic biome influence anchor derived from the master seed.
    /// </summary>
    [Serializable]
    public struct BiomeZonePoint
    {
        public Vector2Int chunkPosition;
        public BiomeType biomeType;
        public int influenceRadius;
    }

    /// <summary>
    /// Owns the master world seed and world-level generation parameters.
    /// Does not generate chunk content directly — <see cref="ChunkGenerator"/> does that.
    /// </summary>
    [DisallowMultipleComponent]
    public class WorldGenerator : MonoBehaviour
    {
        public const float ChunkSize = ChunkData.ChunkSize;

        private const int BiomeZoneCount = 8;
        private const int ForestOnlyRadiusChunks = 8;
        private const int ChunkSeedMultiplierX = 73856093;
        private const int ChunkSeedMultiplierY = 19349663;
        private const int BiomeZoneSeedSalt = 0x5EEDBEEF;

        [SerializeField] private int masterSeed;
        [SerializeField] private bool randomiseSeedOnNewGame = true;
        [SerializeField] private GameDifficulty difficulty = GameDifficulty.Medium;

        private readonly HashSet<Vector2Int> _placedChurchChunks = new HashSet<Vector2Int>();
        private BiomeZonePoint[] _biomeZones = Array.Empty<BiomeZonePoint>();

        /// <summary>Gets the active world generator instance.</summary>
        public static WorldGenerator Instance { get; private set; }

        /// <summary>Gets the master seed driving all chunk generation.</summary>
        public int MasterSeed => masterSeed;

        /// <summary>Gets the active difficulty preset.</summary>
        public GameDifficulty Difficulty => difficulty;

        /// <summary>Gets read-only biome zone anchors.</summary>
        public IReadOnlyList<BiomeZonePoint> BiomeZones => _biomeZones;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (randomiseSeedOnNewGame)
            {
                masterSeed = UnityEngine.Random.Range(0, int.MaxValue);
            }

            InitializeBiomeZones();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Derives a deterministic per-chunk seed from the master seed and chunk coordinate.
        /// </summary>
        /// <param name="chunkCoord">Chunk grid coordinate.</param>
        /// <returns>Unique chunk seed.</returns>
        public int GetChunkSeed(Vector2Int chunkCoord)
        {
            return masterSeed
                ^ (chunkCoord.x * ChunkSeedMultiplierX)
                ^ (chunkCoord.y * ChunkSeedMultiplierY);
        }

        /// <summary>
        /// Converts a world position to the containing chunk coordinate.
        /// </summary>
        /// <param name="worldPos">World-space position.</param>
        /// <returns>Chunk grid coordinate.</returns>
        public Vector2Int WorldToChunkCoord(Vector3 worldPos)
        {
            return new Vector2Int(
                Mathf.FloorToInt(worldPos.x / ChunkSize),
                Mathf.FloorToInt(worldPos.z / ChunkSize));
        }

        /// <summary>
        /// Converts a chunk coordinate to its world-space origin corner.
        /// </summary>
        /// <param name="coord">Chunk grid coordinate.</param>
        /// <returns>World origin for the chunk.</returns>
        public Vector3 ChunkCoordToWorldOrigin(Vector2Int coord)
        {
            return new Vector3(coord.x * ChunkSize, 0f, coord.y * ChunkSize);
        }

        /// <summary>
        /// Returns normalized tree density for a chunk based on distance from the start.
        /// </summary>
        /// <param name="chunkCoord">Chunk grid coordinate.</param>
        /// <returns>Density from 0 (sparse) to 1 (maximum).</returns>
        public float GetTreeDensity(Vector2Int chunkCoord)
        {
            float distFromStart = Vector2Int.Distance(chunkCoord, Vector2Int.zero);
            return Mathf.Clamp01(distFromStart / 8f);
        }

        /// <summary>
        /// Returns church placement rules for the active difficulty preset.
        /// </summary>
        /// <returns>Spacing and clearing configuration.</returns>
        public ChurchPlacementRule GetChurchRule()
        {
            return difficulty switch
            {
                GameDifficulty.Easy => new ChurchPlacementRule
                {
                    minDistanceFromStart = 4,
                    maxDistanceFromStart = 6,
                    spacingBetweenChurches = 3,
                    clearingRadius = 12f,
                    visibilityRange = 30f
                },
                GameDifficulty.Medium => new ChurchPlacementRule
                {
                    minDistanceFromStart = 6,
                    maxDistanceFromStart = 8,
                    spacingBetweenChurches = 5,
                    clearingRadius = 8f,
                    visibilityRange = 20f
                },
                GameDifficulty.Hard => new ChurchPlacementRule
                {
                    minDistanceFromStart = 8,
                    maxDistanceFromStart = 11,
                    spacingBetweenChurches = 7,
                    clearingRadius = 5f,
                    visibilityRange = 12f
                },
                _ => new ChurchPlacementRule()
            };
        }

        /// <summary>
        /// Samples biome type and forest blend for a chunk coordinate.
        /// </summary>
        /// <param name="chunkCoord">Chunk grid coordinate.</param>
        /// <param name="biomeType">Resolved biome type.</param>
        /// <param name="biomeBlend">Blend toward forest where 0 is full biome and 1 is full forest.</param>
        public void GetBiomeAtChunk(Vector2Int chunkCoord, out BiomeType biomeType, out float biomeBlend)
        {
            float distFromStart = Vector2Int.Distance(chunkCoord, Vector2Int.zero);
            if (distFromStart <= ForestOnlyRadiusChunks)
            {
                biomeType = BiomeType.Forest;
                biomeBlend = 0f;
                return;
            }

            biomeType = BiomeType.Forest;
            biomeBlend = 1f;
            float strongestInfluence = 0f;

            for (int i = 0; i < _biomeZones.Length; i++)
            {
                BiomeZonePoint zone = _biomeZones[i];
                float distance = Vector2Int.Distance(chunkCoord, zone.chunkPosition);
                if (distance > zone.influenceRadius)
                {
                    continue;
                }

                float influence = 1f - distance / zone.influenceRadius;
                if (influence <= strongestInfluence)
                {
                    continue;
                }

                strongestInfluence = influence;
                biomeType = zone.biomeType;
                biomeBlend = zone.biomeType == BiomeType.Forest ? 0f : 1f - influence;
            }
        }

        /// <summary>
        /// Returns whether another church already exists within spacing distance.
        /// </summary>
        /// <param name="chunkCoord">Candidate chunk coordinate.</param>
        /// <param name="spacingChunks">Minimum Chebyshev chunk spacing.</param>
        /// <returns>True when a nearby church blocks placement.</returns>
        public bool HasChurchWithinSpacing(Vector2Int chunkCoord, int spacingChunks)
        {
            foreach (Vector2Int placed in _placedChurchChunks)
            {
                int deltaX = Mathf.Abs(placed.x - chunkCoord.x);
                int deltaY = Mathf.Abs(placed.y - chunkCoord.y);
                int chebyshev = Mathf.Max(deltaX, deltaY);
                if (chebyshev > 0 && chebyshev < spacingChunks)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Registers a chunk coordinate as containing a placed church.
        /// </summary>
        /// <param name="chunkCoord">Chunk that received a church.</param>
        public void RegisterChurchChunk(Vector2Int chunkCoord)
        {
            _placedChurchChunks.Add(chunkCoord);
        }

        /// <summary>
        /// Clears tracked church placements. Used when starting a new world.
        /// </summary>
        public void ClearPlacedChurches()
        {
            _placedChurchChunks.Clear();
        }

        private void InitializeBiomeZones()
        {
            var rng = new System.Random(masterSeed ^ BiomeZoneSeedSalt);
            _biomeZones = new BiomeZonePoint[BiomeZoneCount];

            for (int i = 0; i < _biomeZones.Length; i++)
            {
                _biomeZones[i] = new BiomeZonePoint
                {
                    chunkPosition = new Vector2Int(rng.Next(10, 50), rng.Next(10, 80)),
                    biomeType = i % 2 == 0 ? BiomeType.DarkForest : BiomeType.Forest,
                    influenceRadius = rng.Next(8, 20)
                };
            }
        }
    }
}

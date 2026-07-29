using System;
using System.Collections.Generic;
using Ashlight.Environment;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Biome classification for procedural world chunks.
    /// </summary>
    public enum BiomeType
    {
        Forest,
        DarkForest,
        Cemetery,
        Ruins
    }

    /// <summary>
    /// Serializable church placement record for a generated chunk.
    /// </summary>
    [Serializable]
    public class ChurchSpawnData
    {
        public Vector3 worldPosition;
        public ChurchType churchType;
        public string churchID;
        public bool isActivated;
    }

    /// <summary>
    /// Pure data container for one 20×20 world chunk and its generated content.
    /// </summary>
    [Serializable]
    public class ChunkData
    {
        public const int HeightMapResolution = 21;
        public const float ChunkSize = 20f;
        public const float DefaultHeightScale = 7f;

        public Vector2Int chunkCoord;
        public int chunkSeed;

        public float[,] heightMap;
        public float heightScale = DefaultHeightScale;

        public List<Vector3> largTreePositions = new List<Vector3>();
        public List<Vector3> mediumTreePositions = new List<Vector3>();
        public List<Vector3> rockPositions = new List<Vector3>();
        public List<Vector3> rootPositions = new List<Vector3>();
        public List<Vector3> bushPositions = new List<Vector3>();
        public List<Vector3> ghostSpawnPoints = new List<Vector3>();
        public List<ChurchSpawnData> churchSpawns = new List<ChurchSpawnData>();
        public List<Vector3> shadowPocketCentres = new List<Vector3>();

        public BiomeType biomeType;
        public float biomeBlend;

        public bool isGenerated;

        /// <summary>Gets the stable identifier used for save/event lookup.</summary>
        public string chunkID => $"chunk_{chunkCoord.x}_{chunkCoord.y}";
    }
}

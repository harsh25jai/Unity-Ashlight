using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Instantiates grass and optional rock scatter on a terrain chunk from <see cref="ChunkData"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class GroundScatterPlacer : MonoBehaviour
    {
        private const float GrassMaxSlopeDegrees = 30f;
        private const float GrassSurfaceYOffset = 0.5f;
        private const int GrassMaxInstantiatesPerFrame = 6;
        private const float GrassMaxMillisecondsPerFrame = 2f;
        private const float RockMinScale = 0.4f;
        private const float RockMaxScale = 1.5f;
        private const float RockMaxSlopeDegrees = 40f;
        private const float RockSurfaceYOffset = 0.05f;
        private const string EnvironmentLayerName = "Environment";

        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private GameObject[] rockPrefabs;
        [SerializeField] private Transform scatterParent;

        private readonly List<GameObject> _spawnedObjects = new List<GameObject>();

        /// <summary>
        /// Configures scatter prefab arrays and the parent transform at runtime.
        /// </summary>
        /// <param name="grass">Grass clump prefabs.</param>
        /// <param name="rocks">Optional rock prefabs. Skipped when null or empty.</param>
        /// <param name="parent">Parent transform for spawned scatter instances.</param>
        public void Initialise(GameObject[] grass, GameObject[] rocks, Transform parent)
        {
            grassPrefabs = grass;
            rockPrefabs = rocks;
            scatterParent = parent;
        }

        /// <summary>
        /// Places rock scatter on a Unity terrain chunk.
        /// Grass is spawned separately via <see cref="PlaceGrassPatchesAsync"/>.
        /// </summary>
        /// <param name="data">Generated chunk content.</param>
        /// <param name="terrain">Unity terrain used for height and slope sampling.</param>
        public void PlaceRocksUnityTerrain(ChunkData data, TerrainChunkUnity terrain)
        {
            if (data == null || terrain == null || scatterParent == null)
            {
                return;
            }

            if (rockPrefabs == null || rockPrefabs.Length == 0 || data.rockPositions == null)
            {
                return;
            }

            var rng = new System.Random(data.chunkSeed + 1);

            foreach (Vector3 worldPosition in data.rockPositions)
            {
                PlaceScatterObject(
                    worldPosition,
                    terrain,
                    rng,
                    rockPrefabs,
                    RockMinScale,
                    RockMaxScale,
                    RockMaxSlopeDegrees,
                    RockSurfaceYOffset,
                    compensateCenterPivot: false,
                    prefabMeshHalfHeight: 0f);
            }
        }

        /// <summary>
        /// Clears scatter and places rocks. Grass must be started separately.
        /// </summary>
        /// <param name="data">Generated chunk content.</param>
        /// <param name="terrain">Unity terrain used for height and slope sampling.</param>
        public void PlaceScatterUnityTerrain(ChunkData data, TerrainChunkUnity terrain)
        {
            ClearScatter();

            if (data == null || terrain == null || scatterParent == null)
            {
                return;
            }

            PlaceRocksUnityTerrain(data, terrain);
        }

        /// <summary>
        /// Destroys all scatter objects spawned by this placer.
        /// </summary>
        public void ClearScatter()
        {
            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                if (_spawnedObjects[i] != null)
                {
                    Destroy(_spawnedObjects[i]);
                }
            }

            _spawnedObjects.Clear();
        }

        /// <summary>
        /// Spawns grass patches over multiple frames to avoid instantiation spikes.
        /// </summary>
        /// <param name="data">Generated chunk content.</param>
        /// <param name="terrain">Unity terrain used for height and slope sampling.</param>
        /// <param name="rng">Deterministic random source for this chunk.</param>
        public IEnumerator PlaceGrassPatchesAsync(ChunkData data, TerrainChunkUnity terrain, System.Random rng)
        {
            if (grassPrefabs == null || grassPrefabs.Length == 0 || scatterParent == null || terrain == null)
            {
                yield break;
            }

            int bushCount = data.bushPositions != null ? data.bushPositions.Count : 0;
            int patchCount = Mathf.Clamp(bushCount, 8, 16);
            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            int instantiatedThisFrame = 0;
            Stopwatch frameStopwatch = Stopwatch.StartNew();

            for (int p = 0; p < patchCount; p++)
            {
                float centerX = (float)(rng.NextDouble() * ChunkData.ChunkSize);
                float centerZ = (float)(rng.NextDouble() * ChunkData.ChunkSize);
                Vector3 patchCenterLocal = new Vector3(centerX, 0f, centerZ);

                float patchRadius = Mathf.Lerp(1.2f, 2.8f, (float)rng.NextDouble());

                int grassTypeIndex = rng.Next(0, grassPrefabs.Length);
                GameObject chosenPrefab = grassPrefabs[grassTypeIndex];
                if (chosenPrefab == null)
                {
                    continue;
                }

                int clumpCount = Mathf.RoundToInt(patchRadius * patchRadius * 5f);
                clumpCount = Mathf.Clamp(clumpCount, 10, 45);

                float patchBaseRotation = (float)(rng.NextDouble() * 360.0);
                const float rotationVariance = 75f;

                for (int c = 0; c < clumpCount; c++)
                {
                    float angle = (float)(rng.NextDouble() * Mathf.PI * 2);

                    float distT = (float)rng.NextDouble();
                    float dist = Mathf.Pow(distT, 0.7f) * patchRadius;

                    float offsetX = Mathf.Cos(angle) * dist;
                    float offsetZ = Mathf.Sin(angle) * dist;
                    offsetX += (float)(rng.NextDouble() - 0.5) * 0.3f;
                    offsetZ += (float)(rng.NextDouble() - 0.5) * 0.3f;

                    Vector3 clumpLocalPos = patchCenterLocal + new Vector3(offsetX, 0f, offsetZ);
                    clumpLocalPos.x = Mathf.Clamp(clumpLocalPos.x, 0.5f, ChunkData.ChunkSize - 0.5f);
                    clumpLocalPos.z = Mathf.Clamp(clumpLocalPos.z, 0.5f, ChunkData.ChunkSize - 0.5f);

                    Vector3 worldPos = terrain.transform.position + clumpLocalPos;

                    float terrainHeight = terrain.GetHeightAtWorldPos(worldPos.x, worldPos.z);
                    worldPos.y = terrainHeight + GrassSurfaceYOffset;

                    float slope = terrain.GetSlopeAtWorldPos(worldPos.x, worldPos.z);
                    if (slope > GrassMaxSlopeDegrees)
                    {
                        continue;
                    }

                    GameObject clump = Instantiate(chosenPrefab, worldPos, Quaternion.identity, scatterParent);

                    float yRot = patchBaseRotation + ((float)(rng.NextDouble() - 0.5) * 2f * rotationVariance);
                    clump.transform.rotation = Quaternion.Euler(0f, yRot, 0f);

                    float scale = 0.75f + (float)(rng.NextDouble() * 0.6f);
                    clump.transform.localScale = Vector3.one * scale;

                    if (environmentLayer >= 0)
                    {
                        clump.layer = environmentLayer;
                    }

                    _spawnedObjects.Add(clump);

                    instantiatedThisFrame++;
                    if (instantiatedThisFrame >= GrassMaxInstantiatesPerFrame
                        || frameStopwatch.Elapsed.TotalMilliseconds >= GrassMaxMillisecondsPerFrame)
                    {
                        instantiatedThisFrame = 0;
                        yield return null;
                        frameStopwatch.Restart();
                    }
                }
            }
        }

        private void PlaceScatterObject(
            Vector3 worldPosition,
            TerrainChunkUnity terrain,
            System.Random rng,
            GameObject[] prefabs,
            float minScale,
            float maxScale,
            float maxSlopeDegrees,
            float surfaceYOffset,
            bool compensateCenterPivot,
            float prefabMeshHalfHeight)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            float terrainHeight = terrain.GetHeightAtWorldPos(worldPosition.x, worldPosition.z);

            float slope = terrain.GetSlopeAtWorldPos(worldPosition.x, worldPosition.z);
            if (slope > maxSlopeDegrees)
            {
                return;
            }

            int index = rng.Next(0, prefabs.Length);
            GameObject prefab = prefabs[index];
            if (prefab == null)
            {
                return;
            }

            float scale = minScale + (float)(rng.NextDouble() * (maxScale - minScale));
            float pivotYOffset = compensateCenterPivot ? prefabMeshHalfHeight * scale : 0f;
            worldPosition.y = terrainHeight + surfaceYOffset + pivotYOffset;

            GameObject scatterObject = Instantiate(prefab, worldPosition, Quaternion.identity, scatterParent);

            float yRotation = (float)(rng.NextDouble() * 360.0);
            scatterObject.transform.rotation = Quaternion.Euler(0f, yRotation, 0f);

            scatterObject.transform.localScale = Vector3.one * scale;

            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer >= 0)
            {
                scatterObject.layer = environmentLayer;
            }

            _spawnedObjects.Add(scatterObject);
        }
    }
}

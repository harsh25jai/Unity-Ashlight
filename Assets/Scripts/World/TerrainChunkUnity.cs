#if UNITY_EDITOR
using UnityEditor;
#endif
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Ashlight.World
{
    /// <summary>
    /// Wraps a Unity <see cref="Terrain"/> component for a single procedural chunk.
    /// </summary>
    [DisallowMultipleComponent]
    public class TerrainChunkUnity : MonoBehaviour
    {
        private const int HeightmapResolution = 33;
        private const int AlphamapResolution = 33;
        private const float BasemapDistance = 100f;
        private const float ChunkWorldSize = ChunkData.ChunkSize;
        private const string EnvironmentLayerName = "Environment";

        private static readonly Vector3 StandardTerrainSize = new Vector3(
            ChunkData.ChunkSize,
            Mathf.Max(ChunkData.DefaultHeightScale, 0.01f),
            ChunkData.ChunkSize);

        private ChunkData _data;
        private Terrain _terrain;
        private TerrainData _terrainData;
        private TerrainCollider _terrainCollider;
        private TerrainLayer[] _terrainLayers;
        private NavMeshSurface _navMeshSurface;

        /// <summary>Gets the chunk data backing this terrain.</summary>
        public ChunkData Data => _data;

        /// <summary>Gets the Unity terrain component.</summary>
        public Terrain UnityTerrain => _terrain;

        /// <summary>
        /// Builds or rebuilds the Unity terrain from chunk data.
        /// </summary>
        /// <param name="data">Generated chunk data.</param>
        /// <param name="terrainLayers">Optional terrain paint layers.</param>
        public void Initialise(ChunkData data, TerrainLayer[] terrainLayers)
        {
            _data = data;
            _terrainLayers = terrainLayers;

            if (_terrain != null)
            {
                Destroy(_terrain.gameObject);
                _terrain = null;
                _terrainCollider = null;
            }

            if (_terrainData != null)
            {
                Destroy(_terrainData);
                _terrainData = null;
            }

            _terrainData = new TerrainData
            {
                heightmapResolution = HeightmapResolution,
                size = StandardTerrainSize
            };
            _terrainData.alphamapResolution = AlphamapResolution;

            float[,] unityHeights = ConvertToUnityHeightmap(data.heightMap, HeightmapResolution);
            _terrainData.SetHeights(0, 0, unityHeights);

            if (terrainLayers != null && terrainLayers.Length > 0)
            {
                _terrainData.terrainLayers = terrainLayers;
                ApplyDefaultSplatmap(terrainLayers.Length);
            }

            GameObject terrainObject = Terrain.CreateTerrainGameObject(_terrainData);
            terrainObject.transform.SetParent(transform, false);
            terrainObject.transform.localPosition = Vector3.zero;
            terrainObject.name = "UnityTerrain";

            _terrain = terrainObject.GetComponent<Terrain>();
            _terrainCollider = terrainObject.GetComponent<TerrainCollider>();

            if (_terrain != null)
            {
                _terrain.heightmapPixelError = 5f;
                _terrain.basemapDistance = BasemapDistance;
            }

            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer >= 0)
            {
                terrainObject.layer = environmentLayer;
            }

            ConfigureNavMeshSurface(data);
        }

        /// <summary>
        /// Updates the heightmap from refreshed chunk data after neighbour smoothing.
        /// </summary>
        /// <param name="data">Chunk data with updated heights.</param>
        public void RefreshFromData(ChunkData data)
        {
            if (_terrainData == null || data == null || data.heightMap == null)
            {
                return;
            }

            _data = data;
            _terrainData.size = StandardTerrainSize;

            float[,] unityHeights = ConvertToUnityHeightmap(data.heightMap, HeightmapResolution);
            _terrainData.SetHeights(0, 0, unityHeights);

            if (_terrainLayers != null && _terrainLayers.Length > 0)
            {
                ApplyDefaultSplatmap(_terrainLayers.Length);
            }
        }

        /// <summary>
        /// Returns terrain world-space height at the given world XZ position.
        /// </summary>
        /// <param name="worldX">World-space X coordinate.</param>
        /// <param name="worldZ">World-space Z coordinate.</param>
        /// <returns>World-space Y height.</returns>
        public float GetHeightAtWorldPos(float worldX, float worldZ)
        {
            if (_terrain == null)
            {
                return transform.position.y;
            }

            return _terrain.SampleHeight(new Vector3(worldX, 0f, worldZ));
        }

        /// <summary>
        /// Returns slope angle in degrees at the given world XZ position.
        /// </summary>
        /// <param name="worldX">World-space X coordinate.</param>
        /// <param name="worldZ">World-space Z coordinate.</param>
        /// <returns>Slope angle in degrees.</returns>
        public float GetSlopeAtWorldPos(float worldX, float worldZ)
        {
            if (_terrain == null || _terrainData == null)
            {
                return 0f;
            }

            float normX = (worldX - _terrain.transform.position.x) / _terrainData.size.x;
            float normZ = (worldZ - _terrain.transform.position.z) / _terrainData.size.z;
            return _terrainData.GetSteepness(Mathf.Clamp01(normX), Mathf.Clamp01(normZ));
        }

        /// <summary>
        /// Links this terrain to neighbours for seamless edge rendering.
        /// </summary>
        /// <param name="left">Terrain to the west.</param>
        /// <param name="top">Terrain to the north.</param>
        /// <param name="right">Terrain to the east.</param>
        /// <param name="bottom">Terrain to the south.</param>
        public void SetNeighbours(
            TerrainChunkUnity left,
            TerrainChunkUnity top,
            TerrainChunkUnity right,
            TerrainChunkUnity bottom)
        {
            if (_terrain == null || _terrainData == null)
            {
                return;
            }

            ValidateNeighbourMetrics(left, "left");
            ValidateNeighbourMetrics(top, "top");
            ValidateNeighbourMetrics(right, "right");
            ValidateNeighbourMetrics(bottom, "bottom");

            Debug.Log(
                $"Linking terrain at {transform.position} — " +
                $"L:{left != null} T:{top != null} R:{right != null} B:{bottom != null}",
                this);

            _terrain.SetNeighbors(
                left?.UnityTerrain,
                top?.UnityTerrain,
                right?.UnityTerrain,
                bottom?.UnityTerrain);
            _terrain.Flush();
        }

        /// <summary>
        /// Returns whether this terrain uses the standard stitch metrics.
        /// </summary>
        /// <returns>True when resolution and size match the shared chunk standard.</returns>
        public bool HasStandardTerrainMetrics()
        {
            return _terrainData != null
                && _terrainData.heightmapResolution == HeightmapResolution
                && _terrainData.size == StandardTerrainSize;
        }

        private void ValidateNeighbourMetrics(TerrainChunkUnity neighbour, string sideLabel)
        {
            if (neighbour == null || neighbour._terrainData == null || _terrainData == null)
            {
                return;
            }

            if (neighbour._terrainData.heightmapResolution != _terrainData.heightmapResolution
                || neighbour._terrainData.size != _terrainData.size)
            {
                Debug.LogWarning(
                    $"Terrain stitch mismatch on {sideLabel} for {name}: " +
                    $"this(res={_terrainData.heightmapResolution}, size={_terrainData.size}) vs " +
                    $"neighbour(res={neighbour._terrainData.heightmapResolution}, size={neighbour._terrainData.size})",
                    this);
            }
        }

        /// <summary>
        /// Starts an asynchronous NavMesh bake scoped to this chunk's volume.
        /// Call once after terrain, trees, and scatter are fully placed.
        /// </summary>
        /// <returns>Async bake operation, or null when no surface is configured.</returns>
        public AsyncOperation BuildNavMeshAsync()
        {
            if (_navMeshSurface == null)
            {
                return null;
            }

            if (_navMeshSurface.navMeshData == null)
            {
                _navMeshSurface.navMeshData = new NavMeshData
                {
                    name = $"{gameObject.name}_NavMesh"
                };
            }

            _navMeshSurface.RemoveData();
            AsyncOperation operation = _navMeshSurface.UpdateNavMesh(_navMeshSurface.navMeshData);
            _navMeshSurface.AddData();
            return operation;
        }

        /// <summary>
        /// Destroys terrain data and this chunk GameObject.
        /// </summary>
        public void DestroyChunk()
        {
            if (_navMeshSurface != null)
            {
                _navMeshSurface.RemoveData();

                if (_navMeshSurface.navMeshData != null)
                {
                    Destroy(_navMeshSurface.navMeshData);
                    _navMeshSurface.navMeshData = null;
                }
            }

            if (_terrainData != null)
            {
                Destroy(_terrainData);
                _terrainData = null;
            }

            ClearEditorSelectionIfTargeting(gameObject);
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        internal static void ClearEditorSelectionIfTargeting(GameObject target)
        {
            if (target == null)
            {
                return;
            }

            GameObject[] selectedObjects = Selection.gameObjects;
            for (int i = 0; i < selectedObjects.Length; i++)
            {
                GameObject selected = selectedObjects[i];
                if (selected == null)
                {
                    continue;
                }

                if (selected == target || selected.transform.IsChildOf(target.transform))
                {
                    Selection.activeGameObject = null;
                    return;
                }
            }
        }
#else
        internal static void ClearEditorSelectionIfTargeting(GameObject target)
        {
        }
#endif

        private void ConfigureNavMeshSurface(ChunkData data)
        {
            if (data == null)
            {
                return;
            }

            if (_navMeshSurface != null)
            {
                Destroy(_navMeshSurface);
                _navMeshSurface = null;
            }

            _navMeshSurface = gameObject.AddComponent<NavMeshSurface>();
            _navMeshSurface.collectObjects = CollectObjects.Volume;
            _navMeshSurface.size = new Vector3(ChunkWorldSize, data.heightScale + 2f, ChunkWorldSize);
            _navMeshSurface.center = new Vector3(
                ChunkWorldSize * 0.5f,
                data.heightScale * 0.5f,
                ChunkWorldSize * 0.5f);
            _navMeshSurface.layerMask = LayerMask.GetMask(EnvironmentLayerName);
            _navMeshSurface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
        }

        private float[,] ConvertToUnityHeightmap(float[,] sourceHeights, int targetResolution)
        {
            if (sourceHeights == null)
            {
                return new float[targetResolution, targetResolution];
            }

            int sourceRes = sourceHeights.GetLength(0);
            var result = new float[targetResolution, targetResolution];

            float heightScale = _data != null ? _data.heightScale : ChunkData.DefaultHeightScale;
            if (heightScale <= 0f)
            {
                heightScale = 1f;
            }

            for (int tx = 0; tx < targetResolution; tx++)
            {
                for (int tz = 0; tz < targetResolution; tz++)
                {
                    float u = (float)tx / (targetResolution - 1);
                    float v = (float)tz / (targetResolution - 1);

                    float sourceX = u * (sourceRes - 1);
                    float sourceZ = v * (sourceRes - 1);

                    int x0 = Mathf.FloorToInt(sourceX);
                    int z0 = Mathf.FloorToInt(sourceZ);
                    int x1 = Mathf.Min(x0 + 1, sourceRes - 1);
                    int z1 = Mathf.Min(z0 + 1, sourceRes - 1);

                    float fx = sourceX - x0;
                    float fz = sourceZ - z0;

                    float h00 = sourceHeights[x0, z0];
                    float h10 = sourceHeights[x1, z0];
                    float h01 = sourceHeights[x0, z1];
                    float h11 = sourceHeights[x1, z1];

                    float h0 = Mathf.Lerp(h00, h10, fx);
                    float h1 = Mathf.Lerp(h01, h11, fx);

                    float rawHeight = Mathf.Lerp(h0, h1, fz);

                    result[tz, tx] = Mathf.Clamp01(rawHeight / heightScale);
                }
            }

            return result;
        }

        private void ApplyDefaultSplatmap(int layerCount)
        {
            if (layerCount == 0 || _terrainData == null)
            {
                return;
            }

            int alphamapRes = _terrainData.alphamapResolution;
            float[,,] splatmap = new float[alphamapRes, alphamapRes, layerCount];

            for (int x = 0; x < alphamapRes; x++)
            {
                for (int z = 0; z < alphamapRes; z++)
                {
                    if (layerCount == 1)
                    {
                        splatmap[z, x, 0] = 1f;
                        continue;
                    }

                    float u = (float)x / alphamapRes;
                    float v = (float)z / alphamapRes;
                    float slope = GetNormalizedSlopeAt(u, v);

                    splatmap[z, x, 0] = 1f - slope;
                    splatmap[z, x, 1] = slope;

                    for (int layer = 2; layer < layerCount; layer++)
                    {
                        splatmap[z, x, layer] = 0f;
                    }
                }
            }

            _terrainData.SetAlphamaps(0, 0, splatmap);
        }

        private float GetNormalizedSlopeAt(float u, float v)
        {
            if (_data?.heightMap == null)
            {
                return 0f;
            }

            int hmRes = _data.heightMap.GetLength(0);
            int x = Mathf.Clamp(Mathf.RoundToInt(u * (hmRes - 1)), 1, hmRes - 2);
            int z = Mathf.Clamp(Mathf.RoundToInt(v * (hmRes - 1)), 1, hmRes - 2);

            float slope = HeightmapGenerator.GetSlopeAngle(_data.heightMap, x, z);
            return Mathf.Clamp01(slope / 45f);
        }
    }
}

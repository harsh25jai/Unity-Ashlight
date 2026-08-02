using System.Collections;
using System.Collections.Generic;
using Ashlight.Systems;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.World
{
    /// <summary>
    /// Unity event wrapper for chunk data load notifications.
    /// </summary>
    [System.Serializable]
    public class ChunkDataEvent : UnityEvent<ChunkData> { }

    /// <summary>
    /// Unity event wrapper for chunk unload notifications.
    /// </summary>
    [System.Serializable]
    public class ChunkCoordEvent : UnityEvent<Vector2Int> { }

    /// <summary>
    /// Tracks player chunk position and loads or unloads generated chunk data.
    /// </summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    public class ChunkLoader : MonoBehaviour
    {
        private const float ChunkCheckIntervalSeconds = 0.5f;
        private const int UnloadBufferChunks = 3;
        private const float PlayerGroundClearance = 0.05f;
        private const string EnvironmentLayerName = "Environment";
        private const string ChunksParentName = "_Chunks";

        [SerializeField] private Transform player;
        [SerializeField] private int loadRadius = 5;
        [SerializeField] private WorldGenerator worldGenerator;
        [SerializeField] private TerrainLayer[] terrainLayers;
        [SerializeField] private GameObject[] largTreePrefabs;
        [SerializeField] private GameObject[] grassPrefabs;
        [SerializeField] private Transform chunksParent;
        [SerializeField] private bool disableLegacyGroundNavMesh = true;

        [Header("Events")]
        [SerializeField] private ChunkDataEvent _onChunkLoaded;
        [SerializeField] private ChunkCoordEvent _onChunkUnloaded;

        private readonly Dictionary<Vector2Int, ChunkData> _loadedChunks = new Dictionary<Vector2Int, ChunkData>();
        private readonly Dictionary<Vector2Int, GameObject> _chunkObjects = new Dictionary<Vector2Int, GameObject>();
        private readonly Dictionary<Vector2Int, Coroutine> _chunkLoadCoroutines = new Dictionary<Vector2Int, Coroutine>();

        private ChunkGenerator _generator;
        private Vector2Int _lastPlayerChunk;
        private Coroutine _chunkMonitorRoutine;

        /// <summary>Invoked when chunk data has been generated and is ready for visuals.</summary>
        public ChunkDataEvent OnChunkLoaded => _onChunkLoaded;

        /// <summary>Invoked when a chunk has been unloaded.</summary>
        public ChunkCoordEvent OnChunkUnloaded => _onChunkUnloaded;

        private void Awake()
        {
            if (worldGenerator == null)
            {
                worldGenerator = WorldGenerator.Instance ?? FindAnyObjectByType<WorldGenerator>();
            }

            if (worldGenerator == null)
            {
                Debug.LogError($"{nameof(ChunkLoader)} requires a {nameof(WorldGenerator)}.", this);
                enabled = false;
                return;
            }

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (player == null)
            {
                Debug.LogError($"{nameof(ChunkLoader)} requires a player {nameof(Transform)}.", this);
                enabled = false;
                return;
            }

            EnsureChunksParent();
            DisableLegacyGroundNavMesh();

            _generator = new ChunkGenerator(worldGenerator);
        }

        private void Start()
        {
            if (!enabled || worldGenerator == null || player == null)
            {
                return;
            }

            _onChunkLoaded ??= new ChunkDataEvent();
            OnChunkLoaded.AddListener(OnChunkLoaded_Debug);

            worldGenerator.ClearPlacedChurches();
            GenerateStartingChunks();
            _lastPlayerChunk = worldGenerator.WorldToChunkCoord(player.position);

            SaveSystem saveSystem = SaveSystem.Instance;
            if (saveSystem != null)
            {
                saveSystem.OnLoadComplete.AddListener(OnSaveLoaded);
            }
        }

        private void OnEnable()
        {
            if (!enabled || worldGenerator == null || player == null)
            {
                return;
            }

            if (_chunkMonitorRoutine != null)
            {
                StopCoroutine(_chunkMonitorRoutine);
            }

            _chunkMonitorRoutine = StartCoroutine(MonitorPlayerChunkRoutine());
        }

        private void OnDisable()
        {
            if (_chunkMonitorRoutine != null)
            {
                StopCoroutine(_chunkMonitorRoutine);
                _chunkMonitorRoutine = null;
            }
        }

        private void OnDestroy()
        {
            SaveSystem saveSystem = SaveSystem.Instance;
            if (saveSystem != null)
            {
                saveSystem.OnLoadComplete.RemoveListener(OnSaveLoaded);
            }
        }

        /// <summary>
        /// Gets loaded chunk data for a coordinate when available.
        /// </summary>
        /// <param name="coord">Chunk grid coordinate.</param>
        /// <returns>Chunk data or null when not loaded.</returns>
        public ChunkData GetChunkData(Vector2Int coord)
        {
            return _loadedChunks.TryGetValue(coord, out ChunkData data) ? data : null;
        }

        /// <summary>
        /// Returns whether a chunk is currently loaded.
        /// </summary>
        /// <param name="coord">Chunk grid coordinate.</param>
        /// <returns>True when chunk data is loaded.</returns>
        public bool IsChunkLoaded(Vector2Int coord)
        {
            return _loadedChunks.ContainsKey(coord);
        }

        /// <summary>
        /// Returns a snapshot list of all loaded chunk data.
        /// </summary>
        /// <returns>Loaded chunk data list.</returns>
        public List<ChunkData> GetAllLoadedChunks()
        {
            return new List<ChunkData>(_loadedChunks.Values);
        }

        private IEnumerator MonitorPlayerChunkRoutine()
        {
            var wait = new WaitForSeconds(ChunkCheckIntervalSeconds);

            while (true)
            {
                if (player != null && worldGenerator != null)
                {
                    Vector2Int currentChunk = worldGenerator.WorldToChunkCoord(player.position);
                    if (currentChunk != _lastPlayerChunk)
                    {
                        _lastPlayerChunk = currentChunk;
                        UpdateLoadedChunks(currentChunk);
                    }
                }

                yield return wait;
            }
        }

        private void UpdateLoadedChunks(Vector2Int centreChunk)
        {
            var desiredChunks = new HashSet<Vector2Int>();

            for (int x = centreChunk.x - loadRadius; x <= centreChunk.x + loadRadius; x++)
            {
                for (int y = centreChunk.y - loadRadius; y <= centreChunk.y + loadRadius; y++)
                {
                    desiredChunks.Add(new Vector2Int(x, y));
                }
            }

            var toLoad = new List<Vector2Int>();
            foreach (Vector2Int coord in desiredChunks)
            {
                if (!_loadedChunks.ContainsKey(coord))
                {
                    toLoad.Add(coord);
                }
            }

            for (int i = 0; i < toLoad.Count; i++)
            {
                LoadChunk(toLoad[i]);
            }

            int unloadDistance = loadRadius + UnloadBufferChunks;
            var toUnload = new List<Vector2Int>();
            foreach (Vector2Int loadedCoord in _loadedChunks.Keys)
            {
                int deltaX = Mathf.Abs(loadedCoord.x - centreChunk.x);
                int deltaY = Mathf.Abs(loadedCoord.y - centreChunk.y);
                if (deltaX > unloadDistance || deltaY > unloadDistance)
                {
                    toUnload.Add(loadedCoord);
                }
            }

            for (int i = 0; i < toUnload.Count; i++)
            {
                UnloadChunk(toUnload[i]);
            }
        }

        private void LoadChunk(Vector2Int coord)
        {
            if (_loadedChunks.ContainsKey(coord))
            {
                return;
            }

            ChunkData data = _generator.Generate(coord);
            MatchEdgeHeightsWithLoadedNeighbours(data);
            ApplySavedEvents(data);
            _loadedChunks[coord] = data;
            CreateTerrainMesh(data);
            _chunkLoadCoroutines[data.chunkCoord] = StartCoroutine(LoadChunkContentStaggered(data));
            _onChunkLoaded?.Invoke(data);
        }

        private void MatchEdgeHeightsWithLoadedNeighbours(ChunkData data)
        {
            if (data == null || data.heightMap == null)
            {
                return;
            }

            Vector2Int coord = data.chunkCoord;
            int matchCount = 0;

            if (_loadedChunks.TryGetValue(coord + Vector2Int.left, out ChunkData westData))
            {
                MatchEdge(data.heightMap, westData.heightMap, 0);
                matchCount++;
            }

            if (_loadedChunks.TryGetValue(coord + Vector2Int.right, out ChunkData eastData))
            {
                MatchEdge(data.heightMap, eastData.heightMap, 1);
                matchCount++;
            }

            if (_loadedChunks.TryGetValue(coord + Vector2Int.up, out ChunkData northData))
            {
                MatchEdge(data.heightMap, northData.heightMap, 2);
                matchCount++;
            }

            if (_loadedChunks.TryGetValue(coord + Vector2Int.down, out ChunkData southData))
            {
                MatchEdge(data.heightMap, southData.heightMap, 3);
                matchCount++;
            }

            Debug.Log($"Edge match for chunk {coord}: {matchCount} edges matched to loaded neighbours", this);
        }

        private static void MatchEdge(float[,] myHeights, float[,] neighbourHeights, int edgeDirection)
        {
            if (myHeights == null || neighbourHeights == null)
            {
                return;
            }

            int size = myHeights.GetLength(0);
            switch (edgeDirection)
            {
                case 0:
                    for (int z = 0; z < size; z++)
                    {
                        myHeights[0, z] = neighbourHeights[size - 1, z];
                    }

                    break;
                case 1:
                    for (int z = 0; z < size; z++)
                    {
                        myHeights[size - 1, z] = neighbourHeights[0, z];
                    }

                    break;
                case 2:
                    for (int x = 0; x < size; x++)
                    {
                        myHeights[x, size - 1] = neighbourHeights[x, 0];
                    }

                    break;
                case 3:
                    for (int x = 0; x < size; x++)
                    {
                        myHeights[x, 0] = neighbourHeights[x, size - 1];
                    }

                    break;
            }
        }

        private void UnloadChunk(Vector2Int coord)
        {
            if (!_loadedChunks.Remove(coord))
            {
                return;
            }

            if (_chunkLoadCoroutines.TryGetValue(coord, out Coroutine loadRoutine))
            {
                StopCoroutine(loadRoutine);
                _chunkLoadCoroutines.Remove(coord);
            }

            if (_chunkObjects.TryGetValue(coord, out GameObject chunkObject))
            {
                TerrainChunkUnity terrain = chunkObject.GetComponent<TerrainChunkUnity>();
                if (terrain != null)
                {
                    terrain.DestroyChunk();
                }
                else
                {
                    TerrainChunkUnity.ClearEditorSelectionIfTargeting(chunkObject);
                    Destroy(chunkObject);
                }

                _chunkObjects.Remove(coord);
            }

            _onChunkUnloaded?.Invoke(coord);
        }

        private void CreateTerrainMesh(ChunkData data)
        {
            if (data == null || worldGenerator == null)
            {
                return;
            }

            if (terrainLayers == null || terrainLayers.Length == 0)
            {
                Debug.LogWarning($"{nameof(ChunkLoader)} has no {nameof(terrainLayers)} assigned.", this);
            }

            EnsureChunksParent();

            SmoothWithNeighbours(data);

            Vector3 worldOrigin = worldGenerator.ChunkCoordToWorldOrigin(data.chunkCoord);
            var chunkObject = new GameObject($"Chunk_{data.chunkCoord.x}_{data.chunkCoord.y}");
            chunkObject.transform.SetParent(chunksParent, false);
            chunkObject.transform.position = worldOrigin;

            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer >= 0)
            {
                chunkObject.layer = environmentLayer;
            }

            TerrainChunkUnity terrain = chunkObject.AddComponent<TerrainChunkUnity>();
            terrain.Initialise(data, terrainLayers);
            _chunkObjects[data.chunkCoord] = chunkObject;

            LinkNeighbourTerrains(data.chunkCoord, terrain);

            var treesParent = new GameObject("Trees").transform;
            treesParent.SetParent(chunkObject.transform, false);
            treesParent.localPosition = Vector3.zero;

            var scatterParent = new GameObject("Scatter").transform;
            scatterParent.SetParent(chunkObject.transform, false);
            scatterParent.localPosition = Vector3.zero;

            TreePlacer treePlacer = chunkObject.AddComponent<TreePlacer>();
            treePlacer.Initialise(largTreePrefabs, largTreePrefabs, treesParent);

            GroundScatterPlacer scatterPlacer = chunkObject.AddComponent<GroundScatterPlacer>();
            scatterPlacer.Initialise(grassPrefabs, null, scatterParent);
        }

        private IEnumerator LoadChunkContentStaggered(ChunkData data)
        {
            if (data == null)
            {
                yield break;
            }

            Vector2Int coord = data.chunkCoord;

            try
            {
                if (!_chunkObjects.TryGetValue(coord, out GameObject chunkObject) || chunkObject == null)
                {
                    yield break;
                }

                TerrainChunkUnity terrain = chunkObject.GetComponent<TerrainChunkUnity>();
                TreePlacer treePlacer = chunkObject.GetComponent<TreePlacer>();
                GroundScatterPlacer scatterPlacer = chunkObject.GetComponent<GroundScatterPlacer>();

                if (terrain == null || treePlacer == null || scatterPlacer == null)
                {
                    yield break;
                }

                // TODO: Replace with treePlacer.PlaceTreesUnityTerrainAsync when frame budgeting is added.
                treePlacer.PlaceTreesUnityTerrain(data, terrain);
                yield return null;

                if (!IsChunkLoaded(coord) || chunkObject == null)
                {
                    yield break;
                }

                var rng = new System.Random(data.chunkSeed + 1);
                yield return scatterPlacer.PlaceGrassPatchesAsync(data, terrain, rng);
                yield return null;

                if (!IsChunkLoaded(coord) || chunkObject == null || terrain == null)
                {
                    yield break;
                }

                scatterPlacer.PlaceRocksUnityTerrain(data, terrain);
                yield return null;

                if (!IsChunkLoaded(coord) || chunkObject == null || terrain == null)
                {
                    yield break;
                }

                terrain.BuildNavMeshAsync();
            }
            finally
            {
                _chunkLoadCoroutines.Remove(coord);
            }
        }

        private void LinkNeighbourTerrains(Vector2Int coord, TerrainChunkUnity terrain)
        {
            TerrainChunkUnity left = GetNeighbourTerrain(coord + Vector2Int.left);
            TerrainChunkUnity right = GetNeighbourTerrain(coord + Vector2Int.right);
            TerrainChunkUnity top = GetNeighbourTerrain(coord + Vector2Int.up);
            TerrainChunkUnity bottom = GetNeighbourTerrain(coord + Vector2Int.down);

            terrain.SetNeighbours(left, top, right, bottom);

            if (left != null)
            {
                TerrainChunkUnity leftLeft = GetNeighbourTerrain(coord + Vector2Int.left + Vector2Int.left);
                TerrainChunkUnity leftTop = GetNeighbourTerrain(coord + Vector2Int.left + Vector2Int.up);
                TerrainChunkUnity leftBottom = GetNeighbourTerrain(coord + Vector2Int.left + Vector2Int.down);
                left.SetNeighbours(leftLeft, leftTop, terrain, leftBottom);
            }

            if (right != null)
            {
                TerrainChunkUnity rightRight = GetNeighbourTerrain(coord + Vector2Int.right + Vector2Int.right);
                TerrainChunkUnity rightTop = GetNeighbourTerrain(coord + Vector2Int.right + Vector2Int.up);
                TerrainChunkUnity rightBottom = GetNeighbourTerrain(coord + Vector2Int.right + Vector2Int.down);
                right.SetNeighbours(terrain, rightTop, rightRight, rightBottom);
            }

            if (top != null)
            {
                TerrainChunkUnity topLeft = GetNeighbourTerrain(coord + Vector2Int.up + Vector2Int.left);
                TerrainChunkUnity topRight = GetNeighbourTerrain(coord + Vector2Int.up + Vector2Int.right);
                TerrainChunkUnity topTop = GetNeighbourTerrain(coord + Vector2Int.up + Vector2Int.up);
                top.SetNeighbours(topLeft, topTop, topRight, terrain);
            }

            if (bottom != null)
            {
                TerrainChunkUnity bottomLeft = GetNeighbourTerrain(coord + Vector2Int.down + Vector2Int.left);
                TerrainChunkUnity bottomRight = GetNeighbourTerrain(coord + Vector2Int.down + Vector2Int.right);
                TerrainChunkUnity bottomBottom = GetNeighbourTerrain(coord + Vector2Int.down + Vector2Int.down);
                bottom.SetNeighbours(bottomLeft, terrain, bottomRight, bottomBottom);
            }
        }

        private TerrainChunkUnity GetNeighbourTerrain(Vector2Int coord)
        {
            if (!_chunkObjects.TryGetValue(coord, out GameObject chunkObject))
            {
                return null;
            }

            return chunkObject.GetComponent<TerrainChunkUnity>();
        }

        private void SmoothWithNeighbours(ChunkData data)
        {
            Vector2Int coord = data.chunkCoord;

            ChunkData north = GetChunkData(coord + Vector2Int.up);
            ChunkData east = GetChunkData(coord + Vector2Int.right);
            ChunkData south = GetChunkData(coord + Vector2Int.down);
            ChunkData west = GetChunkData(coord + Vector2Int.left);

            HeightmapGenerator.SmoothChunkEdges(
                data.heightMap,
                north?.heightMap,
                east?.heightMap,
                south?.heightMap,
                west?.heightMap);

            RebuildTerrain(coord + Vector2Int.up);
            RebuildTerrain(coord + Vector2Int.right);
            RebuildTerrain(coord + Vector2Int.down);
            RebuildTerrain(coord + Vector2Int.left);
        }

        private void RebuildTerrain(Vector2Int coord)
        {
            if (!_chunkObjects.TryGetValue(coord, out GameObject chunkObject))
            {
                return;
            }

            if (!_loadedChunks.TryGetValue(coord, out ChunkData data))
            {
                return;
            }

            TerrainChunkUnity terrain = chunkObject.GetComponent<TerrainChunkUnity>();
            terrain?.RefreshFromData(data);
        }

        private void ApplySavedEvents(ChunkData data)
        {
            if (data == null || data.churchSpawns == null)
            {
                return;
            }

            SaveSystem saveSystem = SaveSystem.Instance;
            if (saveSystem == null)
            {
                return;
            }

            for (int i = 0; i < data.churchSpawns.Count; i++)
            {
                ChurchSpawnData church = data.churchSpawns[i];
                if (church == null || string.IsNullOrEmpty(church.churchID))
                {
                    continue;
                }

                church.isActivated = saveSystem.IsShrineActivated(church.churchID);
            }
        }

        private void GenerateStartingChunks()
        {
            Vector2Int centreChunk = worldGenerator.WorldToChunkCoord(player.position);
            UpdateLoadedChunks(centreChunk);
            SnapPlayerToTerrain();
            Physics.SyncTransforms();
        }

        private void OnSaveLoaded()
        {
            if (!enabled || worldGenerator == null || player == null)
            {
                return;
            }

            Vector2Int centreChunk = worldGenerator.WorldToChunkCoord(player.position);
            _lastPlayerChunk = centreChunk;
            UpdateLoadedChunks(centreChunk);
            SnapPlayerToTerrain();
            Physics.SyncTransforms();
        }

        private void SnapPlayerToTerrain()
        {
            if (player == null || worldGenerator == null)
            {
                return;
            }

            Vector2Int chunkCoord = worldGenerator.WorldToChunkCoord(player.position);
            if (!_chunkObjects.TryGetValue(chunkCoord, out GameObject chunkObject) || chunkObject == null)
            {
                return;
            }

            TerrainChunkUnity terrain = chunkObject.GetComponent<TerrainChunkUnity>();
            if (terrain == null)
            {
                return;
            }

            float terrainHeight = terrain.GetHeightAtWorldPos(player.position.x, player.position.z);
            CharacterController characterController = player.GetComponent<CharacterController>();
            float footOffset = characterController != null
                ? characterController.height * 0.5f - characterController.center.y + characterController.skinWidth
                : 1f;

            Vector3 snappedPosition = player.position;
            snappedPosition.y = terrainHeight + footOffset + PlayerGroundClearance;

            if (characterController != null)
            {
                characterController.enabled = false;
                player.position = snappedPosition;
                characterController.enabled = true;
            }
            else
            {
                player.position = snappedPosition;
            }
        }

        private void EnsureChunksParent()
        {
            if (chunksParent != null)
            {
                return;
            }

            GameObject parentObject = GameObject.Find(ChunksParentName);
            if (parentObject == null)
            {
                parentObject = new GameObject(ChunksParentName);
            }

            chunksParent = parentObject.transform;
        }

        private void DisableLegacyGroundNavMesh()
        {
            if (!disableLegacyGroundNavMesh)
            {
                return;
            }

            GameObject ground = GameObject.Find("Ground");
            if (ground == null)
            {
                return;
            }

            NavMeshSurface legacySurface = ground.GetComponent<NavMeshSurface>();
            if (legacySurface != null)
            {
                legacySurface.enabled = false;
            }
        }

        private void OnChunkLoaded_Debug(ChunkData data)
        {
            Debug.Log(
                $"Chunk {data.chunkCoord} loaded — " +
                $"trees: {data.mediumTreePositions.Count}, " +
                $"church: {data.churchSpawns.Count > 0}, " +
                $"biome: {data.biomeType}",
                this);
        }
    }
}

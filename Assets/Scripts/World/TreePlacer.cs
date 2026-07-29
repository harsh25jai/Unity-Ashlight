using System.Collections.Generic;
using UnityEngine;

namespace Ashlight.World
{
    /// <summary>
    /// Instantiates tree prefabs on a terrain chunk from generated <see cref="ChunkData"/> positions.
    /// </summary>
    [DisallowMultipleComponent]
    public class TreePlacer : MonoBehaviour
    {
        private const float LargeTreeMinScale = 1.4f;
        private const float LargeTreeMaxScale = 2.0f;
        private const float MediumTreeMinScale = 0.7f;
        private const float MediumTreeMaxScale = 1.2f;
        private const float MaxTreeSlopeDegrees = 35f;
        private const float TiltSlopeThresholdDegrees = 10f;
        private const string EnvironmentLayerName = "Environment";

        [SerializeField] private GameObject[] largTreePrefabs;
        [SerializeField] private GameObject[] mediumTreePrefabs;
        [SerializeField] private Transform treesParent;

        private readonly List<GameObject> _spawnedTrees = new List<GameObject>();

        /// <summary>
        /// Configures prefab arrays and the parent transform at runtime.
        /// </summary>
        /// <param name="largePrefabs">Prefabs used for large trees.</param>
        /// <param name="mediumPrefabs">Prefabs used for medium trees.</param>
        /// <param name="parent">Parent transform for spawned tree instances.</param>
        public void Initialise(GameObject[] largePrefabs, GameObject[] mediumPrefabs, Transform parent)
        {
            largTreePrefabs = largePrefabs;
            mediumTreePrefabs = mediumPrefabs;
            treesParent = parent;
        }

        /// <summary>
        /// Places large and medium trees on a Unity terrain chunk.
        /// </summary>
        /// <param name="data">Generated chunk content.</param>
        /// <param name="terrain">Unity terrain used for height and slope sampling.</param>
        public void PlaceTreesUnityTerrain(ChunkData data, TerrainChunkUnity terrain)
        {
            ClearTrees();

            if (data == null || terrain == null || treesParent == null)
            {
                return;
            }

            if (largTreePrefabs == null || largTreePrefabs.Length == 0)
            {
                return;
            }

            var rng = new System.Random(data.chunkSeed);

            if (data.largTreePositions != null)
            {
                foreach (Vector3 worldPosition in data.largTreePositions)
                {
                    PlaceTree(
                        worldPosition,
                        terrain,
                        rng,
                        largTreePrefabs,
                        LargeTreeMinScale,
                        LargeTreeMaxScale);
                }
            }

            GameObject[] mediumPrefabs = mediumTreePrefabs != null && mediumTreePrefabs.Length > 0
                ? mediumTreePrefabs
                : largTreePrefabs;

            if (data.mediumTreePositions != null)
            {
                foreach (Vector3 worldPosition in data.mediumTreePositions)
                {
                    PlaceTree(
                        worldPosition,
                        terrain,
                        rng,
                        mediumPrefabs,
                        MediumTreeMinScale,
                        MediumTreeMaxScale);
                }
            }
        }

        /// <summary>
        /// Destroys all trees spawned by this placer.
        /// </summary>
        public void ClearTrees()
        {
            for (int i = 0; i < _spawnedTrees.Count; i++)
            {
                if (_spawnedTrees[i] != null)
                {
                    Destroy(_spawnedTrees[i]);
                }
            }

            _spawnedTrees.Clear();
        }

        private void PlaceTree(
            Vector3 worldPosition,
            TerrainChunkUnity terrain,
            System.Random rng,
            GameObject[] prefabs,
            float minScale,
            float maxScale)
        {
            if (prefabs == null || prefabs.Length == 0)
            {
                return;
            }

            worldPosition.y = terrain.GetHeightAtWorldPos(worldPosition.x, worldPosition.z);

            float slope = terrain.GetSlopeAtWorldPos(worldPosition.x, worldPosition.z);
            if (slope > MaxTreeSlopeDegrees)
            {
                return;
            }

            int prefabIndex = rng.Next(0, prefabs.Length);
            GameObject prefab = prefabs[prefabIndex];
            if (prefab == null)
            {
                return;
            }

            GameObject tree = Instantiate(prefab, worldPosition, Quaternion.identity, treesParent);

            float randomYRotation = (float)(rng.NextDouble() * 360.0);
            tree.transform.rotation = Quaternion.Euler(0f, randomYRotation, 0f);

            float scale = minScale + (float)(rng.NextDouble() * (maxScale - minScale));
            tree.transform.localScale = Vector3.one * scale;

            if (slope > TiltSlopeThresholdDegrees)
            {
                float tiltAmount = slope * 0.15f;
                tree.transform.rotation *= Quaternion.Euler(
                    (float)(rng.NextDouble() - 0.5) * tiltAmount,
                    0f,
                    (float)(rng.NextDouble() - 0.5) * tiltAmount);
            }

            int environmentLayer = LayerMask.NameToLayer(EnvironmentLayerName);
            if (environmentLayer >= 0)
            {
                tree.layer = environmentLayer;
            }

            _spawnedTrees.Add(tree);
        }
    }
}

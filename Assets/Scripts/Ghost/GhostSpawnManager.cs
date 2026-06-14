using System.Collections;
using System.Collections.Generic;
using Ashlight.Environment;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Weighted spawn point entry for ghost placement.
    /// </summary>
    [System.Serializable]
    public class WeightedSpawnPoint
    {
        [SerializeField] private Transform point;
        [SerializeField] private float weight = 1f;

        /// <summary>Gets the spawn transform.</summary>
        public Transform Point => point;

        /// <summary>Gets the selection weight.</summary>
        public float Weight => Mathf.Max(0f, weight);
    }

    /// <summary>
    /// Pool and spawn configuration for a single ghost archetype.
    /// </summary>
    [System.Serializable]
    public class GhostSpawnEntry
    {
        public string ghostName;
        public GhostAIController prefab;
        public int poolSize = 3;
        public float spawnWeight = 1f;
    }

    /// <summary>
    /// Pools ghost instances and spawns them based on day/night phase limits.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostSpawnManager : MonoBehaviour
    {
        private const float SpawnIntervalMin = 30f;
        private const float SpawnIntervalMax = 60f;

        [SerializeField] private List<GhostSpawnEntry> ghostSpawnEntries = new List<GhostSpawnEntry>();
        [SerializeField] private List<WeightedSpawnPoint> spawnPoints = new List<WeightedSpawnPoint>();
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private Transform player;
        [SerializeField] private HolyTorch playerTorch;

        private readonly Dictionary<string, Queue<GhostAIController>> _pools =
            new Dictionary<string, Queue<GhostAIController>>();

        private readonly Dictionary<GhostAIController, string> _ghostPoolKeys =
            new Dictionary<GhostAIController, string>();

        private readonly List<GhostAIController> _activeGhosts = new List<GhostAIController>();
        private readonly List<GhostAIController> _activeGhostsQuery = new List<GhostAIController>();

        private int _maxActiveGhosts = 3;
        private Coroutine _spawnCoroutine;
        private BoxCollider _spawnExclusionZone;

        private void Awake()
        {
            if (ghostSpawnEntries == null || ghostSpawnEntries.Count == 0)
            {
                Debug.LogError($"{nameof(GhostSpawnManager)} requires at least one {nameof(GhostSpawnEntry)}.", this);
                enabled = false;
                return;
            }

            bool hasValidEntry = false;
            foreach (GhostSpawnEntry entry in ghostSpawnEntries)
            {
                if (entry != null && entry.prefab != null)
                {
                    hasValidEntry = true;
                    break;
                }
            }

            if (!hasValidEntry)
            {
                Debug.LogError($"{nameof(GhostSpawnManager)} requires at least one valid ghost prefab.", this);
                enabled = false;
                return;
            }

            if (spawnPoints == null || spawnPoints.Count == 0)
            {
                Debug.LogError($"{nameof(GhostSpawnManager)} requires at least one spawn point.", this);
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

            if (playerTorch == null && player != null)
            {
                playerTorch = player.GetComponentInChildren<HolyTorch>();
            }

            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }

            if (dayNightCycle == null)
            {
                Debug.LogWarning($"{nameof(GhostSpawnManager)} has no {nameof(DayNightCycle)} reference.", this);
            }
        }

        private void Start()
        {
            BuildPool();

            if (dayNightCycle != null)
            {
                _maxActiveGhosts = GetMaxActiveGhostsForPhase(dayNightCycle.CurrentPhase);
                dayNightCycle.OnPhaseChanged.AddListener(OnPhaseChanged);
            }

            _spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        private void OnDestroy()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                SpawnSpecific(0);
            }

            if (Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                SpawnSpecific(1);
            }

            if (Keyboard.current.digit3Key.wasPressedThisFrame)
            {
                SpawnSpecific(2);
            }

            if (Keyboard.current.digit4Key.wasPressedThisFrame)
            {
                SpawnSpecific(3);
            }

            if (Keyboard.current.digit5Key.wasPressedThisFrame)
            {
                SpawnSpecific(4);
            }
        }

        /// <summary>Blocks ghost spawns inside the given trigger volume.</summary>
        /// <param name="exclusionZone">Church or safe-zone collider.</param>
        public void SetSpawnExclusionZone(BoxCollider exclusionZone)
        {
            _spawnExclusionZone = exclusionZone;
        }

        /// <summary>Clears the active spawn exclusion zone.</summary>
        public void ClearSpawnExclusionZone()
        {
            _spawnExclusionZone = null;
        }

        /// <summary>Gets currently active spawned ghosts.</summary>
        public List<GhostAIController> ActiveGhosts
        {
            get
            {
                _activeGhostsQuery.Clear();
                CleanupInactiveGhosts();

                foreach (GhostAIController ghost in _activeGhosts)
                {
                    if (ghost != null && ghost.gameObject.activeSelf)
                    {
                        _activeGhostsQuery.Add(ghost);
                    }
                }

                return _activeGhostsQuery;
            }
        }

        /// <summary>
        /// Spawns a random ghost type weighted by <see cref="GhostSpawnEntry.spawnWeight"/>.
        /// </summary>
        public void SpawnGhost()
        {
            if (_activeGhosts.Count >= _maxActiveGhosts)
            {
                return;
            }

            GhostSpawnEntry entry = PickRandomSpawnEntry();
            if (entry == null)
            {
                return;
            }

            TrySpawnEntry(entry);
        }

        /// <summary>
        /// Spawns a specific ghost type by index for debug and scripted encounters.
        /// </summary>
        /// <param name="index">Index into <see cref="ghostSpawnEntries"/>.</param>
        public void SpawnSpecific(int index)
        {
            if (_activeGhosts.Count >= _maxActiveGhosts)
            {
                return;
            }

            if (ghostSpawnEntries == null || index < 0 || index >= ghostSpawnEntries.Count)
            {
                Debug.LogWarning($"{nameof(GhostSpawnManager)} could not spawn ghost at index {index}.", this);
                return;
            }

            GhostSpawnEntry entry = ghostSpawnEntries[index];
            if (entry == null || entry.prefab == null)
            {
                Debug.LogWarning($"{nameof(GhostSpawnManager)} has no prefab at index {index}.", this);
                return;
            }

            TrySpawnEntry(entry);
        }

        /// <summary>
        /// Returns the maximum active ghosts for a day/night phase.
        /// </summary>
        /// <param name="phase">Current day/night phase.</param>
        /// <returns>Maximum concurrent active ghosts.</returns>
        public static int GetMaxActiveGhostsForPhase(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return 3;
                case DayNightPhase.Night_Early:
                    return 6;
                case DayNightPhase.Night_Deep:
                    return int.MaxValue;
                default:
                    return 4;
            }
        }

        private void BuildPool()
        {
            if (player == null)
            {
                Debug.LogError($"{nameof(GhostSpawnManager)} requires a player {nameof(Transform)} before building the pool.", this);
                return;
            }

            _pools.Clear();
            _ghostPoolKeys.Clear();

            foreach (GhostSpawnEntry entry in ghostSpawnEntries)
            {
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                string poolKey = GetPoolKey(entry);
                if (!_pools.TryGetValue(poolKey, out Queue<GhostAIController> queue))
                {
                    queue = new Queue<GhostAIController>();
                    _pools[poolKey] = queue;
                }

                int size = Mathf.Max(1, entry.poolSize);
                for (int i = 0; i < size; i++)
                {
                    GhostAIController instance = Instantiate(entry.prefab, Vector3.zero, Quaternion.identity, transform);
                    instance.SetPlayer(player);
                    instance.SetTorch(playerTorch);
                    instance.gameObject.SetActive(false);
                    queue.Enqueue(instance);
                    _ghostPoolKeys[instance] = poolKey;
                }
            }
        }

        private IEnumerator SpawnRoutine()
        {
            while (enabled)
            {
                CleanupInactiveGhosts();

                if (_activeGhosts.Count < _maxActiveGhosts)
                {
                    SpawnGhost();
                }

                float waitDuration = Random.Range(SpawnIntervalMin, SpawnIntervalMax);
                yield return new WaitForSeconds(waitDuration);
            }
        }

        private void OnPhaseChanged(DayNightPhase phase)
        {
            _maxActiveGhosts = GetMaxActiveGhostsForPhase(phase);
        }

        private void CleanupInactiveGhosts()
        {
            for (int i = _activeGhosts.Count - 1; i >= 0; i--)
            {
                GhostAIController ghost = _activeGhosts[i];
                if (ghost == null || !ghost.IsSpawnActive)
                {
                    if (ghost != null)
                    {
                        ReturnToPool(ghost);
                    }

                    _activeGhosts.RemoveAt(i);
                }
            }
        }

        private void TrySpawnEntry(GhostSpawnEntry entry)
        {
            GhostAIController ghost = GetAvailableGhost(entry);
            if (ghost == null)
            {
                return;
            }

            Transform spawnPoint = PickRandomSpawnPoint();
            if (spawnPoint == null)
            {
                ReturnToPool(ghost);
                return;
            }

            Vector3 spawnPosition = spawnPoint.position;
            if (IsPositionInExclusionZone(spawnPosition))
            {
                ReturnToPool(ghost);
                return;
            }

            ghost.transform.position = spawnPosition;

            NavMeshAgent agent = ghost.GetComponent<NavMeshAgent>();
            if (agent != null)
            {
                agent.enabled = true;
                agent.Warp(spawnPosition);
            }

            ghost.gameObject.SetActive(true);
            ghost.SetPlayer(player);
            ghost.SetTorch(playerTorch);
            ghost.Activate(player);
            _activeGhosts.Add(ghost);
        }

        private GhostSpawnEntry PickRandomSpawnEntry()
        {
            float totalWeight = 0f;

            foreach (GhostSpawnEntry entry in ghostSpawnEntries)
            {
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                totalWeight += Mathf.Max(0f, entry.spawnWeight);
            }

            if (totalWeight <= 0f)
            {
                return null;
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (GhostSpawnEntry entry in ghostSpawnEntries)
            {
                if (entry == null || entry.prefab == null)
                {
                    continue;
                }

                cumulative += Mathf.Max(0f, entry.spawnWeight);
                if (roll <= cumulative)
                {
                    return entry;
                }
            }

            return null;
        }

        private GhostAIController GetAvailableGhost(GhostSpawnEntry entry)
        {
            string poolKey = GetPoolKey(entry);

            if (!_pools.TryGetValue(poolKey, out Queue<GhostAIController> queue) || queue.Count == 0)
            {
                return null;
            }

            int attempts = queue.Count;

            while (attempts-- > 0)
            {
                GhostAIController ghost = queue.Dequeue();

                if (ghost != null && !ghost.gameObject.activeSelf)
                {
                    _ghostPoolKeys[ghost] = poolKey;
                    return ghost;
                }

                queue.Enqueue(ghost);
            }

            return null;
        }

        private void ReturnToPool(GhostAIController ghost)
        {
            if (ghost == null)
            {
                return;
            }

            if (!_ghostPoolKeys.TryGetValue(ghost, out string poolKey) || string.IsNullOrEmpty(poolKey))
            {
                return;
            }

            if (!_pools.TryGetValue(poolKey, out Queue<GhostAIController> queue))
            {
                queue = new Queue<GhostAIController>();
                _pools[poolKey] = queue;
            }

            if (!queue.Contains(ghost))
            {
                queue.Enqueue(ghost);
            }
        }

        private static string GetPoolKey(GhostSpawnEntry entry)
        {
            if (!string.IsNullOrEmpty(entry.ghostName))
            {
                return entry.ghostName;
            }

            return entry.prefab != null ? entry.prefab.name : "UnknownGhost";
        }

        private Transform PickRandomSpawnPoint()
        {
            List<Transform> validPoints = new List<Transform>();

            foreach (WeightedSpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint?.Point != null && !IsPositionInExclusionZone(spawnPoint.Point.position))
                {
                    validPoints.Add(spawnPoint.Point);
                }
            }

            if (validPoints.Count == 0)
            {
                return null;
            }

            return validPoints[Random.Range(0, validPoints.Count)];
        }

        private bool IsPositionInExclusionZone(Vector3 position)
        {
            if (_spawnExclusionZone == null)
            {
                return false;
            }

            return _spawnExclusionZone.bounds.Contains(position);
        }
    }
}

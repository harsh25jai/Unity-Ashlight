using System.Collections;
using System.Collections.Generic;
using Ashlight.Environment;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.AI;

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
    /// Pools ghost instances and spawns them based on day/night phase limits.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostSpawnManager : MonoBehaviour
    {
        private const int PoolSize = 10;
        private const float SpawnIntervalMin = 30f;
        private const float SpawnIntervalMax = 60f;

        [SerializeField] private GhostAIController ghostPrefab;
        [SerializeField] private List<WeightedSpawnPoint> spawnPoints = new List<WeightedSpawnPoint>();
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private Transform player;
        [SerializeField] private HolyTorch playerTorch;

        private readonly List<GhostAIController> _pool = new List<GhostAIController>();
        private readonly List<GhostAIController> _activeGhosts = new List<GhostAIController>();
        private int _maxActiveGhosts = 3;
        private Coroutine _spawnCoroutine;
        private BoxCollider _spawnExclusionZone;

        private void Awake()
        {
            if (ghostPrefab == null)
            {
                Debug.LogError($"{nameof(GhostSpawnManager)} requires a {nameof(GhostAIController)} prefab.", this);
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

        /// <summary>
        /// Spawns a ghost when pool capacity and phase limits allow.
        /// </summary>
        public void SpawnGhost()
        {
            if (_activeGhosts.Count >= _maxActiveGhosts)
            {
                return;
            }

            GhostAIController ghost = GetAvailableGhost();
            if (ghost == null)
            {
                return;
            }

            Transform spawnPoint = PickRandomSpawnPoint();
            if (spawnPoint == null)
            {
                return;
            }

            Vector3 spawnPosition = spawnPoint.position;
            if (IsPositionInExclusionZone(spawnPosition))
            {
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
            ghost.Activate(player);
            _activeGhosts.Add(ghost);
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

            for (int i = 0; i < PoolSize; i++)
            {
                GhostAIController instance = Instantiate(ghostPrefab, Vector3.zero, Quaternion.identity, transform);
                instance.SetPlayer(player);
                instance.SetTorch(playerTorch);
                _pool.Add(instance);
                instance.gameObject.SetActive(false);
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
                    _activeGhosts.RemoveAt(i);
                }
            }
        }

        private GhostAIController GetAvailableGhost()
        {
            foreach (GhostAIController ghost in _pool)
            {
                if (ghost != null && !ghost.gameObject.activeSelf)
                {
                    return ghost;
                }
            }

            return null;
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

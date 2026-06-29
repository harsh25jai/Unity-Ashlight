using System;
using System.Collections;
using Ashlight.Ghost;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Environment
{
    /// <summary>
    /// Church type that determines save-point availability and worship features offered in the zone.
    /// </summary>
    public enum ChurchType
    {
        SimpleAltar,
        Candle,
        HolyWater,
        Priest,
        MultiBenefit
    }

    /// <summary>
    /// Church safe zone that repels ghosts and regenerates player stamina regardless of church type.
    /// </summary>
    [DisallowMultipleComponent]
    public class ChurchSafeZone : MonoBehaviour
    {
        private const float StaminaRegenPerSecond = 10f;
        private const float GhostRepelRadius = 20f;
        private const float ProximityInsideThreshold = 0.05f;

        [SerializeField] private ChurchType churchType = ChurchType.SimpleAltar;
        [SerializeField] private GhostSpawnManager ghostSpawnManager;
        [SerializeField] private BoxCollider safeZoneCollider;
        [SerializeField] private bool useProximityFallback = true;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPlayerEntered;
        [SerializeField] private UnityEvent _onPlayerExited;

        private Coroutine _regenCoroutine;
        private PlayerController _activePlayerController;
        private Transform _playerTransform;
        private bool _isPlayerInside;

        /// <summary>Raised when the player enters the church safe zone.</summary>
        public static event Action OnPlayerEnterChurch;

        /// <summary>Raised when the player leaves the church safe zone.</summary>
        public static event Action OnPlayerExitChurch;

        /// <summary>Gets whether this church offers a save point when worshipped.</summary>
        public bool HasSavePoint => churchType != ChurchType.SimpleAltar;

        /// <summary>Gets the configured church type for this safe zone.</summary>
        public ChurchType Type => churchType;

        /// <summary>Invoked when the player enters the church safe zone.</summary>
        public UnityEvent OnPlayerEntered => _onPlayerEntered;

        /// <summary>Invoked when the player leaves the church safe zone.</summary>
        public UnityEvent OnPlayerExited => _onPlayerExited;

        private void Start()
        {
            if (safeZoneCollider == null)
            {
                safeZoneCollider = GetComponent<BoxCollider>();
            }

            if (safeZoneCollider == null)
            {
                Debug.LogError($"{nameof(ChurchSafeZone)} requires a {nameof(BoxCollider)}.", this);
                enabled = false;
                return;
            }

            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                CharacterController characterController = FindAnyObjectByType<CharacterController>();
                if (characterController != null)
                {
                    playerObject = characterController.gameObject;
                }
            }

            if (playerObject != null)
            {
                _playerTransform = playerObject.transform;
                EnsurePlayerTriggerPhysics(playerObject);
            }

            if (ghostSpawnManager == null)
            {
                ghostSpawnManager = FindAnyObjectByType<GhostSpawnManager>();
            }

            if (_playerTransform == null)
            {
                Debug.LogWarning($"{nameof(ChurchSafeZone)} could not resolve the player transform.", this);
            }
        }

        private void Update()
        {
            if (!useProximityFallback || safeZoneCollider == null || _playerTransform == null)
            {
                return;
            }

            bool isInside = IsPositionInsideSafeZone(_playerTransform.position);

            if (isInside && !_isPlayerInside)
            {
                ProcessPlayerEnter(null);
            }
            else if (!isInside && _isPlayerInside)
            {
                ProcessPlayerExit(null);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            ProcessPlayerEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            ProcessPlayerExit(other);
        }

        /// <summary>
        /// Forces all active ghosts within range to retreat from the church.
        /// </summary>
        public void RepelNearbyGhosts()
        {
            Vector3 repelOrigin = safeZoneCollider != null
                ? safeZoneCollider.bounds.center
                : transform.position;

            GhostAIController[] ghosts = FindObjectsByType<GhostAIController>();
            foreach (GhostAIController ghost in ghosts)
            {
                if (ghost == null || !ghost.IsSpawnActive)
                {
                    continue;
                }

                float distance = Vector3.Distance(repelOrigin, ghost.transform.position);
                if (distance <= GhostRepelRadius)
                {
                    ghost.ForceRetreat();
                }
            }
        }

        private void EnsurePlayerTriggerPhysics(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return;
            }

            Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                return;
            }

            playerRigidbody = playerObject.AddComponent<Rigidbody>();
            playerRigidbody.isKinematic = true;
            playerRigidbody.useGravity = false;
        }

        private void ProcessPlayerEnter(Collider playerCollider)
        {
            if (_isPlayerInside)
            {
                return;
            }

            _isPlayerInside = true;
            _activePlayerController = playerCollider != null
                ? playerCollider.GetComponent<PlayerController>()
                : _playerTransform != null ? _playerTransform.GetComponent<PlayerController>() : null;

            if (_regenCoroutine == null)
            {
                _regenCoroutine = StartCoroutine(RegenRoutine());
            }

            if (ghostSpawnManager != null && safeZoneCollider != null)
            {
                ghostSpawnManager.SetSpawnExclusionZone(safeZoneCollider);
            }

            RepelNearbyGhosts();

            OnPlayerEnterChurch?.Invoke();
            _onPlayerEntered?.Invoke();
        }

        private void ProcessPlayerExit(Collider playerCollider)
        {
            if (!_isPlayerInside)
            {
                return;
            }

            _isPlayerInside = false;

            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
                _regenCoroutine = null;
            }

            if (ghostSpawnManager != null)
            {
                ghostSpawnManager.ClearSpawnExclusionZone();
            }

            _activePlayerController = null;

            OnPlayerExitChurch?.Invoke();
            _onPlayerExited?.Invoke();
        }

        private bool IsPositionInsideSafeZone(Vector3 worldPosition)
        {
            if (safeZoneCollider == null)
            {
                return false;
            }

            Vector3 closestPoint = safeZoneCollider.ClosestPoint(worldPosition);
            return Vector3.Distance(closestPoint, worldPosition) <= ProximityInsideThreshold;
        }

        private IEnumerator RegenRoutine()
        {
            while (enabled)
            {
                if (_activePlayerController != null)
                {
                    _activePlayerController.AddStamina(StaminaRegenPerSecond * Time.deltaTime);
                }

                yield return null;
            }
        }
    }
}

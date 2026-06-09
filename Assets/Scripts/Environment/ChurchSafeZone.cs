using System.Collections;
using Ashlight.Ghost;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Environment
{
    /// <summary>
    /// Church safe zone that regenerates the player, repels ghosts, saves, and refuels the torch.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public class ChurchSafeZone : MonoBehaviour
    {
        private const float HealthRegenPerSecond = 5f;
        private const float StaminaRegenPerSecond = 10f;
        private const float TorchRefuelDelay = 2f;
        private const float GhostRepelRadius = 20f;

        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private GhostSpawnManager ghostSpawnManager;
        [SerializeField] private BoxCollider safeZoneCollider;

        [Header("Events")]
        [SerializeField] private UnityEvent _onPlayerEntered;
        [SerializeField] private UnityEvent _onPlayerExited;

        private Coroutine _regenCoroutine;
        private Coroutine _torchRefuelCoroutine;
        private PlayerController _activePlayerController;
        private int _playersInside;

        /// <summary>Invoked when the player enters the church safe zone.</summary>
        public UnityEvent OnPlayerEntered => _onPlayerEntered;

        /// <summary>Invoked when the player leaves the church safe zone.</summary>
        public UnityEvent OnPlayerExited => _onPlayerExited;

        private void Awake()
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

            safeZoneCollider.isTrigger = true;

            if (playerHealth == null || holyTorch == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    if (playerHealth == null)
                    {
                        playerHealth = playerObject.GetComponent<PlayerHealth>();
                    }

                    if (holyTorch == null)
                    {
                        holyTorch = playerObject.GetComponentInChildren<HolyTorch>();
                    }
                }
            }

            if (saveSystem == null)
            {
                saveSystem = FindAnyObjectByType<SaveSystem>();
            }

            if (ghostSpawnManager == null)
            {
                ghostSpawnManager = FindAnyObjectByType<GhostSpawnManager>();
            }

            if (playerHealth == null)
            {
                Debug.LogWarning($"{nameof(ChurchSafeZone)} has no {nameof(PlayerHealth)} assigned.", this);
            }

            if (holyTorch == null)
            {
                Debug.LogWarning($"{nameof(ChurchSafeZone)} has no {nameof(HolyTorch)} assigned.", this);
            }

            if (saveSystem == null)
            {
                Debug.LogWarning($"{nameof(ChurchSafeZone)} has no {nameof(SaveSystem)} assigned.", this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _playersInside++;

            if (_playersInside > 1)
            {
                return;
            }

            _activePlayerController = other.GetComponent<PlayerController>();

            if (_regenCoroutine == null)
            {
                _regenCoroutine = StartCoroutine(RegenRoutine());
            }

            if (ghostSpawnManager != null && safeZoneCollider != null)
            {
                ghostSpawnManager.SetSpawnExclusionZone(safeZoneCollider);
            }

            saveSystem?.AutoSave();
            RepelNearbyGhosts();

            if (_torchRefuelCoroutine != null)
            {
                StopCoroutine(_torchRefuelCoroutine);
            }

            _torchRefuelCoroutine = StartCoroutine(DelayedTorchRefuelRoutine());
            _onPlayerEntered?.Invoke();
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            _playersInside = Mathf.Max(0, _playersInside - 1);

            if (_playersInside > 0)
            {
                return;
            }

            if (_regenCoroutine != null)
            {
                StopCoroutine(_regenCoroutine);
                _regenCoroutine = null;
            }

            if (_torchRefuelCoroutine != null)
            {
                StopCoroutine(_torchRefuelCoroutine);
                _torchRefuelCoroutine = null;
            }

            if (ghostSpawnManager != null)
            {
                ghostSpawnManager.ClearSpawnExclusionZone();
            }

            _activePlayerController = null;
            _onPlayerExited?.Invoke();
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

        private IEnumerator RegenRoutine()
        {
            while (enabled)
            {
                if (playerHealth != null)
                {
                    playerHealth.Heal(HealthRegenPerSecond * Time.deltaTime);
                }

                if (_activePlayerController != null)
                {
                    _activePlayerController.AddStamina(StaminaRegenPerSecond * Time.deltaTime);
                }

                yield return null;
            }
        }

        private IEnumerator DelayedTorchRefuelRoutine()
        {
            yield return new WaitForSeconds(TorchRefuelDelay);

            if (holyTorch != null)
            {
                holyTorch.Refuel(holyTorch.MaxFuel);
            }

            _torchRefuelCoroutine = null;
        }
    }
}

using System.Collections;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Boss ghost phases for the Elder Spirit encounter.
    /// </summary>
    public enum ElderPhase
    {
        Phase1,
        Phase2,
        Phase3,
        Defeated
    }

    /// <summary>
    /// Multi-phase boss ghost that spawns wanderers, teleports, and requires a ritual to fully defeat.
    /// </summary>
    [DisallowMultipleComponent]
    public class ElderSpiritGhost : GhostAIController
    {
        private const float Phase1Speed = 5f;
        private const float WandererSpawnInterval = 30f;
        private const float PhaseCheckInterval = 1f;
        private const float Phase2TeleportInterval = 10f;
        private const float Phase3ExplosionInterval = 5f;
        private const float TeleportHideDuration = 0.5f;
        private const float TeleportResumeDelay = 2f;
        private const float TeleportSearchRadius = 15f;
        private const float TeleportSampleDistance = 4f;
        private const float DarkZoneThreshold = 0.2f;
        private const int DarkSearchSampleCount = 12;
        private const float HollowExplosionDamage = 25f;
        private const float HollowSelfDamage = 30f;
        private const float HollowExplosionRange = 2.5f;
        private const float RitualRange = 3f;
        private const float RitualCompleteDelay = 2f;

        [SerializeField] private GhostSpawnManager spawnManager;
        [SerializeField] private FearSystem fearSystem;
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private float ritualCost = 0.4f;
        [SerializeField] private Canvas ritualPromptCanvas;
        [SerializeField] private ParticleSystem phaseTransitionEffect;
        [SerializeField] private ParticleSystem defeatEffect;
        [SerializeField] private float phase2SpeedBoost = 7f;
        [SerializeField] private float phase3SpeedBoost = 9f;
        [SerializeField] private AudioClip teleportSound;
        [SerializeField] private AudioClip explosionSound;
        [SerializeField] private ParticleSystem teleportEffect;
        [SerializeField] private ParticleSystem explosionEffect;

        [Header("Events")]
        [SerializeField] private UnityEvent _onElderDefeated;

        private ElderPhase _currentElderPhase = ElderPhase.Phase1;
        private float _phaseCheckTimer;
        private float _wandererSpawnTimer;
        private float _phase2TeleportTimer;
        private float _phase3ExplosionTimer;
        private bool _isTeleporting;
        private bool _isPerformingRitual;
        private Coroutine _teleportRoutine;
        private Coroutine _ritualRoutine;
        private AudioSource _audioSource;
        private Text _ritualPromptText;
        private Camera _mainCamera;

        /// <summary>Gets the current Elder Spirit phase.</summary>
        public ElderPhase CurrentPhase => _currentElderPhase;

        /// <summary>Invoked when the Elder Spirit ritual is completed.</summary>
        public UnityEvent OnElderDefeated => _onElderDefeated;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            if (spawnManager == null)
            {
                spawnManager = FindAnyObjectByType<GhostSpawnManager>();
            }

            if (fearSystem == null)
            {
                fearSystem = FindAnyObjectByType<FearSystem>();
            }

            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(ElderSpiritGhost)} requires a {nameof(HolyWaterInventory)}.", this);
            }

            if (ritualPromptCanvas == null)
            {
                ritualPromptCanvas = GetComponentInChildren<Canvas>(true);
            }

            if (ritualPromptCanvas == null)
            {
                Debug.LogError($"{nameof(ElderSpiritGhost)} requires a ritual prompt {nameof(Canvas)}.", this);
            }
            else
            {
                _ritualPromptText = ritualPromptCanvas.GetComponentInChildren<Text>(true);
                SetRitualPromptVisible(false);
            }

            if (spawnManager == null)
            {
                Debug.LogError($"{nameof(ElderSpiritGhost)} requires a {nameof(GhostSpawnManager)}.", this);
            }

            if (fearSystem == null)
            {
                Debug.LogError($"{nameof(ElderSpiritGhost)} requires a {nameof(FearSystem)}.", this);
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
                _audioSource.playOnAwake = false;
                _audioSource.spatialBlend = 1f;
            }
        }

        /// <inheritdoc />
        public override void Activate(Transform playerTransform)
        {
            _currentElderPhase = ElderPhase.Phase1;
            _phaseCheckTimer = PhaseCheckInterval;
            _wandererSpawnTimer = WandererSpawnInterval;
            _phase2TeleportTimer = Phase2TeleportInterval;
            _phase3ExplosionTimer = Phase3ExplosionInterval;
            _isPerformingRitual = false;
            fearSystem?.ClearMinimumFearFloor();
            SetRitualPromptVisible(false);
            base.Activate(playerTransform);
            ApplyPhaseSettings();
        }

        /// <inheritdoc />
        public override void Deactivate()
        {
            if (_teleportRoutine != null)
            {
                StopCoroutine(_teleportRoutine);
                _teleportRoutine = null;
            }

            if (_ritualRoutine != null)
            {
                StopCoroutine(_ritualRoutine);
                _ritualRoutine = null;
            }

            _isPerformingRitual = false;
            fearSystem?.ClearMinimumFearFloor();
            SetRitualPromptVisible(false);
            base.Deactivate();
        }

        /// <inheritdoc />
        protected override void Update()
        {
            if (_currentElderPhase == ElderPhase.Defeated)
            {
                UpdateDefeatedRitual();
                return;
            }

            base.Update();

            _phaseCheckTimer -= Time.deltaTime;
            if (_phaseCheckTimer <= 0f)
            {
                _phaseCheckTimer = PhaseCheckInterval;
                CheckPhaseTransition();
            }

            UpdatePhaseBehaviours();
        }

        /// <inheritdoc />
        public override void TakeTorchDamage(float damage)
        {
            float resistanceMultiplier = GetTorchDamageResistanceMultiplier();
            base.TakeTorchDamage(damage * resistanceMultiplier);
        }

        /// <inheritdoc />
        protected override void OnHealthDepleted()
        {
            if (_currentElderPhase == ElderPhase.Defeated)
            {
                return;
            }

            EnterDefeatedState();
        }

        /// <inheritdoc />
        protected override void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
        {
            if (_currentElderPhase == ElderPhase.Defeated)
            {
                return;
            }

            if (_currentElderPhase == ElderPhase.Phase2 || _currentElderPhase == ElderPhase.Phase3)
            {
                if (CurrentState == GhostState.Retreat ||
                    CurrentState == GhostState.Recharge ||
                    CurrentState == GhostState.Attack ||
                    CurrentState == GhostState.Perish)
                {
                    return;
                }

                if (perceptionLevel >= PerceptionLevel.Suspicious)
                {
                    ChangeState(GhostState.Chase);
                }

                return;
            }

            base.ApplyPerceptionTransitions(perceptionLevel);
        }

        /// <inheritdoc />
        protected override void UpdateStalk()
        {
            ApplyPerceptionTransitions(CalculateCurrentPerception());
            UpdateEnhancedStalkMovement();
        }

        /// <summary>Checks health thresholds and advances Elder Spirit phases.</summary>
        public void CheckPhaseTransition()
        {
            if (_currentElderPhase == ElderPhase.Defeated)
            {
                return;
            }

            ElderPhase nextPhase = _currentElderPhase;

            if (GhostHealthPercent <= 0f)
            {
                EnterDefeatedState();
                return;
            }

            if (GhostHealthPercent <= 0.3f)
            {
                nextPhase = ElderPhase.Phase3;
            }
            else if (GhostHealthPercent <= 0.6f)
            {
                nextPhase = ElderPhase.Phase2;
            }
            else
            {
                nextPhase = ElderPhase.Phase1;
            }

            if (nextPhase == _currentElderPhase)
            {
                return;
            }

            _currentElderPhase = nextPhase;

            if (phaseTransitionEffect != null)
            {
                phaseTransitionEffect.Play();
            }

            ApplyPhaseSettings();
        }

        /// <summary>
        /// Completes the banishment ritual after Holy Water is spent.
        /// </summary>
        public void PerformRitual()
        {
            if (_isPerformingRitual || _currentElderPhase != ElderPhase.Defeated)
            {
                return;
            }

            if (_ritualRoutine != null)
            {
                StopCoroutine(_ritualRoutine);
            }

            _ritualRoutine = StartCoroutine(PerformRitualRoutine());
        }

        private void UpdateDefeatedRitual()
        {
            SetRitualPromptVisible(true);
            UpdateRitualPromptFacing();

            if (_isPerformingRitual || PlayerTransform == null)
            {
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer > RitualRange)
            {
                return;
            }

            if (Keyboard.current == null || !Keyboard.current.fKey.wasPressedThisFrame)
            {
                return;
            }

            if (holyWaterInventory != null && holyWaterInventory.Spend(ritualCost))
            {
                PerformRitual();
            }
            else
            {
                Debug.Log("Not enough Holy Water for ritual");
            }
        }

        private IEnumerator PerformRitualRoutine()
        {
            _isPerformingRitual = true;
            SetRitualPromptVisible(false);

            if (phaseTransitionEffect != null)
            {
                phaseTransitionEffect.Play();
            }
            else if (explosionEffect != null)
            {
                explosionEffect.Play();
            }

            _onElderDefeated?.Invoke();

            yield return new WaitForSeconds(RitualCompleteDelay);

            CurrentGhostHealth = MaxGhostHealth;
            _currentElderPhase = ElderPhase.Phase1;
            _isPerformingRitual = false;
            _ritualRoutine = null;

            Debug.Log("Ritual complete — Elder Spirit banished");
            Deactivate();
        }

        private void UpdatePhaseBehaviours()
        {
            switch (_currentElderPhase)
            {
                case ElderPhase.Phase1:
                    UpdatePhaseOneSpawning();
                    break;
                case ElderPhase.Phase2:
                    UpdatePhaseTwoTeleport();
                    break;
                case ElderPhase.Phase3:
                    UpdatePhaseThreeExplosions();
                    break;
            }
        }

        private void UpdatePhaseOneSpawning()
        {
            if (spawnManager == null)
            {
                return;
            }

            _wandererSpawnTimer -= Time.deltaTime;
            if (_wandererSpawnTimer > 0f)
            {
                return;
            }

            _wandererSpawnTimer = WandererSpawnInterval;
            spawnManager.SpawnGhost();
            spawnManager.SpawnGhost();
        }

        private void UpdatePhaseTwoTeleport()
        {
            if (_isTeleporting)
            {
                return;
            }

            _phase2TeleportTimer -= Time.deltaTime;
            if (_phase2TeleportTimer > 0f)
            {
                return;
            }

            _phase2TeleportTimer = Phase2TeleportInterval;

            if (TryFindDarkTeleportPosition(out Vector3 teleportPosition))
            {
                _teleportRoutine = StartCoroutine(TeleportRoutine(teleportPosition));
            }
        }

        private void UpdatePhaseThreeExplosions()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            _phase3ExplosionTimer -= Time.deltaTime;
            if (_phase3ExplosionTimer > 0f)
            {
                return;
            }

            _phase3ExplosionTimer = Phase3ExplosionInterval;

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer <= HollowExplosionRange)
            {
                TriggerHollowStyleExplosion();
            }
        }

        private void ApplyPhaseSettings()
        {
            switch (_currentElderPhase)
            {
                case ElderPhase.Phase1:
                    SetAgentSpeed(Phase1Speed);
                    fearSystem?.ClearMinimumFearFloor();
                    break;
                case ElderPhase.Phase2:
                    SetAgentSpeed(phase2SpeedBoost);
                    fearSystem?.SetMinimumFearFloor(0.8f);
                    break;
                case ElderPhase.Phase3:
                    SetAgentSpeed(phase3SpeedBoost);
                    fearSystem?.SetMinimumFearFloor(1f);
                    break;
                case ElderPhase.Defeated:
                    fearSystem?.ClearMinimumFearFloor();
                    break;
            }
        }

        private float GetTorchDamageResistanceMultiplier()
        {
            switch (_currentElderPhase)
            {
                case ElderPhase.Phase1:
                    return 0.3f;
                case ElderPhase.Phase2:
                    return 0.5f;
                case ElderPhase.Phase3:
                case ElderPhase.Defeated:
                    return 1f;
                default:
                    return 1f;
            }
        }

        private void UpdateEnhancedStalkMovement()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            Vector3 playerForward = PlayerTransform.forward;
            playerForward.y = 0f;

            if (playerForward.sqrMagnitude <= 0.001f)
            {
                playerForward = (transform.position - PlayerTransform.position).normalized;
            }

            Vector3 stalkPoint = PlayerTransform.position - playerForward.normalized * 4f;
            SetAgentDestination(stalkPoint);
        }

        private void EnterDefeatedState()
        {
            _currentElderPhase = ElderPhase.Defeated;
            CurrentGhostHealth = 0f;
            SetAgentStopped(true);
            ResetAgentPath();
            fearSystem?.ClearMinimumFearFloor();

            if (defeatEffect != null)
            {
                defeatEffect.Play();
            }

            UpdateRitualPromptText();
            SetRitualPromptVisible(true);

            Debug.Log("Elder Spirit defeated — ritual required");
        }

        private void SetRitualPromptVisible(bool visible)
        {
            if (ritualPromptCanvas != null)
            {
                ritualPromptCanvas.gameObject.SetActive(visible);
            }
        }

        private void UpdateRitualPromptText()
        {
            if (_ritualPromptText != null)
            {
                _ritualPromptText.text = $"Press F to perform ritual (costs {ritualCost:0} Holy Water)";
            }
        }

        private void UpdateRitualPromptFacing()
        {
            if (ritualPromptCanvas == null)
            {
                return;
            }

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            if (_mainCamera == null)
            {
                return;
            }

            Transform promptTransform = ritualPromptCanvas.transform;
            Vector3 lookDirection = promptTransform.position - _mainCamera.transform.position;
            lookDirection.y = 0f;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                promptTransform.rotation = Quaternion.LookRotation(lookDirection.normalized);
            }
        }

        private void TriggerHollowStyleExplosion()
        {
            if (explosionEffect != null)
            {
                explosionEffect.Play();
            }

            if (_audioSource != null && explosionSound != null)
            {
                _audioSource.PlayOneShot(explosionSound);
            }

            if (PlayerFaithComponent != null)
            {
                PlayerFaithComponent.TakeDamage(HollowExplosionDamage);
            }

            CurrentGhostHealth = Mathf.Max(0f, CurrentGhostHealth - HollowSelfDamage);

            if (CurrentGhostHealth <= 0f)
            {
                EnterDefeatedState();
            }
        }

        private bool TryFindDarkTeleportPosition(out Vector3 teleportPosition)
        {
            teleportPosition = transform.position;
            float bestLightLevel = float.MaxValue;
            bool foundPosition = false;

            for (int i = 0; i < DarkSearchSampleCount; i++)
            {
                Vector2 randomCircle = Random.insideUnitCircle * TeleportSearchRadius;
                Vector3 sampleTarget = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

                if (!TrySampleNavMeshPosition(sampleTarget, out NavMeshHit hit, TeleportSampleDistance))
                {
                    continue;
                }

                float lightLevel = PerceptionSystem.GetLightLevelAtPosition(hit.position);
                if (lightLevel >= DarkZoneThreshold || lightLevel >= bestLightLevel)
                {
                    continue;
                }

                bestLightLevel = lightLevel;
                teleportPosition = hit.position;
                foundPosition = true;
            }

            return foundPosition;
        }

        private IEnumerator TeleportRoutine(Vector3 teleportPosition)
        {
            _isTeleporting = true;
            SetAgentStopped(true);
            ResetAgentPath();

            if (GhostRenderer != null)
            {
                GhostRenderer.enabled = false;
            }

            yield return new WaitForSeconds(TeleportHideDuration);

            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.Warp(teleportPosition);
            }
            else
            {
                transform.position = teleportPosition;
            }

            if (GhostRenderer != null)
            {
                GhostRenderer.enabled = true;
            }

            if (teleportEffect != null)
            {
                teleportEffect.Play();
            }

            if (_audioSource != null && teleportSound != null)
            {
                _audioSource.PlayOneShot(teleportSound);
            }

            yield return new WaitForSeconds(TeleportResumeDelay);

            SetAgentStopped(false);
            _isTeleporting = false;
            _teleportRoutine = null;

            if ((CurrentState == GhostState.Chase || CurrentState == GhostState.Stalk) && PlayerTransform != null)
            {
                SetAgentDestination(PlayerTransform.position);
            }
        }
    }
}

using System.Collections;
using UnityEngine;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Stationary ambush ghost that detonates when the player enters close range.
    /// </summary>
    [DisallowMultipleComponent]
    public class HollowGhost : GhostAIController
    {
        private const float AmbushDetectionRange = 3f;
        private const float SelfExplosionDamage = 80f;
        private const float SpawnReturnThreshold = 0.5f;

        [SerializeField] private float explosionRange = 2.5f;
        [SerializeField] private float explosionDamage = 40f;
        [SerializeField] private float warningDuration = 1.5f;
        [SerializeField] private ParticleSystem warningEffect;
        [SerializeField] private ParticleSystem explosionEffect;
        [SerializeField] private AudioClip explosionSound;

        private Vector3 _spawnPosition;
        private bool _isWarning;
        private bool _hasExploded;
        private Coroutine _warningRoutine;
        private AudioSource _audioSource;

        /// <inheritdoc />
        protected override float RetreatHealthThreshold => 0.2f;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();
            _spawnPosition = transform.position;

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
            _spawnPosition = transform.position;
            _isWarning = false;
            _hasExploded = false;
            base.Activate(playerTransform);
            ChangeState(GhostState.Idle);
        }

        /// <inheritdoc />
        public override void Deactivate()
        {
            if (_warningRoutine != null)
            {
                StopCoroutine(_warningRoutine);
                _warningRoutine = null;
            }

            _isWarning = false;
            base.Deactivate();
        }

        /// <inheritdoc />
        protected override bool ShouldRetreat()
        {
            if (GhostHealthPercent >= RetreatHealthThreshold)
            {
                return false;
            }

            return base.ShouldRetreat();
        }

        /// <inheritdoc />
        protected override void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
        {
        }

        /// <inheritdoc />
        protected override void UpdateIdle()
        {
            MaintainSpawnPosition();

            if (_isWarning || PlayerTransform == null)
            {
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceToPlayer > AmbushDetectionRange)
            {
                return;
            }

            if (distanceToPlayer <= explosionRange)
            {
                BeginExplosionWarning();
            }
        }

        /// <inheritdoc />
        protected override void UpdateWander()
        {
            MaintainSpawnPosition();
        }

        /// <inheritdoc />
        protected override void EnterWander()
        {
            SetAgentStopped(true);
            ResetAgentPath();
        }

        /// <summary>Triggers the hollow warning pulse and delayed explosion.</summary>
        public void BeginExplosionWarning()
        {
            if (_isWarning || _hasExploded)
            {
                return;
            }

            _isWarning = true;

            if (warningEffect != null)
            {
                warningEffect.Play();
            }

            if (_warningRoutine != null)
            {
                StopCoroutine(_warningRoutine);
            }

            _warningRoutine = StartCoroutine(WarningThenExplodeRoutine());
        }

        /// <summary>Detonates the hollow ghost and damages the player.</summary>
        public void Explode()
        {
            if (_hasExploded)
            {
                return;
            }

            _hasExploded = true;
            _isWarning = false;

            if (explosionEffect != null)
            {
                explosionEffect.Play();
            }

            if (_audioSource != null && explosionSound != null)
            {
                _audioSource.PlayOneShot(explosionSound);
            }

            if (PlayerHealthComponent != null)
            {
                PlayerHealthComponent.TakeDamage(explosionDamage);
            }

            CurrentGhostHealth = Mathf.Max(0f, CurrentGhostHealth - SelfExplosionDamage);

            if (CurrentGhostHealth <= 0f)
            {
                OnHealthDepleted();
                return;
            }

            ReturnToSpawnAndRecharge();
        }

        private IEnumerator WarningThenExplodeRoutine()
        {
            float elapsed = 0f;

            while (elapsed < warningDuration)
            {
                elapsed += Time.deltaTime;
                ApplyWarningPulse();
                yield return null;
            }

            _warningRoutine = null;
            Explode();
        }

        private void ApplyWarningPulse()
        {
            if (GhostRenderer == null)
            {
                return;
            }

            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * 12f);
            GhostRenderer.material.color = Color.Lerp(BaseRendererColor, Color.red, pulse);
        }

        private void MaintainSpawnPosition()
        {
            float distanceFromSpawn = Vector3.Distance(transform.position, _spawnPosition);
            if (distanceFromSpawn <= SpawnReturnThreshold)
            {
                return;
            }

            SetAgentStopped(false);
            SetAgentDestination(_spawnPosition);

            if (Agent != null && Agent.isOnNavMesh &&
                !Agent.pathPending &&
                Agent.remainingDistance <= Agent.stoppingDistance)
            {
                SetAgentStopped(true);
            }
        }

        private void ReturnToSpawnAndRecharge()
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.Warp(_spawnPosition);
            }
            else
            {
                transform.position = _spawnPosition;
            }

            if (GhostRenderer != null)
            {
                GhostRenderer.material.color = BaseRendererColor;
            }

            _hasExploded = false;
            ChangeState(GhostState.Recharge);
        }
    }
}

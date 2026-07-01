using System.Collections;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Stealthy ghost that dashes to close distance, grabs the player, and is broken primarily by Holy Water spray.
    /// </summary>
    [DisallowMultipleComponent]
    public class GrabberGhost : GhostAIController
    {
        private const float DashTriggerDistance = 4f;
        private const float DashDuration = 0.5f;
        private const float RetreatSampleDistance = 5f;
        private const float GrabberRetreatDistance = 10f;
        private const float StealthAlphaUpdateInterval = 0.1f;
        private const float MinStealthAlpha = 0.15f;
        private const float MaxStealthAlpha = 0.85f;
        private const float HolyWaterBreakChancePerSecond = 0.35f;

        [SerializeField] private float stealthRange = 15f;
        [SerializeField] private float dashSpeed = 12f;
        [SerializeField] private float grabRange = 1.2f;
        [SerializeField] private float grabDuration = 6f;
        [SerializeField] private float faithDamagePerSecondWhileGrabbed = 8f;
        [SerializeField] private float dashCooldown = 8f;

        [Header("Effects")]
        [SerializeField] private ParticleSystem grabEffect;
        [SerializeField] private AudioClip grabSound;
        [SerializeField] private AudioClip struggleLoopSound;
        [SerializeField] private AudioClip releaseSound;
        [SerializeField] private AudioSource ghostAudioSource;

        [Header("Events")]
        [SerializeField] private UnityEvent _onGrabStarted;
        [SerializeField] private UnityEvent _onGrabReleased;

        private PlayerController _playerController;
        private HolyWaterSpray _playerHolyWaterSpray;
        private float _lastDashTime = -999f;
        private bool _isGrabActive;
        private bool _isDashing;
        private Coroutine _stealthAlphaRoutine;
        private Coroutine _grabRoutine;
        private Coroutine _dashRoutine;

        /// <summary>Invoked when the Grabber successfully grabs the player.</summary>
        public UnityEvent OnGrabStarted => _onGrabStarted;

        /// <summary>Invoked when the Grabber releases the player.</summary>
        public UnityEvent OnGrabReleased => _onGrabReleased;

        /// <inheritdoc />
        protected override float RetreatDistance => GrabberRetreatDistance;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            if (ghostAudioSource == null)
            {
                ghostAudioSource = GetComponent<AudioSource>();
            }
        }

        /// <inheritdoc />
        public override void SetPlayer(Transform playerTransform)
        {
            base.SetPlayer(playerTransform);

            if (playerTransform == null)
            {
                _playerController = null;
                _playerHolyWaterSpray = null;
                return;
            }

            _playerController = playerTransform.GetComponent<PlayerController>();
            _playerHolyWaterSpray = playerTransform.GetComponent<HolyWaterSpray>();
        }

        /// <inheritdoc />
        public override void Activate(Transform playerTransform)
        {
            _isGrabActive = false;
            _isDashing = false;
            _lastDashTime = -999f;
            base.Activate(playerTransform);
            SetPlayer(playerTransform);

            if (_stealthAlphaRoutine == null)
            {
                _stealthAlphaRoutine = StartCoroutine(UpdateStealthAlpha());
            }
        }

        /// <inheritdoc />
        public override void Deactivate()
        {
            if (_isGrabActive)
            {
                ReleasePlayer("deactivated");
            }

            StopStealthRoutine();
            StopDashRoutine();
            base.Deactivate();
        }

        /// <inheritdoc />
        public override void ForceInstantDeath()
        {
            if (_isGrabActive)
            {
                ReleasePlayer("sanctuary");
            }

            StopStealthRoutine();
            StopDashRoutine();
            base.ForceInstantDeath();
        }

        /// <inheritdoc />
        protected override void UpdateChase()
        {
            if (PlayerTransform == null)
            {
                return;
            }

            float distance = Vector3.Distance(transform.position, PlayerTransform.position);

            if (distance <= grabRange)
            {
                TryGrabPlayer();
                return;
            }

            if (!_isDashing && Time.time - _lastDashTime >= dashCooldown && distance > DashTriggerDistance)
            {
                _dashRoutine = StartCoroutine(DashTowardPlayer());
                _lastDashTime = Time.time;
            }
            else if (!_isDashing)
            {
                base.UpdateChase();
            }
        }

        /// <inheritdoc />
        protected override void EnterGrabbing()
        {
            base.EnterGrabbing();

            if (grabEffect != null)
            {
                grabEffect.Play();
            }

            if (grabSound != null && ghostAudioSource != null)
            {
                ghostAudioSource.PlayOneShot(grabSound);
            }

            if (struggleLoopSound != null && ghostAudioSource != null)
            {
                ghostAudioSource.clip = struggleLoopSound;
                ghostAudioSource.loop = true;
                ghostAudioSource.Play();
            }

            _playerController?.SetMovementMultiplier(0.25f);
            _playerHolyWaterSpray?.SetGrabberLock(transform);
            _onGrabStarted?.Invoke();

            if (_grabRoutine != null)
            {
                StopCoroutine(_grabRoutine);
            }

            _grabRoutine = StartCoroutine(GrabRoutine());
        }

        /// <inheritdoc />
        protected override void ExitGrabbing()
        {
            if (_grabRoutine != null)
            {
                StopCoroutine(_grabRoutine);
                _grabRoutine = null;
            }

            if (ghostAudioSource != null)
            {
                ghostAudioSource.loop = false;
                ghostAudioSource.Stop();
            }

            base.ExitGrabbing();
        }

        private void TryGrabPlayer()
        {
            if (CurrentState == GhostState.Grabbing || _isGrabActive)
            {
                return;
            }

            _isGrabActive = true;
            ChangeState(GhostState.Grabbing);
        }

        private IEnumerator GrabRoutine()
        {
            float elapsed = 0f;

            while (elapsed < grabDuration && CurrentState == GhostState.Grabbing && _isGrabActive)
            {
                PlayerFaithComponent?.ReduceFaith(faithDamagePerSecondWhileGrabbed * Time.deltaTime);

                if (_playerHolyWaterSpray != null && _playerHolyWaterSpray.IsBottleActive)
                {
                    float breakChance = HolyWaterBreakChancePerSecond * Time.deltaTime;
                    if (Random.value < breakChance)
                    {
                        ReleasePlayer("holy_water");
                        yield break;
                    }
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (_isGrabActive)
            {
                ReleasePlayer("timeout");
            }
        }

        private void ReleasePlayer(string reason)
        {
            if (!_isGrabActive)
            {
                return;
            }

            _isGrabActive = false;
            _playerController?.SetMovementMultiplier(1f);
            _playerHolyWaterSpray?.ClearGrabberLock();

            if (releaseSound != null && ghostAudioSource != null)
            {
                ghostAudioSource.loop = false;
                ghostAudioSource.Stop();
                ghostAudioSource.PlayOneShot(releaseSound);
            }

            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = false;
            }

            if (PlayerTransform != null)
            {
                Vector3 retreatDirection = (transform.position - PlayerTransform.position).normalized;
                Vector3 retreatTarget = transform.position + retreatDirection * RetreatDistance;
                NavMeshHit hit = default;
                bool hasRetreatPoint = TrySampleNavMeshPosition(
                    retreatTarget,
                    out hit,
                    RetreatSampleDistance);

                ChangeState(GhostState.Retreat);

                if (hasRetreatPoint && Agent != null && Agent.isOnNavMesh)
                {
                    Agent.SetDestination(hit.position);
                }
            }
            else
            {
                ChangeState(GhostState.Retreat);
            }

            _onGrabReleased?.Invoke();
            Debug.Log($"Grabber released player. Reason: {reason}", this);
        }

        private IEnumerator DashTowardPlayer()
        {
            _isDashing = true;

            if (Agent != null && Agent.isOnNavMesh)
            {
                float originalSpeed = Agent.speed;
                Agent.speed = dashSpeed;

                if (PlayerTransform != null)
                {
                    SetAgentDestination(PlayerTransform.position);
                }

                yield return new WaitForSeconds(DashDuration);

                if (Agent != null && Agent.isOnNavMesh)
                {
                    Agent.speed = originalSpeed;
                }
            }

            _isDashing = false;
            _dashRoutine = null;
        }

        private IEnumerator UpdateStealthAlpha()
        {
            WaitForSeconds wait = new WaitForSeconds(StealthAlphaUpdateInterval);

            while (enabled)
            {
                if (PlayerTransform != null)
                {
                    float distanceToPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
                    float normalizedDistance = stealthRange > 0f
                        ? Mathf.Clamp01((distanceToPlayer - grabRange) / stealthRange)
                        : 1f;
                    float alpha = Mathf.Lerp(MinStealthAlpha, MaxStealthAlpha, normalizedDistance);
                    SetRendererAlpha(alpha);
                }

                yield return wait;
            }
        }

        private void SetRendererAlpha(float alpha)
        {
            if (GhostRenderer == null || GhostRenderer.material == null)
            {
                return;
            }

            if (!GhostRenderer.material.HasProperty("_Color"))
            {
                return;
            }

            Color color = GhostRenderer.material.color;
            color.a = alpha;
            GhostRenderer.material.color = color;
        }

        private void StopStealthRoutine()
        {
            if (_stealthAlphaRoutine != null)
            {
                StopCoroutine(_stealthAlphaRoutine);
                _stealthAlphaRoutine = null;
            }
        }

        private void StopDashRoutine()
        {
            if (_dashRoutine != null)
            {
                StopCoroutine(_dashRoutine);
                _dashRoutine = null;
            }

            _isDashing = false;
        }
    }
}

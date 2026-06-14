using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Torch-resistant ghost that teleports through dark zones while chasing the player.
    /// </summary>
    [DisallowMultipleComponent]
    public class WraithGhost : GhostAIController
    {
        private const float TeleportSearchRadius = 15f;
        private const float TeleportSampleDistance = 4f;
        private const float TeleportHideDuration = 0.5f;
        private const float TeleportResumeDelay = 2f;
        private const int DarkSearchSampleCount = 12;
        private const float HighTorchFuelThreshold = 0.5f;

        [SerializeField] private float teleportCooldown = 5f;
        [SerializeField] private float darkZoneThreshold = 0.2f;
        [SerializeField] private ParticleSystem teleportEffect;
        [SerializeField] private AudioClip teleportSound;

        private float _teleportCooldownTimer;
        private bool _isTeleporting;
        private Coroutine _teleportRoutine;
        private AudioSource _audioSource;

        /// <inheritdoc />
        protected override float RetreatHealthThreshold => 0.15f;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

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
            _teleportCooldownTimer = 0f;
            _isTeleporting = false;
            base.Activate(playerTransform);
        }

        /// <inheritdoc />
        public override void Deactivate()
        {
            if (_teleportRoutine != null)
            {
                StopCoroutine(_teleportRoutine);
                _teleportRoutine = null;
            }

            _isTeleporting = false;
            base.Deactivate();
        }

        /// <inheritdoc />
        public override void TakeTorchDamage(float damage)
        {
            base.TakeTorchDamage(damage * 0.5f);
        }

        /// <inheritdoc />
        protected override void UpdateWander()
        {
            ApplyPerceptionTransitions(CalculateCurrentPerception());

            if (!HasActiveWanderDestination())
            {
                SetDarkWanderDestination();
            }

            if (Agent != null && Agent.isOnNavMesh &&
                !Agent.pathPending &&
                Agent.remainingDistance <= Agent.stoppingDistance)
            {
                SetDarkWanderDestination();
            }
        }

        /// <inheritdoc />
        protected override void UpdateChase()
        {
            if (_isTeleporting)
            {
                return;
            }

            _teleportCooldownTimer = Mathf.Max(0f, _teleportCooldownTimer - Time.deltaTime);

            if (_teleportCooldownTimer <= 0f &&
                GetPlayerTorchFuelPercent() > HighTorchFuelThreshold &&
                TryFindDarkTeleportPosition(out Vector3 teleportPosition))
            {
                _teleportRoutine = StartCoroutine(TeleportRoutine(teleportPosition));
                return;
            }

            base.UpdateChase();
        }

        /// <summary>Attempts to find a dark NavMesh position within teleport range.</summary>
        /// <param name="teleportPosition">Resolved teleport destination.</param>
        /// <returns>True when a valid dark position was found.</returns>
        protected bool TryFindDarkTeleportPosition(out Vector3 teleportPosition)
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
                if (lightLevel >= darkZoneThreshold || lightLevel >= bestLightLevel)
                {
                    continue;
                }

                bestLightLevel = lightLevel;
                teleportPosition = hit.position;
                foundPosition = true;
            }

            return foundPosition;
        }

        /// <summary>Sets a wander destination toward the darkest nearby NavMesh point.</summary>
        protected void SetDarkWanderDestination()
        {
            if (TryFindDarkTeleportPosition(out Vector3 darkPosition))
            {
                SetAgentDestination(darkPosition);
                return;
            }

            SetRandomWanderDestination();
        }

        private IEnumerator TeleportRoutine(Vector3 teleportPosition)
        {
            _isTeleporting = true;
            _teleportCooldownTimer = teleportCooldown;
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

            if (CurrentState == GhostState.Chase && PlayerTransform != null)
            {
                SetAgentDestination(PlayerTransform.position);
            }
        }
    }
}

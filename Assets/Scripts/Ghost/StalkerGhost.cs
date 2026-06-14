using Ashlight.Player;
using UnityEngine;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Persistent follower ghost that maintains close stalk distance once the player is detected.
    /// </summary>
    [DisallowMultipleComponent]
    public class StalkerGhost : GhostAIController
    {
        private const float ShortRetreatDistance = 6f;
        private const float BreathInterval = 8f;

        [SerializeField] private float followDistance = 4f;
        [SerializeField] private float circleSpeed = 1f;
        [SerializeField] private ParticleSystem breathEffect;
        [SerializeField] private AudioSource breathAudioSource;

        private bool _hasDetectedPlayer;
        private Vector3 _lastKnownPlayerPosition;
        private float _breathTimer;
        private PlayerController _playerController;

        /// <inheritdoc />
        protected override void Awake()
        {
            base.Awake();

            if (breathEffect == null)
            {
                breathEffect = GetComponentInChildren<ParticleSystem>();
            }

            if (breathAudioSource == null)
            {
                breathAudioSource = GetComponent<AudioSource>();
            }
        }

        /// <inheritdoc />
        public override void Activate(Transform playerTransform)
        {
            _hasDetectedPlayer = false;
            _breathTimer = BreathInterval;
            base.Activate(playerTransform);

            if (PlayerTransform != null)
            {
                _playerController = PlayerTransform.GetComponent<PlayerController>();
            }
        }

        /// <inheritdoc />
        protected override float RetreatDistance => ShortRetreatDistance;

        /// <inheritdoc />
        protected override PerceptionLevel CalculateCurrentPerception()
        {
            PerceptionLevel perceptionLevel = base.CalculateCurrentPerception();

            if (perceptionLevel >= PerceptionLevel.Suspicious)
            {
                _hasDetectedPlayer = true;
            }

            if (_hasDetectedPlayer && PlayerTransform != null)
            {
                _lastKnownPlayerPosition = PlayerTransform.position;
            }

            if (_hasDetectedPlayer && perceptionLevel == PerceptionLevel.Unaware)
            {
                return PerceptionLevel.Suspicious;
            }

            return perceptionLevel;
        }

        /// <inheritdoc />
        protected override void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
        {
            if (_hasDetectedPlayer)
            {
                if (perceptionLevel == PerceptionLevel.Detected &&
                    (CurrentState == GhostState.Wander || CurrentState == GhostState.Stalk))
                {
                    ChangeState(GhostState.Chase);
                }
                else if (CurrentState == GhostState.Wander)
                {
                    ChangeState(GhostState.Stalk);
                }

                return;
            }

            base.ApplyPerceptionTransitions(perceptionLevel);
        }

        /// <inheritdoc />
        protected override void UpdateStalk()
        {
            ApplyPerceptionTransitions(CalculateCurrentPerception());

            if (PlayerTransform == null)
            {
                return;
            }

            Vector3 targetPosition = GetStalkTargetPosition();
            SetAgentDestination(targetPosition);
            UpdateBreathEffects();
        }

        /// <inheritdoc />
        protected override void UpdateRetreat()
        {
            float distanceFromPlayer = Vector3.Distance(transform.position, PlayerTransform.position);
            if (distanceFromPlayer < ShortRetreatDistance)
            {
                return;
            }

            bool reachedDestination = Agent == null ||
                                      !Agent.isOnNavMesh ||
                                      (!Agent.pathPending &&
                                       Agent.remainingDistance <= Agent.stoppingDistance);

            if (!reachedDestination)
            {
                return;
            }

            ChangeState(GhostState.Stalk);
        }

        /// <inheritdoc />
        protected override void UpdateRecharge()
        {
            base.UpdateRecharge();

            if (_hasDetectedPlayer && CurrentState == GhostState.Wander)
            {
                ChangeState(GhostState.Stalk);
            }
        }

        /// <inheritdoc />
        protected override void UpdateIdle()
        {
            if (_hasDetectedPlayer)
            {
                ChangeState(GhostState.Stalk);
                return;
            }

            base.UpdateIdle();
        }

        /// <summary>Gets the stalk destination behind or circling the player.</summary>
        /// <returns>World-space stalk target.</returns>
        protected Vector3 GetStalkTargetPosition()
        {
            Vector3 playerPosition = PlayerTransform.position;
            bool playerIsMoving = _playerController != null && _playerController.IsMoving;

            if (!playerIsMoving)
            {
                float angle = Time.time * circleSpeed;
                Vector3 circleOffset = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * followDistance;
                return playerPosition + circleOffset;
            }

            Vector3 playerForward = PlayerTransform.forward;
            playerForward.y = 0f;

            if (playerForward.sqrMagnitude <= 0.001f)
            {
                playerForward = (transform.position - playerPosition).normalized;
            }

            return playerPosition - playerForward.normalized * followDistance;
        }

        private void UpdateBreathEffects()
        {
            _breathTimer -= Time.deltaTime;
            if (_breathTimer > 0f)
            {
                return;
            }

            _breathTimer = BreathInterval;

            if (breathEffect != null)
            {
                breathEffect.Play();
            }

            if (breathAudioSource != null && breathAudioSource.clip != null)
            {
                breathAudioSource.Play();
            }
        }
    }
}

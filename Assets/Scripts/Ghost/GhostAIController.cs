using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Ghost behavioral states driven by perception and torch light.
    /// </summary>
    public enum GhostState
    {
        Idle,
        Wander,
        Stalk,
        Chase,
        Attack,
        Retreat
    }

    /// <summary>
    /// NavMesh-driven ghost AI with perception-based state transitions.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NavMeshAgent))]
    public class GhostAIController : MonoBehaviour
    {
        private const float IdleMinDuration = 3f;
        private const float IdleMaxDuration = 8f;
        private const float WanderDestinationRadius = 10f;
        private const float StalkFollowDistance = 6f;
        private const float RetreatDistanceFromPlayer = 15f;
        private const float AttackCooldown = 1.5f;
        private const float DebugLogInterval = 2f;

        [SerializeField] private GhostTypeDefinition ghostType;
        [SerializeField] private GhostPerceptionSystem perception;
        [SerializeField] private Transform player;
        [SerializeField] private Renderer ghostRenderer;
        [SerializeField] [Range(0f, 1f)] private float retreatLightThreshold = 0.7f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onAttackPlayer;
        [SerializeField] private UnityEvent _onRetreat;

        private NavMeshAgent _navMeshAgent;
        private GhostState _currentState;
        private float _stateTimer;
        private float _attackCooldownTimer;
        private float _debugLogTimer;
        private Vector3 _retreatDestination;
        private bool _isActive;

        /// <summary>Gets the assigned ghost type definition.</summary>
        public GhostTypeDefinition GhostType => ghostType;

        /// <summary>Gets the current AI state.</summary>
        public GhostState CurrentState => _currentState;

        /// <summary>Gets whether this ghost is active in the spawn pool.</summary>
        public bool IsSpawnActive => _isActive;

        /// <summary>Invoked when the ghost performs an attack.</summary>
        public UnityEvent OnAttackPlayer => _onAttackPlayer;

        /// <summary>Invoked when the ghost begins retreating from torch light.</summary>
        public UnityEvent OnRetreat => _onRetreat;

        private void Awake()
        {
            _navMeshAgent = GetComponent<NavMeshAgent>();

            if (_navMeshAgent == null)
            {
                Debug.LogError($"{nameof(GhostAIController)} requires a {nameof(NavMeshAgent)}.", this);
                enabled = false;
                return;
            }

            if (ghostType == null)
            {
                Debug.LogError($"{nameof(GhostAIController)} requires a {nameof(GhostTypeDefinition)}.", this);
                enabled = false;
                return;
            }

            if (perception == null)
            {
                Debug.LogError($"{nameof(GhostAIController)} requires a {nameof(GhostPerceptionSystem)}.", this);
                enabled = false;
                return;
            }

            ApplyGhostTypeVisuals();
            _navMeshAgent.enabled = false;
        }

        /// <summary>Assigns the player target at runtime.</summary>
        /// <param name="playerTransform">Player transform to track.</param>
        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;

            if (perception != null)
            {
                perception.SetPlayer(playerTransform);
            }
        }

        private void Update()
        {
            if (!_isActive || ghostType == null || perception == null || player == null)
            {
                return;
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            UpdateDebugLogTimer();

            if (_currentState != GhostState.Retreat && ShouldRetreat())
            {
                ChangeState(GhostState.Retreat);
            }

            switch (_currentState)
            {
                case GhostState.Idle:
                    UpdateIdle();
                    break;
                case GhostState.Wander:
                    UpdateWander();
                    break;
                case GhostState.Stalk:
                    UpdateStalk();
                    break;
                case GhostState.Chase:
                    UpdateChase();
                    break;
                case GhostState.Attack:
                    UpdateAttack();
                    break;
                case GhostState.Retreat:
                    UpdateRetreat();
                    break;
            }
        }

        /// <summary>
        /// Starts AI behavior after the spawn manager positions and warps the agent.
        /// </summary>
        /// <param name="playerTransform">Player transform to track.</param>
        public void Activate(Transform playerTransform)
        {
            if (playerTransform != null)
            {
                SetPlayer(playerTransform);
            }

            _isActive = true;
            _debugLogTimer = DebugLogInterval;

            if (_navMeshAgent != null && !_navMeshAgent.enabled)
            {
                _navMeshAgent.enabled = true;
            }

            ChangeState(GhostState.Wander);
        }

        /// <summary>
        /// Deactivates this ghost and returns it to the spawn pool.
        /// </summary>
        public void Deactivate()
        {
            _isActive = false;
            SetAgentStopped(true);
            ResetAgentPath();
            _currentState = GhostState.Idle;

            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }

            gameObject.SetActive(false);
        }

        private void ApplyGhostTypeVisuals()
        {
            if (ghostRenderer == null)
            {
                ghostRenderer = GetComponentInChildren<Renderer>();
            }

            if (ghostRenderer != null && ghostType.VisualMaterial != null)
            {
                ghostRenderer.material = ghostType.VisualMaterial;
            }

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }
        }

        private float GetPlayerTorchFuelPercent()
        {
            var torch = player.GetComponentInChildren<Systems.HolyTorch>();
            return torch != null ? torch.FuelPercent : 0f;
        }

        /// <summary>
        /// Determines whether torch light should force a retreat.
        /// </summary>
        /// <param name="lightLevel">Sampled torch light from 0 to 1.</param>
        /// <param name="threshold">Light level above which the ghost retreats.</param>
        /// <returns>True when the ghost should retreat.</returns>
        public static bool ShouldRetreatFromLight(float lightLevel, float threshold)
        {
            return lightLevel > threshold;
        }

        private bool ShouldRetreat()
        {
            float lightLevel = perception.GetLightLevelAtPosition(transform.position);
            return ShouldRetreatFromLight(lightLevel, retreatLightThreshold);
        }

        private void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
        {
            if (_currentState == GhostState.Retreat || _currentState == GhostState.Attack)
            {
                return;
            }

            if (perceptionLevel == PerceptionLevel.Detected &&
                (_currentState == GhostState.Wander || _currentState == GhostState.Stalk))
            {
                ChangeState(GhostState.Chase);
                return;
            }

            if (perceptionLevel == PerceptionLevel.Suspicious && _currentState == GhostState.Wander)
            {
                ChangeState(GhostState.Stalk);
            }
        }

        private void UpdateDebugLogTimer()
        {
            _debugLogTimer -= Time.deltaTime;
            if (_debugLogTimer > 0f)
            {
                return;
            }

            _debugLogTimer = DebugLogInterval;
            Debug.Log($"Ghost state: {_currentState}, Distance to player: {Vector3.Distance(transform.position, player.position)}");
        }

        private void ChangeState(GhostState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            ExitState(_currentState);
            _currentState = newState;
            EnterState(_currentState);
        }

        private void EnterState(GhostState state)
        {
            switch (state)
            {
                case GhostState.Idle:
                    EnterIdle();
                    break;
                case GhostState.Wander:
                    EnterWander();
                    break;
                case GhostState.Stalk:
                    EnterStalk();
                    break;
                case GhostState.Chase:
                    EnterChase();
                    break;
                case GhostState.Attack:
                    EnterAttack();
                    break;
                case GhostState.Retreat:
                    EnterRetreat();
                    break;
            }
        }

        private void ExitState(GhostState state)
        {
            switch (state)
            {
                case GhostState.Idle:
                    ExitIdle();
                    break;
                case GhostState.Wander:
                    ExitWander();
                    break;
                case GhostState.Stalk:
                    ExitStalk();
                    break;
                case GhostState.Chase:
                    ExitChase();
                    break;
                case GhostState.Attack:
                    ExitAttack();
                    break;
                case GhostState.Retreat:
                    ExitRetreat();
                    break;
            }
        }

        private void EnterIdle()
        {
            SetAgentStopped(true);
            _stateTimer = Random.Range(IdleMinDuration, IdleMaxDuration);
        }

        private void UpdateIdle()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                ChangeState(GhostState.Wander);
            }
        }

        private void ExitIdle()
        {
        }

        private void EnterWander()
        {
            SetAgentStopped(false);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }

            if (!HasActiveWanderDestination())
            {
                SetRandomWanderDestination();
            }
        }

        private void UpdateWander()
        {
            PerceptionLevel perceptionLevel = perception.CalculatePerception(
                transform,
                player,
                GetPlayerTorchFuelPercent());
            ApplyPerceptionTransitions(perceptionLevel);

            if (!HasActiveWanderDestination())
            {
                SetRandomWanderDestination();
                return;
            }

            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh &&
                !_navMeshAgent.pathPending &&
                _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance)
            {
                SetRandomWanderDestination();
            }
        }

        private void ExitWander()
        {
        }

        private void EnterStalk()
        {
            SetAgentStopped(false);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed * 0.75f;
            }
        }

        private void UpdateStalk()
        {
            PerceptionLevel perceptionLevel = perception.CalculatePerception(
                transform,
                player,
                GetPlayerTorchFuelPercent());
            ApplyPerceptionTransitions(perceptionLevel);

            Vector3 offset = (transform.position - player.position).normalized * StalkFollowDistance;
            Vector3 stalkPoint = player.position + offset;
            SetAgentDestination(stalkPoint);
        }

        private void ExitStalk()
        {
        }

        private void EnterChase()
        {
            SetAgentStopped(false);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }
        }

        private void UpdateChase()
        {
            SetAgentDestination(player.position);

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= ghostType.AttackRange)
            {
                ChangeState(GhostState.Attack);
            }
        }

        private void ExitChase()
        {
        }

        private void EnterAttack()
        {
            SetAgentStopped(true);
        }

        private void UpdateAttack()
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer > ghostType.AttackRange * 1.25f)
            {
                ChangeState(GhostState.Chase);
                return;
            }

            Vector3 lookDirection = player.position - transform.position;
            lookDirection.y = 0f;
            if (lookDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection.normalized);
            }

            if (_attackCooldownTimer <= 0f)
            {
                _attackCooldownTimer = AttackCooldown;
                _onAttackPlayer?.Invoke();
            }
        }

        private void ExitAttack()
        {
            SetAgentStopped(false);
        }

        private void EnterRetreat()
        {
            _onRetreat?.Invoke();
            SetAgentStopped(false);

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.RetreatSpeed;
            }

            Vector3 awayDirection = (transform.position - player.position).normalized;
            _retreatDestination = transform.position + awayDirection * RetreatDistanceFromPlayer;

            if (NavMesh.SamplePosition(_retreatDestination, out NavMeshHit hit, 6f, NavMesh.AllAreas))
            {
                _retreatDestination = hit.position;
            }

            SetAgentDestination(_retreatDestination);
        }

        private void UpdateRetreat()
        {
            float distanceFromPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceFromPlayer >= RetreatDistanceFromPlayer)
            {
                ChangeState(GhostState.Idle);
            }
        }

        private void ExitRetreat()
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }
        }

        private bool HasActiveWanderDestination()
        {
            if (_navMeshAgent == null || !_navMeshAgent.isOnNavMesh)
            {
                return false;
            }

            return _navMeshAgent.hasPath &&
                   !_navMeshAgent.pathPending &&
                   _navMeshAgent.remainingDistance > _navMeshAgent.stoppingDistance;
        }

        private void SetRandomWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * WanderDestinationRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y;

            if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, WanderDestinationRadius, NavMesh.AllAreas))
            {
                SetAgentDestination(hit.position);
            }
        }

        private void SetAgentStopped(bool stopped)
        {
            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.isStopped = stopped;
            }
        }

        private void SetAgentDestination(Vector3 destination)
        {
            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.SetDestination(destination);
            }
        }

        private void ResetAgentPath()
        {
            if (_navMeshAgent != null && _navMeshAgent.isOnNavMesh)
            {
                _navMeshAgent.ResetPath();
            }
        }
    }
}

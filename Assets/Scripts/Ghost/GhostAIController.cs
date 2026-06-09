using System.Collections;
using Ashlight.Player;
using Ashlight.Systems;
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
        Retreat,
        Recharge,
        Perish
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
        private const float PlayerAttackCooldown = 1f;
        private const float DebugLogInterval = 2f;
        private const float RechargeCompleteThreshold = 0.7f;
        private const float PerishDelay = 1.5f;
        private const float RechargePulseSpeed = 4f;

        [SerializeField] private GhostTypeDefinition ghostType;
        [SerializeField] private GhostPerceptionSystem perception;
        [SerializeField] private GhostTorchInteraction ghostTorchInteraction;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Renderer ghostRenderer;
        [SerializeField] private Transform ghostHealthBarFill;
        [SerializeField] private ParticleSystem deathParticles;
        [SerializeField] [Range(0f, 1f)] private float retreatLightThreshold = 0.7f;

        [Header("Ghost Health")]
        [SerializeField] private float maxGhostHealth = 100f;
        [SerializeField] private float currentGhostHealth = 100f;
        [SerializeField] private float torchDamagePerSecond = 15f;
        [SerializeField] private float rechargeRate = 10f;
        [SerializeField] private float rechargeThreshold = 0.3f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onAttackPlayer;
        [SerializeField] private UnityEvent _onRetreat;

        private NavMeshAgent _navMeshAgent;
        private GhostState _currentState;
        private float _stateTimer;
        private float _attackCooldownTimer;
        private float _debugLogTimer;
        private Vector3 _retreatDestination;
        private Color _baseRendererColor = Color.white;
        private bool _isActive;
        private bool _isPerishing;
        private Coroutine _perishCoroutine;

        /// <summary>Gets the assigned ghost type definition.</summary>
        public GhostTypeDefinition GhostType => ghostType;

        /// <summary>Gets the current AI state.</summary>
        public GhostState CurrentState => _currentState;

        /// <summary>Gets whether this ghost is active in the spawn pool.</summary>
        public bool IsSpawnActive => _isActive;

        /// <summary>Gets current ghost health as a 0-1 percentage.</summary>
        public float GhostHealthPercent => maxGhostHealth > 0f ? Mathf.Clamp01(currentGhostHealth / maxGhostHealth) : 0f;

        /// <summary>Gets torch damage applied per second when fully in range.</summary>
        public float TorchDamagePerSecond => torchDamagePerSecond;

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

            if (ghostTorchInteraction == null)
            {
                ghostTorchInteraction = GetComponent<GhostTorchInteraction>();
            }

            if (ghostRenderer == null)
            {
                ghostRenderer = GetComponentInChildren<Renderer>();
            }

            if (ghostRenderer != null)
            {
                _baseRendererColor = ghostRenderer.material.color;
            }

            maxGhostHealth = Mathf.Max(1f, maxGhostHealth);
            currentGhostHealth = Mathf.Clamp(currentGhostHealth, 0f, maxGhostHealth);
            ApplyGhostTypeVisuals();
            UpdateHealthBar();
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

            if (playerHealth == null && playerTransform != null)
            {
                playerHealth = playerTransform.GetComponent<PlayerHealth>();
            }
        }

        /// <summary>Assigns the player's holy torch to perception and torch interaction.</summary>
        /// <param name="torch">Player torch component.</param>
        public void SetTorch(HolyTorch torch)
        {
            if (perception != null)
            {
                perception.SetTorch(torch);
            }

            if (ghostTorchInteraction != null)
            {
                ghostTorchInteraction.SetTorch(torch);
            }
        }

        private void Update()
        {
            if (!_isActive || ghostType == null || perception == null || player == null || _isPerishing)
            {
                return;
            }

            _attackCooldownTimer = Mathf.Max(0f, _attackCooldownTimer - Time.deltaTime);
            UpdateDebugLogTimer();

            if (_currentState != GhostState.Retreat &&
                _currentState != GhostState.Recharge &&
                _currentState != GhostState.Perish &&
                ShouldRetreat())
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
                case GhostState.Recharge:
                    UpdateRecharge();
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
            _isPerishing = false;
            _debugLogTimer = DebugLogInterval;
            currentGhostHealth = maxGhostHealth;
            UpdateHealthBar();

            if (ghostRenderer != null)
            {
                ghostRenderer.enabled = true;
                ghostRenderer.material.color = _baseRendererColor;
            }

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
            _isPerishing = false;

            if (_perishCoroutine != null)
            {
                StopCoroutine(_perishCoroutine);
                _perishCoroutine = null;
            }

            SetAgentStopped(true);
            ResetAgentPath();
            _currentState = GhostState.Idle;

            if (_navMeshAgent != null)
            {
                _navMeshAgent.enabled = false;
            }

            if (ghostRenderer != null)
            {
                ghostRenderer.enabled = true;
                ghostRenderer.material.color = _baseRendererColor;
            }

            gameObject.SetActive(false);
        }

        /// <summary>Forces this ghost to retreat immediately.</summary>
        public void ForceRetreat()
        {
            if (!_isActive || _isPerishing)
            {
                return;
            }

            ChangeState(GhostState.Retreat);
        }

        /// <summary>Applies torch damage to this ghost.</summary>
        /// <param name="damage">Damage amount.</param>
        public void TakeTorchDamage(float damage)
        {
            if (!_isActive || _isPerishing || damage <= 0f)
            {
                return;
            }

            currentGhostHealth = Mathf.Max(0f, currentGhostHealth - damage);
            UpdateHealthBar();

            if (currentGhostHealth <= 0f)
            {
                Perish();
                return;
            }

            if (GhostHealthPercent < rechargeThreshold &&
                _currentState != GhostState.Retreat &&
                _currentState != GhostState.Recharge)
            {
                ChangeState(GhostState.Retreat);
            }
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

        private void Perish()
        {
            if (_isPerishing)
            {
                return;
            }

            _perishCoroutine = StartCoroutine(PerishRoutine());
        }

        private IEnumerator PerishRoutine()
        {
            _isPerishing = true;
            _currentState = GhostState.Perish;
            SetAgentStopped(true);
            ResetAgentPath();

            if (deathParticles != null)
            {
                deathParticles.Play();
            }

            if (ghostRenderer != null)
            {
                ghostRenderer.enabled = false;
            }

            yield return new WaitForSeconds(PerishDelay);

            currentGhostHealth = maxGhostHealth;
            UpdateHealthBar();
            _isPerishing = false;
            _perishCoroutine = null;
            Deactivate();
        }

        private void ApplyGhostTypeVisuals()
        {
            if (ghostRenderer != null && ghostType.VisualMaterial != null)
            {
                ghostRenderer.material = ghostType.VisualMaterial;
                _baseRendererColor = ghostRenderer.material.color;
            }

            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }
        }

        private float GetPlayerTorchFuelPercent()
        {
            HolyTorch torch = player.GetComponentInChildren<HolyTorch>();
            return torch != null ? torch.FuelPercent : 0f;
        }

        private bool ShouldRetreat()
        {
            if (ghostTorchInteraction == null || !ghostTorchInteraction.IsInTorchRange)
            {
                return false;
            }

            float lightLevel = perception.GetLightLevelAtPosition(transform.position);
            return ShouldRetreatFromLight(lightLevel, retreatLightThreshold);
        }

        private void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
        {
            if (_currentState == GhostState.Retreat ||
                _currentState == GhostState.Recharge ||
                _currentState == GhostState.Attack ||
                _currentState == GhostState.Perish)
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

        private void UpdateHealthBar()
        {
            if (ghostHealthBarFill == null)
            {
                return;
            }

            Vector3 scale = ghostHealthBarFill.localScale;
            scale.x = GhostHealthPercent;
            ghostHealthBarFill.localScale = scale;
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
                case GhostState.Recharge:
                    EnterRecharge();
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
                case GhostState.Recharge:
                    ExitRecharge();
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
                _attackCooldownTimer = PlayerAttackCooldown;

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(ghostType.Damage);
                }

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
            if (distanceFromPlayer < RetreatDistanceFromPlayer)
            {
                return;
            }

            bool reachedDestination = _navMeshAgent == null ||
                                      !_navMeshAgent.isOnNavMesh ||
                                      (!_navMeshAgent.pathPending &&
                                       _navMeshAgent.remainingDistance <= _navMeshAgent.stoppingDistance);

            if (!reachedDestination)
            {
                return;
            }

            if (GhostHealthPercent < rechargeThreshold)
            {
                ChangeState(GhostState.Recharge);
            }
            else
            {
                ChangeState(GhostState.Wander);
            }
        }

        private void ExitRetreat()
        {
            if (_navMeshAgent != null)
            {
                _navMeshAgent.speed = ghostType.MoveSpeed;
            }
        }

        private void EnterRecharge()
        {
            SetAgentStopped(true);
        }

        private void UpdateRecharge()
        {
            currentGhostHealth = Mathf.Min(maxGhostHealth, currentGhostHealth + rechargeRate * Time.deltaTime);
            UpdateHealthBar();
            ApplyRechargePulse();

            if (GhostHealthPercent >= RechargeCompleteThreshold)
            {
                ChangeState(GhostState.Wander);
            }
        }

        private void ExitRecharge()
        {
            if (ghostRenderer != null)
            {
                ghostRenderer.material.color = _baseRendererColor;
            }
        }

        private void ApplyRechargePulse()
        {
            if (ghostRenderer == null)
            {
                return;
            }

            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * RechargePulseSpeed);
            ghostRenderer.material.color = _baseRendererColor * pulse;
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

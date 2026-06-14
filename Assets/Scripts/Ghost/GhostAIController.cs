using System.Collections;
using Ashlight.Player;
using Ashlight.Systems;
using Ashlight.UI;
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
        protected const float IdleMinDuration = 3f;
        protected const float IdleMaxDuration = 8f;
        protected const float WanderDestinationRadius = 10f;
        protected const float StalkFollowDistance = 6f;
        protected const float RetreatDistanceFromPlayer = 15f;
        protected const float PlayerAttackCooldown = 1f;
        protected const float DebugLogInterval = 2f;
        protected const float RechargeCompleteThreshold = 0.7f;
        protected const float PerishDelay = 1.5f;
        protected const float RechargePulseSpeed = 4f;

        [SerializeField] private GhostTypeDefinition ghostType;
        [SerializeField] private GhostPerceptionSystem perception;
        [SerializeField] private GhostTorchInteraction ghostTorchInteraction;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Renderer ghostRenderer;
        [SerializeField] private ParticleSystem deathParticles;
        [SerializeField] private GameObject faithOrbPrefab;
        [SerializeField] private int faithOnDeath = 10;
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

        /// <summary>Raised when ghost health percentage changes from 0 to 1.</summary>
        public event System.Action<float> HealthPercentChanged;

        /// <summary>Gets the NavMesh agent used for movement.</summary>
        protected NavMeshAgent Agent => _navMeshAgent;

        /// <summary>Gets the tracked player transform.</summary>
        protected Transform PlayerTransform => player;

        /// <summary>Gets the ghost perception system.</summary>
        protected GhostPerceptionSystem PerceptionSystem => perception;

        /// <summary>Gets the ghost renderer.</summary>
        protected Renderer GhostRenderer => ghostRenderer;

        /// <summary>Gets the player health component.</summary>
        protected PlayerHealth PlayerHealthComponent => playerHealth;

        /// <summary>Gets the torch interaction component.</summary>
        protected GhostTorchInteraction TorchInteraction => ghostTorchInteraction;

        /// <summary>Gets the ghost base renderer color.</summary>
        protected Color BaseRendererColor => _baseRendererColor;

        /// <summary>Gets whether the ghost is currently perishing.</summary>
        protected bool IsPerishing => _isPerishing;

        /// <summary>Gets the health fraction that triggers retreat.</summary>
        protected virtual float RetreatHealthThreshold => rechargeThreshold;

        /// <summary>Gets the retreat distance from the player.</summary>
        protected virtual float RetreatDistance => RetreatDistanceFromPlayer;

        /// <summary>Gets or sets current ghost health.</summary>
        protected float CurrentGhostHealth
        {
            get => currentGhostHealth;
            set
            {
                currentGhostHealth = Mathf.Clamp(value, 0f, maxGhostHealth);
                UpdateHealthBar();
            }
        }

        /// <summary>Gets maximum ghost health.</summary>
        protected float MaxGhostHealth => maxGhostHealth;

        /// <summary>Initializes required components and default ghost state.</summary>
        protected virtual void Awake()
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

        /// <summary>Updates ghost AI while active.</summary>
        protected virtual void Update()
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
        public virtual void Activate(Transform playerTransform)
        {
            if (playerTransform != null)
            {
                SetPlayer(playerTransform);
            }

            _isActive = true;
            _isPerishing = false;
            _debugLogTimer = DebugLogInterval;
            currentGhostHealth = maxGhostHealth;

            GhostHealthBar healthBar = GetComponentInChildren<GhostHealthBar>(true);
            if (healthBar != null)
            {
                healthBar.gameObject.SetActive(true);
                healthBar.RefreshFromGhost();
            }

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
        public virtual void Deactivate()
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
        public virtual void ForceRetreat()
        {
            if (!_isActive || _isPerishing)
            {
                return;
            }

            ChangeState(GhostState.Retreat);
        }

        /// <summary>Applies torch damage to this ghost.</summary>
        /// <param name="damage">Damage amount.</param>
        public virtual void TakeTorchDamage(float damage)
        {
            if (!_isActive || _isPerishing || damage <= 0f)
            {
                return;
            }

            currentGhostHealth = Mathf.Max(0f, currentGhostHealth - damage);
            UpdateHealthBar();

            if (currentGhostHealth <= 0f)
            {
                OnHealthDepleted();
                return;
            }

            if (GhostHealthPercent < RetreatHealthThreshold &&
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

        /// <summary>Handles ghost health reaching zero.</summary>
        protected virtual void OnHealthDepleted()
        {
            Perish();
        }

        /// <summary>Determines whether torch light should force a retreat.</summary>
        /// <returns>True when the ghost should retreat from torch light.</returns>
        protected virtual bool ShouldRetreat()
        {
            if (ghostTorchInteraction == null || !ghostTorchInteraction.IsInTorchRange)
            {
                return false;
            }

            float lightLevel = perception.GetLightLevelAtPosition(transform.position);
            return ShouldRetreatFromLight(lightLevel, retreatLightThreshold);
        }

        /// <summary>Calculates the current perception level for this ghost.</summary>
        /// <returns>Perception level relative to the player.</returns>
        protected virtual PerceptionLevel CalculateCurrentPerception()
        {
            return perception.CalculatePerception(
                transform,
                player,
                GetPlayerTorchFuelPercent());
        }

        /// <summary>Applies perception-driven state transitions.</summary>
        /// <param name="perceptionLevel">Current perception level.</param>
        protected virtual void ApplyPerceptionTransitions(PerceptionLevel perceptionLevel)
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

        /// <summary>Transitions the ghost to a new AI state.</summary>
        /// <param name="newState">Target state.</param>
        protected void ChangeState(GhostState newState)
        {
            if (_currentState == newState)
            {
                return;
            }

            ExitState(_currentState);
            _currentState = newState;
            EnterState(_currentState);
        }

        /// <summary>Gets the player's current torch fuel percentage.</summary>
        /// <returns>Torch fuel from 0 to 1.</returns>
        protected float GetPlayerTorchFuelPercent()
        {
            if (player == null)
            {
                return 0f;
            }

            HolyTorch torch = player.GetComponentInChildren<HolyTorch>();
            return torch != null ? torch.FuelPercent : 0f;
        }

        /// <summary>Sets the NavMesh agent movement speed.</summary>
        /// <param name="speed">Speed in units per second.</param>
        protected void SetAgentSpeed(float speed)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.speed = speed;
            }
        }

        /// <summary>Stops or resumes NavMesh agent movement.</summary>
        /// <param name="stopped">Whether the agent should stop.</param>
        protected void SetAgentStopped(bool stopped)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.isStopped = stopped;
            }
        }

        /// <summary>Sets the NavMesh agent destination when on a valid mesh.</summary>
        /// <param name="destination">World-space destination.</param>
        protected void SetAgentDestination(Vector3 destination)
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.SetDestination(destination);
            }
        }

        /// <summary>Resets the NavMesh agent path when on a valid mesh.</summary>
        protected void ResetAgentPath()
        {
            if (Agent != null && Agent.isOnNavMesh)
            {
                Agent.ResetPath();
            }
        }

        /// <summary>Warps the NavMesh agent to a valid position.</summary>
        /// <param name="position">Target world position.</param>
        /// <returns>True when the warp succeeded.</returns>
        protected bool WarpAgent(Vector3 position)
        {
            if (Agent == null)
            {
                return false;
            }

            return Agent.Warp(position);
        }

        /// <summary>Samples the NavMesh near a target position.</summary>
        /// <param name="targetPosition">Desired position.</param>
        /// <param name="hit">Sampled NavMesh hit.</param>
        /// <param name="maxDistance">Maximum sample distance.</param>
        /// <returns>True when a valid NavMesh position was found.</returns>
        protected static bool TrySampleNavMeshPosition(Vector3 targetPosition, out NavMeshHit hit, float maxDistance)
        {
            return NavMesh.SamplePosition(targetPosition, out hit, maxDistance, NavMesh.AllAreas);
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
            SpawnFaithOrbOnDeath();

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

        /// <summary>Spawns a faith orb at the ghost's death position when configured.</summary>
        protected virtual void SpawnFaithOrbOnDeath()
        {
            if (faithOrbPrefab == null)
            {
                return;
            }

            GameObject orbObject = Instantiate(faithOrbPrefab, transform.position, Quaternion.identity);
            FaithOrb faithOrb = orbObject.GetComponent<FaithOrb>();
            if (faithOrb != null)
            {
                faithOrb.Initialize(faithOnDeath);
            }
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

        private void UpdateDebugLogTimer()
        {
            _debugLogTimer -= Time.deltaTime;
            if (_debugLogTimer > 0f)
            {
                return;
            }

            _debugLogTimer = DebugLogInterval;
        }

        private void UpdateHealthBar()
        {
            HealthPercentChanged?.Invoke(GhostHealthPercent);
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

        /// <summary>Enters the idle state.</summary>
        protected virtual void EnterIdle()
        {
            SetAgentStopped(true);
            _stateTimer = Random.Range(IdleMinDuration, IdleMaxDuration);
        }

        /// <summary>Updates idle behavior.</summary>
        protected virtual void UpdateIdle()
        {
            _stateTimer -= Time.deltaTime;
            if (_stateTimer <= 0f)
            {
                ChangeState(GhostState.Wander);
            }
        }

        /// <summary>Exits the idle state.</summary>
        protected virtual void ExitIdle()
        {
        }

        /// <summary>Enters the wander state.</summary>
        protected virtual void EnterWander()
        {
            SetAgentStopped(false);
            SetAgentSpeed(ghostType.MoveSpeed);

            if (!HasActiveWanderDestination())
            {
                SetRandomWanderDestination();
            }
        }

        /// <summary>Updates wander behavior.</summary>
        protected virtual void UpdateWander()
        {
            ApplyPerceptionTransitions(CalculateCurrentPerception());

            if (!HasActiveWanderDestination())
            {
                SetRandomWanderDestination();
                return;
            }

            if (Agent != null && Agent.isOnNavMesh &&
                !Agent.pathPending &&
                Agent.remainingDistance <= Agent.stoppingDistance)
            {
                SetRandomWanderDestination();
            }
        }

        /// <summary>Exits the wander state.</summary>
        protected virtual void ExitWander()
        {
        }

        /// <summary>Enters the stalk state.</summary>
        protected virtual void EnterStalk()
        {
            SetAgentStopped(false);
            SetAgentSpeed(ghostType.MoveSpeed * 0.75f);
        }

        /// <summary>Updates stalk behavior.</summary>
        protected virtual void UpdateStalk()
        {
            ApplyPerceptionTransitions(CalculateCurrentPerception());

            Vector3 offset = (transform.position - player.position).normalized * StalkFollowDistance;
            Vector3 stalkPoint = player.position + offset;
            SetAgentDestination(stalkPoint);
        }

        /// <summary>Exits the stalk state.</summary>
        protected virtual void ExitStalk()
        {
        }

        /// <summary>Enters the chase state.</summary>
        protected virtual void EnterChase()
        {
            SetAgentStopped(false);
            SetAgentSpeed(ghostType.MoveSpeed);
        }

        /// <summary>Updates chase behavior.</summary>
        protected virtual void UpdateChase()
        {
            SetAgentDestination(player.position);

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceToPlayer <= ghostType.AttackRange)
            {
                ChangeState(GhostState.Attack);
            }
        }

        /// <summary>Exits the chase state.</summary>
        protected virtual void ExitChase()
        {
        }

        /// <summary>Enters the attack state.</summary>
        protected virtual void EnterAttack()
        {
            SetAgentStopped(true);
        }

        /// <summary>Updates attack behavior.</summary>
        protected virtual void UpdateAttack()
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

        /// <summary>Exits the attack state.</summary>
        protected virtual void ExitAttack()
        {
            SetAgentStopped(false);
        }

        /// <summary>Enters the retreat state.</summary>
        protected virtual void EnterRetreat()
        {
            _onRetreat?.Invoke();
            SetAgentStopped(false);
            SetAgentSpeed(ghostType.RetreatSpeed);

            Vector3 awayDirection = (transform.position - player.position).normalized;
            _retreatDestination = transform.position + awayDirection * RetreatDistance;

            if (TrySampleNavMeshPosition(_retreatDestination, out NavMeshHit hit, 6f))
            {
                _retreatDestination = hit.position;
            }

            SetAgentDestination(_retreatDestination);
        }

        /// <summary>Updates retreat behavior.</summary>
        protected virtual void UpdateRetreat()
        {
            float distanceFromPlayer = Vector3.Distance(transform.position, player.position);
            if (distanceFromPlayer < RetreatDistance)
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

            if (GhostHealthPercent < RetreatHealthThreshold)
            {
                ChangeState(GhostState.Recharge);
            }
            else
            {
                ChangeState(GhostState.Wander);
            }
        }

        /// <summary>Exits the retreat state.</summary>
        protected virtual void ExitRetreat()
        {
            SetAgentSpeed(ghostType.MoveSpeed);
        }

        /// <summary>Enters the recharge state.</summary>
        protected virtual void EnterRecharge()
        {
            SetAgentStopped(true);
        }

        /// <summary>Updates recharge behavior.</summary>
        protected virtual void UpdateRecharge()
        {
            currentGhostHealth = Mathf.Min(maxGhostHealth, currentGhostHealth + rechargeRate * Time.deltaTime);
            UpdateHealthBar();
            ApplyRechargePulse();

            if (GhostHealthPercent >= RechargeCompleteThreshold)
            {
                ChangeState(GhostState.Wander);
            }
        }

        /// <summary>Exits the recharge state.</summary>
        protected virtual void ExitRecharge()
        {
            if (ghostRenderer != null)
            {
                ghostRenderer.material.color = _baseRendererColor;
            }
        }

        /// <summary>Applies a pulsing recharge visual.</summary>
        protected void ApplyRechargePulse()
        {
            if (ghostRenderer == null)
            {
                return;
            }

            float pulse = 0.7f + 0.3f * Mathf.Sin(Time.time * RechargePulseSpeed);
            ghostRenderer.material.color = _baseRendererColor * pulse;
        }

        /// <summary>Gets whether the agent is traveling to a wander destination.</summary>
        /// <returns>True when a wander path is active.</returns>
        protected bool HasActiveWanderDestination()
        {
            if (Agent == null || !Agent.isOnNavMesh)
            {
                return false;
            }

            return Agent.hasPath &&
                   !Agent.pathPending &&
                   Agent.remainingDistance > Agent.stoppingDistance;
        }

        /// <summary>Sets a random wander destination on the NavMesh.</summary>
        protected void SetRandomWanderDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * WanderDestinationRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y;

            if (TrySampleNavMeshPosition(randomDirection, out NavMeshHit hit, WanderDestinationRadius))
            {
                SetAgentDestination(hit.position);
            }
        }
    }
}

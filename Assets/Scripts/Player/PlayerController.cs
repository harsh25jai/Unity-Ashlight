using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ashlight.Player
{
    /// <summary>
    /// Isometric CharacterController movement with stamina-driven running and 8-direction facing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        private const float IsometricYawDegrees = 45f;
        private const float MoveInputThreshold = 0.01f;
        private const float StaminaDrainPerSecond = 15f;
        private const float StaminaRegenPerSecond = 8f;
        private const float DefaultWalkSpeed = 4f;
        private const float DefaultRunSpeed = 7f;
        private const float DefaultMaxStamina = 100f;

        [SerializeField] private InputActionAsset _inputActions;
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private float _rotationSpeed = 720f;
        [SerializeField] private float _gravity = -20f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onStaminaDepleted;
        [SerializeField] private UnityEvent _onStaminaRecovered;

        private CharacterController _characterController;
        private InputActionMap _playerActionMap;
        private InputAction _moveAction;
        private InputAction _runAction;
        private InputAction _interactAction;
        private InputAction _useAbilityAction;

        private Vector2 _moveInput;
        private float _verticalVelocity;
        private float _currentStamina;
        private bool _staminaWasDepleted;
        private float _currentSpeed;
        private float _staminaDrainMultiplier = 1f;

        /// <summary>Gets whether the player is currently providing movement input.</summary>
        public bool IsMoving => _moveInput.sqrMagnitude > MoveInputThreshold;

        /// <summary>Gets the current horizontal movement speed in units per second.</summary>
        public float CurrentSpeed => _currentSpeed;

        /// <summary>Gets the current stamina value.</summary>
        public float CurrentStamina => _currentStamina;

        /// <summary>Gets the fear-driven stamina drain multiplier.</summary>
        public float StaminaDrainMultiplier => _staminaDrainMultiplier;

        /// <summary>Sets the stamina drain multiplier applied while running.</summary>
        /// <param name="multiplier">Multiplier from 1 to 2.</param>
        public void SetStaminaDrainMultiplier(float multiplier)
        {
            _staminaDrainMultiplier = Mathf.Clamp(multiplier, 1f, 2f);
        }

        /// <summary>Adds stamina up to the player's maximum.</summary>
        /// <param name="amount">Stamina amount to restore.</param>
        public void AddStamina(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            _currentStamina = Mathf.Min(GetMaxStamina(), _currentStamina + amount);

            if (_currentStamina > 0f && _staminaWasDepleted)
            {
                _staminaWasDepleted = false;
                _onStaminaRecovered?.Invoke();
            }
        }

        /// <summary>Gets the interact input action.</summary>
        public InputAction InteractAction => _interactAction;

        /// <summary>Gets the ability input action.</summary>
        public InputAction UseAbilityAction => _useAbilityAction;

        /// <summary>Invoked when stamina reaches zero.</summary>
        public UnityEvent OnStaminaDepleted => _onStaminaDepleted;

        /// <summary>Invoked when stamina rises above zero after depletion.</summary>
        public UnityEvent OnStaminaRecovered => _onStaminaRecovered;

        private void Awake()
        {
            _characterController = GetComponent<CharacterController>();

            if (_characterController == null)
            {
                Debug.LogError($"{nameof(PlayerController)} requires a {nameof(CharacterController)}.", this);
                enabled = false;
                return;
            }

            if (_inputActions == null)
            {
                Debug.LogError($"{nameof(PlayerController)} requires an {nameof(InputActionAsset)}.", this);
                enabled = false;
                return;
            }

            _playerActionMap = _inputActions.FindActionMap("Player", throwIfNotFound: true);
            _moveAction = _playerActionMap.FindAction("Move", throwIfNotFound: true);
            _runAction = _playerActionMap.FindAction("Run", throwIfNotFound: true);
            _interactAction = _playerActionMap.FindAction("Interact", throwIfNotFound: true);
            _useAbilityAction = _playerActionMap.FindAction("UseAbility", throwIfNotFound: true);

            _currentStamina = GetMaxStamina();
        }

        private void OnEnable()
        {
            if (_playerActionMap != null)
            {
                _playerActionMap.Enable();
            }
        }

        private void OnDisable()
        {
            if (_playerActionMap != null)
            {
                _playerActionMap.Disable();
            }
        }

        private void Update()
        {
            if (_characterController == null || _moveAction == null || _runAction == null)
            {
                return;
            }

            _moveInput = _moveAction.ReadValue<Vector2>();
            bool wantsToRun = _runAction.IsPressed();
            bool hasMovement = IsMoving;

            UpdateStamina(wantsToRun, hasMovement);

            Vector3 worldDirection = ConvertToIsometricDirection(_moveInput);
            bool isRunning = wantsToRun && hasMovement && _currentStamina > 0f;
            _currentSpeed = hasMovement ? (isRunning ? GetRunSpeed() : GetWalkSpeed()) : 0f;

            if (hasMovement)
            {
                RotateTowardDirection(worldDirection);
            }

            ApplyMovement(worldDirection, _currentSpeed);
        }

        /// <summary>
        /// Converts raw WASD input into a world-space direction aligned to the isometric camera.
        /// </summary>
        /// <param name="input">Planar movement input.</param>
        /// <returns>Normalized world-space direction on the XZ plane.</returns>
        public static Vector3 ConvertToIsometricDirection(Vector2 input)
        {
            Vector3 rawDirection = new Vector3(input.x, 0f, input.y);
            if (rawDirection.sqrMagnitude > 1f)
            {
                rawDirection.Normalize();
            }

            Quaternion isometricRotation = Quaternion.Euler(0f, IsometricYawDegrees, 0f);
            Vector3 worldDirection = isometricRotation * rawDirection;
            worldDirection.y = 0f;

            if (worldDirection.sqrMagnitude > MoveInputThreshold)
            {
                worldDirection.Normalize();
            }
            else
            {
                worldDirection = Vector3.zero;
            }

            return worldDirection;
        }

        private void UpdateStamina(bool wantsToRun, bool hasMovement)
        {
            float maxStamina = GetMaxStamina();
            bool isRunning = wantsToRun && hasMovement && _currentStamina > 0f;

            if (isRunning)
            {
                _currentStamina = Mathf.Max(
                    0f,
                    _currentStamina - StaminaDrainPerSecond * _staminaDrainMultiplier * Time.deltaTime);
            }
            else if (!hasMovement)
            {
                _currentStamina = Mathf.Min(maxStamina, _currentStamina + StaminaRegenPerSecond * Time.deltaTime);
            }

            if (_currentStamina <= 0f && !_staminaWasDepleted)
            {
                _staminaWasDepleted = true;
                _onStaminaDepleted?.Invoke();
            }
            else if (_currentStamina > 0f && _staminaWasDepleted)
            {
                _staminaWasDepleted = false;
                _onStaminaRecovered?.Invoke();
            }
        }

        private void RotateTowardDirection(Vector3 worldDirection)
        {
            Quaternion targetRotation = Quaternion.LookRotation(worldDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                _rotationSpeed * Time.deltaTime);
        }

        private void ApplyMovement(Vector3 worldDirection, float speed)
        {
            if (_characterController.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += _gravity * Time.deltaTime;
            }

            Vector3 horizontalVelocity = worldDirection * speed;
            Vector3 velocity = new Vector3(horizontalVelocity.x, _verticalVelocity, horizontalVelocity.z);
            _characterController.Move(velocity * Time.deltaTime);
        }

        private float GetWalkSpeed()
        {
            return _playerStats != null ? _playerStats.ModifiedMoveSpeed : DefaultWalkSpeed;
        }

        private float GetRunSpeed()
        {
            return _playerStats != null ? _playerStats.ModifiedRunSpeed : DefaultRunSpeed;
        }

        private float GetMaxStamina()
        {
            return _playerStats != null ? _playerStats.ModifiedMaxStamina : DefaultMaxStamina;
        }
    }
}

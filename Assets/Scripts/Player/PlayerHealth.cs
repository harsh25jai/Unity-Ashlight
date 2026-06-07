using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Player
{
    /// <summary>
    /// Tracks player health, damage invincibility, and death signaling for GameManager.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private PlayerStats _playerStats;
        [SerializeField] private float _invincibilityDuration = 1f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onDeath;
        [SerializeField] private UnityEvent<float> _onDamageTaken;
        [SerializeField] private UnityEvent<float> _onHealed;

        private float _currentHealth;
        private float _invincibleUntil;
        private bool _isDead;

        /// <summary>Gets the current health value.</summary>
        public float CurrentHealth => _currentHealth;

        /// <summary>Gets whether the player is currently dead.</summary>
        public bool IsDead => _isDead;

        /// <summary>Gets whether damage is currently blocked by invincibility frames.</summary>
        public bool IsInvincible => Time.time < _invincibleUntil;

        /// <summary>Gets the maximum health from stats, or zero when stats are missing.</summary>
        public float MaxHealth => _playerStats != null ? _playerStats.ModifiedMaxHealth : 0f;

        /// <summary>Invoked when health reaches zero. GameManager should handle game-over flow.</summary>
        public UnityEvent OnDeath => _onDeath;

        /// <summary>Invoked with damage amount after health is reduced.</summary>
        public UnityEvent<float> OnDamageTaken => _onDamageTaken;

        /// <summary>Invoked with heal amount after health is restored.</summary>
        public UnityEvent<float> OnHealed => _onHealed;

        private void Awake()
        {
            if (_playerStats == null)
            {
                Debug.LogError($"{nameof(PlayerHealth)} requires a {nameof(PlayerStats)} asset.", this);
                enabled = false;
            }
        }

        private void Start()
        {
            ResetToFullHealth();
        }

        /// <summary>Resets current health to the modified maximum from stats.</summary>
        public void ResetToFullHealth()
        {
            if (_playerStats == null)
            {
                return;
            }

            _isDead = false;
            _invincibleUntil = 0f;
            _currentHealth = _playerStats.ModifiedMaxHealth;
        }

        /// <summary>Applies damage unless invincible or already dead.</summary>
        /// <param name="amount">Damage amount.</param>
        public void TakeDamage(float amount)
        {
            if (!isActiveAndEnabled || _isDead || amount <= 0f || IsInvincible)
            {
                return;
            }

            _currentHealth = Mathf.Max(0f, _currentHealth - amount);
            _invincibleUntil = Time.time + _invincibilityDuration;
            _onDamageTaken?.Invoke(amount);

            if (_currentHealth <= 0f)
            {
                _isDead = true;
                _onDeath?.Invoke();
            }
        }

        /// <summary>Restores health up to the modified maximum.</summary>
        /// <param name="amount">Heal amount.</param>
        public void Heal(float amount)
        {
            if (!isActiveAndEnabled || _isDead || amount <= 0f || _playerStats == null)
            {
                return;
            }

            float previousHealth = _currentHealth;
            _currentHealth = Mathf.Min(_playerStats.ModifiedMaxHealth, _currentHealth + amount);
            float healedAmount = _currentHealth - previousHealth;

            if (healedAmount > 0f)
            {
                _onHealed?.Invoke(healedAmount);
            }
        }

        /// <summary>Resets health and alive state, typically after GameManager respawn.</summary>
        public void Revive()
        {
            ResetToFullHealth();
        }
    }
}

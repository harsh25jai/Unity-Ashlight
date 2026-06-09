using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Player
{
    /// <summary>
    /// Tracks player health and synchronizes torch output as a shared soul-light value.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth = 100f;
        [SerializeField] private HolyTorch holyTorch;

        [Header("Events")]
        [SerializeField] private UnityEvent _onDeath;
        [SerializeField] private UnityEvent<float> _onDamageTaken;
        [SerializeField] private UnityEvent<float> _onHealed;

        private bool _isDead;

        /// <summary>Gets the current health value.</summary>
        public float CurrentHealth => currentHealth;

        /// <summary>Gets the maximum health value.</summary>
        public float MaxHealth => maxHealth;

        /// <summary>Gets current health as a 0-1 percentage.</summary>
        public float HealthPercent => maxHealth > 0f ? Mathf.Clamp01(currentHealth / maxHealth) : 0f;

        /// <summary>Gets whether the player is currently dead.</summary>
        public bool IsDead => _isDead;

        /// <summary>Invoked when health reaches zero. GameManager should handle game-over flow.</summary>
        public UnityEvent OnDeath => _onDeath;

        /// <summary>Invoked with current health after damage is applied.</summary>
        public UnityEvent<float> OnDamageTaken => _onDamageTaken;

        /// <summary>Invoked with current health after healing is applied.</summary>
        public UnityEvent<float> OnHealed => _onHealed;

        private void Awake()
        {
            if (holyTorch == null)
            {
                holyTorch = GetComponentInChildren<HolyTorch>();
            }

            if (holyTorch == null)
            {
                Debug.LogError($"{nameof(PlayerHealth)} requires a {nameof(HolyTorch)} reference.", this);
                enabled = false;
                return;
            }

            maxHealth = Mathf.Max(1f, maxHealth);
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
            SyncTorchHealth();
        }

        /// <summary>Applies damage and weakens the holy torch proportionally.</summary>
        /// <param name="amount">Damage amount.</param>
        public void TakeDamage(float amount)
        {
            if (!isActiveAndEnabled || _isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Max(0f, currentHealth - amount);
            SyncTorchHealth();
            _onDamageTaken?.Invoke(currentHealth);

            if (currentHealth <= 0f)
            {
                _isDead = true;
                _onDeath?.Invoke();
            }
        }

        /// <summary>Restores health and brightens the holy torch proportionally.</summary>
        /// <param name="amount">Heal amount.</param>
        public void Heal(float amount)
        {
            if (!isActiveAndEnabled || _isDead || amount <= 0f)
            {
                return;
            }

            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            SyncTorchHealth();
            _onHealed?.Invoke(currentHealth);
        }

        /// <summary>Resets health and alive state, typically after GameManager respawn.</summary>
        public void Revive()
        {
            _isDead = false;
            currentHealth = maxHealth;
            SyncTorchHealth();
        }

        private void SyncTorchHealth()
        {
            if (holyTorch != null)
            {
                holyTorch.SetHealthModifier(HealthPercent);
            }
        }
    }
}

using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Player
{
    /// <summary>
    /// Tracks the player's life Faith pool and synchronizes torch output as soul-light fades.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlayerFaith : MonoBehaviour
    {
        public const float DefaultMaxFaith = 100f;

        [SerializeField] private float maxFaith = DefaultMaxFaith;
        [SerializeField] private float currentFaith = DefaultMaxFaith;
        [SerializeField] private HolyTorch holyTorch;

        [Header("Events")]
        [SerializeField] private UnityEvent _onFaithDepleted;
        [SerializeField] private UnityEvent<float> _onFaithDamaged;
        [SerializeField] private UnityEvent<float> _onFaithRestored;

        private bool _isDepleted;

        /// <summary>Gets the current Faith value.</summary>
        public float CurrentFaith => currentFaith;

        /// <summary>Gets the maximum Faith value.</summary>
        public float MaxFaith => maxFaith;

        /// <summary>Gets current Faith as a 0-1 percentage.</summary>
        public float FaithPercent => maxFaith > 0f ? Mathf.Clamp01(currentFaith / maxFaith) : 0f;

        /// <summary>Gets whether Faith has been depleted.</summary>
        public bool IsDepleted => _isDepleted;

        /// <summary>Invoked when Faith reaches zero.</summary>
        public UnityEvent OnFaithDepleted => _onFaithDepleted ??= new UnityEvent();

        /// <summary>Invoked with current Faith after damage is applied.</summary>
        public UnityEvent<float> OnFaithDamaged => _onFaithDamaged ??= new UnityEvent<float>();

        /// <summary>Invoked with current Faith after deliberate restoration.</summary>
        public UnityEvent<float> OnFaithRestored => _onFaithRestored ??= new UnityEvent<float>();

        /// <summary>Legacy alias for <see cref="OnFaithDamaged"/>.</summary>
        public UnityEvent<float> OnDamageTaken => OnFaithDamaged;

        /// <summary>Legacy alias for <see cref="OnFaithDepleted"/>.</summary>
        public UnityEvent OnDeath => OnFaithDepleted;

        private void Awake()
        {
            _onFaithDepleted ??= new UnityEvent();
            _onFaithDamaged ??= new UnityEvent<float>();
            _onFaithRestored ??= new UnityEvent<float>();

            if (holyTorch == null)
            {
                holyTorch = GetComponentInChildren<HolyTorch>();
            }

            if (holyTorch == null)
            {
                Debug.LogError($"{nameof(PlayerFaith)} requires a {nameof(HolyTorch)} reference.", this);
                enabled = false;
                return;
            }

            maxFaith = Mathf.Max(1f, maxFaith);
            currentFaith = Mathf.Clamp(currentFaith, 0f, maxFaith);
            SyncTorchFaith();
        }

        /// <summary>Applies damage and weakens the holy torch proportionally.</summary>
        /// <param name="amount">Faith damage amount.</param>
        public void TakeDamage(float amount)
        {
            ReduceFaith(amount);
        }

        /// <summary>Reduces Faith from ghost contact or environmental damage.</summary>
        /// <param name="amount">Faith damage amount.</param>
        public void ReduceFaith(float amount)
        {
            if (!isActiveAndEnabled || _isDepleted || amount <= 0f)
            {
                return;
            }

            currentFaith = Mathf.Max(0f, currentFaith - amount);
            SyncTorchFaith();
            _onFaithDamaged?.Invoke(currentFaith);

            if (currentFaith <= 0f)
            {
                HandleFaithDepleted();
            }
        }

        /// <summary>Restores Faith through deliberate actions such as church worship or altars.</summary>
        /// <param name="amount">Faith restoration amount.</param>
        public void RestoreFaith(float amount)
        {
            if (!isActiveAndEnabled || amount <= 0f)
            {
                return;
            }

            _isDepleted = false;
            currentFaith = Mathf.Min(maxFaith, currentFaith + amount);
            SyncTorchFaith();
            _onFaithRestored?.Invoke(currentFaith);
        }

        /// <summary>Sets Faith directly for save/load restoration.</summary>
        /// <param name="faith">Faith value clamped between 0 and max Faith.</param>
        public void SetCurrentFaith(float faith)
        {
            currentFaith = Mathf.Clamp(faith, 0f, maxFaith);
            _isDepleted = currentFaith <= 0f;
            SyncTorchFaith();
        }

        /// <summary>Restores Faith to maximum after respawn from save.</summary>
        public void ResetToSavedFaith(float faith)
        {
            _isDepleted = false;
            SetCurrentFaith(faith);
        }

        private void HandleFaithDepleted()
        {
            _isDepleted = true;
            _onFaithDepleted?.Invoke();

            SaveSystem saveSystem = SaveSystem.Instance ?? FindAnyObjectByType<SaveSystem>();
            if (saveSystem != null)
            {
                saveSystem.RespawnFromLastSave();
                return;
            }

            Debug.LogWarning($"{nameof(PlayerFaith)} depleted with no {nameof(SaveSystem)} to respawn from.", this);
        }

        private void SyncTorchFaith()
        {
            if (holyTorch != null)
            {
                holyTorch.SetFaithModifier(FaithPercent);
            }
        }
    }
}

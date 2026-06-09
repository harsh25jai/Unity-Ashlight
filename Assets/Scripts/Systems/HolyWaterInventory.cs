using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Systems
{
    /// <summary>
    /// Shared Holy Water storage used by torch drain, pickups, and church replenishment.
    /// </summary>
    [CreateAssetMenu(fileName = "HolyWaterInventory", menuName = "Ashlight/Systems/Holy Water Inventory")]
    public class HolyWaterInventory : ScriptableObject
    {
        private const float CriticalThreshold = 0.2f;

        [SerializeField] private float current = 100f;
        [SerializeField] private float maxCapacity = 100f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onCriticalLevel;
        [SerializeField] private UnityEvent _onEmpty;
        [SerializeField] private UnityEvent _onReplenished;
        [SerializeField] private UnityEvent _onAmountChanged;

        [System.NonSerialized] private bool _criticalLevelFired;

        /// <summary>Gets the current Holy Water amount.</summary>
        public float Current => current;

        /// <summary>Gets the maximum Holy Water capacity.</summary>
        public float MaxCapacity => maxCapacity;

        /// <summary>Gets the fill level from 0 to 1.</summary>
        public float FillPercent => maxCapacity > 0f ? Mathf.Clamp01(current / maxCapacity) : 0f;

        /// <summary>Invoked once when fill drops to or below 20%.</summary>
        public UnityEvent OnCriticalLevel => _onCriticalLevel;

        /// <summary>Invoked when Holy Water reaches zero.</summary>
        public UnityEvent OnEmpty => _onEmpty;

        /// <summary>Invoked when Holy Water is replenished.</summary>
        public UnityEvent OnReplenished => _onReplenished;

        /// <summary>Invoked whenever the Holy Water amount changes.</summary>
        public UnityEvent OnAmountChanged => _onAmountChanged;

        /// <summary>
        /// Attempts to spend Holy Water.
        /// </summary>
        /// <param name="amount">Amount to spend.</param>
        /// <returns>False when there is insufficient Holy Water.</returns>
        public bool Spend(float amount)
        {
            if (amount <= 0f)
            {
                return true;
            }

            if (current < amount)
            {
                return false;
            }

            float previous = current;
            current -= amount;
            current = Mathf.Max(0f, current);
            EvaluateLevelEvents(previous);
            _onAmountChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// Adds Holy Water up to maximum capacity.
        /// </summary>
        /// <param name="amount">Amount to add.</param>
        public void Replenish(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            float previous = current;
            current = Mathf.Min(maxCapacity, current + amount);

            if (current > previous)
            {
                _onReplenished?.Invoke();
            }

            EvaluateLevelEvents(previous);
            _onAmountChanged?.Invoke();
        }

        /// <summary>Resets Holy Water to maximum capacity.</summary>
        public void ResetToFull()
        {
            float previous = current;
            current = maxCapacity;
            _criticalLevelFired = false;

            if (current > previous)
            {
                _onReplenished?.Invoke();
            }

            _onAmountChanged?.Invoke();
        }

        private void OnDisable()
        {
            _criticalLevelFired = false;
        }

        private void EvaluateLevelEvents(float previousAmount)
        {
            if (current <= 0f && previousAmount > 0f)
            {
                _onEmpty?.Invoke();
            }

            if (FillPercent <= CriticalThreshold && !_criticalLevelFired)
            {
                _criticalLevelFired = true;
                _onCriticalLevel?.Invoke();
            }
            else if (FillPercent > CriticalThreshold)
            {
                _criticalLevelFired = false;
            }
        }
    }
}

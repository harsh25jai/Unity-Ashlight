using System.Collections.Generic;
using UnityEngine;

namespace Ashlight.Player
{
    /// <summary>
    /// Base player stat values and runtime multiplier modifiers for upgrades.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerStats", menuName = "Ashlight/Player/Player Stats")]
    public class PlayerStats : ScriptableObject
    {
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float runSpeed = 7f;
        [SerializeField] private float fearResistance = 0.5f;
        [SerializeField] private float carryCapacity = 100f;

        [System.NonSerialized] private Dictionary<string, float> _modifiers;

        /// <summary>Gets the base maximum health.</summary>
        public float MaxHealth => maxHealth;

        /// <summary>Gets the base maximum stamina.</summary>
        public float MaxStamina => maxStamina;

        /// <summary>Gets the base walk speed.</summary>
        public float MoveSpeed => moveSpeed;

        /// <summary>Gets the base run speed.</summary>
        public float RunSpeed => runSpeed;

        /// <summary>Gets the base fear resistance value.</summary>
        public float FearResistance => fearResistance;

        /// <summary>Gets the base carry capacity.</summary>
        public float CarryCapacity => carryCapacity;

        /// <summary>Gets the modified maximum health.</summary>
        public float ModifiedMaxHealth => GetModifiedValue(maxHealth);

        /// <summary>Gets the modified maximum stamina.</summary>
        public float ModifiedMaxStamina => GetModifiedValue(maxStamina);

        /// <summary>Gets the modified walk speed.</summary>
        public float ModifiedMoveSpeed => GetModifiedValue(moveSpeed);

        /// <summary>Gets the modified run speed.</summary>
        public float ModifiedRunSpeed => GetModifiedValue(runSpeed);

        /// <summary>Gets the modified fear resistance.</summary>
        public float ModifiedFearResistance => GetModifiedValue(fearResistance);

        /// <summary>Gets the modified carry capacity.</summary>
        public float ModifiedCarryCapacity => GetModifiedValue(carryCapacity);

        private Dictionary<string, float> Modifiers => _modifiers ??= new Dictionary<string, float>();

        private void OnDisable()
        {
            ClearModifiers();
        }

        /// <summary>Adds or replaces a stat multiplier identified by a unique id.</summary>
        /// <param name="id">Stable modifier identifier.</param>
        /// <param name="multiplier">Multiplier applied to base stat values.</param>
        public void AddModifier(string id, float multiplier)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"{nameof(PlayerStats)}: modifier id cannot be null or empty.");
                return;
            }

            Modifiers[id] = multiplier;
        }

        /// <summary>Removes a stat multiplier by id.</summary>
        /// <param name="id">Modifier identifier to remove.</param>
        /// <returns>True when a modifier was removed.</returns>
        public bool RemoveModifier(string id)
        {
            if (string.IsNullOrEmpty(id) || _modifiers == null)
            {
                return false;
            }

            return _modifiers.Remove(id);
        }

        /// <summary>Clears all runtime modifiers.</summary>
        public void ClearModifiers()
        {
            _modifiers?.Clear();
        }

        /// <summary>Applies all active multipliers to a base stat value.</summary>
        /// <param name="baseValue">Unmodified stat value.</param>
        /// <returns>The value after all modifiers are applied.</returns>
        public float GetModifiedValue(float baseValue)
        {
            if (_modifiers == null || _modifiers.Count == 0)
            {
                return baseValue;
            }

            float multiplierProduct = 1f;
            foreach (float multiplier in _modifiers.Values)
            {
                multiplierProduct *= multiplier;
            }

            return baseValue * multiplierProduct;
        }
    }
}

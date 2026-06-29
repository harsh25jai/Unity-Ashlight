using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Systems
{
    /// <summary>
    /// Bottle-based Holy Water inventory for combat abilities. Capacity unlocks via progression.
    /// </summary>
    [CreateAssetMenu(fileName = "HolyWaterInventory", menuName = "Ashlight/Holy Water Inventory")]
    public class HolyWaterInventory : ScriptableObject
    {
        public const int AbsoluteMaxBottles = 4;

        [SerializeField] private int currentBottles;
        [SerializeField] private int maxBottles;

        [Header("Events")]
        [SerializeField] private UnityEvent _onBottleConsumed;
        [SerializeField] private UnityEvent _onBottleBroken;
        [SerializeField] private UnityEvent<int> _onBottleCountChanged;
        [SerializeField] private UnityEvent<int> _onCapacityChanged;

        /// <summary>Gets the number of bottles currently carried.</summary>
        public int CurrentBottles => currentBottles;

        /// <summary>Gets the maximum bottles the player can carry.</summary>
        public int MaxBottles => maxBottles;

        /// <summary>Gets whether the player is carrying at least one bottle.</summary>
        public bool HasBottles => currentBottles > 0;

        /// <summary>Gets bottle fill from 0 to 1 based on current and max capacity.</summary>
        public float BottleFillPercent => maxBottles > 0 ? (float)currentBottles / maxBottles : 0f;

        /// <summary>Invoked when a bottle is consumed for an ability.</summary>
        public UnityEvent OnBottleConsumed => _onBottleConsumed;

        /// <summary>Invoked when a bottle is broken by ghost contact.</summary>
        public UnityEvent OnBottleBroken => _onBottleBroken;

        /// <summary>Invoked when the bottle count changes. Passes current bottle count.</summary>
        public UnityEvent<int> OnBottleCountChanged => _onBottleCountChanged;

        /// <summary>Invoked when max bottle capacity changes. Passes new max capacity.</summary>
        public UnityEvent<int> OnCapacityChanged => _onCapacityChanged;

        /// <summary>
        /// Consumes one bottle for combat use.
        /// </summary>
        /// <returns>False when no bottles are available.</returns>
        public bool ConsumeBottle()
        {
            if (currentBottles <= 0)
            {
                return false;
            }

            currentBottles--;
            _onBottleConsumed?.Invoke();
            _onBottleCountChanged?.Invoke(currentBottles);
            return true;
        }

        /// <summary>
        /// Adds one bottle when inventory has spare capacity.
        /// </summary>
        /// <returns>False when inventory is full.</returns>
        public bool AddBottle()
        {
            if (currentBottles >= maxBottles)
            {
                return false;
            }

            currentBottles++;
            _onBottleCountChanged?.Invoke(currentBottles);
            return true;
        }

        /// <summary>
        /// Breaks one carried bottle without consuming it for an ability.
        /// </summary>
        /// <returns>False when no bottles are available.</returns>
        public bool BreakBottle()
        {
            if (currentBottles <= 0)
            {
                return false;
            }

            currentBottles--;
            _onBottleBroken?.Invoke();
            _onBottleCountChanged?.Invoke(currentBottles);
            return true;
        }

        /// <summary>
        /// Increases max bottle capacity up to <see cref="AbsoluteMaxBottles"/>.
        /// </summary>
        /// <param name="amount">Number of additional bottle slots to unlock.</param>
        /// <returns>False when already at the absolute cap.</returns>
        public bool IncreaseCapacity(int amount)
        {
            if (amount <= 0)
            {
                return false;
            }

            int newMax = Mathf.Min(maxBottles + amount, AbsoluteMaxBottles);
            if (newMax == maxBottles)
            {
                return false;
            }

            maxBottles = newMax;
            currentBottles = Mathf.Min(currentBottles, maxBottles);
            _onCapacityChanged?.Invoke(maxBottles);
            return true;
        }

        /// <summary>
        /// Restores exact bottle state for save/load.
        /// </summary>
        /// <param name="bottles">Current bottle count.</param>
        /// <param name="capacity">Maximum bottle capacity.</param>
        public void SetState(int bottles, int capacity)
        {
            maxBottles = Mathf.Clamp(capacity, 0, AbsoluteMaxBottles);
            currentBottles = Mathf.Clamp(bottles, 0, maxBottles);
            _onBottleCountChanged?.Invoke(currentBottles);
            _onCapacityChanged?.Invoke(maxBottles);
        }

        /// <summary>Resets inventory to locked, empty state for a new run.</summary>
        public void ResetToDefault()
        {
            SetState(0, 0);
        }
    }
}

using Ashlight.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// Display-only HUD slider bound to Holy Water bottle fill level.
    /// </summary>
    [DisallowMultipleComponent]
    public class HolyWaterHUDBar : MonoBehaviour
    {
        [SerializeField] private Slider waterSlider;
        [SerializeField] private HolyWaterInventory inventory;

        private void Awake()
        {
            if (waterSlider == null)
            {
                Debug.LogError($"{nameof(HolyWaterHUDBar)} requires a {nameof(Slider)}.", this);
                enabled = false;
                return;
            }

            if (inventory == null)
            {
                Debug.LogError($"{nameof(HolyWaterHUDBar)} requires a {nameof(HolyWaterInventory)} reference.", this);
                enabled = false;
                return;
            }

            waterSlider.minValue = 0f;
            waterSlider.maxValue = 1f;
            waterSlider.interactable = false;
        }

        private void OnEnable()
        {
            if (inventory != null)
            {
                inventory.OnBottleCountChanged.AddListener(OnBottleCountChanged);
                inventory.OnCapacityChanged.AddListener(OnCapacityChanged);
            }
        }

        private void Start()
        {
            RefreshSlider();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnBottleCountChanged.RemoveListener(OnBottleCountChanged);
                inventory.OnCapacityChanged.RemoveListener(OnCapacityChanged);
            }
        }

        /// <summary>Updates the slider when bottle count changes.</summary>
        /// <param name="currentBottles">Current bottle count.</param>
        public void OnBottleCountChanged(int currentBottles)
        {
            RefreshSlider();
        }

        /// <summary>Updates the slider when max capacity changes.</summary>
        /// <param name="maxBottles">Maximum bottle capacity.</param>
        public void OnCapacityChanged(int maxBottles)
        {
            RefreshSlider();
        }

        private void RefreshSlider()
        {
            if (waterSlider == null || inventory == null)
            {
                return;
            }

            waterSlider.value = inventory.BottleFillPercent;
        }
    }
}

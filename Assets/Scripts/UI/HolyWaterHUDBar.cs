using Ashlight.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// Display-only HUD slider bound to Holy Water inventory fill level.
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
                inventory.OnInventoryChanged.AddListener(OnInventoryChanged);
            }
        }

        private void Start()
        {
            if (waterSlider == null || inventory == null)
            {
                return;
            }

            waterSlider.value = GetNormalizedFill();
        }

        private void OnDisable()
        {
            if (inventory != null)
            {
                inventory.OnInventoryChanged.RemoveListener(OnInventoryChanged);
            }
        }

        /// <summary>Updates the slider from a normalized inventory value.</summary>
        /// <param name="normalized">Fill level from 0 to 1.</param>
        public void OnInventoryChanged(float normalized)
        {
            if (waterSlider == null)
            {
                return;
            }

            waterSlider.value = Mathf.Clamp01(normalized);
        }

        private float GetNormalizedFill()
        {
            if (inventory == null || inventory.MaxCapacity <= 0f)
            {
                return 0f;
            }

            return inventory.Current / inventory.MaxCapacity;
        }
    }
}

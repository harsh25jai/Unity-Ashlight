using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Legacy world pickup wrapper that grants one bottle via <see cref="WorldHolyWaterBottle"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class HolyWaterPickup : MonoBehaviour
    {
        [SerializeField] private HolyWaterInventory inventory;

        private void Awake()
        {
            if (inventory == null)
            {
                Debug.LogError($"{nameof(HolyWaterPickup)} requires a {nameof(HolyWaterInventory)} reference.", this);
                enabled = false;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (inventory == null || !other.CompareTag("Player"))
            {
                return;
            }

            if (inventory.AddBottle())
            {
                Destroy(gameObject);
            }
        }
    }
}

using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Allows ghost attacks to randomly break a carried Holy Water bottle.
    /// </summary>
    [DisallowMultipleComponent]
    public class BottleBreakOnGhostContact : MonoBehaviour
    {
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private float breakChancePerHit = 0.15f;
        [SerializeField] private AudioClip bottleBreakSound;

        private AudioSource _audioSource;

        private void Awake()
        {
            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(BottleBreakOnGhostContact)} requires a {nameof(HolyWaterInventory)} reference.", this);
                enabled = false;
                return;
            }

            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null)
            {
                _audioSource = gameObject.AddComponent<AudioSource>();
            }

            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 1f;
        }

        /// <summary>
        /// Attempts to break one bottle based on configured hit chance.
        /// </summary>
        /// <returns>True when a bottle was broken.</returns>
        public bool TryBreakBottle()
        {
            if (holyWaterInventory == null || holyWaterInventory.CurrentBottles <= 0)
            {
                return false;
            }

            if (Random.value > breakChancePerHit)
            {
                return false;
            }

            if (!holyWaterInventory.BreakBottle())
            {
                return false;
            }

            if (bottleBreakSound != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(bottleBreakSound);
            }

            return true;
        }
    }
}

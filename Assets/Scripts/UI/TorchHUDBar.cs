using Ashlight.Systems;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// Display-only HUD slider bound to holy torch fuel level.
    /// </summary>
    [DisallowMultipleComponent]
    public class TorchHUDBar : MonoBehaviour
    {
        [SerializeField] private Slider torchSlider;
        [SerializeField] private HolyTorch torch;

        private void Awake()
        {
            if (torchSlider == null)
            {
                Debug.LogError($"{nameof(TorchHUDBar)} requires a {nameof(Slider)}.", this);
                enabled = false;
                return;
            }

            if (torch == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    torch = playerObject.GetComponentInChildren<HolyTorch>();
                }
            }

            if (torch == null)
            {
                Debug.LogError($"{nameof(TorchHUDBar)} requires a {nameof(HolyTorch)} reference.", this);
                enabled = false;
                return;
            }

            torchSlider.minValue = 0f;
            torchSlider.maxValue = 1f;
            torchSlider.interactable = false;
        }

        private void OnEnable()
        {
            if (torch != null)
            {
                torch.OnFuelChanged.AddListener(OnFuelChanged);
            }
        }

        private void Start()
        {
            if (torchSlider == null || torch == null)
            {
                return;
            }

            torchSlider.value = GetNormalizedFuel();
        }

        private void OnDisable()
        {
            if (torch != null)
            {
                torch.OnFuelChanged.RemoveListener(OnFuelChanged);
            }
        }

        /// <summary>Updates the slider from a normalized fuel value.</summary>
        /// <param name="normalized">Fuel level from 0 to 1.</param>
        public void OnFuelChanged(float normalized)
        {
            if (torchSlider == null)
            {
                return;
            }

            torchSlider.value = Mathf.Clamp01(normalized);
        }

        private float GetNormalizedFuel()
        {
            if (torch == null || torch.MaxFuel <= 0f)
            {
                return 0f;
            }

            return torch.CurrentFuel / torch.MaxFuel;
        }
    }
}

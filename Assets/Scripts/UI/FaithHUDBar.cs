using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// Display-only HUD slider for player Faith fill level.
    /// </summary>
    [DisallowMultipleComponent]
    public class FaithHUDBar : MonoBehaviour
    {
        [SerializeField] private Slider faithSlider;

        private void Awake()
        {
            if (faithSlider == null)
            {
                Debug.LogError($"{nameof(FaithHUDBar)} requires a {nameof(Slider)}.", this);
                enabled = false;
                return;
            }

            faithSlider.minValue = 0f;
            faithSlider.maxValue = 1f;
            faithSlider.interactable = false;
        }

        private void Start()
        {
            if (faithSlider == null)
            {
                return;
            }

            faithSlider.value = 1f;
        }

        private void Update()
        {
            if (Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.f9Key.wasPressedThisFrame)
            {
                UpdateFaithBar(0.5f);
                Debug.Log("Faith bar set to 50% for testing");
            }

            if (Keyboard.current.f10Key.wasPressedThisFrame)
            {
                UpdateFaithBar(1.0f);
                Debug.Log("Faith bar set to 100% for testing");
            }
        }

        /// <summary>
        /// Updates the slider from a normalized Faith value.
        /// </summary>
        /// <param name="faithPercent">Faith level from 0 to 1.</param>
        public void UpdateFaithBar(float faithPercent)
        {
            if (faithSlider == null)
            {
                return;
            }

            faithSlider.value = Mathf.Clamp01(faithPercent);
        }
    }
}

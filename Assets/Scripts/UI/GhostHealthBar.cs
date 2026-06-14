using Ashlight.Ghost;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// World-space ghost health bar driven by a UI Slider with camera billboarding.
    /// Pool-safe: resolves a fresh parent ghost reference on every activation.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostHealthBar : MonoBehaviour
    {
        private static readonly Color FullHealthColor = new Color(0.298f, 0.686f, 0.314f);
        private static readonly Color MidHealthColor = new Color(1f, 0.757f, 0.027f);
        private static readonly Color LowHealthColor = new Color(0.957f, 0.263f, 0.212f);

        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private Canvas healthBarCanvas;

        private GhostAIController _ghostAI;
        private bool _pendingHide;

        private void Awake()
        {
            if (healthSlider == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Slider)}.", this);
                enabled = false;
                return;
            }

            if (fillImage == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Image)} fill image.", this);
                enabled = false;
                return;
            }

            if (healthBarCanvas == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Canvas)}.", this);
                enabled = false;
                return;
            }

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
        }

        private void OnEnable()
        {
            _pendingHide = false;

            Debug.Log($"{nameof(GhostHealthBar)} OnEnable fired on {name}.", this);

            if (_ghostAI != null)
            {
                _ghostAI.HealthPercentChanged -= OnHealthChanged;
            }

            _ghostAI = null;

            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(true);
            }

            GhostAIController foundGhost = GetComponentInParent<GhostAIController>(true);
            if (foundGhost == null)
            {
                Debug.LogWarning(
                    $"[GhostHealthBar] No {nameof(GhostAIController)} found in parent hierarchy for {name}. " +
                    "Health bar will not update until a parent ghost is present.",
                    this);
                return;
            }

            Debug.Log("[GhostHealthBar] Found ghost: " + foundGhost.gameObject.name, this);

            _ghostAI = foundGhost;
            _ghostAI.HealthPercentChanged += OnHealthChanged;
            Debug.Log($"{nameof(GhostHealthBar)} event subscription succeeded on {_ghostAI.gameObject.name}.", this);

            float healthPercent = _ghostAI.GhostHealthPercent;
            ApplyHealthToBar(healthPercent);
            Debug.Log($"{nameof(GhostHealthBar)} OnHealthChanged fired — health: {healthPercent:F2} on {name}.", this);
        }

        private void OnDisable()
        {
            if (_ghostAI != null)
            {
                _ghostAI.HealthPercentChanged -= OnHealthChanged;
                _ghostAI = null;
            }
        }

        private void LateUpdate()
        {
            if (_pendingHide && healthBarCanvas != null && healthBarCanvas.gameObject.activeSelf)
            {
                healthBarCanvas.gameObject.SetActive(false);
                _pendingHide = false;
            }

            if (Camera.main == null)
            {
                return;
            }

            transform.LookAt(
                transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        /// <summary>Syncs the bar fill to the bound ghost's current health ratio.</summary>
        public void ResetHealthBar()
        {
            RefreshFromGhost();
        }

        /// <summary>Re-binds to the parent ghost and syncs the health bar display.</summary>
        public void RefreshFromGhost()
        {
            _pendingHide = false;

            if (_ghostAI != null)
            {
                _ghostAI.HealthPercentChanged -= OnHealthChanged;
            }

            _ghostAI = GetComponentInParent<GhostAIController>(true);
            if (_ghostAI == null)
            {
                return;
            }

            _ghostAI.HealthPercentChanged += OnHealthChanged;

            if (healthBarCanvas != null)
            {
                healthBarCanvas.gameObject.SetActive(true);
            }

            ApplyHealthToBar(_ghostAI.GhostHealthPercent);
        }

        private void OnHealthChanged(float healthPercent)
        {
            Debug.Log($"{nameof(GhostHealthBar)} OnHealthChanged fired — health: {healthPercent:F2} on {name}.", this);

            ApplyHealthToBar(healthPercent);

            if (healthPercent <= 0f)
            {
                _pendingHide = true;
            }
        }

        private void ApplyHealthToBar(float healthPercent)
        {
            if (healthSlider != null)
            {
                healthSlider.value = healthPercent;
            }

            ApplyFillColor(healthPercent);
        }

        private void ApplyFillColor(float healthPercent)
        {
            if (fillImage == null)
            {
                return;
            }

            if (healthPercent > 0.6f)
            {
                fillImage.color = FullHealthColor;
            }
            else if (healthPercent > 0.3f)
            {
                fillImage.color = MidHealthColor;
            }
            else
            {
                fillImage.color = LowHealthColor;
            }
        }
    }
}

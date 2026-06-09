using System.Collections;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Screen-space HUD for Holy Water, player health, and damage feedback.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class HUDManager : MonoBehaviour
    {
        private const float HolyWaterUpdateInterval = 0.1f;
        private const float DamageVignetteDuration = 0.5f;
        private const float DamageVignettePeakAlpha = 0.45f;

        private static readonly Color HealthBarColor = new Color(0.7529412f, 0.1882353f, 0.1882353f, 1f);
        private static readonly Color HolyWaterBarColor = new Color(0.35f, 0.65f, 0.95f, 1f);

        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private PlayerHealth playerHealth;

        private UIDocument _uiDocument;
        private ProgressBar _holyWaterBar;
        private ProgressBar _healthBar;
        private Label _healthLabel;
        private VisualElement _damageVignette;
        private Coroutine _holyWaterRoutine;
        private Coroutine _damageVignetteRoutine;

        /// <summary>
        /// Formats current and maximum health for HUD display.
        /// </summary>
        /// <param name="currentHealth">Current health value.</param>
        /// <param name="maxHealth">Maximum health value.</param>
        /// <returns>Display string such as "85 / 100".</returns>
        public static string FormatHealthDisplay(float currentHealth, float maxHealth)
        {
            int current = Mathf.CeilToInt(currentHealth);
            int max = Mathf.CeilToInt(maxHealth);
            return $"{current} / {max}";
        }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument == null)
            {
                Debug.LogError($"{nameof(HUDManager)} requires a {nameof(UIDocument)}.", this);
                enabled = false;
                return;
            }

            if (holyWaterInventory == null)
            {
                Debug.LogWarning($"{nameof(HUDManager)} has no {nameof(HolyWaterInventory)} assigned.", this);
            }

            if (playerHealth == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerHealth = playerObject.GetComponent<PlayerHealth>();
                }
            }

            if (playerHealth == null)
            {
                Debug.LogWarning($"{nameof(HUDManager)} has no {nameof(PlayerHealth)} assigned.", this);
            }
        }

        private void OnEnable()
        {
            BuildInterface();
            BindPlayerHealthEvents();
            RefreshHealthDisplay(playerHealth != null ? playerHealth.CurrentHealth : 0f);
            RefreshHolyWaterDisplay();

            if (_holyWaterRoutine == null)
            {
                _holyWaterRoutine = StartCoroutine(UpdateHolyWaterRoutine());
            }
        }

        private void OnDisable()
        {
            UnbindPlayerHealthEvents();

            if (_holyWaterRoutine != null)
            {
                StopCoroutine(_holyWaterRoutine);
                _holyWaterRoutine = null;
            }

            if (_damageVignetteRoutine != null)
            {
                StopCoroutine(_damageVignetteRoutine);
                _damageVignetteRoutine = null;
            }
        }

        /// <summary>Plays a brief red vignette flash when the player takes damage.</summary>
        public void PlayDamageVignette()
        {
            if (_damageVignette == null)
            {
                return;
            }

            if (_damageVignetteRoutine != null)
            {
                StopCoroutine(_damageVignetteRoutine);
            }

            _damageVignetteRoutine = StartCoroutine(DamageVignetteRoutine());
        }

        private void BuildInterface()
        {
            VisualElement root = _uiDocument.rootVisualElement;
            root.Clear();
            root.style.flexGrow = 1f;

            VisualElement hudContainer = new VisualElement { name = "hud-container" };
            hudContainer.style.position = Position.Absolute;
            hudContainer.style.top = 24;
            hudContainer.style.left = 24;
            hudContainer.style.width = 300;
            hudContainer.style.flexDirection = FlexDirection.Column;

            _holyWaterBar = new ProgressBar { name = "holy-water-bar", title = "Holy Water" };
            _holyWaterBar.style.height = 22;
            _holyWaterBar.style.marginBottom = 10;
            _holyWaterBar.highValue = 100f;
            hudContainer.Add(_holyWaterBar);

            _healthBar = new ProgressBar { name = "health-bar", title = "Health" };
            _healthBar.style.height = 22;
            _healthBar.style.marginBottom = 6;
            _healthBar.highValue = 100f;
            hudContainer.Add(_healthBar);

            _healthLabel = new Label("100 / 100") { name = "health-label" };
            _healthLabel.style.color = HealthBarColor;
            _healthLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _healthLabel.style.fontSize = 14;
            hudContainer.Add(_healthLabel);

            _damageVignette = new VisualElement { name = "damage-vignette" };
            _damageVignette.style.position = Position.Absolute;
            _damageVignette.style.left = 0;
            _damageVignette.style.right = 0;
            _damageVignette.style.top = 0;
            _damageVignette.style.bottom = 0;
            _damageVignette.style.backgroundColor = new Color(HealthBarColor.r, HealthBarColor.g, HealthBarColor.b, 0f);
            _damageVignette.pickingMode = PickingMode.Ignore;

            root.Add(hudContainer);
            root.Add(_damageVignette);

            _holyWaterBar.schedule.Execute(ApplyHolyWaterBarColor).ExecuteLater(1);
            _healthBar.schedule.Execute(ApplyHealthBarColor).ExecuteLater(1);
        }

        private void ApplyHolyWaterBarColor()
        {
            VisualElement progress = _holyWaterBar?.Q(className: "unity-progress-bar__progress");
            if (progress != null)
            {
                progress.style.backgroundColor = HolyWaterBarColor;
            }
        }

        private void ApplyHealthBarColor()
        {
            VisualElement progress = _healthBar?.Q(className: "unity-progress-bar__progress");
            if (progress != null)
            {
                progress.style.backgroundColor = HealthBarColor;
            }
        }

        private void BindPlayerHealthEvents()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.OnDamageTaken.AddListener(OnPlayerDamageTaken);
            playerHealth.OnHealed.AddListener(OnPlayerHealed);
        }

        private void UnbindPlayerHealthEvents()
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.OnDamageTaken.RemoveListener(OnPlayerDamageTaken);
            playerHealth.OnHealed.RemoveListener(OnPlayerHealed);
        }

        private void OnPlayerDamageTaken(float currentHealth)
        {
            RefreshHealthDisplay(currentHealth);
            PlayDamageVignette();
        }

        private void OnPlayerHealed(float currentHealth)
        {
            RefreshHealthDisplay(currentHealth);
        }

        private void RefreshHealthDisplay(float currentHealth)
        {
            if (_healthBar == null || _healthLabel == null)
            {
                return;
            }

            float maxHealth = playerHealth != null ? playerHealth.MaxHealth : 100f;
            _healthBar.value = currentHealth;
            _healthBar.highValue = maxHealth;
            _healthLabel.text = FormatHealthDisplay(currentHealth, maxHealth);
        }

        private void RefreshHolyWaterDisplay()
        {
            if (_holyWaterBar == null || holyWaterInventory == null)
            {
                return;
            }

            _holyWaterBar.value = holyWaterInventory.Current;
            _holyWaterBar.highValue = holyWaterInventory.MaxCapacity;
        }

        private IEnumerator UpdateHolyWaterRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(HolyWaterUpdateInterval);

            while (enabled)
            {
                RefreshHolyWaterDisplay();
                yield return wait;
            }
        }

        private IEnumerator DamageVignetteRoutine()
        {
            float elapsed = 0f;

            while (elapsed < DamageVignetteDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = elapsed / DamageVignetteDuration;
                float alpha;

                if (normalizedTime < 0.25f)
                {
                    alpha = Mathf.Lerp(0f, DamageVignettePeakAlpha, normalizedTime / 0.25f);
                }
                else
                {
                    alpha = Mathf.Lerp(DamageVignettePeakAlpha, 0f, (normalizedTime - 0.25f) / 0.75f);
                }

                _damageVignette.style.backgroundColor = new Color(
                    HealthBarColor.r,
                    HealthBarColor.g,
                    HealthBarColor.b,
                    alpha);

                yield return null;
            }

            _damageVignette.style.backgroundColor = new Color(HealthBarColor.r, HealthBarColor.g, HealthBarColor.b, 0f);
            _damageVignetteRoutine = null;
        }
    }
}

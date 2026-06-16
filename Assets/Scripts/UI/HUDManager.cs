using System.Collections;
using Ashlight.Environment;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Event-driven UI Toolkit HUD for night cycle, fear, damage feedback, and game over.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class HUDManager : MonoBehaviour
    {
        private const float DamageFlashDuration = 0.3f;
        private const float HighFearPulseThreshold = 0.7f;
        private const float FearHeartbeatRate = 8f;
        private const string MainMenuSceneName = "MainMenu";

        private static readonly Color PhaseDay = new Color(0.478f, 0.722f, 0.478f);
        private static readonly Color PhaseDusk = new Color(0.788f, 0.584f, 0.165f);
        private static readonly Color PhaseNight = new Color(0.353f, 0.478f, 0.722f);
        private static readonly Color PhaseDawn = new Color(0.910f, 0.788f, 0.420f);
        private static readonly Color NightCounterGrey = new Color(0.620f, 0.620f, 0.620f);
        private static readonly Color FearRed = new Color(0.753f, 0.188f, 0.188f);
        private static readonly Color DamageFlashRed = new Color(0.753f, 0.188f, 0.188f, 0.45f);

        [SerializeField] private VisualTreeAsset hudLayout;
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private FearSystem fearSystem;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private ChurchSafeZone churchSafeZone;
        [SerializeField] private string mainMenuSceneName = MainMenuSceneName;

        private UIDocument _uiDocument;
        private Label _nightPhaseLabel;
        private Label _nightCounterLabel;
        private Label _fearIndicator;
        private VisualElement _damageFlash;
        private VisualElement _gameOverPanel;
        private Button _returnChurchButton;
        private Button _mainMenuButton;

        private float _currentFear;
        private Coroutine _damageFlashRoutine;
        private Transform _playerTransform;

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

        /// <summary>Formats a day/night phase for HUD display.</summary>
        /// <param name="phase">Current phase.</param>
        /// <returns>Human-readable phase label.</returns>
        public static string FormatNightPhase(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return "Day";
                case DayNightPhase.Dusk:
                    return "Dusk";
                case DayNightPhase.Night_Early:
                    return "Night — Early";
                case DayNightPhase.Night_Deep:
                    return "Night — Deep";
                case DayNightPhase.Dawn:
                    return "Dawn";
                default:
                    return phase.ToString();
            }
        }

        /// <summary>Formats a compact uppercase phase label for the top-center HUD indicator.</summary>
        /// <param name="phase">Current phase.</param>
        /// <returns>Uppercase phase label such as DAY or NIGHT.</returns>
        public static string FormatNightPhaseIndicator(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return "DAY";
                case DayNightPhase.Dusk:
                    return "DUSK";
                case DayNightPhase.Night_Early:
                case DayNightPhase.Night_Deep:
                    return "NIGHT";
                case DayNightPhase.Dawn:
                    return "DAWN";
                default:
                    return phase.ToString().ToUpperInvariant();
            }
        }

        /// <summary>Gets the indicator color for a day/night phase.</summary>
        /// <param name="phase">Current phase.</param>
        /// <returns>Phase-specific HUD color.</returns>
        public static Color GetNightPhaseColor(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return PhaseDay;
                case DayNightPhase.Dusk:
                    return PhaseDusk;
                case DayNightPhase.Night_Early:
                case DayNightPhase.Night_Deep:
                    return PhaseNight;
                case DayNightPhase.Dawn:
                    return PhaseDawn;
                default:
                    return Color.white;
            }
        }

        /// <summary>Formats the night counter for HUD display.</summary>
        /// <param name="nightCount">Completed night cycles.</param>
        /// <returns>Display string such as "Night 3".</returns>
        public static string FormatNightCount(int nightCount)
        {
            return $"Night {Mathf.Max(0, nightCount)}";
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

            if (hudLayout != null)
            {
                _uiDocument.visualTreeAsset = hudLayout;
            }

            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }

            if (fearSystem == null)
            {
                fearSystem = FindAnyObjectByType<FearSystem>();
            }

            if (playerHealth == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerHealth = playerObject.GetComponent<PlayerHealth>();
                    _playerTransform = playerObject.transform;
                }
            }

            if (churchSafeZone == null)
            {
                churchSafeZone = FindAnyObjectByType<ChurchSafeZone>();
            }
        }

        private void OnEnable()
        {
            CacheVisualElements();
            BindEvents();
            RefreshNightDisplay(dayNightCycle != null ? dayNightCycle.CurrentPhase : DayNightPhase.Day);
            RefreshFearIndicator(_currentFear);
            HideGameOverPanel();
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void Update()
        {
            UpdateFearPulse();
        }

        private void CacheVisualElements()
        {
            VisualElement root = _uiDocument.rootVisualElement;

            _nightPhaseLabel = root.Q<Label>("night-phase-label");
            _nightCounterLabel = root.Q<Label>("night-counter-label") ?? root.Q<Label>("night-count-label");
            _fearIndicator = root.Q<Label>("fear-indicator");
            _damageFlash = root.Q<VisualElement>("damage-flash");
            _gameOverPanel = root.Q<VisualElement>("game-over-panel");
            _returnChurchButton = root.Q<Button>("return-church-button");
            _mainMenuButton = root.Q<Button>("main-menu-button");

            _returnChurchButton?.RegisterCallback<ClickEvent>(_ => OnReturnToChurchClicked());
            _mainMenuButton?.RegisterCallback<ClickEvent>(_ => OnMainMenuClicked());
        }

        private void BindEvents()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.AddListener(OnDayNightPhaseChanged);
            }

            if (fearSystem != null)
            {
                fearSystem.OnFearChanged.AddListener(OnFearChanged);
                _currentFear = fearSystem.CurrentFear;
            }

            if (playerHealth != null)
            {
                playerHealth.OnDamageTaken.AddListener(OnDamageTaken);
                playerHealth.OnDeath.AddListener(OnPlayerDeath);
            }
        }

        private void UnbindEvents()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.RemoveListener(OnDayNightPhaseChanged);
            }

            if (fearSystem != null)
            {
                fearSystem.OnFearChanged.RemoveListener(OnFearChanged);
            }

            if (playerHealth != null)
            {
                playerHealth.OnDamageTaken.RemoveListener(OnDamageTaken);
                playerHealth.OnDeath.RemoveListener(OnPlayerDeath);
            }
        }

        private void OnDayNightPhaseChanged(DayNightPhase phase)
        {
            RefreshNightDisplay(phase);
        }

        private void OnFearChanged(float fear)
        {
            _currentFear = Mathf.Clamp01(fear);
            RefreshFearIndicator(_currentFear);
        }

        private void OnDamageTaken(float currentHealth)
        {
            if (_damageFlash == null)
            {
                return;
            }

            if (_damageFlashRoutine != null)
            {
                StopCoroutine(_damageFlashRoutine);
            }

            _damageFlashRoutine = StartCoroutine(DamageFlashRoutine());
        }

        private void OnPlayerDeath()
        {
            ShowGameOverPanel();
        }

        private void OnReturnToChurchClicked()
        {
            RespawnAtChurch();
            HideGameOverPanel();
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void RespawnAtChurch()
        {
            if (playerHealth != null)
            {
                playerHealth.Revive();
            }

            if (_playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    _playerTransform = playerObject.transform;
                }
            }

            if (_playerTransform != null && churchSafeZone != null)
            {
                _playerTransform.position = churchSafeZone.transform.position;
            }
        }

        private void ShowGameOverPanel()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.style.display = DisplayStyle.Flex;
            }
        }

        private void HideGameOverPanel()
        {
            if (_gameOverPanel != null)
            {
                _gameOverPanel.style.display = DisplayStyle.None;
            }
        }

        private IEnumerator DamageFlashRoutine()
        {
            _damageFlash.style.backgroundColor = DamageFlashRed;

            float elapsed = 0f;
            while (elapsed < DamageFlashDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(DamageFlashRed.a, 0f, elapsed / DamageFlashDuration);
                _damageFlash.style.backgroundColor = new Color(DamageFlashRed.r, DamageFlashRed.g, DamageFlashRed.b, alpha);
                yield return null;
            }

            _damageFlash.style.backgroundColor = new Color(DamageFlashRed.r, DamageFlashRed.g, DamageFlashRed.b, 0f);
            _damageFlashRoutine = null;
        }

        private void UpdateFearPulse()
        {
            if (_fearIndicator == null)
            {
                return;
            }

            float opacity = _currentFear;
            if (_currentFear > HighFearPulseThreshold)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.time * FearHeartbeatRate);
                opacity *= pulse;
            }

            _fearIndicator.style.opacity = opacity;
            _fearIndicator.style.color = FearRed;
        }

        private void RefreshFearIndicator(float fear)
        {
            _currentFear = Mathf.Clamp01(fear);
            UpdateFearPulse();
        }

        private void RefreshNightDisplay(DayNightPhase phase)
        {
            if (_nightPhaseLabel != null)
            {
                _nightPhaseLabel.text = FormatNightPhaseIndicator(phase);
                _nightPhaseLabel.style.color = GetNightPhaseColor(phase);
                _nightPhaseLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                _nightPhaseLabel.style.fontSize = 20;
            }

            if (_nightCounterLabel != null && dayNightCycle != null)
            {
                _nightCounterLabel.text = FormatNightCount(dayNightCycle.NightCycleCount);
                _nightCounterLabel.style.color = NightCounterGrey;
            }
        }
    }
}

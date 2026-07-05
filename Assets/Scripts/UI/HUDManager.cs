using System.Collections;
using Ashlight.Environment;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Event-driven UI Toolkit HUD for night cycle, fear, damage feedback, and game over.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    [DefaultExecutionOrder(-100)]
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
        [FormerlySerializedAs("playerHealth")]
        [SerializeField] private PlayerFaith playerFaith;
        [SerializeField] private SaveSystem saveSystem;
        [SerializeField] private ChurchSafeZone churchSafeZone;
        [SerializeField] private IsometricCameraController cameraController;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private string mainMenuSceneName = MainMenuSceneName;

        private UIDocument _uiDocument;
        private Label _nightPhaseLabel;
        private Label _nightCounterLabel;
        private Label _fearIndicator;
        private VisualElement _damageFlash;
        private VisualElement _gameOverPanel;
        private Button _returnChurchButton;
        private Button _mainMenuButton;
        private CelestialCycleIndicator _celestialCycleIndicator;
        private Label _phaseRemainingLabel;

        private float _currentFear;
        private Coroutine _damageFlashRoutine;
        private Coroutine _grabShakeCoroutine;
        private Coroutine _phaseTimerRoutine;
        private Coroutine _hudInitRoutine;
        private Transform _playerTransform;

        /// <summary>
        /// Formats current and maximum Faith for HUD display.
        /// </summary>
        /// <param name="currentFaith">Current Faith value.</param>
        /// <param name="maxFaith">Maximum Faith value.</param>
        /// <returns>Display string such as "85 / 100".</returns>
        public static string FormatFaithDisplay(float currentFaith, float maxFaith)
        {
            int current = Mathf.CeilToInt(currentFaith);
            int max = Mathf.CeilToInt(maxFaith);
            return $"{current} / {max}";
        }

        /// <summary>Legacy alias for <see cref="FormatFaithDisplay"/>.</summary>
        public static string FormatHealthDisplay(float currentHealth, float maxHealth)
            => FormatFaithDisplay(currentHealth, maxHealth);

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

        /// <summary>Formats remaining phase time as minutes and seconds.</summary>
        /// <param name="seconds">Remaining seconds in the current phase.</param>
        /// <returns>Display string such as "4:32".</returns>
        public static string FormatPhaseRemainingTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt(seconds));
            int minutes = totalSeconds / 60;
            int remainingSeconds = totalSeconds % 60;
            return $"{minutes}:{remainingSeconds:D2}";
        }

        /// <summary>Gets phase progress from 0 at phase start to 1 at phase end.</summary>
        /// <param name="phaseDuration">Total phase duration in seconds.</param>
        /// <param name="remainingPhaseTime">Remaining seconds in the phase.</param>
        /// <returns>Normalized progress through the current phase.</returns>
        public static float GetPhaseProgress(float phaseDuration, float remainingPhaseTime)
        {
            if (phaseDuration <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(1f - remainingPhaseTime / phaseDuration);
        }

        /// <summary>Resolves the celestial HUD icon from normalized cycle time.</summary>
        /// <param name="normalizedCycleTime">Position from 0 to 1 across the full cycle.</param>
        /// <param name="config">Phase duration configuration.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Continuous sun/moon display state.</returns>
        public static CelestialDisplayState ResolveCelestialDisplay(
            float normalizedCycleTime,
            DayNightConfig config,
            int nightCycleCount)
        {
            return CelestialCycleIndicator.ResolveDisplay(normalizedCycleTime, config, nightCycleCount);
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

            if (playerFaith == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerFaith = playerObject.GetComponent<PlayerFaith>();
                    _playerTransform = playerObject.transform;
                }
            }

            if (saveSystem == null)
            {
                saveSystem = FindAnyObjectByType<SaveSystem>();
            }

            if (churchSafeZone == null)
            {
                churchSafeZone = FindAnyObjectByType<ChurchSafeZone>();
            }

            if (cameraController == null)
            {
                cameraController = FindAnyObjectByType<IsometricCameraController>();
            }

            if (postProcessVolume == null)
            {
                postProcessVolume = FindAnyObjectByType<Volume>();
            }
        }

        private void OnEnable()
        {
            if (_hudInitRoutine != null)
            {
                StopCoroutine(_hudInitRoutine);
            }

            _hudInitRoutine = StartCoroutine(InitializeHudWhenReady());
        }

        private IEnumerator InitializeHudWhenReady()
        {
            while (_uiDocument == null || _uiDocument.rootVisualElement == null)
            {
                yield return null;
            }

            CacheVisualElements();
            BindEvents();
            RefreshNightDisplay(dayNightCycle != null ? dayNightCycle.CurrentPhase : DayNightPhase.Day);
            RefreshFearIndicator(_currentFear);
            HideGameOverPanel();

            if (dayNightCycle != null)
            {
                UpdateCelestialIndicator();
            }

            if (_phaseTimerRoutine != null)
            {
                StopCoroutine(_phaseTimerRoutine);
            }

            _phaseTimerRoutine = StartCoroutine(UpdatePhaseTimerLoop());
            _hudInitRoutine = null;
        }

        private void OnDisable()
        {
            if (_hudInitRoutine != null)
            {
                StopCoroutine(_hudInitRoutine);
                _hudInitRoutine = null;
            }

            UnbindEvents();

            if (_phaseTimerRoutine != null)
            {
                StopCoroutine(_phaseTimerRoutine);
                _phaseTimerRoutine = null;
            }

            if (_grabShakeCoroutine != null)
            {
                StopCoroutine(_grabShakeCoroutine);
                _grabShakeCoroutine = null;
            }

            fearSystem?.SetPostProcessSuppressed(false);
        }

        private void Update()
        {
            UpdateFearPulse();
            UpdateCelestialIndicator();
        }

        private void CacheVisualElements()
        {
            VisualElement root = _uiDocument != null ? _uiDocument.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            _nightPhaseLabel = root.Q<Label>("night-phase-label");
            _nightCounterLabel = root.Q<Label>("night-counter-label") ?? root.Q<Label>("night-count-label");
            _fearIndicator = root.Q<Label>("fear-indicator");
            _damageFlash = root.Q<VisualElement>("damage-flash");
            _gameOverPanel = root.Q<VisualElement>("game-over-panel");
            _returnChurchButton = root.Q<Button>("return-church-button");
            _mainMenuButton = root.Q<Button>("main-menu-button");
            _phaseRemainingLabel = root.Q<Label>("phase-remaining-time");
            EnsureCelestialCycleIndicator(root);

            _returnChurchButton?.RegisterCallback<ClickEvent>(_ => OnReturnToChurchClicked());
            _mainMenuButton?.RegisterCallback<ClickEvent>(_ => OnMainMenuClicked());
        }

        private void EnsureCelestialCycleIndicator(VisualElement root)
        {
            _celestialCycleIndicator = root.Q<CelestialCycleIndicator>("celestial-cycle-indicator");
            if (_celestialCycleIndicator != null)
            {
                return;
            }

            VisualElement slot = root.Q<VisualElement>("celestial-cycle-indicator-slot");
            VisualElement parent = slot != null ? slot.parent : root.Q<VisualElement>("celestial-cycle-root");
            if (parent == null)
            {
                return;
            }

            int insertIndex = slot != null ? parent.IndexOf(slot) : 0;
            _celestialCycleIndicator = new CelestialCycleIndicator
            {
                name = "celestial-cycle-indicator"
            };
            _celestialCycleIndicator.style.width = 44f;
            _celestialCycleIndicator.style.height = 44f;
            parent.Insert(insertIndex, _celestialCycleIndicator);
            slot?.RemoveFromHierarchy();
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

            if (playerFaith != null)
            {
                playerFaith.OnFaithDamaged.AddListener(OnFaithDamaged);
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

            if (playerFaith != null)
            {
                playerFaith.OnFaithDamaged.RemoveListener(OnFaithDamaged);
            }
        }

        private void OnDayNightPhaseChanged(DayNightPhase phase)
        {
            RefreshNightDisplay(phase);
            UpdateCelestialIndicator();
            UpdatePhaseRemainingTimeLabel();
        }

        private void OnFearChanged(float fear)
        {
            _currentFear = Mathf.Clamp01(fear);
            RefreshFearIndicator(_currentFear);
        }

        private void OnFaithDamaged(float currentFaith)
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

        private void OnReturnToChurchClicked()
        {
            RespawnFromLastSave();
            HideGameOverPanel();
        }

        private void OnMainMenuClicked()
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }

        private void RespawnFromLastSave()
        {
            if (saveSystem != null)
            {
                saveSystem.RespawnFromLastSave();
                return;
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

            playerFaith?.ResetToSavedFaith(PlayerFaith.DefaultMaxFaith);
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

        private void UpdateCelestialIndicator()
        {
            if (_celestialCycleIndicator == null || dayNightCycle == null)
            {
                return;
            }

            CelestialDisplayState display = ResolveCelestialDisplay(
                dayNightCycle.NormalizedDayTime,
                dayNightCycle.Config,
                dayNightCycle.NightCycleCount);

            _celestialCycleIndicator.ApplyDisplay(display);
        }

        private IEnumerator UpdatePhaseTimerLoop()
        {
            while (true)
            {
                UpdatePhaseRemainingTimeLabel();
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void UpdatePhaseRemainingTimeLabel()
        {
            if (_phaseRemainingLabel == null || dayNightCycle == null)
            {
                return;
            }

            _phaseRemainingLabel.text = FormatPhaseRemainingTime(dayNightCycle.RemainingPhaseTime);
        }

        /// <summary>Applies maximum grab struggle screen effects when a Grabber seizes the player.</summary>
        public void OnGrabStarted()
        {
            fearSystem?.SetPostProcessSuppressed(true);
            SetVignetteIntensity(0.8f);
            SetChromaticAberration(0.8f);

            if (_grabShakeCoroutine != null)
            {
                StopCoroutine(_grabShakeCoroutine);
            }

            _grabShakeCoroutine = StartCoroutine(GrabShakeLoop());
        }

        /// <summary>Restores fear-driven post-processing after a Grabber releases the player.</summary>
        public void OnGrabReleased()
        {
            if (_grabShakeCoroutine != null)
            {
                StopCoroutine(_grabShakeCoroutine);
                _grabShakeCoroutine = null;
            }

            fearSystem?.SetPostProcessSuppressed(false);
            float fearVignette = fearSystem != null ? fearSystem.CurrentFear * 0.5f : 0f;
            SetVignetteIntensity(fearVignette);
            SetChromaticAberration(0f);
        }

        private IEnumerator GrabShakeLoop()
        {
            while (true)
            {
                if (cameraController != null)
                {
                    cameraController.CameraShake(0.12f, 0.25f);
                }

                yield return new WaitForSeconds(0.3f);
            }
        }

        private void SetVignetteIntensity(float intensity)
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            if (postProcessVolume.profile.TryGet(out Vignette vignette))
            {
                vignette.intensity.value = intensity;
            }
        }

        private void SetChromaticAberration(float intensity)
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            if (postProcessVolume.profile.TryGet(out ChromaticAberration chromaticAberration))
            {
                chromaticAberration.intensity.value = intensity;
            }
        }
    }
}

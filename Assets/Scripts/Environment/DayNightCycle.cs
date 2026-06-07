using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Environment
{
    /// <summary>
    /// Phases in the Ashlight day/night horror cycle.
    /// </summary>
    public enum DayNightPhase
    {
        Day,
        Dusk,
        Night_Early,
        Night_Deep,
        Dawn
    }

    /// <summary>
    /// UnityEvent wrapper for <see cref="DayNightPhase"/> transitions.
    /// </summary>
    [System.Serializable]
    public class DayNightPhaseChangedEvent : UnityEvent<DayNightPhase> { }

    /// <summary>
    /// Drives directional light, fog, and ambient settings across a multi-phase day/night loop.
    /// </summary>
    [DisallowMultipleComponent]
    public class DayNightCycle : MonoBehaviour
    {
        private const float NightDeepBaseDuration = 120f;

        [SerializeField] private DayNightConfig _config;
        [SerializeField] private Light _directionalLight;
        [SerializeField] private bool startAtDay = true;
        [SerializeField] private float timeScale = 1f;

        [Header("Curves")]
        [SerializeField] private AnimationCurve _lightIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private Gradient _lightColorGradient = new Gradient();
        [SerializeField] private AnimationCurve _fogDensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private Gradient _ambientLightGradient = new Gradient();

        [Header("Events")]
        [SerializeField] private DayNightPhaseChangedEvent _onPhaseChanged;

        private DayNightPhase _currentPhase;
        private float _phaseElapsed;
        private float _cycleElapsed;
        private int _nightCycleCount;

        /// <summary>Gets the current phase.</summary>
        public DayNightPhase CurrentPhase => _currentPhase;

        /// <summary>Gets normalized progress from 0 to 1 across the full current cycle.</summary>
        public float NormalizedDayTime => GetTotalCycleDuration(_nightCycleCount) > 0f
            ? Mathf.Clamp01(_cycleElapsed / GetTotalCycleDuration(_nightCycleCount))
            : 0f;

        /// <summary>Gets how many full night cycles have completed.</summary>
        public int NightCycleCount => _nightCycleCount;

        /// <summary>Invoked on every phase transition.</summary>
        public DayNightPhaseChangedEvent OnPhaseChanged => _onPhaseChanged;

        private void Awake()
        {
            if (_config == null)
            {
                Debug.LogError($"{nameof(DayNightCycle)} requires a {nameof(DayNightConfig)} asset.", this);
                enabled = false;
                return;
            }

            if (_directionalLight == null)
            {
                _directionalLight = RenderSettings.sun;
            }

            if (_directionalLight == null)
            {
                Debug.LogError($"{nameof(DayNightCycle)} requires a directional {nameof(Light)} reference.", this);
                enabled = false;
                return;
            }

            EnsureDefaultGradients();
            InitializeCycle();
        }

        private void Update()
        {
            if (_config == null || _directionalLight == null)
            {
                return;
            }

            float deltaTime = Time.deltaTime * Mathf.Max(0f, timeScale);
            _phaseElapsed += deltaTime;
            _cycleElapsed += deltaTime;

            float phaseDuration = GetPhaseDuration(_currentPhase, _nightCycleCount);
            while (phaseDuration > 0f && _phaseElapsed >= phaseDuration)
            {
                _phaseElapsed -= phaseDuration;
                AdvancePhase();
                phaseDuration = GetPhaseDuration(_currentPhase, _nightCycleCount);
            }

            ApplyEnvironmentSettings();
        }

        /// <summary>
        /// Calculates Night_Deep duration for a given completed night count.
        /// </summary>
        /// <param name="nightCycleCount">Number of completed night cycles.</param>
        /// <param name="baseDuration">Base Night_Deep duration in seconds.</param>
        /// <param name="growthMultiplier">Per-cycle growth multiplier.</param>
        /// <returns>Scaled Night_Deep duration.</returns>
        public static float CalculateNightDeepDuration(int nightCycleCount, float baseDuration, float growthMultiplier)
        {
            return baseDuration * Mathf.Pow(growthMultiplier, nightCycleCount);
        }

        /// <summary>
        /// Gets the total duration of a full cycle for the given night count.
        /// </summary>
        /// <param name="nightCycleCount">Completed night cycles applied to Night_Deep growth.</param>
        /// <returns>Total cycle duration in seconds.</returns>
        public float GetTotalCycleDuration(int nightCycleCount)
        {
            if (_config == null)
            {
                return 0f;
            }

            return _config.DayDuration
                + _config.DuskDuration
                + _config.NightEarlyDuration
                + CalculateNightDeepDuration(nightCycleCount, NightDeepBaseDuration, _config.NightGrowthMultiplier)
                + _config.DawnDuration;
        }

        private void InitializeCycle()
        {
            _nightCycleCount = 0;
            _phaseElapsed = 0f;
            _cycleElapsed = startAtDay
                ? 0f
                : _config.DayDuration + _config.DuskDuration;
            _currentPhase = startAtDay ? DayNightPhase.Day : DayNightPhase.Night_Early;
            _onPhaseChanged?.Invoke(_currentPhase);
            ApplyEnvironmentSettings();
        }

        private void AdvancePhase()
        {
            switch (_currentPhase)
            {
                case DayNightPhase.Day:
                    SetPhase(DayNightPhase.Dusk);
                    break;
                case DayNightPhase.Dusk:
                    SetPhase(DayNightPhase.Night_Early);
                    break;
                case DayNightPhase.Night_Early:
                    SetPhase(DayNightPhase.Night_Deep);
                    break;
                case DayNightPhase.Night_Deep:
                    SetPhase(DayNightPhase.Dawn);
                    break;
                case DayNightPhase.Dawn:
                    _nightCycleCount++;
                    _cycleElapsed = 0f;
                    SetPhase(DayNightPhase.Day);
                    break;
            }
        }

        private void SetPhase(DayNightPhase phase)
        {
            _currentPhase = phase;
            _onPhaseChanged?.Invoke(_currentPhase);
        }

        private float GetPhaseDuration(DayNightPhase phase, int nightCycleCount)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return _config.DayDuration;
                case DayNightPhase.Dusk:
                    return _config.DuskDuration;
                case DayNightPhase.Night_Early:
                    return _config.NightEarlyDuration;
                case DayNightPhase.Night_Deep:
                    return CalculateNightDeepDuration(
                        nightCycleCount,
                        NightDeepBaseDuration,
                        _config.NightGrowthMultiplier);
                case DayNightPhase.Dawn:
                    return _config.DawnDuration;
                default:
                    return 0f;
            }
        }

        private void ApplyEnvironmentSettings()
        {
            float normalizedTime = NormalizedDayTime;
            float intensityBlend = _lightIntensityCurve.Evaluate(normalizedTime);
            float fogBlend = _fogDensityCurve.Evaluate(normalizedTime);

            _directionalLight.intensity = Mathf.Lerp(
                _config.DayLightIntensity,
                _config.NightLightIntensity,
                intensityBlend);
            _directionalLight.color = _lightColorGradient.Evaluate(normalizedTime);

            RenderSettings.fogDensity = Mathf.Lerp(
                _config.DayFogDensity,
                _config.NightFogDensity,
                fogBlend);
            RenderSettings.ambientLight = _ambientLightGradient.Evaluate(normalizedTime);
        }

        private void EnsureDefaultGradients()
        {
            if (_lightColorGradient == null || _lightColorGradient.colorKeys.Length == 0)
            {
                _lightColorGradient = CreateDayNightGradient(
                    new Color(1f, 0.96f, 0.84f),
                    new Color(0.35f, 0.45f, 0.7f));
            }

            if (_ambientLightGradient == null || _ambientLightGradient.colorKeys.Length == 0)
            {
                _ambientLightGradient = CreateDayNightGradient(
                    _config.DayAmbientColor,
                    _config.NightAmbientColor);
            }
        }

        private static Gradient CreateDayNightGradient(Color dayColor, Color nightColor)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(dayColor, 0f),
                    new GradientColorKey(nightColor, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });
            return gradient;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            timeScale = Mathf.Max(0f, timeScale);

            if (_config != null)
            {
                EnsureDefaultGradients();
            }
        }
#endif
    }
}

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

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
        private const float NightDeepDebugFraction = 0.75f;

        [SerializeField] private DayNightConfig _config;
        [SerializeField] private Light _directionalLight;
        [SerializeField] private bool startAtDay = true;
        [SerializeField] private float timeScale = 1f;
        [SerializeField] public bool pauseCycleForDebug = false;

        [Header("Curves")]
        [SerializeField] private AnimationCurve _lightIntensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private Gradient _lightColorGradient = new Gradient();
        [FormerlySerializedAs("_fogDensityCurve")]
        [SerializeField] private AnimationCurve fogDensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [FormerlySerializedAs("_ambientLightGradient")]
        [SerializeField] private Gradient ambientColorGradient = new Gradient();
        [SerializeField] private Gradient fogColorGradient = new Gradient();

        [Header("Skybox")]
        [SerializeField] private Material skyboxMaterial;
        [SerializeField] private AnimationCurve skyboxExposureCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve sunElevationCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField] private AnimationCurve sunAzimuthCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        [Header("Events")]
        [SerializeField] private DayNightPhaseChangedEvent _onPhaseChanged;

        private DayNightPhase _currentPhase;
        private float _phaseElapsed;
        private float _cycleElapsed;
        private int _nightCycleCount;
        private int _lastBuiltNightCycleCount = -1;

        /// <summary>Gets the current phase.</summary>
        public DayNightPhase CurrentPhase => _currentPhase;

        /// <summary>Gets normalized progress from 0 to 1 across the full current cycle.</summary>
        public float NormalizedDayTime => GetTotalCycleDuration(_nightCycleCount) > 0f
            ? Mathf.Clamp01(_cycleElapsed / GetTotalCycleDuration(_nightCycleCount))
            : 0f;

        /// <summary>Gets how many full night cycles have completed.</summary>
        public int NightCycleCount => _nightCycleCount;

        /// <summary>Gets elapsed time in the current day/night cycle.</summary>
        public float CycleElapsed => _cycleElapsed;

        /// <summary>Gets the cycle time scale multiplier.</summary>
        public float TimeScale => timeScale;

        /// <summary>Gets remaining seconds in the current phase.</summary>
        public float RemainingPhaseTime
        {
            get
            {
                float phaseDuration = GetPhaseDuration(_currentPhase, _nightCycleCount);
                return Mathf.Max(0f, phaseDuration - _phaseElapsed);
            }
        }

        /// <summary>Gets the total duration of the current phase in seconds.</summary>
        public float CurrentPhaseDuration => GetPhaseDuration(_currentPhase, _nightCycleCount);

        /// <summary>Gets progress through the current phase from 0 at start to 1 at end.</summary>
        public float CurrentPhaseProgress
        {
            get
            {
                float phaseDuration = CurrentPhaseDuration;
                return phaseDuration > 0f
                    ? Mathf.Clamp01(1f - RemainingPhaseTime / phaseDuration)
                    : 0f;
            }
        }

        /// <summary>Invoked on every phase transition.</summary>
        public DayNightPhaseChangedEvent OnPhaseChanged => _onPhaseChanged;

        /// <summary>Gets the directional light driven by this cycle.</summary>
        public Light DirectionalLight => _directionalLight;

        /// <summary>Gets the day/night configuration asset.</summary>
        public DayNightConfig Config => _config;

        /// <summary>Gets the single directional-light elevation in degrees at the current normalized time.</summary>
        public float GetSunElevation() => sunElevationCurve.Evaluate(NormalizedDayTime);

        /// <summary>Gets the single directional-light azimuth in degrees at the current normalized time.</summary>
        public float GetSunAzimuth() => Mathf.Repeat(sunAzimuthCurve.Evaluate(NormalizedDayTime), 360f);

        /// <summary>Gets the normalized midpoint of a phase for the current night count.</summary>
        /// <param name="phase">Target phase.</param>
        /// <returns>Normalized cycle time from 0 to 1.</returns>
        public float GetPhaseMidpointNormalized(DayNightPhase phase)
        {
            return _config != null
                ? _config.GetPhaseMidpointNormalized(phase, _nightCycleCount)
                : 0f;
        }

        /// <summary>Gets a normalized cycle time at a fraction through a phase.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="phaseFraction">Progress through the phase from 0 to 1.</param>
        /// <returns>Normalized cycle time from 0 to 1.</returns>
        public float GetPhaseNormalizedTime(DayNightPhase phase, float phaseFraction)
        {
            return _config != null
                ? _config.GetPhaseNormalizedTime(phase, phaseFraction, _nightCycleCount)
                : 0f;
        }

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

            RebuildCurvesFromConfig(force: true);
        }

        /// <summary>
        /// Rebuilds runtime lighting curves from <see cref="_config"/> for the current night count.
        /// </summary>
        /// <param name="force">When true, rebuilds even if curves are already cached for this night count.</param>
        public void RebuildCurvesFromConfig(bool force = false)
        {
            if (_config == null)
            {
                return;
            }

            if (!force &&
                _nightCycleCount == _lastBuiltNightCycleCount &&
                _lightIntensityCurve != null)
            {
                return;
            }

            _lastBuiltNightCycleCount = _nightCycleCount;

            DayNightLightingCurves curves = _config.BuildLightingCurves(_nightCycleCount);
            _lightIntensityCurve = curves.LightIntensity;
            _lightColorGradient = curves.LightColor;
            ambientColorGradient = curves.AmbientColor;
            fogDensityCurve = curves.FogDensity;
            fogColorGradient = curves.FogColor;
            skyboxExposureCurve = curves.SkyboxExposure;
            sunElevationCurve = curves.SunElevation;
            sunAzimuthCurve = curves.SunAzimuth;
        }

        private void Start()
        {
            if (_config == null || _directionalLight == null)
            {
                return;
            }

            _nightCycleCount = 0;

            if (startAtDay)
            {
                BeginAtDayMidpoint();
            }
            else
            {
                BeginAtNightEarly();
            }

            Debug.Log(
                $"DayNightCycle started. NormalizedDayTime: {NormalizedDayTime}, Phase: {_currentPhase}",
                this);
        }

        private void Update()
        {
            if (_config == null || _directionalLight == null)
            {
                return;
            }

            if (!pauseCycleForDebug)
            {
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

                UpdateLighting(NormalizedDayTime);
            }
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
                + CalculateNightDeepDuration(nightCycleCount, _config.NightDeepDuration, _config.NightGrowthMultiplier)
                + _config.DawnDuration;
        }

        /// <summary>Resets the cycle to a fresh day start.</summary>
        public void ResetCycle()
        {
            if (startAtDay)
            {
                BeginAtDayMidpoint();
            }
            else
            {
                BeginAtNightEarly();
            }
        }

        /// <summary>Forces the cycle into a phase and snaps lighting to its normalized time.</summary>
        /// <param name="phase">Target day/night phase.</param>
        public void ForcePhase(DayNightPhase phase)
        {
            if (_config == null)
            {
                return;
            }

            float normalizedTime = phase == DayNightPhase.Night_Deep
                ? _config.GetPhaseNormalizedTime(phase, NightDeepDebugFraction, _nightCycleCount)
                : _config.GetPhaseMidpointNormalized(phase, _nightCycleCount);

            SetNormalizedTime(normalizedTime);
        }

        /// <summary>Sets the day/night cycle time scale multiplier.</summary>
        /// <param name="scale">Time scale from 0 upward.</param>
        public void SetTimeScale(float scale)
        {
            timeScale = Mathf.Max(0f, scale);
        }

        /// <summary>Restores saved night progression and elapsed cycle time.</summary>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <param name="cycleElapsed">Elapsed seconds in the current cycle.</param>
        public void LoadCycleState(int nightCycleCount, float cycleElapsed)
        {
            if (_config == null)
            {
                return;
            }

            _nightCycleCount = Mathf.Max(0, nightCycleCount);
            RebuildCurvesFromConfig();
            float totalDuration = GetTotalCycleDuration(_nightCycleCount);
            _cycleElapsed = totalDuration > 0f
                ? Mathf.Clamp(cycleElapsed, 0f, totalDuration - 0.001f)
                : 0f;

            float remaining = _cycleElapsed;
            _currentPhase = DayNightPhase.Day;
            _phaseElapsed = 0f;

            DayNightPhase[] phases =
            {
                DayNightPhase.Day,
                DayNightPhase.Dusk,
                DayNightPhase.Night_Early,
                DayNightPhase.Night_Deep,
                DayNightPhase.Dawn
            };

            foreach (DayNightPhase phase in phases)
            {
                float phaseDuration = GetPhaseDuration(phase, _nightCycleCount);
                if (phaseDuration <= 0f)
                {
                    continue;
                }

                if (remaining < phaseDuration)
                {
                    _currentPhase = phase;
                    _phaseElapsed = remaining;
                    break;
                }

                remaining -= phaseDuration;
            }

            _onPhaseChanged?.Invoke(_currentPhase);
            ApplyEnvironmentSettings();
        }

        private void BeginAtDayMidpoint()
        {
            SetNormalizedTime(GetPhaseMidpointNormalized(DayNightPhase.Day));
        }

        private void BeginAtNightEarly()
        {
            SetNormalizedTime(GetPhaseMidpointNormalized(DayNightPhase.Night_Early));
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
                    _phaseElapsed = 0f;
                    RebuildCurvesFromConfig();
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
                        _config.NightDeepDuration,
                        _config.NightGrowthMultiplier);
                case DayNightPhase.Dawn:
                    return _config.DawnDuration;
                default:
                    return 0f;
            }
        }

        private void ApplyEnvironmentSettings()
        {
            UpdateLighting(NormalizedDayTime);
        }

        /// <summary>
        /// Snaps the cycle to a normalized time, syncs phase timers, and immediately updates lighting.
        /// </summary>
        /// <param name="normalizedTime">Target position from 0 to 1 across the full cycle.</param>
        public void SetNormalizedTime(float normalizedTime)
        {
            if (_config == null)
            {
                return;
            }

            normalizedTime = Mathf.Clamp01(normalizedTime);
            float totalDuration = GetTotalCycleDuration(_nightCycleCount);
            _cycleElapsed = totalDuration > 0f
                ? normalizedTime * totalDuration
                : 0f;

            float remaining = _cycleElapsed;
            _currentPhase = DayNightPhase.Day;
            _phaseElapsed = 0f;

            DayNightPhase[] phases =
            {
                DayNightPhase.Day,
                DayNightPhase.Dusk,
                DayNightPhase.Night_Early,
                DayNightPhase.Night_Deep,
                DayNightPhase.Dawn
            };

            foreach (DayNightPhase phase in phases)
            {
                float phaseDuration = GetPhaseDuration(phase, _nightCycleCount);
                if (phaseDuration <= 0f)
                {
                    continue;
                }

                if (remaining < phaseDuration)
                {
                    _currentPhase = phase;
                    _phaseElapsed = remaining;
                    break;
                }

                remaining -= phaseDuration;
            }

            _onPhaseChanged?.Invoke(_currentPhase);
            UpdateLighting(normalizedTime);
        }

        private void UpdateLighting(float normalizedTime)
        {
            float curveTime = Mathf.Clamp01(normalizedTime);

            if (_directionalLight != null)
            {
                _directionalLight.intensity = _lightIntensityCurve.Evaluate(curveTime);
                _directionalLight.color = _lightColorGradient.Evaluate(curveTime);
            }

            RenderSettings.ambientLight = ambientColorGradient.Evaluate(curveTime);
            RenderSettings.fog = true;
            RenderSettings.fogDensity = fogDensityCurve.Evaluate(curveTime);
            RenderSettings.fogColor = fogColorGradient.Evaluate(curveTime);

            if (skyboxMaterial != null && skyboxExposureCurve != null)
            {
                skyboxMaterial.SetFloat("_Exposure", skyboxExposureCurve.Evaluate(curveTime));
            }

            if (_directionalLight != null &&
                sunElevationCurve != null &&
                sunAzimuthCurve != null)
            {
                float elevation = sunElevationCurve.Evaluate(curveTime);
                float azimuth = Mathf.Repeat(sunAzimuthCurve.Evaluate(curveTime), 360f);
                _directionalLight.transform.rotation = Quaternion.Euler(elevation, azimuth, 0f);
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            timeScale = Mathf.Max(0f, timeScale);

            if (_config != null)
            {
                RebuildCurvesFromConfig(force: true);
            }
        }
#endif
    }
}

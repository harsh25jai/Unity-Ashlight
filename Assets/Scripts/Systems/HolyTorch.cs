using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Systems
{
    /// <summary>
    /// Manages torch fuel drain, dual-light output, low-fuel flicker, and torch lifecycle events.
    /// </summary>
    [DisallowMultipleComponent]
    public class HolyTorch : MonoBehaviour
    {
        private const float PassiveDrainInterval = 3f;
        private const float CombatDrainInterval = 1f;
        private const float LowFuelThreshold = 0.2f;
        private const float FlickerIntensityVariance = 0.3f;
        private const float FlickerWaitMin = 0.1f;
        private const float FlickerWaitMax = 0.3f;

        private const float PointMinIntensity = 0.2f;
        private const float PointMaxIntensity = 3f;
        private const float PointMinRange = 2f;
        private const float PointMaxRange = 8f;

        private const float SpotMinIntensity = 0f;
        private const float SpotMaxIntensity = 2f;
        private const float SpotMinRange = 3f;
        private const float SpotMaxRange = 10f;
        private const float SpotAngle = 45f;

        [SerializeField] private float maxFuel = 100f;
        [SerializeField] private Light pointLight;
        [SerializeField] private Light spotLight;
        [SerializeField] private float healthModifier = 1f;
        [SerializeField] private float fearMultiplier;

        [Header("Events")]
        [SerializeField] private UnityEvent _onTorchLit;
        [SerializeField] private UnityEvent _onTorchLow;
        [SerializeField] private UnityEvent _onTorchExtinguished;
        [SerializeField] private UnityEvent<float> _onFuelChanged;

        private float _currentFuel;
        private bool _combatMode;
        private bool _lowFuelEventFired;
        private bool _isExtinguished = true;
        private float _flickerIntensityOffset;
        private Coroutine _drainCoroutine;
        private Coroutine _flickerCoroutine;

        /// <summary>Gets the maximum torch fuel capacity.</summary>
        public float MaxFuel => maxFuel;

        /// <summary>Gets the current torch fuel amount.</summary>
        public float CurrentFuel => _currentFuel;

        /// <summary>Gets current fuel as a 0-1 percentage.</summary>
        public float FuelPercent => maxFuel > 0f ? Mathf.Clamp01(_currentFuel / maxFuel) : 0f;

        /// <summary>Gets the player health modifier applied to torch output.</summary>
        public float HealthModifier => healthModifier;

        /// <summary>Gets the effective point-light range after fuel and health modifiers.</summary>
        public float CurrentLightRange
        {
            get
            {
                if (!HasValidLights() || _currentFuel <= 0f)
                {
                    return 0f;
                }

                return PointMaxRange * FuelPercent * healthModifier;
            }
        }

        /// <summary>Gets whether the torch is in accelerated combat drain mode.</summary>
        public bool IsCombatMode => _combatMode;

        /// <summary>Invoked when the torch becomes lit.</summary>
        public UnityEvent OnTorchLit => _onTorchLit;

        /// <summary>Invoked once when fuel drops to or below 20%.</summary>
        public UnityEvent OnTorchLow => _onTorchLow;

        /// <summary>Invoked when fuel reaches zero.</summary>
        public UnityEvent OnTorchExtinguished => _onTorchExtinguished;

        /// <summary>Invoked when torch fuel amount changes with normalized fuel from 0 to 1.</summary>
        public UnityEvent<float> OnFuelChanged => _onFuelChanged;

        private void Awake()
        {
            if (pointLight == null || spotLight == null)
            {
                Light[] childLights = GetComponentsInChildren<Light>();
                foreach (Light childLight in childLights)
                {
                    if (pointLight == null && childLight.type == LightType.Point)
                    {
                        pointLight = childLight;
                    }
                    else if (spotLight == null && childLight.type == LightType.Spot)
                    {
                        spotLight = childLight;
                    }
                }
            }

            if (pointLight == null)
            {
                Debug.LogError($"{nameof(HolyTorch)} requires a Point {nameof(Light)} reference.", this);
                enabled = false;
                return;
            }

            if (spotLight == null)
            {
                Debug.LogError($"{nameof(HolyTorch)} requires a Spot {nameof(Light)} reference.", this);
                enabled = false;
                return;
            }

            if (_currentFuel <= 0f)
            {
                _currentFuel = maxFuel;
            }

            _isExtinguished = _currentFuel <= 0f;
            spotLight.spotAngle = SpotAngle;
            ApplyLightState();
        }

        private void Start()
        {
            if (!HasValidLights() || _currentFuel <= 0f)
            {
                return;
            }

            _onTorchLit?.Invoke();
            _isExtinguished = false;
        }

        private void OnEnable()
        {
            if (!HasValidLights())
            {
                return;
            }

            if (_currentFuel > 0f)
            {
                StartDrainRoutine();
                UpdateFlickerRoutine();
            }
        }

        private void OnDisable()
        {
            StopDrainRoutine();
            StopFlickerRoutine();
        }

        /// <summary>Sets player health influence on torch brightness and range.</summary>
        /// <param name="modifier">Health percentage from 0 to 1.</param>
        public void SetHealthModifier(float modifier)
        {
            healthModifier = Mathf.Clamp01(modifier);
            ApplyLightState();
        }

        /// <summary>Enables or disables accelerated combat fuel drain.</summary>
        /// <param name="enabled">True for combat drain rate.</param>
        public void SetCombatMode(bool enabled)
        {
            _combatMode = enabled;
        }

        /// <summary>Sets fear-driven flicker intensity multiplier from 0 to 1.</summary>
        /// <param name="fear">Normalized fear amount.</param>
        public void SetFearMultiplier(float fear)
        {
            fearMultiplier = Mathf.Clamp01(fear);
        }

        /// <summary>Adds fuel and re-lights the torch when previously extinguished.</summary>
        /// <param name="amount">Fuel amount to add.</param>
        public void Refuel(float amount)
        {
            if (amount <= 0f || !HasValidLights())
            {
                return;
            }

            bool wasExtinguished = _currentFuel <= 0f;
            _currentFuel = Mathf.Min(maxFuel, _currentFuel + amount);

            if (wasExtinguished && _currentFuel > 0f)
            {
                _isExtinguished = false;
                _lowFuelEventFired = false;
                _onTorchLit?.Invoke();
                StartDrainRoutine();
            }

            ApplyLightState();
            UpdateFlickerRoutine();
            NotifyFuelChanged();
        }

        /// <summary>Sets absolute fuel level and immediately updates point and spot light output.</summary>
        /// <param name="value">Fuel amount from 0 to max capacity.</param>
        public void SetFuel(float value)
        {
            _currentFuel = Mathf.Clamp(value, 0f, maxFuel);
            ApplyLightState();
        }

        /// <summary>Sets torch fuel to maximum, updates light output, and invokes the lit event.</summary>
        public void RefillFuel()
        {
            if (!HasValidLights())
            {
                return;
            }

            _currentFuel = maxFuel;
            ApplyLightState();
            _onTorchLit?.Invoke();
        }

        private bool HasValidLights()
        {
            return pointLight != null && spotLight != null;
        }

        private void StartDrainRoutine()
        {
            if (_drainCoroutine != null || _currentFuel <= 0f)
            {
                return;
            }

            _drainCoroutine = StartCoroutine(DrainRoutine());
        }

        private void StopDrainRoutine()
        {
            if (_drainCoroutine != null)
            {
                StopCoroutine(_drainCoroutine);
                _drainCoroutine = null;
            }
        }

        private IEnumerator DrainRoutine()
        {
            while (_currentFuel > 0f)
            {
                float interval = _combatMode ? CombatDrainInterval : PassiveDrainInterval;
                yield return new WaitForSeconds(interval);
                ConsumeFuel(1f);
            }

            _drainCoroutine = null;
        }

        private void ConsumeFuel(float amount)
        {
            if (amount <= 0f || _currentFuel <= 0f)
            {
                return;
            }

            float previousFuel = _currentFuel;
            _currentFuel = Mathf.Max(0f, _currentFuel - amount);

            if (!_lowFuelEventFired && previousFuel / maxFuel > LowFuelThreshold && FuelPercent <= LowFuelThreshold)
            {
                _lowFuelEventFired = true;
                _onTorchLow?.Invoke();
            }

            if (_currentFuel <= 0f && previousFuel > 0f)
            {
                ExtinguishTorch();
            }
            else
            {
                ApplyLightState();
                UpdateFlickerRoutine();
            }

            NotifyFuelChanged();
        }

        private void ExtinguishTorch()
        {
            if (_isExtinguished)
            {
                return;
            }

            _currentFuel = 0f;
            _isExtinguished = true;
            StopDrainRoutine();
            StopFlickerRoutine();
            _flickerIntensityOffset = 0f;
            ApplyLightState();
            _onTorchExtinguished?.Invoke();
            NotifyFuelChanged();
        }

        private void NotifyFuelChanged()
        {
            _onFuelChanged?.Invoke(FuelPercent);
        }

        private void ApplyLightState()
        {
            if (!HasValidLights())
            {
                return;
            }

            if (_currentFuel <= 0f)
            {
                pointLight.enabled = false;
                pointLight.range = PointMinRange;
                pointLight.intensity = 0f;

                spotLight.enabled = false;
                spotLight.range = SpotMinRange;
                spotLight.intensity = 0f;
                spotLight.spotAngle = SpotAngle;
                return;
            }

            float fuelPercent = FuelPercent;

            float pointEffectiveIntensity = PointMaxIntensity * fuelPercent * healthModifier;
            float pointEffectiveRange = PointMaxRange * fuelPercent * healthModifier;

            pointLight.enabled = true;
            pointLight.range = Mathf.Max(PointMinRange, pointEffectiveRange);
            pointLight.intensity = Mathf.Max(0f, pointEffectiveIntensity + _flickerIntensityOffset);

            float spotEffectiveIntensity = SpotMaxIntensity * fuelPercent * healthModifier;
            float spotEffectiveRange = SpotMaxRange * fuelPercent * healthModifier;

            spotLight.enabled = true;
            spotLight.range = Mathf.Max(SpotMinRange, spotEffectiveRange);
            spotLight.spotAngle = SpotAngle;
            spotLight.intensity = Mathf.Max(0f, spotEffectiveIntensity + _flickerIntensityOffset);
        }

        private void UpdateFlickerRoutine()
        {
            if (!HasValidLights() || _currentFuel <= 0f)
            {
                StopFlickerRoutine();
                return;
            }

            if (FuelPercent < LowFuelThreshold)
            {
                if (_flickerCoroutine == null)
                {
                    _flickerCoroutine = StartCoroutine(FlickerRoutine());
                }
            }
            else
            {
                StopFlickerRoutine();
                _flickerIntensityOffset = 0f;
                ApplyLightState();
            }
        }

        private void StopFlickerRoutine()
        {
            if (_flickerCoroutine != null)
            {
                StopCoroutine(_flickerCoroutine);
                _flickerCoroutine = null;
            }
        }

        private IEnumerator FlickerRoutine()
        {
            while (_currentFuel > 0f && FuelPercent < LowFuelThreshold)
            {
                float flickerRange = FlickerIntensityVariance + fearMultiplier * 0.5f;
                _flickerIntensityOffset = Random.Range(-flickerRange, flickerRange);
                ApplyLightState();

                float waitDuration = Random.Range(FlickerWaitMin, FlickerWaitMax);
                yield return new WaitForSeconds(waitDuration);
            }

            _flickerIntensityOffset = 0f;
            _flickerCoroutine = null;
            ApplyLightState();
        }
    }
}

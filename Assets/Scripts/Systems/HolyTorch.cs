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

        [Header("Events")]
        [SerializeField] private UnityEvent _onTorchLit;
        [SerializeField] private UnityEvent _onTorchLow;
        [SerializeField] private UnityEvent _onTorchExtinguished;

        private float _currentFuel;
        private bool _combatMode;
        private bool _lowFuelEventFired;
        private bool _isExtinguished = true;
        private float _flickerIntensityOffset;
        private Coroutine _drainCoroutine;
        private Coroutine _flickerCoroutine;

        /// <summary>Gets current fuel as a 0-1 percentage.</summary>
        public float FuelPercent => maxFuel > 0f ? Mathf.Clamp01(_currentFuel / maxFuel) : 0f;

        /// <summary>Gets whether the torch is in accelerated combat drain mode.</summary>
        public bool IsCombatMode => _combatMode;

        /// <summary>Invoked when the torch becomes lit.</summary>
        public UnityEvent OnTorchLit => _onTorchLit;

        /// <summary>Invoked once when fuel drops to or below 20%.</summary>
        public UnityEvent OnTorchLow => _onTorchLow;

        /// <summary>Invoked when fuel reaches zero.</summary>
        public UnityEvent OnTorchExtinguished => _onTorchExtinguished;

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

        /// <summary>Enables or disables accelerated combat fuel drain.</summary>
        /// <param name="enabled">True for combat drain rate.</param>
        public void SetCombatMode(bool enabled)
        {
            _combatMode = enabled;
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

            pointLight.enabled = true;
            pointLight.range = Mathf.Lerp(PointMinRange, PointMaxRange, fuelPercent);
            float pointBaseIntensity = Mathf.Lerp(PointMinIntensity, PointMaxIntensity, fuelPercent);
            pointLight.intensity = Mathf.Max(0f, pointBaseIntensity + _flickerIntensityOffset);

            spotLight.enabled = true;
            spotLight.range = Mathf.Lerp(SpotMinRange, SpotMaxRange, fuelPercent);
            spotLight.spotAngle = SpotAngle;
            float spotBaseIntensity = Mathf.Lerp(SpotMinIntensity, SpotMaxIntensity, fuelPercent);
            spotLight.intensity = Mathf.Max(0f, spotBaseIntensity + _flickerIntensityOffset);
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
                _flickerIntensityOffset = Random.Range(-FlickerIntensityVariance, FlickerIntensityVariance);
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

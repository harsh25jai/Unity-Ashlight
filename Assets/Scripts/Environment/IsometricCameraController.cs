using System.Collections;
using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Drives a fixed isometric Cinemachine camera that follows the player with damping,
    /// clamps to scene bounds, and supports fear-driven camera shake.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public class IsometricCameraController : MonoBehaviour
    {
        [Header("Follow Target")]
        [SerializeField] private Transform _followTarget;

        [Header("Isometric View")]
        [SerializeField] private float _pitchAngle = 45f;
        [SerializeField] private float _yawAngle = 45f;
        [SerializeField] private float _followDistance = 20f;

        [Header("Damping")]
        [SerializeField] private float _positionDamping = 1.5f;

        [Header("Scene Bounds")]
        [SerializeField] private bool _useBounds = true;
        [SerializeField] private Vector3 _boundsMin = new Vector3(-50f, 5f, -50f);
        [SerializeField] private Vector3 _boundsMax = new Vector3(50f, 80f, 50f);

        [Header("Camera Shake")]
        [SerializeField] private float _maxShakeOffset = 0.75f;

        private CinemachineCamera _cinemachineCamera;
        private CinemachineFollow _cinemachineFollow;
        private Quaternion _isometricRotation;
        private Vector3 _shakeOffset;
        private Vector3 _lastAppliedShakeOffset;
        private Coroutine _shakeCoroutine;

        /// <summary>Gets or sets the transform the camera follows.</summary>
        public Transform FollowTarget
        {
            get => _followTarget;
            set => SetFollowTarget(value);
        }

        /// <summary>Gets whether world-space bounds clamping is enabled.</summary>
        public bool UseBounds => _useBounds;

        private void Awake()
        {
            _cinemachineCamera = GetComponent<CinemachineCamera>();
            _cinemachineFollow = GetComponent<CinemachineFollow>();

            if (_cinemachineCamera == null)
            {
                Debug.LogError($"{nameof(IsometricCameraController)} requires a {nameof(CinemachineCamera)}.", this);
                enabled = false;
                return;
            }

            if (_cinemachineFollow == null)
            {
                Debug.LogError($"{nameof(IsometricCameraController)} requires a {nameof(CinemachineFollow)}.", this);
                enabled = false;
                return;
            }

            _isometricRotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
            ConfigureCinemachine();
        }

        private void Start()
        {
            if (_followTarget == null)
            {
                Debug.LogWarning($"{nameof(IsometricCameraController)} has no follow target assigned.", this);
                return;
            }

            _cinemachineCamera.Follow = _followTarget;
        }

        private void LateUpdate()
        {
            Vector3 cinemachinePosition = transform.position - _lastAppliedShakeOffset;
            Vector3 clampedPosition = _useBounds
                ? ClampPositionToBounds(cinemachinePosition, _boundsMin, _boundsMax)
                : cinemachinePosition;

            transform.rotation = _isometricRotation;
            transform.position = clampedPosition + _shakeOffset;
            _lastAppliedShakeOffset = _shakeOffset;
        }

        /// <summary>
        /// Applies a decaying positional shake to the camera for fear and impact feedback.
        /// </summary>
        /// <param name="intensity">Shake strength multiplier in the range [0, 1] or greater.</param>
        /// <param name="duration">Shake duration in seconds.</param>
        public void PublicCameraShake(float intensity, float duration)
        {
            if (!isActiveAndEnabled || intensity <= 0f || duration <= 0f)
            {
                return;
            }

            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
            }

            _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, duration));
        }

        /// <summary>Assigns the player or target transform for Cinemachine follow.</summary>
        /// <param name="target">World transform to track.</param>
        public void SetFollowTarget(Transform target)
        {
            _followTarget = target;

            if (_cinemachineCamera != null)
            {
                _cinemachineCamera.Follow = target;
            }
        }

        /// <summary>Defines axis-aligned world bounds used to clamp the camera position.</summary>
        /// <param name="min">Minimum world-space corner.</param>
        /// <param name="max">Maximum world-space corner.</param>
        public void SetBounds(Vector3 min, Vector3 max)
        {
            _boundsMin = Vector3.Min(min, max);
            _boundsMax = Vector3.Max(min, max);
            _useBounds = true;
        }

        /// <summary>Enables or disables world-space bounds clamping.</summary>
        /// <param name="enabled">When false, the camera is not clamped.</param>
        public void SetBoundsEnabled(bool enabled)
        {
            _useBounds = enabled;
        }

        /// <summary>
        /// Clamps a world position to an axis-aligned bounding box.
        /// </summary>
        /// <param name="position">Position to clamp.</param>
        /// <param name="boundsMin">Minimum corner of the bounds.</param>
        /// <param name="boundsMax">Maximum corner of the bounds.</param>
        /// <returns>The clamped position.</returns>
        public static Vector3 ClampPositionToBounds(Vector3 position, Vector3 boundsMin, Vector3 boundsMax)
        {
            float minX = Mathf.Min(boundsMin.x, boundsMax.x);
            float maxX = Mathf.Max(boundsMin.x, boundsMax.x);
            float minY = Mathf.Min(boundsMin.y, boundsMax.y);
            float maxY = Mathf.Max(boundsMin.y, boundsMax.y);
            float minZ = Mathf.Min(boundsMin.z, boundsMax.z);
            float maxZ = Mathf.Max(boundsMin.z, boundsMax.z);

            return new Vector3(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY),
                Mathf.Clamp(position.z, minZ, maxZ));
        }

        private void ConfigureCinemachine()
        {
            transform.rotation = _isometricRotation;

            _cinemachineCamera.Follow = _followTarget;
            _cinemachineFollow.FollowOffset = ComputeFollowOffset();

            TrackerSettings trackerSettings = _cinemachineFollow.TrackerSettings;
            trackerSettings.BindingMode = BindingMode.WorldSpace;
            trackerSettings.PositionDamping = Vector3.one * _positionDamping;
            _cinemachineFollow.TrackerSettings = trackerSettings;
        }

        private Vector3 ComputeFollowOffset()
        {
            return _isometricRotation * (Vector3.back * _followDistance);
        }

        private IEnumerator ShakeRoutine(float intensity, float duration)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float decay = 1f - (elapsed / duration);
                float magnitude = intensity * decay * _maxShakeOffset;
                _shakeOffset = Random.insideUnitSphere * magnitude;
                elapsed += Time.deltaTime;
                yield return null;
            }

            _shakeOffset = Vector3.zero;
            _shakeCoroutine = null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _boundsMin = Vector3.Min(_boundsMin, _boundsMax);
            _boundsMax = Vector3.Max(_boundsMin, _boundsMax);
            _followDistance = Mathf.Max(0.1f, _followDistance);
            _positionDamping = Mathf.Max(0f, _positionDamping);
            _maxShakeOffset = Mathf.Max(0f, _maxShakeOffset);

            if (!Application.isPlaying && _cinemachineFollow == null)
            {
                _cinemachineFollow = GetComponent<CinemachineFollow>();
            }

            if (!Application.isPlaying && _cinemachineFollow != null)
            {
                _isometricRotation = Quaternion.Euler(_pitchAngle, _yawAngle, 0f);
                _cinemachineFollow.FollowOffset = ComputeFollowOffset();
            }
        }
#endif
    }
}

using System.Collections;
using Ashlight.Environment;
using Ashlight.Ghost;
using Ashlight.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ashlight.Systems
{
    /// <summary>
    /// Tracks proximity-based fear and drives camera shake, vignette, and stamina drain.
    /// </summary>
    [DisallowMultipleComponent]
    public class FearSystem : MonoBehaviour
    {
        private const float GhostScanInterval = 0.5f;
        private const float FearLerpSpeed = 2f;
        private const float ZeroFearDistance = 15f;
        private const float MaxFearDistance = 1f;
        private const float CameraShakeFearThreshold = 0.6f;
        private const float VignetteBaseIntensity = 0.3f;
        private const float VignetteFearScale = 0.5f;
        private const float CameraShakeDuration = 0.35f;

        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private IsometricCameraController cameraController;
        [SerializeField] private Volume postProcessVolume;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onFearChanged;

        private float _currentFear;
        private float _targetFear;
        private Vignette _vignette;
        private Coroutine _ghostScanRoutine;
        private Coroutine _fearSmoothRoutine;

        /// <summary>Gets the smoothed fear level from 0 to 1.</summary>
        public float CurrentFear => _currentFear;

        /// <summary>Invoked every ghost scan interval with the current fear value.</summary>
        public UnityEvent<float> OnFearChanged => _onFearChanged;

        private void Awake()
        {
            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    player = playerObject.transform;
                }
            }

            if (playerController == null && player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }

            if (cameraController == null)
            {
                cameraController = FindAnyObjectByType<IsometricCameraController>();
            }

            if (postProcessVolume == null)
            {
                postProcessVolume = FindAnyObjectByType<Volume>();
            }

            if (postProcessVolume != null && postProcessVolume.profile != null)
            {
                postProcessVolume.profile.TryGet(out _vignette);
            }

            if (player == null)
            {
                Debug.LogWarning($"{nameof(FearSystem)} has no player {nameof(Transform)} assigned.", this);
            }

            if (_vignette == null)
            {
                Debug.LogWarning($"{nameof(FearSystem)} could not resolve a post-processing {nameof(Vignette)} override.", this);
            }
        }

        private void OnEnable()
        {
            if (_ghostScanRoutine == null)
            {
                _ghostScanRoutine = StartCoroutine(GhostScanRoutine());
            }

            if (_fearSmoothRoutine == null)
            {
                _fearSmoothRoutine = StartCoroutine(FearSmoothRoutine());
            }
        }

        private void OnDisable()
        {
            if (_ghostScanRoutine != null)
            {
                StopCoroutine(_ghostScanRoutine);
                _ghostScanRoutine = null;
            }

            if (_fearSmoothRoutine != null)
            {
                StopCoroutine(_fearSmoothRoutine);
                _fearSmoothRoutine = null;
            }

            ApplyVignetteIntensity(0f);
            playerController?.SetStaminaDrainMultiplier(1f);
        }

        /// <summary>
        /// Converts closest ghost distance into a normalized fear value.
        /// </summary>
        /// <param name="distance">Distance to the nearest active ghost.</param>
        /// <returns>Fear from 0 at 15f to 1 at 1f.</returns>
        public static float CalculateFearFromDistance(float distance)
        {
            if (distance >= ZeroFearDistance)
            {
                return 0f;
            }

            if (distance <= MaxFearDistance)
            {
                return 1f;
            }

            return 1f - Mathf.InverseLerp(MaxFearDistance, ZeroFearDistance, distance);
        }

        private IEnumerator GhostScanRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(GhostScanInterval);

            while (enabled)
            {
                _targetFear = EvaluateTargetFear();
                _onFearChanged?.Invoke(_currentFear);

                if (cameraController != null && _currentFear > CameraShakeFearThreshold)
                {
                    cameraController.CameraShake(_currentFear, CameraShakeDuration);
                }

                yield return wait;
            }
        }

        private IEnumerator FearSmoothRoutine()
        {
            while (enabled)
            {
                _currentFear = Mathf.Lerp(_currentFear, _targetFear, FearLerpSpeed * Time.deltaTime);
                ApplyFearEffects(_currentFear);
                yield return null;
            }
        }

        private float EvaluateTargetFear()
        {
            if (player == null)
            {
                return 0f;
            }

            GhostAIController[] ghosts = FindObjectsByType<GhostAIController>();
            float closestDistance = float.MaxValue;
            bool foundActiveGhost = false;

            foreach (GhostAIController ghost in ghosts)
            {
                if (ghost == null || !ghost.IsSpawnActive)
                {
                    continue;
                }

                foundActiveGhost = true;
                float distance = Vector3.Distance(player.position, ghost.transform.position);
                closestDistance = Mathf.Min(closestDistance, distance);
            }

            if (!foundActiveGhost)
            {
                return 0f;
            }

            return CalculateFearFromDistance(closestDistance);
        }

        private void ApplyFearEffects(float fear)
        {
            ApplyVignetteIntensity(fear);
            playerController?.SetStaminaDrainMultiplier(1f + fear);
        }

        private void ApplyVignetteIntensity(float fear)
        {
            if (_vignette == null)
            {
                return;
            }

            _vignette.intensity.Override(VignetteBaseIntensity + VignetteFearScale * fear);
        }
    }
}

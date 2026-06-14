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
    /// Tracks proximity-based fear and drives vignette, camera shake, torch flicker, and stamina drain.
    /// </summary>
    [DisallowMultipleComponent]
    public class FearSystem : MonoBehaviour
    {
        private const float GhostScanInterval = 0.5f;
        private const float FearLerpSpeed = 2f;
        private const float ZeroFearDistance = 15f;
        private const float MaxFearDistance = 1f;
        private const float CameraShakeFearThreshold = 0.6f;
        private const float CameraShakeMaxIntensity = 0.08f;
        private const float CameraShakeDuration = 0.1f;
        private const float CameraShakeCooldown = 0.15f;
        private static readonly Color VignetteLowFearColor = Color.black;
        private static readonly Color VignetteHighFearColor = new Color(0.3f, 0f, 0f);

        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private IsometricCameraController cameraController;
        [SerializeField] private Volume postProcessVolume;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private GhostSpawnManager ghostSpawnManager;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onFearChanged;

        private float _currentFear;
        private float _fearTarget;
        private float _cameraShakeCooldownTimer;
        private Coroutine _ghostScanRoutine;

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

            if (holyTorch == null && player != null)
            {
                holyTorch = player.GetComponentInChildren<HolyTorch>();
            }

            if (ghostSpawnManager == null)
            {
                ghostSpawnManager = FindAnyObjectByType<GhostSpawnManager>();
            }

            if (player == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires a player {nameof(Transform)}.", this);
            }

            if (playerController == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires a {nameof(PlayerController)}.", this);
            }

            if (cameraController == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires an {nameof(IsometricCameraController)}.", this);
            }

            if (postProcessVolume == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires a post-process {nameof(Volume)}.", this);
            }

            if (holyTorch == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires a {nameof(HolyTorch)}.", this);
            }

            if (ghostSpawnManager == null)
            {
                Debug.LogError($"{nameof(FearSystem)} requires a {nameof(GhostSpawnManager)}.", this);
            }
        }

        private void OnEnable()
        {
            if (_ghostScanRoutine == null)
            {
                _ghostScanRoutine = StartCoroutine(GhostScanRoutine());
            }
        }

        private void OnDisable()
        {
            if (_ghostScanRoutine != null)
            {
                StopCoroutine(_ghostScanRoutine);
                _ghostScanRoutine = null;
            }

            _currentFear = 0f;
            _fearTarget = 0f;
            _cameraShakeCooldownTimer = 0f;
            ApplyVignette(_currentFear);
            holyTorch?.SetFearMultiplier(0f);
            playerController?.SetFearStaminaMultiplier(1f);
        }

        private void Update()
        {
            _currentFear = Mathf.Lerp(_currentFear, _fearTarget, FearLerpSpeed * Time.deltaTime);
            ApplyFearEffects(_currentFear);
            UpdateCameraShake(_currentFear);
        }

        /// <summary>
        /// Converts closest ghost distance into a normalized fear value.
        /// </summary>
        /// <param name="distance">Distance to the nearest active ghost.</param>
        /// <returns>Fear from 0 at 15 units to 1 at 1 unit.</returns>
        public static float CalculateFearFromDistance(float distance)
        {
            return Mathf.Clamp01(Mathf.InverseLerp(ZeroFearDistance, MaxFearDistance, distance));
        }

        private IEnumerator GhostScanRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(GhostScanInterval);

            while (enabled)
            {
                _fearTarget = EvaluateTargetFear();
                _onFearChanged?.Invoke(_currentFear);
                yield return wait;
            }
        }

        private float EvaluateTargetFear()
        {
            if (player == null || ghostSpawnManager == null)
            {
                return 0f;
            }

            System.Collections.Generic.List<GhostAIController> activeGhosts = ghostSpawnManager.ActiveGhosts;
            if (activeGhosts == null || activeGhosts.Count == 0)
            {
                return 0f;
            }

            float closestDistance = float.MaxValue;

            foreach (GhostAIController ghost in activeGhosts)
            {
                if (ghost == null || !ghost.gameObject.activeSelf)
                {
                    continue;
                }

                float distance = Vector3.Distance(player.position, ghost.transform.position);
                closestDistance = Mathf.Min(closestDistance, distance);
            }

            if (closestDistance == float.MaxValue)
            {
                return 0f;
            }

            return CalculateFearFromDistance(closestDistance);
        }

        private void ApplyFearEffects(float fear)
        {
            ApplyVignette(fear);
            holyTorch?.SetFearMultiplier(fear);
            playerController?.SetFearStaminaMultiplier(Mathf.Lerp(1f, 2f, fear));
        }

        private void ApplyVignette(float fear)
        {
            if (postProcessVolume == null || postProcessVolume.profile == null)
            {
                return;
            }

            try
            {
                if (!postProcessVolume.profile.TryGet(out Vignette vignette))
                {
                    return;
                }

                vignette.intensity.value = Mathf.Lerp(0.2f, 0.65f, fear);
                vignette.color.value = Color.Lerp(VignetteLowFearColor, VignetteHighFearColor, fear);
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"{nameof(FearSystem)} could not update vignette: {exception.Message}", this);
            }
        }

        private void UpdateCameraShake(float fear)
        {
            if (_cameraShakeCooldownTimer > 0f)
            {
                _cameraShakeCooldownTimer -= Time.deltaTime;
            }

            if (cameraController == null || fear <= CameraShakeFearThreshold || _cameraShakeCooldownTimer > 0f)
            {
                return;
            }

            float normalizedHighFear = (fear - CameraShakeFearThreshold) / (1f - CameraShakeFearThreshold);
            float shakeIntensity = Mathf.Lerp(0f, CameraShakeMaxIntensity, normalizedHighFear);
            cameraController.CameraShake(shakeIntensity, CameraShakeDuration);
            _cameraShakeCooldownTimer = CameraShakeCooldown;
        }
    }
}

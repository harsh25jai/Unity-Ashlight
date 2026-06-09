using Ashlight.Systems;
using UnityEngine;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Player awareness level from a ghost's perspective.
    /// </summary>
    public enum PerceptionLevel
    {
        Unaware,
        Suspicious,
        Detected
    }

    /// <summary>
    /// Evaluates ghost perception using distance, facing arc, and torch light penalties.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostPerceptionSystem : MonoBehaviour
    {
        private const float ForwardArcHalfAngle = 60f;
        private const float HighTorchFuelThreshold = 0.5f;
        private const float DetectedDistanceRatio = 0.4f;
        private const float SuspiciousDistanceRatio = 0.75f;

        [SerializeField] private Transform _player;
        [SerializeField] private HolyTorch _playerTorch;

        /// <summary>Assigns the player transform at runtime.</summary>
        /// <param name="playerTransform">Player transform to track.</param>
        public void SetPlayer(Transform playerTransform)
        {
            _player = playerTransform;

            if (_player != null && _playerTorch == null)
            {
                _playerTorch = _player.GetComponentInChildren<HolyTorch>();
            }
        }

        /// <summary>Assigns the player's holy torch at runtime.</summary>
        /// <param name="torch">Player torch component.</param>
        public void SetTorch(HolyTorch torch)
        {
            _playerTorch = torch;
        }

        /// <summary>
        /// Calculates perception level for a ghost relative to the player.
        /// </summary>
        /// <param name="ghost">Ghost transform.</param>
        /// <param name="player">Player transform.</param>
        /// <param name="torchFuelPercent">Player torch fuel from 0 to 1.</param>
        /// <returns>The resulting perception level.</returns>
        public PerceptionLevel CalculatePerception(Transform ghost, Transform player, float torchFuelPercent)
        {
            if (ghost == null || player == null)
            {
                return PerceptionLevel.Unaware;
            }

            GhostTypeDefinition ghostType = ghost.GetComponent<GhostAIController>()?.GhostType;
            if (ghostType == null)
            {
                return PerceptionLevel.Unaware;
            }

            return EvaluatePerception(
                ghost.position,
                ghost.forward,
                player.position,
                ghostType.DetectionRange,
                ghostType.LightResistance,
                torchFuelPercent);
        }

        /// <summary>
        /// Returns combined torch light influence at a world position.
        /// </summary>
        /// <param name="position">World position to sample.</param>
        /// <returns>Light level from 0 to 1.</returns>
        public float GetLightLevelAtPosition(Vector3 position)
        {
            if (_playerTorch == null || _player == null)
            {
                return 0f;
            }

            if (_playerTorch.FuelPercent <= 0f)
            {
                return 0f;
            }

            float fuelPercent = _playerTorch.FuelPercent;
            float distance = Vector3.Distance(position, _player.position);

            float pointRange = Mathf.Lerp(2f, 8f, fuelPercent);
            float pointContribution = 1f - Mathf.Clamp01(distance / Mathf.Max(0.01f, pointRange));

            Light[] torchLights = _playerTorch.GetComponentsInChildren<Light>();
            float spotContribution = 0f;

            foreach (Light torchLight in torchLights)
            {
                if (torchLight == null || !torchLight.enabled || torchLight.type != LightType.Spot)
                {
                    continue;
                }

                Vector3 toPosition = position - torchLight.transform.position;
                float spotRange = Mathf.Lerp(3f, 10f, fuelPercent);
                float distanceToSpot = toPosition.magnitude;
                if (distanceToSpot > spotRange)
                {
                    continue;
                }

                Vector3 spotForward = torchLight.transform.forward;
                float angle = Vector3.Angle(spotForward, toPosition.normalized);
                if (angle > torchLight.spotAngle * 0.5f)
                {
                    continue;
                }

                float rangeFalloff = 1f - (distanceToSpot / spotRange);
                float angleFalloff = 1f - (angle / (torchLight.spotAngle * 0.5f));
                spotContribution = Mathf.Max(spotContribution, rangeFalloff * angleFalloff);
            }

            return Mathf.Clamp01(Mathf.Max(pointContribution, spotContribution) * fuelPercent);
        }

        /// <summary>
        /// Evaluates perception using world positions and stat inputs.
        /// </summary>
        public static PerceptionLevel EvaluatePerception(
            Vector3 ghostPosition,
            Vector3 ghostForward,
            Vector3 playerPosition,
            float detectionRange,
            float lightResistance,
            float torchFuelPercent)
        {
            if (detectionRange <= 0f)
            {
                return PerceptionLevel.Unaware;
            }

            float distance = Vector3.Distance(ghostPosition, playerPosition);
            float effectiveRange = detectionRange;

            if (torchFuelPercent > HighTorchFuelThreshold)
            {
                effectiveRange *= 1f - lightResistance;
            }

            effectiveRange = Mathf.Max(0.1f, effectiveRange);
            if (distance > effectiveRange)
            {
                return PerceptionLevel.Unaware;
            }

            Vector3 toPlayer = playerPosition - ghostPosition;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude <= 0.001f)
            {
                return PerceptionLevel.Detected;
            }

            Vector3 forward = ghostForward;
            forward.y = 0f;
            float angle = Vector3.Angle(forward.normalized, toPlayer.normalized);
            if (angle > ForwardArcHalfAngle)
            {
                return PerceptionLevel.Unaware;
            }

            float distanceRatio = distance / effectiveRange;
            if (distanceRatio <= DetectedDistanceRatio)
            {
                return PerceptionLevel.Detected;
            }

            if (distanceRatio <= SuspiciousDistanceRatio)
            {
                return PerceptionLevel.Suspicious;
            }

            return PerceptionLevel.Unaware;
        }

        private void Awake()
        {
            if (_player != null && _playerTorch == null)
            {
                _playerTorch = _player.GetComponentInChildren<HolyTorch>();
            }
        }
    }
}

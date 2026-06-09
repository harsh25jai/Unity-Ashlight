using System.Collections;
using Ashlight.Systems;
using UnityEngine;

namespace Ashlight.Ghost
{
    /// <summary>
    /// Applies holy torch damage to a ghost when it is within torch light range.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostTorchInteraction : MonoBehaviour
    {
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private GhostAIController ghostAI;
        [SerializeField] private float damageCheckInterval = 0.2f;
        [SerializeField] private float maxTorchDamageRange = 8f;

        private Coroutine _damageRoutine;
        private bool _isInTorchRange;

        /// <summary>Gets whether the ghost is currently within effective torch range.</summary>
        public bool IsInTorchRange => _isInTorchRange;

        private void Awake()
        {
            if (ghostAI == null)
            {
                ghostAI = GetComponent<GhostAIController>();
            }

            if (ghostAI == null)
            {
                Debug.LogError($"{nameof(GhostTorchInteraction)} requires a {nameof(GhostAIController)}.", this);
                enabled = false;
            }
        }

        /// <summary>Assigns the player's holy torch at runtime.</summary>
        /// <param name="torch">Player torch component.</param>
        public void SetTorch(HolyTorch torch)
        {
            holyTorch = torch;

            if (!isActiveAndEnabled || holyTorch == null)
            {
                return;
            }

            if (_damageRoutine == null)
            {
                _damageRoutine = StartCoroutine(DamageCheckRoutine());
            }
        }

        private void OnEnable()
        {
            if (holyTorch == null || ghostAI == null)
            {
                return;
            }

            if (_damageRoutine == null)
            {
                _damageRoutine = StartCoroutine(DamageCheckRoutine());
            }
        }

        private void OnDisable()
        {
            if (_damageRoutine != null)
            {
                StopCoroutine(_damageRoutine);
                _damageRoutine = null;
            }

            _isInTorchRange = false;
        }

        private IEnumerator DamageCheckRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(damageCheckInterval);

            while (enabled)
            {
                ApplyTorchDamageIfInRange();
                yield return wait;
            }
        }

        private void ApplyTorchDamageIfInRange()
        {
            if (holyTorch == null || ghostAI == null || !ghostAI.IsSpawnActive)
            {
                _isInTorchRange = false;
                return;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, holyTorch.transform.position);
            float torchRange = holyTorch.CurrentLightRange;
            _isInTorchRange = torchRange > 0f && distanceToPlayer <= torchRange;

            if (!_isInTorchRange)
            {
                return;
            }

            float falloffDenominator = Mathf.Max(0.01f, maxTorchDamageRange);
            float damageFalloff = 1f - (distanceToPlayer / falloffDenominator);
            damageFalloff = Mathf.Clamp01(damageFalloff);

            float damage = ghostAI.TorchDamagePerSecond
                * damageFalloff
                * holyTorch.FuelPercent
                * damageCheckInterval;

            ghostAI.TakeTorchDamage(damage);
        }
    }
}

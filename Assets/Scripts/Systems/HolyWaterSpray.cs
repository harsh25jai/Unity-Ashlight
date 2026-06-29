using Ashlight.Ghost;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Ashlight.Systems
{
    /// <summary>
    /// Handles Holy Water bottle activation, spray budget, aiming, and ghost contact damage.
    /// </summary>
    [DisallowMultipleComponent]
    public class HolyWaterSpray : MonoBehaviour
    {
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private Transform sprayOrigin;
        [SerializeField] private ParticleSystem sprayEffect;
        [SerializeField] private AudioSource spraySoundSource;
        [SerializeField] private AudioClip sprayLoopSound;
        [SerializeField] private AudioClip emptySound;
        [SerializeField] private float totalSprayBudget = 2.5f;
        [SerializeField] private float sprayRadius = 1.5f;
        [SerializeField] private float sprayRange = 4f;
        [SerializeField] private float sprayConeAngle = 35f;
        [SerializeField] private float damagePerSecondContact = 25f;
        [SerializeField] private LayerMask ghostLayerMask;
        [SerializeField] private Transform grabberAutoLockTarget;

        private InputAction _activateHolyWaterAction;
        private bool _isBottleActive;
        private float _remainingSprayBudget;
        private bool _isSprayingNow;

        /// <summary>Gets whether a Holy Water bottle is currently active.</summary>
        public bool IsBottleActive => _isBottleActive;

        /// <summary>Gets remaining spray budget as a 0-1 percentage.</summary>
        public float RemainingBudgetPercent => totalSprayBudget > 0f ? _remainingSprayBudget / totalSprayBudget : 0f;

        private void Awake()
        {
            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(HolyWaterSpray)} requires a {nameof(HolyWaterInventory)} reference.", this);
                enabled = false;
                return;
            }

            if (inputActions == null)
            {
                Debug.LogError($"{nameof(HolyWaterSpray)} requires an {nameof(InputActionAsset)} reference.", this);
                enabled = false;
                return;
            }

            InputActionMap playerMap = inputActions.FindActionMap("Player", throwIfNotFound: true);
            _activateHolyWaterAction = playerMap.FindAction("ActivateHolyWater", throwIfNotFound: true);

            if (sprayOrigin == null)
            {
                Debug.LogError($"{nameof(HolyWaterSpray)} requires a {nameof(sprayOrigin)} transform.", this);
                enabled = false;
                return;
            }

            if (spraySoundSource == null)
            {
                spraySoundSource = GetComponent<AudioSource>();
            }
        }

        private void OnEnable()
        {
            if (_activateHolyWaterAction == null)
            {
                return;
            }

            _activateHolyWaterAction.performed += OnActivateHolyWaterPerformed;
            _activateHolyWaterAction.canceled += OnActivateHolyWaterCanceled;
        }

        private void OnDisable()
        {
            if (_activateHolyWaterAction == null)
            {
                return;
            }

            _activateHolyWaterAction.performed -= OnActivateHolyWaterPerformed;
            _activateHolyWaterAction.canceled -= OnActivateHolyWaterCanceled;
            StopSpraying();
        }

        private void Update()
        {
            if (!_isBottleActive)
            {
                UpdateSprayOrientation();
                return;
            }

            if (_isSprayingNow)
            {
                _remainingSprayBudget -= Time.deltaTime;

                if (_remainingSprayBudget <= 0f)
                {
                    _remainingSprayBudget = 0f;
                    DeactivateBottle();
                    return;
                }

                DetectAndDamageGhosts(Time.deltaTime);
            }

            UpdateSprayOrientation();
        }

        /// <summary>
        /// Forces spray aim toward a Grabber ghost while a grab is active.
        /// </summary>
        /// <param name="grabberTransform">Grabber transform to aim at.</param>
        public void SetGrabberLock(Transform grabberTransform)
        {
            grabberAutoLockTarget = grabberTransform;
        }

        /// <summary>Clears Grabber auto-aim override.</summary>
        public void ClearGrabberLock()
        {
            grabberAutoLockTarget = null;
        }

        private void OnActivateHolyWaterPerformed(InputAction.CallbackContext context)
        {
            if (!_isBottleActive)
            {
                if (!holyWaterInventory.ConsumeBottle())
                {
                    PlayEmptySound();
                    return;
                }

                _isBottleActive = true;
                _remainingSprayBudget = totalSprayBudget;
            }

            BeginSpraying();
        }

        private void OnActivateHolyWaterCanceled(InputAction.CallbackContext context)
        {
            StopSpraying();
        }

        private void BeginSpraying()
        {
            _isSprayingNow = true;

            if (sprayEffect != null && !sprayEffect.isPlaying)
            {
                sprayEffect.Play();
            }

            if (spraySoundSource != null && sprayLoopSound != null)
            {
                spraySoundSource.clip = sprayLoopSound;
                spraySoundSource.loop = true;
                spraySoundSource.Play();
            }
        }

        private void StopSpraying()
        {
            _isSprayingNow = false;

            if (sprayEffect != null)
            {
                sprayEffect.Stop();
            }

            if (spraySoundSource != null)
            {
                spraySoundSource.Stop();
            }
        }

        private void DeactivateBottle()
        {
            _isBottleActive = false;
            StopSpraying();
        }

        private void PlayEmptySound()
        {
            if (emptySound == null || spraySoundSource == null)
            {
                return;
            }

            spraySoundSource.PlayOneShot(emptySound);
        }

        private void UpdateSprayOrientation()
        {
            if (sprayOrigin == null)
            {
                return;
            }

            if (grabberAutoLockTarget != null)
            {
                Vector3 directionToGrabber = grabberAutoLockTarget.position - sprayOrigin.position;
                if (directionToGrabber.sqrMagnitude > 0.001f)
                {
                    sprayOrigin.rotation = Quaternion.LookRotation(directionToGrabber.normalized, Vector3.up);
                }

                return;
            }

            sprayOrigin.rotation = transform.rotation;
        }

        private void DetectAndDamageGhosts(float deltaTime)
        {
            if (sprayOrigin == null || deltaTime <= 0f)
            {
                return;
            }

            Collider[] hits = Physics.OverlapSphere(
                sprayOrigin.position,
                sprayRange,
                ghostLayerMask,
                QueryTriggerInteraction.Collide);

            foreach (Collider hit in hits)
            {
                if (hit == null)
                {
                    continue;
                }

                Vector3 directionToTarget = hit.transform.position - sprayOrigin.position;
                if (directionToTarget.sqrMagnitude <= 0.001f)
                {
                    continue;
                }

                directionToTarget.Normalize();
                float angle = Vector3.Angle(sprayOrigin.forward, directionToTarget);

                if (angle > sprayConeAngle)
                {
                    continue;
                }

                GhostAIController ghost = hit.GetComponent<GhostAIController>() ?? hit.GetComponentInParent<GhostAIController>();
                if (ghost == null)
                {
                    continue;
                }

                float damage = damagePerSecondContact * deltaTime;
                ghost.TakeHolyWaterDamage(damage);
            }
        }
    }
}

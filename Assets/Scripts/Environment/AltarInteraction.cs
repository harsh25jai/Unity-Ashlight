using System.Collections;
using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Ashlight.Environment
{
    /// <summary>
    /// Altar refill type offered at an interaction point.
    /// </summary>
    public enum AltarType
    {
        TorchRefill,
        WaterRefill
    }

    /// <summary>
    /// Proximity altar interaction with optional auto-refill and manual E-key use.
    /// </summary>
    [DisallowMultipleComponent]
    public class AltarInteraction : MonoBehaviour
    {
        private const float BlessingCooldown = 60f;
        private const float FaithRestoreOnBlessing = 15f;
        private const string PromptText = "Press E to receive blessing";

        [SerializeField] private AltarType altarType = AltarType.TorchRefill;
        [SerializeField] private float interactRange = 2f;
        [SerializeField] private float refillDuration = 2f;
        [SerializeField] private bool autoRefill = false;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private HolyWaterInventory inventoryAsset;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private PlayerFaith playerFaith;
        [SerializeField] private ParticleSystem blessingParticles;
        [SerializeField] private UIDocument promptDocument;

        [Header("Events")]
        [SerializeField] private UnityEvent _onBlessingReceived;

        private Label _promptLabel;
        private InputAction _interactAction;
        private bool _isOnCooldown;
        private float _cooldownEndTime;
        private bool _wasInRange;
        private Coroutine _cooldownRoutine;
        private Coroutine _refillRoutine;

        /// <summary>Invoked when the player receives an altar blessing.</summary>
        public UnityEvent OnBlessingReceived => _onBlessingReceived;

        /// <summary>Gets whether the altar cooldown has elapsed.</summary>
        public bool IsBlessingReady => !_isOnCooldown;

        /// <summary>Gets remaining cooldown seconds.</summary>
        public float CooldownRemaining => _isOnCooldown ? Mathf.Max(0f, _cooldownEndTime - Time.time) : 0f;

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject == null)
            {
                CharacterController characterController = FindAnyObjectByType<CharacterController>();
                if (characterController != null)
                {
                    playerObject = characterController.gameObject;
                }
            }

            if (playerObject != null)
            {
                player = playerObject.transform;
                EnsurePlayerTriggerPhysics(playerObject);
            }

            if (playerController == null && player != null)
            {
                playerController = player.GetComponent<PlayerController>();
            }

            if (playerFaith == null && player != null)
            {
                playerFaith = player.GetComponent<PlayerFaith>();
            }

            if (playerController != null)
            {
                _interactAction = playerController.InteractAction;
            }

            if (altarType == AltarType.TorchRefill && holyTorch == null && player != null)
            {
                holyTorch = player.GetComponentInChildren<HolyTorch>();
            }

            if (altarType == AltarType.TorchRefill && holyTorch == null)
            {
                Debug.LogWarning($"{nameof(AltarInteraction)} torch altar has no {nameof(HolyTorch)} assigned.", this);
            }

            if (altarType == AltarType.WaterRefill && inventoryAsset == null)
            {
                Debug.LogWarning($"{nameof(AltarInteraction)} water altar has no {nameof(HolyWaterInventory)} assigned.", this);
            }

            if (player == null)
            {
                Debug.LogWarning($"{nameof(AltarInteraction)} could not resolve the player transform.", this);
            }

            BuildPromptUI();
        }

        private void OnDisable()
        {
            if (_promptLabel != null)
            {
                _promptLabel.style.display = DisplayStyle.None;
            }

            if (_cooldownRoutine != null)
            {
                StopCoroutine(_cooldownRoutine);
                _cooldownRoutine = null;
            }

            StopRefillRoutine();
        }

        private void Update()
        {
            bool inRange = IsPlayerInRange();
            UpdatePromptVisibility(inRange);

            if (inRange && !_wasInRange && autoRefill)
            {
                TryInteract();
            }

            if (inRange && IsBlessingReady && _interactAction != null && _interactAction.WasPressedThisFrame())
            {
                TryInteract();
            }

            _wasInRange = inRange;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!autoRefill || !other.CompareTag("Player"))
            {
                return;
            }

            // Debug.Log($"{nameof(AltarInteraction)}: Player entered altar trigger on {name}.");
            TryInteract();
        }

        /// <summary>Attempts to apply the altar refill when in range and off cooldown.</summary>
        /// <returns>True when the refill was applied.</returns>
        public bool TryInteract()
        {
            if (!IsPlayerInRange() || !IsBlessingReady)
            {
                return false;
            }

            return ApplyAltarRefill();
        }

        /// <summary>Attempts to grant a blessing when the player is in range and off cooldown.</summary>
        /// <returns>True when the blessing was applied.</returns>
        public bool TryReceiveBlessing()
        {
            return TryInteract();
        }

        private bool ApplyAltarRefill()
        {
            switch (altarType)
            {
                case AltarType.TorchRefill:
                    if (holyTorch == null)
                    {
                        Debug.LogWarning($"{nameof(AltarInteraction)}: Torch refill failed — no {nameof(HolyTorch)}.", this);
                        return false;
                    }

                    RefillTorch();
                    // Debug.Log($"{nameof(AltarInteraction)}: Torch fuel refilling at {name}.");
                    break;

                case AltarType.WaterRefill:
                    if (inventoryAsset == null)
                    {
                        Debug.LogWarning($"{nameof(AltarInteraction)}: Water refill failed — no inventory asset.", this);
                        return false;
                    }

                    RefillHolyWater();
                    // Debug.Log($"{nameof(AltarInteraction)}: Holy Water refilling at {name}.");
                    break;
            }

            if (blessingParticles != null)
            {
                blessingParticles.Play();
            }

            playerFaith?.RestoreFaith(FaithRestoreOnBlessing);

            BeginCooldown();
            _onBlessingReceived?.Invoke();
            return true;
        }

        /// <summary>Starts a smooth torch fuel refill toward maximum capacity.</summary>
        private void RefillTorch()
        {
            StopRefillRoutine();
            _refillRoutine = StartCoroutine(RefillTorchRoutine());
        }

        /// <summary>Starts a smooth Holy Water refill toward maximum capacity.</summary>
        private void RefillHolyWater()
        {
            StopRefillRoutine();
            _refillRoutine = StartCoroutine(RefillHolyWaterRoutine());
        }

        private void StopRefillRoutine()
        {
            if (_refillRoutine != null)
            {
                StopCoroutine(_refillRoutine);
                _refillRoutine = null;
            }
        }

        private IEnumerator RefillTorchRoutine()
        {
            if (holyTorch == null)
            {
                yield break;
            }

            float duration = Mathf.Max(0.01f, refillDuration);
            float targetFuel = holyTorch.MaxFuel;
            float startFuel = holyTorch.CurrentFuel;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / duration);
                float nextFuel = Mathf.Lerp(startFuel, targetFuel, normalizedTime);
                holyTorch.SetFuel(nextFuel);
                yield return null;
            }

            holyTorch.SetFuel(targetFuel);

            _refillRoutine = null;
            // Debug.Log($"{nameof(AltarInteraction)}: Torch fuel refill complete at {name}.");
        }

        private IEnumerator RefillHolyWaterRoutine()
        {
            if (inventoryAsset == null)
            {
                yield break;
            }

            while (inventoryAsset != null && inventoryAsset.AddBottle())
            {
                yield return new WaitForSeconds(refillDuration / Mathf.Max(1, inventoryAsset.MaxBottles));
            }

            _refillRoutine = null;
        }

        private void BeginCooldown()
        {
            if (_cooldownRoutine != null)
            {
                StopCoroutine(_cooldownRoutine);
            }

            _cooldownRoutine = StartCoroutine(CooldownRoutine());
        }

        private IEnumerator CooldownRoutine()
        {
            _isOnCooldown = true;
            _cooldownEndTime = Time.time + BlessingCooldown;
            // Debug.Log($"{nameof(AltarInteraction)}: Altar {name} entered cooldown for {BlessingCooldown} seconds.");

            yield return new WaitForSeconds(BlessingCooldown);

            _isOnCooldown = false;
            _cooldownEndTime = 0f;
            _cooldownRoutine = null;
            // Debug.Log($"{nameof(AltarInteraction)}: Altar {name} is ready again.");
        }

        private void EnsurePlayerTriggerPhysics(GameObject playerObject)
        {
            if (playerObject == null)
            {
                return;
            }

            Rigidbody playerRigidbody = playerObject.GetComponent<Rigidbody>();
            if (playerRigidbody != null)
            {
                return;
            }

            playerRigidbody = playerObject.AddComponent<Rigidbody>();
            playerRigidbody.isKinematic = true;
            playerRigidbody.useGravity = false;
            // Debug.Log($"{nameof(AltarInteraction)} added kinematic {nameof(Rigidbody)} to Player for trigger detection.");
        }

        private bool IsPlayerInRange()
        {
            if (player == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= interactRange;
        }

        private void UpdatePromptVisibility(bool inRange)
        {
            if (_promptLabel == null)
            {
                return;
            }

            bool showPrompt = inRange && IsBlessingReady;
            _promptLabel.style.display = showPrompt ? DisplayStyle.Flex : DisplayStyle.None;
            _promptLabel.text = PromptText;
        }

        private void BuildPromptUI()
        {
            if (promptDocument == null)
            {
                promptDocument = GetComponent<UIDocument>();
            }

            if (promptDocument == null)
            {
                promptDocument = gameObject.AddComponent<UIDocument>();
            }

            VisualElement root = promptDocument.rootVisualElement;
            root.Clear();

            _promptLabel = new Label(PromptText) { name = "altar-prompt" };
            _promptLabel.style.position = Position.Absolute;
            _promptLabel.style.bottom = 120;
            _promptLabel.style.left = 0;
            _promptLabel.style.right = 0;
            _promptLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _promptLabel.style.fontSize = 18;
            _promptLabel.style.color = new Color(0.95f, 0.9f, 0.75f, 1f);
            _promptLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _promptLabel.style.display = DisplayStyle.None;
            root.Add(_promptLabel);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.35f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, interactRange);
        }
    }
}

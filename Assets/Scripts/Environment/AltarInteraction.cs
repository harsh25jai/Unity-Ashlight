using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Ashlight.Environment
{
    /// <summary>
    /// Altar blessing interaction that refills Holy Water and torch fuel on a cooldown.
    /// </summary>
    [DisallowMultipleComponent]
    public class AltarInteraction : MonoBehaviour
    {
        private const float InteractionRange = 2f;
        private const float BlessingCooldown = 60f;
        private const string PromptText = "Press E to receive blessing";

        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private Transform player;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private ParticleSystem blessingParticles;
        [SerializeField] private UIDocument promptDocument;

        [Header("Events")]
        [SerializeField] private UnityEvent _onBlessingReceived;

        private Label _promptLabel;
        private float _nextBlessingTime;
        private InputAction _interactAction;

        /// <summary>Invoked when the player receives an altar blessing.</summary>
        public UnityEvent OnBlessingReceived => _onBlessingReceived;

        /// <summary>Gets whether the blessing cooldown has elapsed.</summary>
        public bool IsBlessingReady => Time.time >= _nextBlessingTime;

        /// <summary>Gets remaining cooldown seconds.</summary>
        public float CooldownRemaining => Mathf.Max(0f, _nextBlessingTime - Time.time);

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

            if (playerController != null)
            {
                _interactAction = playerController.InteractAction;
            }

            if (holyWaterInventory == null)
            {
                Debug.LogWarning($"{nameof(AltarInteraction)} has no {nameof(HolyWaterInventory)} assigned.", this);
            }

            if (holyTorch == null && player != null)
            {
                holyTorch = player.GetComponentInChildren<HolyTorch>();
            }

            if (holyTorch == null)
            {
                Debug.LogWarning($"{nameof(AltarInteraction)} has no {nameof(HolyTorch)} assigned.", this);
            }

            BuildPromptUI();
        }

        private void OnDisable()
        {
            if (_promptLabel != null)
            {
                _promptLabel.style.display = DisplayStyle.None;
            }
        }

        private void Update()
        {
            UpdatePromptVisibility();

            if (!IsPlayerInRange() || !IsBlessingReady)
            {
                return;
            }

            if (_interactAction != null && _interactAction.WasPressedThisFrame())
            {
                TryReceiveBlessing();
            }
        }

        /// <summary>Attempts to grant a blessing when the player is in range and off cooldown.</summary>
        /// <returns>True when the blessing was applied.</returns>
        public bool TryReceiveBlessing()
        {
            if (!IsPlayerInRange() || !IsBlessingReady)
            {
                return false;
            }

            if (holyWaterInventory != null)
            {
                holyWaterInventory.ResetToFull();
            }

            if (holyTorch != null)
            {
                holyTorch.Refuel(holyTorch.MaxFuel);
            }

            if (blessingParticles != null)
            {
                blessingParticles.Play();
            }

            _nextBlessingTime = Time.time + BlessingCooldown;
            _onBlessingReceived?.Invoke();
            return true;
        }

        private bool IsPlayerInRange()
        {
            if (player == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= InteractionRange;
        }

        private void UpdatePromptVisibility()
        {
            if (_promptLabel == null)
            {
                return;
            }

            bool showPrompt = IsPlayerInRange() && IsBlessingReady;
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
    }
}

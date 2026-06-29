using Ashlight.Player;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Ashlight.Environment
{
    /// <summary>
    /// Base class for church worship-point interactions with shared proximity, prompt, and save logic.
    /// </summary>
    public abstract class WorshipInteraction : MonoBehaviour
    {
        [SerializeField] protected ChurchSafeZone parentChurch;
        [SerializeField] protected float interactionRange = 2f;
        [SerializeField] protected SaveSystem saveSystem;
        [SerializeField] protected GameSettings gameSettings;
        [SerializeField] private float promptHeightOffset = 2f;

        [Header("Events")]
        [SerializeField] private UnityEvent _onManualSaveRequested;

        protected Transform player;

        private PlayerController _playerController;
        private InputAction _interactAction;
        private Canvas _promptCanvas;
        private Text _promptText;
        private bool _isPromptVisible;

        /// <summary>Invoked when manual save UI should be shown instead of auto-saving.</summary>
        public UnityEvent OnManualSaveRequested => _onManualSaveRequested;

        protected virtual void Awake()
        {
            if (parentChurch == null)
            {
                parentChurch = GetComponentInParent<ChurchSafeZone>();
            }

            if (parentChurch == null)
            {
                Debug.LogError($"{nameof(WorshipInteraction)} requires a {nameof(ChurchSafeZone)} reference.", this);
                enabled = false;
                return;
            }

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
                _playerController = playerObject.GetComponent<PlayerController>();
            }

            if (player == null)
            {
                Debug.LogWarning($"{nameof(WorshipInteraction)} could not resolve the player transform.", this);
            }

            if (_playerController != null)
            {
                _interactAction = _playerController.InteractAction;
            }

            if (saveSystem == null)
            {
                saveSystem = FindAnyObjectByType<SaveSystem>();
            }

            if (saveSystem == null)
            {
                Debug.LogWarning($"{nameof(WorshipInteraction)} has no {nameof(SaveSystem)} assigned.", this);
            }

            BuildWorldPrompt();
        }

        private void OnDisable()
        {
            HidePrompt();
        }

        protected virtual void Update()
        {
            bool inRange = IsPlayerInRange();

            if (inRange)
            {
                ShowPrompt();
            }
            else
            {
                HidePrompt();
            }

            if (!inRange || _interactAction == null || !_interactAction.WasPressedThisFrame())
            {
                return;
            }

            OnWorship();
        }

        protected virtual void LateUpdate()
        {
            if (_promptCanvas == null || !_isPromptVisible || Camera.main == null)
            {
                return;
            }

            Transform promptTransform = _promptCanvas.transform;
            promptTransform.LookAt(
                promptTransform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        /// <summary>Applies worship-specific benefits. Subclasses should call base after their logic.</summary>
        protected virtual void OnWorship()
        {
            if (parentChurch == null || !parentChurch.HasSavePoint)
            {
                return;
            }

            if (gameSettings == null || gameSettings.AutoSaveEnabled)
            {
                saveSystem?.AutoSave();
                return;
            }

            _onManualSaveRequested?.Invoke();
        }

        /// <summary>Gets the prompt text shown when the player is in range.</summary>
        /// <returns>Localized prompt string.</returns>
        protected abstract string GetPromptText();

        /// <summary>Shows the world-space worship prompt.</summary>
        protected virtual void ShowPrompt()
        {
            if (_promptCanvas == null || _promptText == null)
            {
                return;
            }

            _promptText.text = GetPromptText();
            _promptCanvas.gameObject.SetActive(true);
            _isPromptVisible = true;
        }

        /// <summary>Hides the world-space worship prompt.</summary>
        protected virtual void HidePrompt()
        {
            if (_promptCanvas == null)
            {
                return;
            }

            _promptCanvas.gameObject.SetActive(false);
            _isPromptVisible = false;
        }

        /// <summary>Gets whether the player is within interaction range.</summary>
        /// <returns>True when the player can interact.</returns>
        protected bool IsPlayerInRange()
        {
            if (player == null)
            {
                return false;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            return distance <= interactionRange;
        }

        private void BuildWorldPrompt()
        {
            GameObject promptObject = new GameObject("WorshipPrompt");
            promptObject.transform.SetParent(transform, false);
            promptObject.transform.localPosition = Vector3.up * promptHeightOffset;

            _promptCanvas = promptObject.AddComponent<Canvas>();
            _promptCanvas.renderMode = RenderMode.WorldSpace;
            _promptCanvas.sortingOrder = 50;

            RectTransform canvasRect = _promptCanvas.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(4f, 1f);
            canvasRect.localScale = Vector3.one * 0.01f;

            GameObject textObject = new GameObject("PromptText");
            textObject.transform.SetParent(promptObject.transform, false);

            _promptText = textObject.AddComponent<Text>();
            _promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _promptText.alignment = TextAnchor.MiddleCenter;
            _promptText.fontSize = 32;
            _promptText.color = new Color(0.95f, 0.9f, 0.75f, 1f);
            _promptText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _promptText.verticalOverflow = VerticalWrapMode.Overflow;

            RectTransform textRect = _promptText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            promptObject.SetActive(false);
            _isPromptVisible = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.95f, 0.85f, 0.35f, 0.85f);
            Gizmos.DrawWireSphere(transform.position, interactionRange);
        }
    }
}

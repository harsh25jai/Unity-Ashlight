using System.Collections.Generic;
using Ashlight.Audio;
using Ashlight.Environment;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Church upgrade station panel built with UI Toolkit. Shown after receiving an altar blessing.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class UpgradeStationUI : MonoBehaviour
    {
        private static readonly Color OwnedGreen = new Color(0.48f, 0.72f, 0.48f);
        private static readonly Color PurchaseGold = new Color(0.79f, 0.58f, 0.16f);
        private static readonly Color LockedGrey = new Color(0.55f, 0.55f, 0.55f);
        private static readonly Color FlashSuccess = new Color(0.3f, 0.6f, 0.3f, 0.5f);
        private static readonly Color FlashError = new Color(0.75f, 0.15f, 0.15f, 0.6f);
        private static readonly Color CardBackground = new Color(0.12f, 0.14f, 0.12f, 0.9f);

        [SerializeField] private UpgradeSystem upgradeSystem;
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private AltarInteraction altarInteraction;
        [SerializeField] private AudioClip purchaseSound;

        private VisualElement _overlay;
        private Label _faithLabel;
        private VisualElement _torchColumn;
        private VisualElement _churchColumn;
        private VisualElement _characterColumn;
        private Button _closeButton;

        private readonly Dictionary<string, VisualElement> _upgradeCards = new Dictionary<string, VisualElement>();
        private string _lastPurchaseAttemptId;
        private bool _isVisible;

        private void Awake()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            if (upgradeSystem == null)
            {
                upgradeSystem = FindAnyObjectByType<UpgradeSystem>();
            }

            if (altarInteraction == null)
            {
                altarInteraction = FindAnyObjectByType<AltarInteraction>();
            }
        }

        private void Start()
        {
            CacheVisualElements();
            BuildCategoryHeaders();
            BuildUpgradeCards();
            HidePanel();
            WireButtons();
        }

        private void OnEnable()
        {
            if (altarInteraction != null)
            {
                altarInteraction.OnBlessingReceived.AddListener(ShowPanel);
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.OnFaithChanged.AddListener(OnFaithChanged);
                upgradeSystem.OnUpgradePurchased.AddListener(OnUpgradePurchased);
                upgradeSystem.OnUpgradeFailed.AddListener(OnUpgradeFailed);
            }
        }

        private void OnDisable()
        {
            if (altarInteraction != null)
            {
                altarInteraction.OnBlessingReceived.RemoveListener(ShowPanel);
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.OnFaithChanged.RemoveListener(OnFaithChanged);
                upgradeSystem.OnUpgradePurchased.RemoveListener(OnUpgradePurchased);
                upgradeSystem.OnUpgradeFailed.RemoveListener(OnUpgradeFailed);
            }
        }

        private void Update()
        {
            if (!_isVisible || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                HidePanel();
            }
        }

        /// <summary>
        /// Shows the upgrade station overlay and refreshes all card states.
        /// </summary>
        public void ShowPanel()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.style.display = DisplayStyle.Flex;
            _isVisible = true;
            RefreshFaithLabel();
            RefreshAllCards();
        }

        /// <summary>
        /// Hides the upgrade station overlay.
        /// </summary>
        public void HidePanel()
        {
            if (_overlay == null)
            {
                return;
            }

            _overlay.style.display = DisplayStyle.None;
            _isVisible = false;
        }

        /// <summary>
        /// Formats upgrade failure reason codes for HUD display.
        /// </summary>
        /// <param name="reason">Internal failure reason code.</param>
        /// <returns>User-facing status text.</returns>
        public static string FormatUpgradeFailureReason(string reason)
        {
            switch (reason)
            {
                case "insufficient_faith":
                    return "INSUFFICIENT FAITH";
                case "prerequisite_missing":
                    return "LOCKED";
                case "already_purchased":
                    return "OWNED";
                case "upgrade_not_found":
                    return "NOT FOUND";
                default:
                    return reason ?? "FAILED";
            }
        }

        private void CacheVisualElements()
        {
            VisualElement root = uiDocument != null ? uiDocument.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogError($"{nameof(UpgradeStationUI)} has no UI root.", this);
                return;
            }

            _overlay = root.Q<VisualElement>("overlay");
            _faithLabel = root.Q<Label>("faith-label");
            _torchColumn = root.Q<VisualElement>("torch-column");
            _churchColumn = root.Q<VisualElement>("church-column");
            _characterColumn = root.Q<VisualElement>("character-column");
            _closeButton = root.Q<Button>("close-button");
        }

        private void WireButtons()
        {
            _closeButton?.RegisterCallback<ClickEvent>(_ => HidePanel());
        }

        private void BuildCategoryHeaders()
        {
            AddColumnHeader(_torchColumn, "TORCH");
            AddColumnHeader(_churchColumn, "CHURCH");
            AddColumnHeader(_characterColumn, "CHARACTER");
        }

        private static void AddColumnHeader(VisualElement column, string title)
        {
            if (column == null)
            {
                return;
            }

            Label header = new Label($"{title} UPGRADES")
            {
                name = $"{title.ToLower()}-header"
            };
            header.style.fontSize = 16;
            header.style.color = PurchaseGold;
            header.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.style.unityTextAlign = TextAnchor.MiddleCenter;
            header.style.marginBottom = 8;
            column.Add(header);
        }

        private void BuildUpgradeCards()
        {
            if (upgradeSystem == null || upgradeSystem.UpgradeTree == null)
            {
                return;
            }

            PopulateColumn(_torchColumn, UpgradeCategory.Torch);
            PopulateColumn(_churchColumn, UpgradeCategory.Church);
            PopulateColumn(_characterColumn, UpgradeCategory.Character);
        }

        private void PopulateColumn(VisualElement column, UpgradeCategory category)
        {
            if (column == null || upgradeSystem.UpgradeTree == null)
            {
                return;
            }

            List<UpgradeDefinition> upgrades = upgradeSystem.UpgradeTree.GetByCategory(category);
            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade == null || string.IsNullOrEmpty(upgrade.upgradeID))
                {
                    continue;
                }

                VisualElement card = CreateUpgradeCard(upgrade);
                column.Add(card);
                _upgradeCards[upgrade.upgradeID] = card;
            }
        }

        private VisualElement CreateUpgradeCard(UpgradeDefinition upgrade)
        {
            VisualElement card = new VisualElement
            {
                name = $"upgrade-card-{upgrade.upgradeID}"
            };
            card.style.backgroundColor = CardBackground;
            card.style.borderTopWidth = 1;
            card.style.borderBottomWidth = 1;
            card.style.borderLeftWidth = 1;
            card.style.borderRightWidth = 1;
            card.style.borderTopColor = LockedGrey;
            card.style.borderBottomColor = LockedGrey;
            card.style.borderLeftColor = LockedGrey;
            card.style.borderRightColor = LockedGrey;
            card.style.borderTopLeftRadius = 4;
            card.style.borderTopRightRadius = 4;
            card.style.borderBottomLeftRadius = 4;
            card.style.borderBottomRightRadius = 4;
            card.style.paddingTop = 8;
            card.style.paddingBottom = 8;
            card.style.paddingLeft = 8;
            card.style.paddingRight = 8;
            card.style.marginBottom = 8;

            Label nameLabel = new Label(upgrade.upgradeName)
            {
                name = "name-label"
            };
            nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLabel.style.color = Color.white;
            nameLabel.style.fontSize = 14;
            card.Add(nameLabel);

            Label descriptionLabel = new Label(upgrade.description)
            {
                name = "description-label"
            };
            descriptionLabel.style.unityFontStyleAndWeight = FontStyle.Italic;
            descriptionLabel.style.fontSize = 11;
            descriptionLabel.style.color = new Color(0.85f, 0.85f, 0.85f);
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            descriptionLabel.style.marginTop = 4;
            descriptionLabel.style.marginBottom = 4;
            card.Add(descriptionLabel);

            Label costLabel = new Label($"{upgrade.faithCost} Faith")
            {
                name = "cost-label"
            };
            costLabel.style.fontSize = 12;
            costLabel.style.color = PurchaseGold;
            costLabel.style.marginBottom = 6;
            card.Add(costLabel);

            VisualElement statusContainer = new VisualElement { name = "status-container" };
            card.Add(statusContainer);

            RefreshCardState(upgrade.upgradeID);
            return card;
        }

        private void RefreshAllCards()
        {
            foreach (string upgradeId in _upgradeCards.Keys)
            {
                RefreshCardState(upgradeId);
            }
        }

        private void RefreshCardState(string upgradeId)
        {
            if (!_upgradeCards.TryGetValue(upgradeId, out VisualElement card) || upgradeSystem == null)
            {
                return;
            }

            UpgradeDefinition upgrade = upgradeSystem.UpgradeTree?.GetUpgrade(upgradeId);
            if (upgrade == null)
            {
                return;
            }

            VisualElement statusContainer = card.Q<VisualElement>("status-container");
            if (statusContainer == null)
            {
                return;
            }

            statusContainer.Clear();

            if (upgradeSystem.IsUpgradePurchased(upgradeId))
            {
                Label ownedLabel = new Label("OWNED");
                ownedLabel.style.color = OwnedGreen;
                ownedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                statusContainer.Add(ownedLabel);
                return;
            }

            if (!upgradeSystem.IsPrerequisiteMet(upgrade))
            {
                Label lockedLabel = new Label("LOCKED");
                lockedLabel.style.color = LockedGrey;
                lockedLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                statusContainer.Add(lockedLabel);
                return;
            }

            if (upgradeSystem.CurrentFaith < upgrade.faithCost)
            {
                Label insufficientLabel = new Label("INSUFFICIENT FAITH");
                insufficientLabel.style.color = LockedGrey;
                insufficientLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                insufficientLabel.style.fontSize = 11;
                statusContainer.Add(insufficientLabel);
                return;
            }

            Button purchaseButton = new Button(() => OnPurchaseClicked(upgradeId))
            {
                text = "PURCHASE",
                name = "purchase-button"
            };
            purchaseButton.style.backgroundColor = new Color(0.2f, 0.15f, 0.05f);
            purchaseButton.style.color = PurchaseGold;
            purchaseButton.style.borderTopColor = PurchaseGold;
            purchaseButton.style.borderBottomColor = PurchaseGold;
            purchaseButton.style.borderLeftColor = PurchaseGold;
            purchaseButton.style.borderRightColor = PurchaseGold;
            purchaseButton.style.borderTopWidth = 1;
            purchaseButton.style.borderBottomWidth = 1;
            purchaseButton.style.borderLeftWidth = 1;
            purchaseButton.style.borderRightWidth = 1;
            purchaseButton.style.unityFontStyleAndWeight = FontStyle.Bold;
            statusContainer.Add(purchaseButton);
        }

        private void OnPurchaseClicked(string upgradeId)
        {
            if (upgradeSystem == null)
            {
                return;
            }

            _lastPurchaseAttemptId = upgradeId;
            bool purchased = upgradeSystem.PurchaseUpgrade(upgradeId);
            RefreshAllCards();

            if (purchased)
            {
                PlayPurchaseSound();
            }
        }

        private void PlayPurchaseSound()
        {
            if (purchaseSound == null)
            {
                return;
            }

            Vector3 position = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            AudioManager.Instance?.PlaySFX(purchaseSound, position);
        }

        private void OnFaithChanged(int faith)
        {
            UpdateFaithLabel(faith);
            RefreshAllCards();
        }

        private void RefreshFaithLabel()
        {
            if (upgradeSystem != null)
            {
                UpdateFaithLabel(upgradeSystem.CurrentFaith);
            }
        }

        private void UpdateFaithLabel(int faith)
        {
            if (_faithLabel != null)
            {
                _faithLabel.text = $"Faith: {faith}";
            }
        }

        private void OnUpgradePurchased(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            RefreshAllCards();
            FlashCard(upgrade.upgradeID, "Purchased!", FlashSuccess);
        }

        private void OnUpgradeFailed(string reason)
        {
            string upgradeId = _lastPurchaseAttemptId;
            string message = FormatUpgradeFailureReason(reason);
            FlashCard(upgradeId, message, FlashError);
            RefreshAllCards();
        }

        private void FlashCard(string upgradeId, string message, Color flashColor)
        {
            if (string.IsNullOrEmpty(upgradeId) || !_upgradeCards.TryGetValue(upgradeId, out VisualElement card))
            {
                return;
            }

            Color originalColor = card.style.backgroundColor.value;
            card.style.backgroundColor = flashColor;

            Label flashLabel = new Label(message)
            {
                name = "flash-label"
            };
            flashLabel.style.position = Position.Absolute;
            flashLabel.style.left = 0;
            flashLabel.style.right = 0;
            flashLabel.style.top = 0;
            flashLabel.style.bottom = 0;
            flashLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            flashLabel.style.color = Color.white;
            flashLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            flashLabel.style.fontSize = 13;
            card.Add(flashLabel);

            card.schedule.Execute(() =>
            {
                flashLabel.RemoveFromHierarchy();
                card.style.backgroundColor = originalColor;
            }).ExecuteLater(600);
        }
    }
}

using System.Collections.Generic;
using Ashlight.Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Ashlight.Systems
{
    /// <summary>
    /// UnityEvent wrapper for <see cref="UpgradeDefinition"/> purchases.
    /// </summary>
    [System.Serializable]
    public class UpgradePurchasedEvent : UnityEvent<UpgradeDefinition> { }

    /// <summary>
    /// Manages faith currency and applies purchased church upgrades.
    /// </summary>
    [DisallowMultipleComponent]
    public class UpgradeSystem : MonoBehaviour
    {
        [SerializeField] private UpgradeTree upgradeTree;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private PlayerController playerController;
        [SerializeField] private SaveSystem saveSystem;

        [Header("Events")]
        [SerializeField] private UnityEvent<int> _onFaithChanged;
        [SerializeField] private UpgradePurchasedEvent _onUpgradePurchased;
        [SerializeField] private UnityEvent<string> _onUpgradeFailed;

        private readonly List<string> _purchasedUpgradeIDs = new List<string>();
        private int _currentFaith;
        private float _shrineActivationModifier = 1f;
        private bool _ritualsUnlocked;

        /// <summary>Gets the current faith balance.</summary>
        public int CurrentFaith => _currentFaith;

        /// <summary>Gets the shrine activation cost modifier from 0 to 1.</summary>
        public float ShrineActivationModifier => _shrineActivationModifier;

        /// <summary>Gets whether ritual abilities are unlocked.</summary>
        public bool RitualsUnlocked => _ritualsUnlocked;

        /// <summary>Invoked when faith changes.</summary>
        public UnityEvent<int> OnFaithChanged => _onFaithChanged;

        /// <summary>Invoked when an upgrade is purchased successfully.</summary>
        public UpgradePurchasedEvent OnUpgradePurchased => _onUpgradePurchased;

        /// <summary>Invoked when an upgrade purchase fails.</summary>
        public UnityEvent<string> OnUpgradeFailed => _onUpgradeFailed;

        private void Awake()
        {
            if (upgradeTree == null)
            {
                Debug.LogError($"{nameof(UpgradeSystem)} requires an {nameof(UpgradeTree)}.", this);
            }

            if (holyTorch == null)
            {
                holyTorch = FindAnyObjectByType<HolyTorch>();
            }

            if (playerStats == null)
            {
                Debug.LogError($"{nameof(UpgradeSystem)} requires a {nameof(PlayerStats)}.", this);
            }

            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(UpgradeSystem)} requires a {nameof(HolyWaterInventory)}.", this);
            }

            if (playerController == null)
            {
                playerController = FindAnyObjectByType<PlayerController>();
            }

            if (saveSystem == null)
            {
                saveSystem = FindAnyObjectByType<SaveSystem>();
            }

            if (holyTorch == null)
            {
                Debug.LogError($"{nameof(UpgradeSystem)} requires a {nameof(HolyTorch)}.", this);
            }

            if (playerController == null)
            {
                Debug.LogError($"{nameof(UpgradeSystem)} requires a {nameof(PlayerController)}.", this);
            }

            if (saveSystem == null)
            {
                Debug.LogWarning($"{nameof(UpgradeSystem)} has no {nameof(SaveSystem)} assigned.", this);
            }
        }

        private void Start()
        {
            _currentFaith = upgradeTree != null ? upgradeTree.StartingFaith : 0;
            _onFaithChanged?.Invoke(_currentFaith);
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
            {
                AddFaith(200);
                bool result = PurchaseUpgrade("torch_pulse_1");
                Debug.Log("Purchase result: " + result);
                Debug.Log("Torch radius should now be increased");
            }
        }

        /// <summary>
        /// Adds faith currency and notifies listeners.
        /// </summary>
        /// <param name="amount">Faith amount to add.</param>
        public void AddFaith(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            _currentFaith += amount;
            _onFaithChanged?.Invoke(_currentFaith);
        }

        /// <summary>
        /// Attempts to spend faith currency.
        /// </summary>
        /// <param name="amount">Faith amount to spend.</param>
        /// <returns>False when there is insufficient faith.</returns>
        public bool SpendFaith(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (_currentFaith < amount)
            {
                return false;
            }

            _currentFaith -= amount;
            _onFaithChanged?.Invoke(_currentFaith);
            return true;
        }

        /// <summary>
        /// Gets whether the given upgrade has already been purchased.
        /// </summary>
        /// <param name="upgradeID">Upgrade identifier.</param>
        /// <returns>True when the upgrade is owned.</returns>
        public bool IsUpgradePurchased(string upgradeID)
        {
            return !string.IsNullOrEmpty(upgradeID) && _purchasedUpgradeIDs.Contains(upgradeID);
        }

        /// <summary>
        /// Attempts to purchase and apply an upgrade by identifier.
        /// </summary>
        /// <param name="upgradeID">Upgrade identifier.</param>
        /// <returns>True when the purchase succeeded.</returns>
        public bool PurchaseUpgrade(string upgradeID)
        {
            if (upgradeTree == null)
            {
                _onUpgradeFailed?.Invoke("missing_upgrade_tree");
                return false;
            }

            UpgradeDefinition upgrade = upgradeTree.GetUpgrade(upgradeID);
            if (upgrade == null)
            {
                _onUpgradeFailed?.Invoke("upgrade_not_found");
                return false;
            }

            if (IsUpgradePurchased(upgradeID))
            {
                _onUpgradeFailed?.Invoke("already_purchased");
                return false;
            }

            if (!upgradeTree.HasPrerequisite(upgrade, _purchasedUpgradeIDs))
            {
                _onUpgradeFailed?.Invoke("prerequisite_missing");
                return false;
            }

            if (!SpendFaith(upgrade.faithCost))
            {
                _onUpgradeFailed?.Invoke("insufficient_faith");
                return false;
            }

            ApplyUpgrade(upgrade);
            _purchasedUpgradeIDs.Add(upgradeID);

            if (saveSystem != null)
            {
                saveSystem.RegisterPurchasedUpgrade(upgradeID);
            }
            else
            {
                Debug.LogWarning($"{nameof(UpgradeSystem)} purchased {upgradeID} without a save system.", this);
            }

            _onUpgradePurchased?.Invoke(upgrade);
            return true;
        }

        /// <summary>
        /// Applies the gameplay effect of a purchased upgrade.
        /// </summary>
        /// <param name="upgrade">Upgrade definition to apply.</param>
        public void ApplyUpgrade(UpgradeDefinition upgrade)
        {
            if (upgrade == null)
            {
                return;
            }

            switch (upgrade.effect)
            {
                case UpgradeEffect.TorchRadius:
                    holyTorch?.AddRadiusBonus(upgrade.effectValue);
                    break;

                case UpgradeEffect.TorchBurnRate:
                    holyTorch?.AddBurnRateModifier(-upgrade.effectValue / 100f);
                    break;

                case UpgradeEffect.TorchUnlockPulse:
                    holyTorch?.UnlockHolyPulse();
                    break;

                case UpgradeEffect.HolyWaterCapacity:
                    if (holyWaterInventory != null)
                    {
                        holyWaterInventory.AddMaxCapacity(upgrade.effectValue);
                        holyWaterInventory.Replenish(upgrade.effectValue);
                    }
                    break;

                case UpgradeEffect.ShrineActivationCost:
                    _shrineActivationModifier -= upgrade.effectValue / 100f;
                    _shrineActivationModifier = Mathf.Clamp01(_shrineActivationModifier);
                    break;

                case UpgradeEffect.UnlockRituals:
                    _ritualsUnlocked = true;
                    Debug.Log("Rituals unlocked — future feature");
                    break;

                case UpgradeEffect.MoveSpeed:
                    playerStats?.AddModifier("upgrade_speed", 1f + upgrade.effectValue / 100f);
                    playerController?.RefreshStatsFromScriptableObject();
                    break;

                case UpgradeEffect.FearResistance:
                    playerStats?.AddFearResistance(upgrade.effectValue / 100f);
                    break;

                case UpgradeEffect.CarryCapacity:
                    if (playerStats != null)
                    {
                        playerStats.MultiplyCarryCapacity(1f + upgrade.effectValue / 100f);
                    }

                    holyWaterInventory?.MultiplyMaxCapacity(1f + upgrade.effectValue / 100f);
                    break;
            }
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Collection of all upgrade definitions available in Ashlight.
    /// </summary>
    [CreateAssetMenu(fileName = "UpgradeTree", menuName = "Ashlight/Upgrade Tree")]
    public class UpgradeTree : ScriptableObject
    {
        [SerializeField] private List<UpgradeDefinition> allUpgrades = new List<UpgradeDefinition>();
        [SerializeField] private int startingFaith;

        /// <summary>Gets the starting faith granted on a new run.</summary>
        public int StartingFaith => startingFaith;

        /// <summary>Gets all upgrade definitions in this tree.</summary>
        public IReadOnlyList<UpgradeDefinition> AllUpgrades => allUpgrades;

        /// <summary>
        /// Finds an upgrade definition by its unique identifier.
        /// </summary>
        /// <param name="upgradeID">Upgrade identifier.</param>
        /// <returns>Matching upgrade or null when not found.</returns>
        public UpgradeDefinition GetUpgrade(string upgradeID)
        {
            if (string.IsNullOrEmpty(upgradeID) || allUpgrades == null)
            {
                return null;
            }

            foreach (UpgradeDefinition upgrade in allUpgrades)
            {
                if (upgrade != null && upgrade.upgradeID == upgradeID)
                {
                    return upgrade;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns all upgrades in the given category.
        /// </summary>
        /// <param name="category">Upgrade category filter.</param>
        /// <returns>Matching upgrade definitions.</returns>
        public List<UpgradeDefinition> GetByCategory(UpgradeCategory category)
        {
            List<UpgradeDefinition> results = new List<UpgradeDefinition>();

            if (allUpgrades == null)
            {
                return results;
            }

            foreach (UpgradeDefinition upgrade in allUpgrades)
            {
                if (upgrade != null && upgrade.category == category)
                {
                    results.Add(upgrade);
                }
            }

            return results;
        }

        /// <summary>
        /// Checks whether an upgrade's prerequisite has been purchased.
        /// </summary>
        /// <param name="upgrade">Upgrade to validate.</param>
        /// <param name="purchasedIDs">Currently purchased upgrade identifiers.</param>
        /// <returns>True when no prerequisite is required or it is already owned.</returns>
        public bool HasPrerequisite(UpgradeDefinition upgrade, List<string> purchasedIDs)
        {
            if (upgrade == null || string.IsNullOrEmpty(upgrade.prerequisiteID))
            {
                return true;
            }

            return purchasedIDs != null && purchasedIDs.Contains(upgrade.prerequisiteID);
        }
    }
}

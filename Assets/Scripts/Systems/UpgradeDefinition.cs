using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Upgrade categories available at the church upgrade station.
    /// </summary>
    public enum UpgradeCategory
    {
        Torch,
        Church,
        Character
    }

    /// <summary>
    /// Stat or ability modified by an upgrade purchase.
    /// </summary>
    public enum UpgradeEffect
    {
        TorchRadius,
        TorchBurnRate,
        TorchUnlockPulse,
        HolyWaterCapacity,
        ShrineActivationCost,
        UnlockRituals,
        MoveSpeed,
        FearResistance,
        CarryCapacity
    }

    /// <summary>
    /// Defines a single faith-purchasable upgrade node.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "Ashlight/Upgrade Definition")]
    public class UpgradeDefinition : ScriptableObject
    {
        public string upgradeID;
        public string upgradeName;
        public string description;
        public Sprite icon;
        public int faithCost;
        public UpgradeCategory category;
        public UpgradeEffect effect;
        public float effectValue;
        public string prerequisiteID;
    }
}

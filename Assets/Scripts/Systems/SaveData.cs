using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// Serializable snapshot of Ashlight player, resource, and progression state.
    /// </summary>
    [Serializable]
    public class SaveData : ISerializationCallbackReceiver
    {
        public float playerPosX;
        public float playerPosY;
        public float playerPosZ;
        public float playerHealth;

        public float holyWaterCurrent;
        public float torchFuelCurrent;

        public int nightCycleCount;
        public float currentNightDuration;

        public List<string> activatedShrineIDs = new List<string>();
        public List<string> purchasedUpgradeIDs = new List<string>();

        public string saveDateTime;
        public float totalPlayTime;

        [SerializeField] private string[] _activatedShrineIDsSerialized = Array.Empty<string>();
        [SerializeField] private string[] _purchasedUpgradeIDsSerialized = Array.Empty<string>();

        /// <inheritdoc />
        public void OnBeforeSerialize()
        {
            _activatedShrineIDsSerialized = activatedShrineIDs != null
                ? activatedShrineIDs.ToArray()
                : Array.Empty<string>();

            _purchasedUpgradeIDsSerialized = purchasedUpgradeIDs != null
                ? purchasedUpgradeIDs.ToArray()
                : Array.Empty<string>();
        }

        /// <inheritdoc />
        public void OnAfterDeserialize()
        {
            activatedShrineIDs = _activatedShrineIDsSerialized != null
                ? new List<string>(_activatedShrineIDsSerialized)
                : new List<string>();

            purchasedUpgradeIDs = _purchasedUpgradeIDsSerialized != null
                ? new List<string>(_purchasedUpgradeIDsSerialized)
                : new List<string>();
        }
    }
}

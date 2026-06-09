using System.IO;
using Ashlight.Player;
using UnityEngine;

namespace Ashlight.Systems
{
    /// <summary>
    /// JSON save/load for core Ashlight progression and player state.
    /// </summary>
    [DisallowMultipleComponent]
    public class SaveSystem : MonoBehaviour
    {
        private const string SaveFileName = "ashlight_save.json";

        [SerializeField] private Transform player;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private HolyWaterInventory holyWaterInventory;

        /// <summary>Serializable snapshot written to disk.</summary>
        [System.Serializable]
        public class SaveData
        {
            public float playerHealth;
            public float holyWater;
            public Vector3 playerPosition;
            public int nightNumber;
        }

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

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        /// <summary>Captures current state and writes a JSON save file.</summary>
        public void AutoSave()
        {
            SaveData data = CaptureSaveData();
            string json = JsonUtility.ToJson(data, true);
            string path = Path.Combine(Application.persistentDataPath, SaveFileName);
            File.WriteAllText(path, json);
            Debug.Log($"{nameof(SaveSystem)} auto-saved to {path}");
        }

        /// <summary>Builds a save snapshot from current scene references.</summary>
        /// <returns>Populated save data.</returns>
        public SaveData CaptureSaveData()
        {
            SaveData data = new SaveData
            {
                playerHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f,
                holyWater = holyWaterInventory != null ? holyWaterInventory.Current : 0f,
                playerPosition = player != null ? player.position : Vector3.zero,
                nightNumber = 0
            };

            return data;
        }
    }
}

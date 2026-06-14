using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Ashlight.Environment;
using Ashlight.Player;
using UnityEngine;
using UnityEngine.Events;

namespace Ashlight.Systems
{
    /// <summary>
    /// JSON save/load for Ashlight progression, resources, and player state.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(100)]
    public class SaveSystem : MonoBehaviour
    {
        private const string SaveFileName = "ashlight_save.json";
        private const float AutoSaveIntervalSeconds = 300f;

        [SerializeField] private Transform playerTransform;
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private DayNightCycle dayNightCycle;

        [Header("Events")]
        [SerializeField] private UnityEvent _onSaveComplete;
        [SerializeField] private UnityEvent _onLoadComplete;
        [SerializeField] private UnityEvent _onSaveFailed;

        private string _savePath;
        private readonly List<string> _activatedShrineIDs = new List<string>();
        private readonly List<string> _purchasedUpgradeIDs = new List<string>();

        private float _loadedPlayTime;
        private float _sessionStartTime;
        private Coroutine _autoSaveRoutine;
        private bool _isSaving;

        /// <summary>Gets the active <see cref="SaveSystem"/> instance.</summary>
        public static SaveSystem Instance { get; private set; }

        /// <summary>Gets whether a save file exists on disk.</summary>
        public bool HasSave => !string.IsNullOrEmpty(_savePath) && File.Exists(_savePath);

        /// <summary>Invoked after a successful save.</summary>
        public UnityEvent OnSaveComplete => _onSaveComplete;

        /// <summary>Invoked after a successful load.</summary>
        public UnityEvent OnLoadComplete => _onLoadComplete;

        /// <summary>Invoked when save or load fails.</summary>
        public UnityEvent OnSaveFailed => _onSaveFailed;

        private void Awake()
        {
            _savePath = Path.Combine(Application.persistentDataPath, SaveFileName);

            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (playerTransform == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
            }

            if (playerHealth == null && playerTransform != null)
            {
                playerHealth = playerTransform.GetComponent<PlayerHealth>();
            }

            if (holyTorch == null && playerTransform != null)
            {
                holyTorch = playerTransform.GetComponentInChildren<HolyTorch>();
            }

            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }

            if (playerTransform == null)
            {
                Debug.LogError($"{nameof(SaveSystem)} requires a player {nameof(Transform)}.", this);
            }

            if (playerHealth == null)
            {
                Debug.LogError($"{nameof(SaveSystem)} requires a {nameof(PlayerHealth)}.", this);
            }

            if (holyWaterInventory == null)
            {
                Debug.LogError($"{nameof(SaveSystem)} requires a {nameof(HolyWaterInventory)}.", this);
            }

            if (holyTorch == null)
            {
                Debug.LogError($"{nameof(SaveSystem)} requires a {nameof(HolyTorch)}.", this);
            }

            if (dayNightCycle == null)
            {
                Debug.LogError($"{nameof(SaveSystem)} requires a {nameof(DayNightCycle)}.", this);
            }

            _sessionStartTime = Time.unscaledTime;
        }

        private void Start()
        {
            if (HasSave)
            {
                LoadSave();
            }
            else
            {
                InitializeDefaults();
            }

            if (_autoSaveRoutine == null)
            {
                _autoSaveRoutine = StartCoroutine(AutoSaveRoutine());
            }
        }

        private void OnDestroy()
        {
            if (_autoSaveRoutine != null)
            {
                StopCoroutine(_autoSaveRoutine);
                _autoSaveRoutine = null;
            }

            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Captures current game state and writes it to disk asynchronously.
        /// </summary>
        public async void AutoSave()
        {
            if (_isSaving)
            {
                return;
            }

            _isSaving = true;

            try
            {
                SaveData data = GetCurrentSaveData();
                string json = JsonUtility.ToJson(data, true);
                await File.WriteAllTextAsync(_savePath, json);
                Debug.Log("Game saved at: " + _savePath);
                _onSaveComplete?.Invoke();
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(SaveSystem)} failed to save: {exception.Message}", this);
                _onSaveFailed?.Invoke();
            }
            finally
            {
                _isSaving = false;
            }
        }

        /// <summary>
        /// Loads game state from disk when a save file exists.
        /// </summary>
        /// <returns>True when a save was loaded successfully.</returns>
        public bool LoadSave()
        {
            if (!HasSave)
            {
                return false;
            }

            try
            {
                string json = File.ReadAllText(_savePath);
                SaveData data = JsonUtility.FromJson<SaveData>(json);

                if (data == null)
                {
                    Debug.LogError($"{nameof(SaveSystem)} could not deserialize save data.", this);
                    _onSaveFailed?.Invoke();
                    return false;
                }

                ApplySaveData(data);
                _onLoadComplete?.Invoke();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(SaveSystem)} failed to load: {exception.Message}", this);
                _onSaveFailed?.Invoke();
                return false;
            }
        }

        /// <summary>
        /// Deletes the save file and resets all tracked systems to default values.
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(_savePath))
                {
                    File.Delete(_savePath);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError($"{nameof(SaveSystem)} failed to delete save: {exception.Message}", this);
                _onSaveFailed?.Invoke();
                return;
            }

            InitializeDefaults();
        }

        /// <summary>
        /// Returns the current game state as <see cref="SaveData"/> without writing to disk.
        /// </summary>
        /// <returns>Populated save snapshot.</returns>
        public SaveData GetCurrentSaveData()
        {
            Vector3 playerPosition = playerTransform != null ? playerTransform.position : Vector3.zero;

            SaveData data = new SaveData
            {
                playerPosX = playerPosition.x,
                playerPosY = playerPosition.y,
                playerPosZ = playerPosition.z,
                playerHealth = playerHealth != null ? playerHealth.CurrentHealth : 0f,
                holyWaterCurrent = holyWaterInventory != null ? holyWaterInventory.Current : 0f,
                torchFuelCurrent = holyTorch != null ? holyTorch.CurrentFuel : 0f,
                nightCycleCount = dayNightCycle != null ? dayNightCycle.NightCycleCount : 0,
                currentNightDuration = dayNightCycle != null ? dayNightCycle.CycleElapsed : 0f,
                saveDateTime = DateTime.Now.ToString("o"),
                totalPlayTime = _loadedPlayTime + (Time.unscaledTime - _sessionStartTime)
            };

            data.activatedShrineIDs.Clear();
            data.activatedShrineIDs.AddRange(_activatedShrineIDs);

            data.purchasedUpgradeIDs.Clear();
            data.purchasedUpgradeIDs.AddRange(_purchasedUpgradeIDs);

            return data;
        }

        /// <summary>
        /// Records a shrine activation and triggers an auto-save.
        /// </summary>
        /// <param name="shrineId">Unique shrine identifier.</param>
        public void RegisterShrineActivation(string shrineId)
        {
            if (string.IsNullOrEmpty(shrineId) || _activatedShrineIDs.Contains(shrineId))
            {
                return;
            }

            _activatedShrineIDs.Add(shrineId);
            AutoSave();
        }

        /// <summary>
        /// Records a purchased upgrade and triggers an auto-save.
        /// </summary>
        /// <param name="upgradeId">Unique upgrade identifier.</param>
        public void RegisterPurchasedUpgrade(string upgradeId)
        {
            if (string.IsNullOrEmpty(upgradeId) || _purchasedUpgradeIDs.Contains(upgradeId))
            {
                return;
            }

            _purchasedUpgradeIDs.Add(upgradeId);
            AutoSave();
        }

        /// <summary>
        /// Gets whether the given shrine has been activated in the current save.
        /// </summary>
        /// <param name="shrineId">Unique shrine identifier.</param>
        /// <returns>True when the shrine is activated.</returns>
        public bool IsShrineActivated(string shrineId)
        {
            return !string.IsNullOrEmpty(shrineId) && _activatedShrineIDs.Contains(shrineId);
        }

        /// <summary>
        /// Gets whether the given upgrade has been purchased in the current save.
        /// </summary>
        /// <param name="upgradeId">Unique upgrade identifier.</param>
        /// <returns>True when the upgrade is purchased.</returns>
        public bool IsUpgradePurchased(string upgradeId)
        {
            return !string.IsNullOrEmpty(upgradeId) && _purchasedUpgradeIDs.Contains(upgradeId);
        }

        private IEnumerator AutoSaveRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(AutoSaveIntervalSeconds);

            while (enabled)
            {
                yield return wait;
                AutoSave();
            }
        }

        private void ApplySaveData(SaveData data)
        {
            ApplyPlayerPosition(data);
            ApplyPlayerHealth(data.playerHealth);
            ApplyHolyWater(data.holyWaterCurrent);
            ApplyTorchFuel(data.torchFuelCurrent);
            ApplyDayNightCycle(data.nightCycleCount, data.currentNightDuration);

            _activatedShrineIDs.Clear();
            if (data.activatedShrineIDs != null)
            {
                _activatedShrineIDs.AddRange(data.activatedShrineIDs);
            }

            _purchasedUpgradeIDs.Clear();
            if (data.purchasedUpgradeIDs != null)
            {
                _purchasedUpgradeIDs.AddRange(data.purchasedUpgradeIDs);
            }

            _loadedPlayTime = Mathf.Max(0f, data.totalPlayTime);
            _sessionStartTime = Time.unscaledTime;
        }

        private void InitializeDefaults()
        {
            _loadedPlayTime = 0f;
            _sessionStartTime = Time.unscaledTime;

            _activatedShrineIDs.Clear();
            _purchasedUpgradeIDs.Clear();

            if (playerHealth != null)
            {
                playerHealth.Revive();
            }

            if (holyWaterInventory != null)
            {
                holyWaterInventory.ResetToFull();
            }

            if (holyTorch != null)
            {
                holyTorch.SetFuel(holyTorch.MaxFuel);
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.ResetCycle();
            }
        }

        private void ApplyPlayerPosition(SaveData data)
        {
            if (playerTransform == null)
            {
                return;
            }

            Vector3 savedPosition = new Vector3(data.playerPosX, data.playerPosY, data.playerPosZ);
            CharacterController characterController = playerTransform.GetComponent<CharacterController>();

            if (characterController != null)
            {
                characterController.enabled = false;
                playerTransform.position = savedPosition;
                characterController.enabled = true;
            }
            else
            {
                playerTransform.position = savedPosition;
            }
        }

        private void ApplyPlayerHealth(float health)
        {
            if (playerHealth == null)
            {
                return;
            }

            playerHealth.SetCurrentHealth(health);
        }

        private void ApplyHolyWater(float amount)
        {
            if (holyWaterInventory == null)
            {
                return;
            }

            holyWaterInventory.SetCurrent(amount);
        }

        private void ApplyTorchFuel(float fuel)
        {
            if (holyTorch == null)
            {
                return;
            }

            holyTorch.SetFuel(fuel);
        }

        private void ApplyDayNightCycle(int nightCycleCount, float cycleElapsed)
        {
            if (dayNightCycle == null)
            {
                return;
            }

            dayNightCycle.LoadCycleState(nightCycleCount, cycleElapsed);
        }
    }
}

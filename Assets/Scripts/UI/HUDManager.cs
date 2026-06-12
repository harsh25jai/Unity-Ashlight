using Ashlight.Environment;
using Ashlight.Systems;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Event-driven UI Toolkit HUD for Holy Water, torch fuel, and night cycle status.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIDocument))]
    public class HUDManager : MonoBehaviour
    {
        private const float LowHolyWaterThreshold = 0.2f;
        private const string TorchFlameGlyph = "\uD83D\uDD25";

        private static readonly Color HolyWaterGold = new Color(0.92f, 0.75f, 0.2f, 1f);
        private static readonly Color HolyWaterWarning = new Color(0.85f, 0.2f, 0.2f, 1f);
        private static readonly Color PanelBackground = new Color(0f, 0f, 0f, 0.6f);

        [SerializeField] private VisualTreeAsset hudLayout;
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private DayNightCycle dayNightCycle;

        private UIDocument _uiDocument;
        private ProgressBar _holyWaterBar;
        private Label _torchIndicator;
        private Label _nightPhaseLabel;
        private Label _nightCountLabel;

        /// <summary>
        /// Formats current and maximum health for HUD display.
        /// </summary>
        /// <param name="currentHealth">Current health value.</param>
        /// <param name="maxHealth">Maximum health value.</param>
        /// <returns>Display string such as "85 / 100".</returns>
        public static string FormatHealthDisplay(float currentHealth, float maxHealth)
        {
            int current = Mathf.CeilToInt(currentHealth);
            int max = Mathf.CeilToInt(maxHealth);
            return $"{current} / {max}";
        }

        /// <summary>Formats a day/night phase for HUD display.</summary>
        /// <param name="phase">Current phase.</param>
        /// <returns>Human-readable phase label.</returns>
        public static string FormatNightPhase(DayNightPhase phase)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return "Day";
                case DayNightPhase.Dusk:
                    return "Dusk";
                case DayNightPhase.Night_Early:
                    return "Night — Early";
                case DayNightPhase.Night_Deep:
                    return "Night — Deep";
                case DayNightPhase.Dawn:
                    return "Dawn";
                default:
                    return phase.ToString();
            }
        }

        /// <summary>Formats the night counter for HUD display.</summary>
        /// <param name="nightCount">Completed night cycles.</param>
        /// <returns>Display string such as "Night 3".</returns>
        public static string FormatNightCount(int nightCount)
        {
            return $"Night {Mathf.Max(0, nightCount)}";
        }

        /// <summary>Formats torch fuel for HUD display.</summary>
        /// <param name="fuelPercent">Fuel from 0 to 1.</param>
        /// <returns>Display string with flame glyph and percentage.</returns>
        public static string FormatTorchFuel(float fuelPercent)
        {
            int percent = Mathf.RoundToInt(Mathf.Clamp01(fuelPercent) * 100f);
            return $"{TorchFlameGlyph} {percent}%";
        }

        private void Awake()
        {
            _uiDocument = GetComponent<UIDocument>();

            if (_uiDocument == null)
            {
                Debug.LogError($"{nameof(HUDManager)} requires a {nameof(UIDocument)}.", this);
                enabled = false;
                return;
            }

            if (hudLayout != null)
            {
                _uiDocument.visualTreeAsset = hudLayout;
            }

            if (holyWaterInventory == null)
            {
                Debug.LogWarning($"{nameof(HUDManager)} has no {nameof(HolyWaterInventory)} assigned.", this);
            }

            if (holyTorch == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    holyTorch = playerObject.GetComponentInChildren<HolyTorch>();
                }
            }

            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }
        }

        private void OnEnable()
        {
            CacheVisualElements();
            BindEvents();
            RefreshHolyWaterBar();
            RefreshTorchIndicator();
            RefreshNightDisplay(dayNightCycle != null ? dayNightCycle.CurrentPhase : DayNightPhase.Day);
        }

        private void OnDisable()
        {
            UnbindEvents();
        }

        private void CacheVisualElements()
        {
            VisualElement root = _uiDocument.rootVisualElement;

            _holyWaterBar = root.Q<ProgressBar>("holy-water-bar");
            _torchIndicator = root.Q<Label>("torch-indicator");
            _nightPhaseLabel = root.Q<Label>("night-phase-label");
            _nightCountLabel = root.Q<Label>("night-count-label");

            if (_holyWaterBar == null)
            {
                Debug.LogError($"{nameof(HUDManager)} could not find holy-water-bar in the HUD layout.", this);
            }

            _holyWaterBar?.schedule.Execute(ApplyHolyWaterBarColor).ExecuteLater(1);
        }

        private void BindEvents()
        {
            if (holyWaterInventory != null)
            {
                holyWaterInventory.OnAmountChanged.AddListener(RefreshHolyWaterBar);
                holyWaterInventory.OnReplenished.AddListener(RefreshHolyWaterBar);
                holyWaterInventory.OnCriticalLevel.AddListener(RefreshHolyWaterBar);
                holyWaterInventory.OnEmpty.AddListener(RefreshHolyWaterBar);
            }

            if (holyTorch != null)
            {
                holyTorch.OnFuelChanged.AddListener(OnTorchFuelChanged);
                holyTorch.OnTorchLit.AddListener(RefreshTorchIndicator);
                holyTorch.OnTorchLow.AddListener(RefreshTorchIndicator);
                holyTorch.OnTorchExtinguished.AddListener(RefreshTorchIndicator);
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.AddListener(OnDayNightPhaseChanged);
            }
        }

        private void UnbindEvents()
        {
            if (holyWaterInventory != null)
            {
                holyWaterInventory.OnAmountChanged.RemoveListener(RefreshHolyWaterBar);
                holyWaterInventory.OnReplenished.RemoveListener(RefreshHolyWaterBar);
                holyWaterInventory.OnCriticalLevel.RemoveListener(RefreshHolyWaterBar);
                holyWaterInventory.OnEmpty.RemoveListener(RefreshHolyWaterBar);
            }

            if (holyTorch != null)
            {
                holyTorch.OnFuelChanged.RemoveListener(OnTorchFuelChanged);
                holyTorch.OnTorchLit.RemoveListener(RefreshTorchIndicator);
                holyTorch.OnTorchLow.RemoveListener(RefreshTorchIndicator);
                holyTorch.OnTorchExtinguished.RemoveListener(RefreshTorchIndicator);
            }

            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.RemoveListener(OnDayNightPhaseChanged);
            }
        }

        private void OnDayNightPhaseChanged(DayNightPhase phase)
        {
            RefreshNightDisplay(phase);
        }

        private void RefreshHolyWaterBar()
        {
            if (_holyWaterBar == null || holyWaterInventory == null)
            {
                return;
            }

            _holyWaterBar.value = holyWaterInventory.Current;
            _holyWaterBar.highValue = holyWaterInventory.MaxCapacity;
            ApplyHolyWaterBarColor();
        }

        private void OnTorchFuelChanged(float normalizedFuel)
        {
            RefreshTorchIndicator();
        }

        private void RefreshTorchIndicator()
        {
            if (_torchIndicator == null || holyTorch == null)
            {
                return;
            }

            _torchIndicator.text = FormatTorchFuel(holyTorch.FuelPercent);
        }

        private void RefreshNightDisplay(DayNightPhase phase)
        {
            if (_nightPhaseLabel != null)
            {
                _nightPhaseLabel.text = FormatNightPhase(phase);
            }

            if (_nightCountLabel != null && dayNightCycle != null)
            {
                _nightCountLabel.text = FormatNightCount(dayNightCycle.NightCycleCount);
            }
        }

        private void ApplyHolyWaterBarColor()
        {
            if (_holyWaterBar == null || holyWaterInventory == null)
            {
                return;
            }

            bool isLow = holyWaterInventory.FillPercent <= LowHolyWaterThreshold;
            Color fillColor = isLow ? HolyWaterWarning : HolyWaterGold;

            VisualElement progress = _holyWaterBar.Q(className: "unity-progress-bar__progress");
            if (progress != null)
            {
                progress.style.backgroundColor = fillColor;
            }

            VisualElement background = _holyWaterBar.Q(className: "unity-progress-bar__background");
            if (background != null)
            {
                background.style.backgroundColor = PanelBackground;
            }
        }
    }
}

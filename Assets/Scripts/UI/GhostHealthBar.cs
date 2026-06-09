using System.Collections;
using Ashlight.Ghost;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// World-space health bar that billboards above a ghost and reflects combat state.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostHealthBar : MonoBehaviour
    {
        private const float UpdateInterval = 0.1f;
        private const float HeadOffsetY = 2.5f;
        private const float WorldCanvasScale = 0.01f;
        private const float CanvasWidth = 200f;
        private const float CanvasHeight = 24f;

        private static readonly Color GreenHealth = new Color(0.2f, 0.85f, 0.3f, 1f);
        private static readonly Color YellowHealth = new Color(0.95f, 0.85f, 0.2f, 1f);
        private static readonly Color RedHealth = new Color(0.9f, 0.2f, 0.2f, 1f);

        [SerializeField] private GhostAIController ghostAI;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Canvas healthCanvas;
        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;

        private Coroutine _updateRoutine;
        private Transform _canvasTransform;

        /// <summary>
        /// Determines whether the health bar should be visible for a ghost AI state.
        /// </summary>
        /// <param name="state">Current ghost state.</param>
        /// <returns>True when the bar should be shown.</returns>
        public static bool ShouldShowForState(GhostState state)
        {
            switch (state)
            {
                case GhostState.Stalk:
                case GhostState.Chase:
                case GhostState.Attack:
                case GhostState.Retreat:
                case GhostState.Recharge:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Resolves fill color from a normalized health percentage.
        /// </summary>
        /// <param name="healthPercent">Health from 0 to 1.</param>
        /// <returns>Green, yellow, or red based on thresholds.</returns>
        public static Color GetColorForHealthPercent(float healthPercent)
        {
            if (healthPercent > 0.6f)
            {
                return GreenHealth;
            }

            if (healthPercent >= 0.3f)
            {
                return YellowHealth;
            }

            return RedHealth;
        }

        private void Awake()
        {
            if (ghostAI == null)
            {
                ghostAI = GetComponent<GhostAIController>();
            }

            if (ghostAI == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(GhostAIController)}.", this);
                enabled = false;
                return;
            }

            if (targetCamera == null)
            {
                targetCamera = Camera.main;
            }

            if (healthSlider == null || healthCanvas == null)
            {
                BuildHealthBarUI();
            }

            if (healthSlider == null || healthCanvas == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} failed to create UI references.", this);
                enabled = false;
                return;
            }

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
            _canvasTransform = healthCanvas.transform;
        }

        private void OnEnable()
        {
            if (ghostAI == null || healthSlider == null)
            {
                return;
            }

            _updateRoutine = StartCoroutine(UpdateHealthBarRoutine());
        }

        private void OnDisable()
        {
            if (_updateRoutine != null)
            {
                StopCoroutine(_updateRoutine);
                _updateRoutine = null;
            }
        }

        private void LateUpdate()
        {
            if (_canvasTransform == null || targetCamera == null)
            {
                return;
            }

            Vector3 lookDirection = _canvasTransform.position - targetCamera.transform.position;
            if (lookDirection.sqrMagnitude > 0.0001f)
            {
                _canvasTransform.rotation = Quaternion.LookRotation(lookDirection);
            }
        }

        /// <summary>Refreshes slider value, color, and visibility immediately.</summary>
        public void RefreshHealthBar()
        {
            if (ghostAI == null || healthSlider == null)
            {
                return;
            }

            float healthPercent = ghostAI.GhostHealthPercent;
            healthSlider.value = healthPercent;

            if (fillImage != null)
            {
                fillImage.color = GetColorForHealthPercent(healthPercent);
            }

            bool shouldShow = ShouldShowForState(ghostAI.CurrentState) && ghostAI.IsSpawnActive;
            if (healthCanvas != null)
            {
                healthCanvas.enabled = shouldShow;
            }
        }

        private IEnumerator UpdateHealthBarRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(UpdateInterval);

            while (enabled)
            {
                RefreshHealthBar();
                yield return wait;
            }
        }

        private void BuildHealthBarUI()
        {
            GameObject canvasObject = new GameObject("GhostHealthCanvas");
            canvasObject.transform.SetParent(transform, false);
            canvasObject.transform.localPosition = new Vector3(0f, HeadOffsetY, 0f);
            canvasObject.transform.localRotation = Quaternion.identity;
            canvasObject.transform.localScale = Vector3.one * WorldCanvasScale;

            healthCanvas = canvasObject.AddComponent<Canvas>();
            healthCanvas.renderMode = RenderMode.WorldSpace;
            healthCanvas.worldCamera = targetCamera;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            canvasObject.AddComponent<GraphicRaycaster>();

            RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(CanvasWidth, CanvasHeight);

            GameObject backgroundObject = new GameObject("Background");
            backgroundObject.transform.SetParent(canvasObject.transform, false);
            Image backgroundImage = backgroundObject.AddComponent<Image>();
            backgroundImage.color = new Color(0.05f, 0.05f, 0.05f, 0.85f);
            RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
            backgroundRect.anchorMin = Vector2.zero;
            backgroundRect.anchorMax = Vector2.one;
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            GameObject sliderObject = new GameObject("HealthSlider");
            sliderObject.transform.SetParent(canvasObject.transform, false);
            healthSlider = sliderObject.AddComponent<Slider>();
            RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
            sliderRect.anchorMin = Vector2.zero;
            sliderRect.anchorMax = Vector2.one;
            sliderRect.offsetMin = new Vector2(3f, 3f);
            sliderRect.offsetMax = new Vector2(-3f, -3f);

            GameObject fillAreaObject = new GameObject("Fill Area");
            fillAreaObject.transform.SetParent(sliderObject.transform, false);
            RectTransform fillAreaRect = fillAreaObject.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            GameObject fillObject = new GameObject("Fill");
            fillObject.transform.SetParent(fillAreaObject.transform, false);
            fillImage = fillObject.AddComponent<Image>();
            fillImage.color = GreenHealth;
            RectTransform fillRect = fillObject.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            healthSlider.fillRect = fillRect;
            healthSlider.targetGraphic = fillImage;
            healthCanvas.enabled = false;
        }
    }
}

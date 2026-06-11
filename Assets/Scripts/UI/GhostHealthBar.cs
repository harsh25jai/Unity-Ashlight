using System.Collections;
using Ashlight.Ghost;
using UnityEngine;
using UnityEngine.UI;

namespace Ashlight.UI
{
    /// <summary>
    /// World-space ghost health bar driven by a UI Slider with camera billboarding.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostHealthBar : MonoBehaviour
    {
        private const float UpdateInterval = 0.1f;

        [SerializeField] private Slider healthSlider;
        [SerializeField] private Image fillImage;
        [SerializeField] private GhostAIController ghostAI;
        [SerializeField] private Canvas healthBarCanvas;

        private Coroutine _updateRoutine;

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

            if (healthSlider == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Slider)}.", this);
                enabled = false;
                return;
            }

            if (fillImage == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Image)} fill image.", this);
                enabled = false;
                return;
            }

            if (healthBarCanvas == null)
            {
                Debug.LogError($"{nameof(GhostHealthBar)} requires a {nameof(Canvas)}.", this);
                enabled = false;
                return;
            }

            Debug.Log("GhostHealthBar initialized. Canvas: " +
                      (healthBarCanvas != null ? "found" : "NULL") +
                      ", HealthSlider: " + (healthSlider != null ? "found" : "NULL") +
                      ", FillImage: " + (fillImage != null ? "found" : "NULL") +
                      ", GhostAI: " + (ghostAI != null ? "found" : "NULL"));

            Debug.Log($"HealthBarCanvas active: {healthBarCanvas.gameObject.activeSelf}, " +
                      $"Canvas renderMode: {healthBarCanvas.renderMode}, " +
                      $"Slider: {(healthSlider != null ? "found" : "NULL")}, " +
                      $"Fill: {(fillImage != null ? "found" : "NULL")}");

            healthSlider.minValue = 0f;
            healthSlider.maxValue = 1f;
            healthSlider.interactable = false;
            healthSlider.value = 1f;
        }

        private void Start()
        {
            Debug.Log("GhostHealthBar coroutine starting");
            _updateRoutine = StartCoroutine(UpdateBar());

            Debug.Log($"GhostHealthBar Start — canvas position: " +
                      $"{healthBarCanvas.transform.position}, " +
                      $"world scale: {healthBarCanvas.transform.lossyScale}");
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
            if (Camera.main == null)
            {
                return;
            }

            transform.LookAt(
                transform.position + Camera.main.transform.rotation * Vector3.forward,
                Camera.main.transform.rotation * Vector3.up);
        }

        private IEnumerator UpdateBar()
        {
            WaitForSeconds wait = new WaitForSeconds(UpdateInterval);

            while (true)
            {
                if (ghostAI == null || healthSlider == null || fillImage == null || healthBarCanvas == null)
                {
                    yield return wait;
                    continue;
                }

                Debug.Log($"UpdateBar tick — health: {ghostAI.GhostHealthPercent:F2}, " +
                          $"slider value: {healthSlider.value:F2}, " +
                          $"canvas active: {healthBarCanvas.gameObject.activeSelf}, " +
                          $"canvas pos: {healthBarCanvas.transform.position}");

                float healthPercent = ghostAI.GhostHealthPercent;

                healthSlider.value = healthPercent;

                if (healthPercent > 0.6f)
                {
                    fillImage.color = new Color(0.298f, 0.686f, 0.314f);
                }
                else if (healthPercent > 0.3f)
                {
                    fillImage.color = new Color(1f, 0.757f, 0.027f);
                }
                else
                {
                    fillImage.color = new Color(0.957f, 0.263f, 0.212f);
                }

                healthBarCanvas.gameObject.SetActive(true);

                yield return wait;
            }
        }
    }
}

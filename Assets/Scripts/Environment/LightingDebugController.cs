using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Ashlight.Environment
{
    /// <summary>
    /// Debug shortcuts that jump <see cref="DayNightCycle"/> to phase midpoints and log lighting state.
    /// </summary>
    [DisallowMultipleComponent]
    public class LightingDebugController : MonoBehaviour
    {
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private Material skyboxMaterial;

        private void Awake()
        {
            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }

            if (skyboxMaterial == null)
            {
                skyboxMaterial = RenderSettings.skybox;
            }

            if (dayNightCycle == null)
            {
                Debug.LogWarning($"{nameof(LightingDebugController)} has no {nameof(DayNightCycle)} assigned.", this);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (dayNightCycle == null)
            {
                if (keyboard.f12Key.wasPressedThisFrame)
                {
                    LogLightingDiagnostic();
                }

                return;
            }

            if (keyboard.f1Key.wasPressedThisFrame)
            {
                JumpToPhase(DayNightPhase.Day, 0.5f, "Day midpoint");
            }

            if (keyboard.f2Key.wasPressedThisFrame)
            {
                JumpToPhase(DayNightPhase.Dusk, 0.5f, "Dusk midpoint");
            }

            if (keyboard.f3Key.wasPressedThisFrame)
            {
                JumpToPhase(DayNightPhase.Night_Early, 0.5f, "Night Early midpoint");
            }

            if (keyboard.f4Key.wasPressedThisFrame)
            {
                JumpToPhase(DayNightPhase.Night_Deep, 0.75f, "Night Deep midpoint");
            }

            if (keyboard.f5Key.wasPressedThisFrame)
            {
                JumpToPhase(DayNightPhase.Dawn, 0.5f, "Dawn midpoint");
            }

            if (keyboard.f6Key.wasPressedThisFrame)
            {
                dayNightCycle.pauseCycleForDebug = false;
                Debug.Log("LIGHTING DEBUG: Cycle resumed");
            }

            if (keyboard.f12Key.wasPressedThisFrame)
            {
                LogLightingDiagnostic();
            }
        }

        private void JumpToPhase(DayNightPhase phase, float phaseFraction, string label)
        {
            float normalizedTime = dayNightCycle.GetPhaseNormalizedTime(phase, phaseFraction);
            dayNightCycle.pauseCycleForDebug = true;
            dayNightCycle.SetNormalizedTime(normalizedTime);
            Debug.Log($"LIGHTING DEBUG: Jumped to {label} (t={normalizedTime:F2}) — cycle paused");
        }

        /// <summary>Logs all active lighting settings to the console for diagnosis.</summary>
        public void LogLightingDiagnostic()
        {
            Light directionalLight = dayNightCycle != null
                ? dayNightCycle.DirectionalLight
                : RenderSettings.sun;
            Volume globalVolume = FindAnyObjectByType<Volume>();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("═══ LIGHTING DIAGNOSTIC ═══");

            if (directionalLight != null)
            {
                sb.AppendLine($"DirectionalLight.intensity:      {directionalLight.intensity}");
                sb.AppendLine($"DirectionalLight.color:          {directionalLight.color}");
                sb.AppendLine($"DirectionalLight.enabled:          {directionalLight.enabled}");
                sb.AppendLine($"DirectionalLight.shadowStrength:   {directionalLight.shadowStrength}");
                sb.AppendLine($"DirectionalLight.type:             {directionalLight.type}");
                sb.AppendLine($"DirectionalLight.rotation: {directionalLight.transform.rotation.eulerAngles}");

                if (dayNightCycle != null)
                {
                    sb.AppendLine(
                        $"CelestialElevation at t={dayNightCycle.NormalizedDayTime:F2}: " +
                        $"{dayNightCycle.GetSunElevation():F1} degrees (single sun/moon light)");
                }
            }
            else
            {
                sb.AppendLine("DirectionalLight: NULL — not assigned");
            }

            sb.AppendLine($"RenderSettings.fog:                {RenderSettings.fog}");
            sb.AppendLine($"RenderSettings.fogDensity:         {RenderSettings.fogDensity}");
            sb.AppendLine($"RenderSettings.fogColor:           {RenderSettings.fogColor}");
            sb.AppendLine($"RenderSettings.fogMode:            {RenderSettings.fogMode}");
            sb.AppendLine($"RenderSettings.ambientMode:        {RenderSettings.ambientMode}");
            sb.AppendLine($"RenderSettings.ambientLight:       {RenderSettings.ambientLight}");
            sb.AppendLine($"RenderSettings.ambientIntensity:   {RenderSettings.ambientIntensity}");
            sb.AppendLine($"RenderSettings.skybox:             {(RenderSettings.skybox != null ? RenderSettings.skybox.name : "NULL")}");

            if (globalVolume != null)
            {
                sb.AppendLine($"GlobalVolume.enabled:              {globalVolume.enabled}");
                sb.AppendLine($"GlobalVolume.weight:               {globalVolume.weight}");
                sb.AppendLine($"GlobalVolume.isGlobal:             {globalVolume.isGlobal}");
                sb.AppendLine($"GlobalVolume.profile:              {(globalVolume.profile != null ? globalVolume.profile.name : "NULL")}");

                if (globalVolume.profile != null)
                {
                    if (globalVolume.profile.TryGet(out Bloom bloom))
                    {
                        sb.AppendLine($"Bloom.active:                      {bloom.active}");
                        sb.AppendLine($"Bloom.intensity.value:             {bloom.intensity.value}");
                        sb.AppendLine($"Bloom.threshold.value:             {bloom.threshold.value}");
                    }
                    else
                    {
                        sb.AppendLine("Bloom: NOT FOUND in profile");
                    }

                    if (globalVolume.profile.TryGet(out Vignette vignette))
                    {
                        sb.AppendLine($"Vignette.active:                   {vignette.active}");
                        sb.AppendLine($"Vignette.intensity.value:          {vignette.intensity.value}");
                    }
                    else
                    {
                        sb.AppendLine("Vignette: NOT FOUND in profile");
                    }

                    if (globalVolume.profile.TryGet(out Tonemapping tonemapping))
                    {
                        sb.AppendLine($"Tonemapping.active:                {tonemapping.active}");
                        sb.AppendLine($"Tonemapping.mode.value:            {tonemapping.mode.value}");
                    }
                    else
                    {
                        sb.AppendLine("Tonemapping: NOT FOUND in profile");
                    }

                    if (globalVolume.profile.TryGet(out ColorAdjustments colorAdjustments))
                    {
                        sb.AppendLine($"ColorAdjustments.active:           {colorAdjustments.active}");
                        sb.AppendLine($"ColorAdjustments.postExposure:     {colorAdjustments.postExposure.value}");
                        sb.AppendLine($"ColorAdjustments.contrast:         {colorAdjustments.contrast.value}");
                        sb.AppendLine($"ColorAdjustments.saturation:       {colorAdjustments.colorFilter.value}");
                    }
                    else
                    {
                        sb.AppendLine("ColorAdjustments: NOT FOUND in profile");
                    }
                }
            }
            else
            {
                sb.AppendLine("GlobalVolume: NULL — not assigned");
            }

            UniversalRenderPipelineAsset urpAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            if (urpAsset != null)
            {
                sb.AppendLine($"URP.HDR:                           {urpAsset.supportsHDR}");
                sb.AppendLine($"URP.MainLightRenderingMode:         {urpAsset.mainLightRenderingMode}");
                sb.AppendLine($"URP.ShadowDistance:                {urpAsset.shadowDistance}");
            }
            else
            {
                sb.AppendLine("URP Asset: NULL or not URP project");
            }

            if (dayNightCycle != null)
            {
                sb.AppendLine($"DayNightCycle.NormalizedTime:      {dayNightCycle.NormalizedDayTime}");
                sb.AppendLine($"DayNightCycle.CurrentPhase:        {dayNightCycle.CurrentPhase}");
                sb.AppendLine($"DayNightCycle.TimeScale:           {dayNightCycle.TimeScale}");
                sb.AppendLine($"DayNightCycle.pauseCycleForDebug:  {dayNightCycle.pauseCycleForDebug}");
            }
            else
            {
                sb.AppendLine("DayNightCycle: NULL — not assigned");
            }

            sb.AppendLine("═══════════════════════════");
            Debug.Log(sb.ToString(), this);
        }
    }
}

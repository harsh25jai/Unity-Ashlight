using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Built lighting curves for a single cycle configuration.
    /// </summary>
    public struct DayNightLightingCurves
    {
        public AnimationCurve LightIntensity;
        public Gradient LightColor;
        public Gradient AmbientColor;
        public AnimationCurve FogDensity;
        public Gradient FogColor;
        public AnimationCurve SkyboxExposure;
        public AnimationCurve SunElevation;
        public AnimationCurve SunAzimuth;
    }

    /// <summary>
    /// Authoring data for day/night phase durations and lighting endpoints.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "Ashlight/Environment/Day Night Config")]
    public class DayNightConfig : ScriptableObject
    {
        private const float AuthoredDayLightIntensity = 1.25f;
        private const float AuthoredDayPeakIntensity = 1.80f;
        private const float AuthoredNightLightIntensity = 0.15f;
        private const float AuthoredNightDeepIntensity = 0.02f;

        [Header("Phase Durations (seconds)")]
        [SerializeField] private float dayDuration = 120f;
        [SerializeField] private float duskDuration = 120f;
        [SerializeField] private float nightEarlyDuration = 120f;
        [SerializeField] private float nightDeepDuration = 300f;
        [SerializeField] private float dawnDuration = 120f;

        [Header("Night Growth")]
        [SerializeField] private float nightGrowthMultiplier = 1.15f;

        [Header("Directional Light")]
        [SerializeField] private float dayLightIntensity = 1.25f;
        [SerializeField] private float nightLightIntensity = 0.15f;

        [Header("Fog")]
        [SerializeField] private float dayFogDensity = 0.01f;
        [SerializeField] private float nightFogDensity = 0.06f;

        [Header("Ambient")]
        [SerializeField] private Color dayAmbientColor = new Color(0.55f, 0.58f, 0.65f);
        [SerializeField] private Color nightAmbientColor = new Color(0.04f, 0.05f, 0.12f);

        /// <summary>Gets the Day phase duration in seconds.</summary>
        public float DayDuration => dayDuration;

        /// <summary>Gets the Dusk phase duration in seconds.</summary>
        public float DuskDuration => duskDuration;

        /// <summary>Gets the early Night phase duration in seconds.</summary>
        public float NightEarlyDuration => nightEarlyDuration;

        /// <summary>Gets the base Night_Deep phase duration in seconds before growth scaling.</summary>
        public float NightDeepDuration => nightDeepDuration;

        /// <summary>Gets the Dawn phase duration in seconds.</summary>
        public float DawnDuration => dawnDuration;

        /// <summary>Gets the per-cycle multiplier applied to Night_Deep duration.</summary>
        public float NightGrowthMultiplier => nightGrowthMultiplier;

        /// <summary>Gets the directional light intensity during Day.</summary>
        public float DayLightIntensity => dayLightIntensity;

        /// <summary>Gets the directional light intensity during Night.</summary>
        public float NightLightIntensity => nightLightIntensity;

        /// <summary>Gets the fog density during Day.</summary>
        public float DayFogDensity => dayFogDensity;

        /// <summary>Gets the fog density during Night.</summary>
        public float NightFogDensity => nightFogDensity;

        /// <summary>Gets the flat ambient color during Day.</summary>
        public Color DayAmbientColor => dayAmbientColor;

        /// <summary>Gets the flat ambient color during Night.</summary>
        public Color NightAmbientColor => nightAmbientColor;

        /// <summary>Gets the total cycle duration for the given completed night count.</summary>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Total cycle length in seconds.</returns>
        public float GetTotalCycleDuration(int nightCycleCount)
        {
            return dayDuration
                + duskDuration
                + nightEarlyDuration
                + GetScaledNightDeepDuration(nightCycleCount)
                + dawnDuration;
        }

        /// <summary>Gets the scaled Night_Deep duration for the given completed night count.</summary>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Night_Deep duration in seconds.</returns>
        public float GetScaledNightDeepDuration(int nightCycleCount)
        {
            return nightDeepDuration * Mathf.Pow(nightGrowthMultiplier, nightCycleCount);
        }

        /// <summary>Gets the duration of a single phase in seconds.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Phase duration in seconds.</returns>
        public float GetPhaseDuration(DayNightPhase phase, int nightCycleCount)
        {
            switch (phase)
            {
                case DayNightPhase.Day:
                    return dayDuration;
                case DayNightPhase.Dusk:
                    return duskDuration;
                case DayNightPhase.Night_Early:
                    return nightEarlyDuration;
                case DayNightPhase.Night_Deep:
                    return GetScaledNightDeepDuration(nightCycleCount);
                case DayNightPhase.Dawn:
                    return dawnDuration;
                default:
                    return 0f;
            }
        }

        /// <summary>Gets the normalized cycle time where a phase begins.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Normalized time from 0 to 1.</returns>
        public float GetPhaseStartNormalized(DayNightPhase phase, int nightCycleCount)
        {
            float total = GetTotalCycleDuration(nightCycleCount);
            if (total <= 0f)
            {
                return 0f;
            }

            float elapsed = 0f;
            DayNightPhase[] phases =
            {
                DayNightPhase.Day,
                DayNightPhase.Dusk,
                DayNightPhase.Night_Early,
                DayNightPhase.Night_Deep,
                DayNightPhase.Dawn
            };

            foreach (DayNightPhase currentPhase in phases)
            {
                if (currentPhase == phase)
                {
                    return elapsed / total;
                }

                elapsed += GetPhaseDuration(currentPhase, nightCycleCount);
            }

            return 0f;
        }

        /// <summary>Gets the normalized cycle time where a phase ends.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Normalized time from 0 to 1.</returns>
        public float GetPhaseEndNormalized(DayNightPhase phase, int nightCycleCount)
        {
            float total = GetTotalCycleDuration(nightCycleCount);
            if (total <= 0f)
            {
                return 0f;
            }

            return GetPhaseStartNormalized(phase, nightCycleCount)
                + GetPhaseDuration(phase, nightCycleCount) / total;
        }

        /// <summary>Gets the normalized midpoint of a phase.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Normalized time from 0 to 1.</returns>
        public float GetPhaseMidpointNormalized(DayNightPhase phase, int nightCycleCount)
        {
            return GetPhaseNormalizedTime(phase, 0.5f, nightCycleCount);
        }

        /// <summary>Gets a normalized cycle time at a fraction through a phase.</summary>
        /// <param name="phase">Target phase.</param>
        /// <param name="phaseFraction">Progress through the phase from 0 to 1.</param>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Normalized time from 0 to 1.</returns>
        public float GetPhaseNormalizedTime(DayNightPhase phase, float phaseFraction, int nightCycleCount)
        {
            float start = GetPhaseStartNormalized(phase, nightCycleCount);
            float duration = GetPhaseDuration(phase, nightCycleCount);
            float total = GetTotalCycleDuration(nightCycleCount);
            if (total <= 0f)
            {
                return 0f;
            }

            return start + duration * Mathf.Clamp01(phaseFraction) / total;
        }

        /// <summary>
        /// Builds lighting curves keyed to the configured phase durations.
        /// </summary>
        /// <param name="nightCycleCount">Completed night cycles.</param>
        /// <returns>Curve set for the cycle.</returns>
        public DayNightLightingCurves BuildLightingCurves(int nightCycleCount)
        {
            float total = GetTotalCycleDuration(nightCycleCount);
            if (total <= 0f)
            {
                total = 1f;
            }

            float invTotal = 1f / total;
            float dayStart = 0f;
            float dayEnd = dayDuration * invTotal;
            float duskEnd = (dayDuration + duskDuration) * invTotal;
            float nightEarlyEnd = (dayDuration + duskDuration + nightEarlyDuration) * invTotal;
            float scaledNightDeep = GetScaledNightDeepDuration(nightCycleCount);
            float nightDeepEnd = (dayDuration + duskDuration + nightEarlyDuration + scaledNightDeep) * invTotal;
            float dawnEnd = 1f;

            float AtDay(float fraction) => (dayDuration * fraction) * invTotal;
            float AtDusk(float fraction) => dayEnd + (duskDuration * fraction) * invTotal;
            float AtNightEarly(float fraction) => (dayDuration + duskDuration + nightEarlyDuration * fraction) * invTotal;
            float AtNightDeep(float fraction) =>
                (dayDuration + duskDuration + nightEarlyDuration + scaledNightDeep * fraction) * invTotal;
            float AtDawn(float fraction) =>
                (dayDuration + duskDuration + nightEarlyDuration + scaledNightDeep + dawnDuration * fraction) * invTotal;

            float dayPeak = dayLightIntensity * (AuthoredDayPeakIntensity / AuthoredDayLightIntensity);
            float nightDeepIntensity = nightLightIntensity * (AuthoredNightDeepIntensity / AuthoredNightLightIntensity);
            float dawnIntensity = nightLightIntensity * 0.33f;

            AnimationCurve lightIntensity = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(dayStart, dawnIntensity),
                new Keyframe(AtDay(0.08f), dayLightIntensity * 0.64f),
                new Keyframe(AtDay(0.15f), dayPeak),
                new Keyframe(AtDay(0.95f), dayPeak),
                new Keyframe(dayEnd, dayPeak * 0.5f),
                new Keyframe(AtDusk(0.50f), dayLightIntensity * 0.16f),
                new Keyframe(duskEnd, dayLightIntensity * 0.16f),
                new Keyframe(AtNightEarly(0.50f), nightLightIntensity),
                new Keyframe(nightEarlyEnd, nightLightIntensity),
                new Keyframe(AtNightDeep(0.50f), nightDeepIntensity),
                new Keyframe(nightDeepEnd, nightDeepIntensity),
                new Keyframe(AtDawn(0.95f), dawnIntensity),
                new Keyframe(dawnEnd, dawnIntensity)
            });
            SmoothCurve(lightIntensity, 0.5f);

            Gradient lightColor = new Gradient();
            lightColor.SetKeys(
                new[]
                {
                    new GradientColorKey(HexColor("1A2040"), dayStart),
                    new GradientColorKey(HexColor("FF7043"), AtDay(0.10f)),
                    new GradientColorKey(HexColor("FFF5E0"), AtDay(0.15f)),
                    new GradientColorKey(HexColor("FFF5E0"), AtDay(0.95f)),
                    new GradientColorKey(HexColor("FF8C42"), AtDusk(0.50f)),
                    new GradientColorKey(HexColor("4A3060"), duskEnd),
                    new GradientColorKey(HexColor("1A1F3A"), AtNightDeep(0.50f)),
                    new GradientColorKey(HexColor("1A2040"), dawnEnd)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            Gradient ambientColor = new Gradient();
            ambientColor.SetKeys(
                new[]
                {
                    new GradientColorKey(nightAmbientColor, dayStart),
                    new GradientColorKey(LerpColor(nightAmbientColor, dayAmbientColor, 0.35f), AtDay(0.10f)),
                    new GradientColorKey(dayAmbientColor, AtDay(0.20f)),
                    new GradientColorKey(dayAmbientColor, AtDay(0.95f)),
                    new GradientColorKey(LerpColor(dayAmbientColor, nightAmbientColor, 0.5f), AtDusk(0.50f)),
                    new GradientColorKey(nightAmbientColor, duskEnd),
                    new GradientColorKey(nightAmbientColor, nightDeepEnd),
                    new GradientColorKey(nightAmbientColor, dawnEnd)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            AnimationCurve fogDensity = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(dayStart, dayFogDensity),
                new Keyframe(AtDay(0.20f), dayFogDensity * 0.6f),
                new Keyframe(AtDay(0.95f), dayFogDensity * 0.6f),
                new Keyframe(dayEnd, dayFogDensity * 1.2f),
                new Keyframe(duskEnd, dayFogDensity * 1.2f),
                new Keyframe(nightEarlyEnd, nightFogDensity * 0.42f),
                new Keyframe(AtNightDeep(0.50f), nightFogDensity * 0.67f),
                new Keyframe(AtNightDeep(0.95f), nightFogDensity),
                new Keyframe(dawnEnd, dayFogDensity)
            });
            SmoothCurve(fogDensity, 0.5f);

            Gradient fogColor = new Gradient();
            fogColor.SetKeys(
                new[]
                {
                    new GradientColorKey(HexColor("1A2A1A"), dayStart),
                    new GradientColorKey(HexColor("B0C4B8"), AtDay(0.15f)),
                    new GradientColorKey(HexColor("C0CDCA"), AtDay(0.20f)),
                    new GradientColorKey(HexColor("C0CDCA"), AtDay(0.95f)),
                    new GradientColorKey(HexColor("C87941"), AtDusk(0.50f)),
                    new GradientColorKey(HexColor("0D0F1E"), duskEnd),
                    new GradientColorKey(HexColor("060810"), AtNightDeep(0.95f)),
                    new GradientColorKey(HexColor("1A2A1A"), dawnEnd)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            AnimationCurve skyboxExposure = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(dayStart, 0.1f),
                new Keyframe(AtDay(0.08f), 0.8f),
                new Keyframe(AtDay(0.15f), 1.2f),
                new Keyframe(AtDay(0.95f), 1.2f),
                new Keyframe(AtDusk(0.50f), 0.6f),
                new Keyframe(duskEnd, 0.2f),
                new Keyframe(nightEarlyEnd, 0.02f),
                new Keyframe(AtNightDeep(0.95f), 0.02f),
                new Keyframe(dawnEnd, 0.1f)
            });
            SmoothCurve(skyboxExposure, 0.5f);

            AnimationCurve sunElevation = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(dayStart, 5f),
                new Keyframe(AtDay(0.08f), 15f),
                new Keyframe(AtDay(0.15f), 35f),
                new Keyframe(AtDay(0.50f), 75f),
                new Keyframe(AtDay(0.75f), 65f),
                new Keyframe(AtDay(0.95f), 30f),
                new Keyframe(AtDusk(0.50f), 12f),
                new Keyframe(duskEnd, 2f),
                new Keyframe(AtNightEarly(0.15f), 35f),
                new Keyframe(AtNightDeep(0.50f), 45f),
                new Keyframe(AtDawn(0.50f), 20f),
                new Keyframe(dawnEnd, 5f)
            });
            SmoothCurve(sunElevation, 0.3f);

            AnimationCurve sunAzimuth = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(dayStart, 80f),
                new Keyframe(AtDay(0.08f), 90f),
                new Keyframe(AtDay(0.50f), 160f),
                new Keyframe(AtDay(0.75f), 200f),
                new Keyframe(AtDay(0.95f), 250f),
                new Keyframe(AtDusk(0.50f), 265f),
                new Keyframe(duskEnd, 275f),
                new Keyframe(AtNightEarly(0.15f), 10f),
                new Keyframe(AtNightDeep(0.50f), 350f),
                new Keyframe(AtDawn(0.50f), 330f),
                new Keyframe(dawnEnd, 80f)
            });
            SmoothCurve(sunAzimuth, 0.3f);

            return new DayNightLightingCurves
            {
                LightIntensity = lightIntensity,
                LightColor = lightColor,
                AmbientColor = ambientColor,
                FogDensity = fogDensity,
                FogColor = fogColor,
                SkyboxExposure = skyboxExposure,
                SunElevation = sunElevation,
                SunAzimuth = sunAzimuth
            };
        }

        private static void SmoothCurve(AnimationCurve curve, float weight)
        {
            for (int i = 0; i < curve.length; i++)
            {
                curve.SmoothTangents(i, weight);
            }
        }

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString("#" + hex, out Color color))
            {
                return color;
            }

            return Color.white;
        }

        private static Color LerpColor(Color a, Color b, float t)
        {
            return Color.Lerp(a, b, Mathf.Clamp01(t));
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            dayDuration = Mathf.Max(1f, dayDuration);
            duskDuration = Mathf.Max(1f, duskDuration);
            nightEarlyDuration = Mathf.Max(1f, nightEarlyDuration);
            nightDeepDuration = Mathf.Max(1f, nightDeepDuration);
            dawnDuration = Mathf.Max(1f, dawnDuration);
            nightGrowthMultiplier = Mathf.Max(1f, nightGrowthMultiplier);
            dayLightIntensity = Mathf.Max(0f, dayLightIntensity);
            nightLightIntensity = Mathf.Max(0f, nightLightIntensity);
            dayFogDensity = Mathf.Max(0f, dayFogDensity);
            nightFogDensity = Mathf.Max(0f, nightFogDensity);
        }
#endif
    }
}

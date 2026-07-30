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
        /// <summary>Single directional-light elevation in degrees (sun by day, moon by night).</summary>
        public AnimationCurve SunElevation;

        /// <summary>Single directional-light azimuth in degrees, stored unwrapped for smooth interpolation.</summary>
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
            float dawnIntensity = nightLightIntensity * 0.33f;
            float dayPeak = dayLightIntensity * (AuthoredDayPeakIntensity / AuthoredDayLightIntensity);
            float duskIntensity = Mathf.Lerp(nightLightIntensity, dayPeak, 0.28f);
            float nightEarlyIntensity = nightLightIntensity;
            float nightDeepIntensity = nightLightIntensity * (AuthoredNightDeepIntensity / AuthoredNightLightIntensity);

            float dayMid = GetPhaseMidpointNormalized(DayNightPhase.Day, nightCycleCount);
            float plateauBefore = Mathf.Clamp(dayMid - 0.05f, 0f, 1f);
            float plateauAfter = Mathf.Clamp(dayMid + 0.05f, 0f, 1f);
            float duskMidDecline = GetPhaseNormalizedTime(DayNightPhase.Dusk, 0.6f, nightCycleCount);
            float nightEarlyStart = GetPhaseStartNormalized(DayNightPhase.Night_Early, nightCycleCount);
            float nightDeepMid = GetPhaseMidpointNormalized(DayNightPhase.Night_Deep, nightCycleCount);
            float dawnPhaseStart = GetPhaseStartNormalized(DayNightPhase.Dawn, nightCycleCount);
            float dawnRiseValue = Mathf.Lerp(nightDeepIntensity, dawnIntensity, 0.55f);
            const float loopEpsilon = 0.0001f;
            float beforeLoop = 1f - loopEpsilon;

            Keyframe[] intensityKeys =
            {
                CreateKeyframe(0f, dawnIntensity),
                CreateKeyframe(plateauBefore, dayLightIntensity),
                CreateKeyframe(plateauAfter, dayPeak),
                CreateKeyframe(duskMidDecline, duskIntensity),
                CreateKeyframe(nightEarlyStart, nightEarlyIntensity),
                CreateKeyframe(nightDeepMid, nightDeepIntensity),
                CreateKeyframe(dawnPhaseStart, dawnRiseValue),
                CreateKeyframe(beforeLoop, dawnIntensity),
                CreateKeyframe(1f, dawnIntensity)
            };

            int intensityPeakIndex = 2;
            int intensityTroughIndex = 5;
            ApplyMonotonicTangents(intensityKeys, intensityPeakIndex, intensityTroughIndex);
            AnimationCurve lightIntensity = new AnimationCurve(intensityKeys);

            float fogMinimum = dayFogDensity * 0.6f;
            float fogDuskValue = Mathf.Lerp(fogMinimum, nightFogDensity, 0.35f);
            float fogNightEarlyValue = Mathf.Lerp(fogDuskValue, nightFogDensity, 0.65f);
            float fogDawnValue = Mathf.Lerp(nightFogDensity, dayFogDensity, 0.45f);

            Keyframe[] fogKeys =
            {
                CreateKeyframe(0f, dayFogDensity),
                CreateKeyframe(plateauBefore, fogMinimum),
                CreateKeyframe(plateauAfter, fogMinimum),
                CreateKeyframe(duskMidDecline, fogDuskValue),
                CreateKeyframe(nightEarlyStart, fogNightEarlyValue),
                CreateKeyframe(nightDeepMid, nightFogDensity),
                CreateKeyframe(dawnPhaseStart, fogDawnValue),
                CreateKeyframe(beforeLoop, dayFogDensity),
                CreateKeyframe(1f, dayFogDensity)
            };

            int fogMinimumIndex = 2;
            int fogMaximumIndex = 5;
            ApplyMonotonicTangents(fogKeys, fogMaximumIndex, fogMinimumIndex);
            AnimationCurve fogDensity = new AnimationCurve(fogKeys);

            ValidateCurveMonotonicity(
                lightIntensity,
                "Light Intensity",
                new MonotonicRegion(0f, plateauAfter, shouldRise: true),
                new MonotonicRegion(plateauAfter, nightDeepMid, shouldRise: false),
                new MonotonicRegion(nightDeepMid, 1f, shouldRise: true));

            ValidateCurveMonotonicity(
                fogDensity,
                "Fog Density",
                new MonotonicRegion(0f, plateauAfter, shouldRise: false),
                new MonotonicRegion(plateauAfter, nightDeepMid, shouldRise: true),
                new MonotonicRegion(nightDeepMid, 1f, shouldRise: false));

            Gradient lightColor = BuildLuminanceGradient(
                new[]
                {
                    (0f, HexColor("1A2040")),
                    (plateauBefore, HexColor("FF7043")),
                    (plateauAfter, HexColor("FFF5E0")),
                    (duskMidDecline, HexColor("FF8C42")),
                    (nightEarlyStart, HexColor("4A3060")),
                    (nightDeepMid, HexColor("1A1F3A")),
                    (dawnPhaseStart, HexColor("243060")),
                    (1f, HexColor("1A2040"))
                },
                lightIntensity);

            Gradient ambientColor = BuildLuminanceGradient(
                new[]
                {
                    (0f, nightAmbientColor),
                    (plateauBefore, LerpColor(nightAmbientColor, dayAmbientColor, 0.35f)),
                    (plateauAfter, dayAmbientColor),
                    (duskMidDecline, LerpColor(dayAmbientColor, nightAmbientColor, 0.55f)),
                    (nightEarlyStart, nightAmbientColor),
                    (nightDeepMid, nightAmbientColor * 0.85f),
                    (dawnPhaseStart, LerpColor(nightAmbientColor, dayAmbientColor, 0.25f)),
                    (1f, nightAmbientColor)
                },
                lightIntensity);

            ValidateGradientLuminanceMonotonicity(
                lightColor,
                "Light Color",
                new MonotonicRegion(0f, plateauAfter, shouldRise: true),
                new MonotonicRegion(plateauAfter, nightDeepMid, shouldRise: false),
                new MonotonicRegion(nightDeepMid, 1f, shouldRise: true));

            ValidateGradientLuminanceMonotonicity(
                ambientColor,
                "Ambient Color",
                new MonotonicRegion(0f, plateauAfter, shouldRise: true),
                new MonotonicRegion(plateauAfter, nightDeepMid, shouldRise: false),
                new MonotonicRegion(nightDeepMid, 1f, shouldRise: true));

            Gradient fogColor = new Gradient();
            fogColor.SetKeys(
                new[]
                {
                    new GradientColorKey(HexColor("1A2A1A"), 0f),
                    new GradientColorKey(HexColor("B0C4B8"), plateauBefore),
                    new GradientColorKey(HexColor("C0CDCA"), plateauAfter),
                    new GradientColorKey(HexColor("C87941"), duskMidDecline),
                    new GradientColorKey(HexColor("0D0F1E"), nightEarlyStart),
                    new GradientColorKey(HexColor("060810"), nightDeepMid),
                    new GradientColorKey(HexColor("142018"), dawnPhaseStart),
                    new GradientColorKey(HexColor("1A2A1A"), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            float total = GetTotalCycleDuration(nightCycleCount);
            if (total <= 0f)
            {
                total = 1f;
            }

            float invTotal = 1f / total;
            float dayEnd = dayDuration * invTotal;
            float duskEnd = (dayDuration + duskDuration) * invTotal;
            float nightEarlyEnd = (dayDuration + duskDuration + nightEarlyDuration) * invTotal;
            float scaledNightDeep = GetScaledNightDeepDuration(nightCycleCount);
            float dawnEnd = 1f;

            float AtDay(float fraction) => (dayDuration * fraction) * invTotal;
            float AtDusk(float fraction) => dayEnd + (duskDuration * fraction) * invTotal;
            float AtNightEarly(float fraction) => (dayDuration + duskDuration + nightEarlyDuration * fraction) * invTotal;
            float AtNightDeep(float fraction) =>
                (dayDuration + duskDuration + nightEarlyDuration + scaledNightDeep * fraction) * invTotal;
            float AtDawn(float fraction) =>
                (dayDuration + duskDuration + nightEarlyDuration + scaledNightDeep + dawnDuration * fraction) * invTotal;

            AnimationCurve skyboxExposure = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0f, 0.1f),
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

            const float dawnElevation = 8f;
            const float daySunPeakElevation = 72f;
            const float sunsetElevation = 4f;
            const float moonPeakElevation = 16f;
            const float dawnAzimuth = 70f;
            const float azimuthCycleOffset = 360f;

            Keyframe[] celestialElevationKeys =
            {
                CreateKeyframe(0f, dawnElevation),
                CreateKeyframe(plateauBefore, 52f),
                CreateKeyframe(plateauAfter, daySunPeakElevation),
                CreateKeyframe(duskMidDecline, 22f),
                CreateKeyframe(nightEarlyStart, sunsetElevation),
                CreateKeyframe(nightDeepMid, moonPeakElevation),
                CreateKeyframe(dawnPhaseStart, 10f),
                CreateKeyframe(beforeLoop, dawnElevation),
                CreateKeyframe(1f, dawnElevation)
            };

            const int sunPeakIndex = 2;
            const int sunsetIndex = 4;
            const int moonPeakIndex = 5;
            ApplyMultiExtremaTangents(celestialElevationKeys, sunPeakIndex, sunsetIndex, moonPeakIndex);
            AnimationCurve sunElevation = new AnimationCurve(celestialElevationKeys);

            Keyframe[] celestialAzimuthKeys =
            {
                CreateKeyframe(0f, dawnAzimuth),
                CreateKeyframe(plateauBefore, 140f),
                CreateKeyframe(plateauAfter, 175f),
                CreateKeyframe(duskMidDecline, 248f),
                CreateKeyframe(nightEarlyStart, 272f),
                CreateKeyframe(nightDeepMid, 335f),
                CreateKeyframe(dawnPhaseStart, dawnAzimuth + 335f),
                CreateKeyframe(beforeLoop, dawnAzimuth + azimuthCycleOffset - 2f),
                CreateKeyframe(1f, dawnAzimuth + azimuthCycleOffset)
            };

            ApplyRisingTangents(celestialAzimuthKeys);
            AnimationCurve sunAzimuth = new AnimationCurve(celestialAzimuthKeys);

            ValidateCelestialElevation(sunElevation, nightEarlyStart, dawnPhaseStart, moonPeakElevation);

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

        private static Keyframe CreateKeyframe(float time, float value)
        {
            return new Keyframe(time, value, 0f, 0f);
        }

        private static void ApplyMonotonicTangents(Keyframe[] keys, int zeroTangentMaxIndex, int zeroTangentMinIndex)
        {
            ApplyMultiExtremaTangents(keys, zeroTangentMaxIndex, zeroTangentMinIndex);
        }

        private static void ApplyMultiExtremaTangents(Keyframe[] keys, params int[] zeroTangentIndices)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                if (IsZeroTangentIndex(i, zeroTangentIndices))
                {
                    keys[i].inTangent = 0f;
                    keys[i].outTangent = 0f;
                    continue;
                }

                float inTangent = i > 0 ? SegmentSlope(keys[i - 1], keys[i]) : 0f;
                float outTangent = i < keys.Length - 1 ? SegmentSlope(keys[i], keys[i + 1]) : 0f;

                if (i > 0)
                {
                    float deltaIn = keys[i].value - keys[i - 1].value;
                    inTangent = deltaIn >= 0f ? Mathf.Max(0f, inTangent) : Mathf.Min(0f, inTangent);
                }

                if (i < keys.Length - 1)
                {
                    float deltaOut = keys[i + 1].value - keys[i].value;
                    outTangent = deltaOut >= 0f ? Mathf.Max(0f, outTangent) : Mathf.Min(0f, outTangent);
                }

                keys[i].inTangent = inTangent;
                keys[i].outTangent = outTangent;
            }
        }

        private static void ApplyRisingTangents(Keyframe[] keys)
        {
            for (int i = 0; i < keys.Length; i++)
            {
                float inTangent = i > 0 ? SegmentSlope(keys[i - 1], keys[i]) : 0f;
                float outTangent = i < keys.Length - 1 ? SegmentSlope(keys[i], keys[i + 1]) : 0f;
                keys[i].inTangent = Mathf.Max(0f, inTangent);
                keys[i].outTangent = Mathf.Max(0f, outTangent);
            }
        }

        private static bool IsZeroTangentIndex(int index, int[] zeroTangentIndices)
        {
            for (int i = 0; i < zeroTangentIndices.Length; i++)
            {
                if (zeroTangentIndices[i] == index)
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateCelestialElevation(
            AnimationCurve elevation,
            float nightStart,
            float dawnStart,
            float moonPeakElevation)
        {
            const int sampleCount = 50;
            const float tolerance = 0.25f;
            float nightCap = moonPeakElevation + tolerance;

            for (int i = 0; i <= sampleCount; i++)
            {
                float alpha = i / (float)sampleCount;
                float time = Mathf.Lerp(nightStart, dawnStart, alpha);
                float value = elevation.Evaluate(time);

                if (value > nightCap)
                {
                    Debug.LogWarning(
                        $"Celestial elevation exceeds moon cap near t={time:F3} (value {value:F1}°, cap {nightCap:F1}°).",
                        null);
                }
            }
        }

        private static float SegmentSlope(Keyframe from, Keyframe to)
        {
            return (to.value - from.value) / Mathf.Max(to.time - from.time, 1e-6f);
        }

        private static Gradient BuildLuminanceGradient(
            (float time, Color hue)[] stops,
            AnimationCurve luminanceEnvelope)
        {
            var colorKeys = new GradientColorKey[stops.Length];
            for (int i = 0; i < stops.Length; i++)
            {
                float targetLuminance = luminanceEnvelope.Evaluate(stops[i].time);
                colorKeys[i] = new GradientColorKey(
                    ScaleColorToLuminance(stops[i].hue, targetLuminance),
                    stops[i].time);
            }

            var gradient = new Gradient();
            gradient.SetKeys(
                colorKeys,
                new[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                });

            return gradient;
        }

        private static Color ScaleColorToLuminance(Color color, float targetLuminance)
        {
            float currentLuminance = GetLuminance(color);
            if (currentLuminance <= 1e-5f)
            {
                return new Color(targetLuminance, targetLuminance, targetLuminance, color.a);
            }

            float scale = targetLuminance / currentLuminance;
            return new Color(
                Mathf.Max(0f, color.r * scale),
                Mathf.Max(0f, color.g * scale),
                Mathf.Max(0f, color.b * scale),
                color.a);
        }

        private static float GetLuminance(Color color)
        {
            return (0.2126f * color.r) + (0.7152f * color.g) + (0.0722f * color.b);
        }

        private readonly struct MonotonicRegion
        {
            public readonly float Start;
            public readonly float End;
            public readonly bool ShouldRise;

            public MonotonicRegion(float start, float end, bool shouldRise)
            {
                Start = start;
                End = end;
                ShouldRise = shouldRise;
            }
        }

        private static void ValidateCurveMonotonicity(
            AnimationCurve curve,
            string curveName,
            params MonotonicRegion[] regions)
        {
            const int sampleCount = 50;
            const float tolerance = 1e-4f;

            for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
            {
                MonotonicRegion region = regions[regionIndex];
                float previousTime = region.Start;
                float previousValue = curve.Evaluate(region.Start);

                for (int i = 1; i <= sampleCount; i++)
                {
                    float alpha = i / (float)sampleCount;
                    float time = Mathf.Lerp(region.Start, region.End, alpha);
                    float value = curve.Evaluate(time);
                    bool valid = region.ShouldRise
                        ? value + tolerance >= previousValue
                        : value - tolerance <= previousValue;

                    if (!valid)
                    {
                        string direction = region.ShouldRise ? "rising" : "falling";
                        LogMonotonicityViolation(curveName, direction, previousTime, time, previousValue, value);
                    }

                    previousTime = time;
                    previousValue = value;
                }
            }
        }

        private static void LogMonotonicityViolation(
            string curveName,
            string regionLabel,
            float fromTime,
            float toTime,
            float fromValue,
            float toValue)
        {
            Debug.LogWarning(
                $"{curveName} monotonicity violation in {regionLabel} region near t={fromTime:F3}→{toTime:F3} " +
                $"(values {fromValue:F3}→{toValue:F3}).",
                null);
        }

        private static void ValidateGradientLuminanceMonotonicity(
            Gradient gradient,
            string gradientName,
            params MonotonicRegion[] regions)
        {
            const int sampleCount = 50;
            const float tolerance = 1e-4f;

            for (int regionIndex = 0; regionIndex < regions.Length; regionIndex++)
            {
                MonotonicRegion region = regions[regionIndex];
                float previousTime = region.Start;
                float previousLuminance = GetLuminance(gradient.Evaluate(region.Start));

                for (int i = 1; i <= sampleCount; i++)
                {
                    float alpha = i / (float)sampleCount;
                    float time = Mathf.Lerp(region.Start, region.End, alpha);
                    float luminance = GetLuminance(gradient.Evaluate(time));
                    bool valid = region.ShouldRise
                        ? luminance + tolerance >= previousLuminance
                        : luminance - tolerance <= previousLuminance;

                    if (!valid)
                    {
                        string direction = region.ShouldRise ? "rising" : "falling";
                        LogMonotonicityViolation(
                            gradientName + " luminance",
                            direction,
                            previousTime,
                            time,
                            previousLuminance,
                            luminance);
                    }

                    previousTime = time;
                    previousLuminance = luminance;
                }
            }
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

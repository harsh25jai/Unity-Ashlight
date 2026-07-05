using Ashlight.Environment;
using UnityEngine;
using UnityEngine.UIElements;

namespace Ashlight.UI
{
    /// <summary>
    /// Moon phase visuals used by legacy HUD tests and labels.
    /// </summary>
    public enum MoonPhaseVisual
    {
        WaxingCrescent,
        FirstQuarter,
        WaxingGibbous,
        Full,
        WaningGibbous,
        LastQuarter,
        WaningCrescent
    }

    /// <summary>
    /// Continuous display state for the top-right sun/moon cycle HUD icon.
    /// </summary>
    public struct CelestialDisplayState
    {
        /// <summary>0 = sun body, 1 = moon body.</summary>
        public float BodyBlend;

        /// <summary>Sun ray length and opacity from 0 to 1.</summary>
        public float RayIntensity;

        /// <summary>Moon lit fraction from 0 (new) to 1 (full).</summary>
        public float MoonIllumination;

        /// <summary>True when the moon is waxing (shadow on the right).</summary>
        public bool Waxing;

        /// <summary>Disc fill tint for the sun or blended body.</summary>
        public Color DiscColor;

        /// <summary>Ray stroke tint during sun phases.</summary>
        public Color RayColor;
    }

    /// <summary>
    /// UI Toolkit painter for a continuous sun-to-moon celestial cycle icon.
    /// </summary>
    public class CelestialCycleIndicator : VisualElement
    {
        private const int SunRayCount = 8;
        private const float DiscRadius = 9f;
        private const float MaxRayLength = 11f;
        private const float RayWidth = 2f;
        private const float RayInnerOffset = DiscRadius + 1f;
        private const float FullMoonIllumination = 0.99f;
        private const float DisplayEpsilon = 0.004f;
        private const float SunriseRayIntensity = 0.35f;
        private const float DuskMorphStartFraction = 0.72f;
        private const float DawnMorphEndFraction = 0.35f;

        private static readonly Color DayDiscColor = new Color(1f, 0.961f, 0.878f, 1f);
        private static readonly Color DayRayColor = new Color(1f, 0.902f, 0.620f, 1f);
        private static readonly Color DuskDiscColor = new Color(1f, 0.55f, 0.28f, 1f);
        private static readonly Color DuskRayColor = new Color(1f, 0.38f, 0.14f, 1f);
        private static readonly Color MoonShadowColor = new Color(0.08f, 0.09f, 0.12f, 1f);
        private static readonly Color MoonDiscColor = new Color(0.82f, 0.86f, 0.94f, 1f);
        private static readonly Vector2[] SunRayDirections = BuildSunRayDirections();

        private float _bodyBlend;
        private float _rayIntensity;
        private float _moonIllumination;
        private bool _waxing;
        private Color _discColor = Color.white;
        private Color _rayColor = Color.white;

        private float _lastBodyBlend = -1f;
        private float _lastRayIntensity = -1f;
        private float _lastMoonIllumination = -1f;
        private bool _lastWaxing;
        private Color _lastDiscColor = Color.clear;
        private Color _lastRayColor = Color.clear;

        private float _cachedRayLength;
        private Color _cachedRayStrokeColor = Color.white;
        private bool _moonDrawsShadow;
        private float _cachedMoonShadowOffsetX;

        public CelestialCycleIndicator()
        {
            generateVisualContent += OnGenerateVisualContent;
            pickingMode = PickingMode.Ignore;
        }

        /// <summary>
        /// Resolves a continuous celestial display from normalized cycle time.
        /// </summary>
        /// <param name="normalizedCycleTime">Position from 0 to 1 across the full day/night loop.</param>
        /// <param name="config">Phase duration configuration.</param>
        /// <param name="nightCycleCount">Completed night cycles for Night_Deep scaling.</param>
        /// <returns>Blended sun/moon HUD state.</returns>
        public static CelestialDisplayState ResolveDisplay(
            float normalizedCycleTime,
            DayNightConfig config,
            int nightCycleCount)
        {
            if (config == null)
            {
                return DefaultDisplayState();
            }

            float t = Mathf.Clamp01(normalizedCycleTime);
            float bodyBlend = SampleBodyBlend(t, config, nightCycleCount);
            float rayIntensity = SampleRayIntensity(t, config, nightCycleCount);
            SampleMoonIllumination(t, config, nightCycleCount, out float moonIllumination, out bool waxing);
            SampleDiscColors(t, config, nightCycleCount, bodyBlend, out Color discColor, out Color rayColor);

            return new CelestialDisplayState
            {
                BodyBlend = bodyBlend,
                RayIntensity = rayIntensity,
                MoonIllumination = moonIllumination,
                Waxing = waxing,
                DiscColor = discColor,
                RayColor = rayColor
            };
        }

        /// <summary>Applies a resolved celestial display state.</summary>
        /// <param name="state">Continuous sun/moon configuration.</param>
        public void ApplyDisplay(CelestialDisplayState state)
        {
            float bodyBlend = Mathf.Clamp01(state.BodyBlend);
            float rayIntensity = Mathf.Clamp01(state.RayIntensity);
            float moonIllumination = Mathf.Clamp01(state.MoonIllumination);
            bool waxing = state.Waxing;

            if (Mathf.Abs(bodyBlend - _lastBodyBlend) < DisplayEpsilon &&
                Mathf.Abs(rayIntensity - _lastRayIntensity) < DisplayEpsilon &&
                Mathf.Abs(moonIllumination - _lastMoonIllumination) < DisplayEpsilon &&
                waxing == _lastWaxing &&
                ColorsApproximatelyEqual(state.DiscColor, _lastDiscColor) &&
                ColorsApproximatelyEqual(state.RayColor, _lastRayColor))
            {
                return;
            }

            _lastBodyBlend = bodyBlend;
            _lastRayIntensity = rayIntensity;
            _lastMoonIllumination = moonIllumination;
            _lastWaxing = waxing;
            _lastDiscColor = state.DiscColor;
            _lastRayColor = state.RayColor;

            _bodyBlend = bodyBlend;
            _rayIntensity = rayIntensity;
            _moonIllumination = moonIllumination;
            _waxing = waxing;
            _discColor = state.DiscColor;
            _rayColor = state.RayColor;

            _cachedRayLength = MaxRayLength * _rayIntensity;
            _cachedRayStrokeColor = _rayColor;
            _cachedRayStrokeColor.a *= Mathf.Lerp(0.2f, 1f, _rayIntensity) * (1f - _bodyBlend * 0.85f);

            _cachedMoonShadowOffsetX = ComputeMoonShadowOffset(_moonIllumination, _waxing);
            _moonDrawsShadow = _moonIllumination < FullMoonIllumination;

            MarkDirtyRepaint();
        }

        private void OnGenerateVisualContent(MeshGenerationContext context)
        {
            Rect rect = contentRect;
            if (rect.width <= 0f || rect.height <= 0f)
            {
                return;
            }

            Vector2 center = rect.center;
            Painter2D painter = context.painter2D;
            float sunWeight = 1f - _bodyBlend;
            float moonWeight = _bodyBlend;

            if (sunWeight > 0.01f)
            {
                DrawSun(painter, center, sunWeight);
            }

            if (moonWeight > 0.01f)
            {
                DrawMoon(painter, center, moonWeight);
            }
        }

        private void DrawSun(Painter2D painter, Vector2 center, float alpha)
        {
            if (_rayIntensity > 0.01f)
            {
                painter.lineWidth = RayWidth;
                Color strokeColor = _cachedRayStrokeColor;
                strokeColor.a *= alpha;
                painter.strokeColor = strokeColor;
                painter.lineCap = LineCap.Round;

                float rayOuterRadius = DiscRadius + _cachedRayLength;

                for (int i = 0; i < SunRayCount; i++)
                {
                    Vector2 direction = SunRayDirections[i];
                    Vector2 rayStart = center + direction * RayInnerOffset;
                    Vector2 rayEnd = center + direction * rayOuterRadius;

                    painter.BeginPath();
                    painter.MoveTo(rayStart);
                    painter.LineTo(rayEnd);
                    painter.Stroke();
                }
            }

            Color disc = _discColor;
            disc.a *= alpha;
            painter.fillColor = disc;
            painter.BeginPath();
            painter.Arc(center, DiscRadius, 0f, 360f);
            painter.Fill();
        }

        private void DrawMoon(Painter2D painter, Vector2 center, float alpha)
        {
            Color disc = Color.Lerp(_discColor, MoonDiscColor, 0.65f);
            disc.a *= alpha;
            painter.fillColor = disc;
            painter.BeginPath();
            painter.Arc(center, DiscRadius, 0f, 360f);
            painter.Fill();

            if (_moonDrawsShadow)
            {
                Vector2 shadowCenter = center;
                shadowCenter.x += _cachedMoonShadowOffsetX;

                Color shadow = MoonShadowColor;
                shadow.a *= alpha;
                painter.fillColor = shadow;
                painter.BeginPath();
                painter.Arc(shadowCenter, DiscRadius, 0f, 360f);
                painter.Fill();
            }
        }

        private static CelestialDisplayState DefaultDisplayState()
        {
            return new CelestialDisplayState
            {
                BodyBlend = 0f,
                RayIntensity = 1f,
                MoonIllumination = 1f,
                Waxing = true,
                DiscColor = DayDiscColor,
                RayColor = DayRayColor
            };
        }

        private static float SampleRayIntensity(float t, DayNightConfig config, int nightCycleCount)
        {
            float dawnStart = config.GetPhaseStartNormalized(DayNightPhase.Dawn, nightCycleCount);
            float dayMid = config.GetPhaseMidpointNormalized(DayNightPhase.Day, nightCycleCount);
            float dayEnd = config.GetPhaseEndNormalized(DayNightPhase.Day, nightCycleCount);
            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, nightCycleCount);

            if (t >= dawnStart)
            {
                float span = Mathf.Max(0.0001f, 1f - dawnStart);
                float local = (t - dawnStart) / span;
                return Mathf.SmoothStep(0f, SunriseRayIntensity, local);
            }

            if (t <= dayMid)
            {
                float local = dayMid > 0f ? t / dayMid : 1f;
                return Mathf.Lerp(SunriseRayIntensity, 1f, Smooth(local));
            }

            if (t <= dayEnd)
            {
                return 1f;
            }

            if (t <= duskEnd)
            {
                float span = Mathf.Max(0.0001f, duskEnd - dayEnd);
                float local = (t - dayEnd) / span;
                return Mathf.Lerp(1f, 0f, Smooth(local));
            }

            return 0f;
        }

        private static float SampleBodyBlend(float t, DayNightConfig config, int nightCycleCount)
        {
            float duskMorphStart = config.GetPhaseNormalizedTime(
                DayNightPhase.Dusk,
                DuskMorphStartFraction,
                nightCycleCount);
            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, nightCycleCount);
            float dawnStart = config.GetPhaseStartNormalized(DayNightPhase.Dawn, nightCycleCount);
            float dawnMorphEnd = config.GetPhaseNormalizedTime(
                DayNightPhase.Dawn,
                DawnMorphEndFraction,
                nightCycleCount);

            if (t >= duskMorphStart && t < duskEnd)
            {
                float span = Mathf.Max(0.0001f, duskEnd - duskMorphStart);
                float local = (t - duskMorphStart) / span;
                return Smooth(local);
            }

            if (t >= duskEnd && t < dawnStart)
            {
                return 1f;
            }

            if (t >= dawnStart && t < dawnMorphEnd)
            {
                float span = Mathf.Max(0.0001f, dawnMorphEnd - dawnStart);
                float local = (t - dawnStart) / span;
                return 1f - Smooth(local);
            }

            return 0f;
        }

        private static void SampleMoonIllumination(
            float t,
            DayNightConfig config,
            int nightCycleCount,
            out float illumination,
            out bool waxing)
        {
            illumination = 0f;
            waxing = true;

            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, nightCycleCount);
            float nightEarlyEnd = config.GetPhaseEndNormalized(DayNightPhase.Night_Early, nightCycleCount);
            float nightDeepMid = config.GetPhaseMidpointNormalized(DayNightPhase.Night_Deep, nightCycleCount);
            float nightDeepEnd = config.GetPhaseEndNormalized(DayNightPhase.Night_Deep, nightCycleCount);
            float dawnStart = config.GetPhaseStartNormalized(DayNightPhase.Dawn, nightCycleCount);
            float nightDeepWaning = config.GetPhaseNormalizedTime(DayNightPhase.Night_Deep, 0.82f, nightCycleCount);
            float duskMorphStart = config.GetPhaseNormalizedTime(
                DayNightPhase.Dusk,
                DuskMorphStartFraction,
                nightCycleCount);

            if (t >= duskMorphStart && t < duskEnd)
            {
                float span = Mathf.Max(0.0001f, duskEnd - duskMorphStart);
                float local = (t - duskMorphStart) / span;
                illumination = Mathf.Lerp(0f, 0.12f, Smooth(local));
                waxing = true;
                return;
            }

            if (t < duskEnd)
            {
                return;
            }

            if (t >= dawnStart)
            {
                float span = Mathf.Max(0.0001f, 1f - dawnStart);
                float local = (t - dawnStart) / span;
                illumination = Mathf.Lerp(0.22f, 0f, Smooth(local));
                waxing = false;
                return;
            }

            if (t < nightEarlyEnd)
            {
                float span = Mathf.Max(0.0001f, nightEarlyEnd - duskEnd);
                float local = (t - duskEnd) / span;
                illumination = Mathf.Lerp(0.12f, 0.5f, Smooth(local));
                waxing = true;
                return;
            }

            if (t < nightDeepMid)
            {
                float span = Mathf.Max(0.0001f, nightDeepMid - nightEarlyEnd);
                float local = (t - nightEarlyEnd) / span;
                illumination = Mathf.Lerp(0.5f, 1f, Smooth(local));
                waxing = true;
                return;
            }

            if (t < nightDeepWaning)
            {
                float span = Mathf.Max(0.0001f, nightDeepWaning - nightDeepMid);
                float local = (t - nightDeepMid) / span;
                illumination = Mathf.Lerp(1f, 0.78f, Smooth(local));
                waxing = false;
                return;
            }

            if (t < nightDeepEnd)
            {
                float span = Mathf.Max(0.0001f, nightDeepEnd - nightDeepWaning);
                float local = (t - nightDeepWaning) / span;
                illumination = Mathf.Lerp(0.78f, 0.22f, Smooth(local));
                waxing = false;
            }
        }

        private static void SampleDiscColors(
            float t,
            DayNightConfig config,
            int nightCycleCount,
            float bodyBlend,
            out Color discColor,
            out Color rayColor)
        {
            float duskStart = config.GetPhaseStartNormalized(DayNightPhase.Dusk, nightCycleCount);
            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, nightCycleCount);

            discColor = DayDiscColor;
            rayColor = DayRayColor;

            if (t >= duskStart && t <= duskEnd)
            {
                float span = Mathf.Max(0.0001f, duskEnd - duskStart);
                float local = (t - duskStart) / span;
                float warmth = Smooth(Mathf.InverseLerp(0.15f, 1f, local));
                discColor = Color.Lerp(DayDiscColor, DuskDiscColor, warmth);
                rayColor = Color.Lerp(DayRayColor, DuskRayColor, warmth);
            }

            if (bodyBlend > 0f)
            {
                discColor = Color.Lerp(discColor, MoonDiscColor, bodyBlend);
                rayColor = Color.Lerp(rayColor, MoonDiscColor, bodyBlend * 0.5f);
            }
        }

        private static float ComputeMoonShadowOffset(float illumination, bool waxing)
        {
            if (illumination >= FullMoonIllumination)
            {
                return 0f;
            }

            if (illumination <= 0.01f)
            {
                return waxing ? DiscRadius * 2f : -DiscRadius * 2f;
            }

            float offset = (1f - illumination) * DiscRadius * 2f;
            return waxing ? offset : -offset;
        }

        private static bool ColorsApproximatelyEqual(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < DisplayEpsilon &&
                   Mathf.Abs(a.g - b.g) < DisplayEpsilon &&
                   Mathf.Abs(a.b - b.b) < DisplayEpsilon &&
                   Mathf.Abs(a.a - b.a) < DisplayEpsilon;
        }

        private static float Smooth(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }

        private static Vector2[] BuildSunRayDirections()
        {
            Vector2[] directions = new Vector2[SunRayCount];
            for (int i = 0; i < SunRayCount; i++)
            {
                float angle = i * Mathf.PI * 2f / SunRayCount;
                directions[i] = new Vector2(Mathf.Cos(angle), -Mathf.Sin(angle));
            }

            return directions;
        }
    }
}

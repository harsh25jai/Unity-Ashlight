using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Environment.Tests
{
    public class DayNightConfigTests
    {
        [Test]
        public void GetPhaseMidpointNormalized_ScalesWithPhaseDurations()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();

            float shortDayMidpoint = config.GetPhaseMidpointNormalized(DayNightPhase.Day, 0);

            typeof(DayNightConfig).GetField(
                "dayDuration",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(config, 480f);

            float longDayMidpoint = config.GetPhaseMidpointNormalized(DayNightPhase.Day, 0);

            Assert.Greater(longDayMidpoint, shortDayMidpoint);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildLightingCurves_PlacesDayPeakInsideDayPhase()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();
            DayNightLightingCurves curves = config.BuildLightingCurves(0);

            float dayMidpoint = config.GetPhaseMidpointNormalized(DayNightPhase.Day, 0);
            float duskStart = config.GetPhaseStartNormalized(DayNightPhase.Dusk, 0);
            float intensityAtDayMid = curves.LightIntensity.Evaluate(dayMidpoint);

            Assert.Less(dayMidpoint, duskStart);
            Assert.Greater(intensityAtDayMid, config.DayLightIntensity);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildLightingCurves_KeepsNightElevationWithinMoonCap()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();
            DayNightLightingCurves curves = config.BuildLightingCurves(0);

            float nightStart = config.GetPhaseStartNormalized(DayNightPhase.Night_Early, 0);
            float dawnStart = config.GetPhaseStartNormalized(DayNightPhase.Dawn, 0);
            const float moonCap = 22f;

            for (int i = 0; i <= 50; i++)
            {
                float time = Mathf.Lerp(nightStart, dawnStart, i / 50f);
                Assert.LessOrEqual(curves.SunElevation.Evaluate(time), moonCap);
            }

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildLightingCurves_SunSetsBeforeMoonArc()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();
            DayNightLightingCurves curves = config.BuildLightingCurves(0);

            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, 0);
            float nightEarlyMid = config.GetPhaseMidpointNormalized(DayNightPhase.Night_Early, 0);
            float sunsetElevation = curves.SunElevation.Evaluate(duskEnd);
            float nightElevation = curves.SunElevation.Evaluate(nightEarlyMid);

            Assert.Less(sunsetElevation, 12f);
            Assert.Less(nightElevation, 22f);
            Assert.Greater(nightElevation, sunsetElevation);

            Object.DestroyImmediate(config);
        }

        [Test]
        public void BuildLightingCurves_AzimuthAdvancesContinuouslyThroughNight()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();
            DayNightLightingCurves curves = config.BuildLightingCurves(0);

            float duskEnd = config.GetPhaseEndNormalized(DayNightPhase.Dusk, 0);
            float dawnStart = config.GetPhaseStartNormalized(DayNightPhase.Dawn, 0);
            float previousAzimuth = curves.SunAzimuth.Evaluate(duskEnd);

            for (int i = 1; i <= 50; i++)
            {
                float time = Mathf.Lerp(duskEnd, dawnStart, i / 50f);
                float azimuth = curves.SunAzimuth.Evaluate(time);
                Assert.GreaterOrEqual(azimuth, previousAzimuth - 0.5f);
                previousAzimuth = azimuth;
            }

            Object.DestroyImmediate(config);
        }
    }
}

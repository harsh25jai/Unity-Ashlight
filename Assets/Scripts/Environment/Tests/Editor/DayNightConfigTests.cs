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
    }
}

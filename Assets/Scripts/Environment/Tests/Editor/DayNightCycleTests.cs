using NUnit.Framework;
using UnityEngine;

namespace Ashlight.Environment.Tests
{
    public class DayNightCycleTests
    {
        [Test]
        public void CalculateNightDeepDuration_ScalesWithNightCount()
        {
            float firstNight = DayNightCycle.CalculateNightDeepDuration(0, 120f, 1.15f);
            float secondNight = DayNightCycle.CalculateNightDeepDuration(1, 120f, 1.15f);

            Assert.AreEqual(120f, firstNight, 0.001f);
            Assert.AreEqual(138f, secondNight, 0.001f);
        }

        [Test]
        public void GetTotalCycleDuration_IncludesAllPhases()
        {
            DayNightConfig config = ScriptableObject.CreateInstance<DayNightConfig>();
            GameObject cycleObject = new GameObject("DayNightCycle");
            DayNightCycle cycle = cycleObject.AddComponent<DayNightCycle>();

            typeof(DayNightCycle).GetField(
                "_config",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(cycle, config);

            float expected = 120f + 120f + 120f + 300f + 120f;
            Assert.AreEqual(expected, cycle.GetTotalCycleDuration(0), 0.001f);

            Object.DestroyImmediate(cycleObject);
            Object.DestroyImmediate(config);
        }

        [Test]
        public void ForcePhase_AndSetTimeScale_MethodsExist()
        {
            Assert.NotNull(typeof(DayNightCycle).GetMethod(nameof(DayNightCycle.ForcePhase)));
            Assert.NotNull(typeof(DayNightCycle).GetMethod(nameof(DayNightCycle.SetTimeScale)));
        }

        [Test]
        public void RemainingPhaseTime_PropertyExists()
        {
            Assert.NotNull(typeof(DayNightCycle).GetProperty(nameof(DayNightCycle.RemainingPhaseTime)));
        }

        [Test]
        public void TimeScale_PropertyExists()
        {
            Assert.NotNull(typeof(DayNightCycle).GetProperty(nameof(DayNightCycle.TimeScale)));
        }

        [Test]
        public void DirectionalLight_PropertyExists()
        {
            Assert.NotNull(typeof(DayNightCycle).GetProperty(nameof(DayNightCycle.DirectionalLight)));
        }

        [Test]
        public void SetNormalizedTime_MethodExists()
        {
            Assert.NotNull(typeof(DayNightCycle).GetMethod(nameof(DayNightCycle.SetNormalizedTime)));
        }

        [Test]
        public void GetSunElevation_AndGetSunAzimuth_MethodsExist()
        {
            Assert.NotNull(typeof(DayNightCycle).GetMethod(nameof(DayNightCycle.GetSunElevation)));
            Assert.NotNull(typeof(DayNightCycle).GetMethod(nameof(DayNightCycle.GetSunAzimuth)));
        }
    }
}

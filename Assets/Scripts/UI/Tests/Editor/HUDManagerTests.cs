using Ashlight.Environment;
using NUnit.Framework;
using UnityEngine;

namespace Ashlight.UI.Tests
{
    public class HUDManagerTests
    {
        private DayNightConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<DayNightConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
            {
                Object.DestroyImmediate(_config);
            }
        }

        [Test]
        public void FormatHealthDisplay_RoundsUpAndShowsMax()
        {
            Assert.AreEqual("86 / 100", HUDManager.FormatHealthDisplay(85.2f, 100f));
            Assert.AreEqual("1 / 100", HUDManager.FormatHealthDisplay(0.4f, 100f));
        }

        [Test]
        public void FormatNightCount_ShowsNightNumber()
        {
            Assert.AreEqual("Night 3", HUDManager.FormatNightCount(3));
        }

        [Test]
        public void FormatNightPhase_MapsKnownPhases()
        {
            Assert.AreEqual("Day", HUDManager.FormatNightPhase(DayNightPhase.Day));
            Assert.AreEqual("Night — Deep", HUDManager.FormatNightPhase(DayNightPhase.Night_Deep));
        }

        [Test]
        public void FormatNightPhaseIndicator_UsesUppercaseLabels()
        {
            Assert.AreEqual("DAY", HUDManager.FormatNightPhaseIndicator(DayNightPhase.Day));
            Assert.AreEqual("NIGHT", HUDManager.FormatNightPhaseIndicator(DayNightPhase.Night_Deep));
            Assert.AreEqual("DAWN", HUDManager.FormatNightPhaseIndicator(DayNightPhase.Dawn));
        }

        [Test]
        public void FormatPhaseRemainingTime_ShowsMinutesAndSeconds()
        {
            Assert.AreEqual("4:32", HUDManager.FormatPhaseRemainingTime(272f));
            Assert.AreEqual("0:05", HUDManager.FormatPhaseRemainingTime(4.2f));
        }

        [Test]
        public void ResolveCelestialDisplay_PeaksRayIntensityNearMidday()
        {
            float dayMid = _config.GetPhaseMidpointNormalized(DayNightPhase.Day, 0);
            CelestialDisplayState state = HUDManager.ResolveCelestialDisplay(dayMid, _config, 0);

            Assert.Less(state.BodyBlend, 0.1f);
            Assert.Greater(state.RayIntensity, 0.95f);
        }

        [Test]
        public void ResolveCelestialDisplay_DoesNotResetRaysAtDuskStart()
        {
            float dayEnd = _config.GetPhaseEndNormalized(DayNightPhase.Day, 0);
            float duskStart = _config.GetPhaseStartNormalized(DayNightPhase.Dusk, 0);

            CelestialDisplayState endOfDay = HUDManager.ResolveCelestialDisplay(dayEnd - 0.001f, _config, 0);
            CelestialDisplayState startOfDusk = HUDManager.ResolveCelestialDisplay(duskStart, _config, 0);

            Assert.Greater(endOfDay.RayIntensity, 0.9f);
            Assert.Greater(startOfDusk.RayIntensity, 0.85f);
            Assert.Less(Mathf.Abs(endOfDay.RayIntensity - startOfDusk.RayIntensity), 0.15f);
        }

        [Test]
        public void ResolveCelestialDisplay_MorphsToMoonAfterDusk()
        {
            float nightEarlyMid = _config.GetPhaseMidpointNormalized(DayNightPhase.Night_Early, 0);
            CelestialDisplayState state = HUDManager.ResolveCelestialDisplay(nightEarlyMid, _config, 0);

            Assert.Greater(state.BodyBlend, 0.9f);
            Assert.Greater(state.MoonIllumination, 0.2f);
            Assert.Less(state.MoonIllumination, 0.6f);
            Assert.IsTrue(state.Waxing);
        }

        [Test]
        public void ResolveCelestialDisplay_ReachesFullMoonDuringDeepNight()
        {
            float deepNightMid = _config.GetPhaseMidpointNormalized(DayNightPhase.Night_Deep, 0);
            CelestialDisplayState state = HUDManager.ResolveCelestialDisplay(deepNightMid, _config, 0);

            Assert.Greater(state.BodyBlend, 0.9f);
            Assert.Greater(state.MoonIllumination, 0.95f);
        }

        [Test]
        public void ResolveCelestialDisplay_FadesToNewMoonBeforeSunrise()
        {
            float dawnEnd = 0.999f;
            CelestialDisplayState state = HUDManager.ResolveCelestialDisplay(dawnEnd, _config, 0);

            Assert.Less(state.MoonIllumination, 0.05f);
            Assert.Less(state.BodyBlend, 0.5f);
            Assert.Less(state.RayIntensity, 0.4f);
        }
    }
}

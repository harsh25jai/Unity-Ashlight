using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Authoring data for day/night phase durations and lighting endpoints.
    /// </summary>
    [CreateAssetMenu(fileName = "DayNightConfig", menuName = "Ashlight/Environment/Day Night Config")]
    public class DayNightConfig : ScriptableObject
    {
        [Header("Phase Durations (seconds)")]
        [SerializeField] private float dayDuration = 480f;
        [SerializeField] private float duskDuration = 120f;
        [SerializeField] private float nightEarlyDuration = 300f;
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
    }
}

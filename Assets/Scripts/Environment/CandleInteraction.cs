using Ashlight.Systems;
using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// One-time candle worship that refuels the Holy Torch and burns out the candle visual.
    /// </summary>
    [DisallowMultipleComponent]
    public class CandleInteraction : WorshipInteraction
    {
        [SerializeField] private HolyTorch holyTorch;
        [SerializeField] private float torchRefillAmount = 50f;
        [SerializeField] private GameObject candleVisual;
        [SerializeField] private ParticleSystem burnOutParticles;
        [SerializeField] private AudioSource feedbackAudioSource;
        [SerializeField] private AudioClip drainedSound;
        [SerializeField] private bool isDrained;

        protected override void Awake()
        {
            base.Awake();

            if (holyTorch == null && player != null)
            {
                holyTorch = player.GetComponentInChildren<HolyTorch>();
            }

            if (holyTorch == null)
            {
                Debug.LogWarning($"{nameof(CandleInteraction)} has no {nameof(HolyTorch)} assigned.", this);
            }
        }

        /// <inheritdoc />
        protected override void OnWorship()
        {
            if (isDrained)
            {
                PlayDrainedFeedback();
                return;
            }

            holyTorch?.Refuel(torchRefillAmount);
            isDrained = true;

            if (burnOutParticles != null)
            {
                burnOutParticles.Play();
            }

            if (candleVisual != null)
            {
                candleVisual.SetActive(false);
            }

            base.OnWorship();
        }

        /// <inheritdoc />
        protected override string GetPromptText()
        {
            return isDrained ? "Candle has burned out" : "Press E to absorb candle light";
        }

        private void PlayDrainedFeedback()
        {
            if (burnOutParticles != null && !burnOutParticles.isPlaying)
            {
                var emission = burnOutParticles.emission;
                emission.rateOverTime = 2f;
                burnOutParticles.Play();
            }

            if (feedbackAudioSource != null && drainedSound != null)
            {
                feedbackAudioSource.PlayOneShot(drainedSound);
            }
        }
    }
}

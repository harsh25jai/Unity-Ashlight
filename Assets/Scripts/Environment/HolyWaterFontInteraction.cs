using Ashlight.Systems;
using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Holy water font worship that grants bottles on a cooldown and triggers church saves.
    /// </summary>
    [DisallowMultipleComponent]
    public class HolyWaterFontInteraction : WorshipInteraction
    {
        [SerializeField] private HolyWaterInventory holyWaterInventory;
        [SerializeField] private int bottlesGranted = 1;
        [SerializeField] private float refillCooldown = 120f;
        [SerializeField] private ParticleSystem fillParticles;
        [SerializeField] private AudioSource feedbackAudioSource;
        [SerializeField] private AudioClip fillSound;
        [SerializeField] private AudioClip emptySound;
        [SerializeField] private AudioClip fullSound;

        private float _lastRefillTime = -999f;

        protected override void Awake()
        {
            base.Awake();

            if (holyWaterInventory == null)
            {
                Debug.LogWarning($"{nameof(HolyWaterFontInteraction)} has no {nameof(HolyWaterInventory)} assigned.", this);
            }
        }

        /// <inheritdoc />
        protected override void OnWorship()
        {
            if (Time.time - _lastRefillTime < refillCooldown)
            {
                PlayFeedback(emptySound);
                return;
            }

            bool addedAny = false;
            for (int i = 0; i < bottlesGranted; i++)
            {
                if (holyWaterInventory != null && holyWaterInventory.AddBottle())
                {
                    addedAny = true;
                }
            }

            if (addedAny)
            {
                _lastRefillTime = Time.time;

                if (fillParticles != null)
                {
                    fillParticles.Play();
                }

                PlayFeedback(fillSound);
            }
            else
            {
                PlayFeedback(fullSound);
            }

            base.OnWorship();
        }

        /// <inheritdoc />
        protected override string GetPromptText()
        {
            return "Press E to fill holy water";
        }

        private void PlayFeedback(AudioClip clip)
        {
            if (feedbackAudioSource == null || clip == null)
            {
                return;
            }

            feedbackAudioSource.PlayOneShot(clip);
        }
    }
}

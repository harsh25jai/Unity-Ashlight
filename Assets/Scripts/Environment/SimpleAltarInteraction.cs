using Ashlight.Player;
using UnityEngine;

namespace Ashlight.Environment
{
    /// <summary>
    /// Simple altar worship that restores Faith on a cooldown without triggering a save point.
    /// </summary>
    [DisallowMultipleComponent]
    public class SimpleAltarInteraction : WorshipInteraction
    {
        [SerializeField] private PlayerFaith playerFaith;
        [SerializeField] private float faithBoostAmount = 15f;
        [SerializeField] private float worshipCooldown = 60f;
        [SerializeField] private ParticleSystem worshipParticles;
        [SerializeField] private AudioSource worshipAudioSource;
        [SerializeField] private AudioClip worshipSound;

        private float _lastWorshipTime = -999f;

        protected override void Awake()
        {
            base.Awake();

            if (playerFaith == null && player != null)
            {
                playerFaith = player.GetComponent<PlayerFaith>();
            }

            if (playerFaith == null)
            {
                Debug.LogWarning($"{nameof(SimpleAltarInteraction)} has no {nameof(PlayerFaith)} assigned.", this);
            }
        }

        /// <inheritdoc />
        protected override void OnWorship()
        {
            if (Time.time - _lastWorshipTime < worshipCooldown)
            {
                return;
            }

            _lastWorshipTime = Time.time;
            playerFaith?.RestoreFaith(faithBoostAmount);

            if (worshipParticles != null)
            {
                worshipParticles.Play();
            }

            if (worshipAudioSource != null && worshipSound != null)
            {
                worshipAudioSource.PlayOneShot(worshipSound);
            }

            base.OnWorship();
        }

        /// <inheritdoc />
        protected override string GetPromptText()
        {
            return "Press E to pray";
        }
    }
}

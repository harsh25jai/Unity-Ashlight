using Ashlight.Systems;
using UnityEngine;
using UnityEngine.Audio;

namespace Ashlight.Audio
{
    /// <summary>
    /// Drives layered horror audio from <see cref="FearSystem"/> fear values.
    /// </summary>
    [DisallowMultipleComponent]
    public class FearAudioSystem : MonoBehaviour
    {
        private const float HeartbeatStartThreshold = 0.1f;
        private const float WhisperStartThreshold = 0.3f;
        private const float WhisperPitchFearThreshold = 0.5f;

        [SerializeField] private FearSystem fearSystem;
        [SerializeField] private AudioSource heartbeatSource;
        [SerializeField] private AudioSource whisperSource;
        [SerializeField] private AudioSource staticSource;
        [SerializeField] private AudioSource breathingSource;
        [SerializeField] private AudioMixerGroup fearGroup;

        private void Awake()
        {
            if (fearSystem == null)
            {
                fearSystem = FindAnyObjectByType<FearSystem>();
            }

            if (fearGroup == null && AudioManager.Instance != null)
            {
                fearGroup = AudioManager.Instance.FearGroup;
            }

            if (fearSystem == null)
            {
                Debug.LogError($"{nameof(FearAudioSystem)} requires a {nameof(FearSystem)}.", this);
            }

            if (heartbeatSource == null)
            {
                Debug.LogError($"{nameof(FearAudioSystem)} requires a heartbeat {nameof(AudioSource)}.", this);
            }

            if (whisperSource == null)
            {
                Debug.LogError($"{nameof(FearAudioSystem)} requires a whisper {nameof(AudioSource)}.", this);
            }

            if (staticSource == null)
            {
                Debug.LogError($"{nameof(FearAudioSystem)} requires a static {nameof(AudioSource)}.", this);
            }

            if (breathingSource == null)
            {
                Debug.LogError($"{nameof(FearAudioSystem)} requires a breathing {nameof(AudioSource)}.", this);
            }

            ConfigureSource(heartbeatSource);
            ConfigureSource(whisperSource);
            ConfigureSource(staticSource);
            ConfigureSource(breathingSource);
        }

        private void OnEnable()
        {
            if (fearSystem != null)
            {
                fearSystem.OnFearChanged.AddListener(OnFearChanged);
                OnFearChanged(fearSystem.CurrentFear);
            }
        }

        private void OnDisable()
        {
            if (fearSystem != null)
            {
                fearSystem.OnFearChanged.RemoveListener(OnFearChanged);
            }

            StopLayer(heartbeatSource);
            StopLayer(whisperSource);
            StopLayer(staticSource);
            StopLayer(breathingSource);
        }

        /// <summary>
        /// Updates fear-driven audio layers when the fear value changes.
        /// </summary>
        /// <param name="fear">Normalized fear from 0 to 1.</param>
        public void OnFearChanged(float fear)
        {
            float clampedFear = Mathf.Clamp01(fear);

            UpdateHeartbeat(clampedFear);
            UpdateWhispers(clampedFear);
            UpdateStatic(clampedFear);
            UpdateBreathing(clampedFear);
        }

        private void ConfigureSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.loop = true;
            source.playOnAwake = false;

            if (fearGroup != null)
            {
                source.outputAudioMixerGroup = fearGroup;
            }
        }

        private void UpdateHeartbeat(float fear)
        {
            if (heartbeatSource == null || heartbeatSource.clip == null)
            {
                return;
            }

            if (fear <= HeartbeatStartThreshold)
            {
                StopLayer(heartbeatSource);
                return;
            }

            heartbeatSource.volume = Mathf.Lerp(0f, 0.8f, fear);
            heartbeatSource.pitch = Mathf.Lerp(0.6f, 1.4f, fear);
            EnsurePlaying(heartbeatSource);
        }

        private void UpdateWhispers(float fear)
        {
            if (whisperSource == null || whisperSource.clip == null)
            {
                return;
            }

            if (fear <= WhisperStartThreshold)
            {
                whisperSource.volume = 0f;
                StopLayer(whisperSource);
                return;
            }

            float whisperBlend = Mathf.Max(0f, fear - WhisperStartThreshold) / 0.7f;
            whisperSource.volume = Mathf.Lerp(0f, 0.6f, whisperBlend);

            if (fear > WhisperPitchFearThreshold)
            {
                whisperSource.pitch = Random.Range(0.8f, 1.1f);
            }
            else
            {
                whisperSource.pitch = 1f;
            }

            EnsurePlaying(whisperSource);
        }

        private void UpdateStatic(float fear)
        {
            if (staticSource == null || staticSource.clip == null)
            {
                return;
            }

            staticSource.volume = Mathf.Lerp(0f, 0.3f, fear);
            EnsurePlaying(staticSource);
        }

        private void UpdateBreathing(float fear)
        {
            if (breathingSource == null || breathingSource.clip == null)
            {
                return;
            }

            breathingSource.volume = Mathf.Lerp(0f, 0.5f, fear);
            breathingSource.pitch = Mathf.Lerp(1f, 1.5f, fear);
            EnsurePlaying(breathingSource);
        }

        private static void EnsurePlaying(AudioSource source)
        {
            if (source != null && !source.isPlaying)
            {
                source.Play();
            }
        }

        private static void StopLayer(AudioSource source)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
    }
}

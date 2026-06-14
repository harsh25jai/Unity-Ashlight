using System.Collections;
using System.Collections.Generic;
using Ashlight.Environment;
using UnityEngine;
using UnityEngine.Audio;

namespace Ashlight.Audio
{
    /// <summary>
    /// Crossfades forest ambient layers in response to day/night phase changes.
    /// </summary>
    [DisallowMultipleComponent]
    public class AmbientAudioSystem : MonoBehaviour
    {
        [SerializeField] private DayNightCycle dayNightCycle;
        [SerializeField] private AudioSource dayAmbientSource;
        [SerializeField] private AudioSource nightAmbientSource;
        [SerializeField] private AudioSource nightDeepSource;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private float crossfadeDuration = 3f;

        private readonly List<Coroutine> _activeCrossfades = new List<Coroutine>();

        private void Awake()
        {
            if (dayNightCycle == null)
            {
                dayNightCycle = FindAnyObjectByType<DayNightCycle>();
            }

            if (ambientGroup == null && AudioManager.Instance != null)
            {
                ambientGroup = AudioManager.Instance.AmbientGroup;
            }

            if (dayNightCycle == null)
            {
                Debug.LogError($"{nameof(AmbientAudioSystem)} requires a {nameof(DayNightCycle)}.", this);
            }

            if (dayAmbientSource == null)
            {
                Debug.LogError($"{nameof(AmbientAudioSystem)} requires a day ambient {nameof(AudioSource)}.", this);
            }

            if (nightAmbientSource == null)
            {
                Debug.LogError($"{nameof(AmbientAudioSystem)} requires a night ambient {nameof(AudioSource)}.", this);
            }

            if (nightDeepSource == null)
            {
                Debug.LogError($"{nameof(AmbientAudioSystem)} requires a night deep {nameof(AudioSource)}.", this);
            }

            ConfigureAmbientSource(dayAmbientSource);
            ConfigureAmbientSource(nightAmbientSource);
            ConfigureAmbientSource(nightDeepSource);
        }

        private void OnEnable()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.AddListener(OnPhaseChanged);
                OnPhaseChanged(dayNightCycle.CurrentPhase);
            }
        }

        private void OnDisable()
        {
            if (dayNightCycle != null)
            {
                dayNightCycle.OnPhaseChanged.RemoveListener(OnPhaseChanged);
            }

            StopAllCrossfades();
        }

        /// <summary>
        /// Smoothly crossfades volume from one ambient source to another.
        /// </summary>
        /// <param name="from">Source fading out.</param>
        /// <param name="to">Source fading in.</param>
        /// <param name="duration">Crossfade duration in seconds.</param>
        public void CrossfadeAudio(AudioSource from, AudioSource to, float duration)
        {
            CrossfadeAudio(from, to, 1f, duration);
        }

        /// <summary>
        /// Smoothly crossfades volume from one ambient source to another at a target volume.
        /// </summary>
        /// <param name="from">Source fading out.</param>
        /// <param name="to">Source fading in.</param>
        /// <param name="toTargetVolume">Final volume for the incoming source.</param>
        /// <param name="duration">Crossfade duration in seconds.</param>
        public void CrossfadeAudio(AudioSource from, AudioSource to, float toTargetVolume, float duration)
        {
            if (from == null && to == null)
            {
                return;
            }

            Coroutine routine = StartCoroutine(CrossfadeAudioRoutine(from, to, toTargetVolume, duration));
            _activeCrossfades.Add(routine);
        }

        private void FadeSourceTo(AudioSource source, float targetVolume, float duration)
        {
            if (source == null || source.clip == null)
            {
                return;
            }

            Coroutine routine = StartCoroutine(FadeSourceRoutine(source, targetVolume, duration));
            _activeCrossfades.Add(routine);
        }

        private void OnPhaseChanged(DayNightPhase phase)
        {
            StopAllCrossfades();

            switch (phase)
            {
                case DayNightPhase.Day:
                    FadeSourceTo(dayAmbientSource, 1f, crossfadeDuration);
                    FadeSourceTo(nightAmbientSource, 0f, crossfadeDuration);
                    FadeSourceTo(nightDeepSource, 0f, crossfadeDuration);
                    break;

                case DayNightPhase.Dusk:
                    CrossfadeAudio(dayAmbientSource, nightAmbientSource, 0.5f, crossfadeDuration);
                    FadeSourceTo(nightDeepSource, 0f, crossfadeDuration);
                    break;

                case DayNightPhase.Night_Early:
                    FadeSourceTo(dayAmbientSource, 0f, crossfadeDuration);
                    FadeSourceTo(nightAmbientSource, 1f, crossfadeDuration);
                    FadeSourceTo(nightDeepSource, 0.3f, crossfadeDuration);
                    break;

                case DayNightPhase.Night_Deep:
                    FadeSourceTo(dayAmbientSource, 0f, crossfadeDuration);
                    FadeSourceTo(nightAmbientSource, 1f, crossfadeDuration);
                    FadeSourceTo(nightDeepSource, 1f, crossfadeDuration);
                    break;

                case DayNightPhase.Dawn:
                    CrossfadeAudio(nightAmbientSource, dayAmbientSource, 1f, crossfadeDuration);
                    FadeSourceTo(nightDeepSource, 0f, crossfadeDuration);
                    break;
            }
        }

        private void ConfigureAmbientSource(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            source.loop = true;
            source.playOnAwake = false;
            source.volume = 0f;

            if (ambientGroup != null)
            {
                source.outputAudioMixerGroup = ambientGroup;
            }
        }

        private IEnumerator CrossfadeAudioRoutine(AudioSource from, AudioSource to, float toTargetVolume, float duration)
        {
            float fromStartVolume = from != null ? from.volume : 0f;
            float toStartVolume = to != null ? to.volume : 0f;

            if (to != null && to.clip != null && toTargetVolume > 0f && !to.isPlaying)
            {
                to.Play();
            }

            if (duration <= 0f)
            {
                if (from != null)
                {
                    from.volume = 0f;
                    from.Stop();
                }

                if (to != null)
                {
                    to.volume = toTargetVolume;
                    if (toTargetVolume <= 0f)
                    {
                        to.Stop();
                    }
                }

                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                if (from != null)
                {
                    from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                }

                if (to != null)
                {
                    to.volume = Mathf.Lerp(toStartVolume, toTargetVolume, t);
                }

                yield return null;
            }

            if (from != null)
            {
                from.volume = 0f;
                from.Stop();
            }

            if (to != null)
            {
                to.volume = toTargetVolume;
                if (toTargetVolume <= 0f)
                {
                    to.Stop();
                }
            }
        }

        private IEnumerator FadeSourceRoutine(AudioSource source, float targetVolume, float duration)
        {
            float startVolume = source.volume;

            if (targetVolume > 0f && source.clip != null && !source.isPlaying)
            {
                source.Play();
            }

            if (duration <= 0f)
            {
                source.volume = targetVolume;
                if (targetVolume <= 0f)
                {
                    source.Stop();
                }

                yield break;
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);
                yield return null;
            }

            source.volume = targetVolume;
            if (targetVolume <= 0f)
            {
                source.Stop();
            }
        }

        private void StopAllCrossfades()
        {
            foreach (Coroutine routine in _activeCrossfades)
            {
                if (routine != null)
                {
                    StopCoroutine(routine);
                }
            }

            _activeCrossfades.Clear();
        }
    }
}

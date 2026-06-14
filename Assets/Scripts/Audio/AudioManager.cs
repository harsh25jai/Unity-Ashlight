using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

namespace Ashlight.Audio
{
    /// <summary>
    /// Central audio routing singleton for music, SFX, and mixer group volume control.
    /// </summary>
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        private const float MinAudibleVolume = 0.0001f;
        private const float SilentDecibels = -80f;

        [SerializeField] private AudioMixer masterMixer;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;
        [SerializeField] private AudioMixerGroup ghostGroup;
        [SerializeField] private AudioMixerGroup fearGroup;
        [SerializeField] private AudioSource musicSource;

        private Coroutine _musicFadeCoroutine;

        /// <summary>Gets the active <see cref="AudioManager"/> instance.</summary>
        public static AudioManager Instance { get; private set; }

        /// <summary>Gets the music mixer group.</summary>
        public AudioMixerGroup MusicGroup => musicGroup;

        /// <summary>Gets the SFX mixer group.</summary>
        public AudioMixerGroup SfxGroup => sfxGroup;

        /// <summary>Gets the ambient mixer group.</summary>
        public AudioMixerGroup AmbientGroup => ambientGroup;

        /// <summary>Gets the ghost mixer group.</summary>
        public AudioMixerGroup GhostGroup => ghostGroup;

        /// <summary>Gets the fear mixer group.</summary>
        public AudioMixerGroup FearGroup => fearGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (masterMixer == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires a {nameof(AudioMixer)}.", this);
            }

            if (musicGroup == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires a music {nameof(AudioMixerGroup)}.", this);
            }

            if (sfxGroup == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires an SFX {nameof(AudioMixerGroup)}.", this);
            }

            if (ambientGroup == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires an ambient {nameof(AudioMixerGroup)}.", this);
            }

            if (ghostGroup == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires a ghost {nameof(AudioMixerGroup)}.", this);
            }

            if (fearGroup == null)
            {
                Debug.LogError($"{nameof(AudioManager)} requires a fear {nameof(AudioMixerGroup)}.", this);
            }

            if (musicSource == null)
            {
                musicSource = gameObject.AddComponent<AudioSource>();
                musicSource.playOnAwake = false;
                musicSource.loop = true;
            }

            if (musicGroup != null)
            {
                musicSource.outputAudioMixerGroup = musicGroup;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        /// <summary>
        /// Plays a one-shot SFX at a world position and destroys the temporary source afterward.
        /// </summary>
        /// <param name="clip">Clip to play.</param>
        /// <param name="position">World position for the sound.</param>
        /// <param name="volume">Normalized playback volume from 0 to 1.</param>
        public void PlaySFX(AudioClip clip, Vector3 position, float volume = 1f)
        {
            if (clip == null || sfxGroup == null)
            {
                return;
            }

            GameObject tempSourceObject = new GameObject("SFX_AudioSource");
            tempSourceObject.transform.position = position;

            AudioSource source = tempSourceObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = Mathf.Clamp01(volume);
            source.spatialBlend = 1f;
            source.outputAudioMixerGroup = sfxGroup;
            source.Play();

            Destroy(tempSourceObject, clip.length + 0.1f);
        }

        /// <summary>
        /// Starts music playback with a coroutine-driven fade in on the music mixer group.
        /// </summary>
        /// <param name="clip">Music clip to play.</param>
        /// <param name="fadeInDuration">Seconds to fade from silence to full volume.</param>
        public void PlayMusic(AudioClip clip, float fadeInDuration = 2f)
        {
            if (clip == null || musicSource == null)
            {
                return;
            }

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = StartCoroutine(PlayMusicRoutine(clip, fadeInDuration));
        }

        /// <summary>
        /// Sets a mixer group volume using a normalized 0-1 value converted to decibels.
        /// </summary>
        /// <param name="groupName">Exposed mixer parameter name.</param>
        /// <param name="normalizedVolume">Target volume from 0 to 1.</param>
        public void SetMixerVolume(string groupName, float normalizedVolume)
        {
            if (masterMixer == null || string.IsNullOrEmpty(groupName))
            {
                return;
            }

            float clampedVolume = Mathf.Clamp01(normalizedVolume);
            float decibels = clampedVolume > MinAudibleVolume
                ? Mathf.Log10(clampedVolume) * 20f
                : SilentDecibels;

            masterMixer.SetFloat(groupName, decibels);
        }

        /// <summary>
        /// Fades out and stops the active music track.
        /// </summary>
        /// <param name="fadeOutDuration">Seconds to fade from current volume to silence.</param>
        public void StopMusic(float fadeOutDuration = 2f)
        {
            if (musicSource == null || !musicSource.isPlaying)
            {
                return;
            }

            if (_musicFadeCoroutine != null)
            {
                StopCoroutine(_musicFadeCoroutine);
            }

            _musicFadeCoroutine = StartCoroutine(StopMusicRoutine(fadeOutDuration));
        }

        private IEnumerator PlayMusicRoutine(AudioClip clip, float fadeInDuration)
        {
            musicSource.clip = clip;
            musicSource.loop = true;
            musicSource.volume = 0f;
            musicSource.Play();

            if (fadeInDuration <= 0f)
            {
                musicSource.volume = 1f;
                _musicFadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(0f, 1f, elapsed / fadeInDuration);
                yield return null;
            }

            musicSource.volume = 1f;
            _musicFadeCoroutine = null;
        }

        private IEnumerator StopMusicRoutine(float fadeOutDuration)
        {
            float startVolume = musicSource.volume;

            if (fadeOutDuration <= 0f)
            {
                musicSource.Stop();
                musicSource.volume = 0f;
                _musicFadeCoroutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                musicSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeOutDuration);
                yield return null;
            }

            musicSource.Stop();
            musicSource.volume = 0f;
            _musicFadeCoroutine = null;
        }
    }
}

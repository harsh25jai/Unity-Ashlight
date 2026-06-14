using System.Collections;
using Ashlight.Ghost;
using UnityEngine;
using UnityEngine.Audio;

namespace Ashlight.Audio
{
    /// <summary>
    /// Emits spatialized ghost audio based on <see cref="GhostAIController"/> state.
    /// </summary>
    [DisallowMultipleComponent]
    public class GhostAudioEmitter : MonoBehaviour
    {
        private const float StateCheckInterval = 2f;
        private const float MinDistance = 1f;
        private const float MaxDistance = 20f;

        [SerializeField] private GhostAIController ghostAI;
        [SerializeField] private AudioSource ghostAudioSource;
        [SerializeField] private AudioMixerGroup ghostGroup;
        [SerializeField] private AudioClip[] wanderSounds;
        [SerializeField] private AudioClip[] stalkSounds;
        [SerializeField] private AudioClip[] chaseSounds;
        [SerializeField] private AudioClip[] attackSounds;
        [SerializeField] private AudioClip retreatSound;

        private GhostState _lastKnownState;
        private int _lastClipIndex = -1;
        private Coroutine _stateAudioRoutine;

        private void Awake()
        {
            if (ghostAI == null)
            {
                ghostAI = GetComponentInParent<GhostAIController>();
            }

            if (ghostAudioSource == null)
            {
                ghostAudioSource = GetComponent<AudioSource>();
            }

            if (ghostGroup == null && AudioManager.Instance != null)
            {
                ghostGroup = AudioManager.Instance.GhostGroup;
            }

            if (ghostAI == null)
            {
                Debug.LogError($"{nameof(GhostAudioEmitter)} requires a {nameof(GhostAIController)}.", this);
            }

            if (ghostAudioSource == null)
            {
                Debug.LogError($"{nameof(GhostAudioEmitter)} requires a {nameof(AudioSource)}.", this);
            }

            ConfigureSpatialAudio();
        }

        private void OnEnable()
        {
            _lastKnownState = ghostAI != null ? ghostAI.CurrentState : GhostState.Idle;
            _lastClipIndex = -1;

            if (_stateAudioRoutine == null)
            {
                _stateAudioRoutine = StartCoroutine(StateAudioRoutine());
            }
        }

        private void OnDisable()
        {
            if (_stateAudioRoutine != null)
            {
                StopCoroutine(_stateAudioRoutine);
                _stateAudioRoutine = null;
            }

            if (ghostAudioSource != null && ghostAudioSource.isPlaying)
            {
                ghostAudioSource.Stop();
            }
        }

        private void ConfigureSpatialAudio()
        {
            if (ghostAudioSource == null)
            {
                return;
            }

            ghostAudioSource.spatialBlend = 1f;
            ghostAudioSource.minDistance = MinDistance;
            ghostAudioSource.maxDistance = MaxDistance;
            ghostAudioSource.rolloffMode = AudioRolloffMode.Custom;

            AnimationCurve horrorRolloff = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.12f, 0.75f),
                new Keyframe(0.35f, 0.35f),
                new Keyframe(1f, 0.28f));

            ghostAudioSource.SetCustomCurve(AudioSourceCurveType.CustomRolloff, horrorRolloff);

            if (ghostGroup != null)
            {
                ghostAudioSource.outputAudioMixerGroup = ghostGroup;
            }
        }

        private IEnumerator StateAudioRoutine()
        {
            WaitForSeconds wait = new WaitForSeconds(StateCheckInterval);

            while (enabled)
            {
                if (ghostAI != null && ghostAudioSource != null)
                {
                    EvaluateStateAudio(ghostAI.CurrentState);
                }

                yield return wait;
            }
        }

        private void EvaluateStateAudio(GhostState state)
        {
            if (state == GhostState.Attack && _lastKnownState != GhostState.Attack)
            {
                PlayRandomClip(attackSounds, 1f, loop: false);
            }
            else if (state == GhostState.Retreat && _lastKnownState != GhostState.Retreat)
            {
                PlayClipOnce(retreatSound, 0.3f);
            }
            else
            {
                switch (state)
                {
                    case GhostState.Idle:
                    case GhostState.Wander:
                        PlayRandomClip(wanderSounds, 0.2f, loop: true);
                        break;

                    case GhostState.Stalk:
                        PlayRandomClip(stalkSounds, 0.5f, loop: true);
                        break;

                    case GhostState.Chase:
                        PlayRandomClip(chaseSounds, 0.8f, loop: true);
                        break;
                }
            }

            _lastKnownState = state;
        }

        /// <summary>
        /// Plays a random clip from an array without repeating the same clip back to back.
        /// </summary>
        /// <param name="clips">Candidate clips.</param>
        /// <param name="volume">Playback volume from 0 to 1.</param>
        /// <param name="loop">Whether the source should loop.</param>
        public void PlayRandomClip(AudioClip[] clips, float volume, bool loop)
        {
            if (ghostAudioSource == null || clips == null || clips.Length == 0)
            {
                return;
            }

            int clipIndex = PickClipIndex(clips);
            if (clipIndex < 0)
            {
                return;
            }

            AudioClip selectedClip = clips[clipIndex];
            if (selectedClip == null)
            {
                return;
            }

            if (ghostAudioSource.isPlaying && ghostAudioSource.clip == selectedClip)
            {
                return;
            }

            _lastClipIndex = clipIndex;
            ghostAudioSource.loop = loop;
            ghostAudioSource.clip = selectedClip;
            ghostAudioSource.volume = Mathf.Clamp01(volume);
            ghostAudioSource.Play();
        }

        private void PlayClipOnce(AudioClip clip, float volume)
        {
            if (ghostAudioSource == null || clip == null)
            {
                return;
            }

            ghostAudioSource.loop = false;
            ghostAudioSource.clip = clip;
            ghostAudioSource.volume = Mathf.Clamp01(volume);
            ghostAudioSource.Play();
        }

        private int PickClipIndex(AudioClip[] clips)
        {
            if (clips == null || clips.Length == 0)
            {
                return -1;
            }

            if (clips.Length == 1)
            {
                return clips[0] != null ? 0 : -1;
            }

            int clipIndex;
            int safetyCounter = 0;

            do
            {
                clipIndex = Random.Range(0, clips.Length);
                safetyCounter++;
            }
            while (clipIndex == _lastClipIndex && safetyCounter < 8);

            return clips[clipIndex] != null ? clipIndex : -1;
        }
    }
}

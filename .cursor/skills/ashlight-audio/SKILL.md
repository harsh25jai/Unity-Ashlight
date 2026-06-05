---
name: ashlight-audio
description: >-
  Generates Ashlight audio systems — AudioManager mixer groups, fear-driven
  procedural audio, spatial ghost emitters. Use when implementing Audio module
  scripts, mixers, or horror sound design.
---

# Ashlight — Audio Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.Audio` namespace.

**Singleton note:** `AudioManager` may use a singleton-like access pattern only if explicitly designated alongside `GameManager` in design docs; prefer injection/events where possible. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Audio manager

Create an AudioManager.cs singleton using Unity Audio Mixer groups for: Ambient, SFX, Ghost, Music, Fear. Implement dynamic mixing that increases Ghost and Fear channels based on proximity and night phase.

### Fear audio system

Write a FearAudioSystem.cs that procedurally blends heartbeat intensity, whisper volume, and static noise based on a 0-1 fear value input. Uses AudioMixer snapshots and coroutine-driven crossfades.

### Spatial audio emitter

Generate a SpatialAudioEmitter.cs for ghost sounds that uses Unity's 3D audio with custom rolloff curves for horror effect. Ghost sounds should feel close even when ghosts are moderately distant.

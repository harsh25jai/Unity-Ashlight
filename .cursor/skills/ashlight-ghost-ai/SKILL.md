---
name: ashlight-ghost-ai
description: >-
  Generates Ashlight ghost AI — NavMesh state machine, light-aware perception,
  spawn manager with pooling, GhostTypeDefinition ScriptableObjects. Use when
  implementing Ghost/AI module scripts or enemy behavior.
---

# Ashlight — Ghost / AI Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.Ghost` or `Ashlight.AI` namespace. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Ghost AI controller

Create a GhostAIController.cs using Unity NavMesh and a state machine pattern (Idle, Wander, Stalk, Chase, Attack, Retreat). Ghost detects player via light-level-aware perception. Retreats when Holy Torch light hits it directly.

### Ghost perception

Write a GhostPerceptionSystem.cs that calculates player visibility based on distance, angle, and current light level. Returns a PerceptionLevel enum (Unaware/Suspicious/Detected). Used by all ghost AI types.

### Ghost spawn manager

Generate a GhostSpawnManager.cs that reads from a GhostSpawnConfig ScriptableObject defining spawn rates by time-of-night, max active ghosts, spawn point weights, and night cycle multipliers. Uses object pooling.

### Ghost type definition

Create a GhostTypeDefinition.cs ScriptableObject containing: speed, damage, light resistance, detection range, attack range, special ability type, and visual material reference. Create 5 ghost type assets from the game design.

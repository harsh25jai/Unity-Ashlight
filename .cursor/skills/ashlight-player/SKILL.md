---
name: ashlight-player
description: >-
  Generates Ashlight player systems — isometric movement, Holy Torch, stats
  ScriptableObjects, 8-direction animation. Use when implementing Player module
  scripts, input, torch, or character controllers.
---

# Ashlight — Player Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.Player` (or submodule) namespace. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Player controller

Generate a PlayerController.cs for Unity 6 isometric 2.5D movement using the new Input System. WASD movement relative to isometric camera angle, smooth rotation toward movement direction, configurable walk/run speeds, and stamina system.

### Holy torch

Create a HolyTorch.cs component that manages fuel (Holy Water), emits a dynamic point light with configurable radius/intensity, spawns holy particle effects, and exposes events for: torch lit, torch low (20% fuel), torch extinguished.

### Player stats

Write a PlayerStats.cs using ScriptableObjects for base values (health, stamina, fear resistance, carry capacity). Include modifier system for upgrades. Expose UnityEvents for stat change callbacks.

### Isometric animation

Generate an IsometricAnimationController.cs that maps 8-directional movement to sprite animations in a 2.5D isometric context, handles idle/walk/run/interact states using Unity Animator with blend trees.

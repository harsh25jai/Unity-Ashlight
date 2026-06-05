---
name: ashlight-environment
description: >-
  Generates Ashlight environment systems — isometric camera, forest fog/shadows,
  day/night URP cycle, procedural forest population. Use when implementing
  Environment module scripts or isometric world/atmosphere features.
---

# Ashlight — Environment Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under an appropriate `Ashlight.Environment` (or submodule) namespace. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Isometric camera

Create an IsometricCameraController.cs for Unity 6 that maintains a fixed 45-degree isometric view, follows the player smoothly with configurable damping, and supports camera shake for fear effects. Include camera bounds clamping.

### Forest environment manager

Generate a ForestEnvironmentManager.cs that manages dynamic fog density using Unity's VolumeProfile, creates shadow pocket zones as trigger colliders, and controls ambient lighting shifts between day and night states using AnimationCurves.

### Day / night cycle

Write a DayNightCycle.cs ScriptableObject-driven system for Unity 6 URP that smoothly transitions directional light color/intensity, fog density, ambient light, and triggers phase-change events. Day=8min, Dusk=2min, Night grows each cycle.

### Procedural forest populator

Create a ProceduralForestPopulator.cs that scatters trees, rocks, roots, and shrines across a defined grid using Poisson disk sampling. Must avoid overlap with NavMesh obstacles and player spawn area.

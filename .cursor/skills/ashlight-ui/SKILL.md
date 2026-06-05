---
name: ashlight-ui
description: >-
  Generates Ashlight UI and HUD — UI Toolkit HUD (Holy Water, torch, fear,
  night phase, minimap), torch vignette shader effects. Use when implementing
  UI/HUD module scripts or screen-space effects.
---

# Ashlight — UI / HUD Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.UI` namespace. Add tests for each new MonoBehaviour where feasible (UI logic may use testable presenters/view-models).

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### HUD manager

Create a HUDManager.cs using Unity UI Toolkit that displays: Holy Water bar (animated depletion), torch flame indicator, fear meter (subtle vignette-based), night phase indicator, and minimap toggle.

### Torch UI effect

Write a TorchUIEffect.cs that drives a screen-space vignette shader parameter based on torch fuel level. At 100% fuel: minimal vignette. At 0%: heavy dark vignette with red edge pulse.

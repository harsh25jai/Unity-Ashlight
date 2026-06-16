---

name: ashlight-ui
description: >-
Generates high-quality Ashlight UI systems — including UI Toolkit HUD,
story-driven menus, animation systems, and screen-space effects.
Optimized for performance, readability, and immersive UX.
---------------------------------------------------------

# Ashlight — UI / HUD + Menu System (Production Grade)

Apply `.cursorrules` / `ashlight-unity` conventions.
Use namespace: `Ashlight.UI`.

Separate logic using:

* View (MonoBehaviour / UIDocument)
* Presenter / ViewModel (testable logic)

Prefer **UI Toolkit** for HUD and menus.

---

# 🧠 CORE PRINCIPLES (MANDATORY)

## Minimal HUD Design

* Show only critical gameplay info
* Use icons + short text
* Place elements at screen edges
* Avoid center clutter

## Story-Driven UI

All menus must reflect game tone:

* Horror → flicker, distortion, darkness
* Survival → rough, diegetic feel
* Fantasy → ornate, soft animations

## Motion Design Rules

* Use subtle, fast animations (0.15–0.4s)
* Never block input with long transitions
* Prioritize responsiveness over flair

## Performance

* Avoid excessive UI rebuilds
* Use USS classes instead of runtime layout changes
* Batch updates via data binding

---

# 🧩 UI ARCHITECTURE

## Root Structure

UIDocument
├── MainMenu
├── HUD
├── PauseMenu
├── OverlayEffects (vignette, pulse, damage)

---

# 🎮 GENERATION PROMPTS

Use these prompts exactly:

---

## HUD Manager (Advanced)

Create a modular HUDManager.cs using Unity UI Toolkit.

Requirements:

* Holy Water bar (smooth animated depletion using lerp)
* Torch fuel indicator (icon + radial fill)
* Fear meter (drives vignette intensity, not just bar)
* Night phase indicator (text + subtle pulse)
* Minimap toggle (visibility + fade animation)

Architecture:

* Separate logic into HUDPresenter (testable)
* Use events or data binding (avoid polling)
* Avoid Update() where possible

---

## Torch Screen Effect (Enhanced)

Write TorchUIEffect.cs that controls a vignette shader.

Behavior:

* 100% fuel → minimal vignette
* 50% → noticeable darkening
* 0% → heavy vignette + red pulse + breathing effect

Add:

* Smooth interpolation
* Optional noise flicker for horror tone

---

## Main Menu Generator

Create a MainMenuController.cs using UI Toolkit.

Menu must include:

* Title
* Play / Continue / Settings / Quit
* Animated or reactive background

UX Rules:

* Vertical layout with clear hierarchy
* Strong selected-state feedback
* Keyboard + controller navigation support

Animations:

* Fade-in on load
* Button hover scale (~1.05x)
* Panel transitions (fade or slide)

Theme:

* Dark, eerie, minimal (Ashlight tone)

---

## Pause Menu

Create PauseMenuController.cs.

Features:

* Resume
* Settings
* Quit to menu

Behavior:

* Freeze time (Time.timeScale = 0)
* Background dim or blur
* Animate entry (scale + fade)

---

## UI Animation Utility

Create UIAnimator.cs utility.

Support:

* Fade (opacity / CanvasGroup equivalent)
* Scale transitions
* Slide transitions

Constraints:

* Reusable across all UI
* Avoid coroutine spam (centralized animation handling)

---

# 🎨 UI TOOLKIT BEST PRACTICES

* Use USS classes (no inline styling)
* Use Flexbox layouts (row/column)
* Keep hierarchy shallow
* Prefer % sizes over fixed pixels
* Use `display: none` instead of destroying elements

---

# 🧪 TESTING

For each system:

* Extract Presenter logic
* Unit test:

  * Value clamping
  * State transitions
  * UI mapping correctness

---

# ⚠️ ANTI-PATTERNS (DO NOT GENERATE)

* Monolithic UI scripts
* UI logic in Update loops
* Hardcoded pixel layouts
* Slow or blocking animations
* HUD elements in screen center

---

# 🚀 OUTPUT REQUIREMENTS

Every generation must include:

1. Clean C# scripts (modular)
2. UXML layout structure
3. USS styling (extensible)
4. Animation handling
5. Logic/UI separation

---

# 🎯 GOAL

Generate UI that is:

* Responsive
* Minimal
* Immersive
* Thematically consistent

Not just functional.
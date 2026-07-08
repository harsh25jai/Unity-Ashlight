# Ashlight

> Survive an isometric forest at night — keep your Holy Torch lit, outrun the ghosts, and reach the church.

[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

## About

Ashlight is a horror survival game built in **Unity 6 LTS** with the Universal Render Pipeline. You explore a fog-shrouded forest after dark, managing torch fuel and fear while hostile ghosts hunt by sound and light. Church safe zones offer respite, upgrades, and save points. The project is under active development.

## Requirements

- [Unity 6 LTS](https://unity.com/releases/lts) — `6000.4.10f1` (see `ProjectSettings/ProjectVersion.txt`)
- Windows, macOS, or Linux editor

## Getting Started

1. Clone the repository and open the project folder in **Unity Hub**.
2. Let Unity import packages and compile scripts (first open may take a few minutes).
3. Open `Assets/Scenes/MainMenu.unity` and press **Play**, or open `Assets/Scenes/SampleScene.unity` to jump straight into the forest.

```bash
git clone <repository-url>
# Unity Hub → Add → select the Unity-Ashlight folder → Open with 6000.4.10f1
```

## Features

- **Isometric exploration** — Cinemachine camera, forest fog, and a day/night horror cycle
- **Holy Torch** — dual-light weapon that damages ghosts; fuel drains faster in combat and flickers when low
- **Ghost AI** — NavMesh-driven enemies (Wraith, Hollow, Stalker, Grabber, Elder Spirit) with light-aware perception and pooled spawning
- **Fear system** — proximity-driven vignette, camera shake, stamina drain, and procedural audio
- **Church safe zones** — repel ghosts, regenerate stamina, worship interactions, and JSON save/load
- **Resources & upgrades** — Holy Water inventory, Faith currency, and a ScriptableObject upgrade tree
- **UI Toolkit HUD** — torch, faith, holy water, ghost health, and celestial cycle indicators

## Controls

| Input | Action |
|-------|--------|
| `W A S D` / Arrow keys | Move |
| `Left Shift` (hold) | Run |
| `E` | Interact |
| `Q` | Use ability / activate Holy Water |

Bindings are defined in `Assets/Input/PlayerInputActions.inputactions`.

## Project Layout

```
Assets/
├── Scenes/          MainMenu, SampleScene (forest play area)
├── Scripts/
│   ├── Player/      Movement, stats, faith
│   ├── Ghost/       AI, spawning, perception
│   ├── Environment/ Camera, day/night, church interactions
│   ├── Systems/     Torch, fear, saves, upgrades, holy water
│   ├── Audio/       Mixer, ambient, fear-driven audio
│   └── UI/          HUD, main menu, upgrade station
├── Data/            ScriptableObjects (upgrades, settings)
└── UI/              UI Toolkit UXML/USS
```

Conventions: namespace `Ashlight.[Module]`, composition over inheritance, `GameManager` as the sole singleton. See `.cursorrules` for full coding standards.

## Development

- **Tests** — Window → General → Test Runner (Edit Mode tests live under `Assets/Scripts/**/Tests/Editor/`).
- **Cursor workflow** — module-specific agent skills in `.cursor/skills/` (environment, player, ghost AI, audio, UI, and more).
- **Key packages** — URP, Cinemachine, AI Navigation, Input System, UI Toolkit, Test Framework.

```bash
# After changing scripts, run Edit Mode tests in the Unity Test Runner before committing.
```

## License

MIT — see [LICENSE](LICENSE).

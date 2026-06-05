---
name: ashlight-church
description: >-
  Generates Ashlight church and safe-zone systems — repel ghosts, regen, save
  triggers, Upgrade Station UI Toolkit, JSON SaveSystem. Use when implementing
  Church/Safe Zone module scripts, saves, or upgrade UI.
---

# Ashlight — Church / Safe Zone Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.Church` or `Ashlight.SafeZone` namespace. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Church safe zone

Create a ChurchSafeZone.cs that defines a trigger area where: all ghosts are instantly repelled, player health/stamina regenerates over time, saving triggers, and upgrade UI can be accessed. Must block ghost NavMesh entry.

### Upgrade station UI

Write an UpgradeStationUI.cs using Unity UI Toolkit that displays the upgrade tree, shows current Faith balance, highlights affordable upgrades, plays purchase animations, and updates stats in real-time.

### Save system

Generate a SaveSystem.cs using JSON serialization that saves: player position, inventory state, purchased upgrades, night number, discovered areas, and shrine activation states. Include auto-save on church entry.

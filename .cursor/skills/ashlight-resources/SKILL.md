---
name: ashlight-resources
description: >-
  Generates Ashlight resource and inventory systems — Holy Water inventory,
  upgrade tree with Faith currency, pickup base classes. Use when implementing
  Resource/Inventory module scripts, pickups, or upgrades.
---

# Ashlight — Resource / Inventory Module

Apply `.cursorrules` / `ashlight-unity` conventions. Place scripts under `Ashlight.Resources` or `Ashlight.Inventory` namespace. Add tests for each new MonoBehaviour.

## Generation prompts

Use these prompts verbatim in Composer or Chat:

### Holy Water inventory

Write a HolyWaterInventory.cs that tracks current/max Holy Water, handles drain (torch), spend (abilities), and replenish (church/shrines). Uses a ScriptableObject for capacity values. Fires events on critical levels.

### Upgrade system

Create an UpgradeSystem.cs that reads from UpgradeTree ScriptableObject, checks Faith currency, applies upgrades by modifying player/torch stats via the modifier system, and persists purchased upgrades to JSON save file.

### Resource pickups

Generate a ResourcePickup.cs base class and HolyWaterPickup.cs, FaithOrb.cs, RelicPickup.cs child classes. Include floating animation, glow effect trigger, pickup range detection, and inventory integration.

---
name: ashlight-development
description: >-
  Ashlight Unity 6 vibe-coding workflow in Cursor — setup, Composer vs Chat,
  post-generation checks, Test Runner, and git commits. Use when starting Ashlight
  work, onboarding to the repo, or asking how to develop Ashlight with Cursor.
---

# Ashlight Development (Cursor)

## Recommended Cursor settings

- **Model**: Claude Sonnet 4.5 (best for Unity C# generation)
- **Context window**: Max — always include full project context
- **Enable**: Codebase indexing for cross-file awareness
- **`.cursorignore`**: `Library/`, `Temp/`, `Build/`, `.git/` (project root)

Project conventions live in `.cursorrules` and `.cursor/rules/ashlight-unity.mdc`.

## Vibe coding workflow

1. Open **Cursor Composer** (`Cmd+I` / `Ctrl+I`) for multi-file generation.
2. Use **Chat** (`Cmd+L`) for single-file questions and fixes.
3. After each generation: read the code; check for null refs and missing `[SerializeField]` tags.
4. Run **Unity Test Runner** immediately after each module — fix before moving on.
5. Use **inline edit** (`Cmd+K`) to tweak specific methods without regenerating.
6. **Commit to Git** after each working module — never lose a working state.

## Module skills

Use the matching skill (or paste its prompts) when implementing a system:

| Module | Skill |
|--------|--------|
| Environment | `ashlight-environment` |
| Player | `ashlight-player` |
| Ghost / AI | `ashlight-ghost-ai` |
| Resource / Inventory | `ashlight-resources` |
| Church / Safe Zone | `ashlight-church` |
| Audio | `ashlight-audio` |
| UI / HUD | `ashlight-ui` |

## Post-generation checklist

- [ ] Namespace `Ashlight.[Module]`
- [ ] XML docs on public API
- [ ] No `GetComponent` in `Update()`
- [ ] Null checks in `Awake`/`Start`
- [ ] Corresponding Edit Mode / Play Mode test for new MonoBehaviours
- [ ] Only `GameManager` is a singleton

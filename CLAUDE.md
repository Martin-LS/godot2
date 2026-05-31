# CLAUDE.md — godot1


## Project Overview

Top-down auto-attack horde survival game (Vampire Survivors / Diablo style). Godot 4.6, C#, Forward Plus renderer. 3D world with custom voxel-art style characters, perspective camera.

## Docs

- `docs/gdd-mechanics.md` — Game design: combat, skills, EoT, characters, enemies, run structure, win/lose, future notes
- `docs/gdd-progression.md` — Game design: meta-progression, gear, augments, crafting, currencies, UI/menus
- `docs/technical-scene.md` — Architecture: scene layout, scene flow, core systems table, signals, C# conventions, rendering, third-party tools
- `docs/technical-systems.md` — Architecture: data types, save format, crafting methods, EoT system, damage pipeline, enemy spawner, drop system
- `docs/technical-assets.md` — 3D asset pipeline: visual style, proportions, rig standard, animation clip names, Blender export settings, Godot import settings

**Read the relevant doc before making design or architectural decisions.**

## Scope Rules

- **Bug fixes are strictly localised to the reported bug.** Change only the line(s) directly causing the issue. Do not refactor, rename, restructure, or clean up anything else — not in the same file, not in related files.
- **If a related flaw or issue is spotted while investigating, stop, report it to the user, and wait for instruction before touching it.**
- **Do not change data models, method signatures, or architecture as part of a bug fix unless explicitly asked.**
## Tools

- **Godot MCP Pro** is connected — use `mcp__godot-mcp-pro__*` tools to inspect/modify the live editor
- MCP tools are auto-approved globally
- Proactively use `play_scene`, `get_game_screenshot`, `get_output_log`, `get_editor_errors` to verify changes work before reporting done

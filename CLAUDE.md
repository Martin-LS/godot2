# CLAUDE.md — godot1


## Project Overview

Top-down auto-attack horde survival game (Vampire Survivors / Diablo style). Godot 4.6, C#, Forward Plus renderer. 3D world with billboarded 2D sprites, orthographic isometric camera.

## Docs

- `docs/GDD.md` — Game design: mechanics, characters, enemies, progression, UI/menus
- `docs/TECHNICAL.md` — Architecture: scene layout, systems, data types, signals, save layers, C# conventions, rendering decisions
- `docs/BALANCE.md` — All tunable numbers: XP curves, enemy scaling, drop rates, stat growth
- `docs/PLAYTEST.md` — Playtest observations and feedback

**Read the relevant doc before making design or architectural decisions.**

## Tools

- **Godot MCP Pro** is connected — use `mcp__godot-mcp-pro__*` tools to inspect/modify the live editor
- MCP tools are auto-approved globally
- Proactively use `play_scene`, `get_game_screenshot`, `get_output_log`, `get_editor_errors` to verify changes work before reporting done

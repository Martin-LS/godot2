# CLAUDE.md — godot1

> **Maintenance rule:** Keep this file and everything in `docs/` in sync with the actual project. Any time you add, move, rename, or delete a scene, script, system, or doc — update the affected files and this file before considering the task done.

## Project Overview

Top-down auto-attack horde survival game (Vampire Survivors style). Godot 4.6, C#, Forward Plus renderer.

## Key Docs

- `docs/GDD.md` — Game design: mechanics, characters, enemies, meta-progression, UI/menus
- `docs/TECHNICAL.md` — Architecture: scene layout, systems table, data types, signals, save layers, C# conventions

**Read these before making design or architectural decisions.** Both are living documents — update them when systems change.

## Project Layout

```
src/
  character/   CharacterData, CharacterManager (autoload), CharacterType
  enemies/     EnemyController, EnemySpawner, enemy.tscn
  hud/         Hud.cs, hud.tscn
  player/      PlayerController, player.tscn
  run/         RunSession (run timer, win/lose detection)
  ui/          MainMenu, CharacterSelect, UpgradePicker, RunEndOverlay, MetaUpgradesPanel + their .tscn files
  weapon/      WeaponController, Projectile, projectile.tscn
  xp/          XpGem, xp_gem.tscn
  meta/        MetaUpgradeType (enum), CoinPickup, coin_pickup.tscn
docs/
  GDD.md
  TECHNICAL.md
main.tscn      Run scene (entry point during a run)
project.godot  Main scene: src/ui/main_menu.tscn
```

## Scene Flow

```
src/ui/main_menu.tscn  →  src/ui/character_select.tscn  →  main.tscn
```

`CharacterManager` autoload holds the selected character across transitions.

## C# Conventions

- Namespaces: `Godot1.<System>`
- Node classes: PascalCase (`PlayerController`, `WeaponController`)
- Resource classes: suffix `Data` (`EnemyData`, `WeaponData`)
- Private fields: `_camelCase`; public properties: `PascalCase`
- Signals: `[Signal]` delegate, past-tense (`HealthChanged`, `EnemyDied`)
- Systems communicate via signals only — no direct cross-system method calls

## Save Layers

- **Persistent:** `user://characters.json` — managed by `CharacterManager`, written on every create/delete/upgrade
- **In-memory:** `RunSession` node — discarded on run end, results flushed to character via `CharacterManager.RecordRunCompletion()`

## Tools

- **Godot MCP Pro** is connected — use `mcp__godot-mcp-pro__*` tools to inspect/modify the live editor
- MCP tools are auto-approved globally

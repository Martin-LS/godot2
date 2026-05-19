# Technical Design Document — godot1

> Living document — architecture will evolve as systems are built and playtested.

## Architecture Overview

Godot 4.6, C#, Forward Plus renderer. Scene composition over inheritance — each system is a self-contained scene or node that communicates via signals. Two save layers: a persistent save file (meta) and an in-memory run session (discarded on run end).

---

## Scene Flow

```
main_menu.tscn  →  character_select.tscn  →  main.tscn
```

`CharacterManager` (autoload) holds the selected character across scene transitions.

## Scene Layout

### `src/ui/main_menu.tscn`
```
MainMenu (Control)
└── VBox (VBoxContainer)
    ├── Title (Label)
    └── PlayButton (Button)
```

### `src/ui/character_select.tscn`
```
CharacterSelect (Control)
└── HSplit (HSplitContainer)
    ├── Left (VBoxContainer)
    │   ├── CharactersLabel (Label)
    │   ├── Scroll (ScrollContainer)
    │   │   └── CharacterList (VBoxContainer)  ← cards added at runtime
    │   ├── NewCharacterButton (Button)
    │   └── StartRunButton (Button)
    └── Right (VBoxContainer)
        └── CreatePanel (Panel)
            └── VBox (VBoxContainer)
                ├── CreateLabel, NameInput, WarriorBtn, RogueBtn, MageBtn
                ├── ConfirmBtn, CancelBtn
```

### `main.tscn` (run scene)
```
Main (Node)
├── Player (CharacterBody2D)   ← stats seeded from CharacterManager.SelectedCharacter
│   ├── CollisionShape
│   ├── Camera2D
│   └── Weapon (Node)
├── Background (Node2D)
├── Hud (CanvasLayer)
└── EnemySpawner (Node)
```

> Provisional — update as scenes are created.

---

## Core Systems

| System            | Responsibility                                               | Path                      |
|-------------------|--------------------------------------------------------------|---------------------------|
| CharacterManager  | Autoload — load/save characters, hold selected character     | `res://src/character/`    |
| Player            | Input, movement, stat sheet, taking damage                   | `res://src/player/`       |
| Weapon            | Auto-attack, targeting nearest enemy, firing on cooldown     | `res://src/weapon/`       |
| EnemySpawner      | Time-based wave scaling, spawning enemy scenes               | `res://src/enemies/`      |
| Enemy             | AI (chase), taking damage, death + XP gem spawning           | `res://src/enemies/`      |
| XpGem             | XP pickup — auto-collected on contact                        | `res://src/xp/`           |
| Hud               | Health bar, XP bar, level label — reacts to player signals   | `res://src/hud/`          |
| RunSession        | Tracks elapsed time, XP, current level, run state           | `res://src/run/`          |
| UpgradePicker     | Pause game, present N choices, apply selected upgrade        | `res://src/ui/`           |
| MetaProgression   | Per-character permanent upgrades, coin bank                  | `res://src/meta/`         |

---

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, TotalXpEarned, BonusMaxHealth, BonusSpeed, BonusDamage |
| `CharacterType`     | C# enum     | Warrior, Rogue, Mage                                           |
| `WeaponData`        | Godot Resource | Name, base damage, cooldown, upgrade path                   |
| `WeaponUpgradeData` | Godot Resource | Damage delta, cooldown delta, new behaviour flags           |
| `UpgradeOptionData` | Godot Resource | Display name, description, effect type + value              |
| `EnemyData`         | Godot Resource | HP, speed, damage, XP value, drop table weights             |

---

## Save Layers

### Character Save (`user://characters.json`)
Managed by `CharacterManager` autoload. Written on every create/delete/upgrade.
```json
{
  "characters": [
    {
      "id": "<guid>",
      "name": "Ironclad",
      "type": "Warrior",
      "runsCompleted": 3,
      "totalXpEarned": 420,
      "bonusMaxHealth": 10,
      "bonusSpeed": 0,
      "bonusDamage": 5
    }
  ]
}
```

### Run Session (in-memory only)
Lives on the `RunSession` node. Discarded when the scene unloads. On run end, results are flushed back to the character via `CharacterManager.RecordRunCompletion()`.
- Elapsed time
- Current XP + level
- Upgrades chosen this run
- Coins earned this run

---

## Weapon Upgrade Path

Weapon is a single entity that evolves. On level-up, one upgrade choice may advance the weapon along its path.

```
WeaponData
  └── UpgradePath: WeaponUpgradeData[]
        [0] → Stage 1 (base)
        [1] → Stage 2 (faster fire)
        [2] → Stage 3 (piercing)
        [3] → Stage 4 (AoE explosion)
```

Current stage index stored on the player/weapon instance during the run.

---

## Enemy Spawner — Wave Scaling

Time-driven, no fixed waves. Every N seconds the spawner recalculates:
- **Spawn rate** — increases with time
- **Enemy pool** — harder variants unlock at time thresholds
- **Horde size** — group spawns grow larger over time

Final boss spawns when the run timer expires.

---

## Drop System

Each enemy holds a weighted drop table from its `EnemyData`.

| Drop          | Default weight |
|---------------|---------------|
| Nothing       | High           |
| XP gem (small)| Medium         |
| XP gem (large)| Low            |
| Coin          | Low            |
| Health pickup | Very low       |

---

## Class Conventions (C#)

- **Namespaces:** `Godot1.<System>` (e.g. `Godot1.Player`, `Godot1.Combat`)
- **Node classes:** PascalCase — `PlayerController`, `EnemyBase`, `WeaponController`
- **Resource classes:** suffix `Data` — `EnemyData`, `WeaponData`, `GearData`
- **Private fields:** `_camelCase`; public properties: `PascalCase`
- **Signals:** `[Signal]` delegate, past-tense — `HealthChanged`, `EnemyDied`, `LeveledUp`
- **Folder layout:** `src/<system>/` mirrors namespace

---

## Signals & Events

Systems communicate via signals only — no direct cross-system method calls.

| Signal                  | Emitter        | Receivers                        |
|-------------------------|----------------|----------------------------------|
| `HealthChanged(int)`    | Player         | HUD, GameManager                 |
| `PlayerDied`            | Player         | RunSession (end run)             |
| `LeveledUp(int)`        | RunSession     | UpgradePicker (show choices)     |
| `UpgradeChosen(data)`   | UpgradePicker  | Player, WeaponController         |
| `EnemyDied(position)`   | Enemy          | DropSpawner, RunSession (XP)     |
| `XpCollected(int)`      | Pickup         | RunSession                       |
| `CoinCollected(int)`    | Pickup         | RunSession                       |
| `RunTimerExpired`       | RunSession     | EnemySpawner (spawn boss)        |
| `RunEnded(result)`      | RunSession     | SaveManager (flush coins/rewards)|

---

## Third-party / Tools

| Tool           | Purpose                               |
|----------------|---------------------------------------|
| Godot MCP Pro  | AI-assisted editor control via Claude |

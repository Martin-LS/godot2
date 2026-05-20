# Technical Design Document — godot1

> Living document — architecture will evolve as systems are built and playtested.

## Architecture Overview

Godot 4.6, C#, Forward Plus renderer. Scene composition over inheritance — each system is a self-contained scene or node that communicates via signals. Two save layers: a persistent save file (meta) and an in-memory run session (discarded on run end).

---

## Scene Flow

```
main_menu.tscn  →  character_select.tscn  →  character_screen.tscn  →  main.tscn
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
    │   │   └── CharacterList (VBoxContainer)  ← cards added at runtime; clicking a card navigates to character_screen
    │   └── NewCharacterButton (Button)
    └── Right (VBoxContainer)
        └── CreatePanel (Panel)
            └── VBox (VBoxContainer)
                ├── CreateLabel, NameInput, WarriorBtn, RogueBtn, MageBtn
                └── ConfirmBtn, CancelBtn
```

### `src/ui/character_screen.tscn`
```
CharacterScreen (Control)
└── VBox (VBoxContainer, centered ~500px wide)
    ├── NameLabel (Label, 32px font)
    ├── TypeLabel (Label)
    ├── LevelLabel (Label)
    ├── StatsLabel (Label)
    ├── Spacer (Control, expand)
    └── Buttons (HBoxContainer)
        ├── BackButton  → character_select.tscn
        └── StartRunButton → main.tscn
```
> Future home of: meta-upgrade panel, gear slots, crafting.

### `main.tscn` (run scene)
```
Main (Node)
├── Player (CharacterBody2D)   ← stats seeded from CharacterManager.SelectedCharacter
│   ├── CollisionShape
│   ├── Camera2D
│   └── Weapon (Node)
├── Background (Node2D)
├── Hud (CanvasLayer)
├── EnemySpawner (Node)
├── UpgradePicker (CanvasLayer)  ← shown on LeveledUp; pauses tree
├── RunSession (Node)            ← tracks elapsed time; emits RunEnded(won, level, elapsed)
└── RunEndOverlay (CanvasLayer)  ← shown on RunEnded; returns to main menu
```

> Provisional — update as scenes are created.

---

## Core Systems

| System            | Responsibility                                               | Path                      | Status |
|-------------------|--------------------------------------------------------------|---------------------------|--------|
| CharacterManager  | Autoload — load/save characters, hold selected character     | `res://src/character/`    | ✅ done |
| Player            | Input, movement, stat sheet, taking damage                   | `res://src/player/`       | ✅ done |
| Weapon            | Auto-attack, targeting nearest enemy, firing on cooldown     | `res://src/weapon/`       | ✅ done |
| EnemySpawner      | Time-based wave scaling, spawning enemy scenes               | `res://src/enemies/`      | ✅ done |
| Enemy             | AI (chase), taking damage, death + XP gem spawning           | `res://src/enemies/`      | ✅ done |
| XpGem             | XP pickup — auto-collected on contact                        | `res://src/xp/`           | ✅ done |
| Hud               | Health bar, XP bar, level, coin counter, run timer           | `res://src/hud/`          | ✅ done |
| RunSession        | Run timer, win/lose detection, emits RunEnded signal         | `res://src/run/`          | ✅ done |
| UpgradePicker     | (removed from scene — code kept dormant)                     | `res://src/ui/`           | ❌ removed |
| RunEndOverlay     | Show win/die results, flush run to character, return to menu | `res://src/ui/`           | ✅ done |
| CoinPickup        | Coin drop (25% on enemy death) — reports to RunSession       | `res://src/meta/`         | ✅ done |
| MetaProgression   | Per-character coin bank + permanent upgrades (HP/Speed/DMG)  | `res://src/meta/`, `src/ui/` | ✅ done |
| HealthPickup      | Health drop (10% on enemy death) — heals player on contact   | `res://src/health/`       | ✅ done |

---

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, CurrentLevel, CurrentXp, CoinBank, BonusMaxHealth, BonusSpeed, BonusDamage |
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
      "currentLevel": 7,
      "currentXp": 12,
      "coinBank": 150,
      "bonusMaxHealth": 10,
      "bonusSpeed": 0,
      "bonusDamage": 5
    }
  ]
}
```

### Run Session (in-memory only)
Lives on the `RunSession` node. Discarded when the scene unloads. On run end, `CharacterManager.RecordRunCompletion(finalLevel, finalXp, coinsEarned)` writes the persistent state.
- Elapsed time
- Coins earned this run

Level and XP are NOT run-scoped — they live on `CharacterData` and are written back at run end.

### Future: Profile Envelope
If multi-user slots or cloud saves are ever needed, evaluate wrapping save data under a profile envelope. `CharacterManager` is the only entry point — the refactor scope is bounded (1 constant, a handful of callers).

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

Time-driven, no fixed waves. `EnemySpawner` recalculates each spawn:
- **Spawn rate** — increases with time; `InitialInterval / (1 + minutes * 0.5)`, clamped to `MinInterval = 0.3s`
- **Enemy types** — unlocked by elapsed minutes, chosen randomly from the available pool:

| Type     | Sprite row | Unlocks | Speed | HP | Damage |
|----------|-----------|---------|-------|----|--------|
| Standard | 6 (grey)  | 0:00    | 80    | 30 | 10     |
| Runner   | 4 (purple)| 1:00    | 140   | 15 | 8      |
| Tank     | 2 (orange)| 2:00    | 45    | 80 | 18     |

All types receive a time-scaling bonus on top: `Speed += 10 * minutes`, `MaxHealth += 5 * (int)minutes`.

---

## Drop System

Current implementation — drops are hardcoded in `EnemyController.Die()`:

| Drop         | Chance | Notes                                                          |
|--------------|--------|----------------------------------------------------------------|
| XP gem       | 100%   | Always dropped; value = 5 XP                                  |
| Coin         | 25%    | `CoinPickup` auto-collected; reports to `RunSession.AddCoin()` |
| Health pack  | 10%    | `HealthPickup` heals player for 15 HP on contact              |

> Planned: large XP gems, weighted drop tables via `EnemyData` resource.

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
| `LeveledUp(int)`        | Player         | Hud (level display)              |
| `UpgradeChosen(data)`   | UpgradePicker  | Player, WeaponController         |
| `EnemyDied(position)`   | Enemy          | DropSpawner, RunSession (XP)     |
| `XpCollected(int)`      | Pickup         | RunSession                       |
| `CoinCollected(int)`    | Pickup         | RunSession                       |
| `CoinChanged(int)`      | RunSession     | Hud (coin counter)               |
| `RunTimerExpired`       | RunSession     | EnemySpawner (spawn boss)        |
| `RunEnded(result)`      | RunSession     | SaveManager (flush coins/rewards)|

---

## Third-party / Tools

| Tool           | Purpose                               |
|----------------|---------------------------------------|
| Godot MCP Pro  | AI-assisted editor control via Claude |

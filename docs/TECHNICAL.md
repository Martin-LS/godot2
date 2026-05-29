# Technical Design Document — godot1

> Living document — architecture will evolve as systems are built and playtested.

## Architecture Overview

Godot 4.6, C#, Forward Plus renderer. **3D billboard** — game world is 3D (CharacterBody3D, XZ movement plane, Y-up); characters and enemies are rendered as `Sprite3D` billboard sprites that always face the camera. Camera is orthographic, fixed ~45° isometric tilt (Diablo-style), no player rotation. UI is 2D (`Control` / `CanvasLayer`) as standard in Godot — unaffected by the 3D world. Scene composition over inheritance — each system is a self-contained scene or node that communicates via signals. Two save layers: a persistent save file (meta) and an in-memory run session (discarded on run end).

---

## Rendering & Camera

| Decision         | Choice                        | Rationale                                                                 |
|------------------|-------------------------------|---------------------------------------------------------------------------|
| World dimensions | 3D, XZ movement plane, Y-up   | Standard for top-down 3D; gravity, navmesh, and lighting all assume Y-up  |
| Camera type      | `Camera3D`, orthographic      | No perspective distortion — correct pairing for billboarded sprites        |
| Camera angle     | Fixed ~51° isometric tilt     | Diablo 4 uses ~50°; no player rotation                                     |
| Character render | `Sprite3D` billboard          | Kenney 2D sprite sheets; billboard faces camera at all times               |
| Projectiles      | Physical traveling objects    | Visible projectile travel is core to ARPG feel (not raycasts)              |

### Camera Values (`src/CameraFollow.cs`)

| Property             | Current value       | Notes                                                         |
|----------------------|---------------------|---------------------------------------------------------------|
| `Offset`             | `(0, 200, 240)`     | World-unit offset from player; ratio gives ~40° tilt          |
| `Projection`         | Orthographic        | —                                                             |
| `Size`               | `200`               | World units visible vertically                                |
| `Sprite3D.PixelSize` | `2.0` (player/enemy), `1.5` (pickups) | 16px sprite → 32 world units tall |
| Checker tile size    | `16` world units    | Set in `CheckerBackground.cs` shader (`world_pos.xz / 16.0`) |

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
    ├── GearPanel (VBoxContainer)
    │   ├── GearLabel (Label)
    │   ├── WeaponSlotButton (Button)    ← click → ItemPickerPanel for Weapon slot
    │   ├── ArmorSlotButton (Button)     ← click → ItemPickerPanel for Armor slot
    │   └── AccessorySlotButton (Button) ← click → ItemPickerPanel for Accessory slot
    ├── InventoryPanel (VBoxContainer)
    │   ├── InventoryLabel (Label)
    │   └── InventoryScroll (ScrollContainer, min height 120px)
    │       └── InventoryList (VBoxContainer) ← labels added at runtime, one per owned item
    ├── Spacer (Control, expand)
    └── Buttons (HBoxContainer)
        ├── BackButton  → character_select.tscn
        └── StartRunButton → main.tscn
```

### `src/ui/item_picker_panel.tscn`
Modal overlay opened from CharacterScreen slot buttons.
```
ItemPickerPanel (Control, full-screen)
├── Dim (ColorRect, semi-transparent black)
└── Panel (PanelContainer, centered)
    └── VBox (VBoxContainer)
        ├── TitleLabel (Label)
        ├── Scroll (ScrollContainer)
        │   └── ItemList (VBoxContainer) ← buttons added at runtime, one per owned item in slot
        ├── UnequipButton (Button)
        └── CloseButton (Button)
```

### `main.tscn` (run scene)
```
Main (Node)
├── Player (CharacterBody3D)   ← stats seeded from CharacterManager.SelectedCharacter
│   ├── CollisionShape (CollisionShape3D)
│   └── Weapon (Node)
├── Background (Node3D)        ← checker floor plane via ShaderMaterial
├── WorldEnvironment
├── Camera3D                   ← CameraFollow script; orthographic, ~51° isometric tilt
├── Hud (CanvasLayer)          ← health bar, XP bar, level, coin counter, run timer
├── EnemySpawner (Node)
├── RunSession (Node)          ← tracks elapsed time; emits RunEnded(won, level, elapsed)
├── RunEndOverlay (CanvasLayer)← shown on RunEnded; returns to character_screen.tscn
└── PauseMenu (CanvasLayer)   ← shown on ESC; pauses tree; Resume or End Run (discards progress)
```

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
| CharacterScreen   | Per-character hub: stats, gear slots, Start Run              | `res://src/ui/`           | ✅ done |
| ItemPickerPanel   | Modal picker for equipping/unequipping gear by slot          | `res://src/ui/`           | ✅ done |
| ItemRegistry      | Static catalogue of all `ItemData` records (9 starter items) | `res://src/items/`        | ✅ done |
| RunEndOverlay     | Show win/die results, flush run to character, return to character screen | `res://src/ui/` | ✅ done |
| PauseMenu         | ESC during run — pauses tree, Resume or End Run (skips RecordRunCompletion → discards progress) | `res://src/ui/` | 🔲 todo |
| CoinPickup        | Coin drop (25% on enemy death) — reports to RunSession       | `res://src/meta/`         | ✅ done |
| MetaProgression   | Per-character coin bank + permanent upgrades (HP/Speed/DMG)  | `res://src/meta/`, `src/ui/` | ✅ done |
| HealthPickup      | Health drop (10% on enemy death) — heals player on contact   | `res://src/health/`       | ✅ done |

---

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, CurrentLevel, CurrentXp, CoinBank, CraftingCurrency1, BonusMaxHealth, BonusSpeed, BonusDamage, OwnedItemIds, EquippedItems |
| `CharacterType`     | C# enum     | Warrior, Rogue, Mage                                           |
| `ItemData`          | C# record   | Id, Name, Slot (enum), BonusHp, BonusSpeed, BonusDamage       |
| `ItemSlot`          | C# enum     | Weapon, Armor, Accessory                                       |
| `ItemRegistry`      | Static class| `All` dict, `Get(id)`, `ForSlot(slot)`, `RandomDrop()`        |
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
      "craftingCurrency1": 30,
      "bonusMaxHealth": 10,
      "bonusSpeed": 0,
      "bonusDamage": 5,
      "ownedItemIds": ["iron_sword", "leather_vest"],
      "equippedItems": { "Weapon": "iron_sword", "Armor": "leather_vest" }
    }
  ]
}
```
`ownedItemIds` and `equippedItems` default to empty if absent — backwards-compatible with saves written before the items system. New characters are seeded with 3 archetype-specific items by `CharacterManager.SeedStarterGear()`. `craftingCurrency1` defaults to 0 if absent.

### Run Session (in-memory only)
Lives on the `RunSession` node. Discarded when the scene unloads. On run end, `CharacterManager.RecordRunCompletion(finalLevel, finalXp, coinsEarned)` writes the persistent state.
- Elapsed time
- Coins earned this run

Level and XP are NOT run-scoped — they live on `CharacterData` and are written back at run end.

### Future: Profile Envelope
If multi-user slots or cloud saves are ever needed, evaluate wrapping save data under a profile envelope. `CharacterManager` is the only entry point — the refactor scope is bounded (1 constant, a handful of callers).

---

## Weapon

Single weapon per character. Damage is set at run start from `CharacterData.BaseStats()` plus level bonuses (`+1 per level above 1`). `WeaponController` exposes `SetDamage(float)` and `AddDamage(float)`.

[TBD] Weapon upgrade path (stages, piercing, AoE) — deferred until UpgradePicker or equivalent is reintroduced.

---

## Enemy Spawner — Wave Scaling

Time-driven, no fixed waves. `EnemySpawner` recalculates each spawn:
- **Spawn rate** — starts immediately at t=0; interval = `InitialInterval / (1 + minutes * 0.5)`, clamped to `MinInterval = 0.3s`
- **Spawn position** — fixed-radius ring (350px) around the player; viewport-size-independent
- **Enemy types** — unlocked by elapsed minutes, chosen randomly from the available pool:

| Type     | Sprite row | Unlocks | Speed | HP | Damage |
|----------|-----------|---------|-------|----|--------|
| Standard | 6 (grey)  | 0:00    | 260   | 1  | 10     |
| Runner   | 4 (purple)| 1:00    | 400   | 1  | 8      |
| Tank     | 2 (orange)| 2:00    | 160   | 1  | 18     |

All types receive a time-scaling bonus on top: `Speed += 10 * minutes`, `MaxHealth += 5 * (int)minutes`.

---

## Map Attributes

Each run is played on a map. Maps carry an attribute set that modifies run behaviour. The attribute set is small now and will grow.

| Attribute  | Type  | Effect                                                                     |
|------------|-------|----------------------------------------------------------------------------|
| `MapLevel` | `int` | On enemy death, `PlayerController.CollectXp(MapLevel)` is called directly — no pickup required. Stacks on top of any XP gem drop. |

`MapLevel` is passed into the run scene at startup (e.g. via `RunSession` or a `MapData` resource — exact wiring TBD when maps are selectable).

---

## Drop System

On enemy death, two XP sources fire independently:

1. **Kill XP** — `1 × MapLevel` XP granted instantly via `PlayerController.CollectXp()`
2. **XP gem drop** — physical `XpGem` scene spawned; player must walk over it (value = 5 XP)

Other drops hardcoded in `EnemyController.Die()`:

| Drop              | Chance | Notes                                                                  |
|-------------------|--------|------------------------------------------------------------------------|
| XP gem            | 100%   | Always dropped; value = 5 XP                                           |
| Coin              | 25%    | `CoinPickup` auto-collected; reports to `RunSession.AddCoin()`         |
| Health pack       | 10%    | `HealthPickup` heals player for 15 HP on contact                       |
| Crafting currency | 20%    | Instant; calls `RunSession.AddCraftingCurrency1(1)` — no pickup scene  |

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
| `EnemyDied(position)`   | Enemy          | (reserved — not yet wired)       |
| `CoinChanged(int)`      | RunSession     | Hud (coin counter)               |
| `RunTimerExpired`       | RunSession     | EnemySpawner (spawn boss)        |
| `RunEnded(result)`      | RunSession     | SaveManager (flush coins/rewards)|

---

## Third-party / Tools

| Tool           | Purpose                               |
|----------------|---------------------------------------|
| Godot MCP Pro  | AI-assisted editor control via Claude |

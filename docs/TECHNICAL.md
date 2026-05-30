# Technical Design Document — godot1

> Living document — architecture will evolve as systems are built and playtested.

## Architecture Overview

Godot 4.6, C#, Forward Plus renderer. Game world is 3D (CharacterBody3D, XZ movement plane, Y-up); characters and enemies are rendered as KayKit `.glb` models loaded at runtime as `PackedScene` child nodes. Camera is perspective, fixed ~60° from horizontal (Diablo 4-style), no player rotation, parented to player. UI is 2D (`Control` / `CanvasLayer`) as standard in Godot — unaffected by the 3D world. Scene composition over inheritance — each system is a self-contained scene or node that communicates via signals. Two save layers: a persistent save file (meta) and an in-memory run session (discarded on run end).

---

## Rendering & Camera

| Decision         | Choice                        | Rationale                                                                 |
|------------------|-------------------------------|---------------------------------------------------------------------------|
| World dimensions | 3D, XZ movement plane, Y-up   | Standard for top-down 3D; gravity, navmesh, and lighting all assume Y-up  |
| Camera type      | `Camera3D`, perspective       | Subtle depth like Diablo 4; fixed angle, no player rotation               |
| Camera angle     | Fixed ~60° from horizontal    | Closer to overhead than classic 45° isometric; Diablo 4 reference         |
| Character render | KayKit `.glb` loaded as `PackedScene`, instanced as child `Node3D` | Player = Knight.glb (scale 12), enemies = Skeleton_Minion.glb (scale 10). Model child rotates independently via `_model.LookAt()` — CharacterBody3D stays unrotated so camera doesn't spin. |
| Lighting         | Single `DirectionalLight3D` parented to `Camera3D` | Global main light source, moves with camera; one light for now |
| Projectiles      | Physical traveling objects    | Visible projectile travel is core to ARPG feel (not raycasts)              |
| Target aspect ratio | 16:9, PC primary           | All UI scenes must use Godot anchor presets (no absolute offsets) — makes ratio changes free later. Mobile not in scope. |
| Base viewport resolution | 1280×720               | Set in project.godot; Godot stretch mode scales to player's screen. |
| Stretch mode        | `canvas_items`                | Scales UI and world together; crisp at integer multiples of 720p.  |
| UI theme            | Spacey (`res://addons/Themey/themes/spacey/spacey.tres`) | Free Themey pack; set as project-wide theme — all Control nodes inherit automatically. No per-scene theme overrides. |
| Floor               | Procedural checkerboard (`CheckerBackground.cs`) | Two-tone grey, generated at runtime — no external assets. |
| Pickup visuals      | Colored `BoxMesh` (10×10×10) | XP gem = green, coin = yellow, health = red. Opaque to all systems. |

---

## Scene Flow

```
main_menu.tscn  →  character_select.tscn  →  character_screen.tscn  →  main.tscn
                          ↕
                 character_create.tscn
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
Roster-only screen. Lists existing characters; "+ New Character" navigates to `character_create.tscn`.
```
CharacterSelect (Control)
└── VBox (VBoxContainer)
    ├── CharactersLabel (Label)
    ├── Scroll (ScrollContainer, expand)
    │   └── CharacterList (VBoxContainer) ← character cards added at runtime
    ├── NewCharacterButton (Button) → character_create.tscn
    └── BackButton (Button) → main_menu.tscn
```
On character selected: `CharacterManager.SelectCharacter(id)` → `character_screen.tscn`.

### `src/ui/character_create.tscn`
Dedicated character creation screen. Centred form; Create disabled until a name is entered.
```
CharacterCreate (Control)
└── VBox (VBoxContainer, centred)
    ├── TitleLabel (Label)
    ├── NameInput (LineEdit) ← enables ConfirmBtn when non-empty
    ├── WarriorBtn, RogueBtn, MageBtn (type selection)
    ├── ConfirmBtn (Button) ← creates character → character_select.tscn
    └── CancelBtn (Button) → character_select.tscn
```

### `src/ui/character_screen.tscn`
Full management hub for the selected character. Always has a character in context.
```
CharacterScreen (Control)
└── VBox (VBoxContainer)
    ├── BackButton (Button) ← "← Change Character" → character_select.tscn
    └── HSplit (HSplitContainer)
        ├── LeftPanel (PanelContainer, min width 280)
        │   └── LeftVBox (VBoxContainer)
        │       ├── InventoryTitle (Label)
        │       ├── InventoryInfo (Label)  ← "N / 50  Coins: X  Crafting: Y"
        │       └── InventoryScroll (ScrollContainer, expand vertical)
        │           └── InventoryGrid (GridContainer, 5 cols) ← 50 Button slots, runtime-populated
        └── RightPanel (VBoxContainer)
            └── TabContainer
                ├── Equipment tab (VBoxContainer)
                │   └── CharacterView (VBoxContainer, always visible)
                │       └── HSplit (HBoxContainer)
                │           ├── InfoVBox (VBoxContainer, expand) ← name/type/level/stats labels
                │           │   ├── NameLabel, TypeLabel, LevelLabel, StatsLabel
                │           │   ├── Spacer (expand)
                │           │   └── Buttons ← StartRunButton
                │           └── GearPanel (VBoxContainer) ← right column, shrink width
                │               ├── GearLabel ("— Equipment —")
                │               ├── WeaponSlot (VBoxContainer)
                │               │   ├── WeaponLabel ("Weapon")
                │               │   └── WeaponSlotButton (60×60, size_flags_h=shrink) ← popup (Unequip/Delete) if equipped; ItemPickerPanel if empty
                │               ├── ArmorSlot / ArmorLabel / ArmorSlotButton (same pattern)
                │               └── AccessorySlot / AccessoryLabel / AccessorySlotButton (same pattern)
                └── Crafting tab
                    └── VBox ← CraftWeaponButton, CraftArmorButton, CraftAccessoryButton
```
**Inventory grid:** 50 slots (5 cols, scrollable), all always visible. Empty slots are dimmed. Clicking a filled slot opens a `PopupMenu` (Equip / Delete). Equip auto-routes to the item's slot on the selected character, swapping out any currently equipped item. Capacity defined by `ProfileData.MaxInventory = 50` — counts only unequipped items; equipped items live separately in `CharacterData.EquippedItems`. If `SelectedCharacter` is null on `_Ready`, redirects to `character_select.tscn`.

### `src/ui/item_picker_panel.tscn`
Modal overlay opened from gear slot buttons **when the slot is empty**. Occupied slots use a PopupMenu (Unequip / Delete) instead — ItemPickerPanel is never opened for an occupied slot.
```
ItemPickerPanel (Control, full-screen)
├── Dim (ColorRect, semi-transparent black)
└── Panel (PanelContainer, centered)
    └── VBox (VBoxContainer)
        ├── TitleLabel (Label)
        ├── Scroll (ScrollContainer)
        │   └── ItemList (VBoxContainer) ← buttons added at runtime, one per owned item in slot
        └── CloseButton (Button)
```

### `main.tscn` (run scene)
```
Main (Node)
├── Player (CharacterBody3D)   ← stats seeded from CharacterManager.SelectedCharacter
│   ├── CollisionShape3D
│   ├── Camera3D               ← perspective, ~60°, parented to player (follows automatically)
│   │   └── DirectionalLight3D ← global main light, moves with camera
│   └── Weapon (Node)
├── Background (Node3D)        ← procedural floor plane
├── WorldEnvironment
├── Hud (CanvasLayer)          ← health bar, XP bar, level, coin counter, run timer
├── EnemySpawner (Node)
├── RunSession (Node)          ← tracks elapsed time; emits RunEnded(won, level, elapsed)
├── RunEndOverlay (CanvasLayer)← shown on RunEnded; returns to character_screen.tscn
├── PauseMenu (CanvasLayer)
└── DevOverlay (CanvasLayer)   ← debug only; hidden when OS.IsDebugBuild() is false
    ├── ToggleButton (Button)  ← anchored top-center; always visible in debug builds
    └── DevPanel (PanelContainer) ← hidden by default; toggled by ToggleButton
        └── VBox (VBoxContainer)
            ├── SpeedLabel (Label)   ← displays current speed value
            └── SpeedSlider (HSlider) ← live-edits PlayerController.Speed
```
**DevOverlay behaviour:** `_Ready()` checks `OS.IsDebugBuild()` — if false, hides (or frees) the entire overlay so no dev UI is visible in release exports. `ToggleButton` flips `DevPanel.Visible` on each press. The slider's `ValueChanged` signal writes directly to `PlayerController.Speed`.

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
| CharacterSelect   | Roster screen: list and delete characters; no inventory      | `res://src/ui/`           | ✅ done |
| CharacterCreate   | Dedicated create screen: name input + archetype choice       | `res://src/ui/`           | ✅ done |
| CharacterScreen   | Per-character hub: inventory, gear slots, tabs, Start Run    | `res://src/ui/`           | ✅ done |
| ItemPickerPanel   | Modal picker for equipping/unequipping gear by slot          | `res://src/ui/`           | ✅ done |
| ItemRegistry      | Static catalogue of all `ItemData` records (9 starter items) | `res://src/items/`        | ✅ done |
| RunEndOverlay     | Show win/die results, flush run to character, return to character screen | `res://src/ui/` | ✅ done |
| CoinPickup        | Coin drop (25% on enemy death) — reports to RunSession       | `res://src/meta/`         | ✅ done |
| MetaProgression   | Per-character coin bank + permanent upgrades (HP/Speed/DMG)  | `res://src/meta/`, `src/ui/` | ✅ done |
| HealthPickup      | Health drop (10% on enemy death) — heals player on contact   | `res://src/health/`       | ✅ done |

---

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `ProfileData`       | Plain C#    | CoinBank, CraftingCurrency1, OwnedItemIds (List\<string\>), MaxInventory (const = 50) — account-shared across all characters |
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, CurrentLevel, CurrentXp, BonusMaxHealth, BonusSpeed, BonusDamage, EquippedItems |
| `CharacterType`     | C# enum     | Warrior, Rogue, Mage                                           |
| `ItemData`          | C# record   | Id, Name, Slot (enum), BonusHp, BonusSpeed, BonusDamage, IconPath (string `res://` path to item texture) |
| `ItemSlot`          | C# enum     | Weapon, Armor, Accessory                                       |
| `ItemRegistry`      | Static class| `All` dict, `Get(id)`, `ForSlot(slot)`, `RandomDrop()`        |
| `WeaponData`        | Godot Resource (planned) | Name, base damage, cooldown, upgrade path — not yet implemented; `WeaponController` manages damage as a plain `float` |
| `WeaponUpgradeData` | Godot Resource (planned) | Damage delta, cooldown delta, new behaviour flags — not yet implemented |
| `UpgradeOptionData` | Godot Resource (planned) | Display name, description, effect type + value — not yet implemented (UpgradePicker dormant) |
| `EnemyData`         | C# record      | EnemyType (string), BaseSpeed, BaseHealth, ContactDamage, DamageInterval |

---

## Save Layers

### Character Save (`user://save.json`)
Managed by `CharacterManager` autoload. Written on every create/delete/upgrade.
```json
{
  "profile": {
    "coinBank": 150,
    "craftingCurrency1": 30,
    "ownedItemIds": ["swift_ring"]
  },
  "characters": [
    {
      "id": "<guid>",
      "name": "Ironclad",
      "type": "Warrior",
      "runsCompleted": 3,
      "currentLevel": 7,
      "currentXp": 12,
      "equippedItems": { "Weapon": "iron_sword", "Armor": "leather_vest" }
    }
  ]
}
```
`ownedItemIds` holds only **unequipped** inventory items; equipped items live exclusively in `equippedItems`. `EquipItem` moves an item out of `ownedItemIds` and swaps the old equipped item back in. `UnequipItem` returns `false` (blocked) if inventory is at capacity. Old saves are migrated on load — any item ID present in both lists is removed from `ownedItemIds`. Starter gear is seeded directly into `equippedItems` (not inventory) by `SeedStarterGear()`. Fields default to empty if absent.

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

| Type     | Unlocks | Speed | HP | Damage |
|----------|---------|-------|----|--------|
| Standard | 0:00    | 75    | 1  | 10     |
| Runner   | 1:00    | 110   | 1  | 8      |
| Tank     | 2:00    | 45    | 1  | 18     |

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
| `Died(position)`        | Enemy          | (reserved — not yet wired)       |
| `CoinChanged(int)`      | RunSession     | Hud (coin counter)               |
| `RunTimerExpired`       | RunSession     | EnemySpawner (spawn boss)        |
| `RunEnded(result)`      | RunSession     | CharacterManager (flush coins/XP/level via `RecordRunCompletion`) |

---

## Third-party / Tools

| Tool           | Purpose                               |
|----------------|---------------------------------------|
| Godot MCP Pro  | AI-assisted editor control via Claude |
| Themey         | Free open-source Godot 4 UI theme pack — Spacey theme in use (`res://addons/Themey/`) |
| Ravenmore Fantasy Icon Pack | Item slot icons (`res://assets/icons/items/`) — CC-BY 3.0, credit: ravenmore.itch.io |
| KayKit (Kay Lousberg)       | Character models for testing — Knight (player), Skeleton_Minion (enemies) (`res://assets/models/`) — CC0, no attribution required |

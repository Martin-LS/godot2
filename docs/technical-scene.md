# Technical Design Document — Scene & Architecture

> Part of the technical docs. See also `technical-systems.md` for data types, save format, crafting, combat systems, and more.
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
| Pickup visuals      | Colored `BoxMesh` (10×10×10) | XP Shard = green, coin = yellow, health = red. Opaque to all systems. |

---

## Scene Flow

```
main_menu.tscn  →  account_screen.tscn  →  character_screen.tscn  →  main.tscn
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

### `src/ui/account_screen.tscn`
Account-level hub. Always the first screen after Main Menu. Contains the character roster; designed to grow with additional account-level info. Character create is inline (no separate scene navigation).
```
AccountScreen (Control)
└── VBox (VBoxContainer)
    ├── HSplit (HSplitContainer)
    │   └── RightPanel
    │       └── TabContainer
    │           └── Characters tab
    │               └── RosterView ← character list + inline create panel
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
    ├── ConfirmBtn (Button) ← creates character → account_screen.tscn
    └── CancelBtn (Button) → account_screen.tscn
```

### `src/ui/character_screen.tscn`
Full management hub for the selected character. Always has a character in context.
```
CharacterScreen (Control)
└── VBox (VBoxContainer)
    ├── BackButton (Button) ← "← Change Character" → account_screen.tscn
    └── HSplit (HSplitContainer)
        ├── LeftPanel (PanelContainer, min width 280)
        │   └── LeftVBox (VBoxContainer)
        │       ├── InventoryTitle (Label)
        │       ├── InventoryInfo (Label)  ← "Gear: N/50  Skills: M/50  Augments: P/50  Coins: X  Common: Y"
        │       └── InventoryTabs (TabContainer, expand vertical)
        │           ├── Equipment tab
        │           │   └── InventoryScroll (ScrollContainer)
        │           │       └── InventoryGrid (GridContainer, 5 cols) ← 50 Button slots, runtime-populated from OwnedGearInstances
        │           ├── Skills tab
        │           │   └── SkillsScroll (ScrollContainer)
        │           │       └── SkillsGrid (GridContainer, 5 cols) ← 50 Button slots, runtime-populated from OwnedSkillInstances
        │           └── Augments tab
        │               └── AugmentsScroll (ScrollContainer)
        │                   └── AugmentsGrid (GridContainer, 5 cols) ← 50 Button slots, runtime-populated from OwnedSkillAugmentInstances + OwnedEquipmentAugmentInstances (interleaved, Skill Augments first)
        └── RightPanel (VBoxContainer)
            └── TabContainer
                ├── Loadout tab (VBoxContainer)
                │   └── CharacterView (VBoxContainer)
                │       ├── HSplit (HBoxContainer)
                │       │   ├── InfoVBox (VBoxContainer, expand) ← name/type/level/stats labels
                │       │   │   └── NameLabel, TypeLabel, LevelLabel, StatsLabel
                │       │   └── GearPanel (VBoxContainer) ← right column, shrink width
                │       │       ├── GearLabel ("— Equipment —")
                │       │       ├── WeaponSlot (VBoxContainer)
                │       │       │   ├── WeaponLabel ("Weapon")
                │       │       │   └── WeaponSlotButton (60×60, size_flags_h=shrink) ← popup (Unequip/Delete) if equipped; ItemPickerPanel if empty
                │       │       ├── ArmorSlot / ArmorLabel / ArmorSlotButton (same pattern)
                │       │       └── AccessorySlot / AccessoryLabel / AccessorySlotButton (same pattern)
                │       ├── SkillBar (HBoxContainer, size_flags_h=SHRINK_CENTER) ← centered row, below HSplit
                │       │   ├── SkillSlot1 (VBoxContainer) → SkillLabel1 ("Skill 1") + SkillSlotButton1 (60×60) ← popup (Unequip/Delete) if equipped; SkillPickerPanel if empty
                │       │   ├── SkillSlot2 / SkillLabel2 / SkillSlotButton2 (same pattern)
                │       │   └── SkillSlot3 / SkillLabel3 / SkillSlotButton3 (same pattern)
                │       └── Buttons (HBoxContainer)
                │           └── StartRunButton (Button, expand fill)
                ├── Equipment Crafting tab (CraftingTabs TabContainer)
                │   ├── Create sub-tab
                │   │   └── VBox ← materials label + one Button per RecipeRegistry.ForType(Gear) entry; one Button per RecipeRegistry.ForType(EquipmentAugment) entry
                │   └── Modify sub-tab (ModifyVBox)
                │       ├── GearModifySlotBtn (Button 60×60) ← click → inline PopupMenu listing all gear instances (owned + equipped)
                │       ├── GearUpgradeBtn (Button) ← costs 1 Common; disabled if no item loaded / max tier / insufficient materials
                │       └── EquipmentAugmentSlotsRow (HBoxContainer) ← one slot button per augment slot (count = tier); each slot: empty → EquipmentAugmentPickerPanel filtered to compatible augments from OwnedEquipmentAugmentInstances; occupied → PopupMenu (Remove)
                ├── Skill Crafting tab (SkillCraftingTabs TabContainer)
                │   ├── Create sub-tab
                │   │   └── VBox ← materials label + one Button per RecipeRegistry.ForType(Skill) entry; one Button per RecipeRegistry.ForType(SkillAugment) entry
                │   └── Modify sub-tab (ModifyVBox)
                │       ├── SkillModifySlotBtn (Button 60×60) ← click → inline PopupMenu listing OwnedSkillInstances
                │       ├── SkillUpgradeBtn (Button) ← costs 1 Common; disabled if no skill loaded / max tier / insufficient materials
                │       └── SkillAugmentSlotsRow (HBoxContainer) ← one slot button per augment slot (count = tier); each slot: empty → SkillAugmentPickerPanel filtered to compatible augments from OwnedSkillAugmentInstances; occupied → PopupMenu (Remove)
                └── Sigils tab    ← empty; reserved for future sigil system
```
**Inventory grids:** Each tab has 50 slots (5 cols, scrollable), all always visible. Empty slots are dimmed. Clicking a filled slot opens a `PopupMenu` (Equip / Delete for gear/skills; Delete for Skill Augments and Equipment Augments — augments are not equipped directly, they are socketed into skill or gear items). Capacity: `ProfileData.MaxInventory = 50` per tab — counts only unequipped/unsocketed items; equipped gear lives in `CharacterData.EquippedGear`, slotted skill GUIDs in `SlottedSkillInstanceIds`, socketed Skill Augment GUIDs in `SkillItemInstance.SocketedSkillAugmentIds`, socketed Equipment Augment GUIDs in `GearItemInstance.SocketedEquipmentAugmentIds`. If `SelectedCharacter` is null on `_Ready`, redirects to `account_screen.tscn`.

**Skill slots:** Skill slot buttons open a `SkillPickerPanel` when empty. Occupied skill slots use a `PopupMenu` (Unequip / Delete).

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

### `src/ui/skill_picker_panel.tscn`
Modal overlay opened from skill slot buttons **when the slot is empty**. Occupied skill slots use a PopupMenu (Unequip / Delete) instead.
```
SkillPickerPanel (Control, full-screen)
├── Dim (ColorRect, semi-transparent black)
└── Panel (PanelContainer, centered)
    └── VBox (VBoxContainer)
        ├── TitleLabel (Label) ← "Choose Skill"
        ├── Scroll (ScrollContainer)
        │   └── ItemList (VBoxContainer) ← buttons added at runtime, one per OwnedSkillIds entry
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
├── Hud (CanvasLayer)          ← health bar, XP bar, level, coin counter, run timer, skill bar
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
| Enemy             | AI (chase), taking damage, death + XP Shard spawning           | `res://src/enemies/`      | ✅ done |
| XpShard           | XP Shard pickup — auto-collected on contact                  | `res://src/xp/`           | ✅ done |
| EoT               | Effect over Time — apply, tick, expire on enemies            | `res://src/eot/`          | ✅ done |
| Hud               | Health bar, XP bar, level, coin counter, run timer           | `res://src/hud/`          | ✅ done |
| RunSession        | Run timer, win/lose detection, emits RunEnded signal         | `res://src/run/`          | ✅ done |
| UpgradePicker     | (removed from scene — code kept dormant)                     | `res://src/ui/`           | ❌ removed |
| AccountScreen     | Account hub: character roster, crafting tab; navigates to CharacterScreen on select | `res://src/ui/` | ✅ done |
| CharacterCreate   | Dedicated create screen: name input + archetype choice       | `res://src/ui/`           | ✅ done |
| CharacterScreen   | Per-character hub: inventory, gear slots, tabs, Start Run    | `res://src/ui/`           | ✅ done |
| ItemPickerPanel          | Modal picker for equipping/unequipping gear by slot                    | `res://src/ui/`       | ✅ done |
| SkillPickerPanel         | Modal picker for equipping skills into skill slots                     | `res://src/ui/`       | ✅ done |
| ItemRegistry      | Static catalogue of all `ItemData` records                   | `res://src/items/`        | ✅ done |
| SkillRegistry     | Static catalogue of all `SkillData` records                  | `res://src/skills/`       | ✅ done |
| RecipeRegistry    | Static catalogue of all `RecipeData` records                 | `res://src/crafting/`     | ✅ done |
| RunEndOverlay     | Show win/die results, flush run to character, return to character screen | `res://src/ui/` | ✅ done |
| CoinPickup        | Coin drop (25% on enemy death) — reports to RunSession       | `res://src/meta/`         | ✅ done |
| MetaProgression   | Level bonuses (automatic +HP/+DMG per level); coin bank accumulates — spend mechanic TBD | `res://src/meta/`, `src/ui/` | ✅ done |
| HealthPickup      | Health drop (10% on enemy death) — heals player on contact   | `res://src/health/`       | ✅ done |

---

## Class Conventions (C#)

- **Namespaces:** `Godot1.<System>` (e.g. `Godot1.Player`, `Godot1.Combat`)
- **Node classes:** PascalCase — `PlayerController`, `EnemyBase`, `WeaponController`
- **Resource classes:** suffix `Data` — `EnemyData`, `ItemData`, `CharacterData`
- **Private fields:** `_camelCase`; public properties: `PascalCase`
- **Signals:** `[Signal]` delegate, past-tense — `HealthChanged`, `EnemyDied`, `LeveledUp`
- **Folder layout:** `src/<system>/` mirrors namespace

---

## Signals & Events

Systems communicate via signals only — no direct cross-system method calls.

| Signal                  | Emitter        | Receivers                        |
|-------------------------|----------------|----------------------------------|
| `HealthChanged(float)`      | Player         | HUD (formats to int for display), GameManager |
| `PlayerDied`                | Player         | RunSession (end run)             |
| `LeveledUp(int)`            | Player         | Hud (level display)              |
| `SkillFired(int, float)`    | WeaponController | Hud skill bar (slotIndex, cooldown — resets cooldown overlay) |
| `Died(position)`            | Enemy          | (reserved — not yet wired)       |
| `CoinChanged(int)`          | RunSession     | Hud (coin counter)               |
| `RunTimerExpired`           | RunSession     | EnemySpawner (spawn boss)        |
| `RunEnded(result)`          | RunSession     | CharacterManager (flush coins/XP/level via `RecordRunCompletion`) |

---

## Future Systems

### Focus (Skill Resource)

`Focus` is the planned universal skill resource — all archetypes spend it to fire skills, but each interacts with it differently (Warrior: low-cost non-magic skills; Rogue: agile skills refill on use; Mage: Focus also acts as a damage buffer before HP). See GDD § Future Design Notes.

**Architecture placeholder:** `StatId.Focus` (max pool) and `StatId.FocusRegen` (regen rate) will be added when Focus is implemented. `PlayerController` will track `CurrentFocus` analogously to `CurrentHealth`. `WeaponController` will deduct Focus per shot based on skill cost; if insufficient Focus, the slot does not fire. Archetype multiplier table will include a Focus row.

*Not scheduled for v1.*

---

## Third-party / Tools

| Tool           | Purpose                               |
|----------------|---------------------------------------|
| Godot MCP Pro  | AI-assisted editor control via Claude |
| Themey         | Free open-source Godot 4 UI theme pack — Spacey theme in use (`res://addons/Themey/`) |
| Ravenmore Fantasy Icon Pack | Item slot icons (`res://assets/icons/items/`) — CC-BY 3.0, credit: ravenmore.itch.io |
| KayKit (Kay Lousberg)       | Character models for testing — Knight (player), Skeleton_Minion (enemies) (`res://assets/models/`) — CC0, no attribution required |

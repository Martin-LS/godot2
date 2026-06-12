# Technical Design Document — Data, Systems & Crafting

> Part of the technical docs. See also `technical-scene.md` for scene layout, architecture overview, signals, and C# conventions.
> Living document — architecture will evolve as systems are built and playtested.

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `GearItemInstance`  | Plain C#    | Id (string, GUID), DefinitionId (string → `ItemRegistry`), Tier (int, 1–3), SocketedEquipmentAugmentIds (List\<string\> — instance GUIDs, one entry per augment slot; "" = empty; max length = augment slots for tier: 1/2/3). Authoritative tier for runtime stat scaling. |
| `EotInstance`       | Plain C#    | DefinitionId (string), TimeRemaining (float), TickTimer (float — damage EoTs only), **CritMultiplier (float, default 1.0f)** — set to the firing hit's crit multiplier when the applying hit was a critical hit; damage ticks use `DamagePerTick × CritMultiplier`. Non-damage EoTs ignore this field. |
| `SkillItemInstance` | Plain C#    | Id (string, GUID), DefinitionId (string → `SkillRegistry`), Tier (int, 1–3), SocketedSkillAugmentIds (List\<string\> — instance GUIDs, one entry per augment slot; "" = empty slot; max length = augment slots for tier: 1/2/3). |
| `ProfileData`       | Plain C#    | CoinBank, Materials (Dictionary\<string, int\>), OwnedGearInstances (List\<GearItemInstance\>), OwnedSkillInstances (List\<SkillItemInstance\>), OwnedSkillAugmentInstances (List\<SkillAugmentInstance\>), OwnedEquipmentAugmentInstances (List\<EquipmentAugmentInstance\>), MaxInventory (const = 50) — applies separately to each list. Account-shared. Migration: old `ownedItemIds`/`ownedSkillIds` string lists are wrapped into instances (new GUID, Tier = 1) on load. Old `augment`/`chainInstanceId` fields on skill instances are dropped on load. |
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, CurrentLevel, CurrentXp, EquippedGear (Dictionary\<string, GearItemInstance\> — slot → full instance), SlottedSkillInstanceIds (List\<string\> — instance GUIDs; skill instances stay in `OwnedSkillInstances`). Archetype base stats computed inline in `BuildStatBlock()` — applies archetype multiplier formula before returning. |
| `CharacterType`     | C# enum     | Warrior, Rogue, Mage                                           |
| `StatId`            | C# enum     | MaxHp, Speed, PhysicalDamage, MagicDamage, PhysicalResistance, MagicResistance, MaxFocus, FocusRegen |
| `StatModifier`      | Plain C#    | StatId, ModifierType (FlatAdd), Value (float), ModifierSource (Level, Item) |
| `StatBlock`         | Plain C#    | Internal flat modifier list per `StatId`. `Get(StatId)` returns the sum of all flat modifiers for that stat — archetype multiplier is applied in `BuildStatBlock()` before the block is returned, so callers always get effective values. |
| `ItemData`          | C# record   | Id, Name, Slot (enum), IconPath, Tags (string[] — equipment tags for augment compatibility; e.g. `["Melee"]` for Sword, `["Heavy"]` for heavy armour, `[]` for Accessory) — plus slot-specific fields: `WeaponRange (float, in tiles)`, `PreferredDelivery (string — "Melee" or "Ranged")` for Weapon; `ArmorCategory`, `BonusHp`, `BonusSpeed`, `DamageReduction (float)`, `RangeModifier (float, in tiles)` for Armor; `PhysicalResistance (float)` for Accessory. Unused fields default to zero. `Tier` removed — tier lives on `GearItemInstance`, not the definition. **Range fields are always in tiles** — multiply by `GameScale.TileSize` to get world units. |
| `ItemSlot`          | C# enum     | Weapon, Hat, Body, Ring                                        |
| `SkillData`         | C# record   | Id, Name, Type (SkillType enum), Tags (string[]) — e.g. `["Melee","Attack"]`, `["Ranged","Attack"]`, `["Ranged","Magic","Spell"]`. Cooldown (float, seconds — for Active: time between casts; for Channeled/Aura: damage tick interval; 0 for Passive), FocusCost (float — Active: flat spend per cast; Channeled: drain per second; Aura: fraction of MaxFocus to reserve (0.0–1.0); Passive: 0/ignored), Range (float), IconPath (string, default ""), Description (string, default ""), IsPrototype (bool, default false — v1: all skills true; v2: derived skills false), TargetingShape (SkillTargetingShape enum, default Self), WindUp (float seconds, default 0.0f — 0 = instant), DamagePattern (SkillDamagePattern enum, default Burst), StackLimit (int — −1 = not a zone skill; 1+ = max simultaneous active instances), ZoneTracksEntity (bool, default false), Duration (float seconds, default 0f — 0 = permanent; zone and summon skills must set this), TriggerRadius (float tiles, default 0f — 0 = not a trap; >0 = trap proximity detection radius), ArmTime (float seconds, default 0f — delay after placement before trap can trigger; ignored when TriggerRadius = 0), TriggerCount (int, default 0 — 0 = not a trap; 1 = single-trigger then despawn; >1 = multi-trigger). No Tier — tier lives on `SkillItemInstance`. No `BasePrototypeId` — prototype relationship is design-time documentation only; C# record required fields enforce new-field completeness at compile time. |
| `SkillType`         | C# enum     | Active, Channeled, Aura, Passive                               |
| `SkillTargetingShape` | C# enum   | Self (effect fires from player position — no targeting input needed), Position (effect lands at locked target's world position on controller/keyboard; at cursor on mouse — no enemy required), Entity (must land on a specific enemy — blocked if no valid target; on mouse snaps to nearest enemy to cursor; on controller/keyboard uses locked target) |
| `SkillDamagePattern` | C# enum    | Burst (single hit fires on cast), Tick (damage repeats over duration at tick rate), None (debuff or utility only — no damage output) |
| `ArmorCategory`     | C# enum     | None, Heavy, Medium, Light                                     |
| `DamageType`        | C# enum     | Physical, Magic                                                |
| `ItemTier`          | C# static class (const ints) | Common = 1, Uncommon = 2, Rare = 3, Max = 3. `Label(int)` → display name. `BorderColor(int)` → Godot `Color`. Used for the rarity border colour on item slot buttons. |
| `BalanceConfig`     | Static class (nested) | Sections: `Weapons` (SwordRange/BowRange/WandRange), `Armour` (Heavy/Medium/Light — BonusHp, BonusSpeed, DamageReduction, RangeModifier per tier), `Accessories` (RingPhysicalResistance), `Skills` (cooldown + range per skill), `Eots` (ApplyChance, Duration, per-effect fields), `Enemies.Skeleton` + scaling consts + MeleeContactRange, `Drops` (coin/health/crafting chances), `Pickups` (XpShardValue, HealthHealAmount), `Archetypes` (DefaultMultiplier + MaxHp/Speed/PhysicalDamage/MagicDamage per archetype), `LevelUp` (HpBonusPerLevel, DamageBonusPerLevel), `Focus` (per-archetype MaxFocus/RegenPerSec base values, ShieldFraction, ShieldRegenPerSec; per-skill FocusCost constants). All values are `const` — compile-time resolvable. |
| `ItemRegistry`      | Static class| `All` dict, `Get(id)`, `ForSlot(slot)` — 7 starter gear definitions. Definitions carry no tier — all instances start at Tier = 1 when crafted. |
| `SkillRegistry`     | Static class| `All` dict, `Get(id)` — v1: 4 renamed prototype entries: `entity_burst` (was `strike` — Active, Tags: `["Attack"]`, FocusCost: 5, IsPrototype: true), `self_channeled_tick` (was `cyclone` — Channeled, Tags: `["Melee","Attack"]`, FocusCost: 12/sec, Cooldown: 0.25s, IsPrototype: true), `self_burst` (was `nova` — Active, Tags: `["Attack"]`, FocusCost: 20, Cooldown: 1.5s, IsPrototype: true), `self_aura_tick` (was `damage_aura` — Aura, Tags: `["Aura"]`, FocusCost: 0.25 fraction, Cooldown: 1.0s, IsPrototype: true). Plus 7 new targeting-system prototype entries (TBD — see todo). **Save migration:** `CharacterManager.Load()` must rewrite old definition IDs before any registry lookup: `strike` → `entity_burst`, `cyclone` → `self_channeled_tick`, `nova` → `self_burst`, `damage_aura` → `self_aura_tick`. Apply to all `DefinitionId` fields in `OwnedSkillInstances` and `SlottedSkillInstanceIds` resolution. |
| `RecipeData`        | C# record   | Id, OutputItemId (string — definition ID), RecipeType (enum), MaterialCosts (Dictionary\<string, int\>). Crafting always produces a new instance at Tier = 1. |
| `SkillAugmentData`  | C# record   | Id (string), Name (string), RequiredTags (string[]) — skill must share at least one tag. EotId (string?, nullable) — links augment to an EoT definition; null for augments with no timed effect (e.g. Splash, Pierce). No Effect field — behaviour dispatched by Id in code. v1: Splash (`["Melee"]`, EotId: null), Pierce (`["Ranged"]`, EotId: null), Slow (`["Attack"]`, EotId: `"slow"`). |
| `SkillAugmentInstance` | Plain C# | Id (string, GUID), DefinitionId (string → `SkillAugmentRegistry`). No tier — augments are flat items in v1. |
| `SkillAugmentRegistry` | Static class | `All` dict, `Get(id)`, `All()` — static catalog of available Skill Augments. v1: 3 entries (splash, pierce, slow). |
| `EquipmentAugmentData` | C# record | Id (string), Name (string), RequiredTags (string[]) — equipment item must share at least one tag; empty = universal (works on any equipment). No Effect field — behaviour dispatched by Id in code. v1: Retaliation (`["Heavy"]`), Fortify (`["Heavy"]`), Dash Reflex (`["Light"]`), Ghost Step (`["Light"]`), Mending (`["Medium"]`), Adaptation (`["Medium"]`). |
| `EquipmentAugmentInstance` | Plain C# | Id (string, GUID), DefinitionId (string → `EquipmentAugmentRegistry`). No tier — augments are flat items in v1. |
| `EquipmentAugmentRegistry` | Static class | `All` dict, `Get(id)`, `All()` — static catalog of available Equipment Augments. v1: 6 entries (retaliation, fortify, dash_reflex, ghost_step, mending, adaptation). |
| `RecipeType`        | C# enum     | Gear, Skill, SkillAugment, EquipmentAugment                   |
| `CraftResult`       | C# enum     | Success, InsufficientMaterials, InventoryFull                  |
| `RecipeRegistry`    | Static class| `All` dict, `Get(id)`, `ForSlot(ItemSlot)`, `ForType(RecipeType)` — v1: 7 gear recipes + 7 skill recipes (prototype library: fixed_zone_burst, fixed_zone_tick, windup_burst, entity_debuff, tracked_tick, stackable_zone, triggered_zone_burst — 1× common each) + 5 SkillAugment recipes (Splash/Pierce/Slow/CriticalStrike/MagicDamage, 1× common each) + 6 EquipmentAugment recipes (Retaliation/Fortify/DashReflex/GhostStep/Mending/Adaptation, 1× common each). |
| `EnemyData`         | C# record   | EnemyType (string), BaseSpeed, BaseHealth, ContactDamage, DamageInterval, PhysicalResistance (float), MagicResistance (float), ModelPath (string — GLB res:// path, defaults to enemy_generic.glb) |
| `EotData`           | C# record   | Id (string), Name (string), ApplyChance (float 0–1), Duration (float seconds), IsDamageEot (bool), TickRate (float seconds — ignored when IsDamageEot = false), DamagePerTick (float — ignored when IsDamageEot = false). All EoTs share these four properties; only damage EoTs use TickRate and DamagePerTick. |
| `EotInstance`       | Plain C#    | Runtime state per active EoT on an enemy: DefinitionId (string), TimeRemaining (float), TickTimer (float — only relevant for damage EoTs). Held in `EnemyController._activeEots (Dictionary<string, EotInstance>)` keyed by EotData.Id — enforces one instance per type. |
| `EotRegistry`       | Static class| `Get(id)`, `GetAll()` — static catalogue of all EoT definitions. v1: `slow` (IsDamageEot = false), `burn` (IsDamageEot = true). |
| `ArchetypeMultiplierRegistry` | Static class | `Get(CharacterType, StatId) → float` — returns the archetype-specific level multiplier for a stat. Returns `0.1f` for any unspecified pair. Override table lives here — owned by the Balancer. Lives in `src/character/`. |

---

## Save Layers

### Character Save (`user://save.json`)
Managed by `CharacterManager` autoload. Written on every create/delete/upgrade.
```json
{
  "profile": {
    "coinBank": 150,
    "materials": { "crafting_common": 30 },
    "ownedGearInstances": [
      { "id": "<guid>", "defId": "bow_t1", "tier": 1, "socketedEquipmentAugmentIds": ["", ""] }
    ],
    "ownedSkillInstances": [
      { "id": "<guid>", "defId": "strike", "tier": 1, "socketedSkillAugmentIds": ["<skill-augment-guid>", ""] }
    ],
    "ownedSkillAugmentInstances": [
      { "id": "<skill-augment-guid>", "defId": "slow" }
    ],
    "ownedEquipmentAugmentInstances": [
      { "id": "<equip-augment-guid>", "defId": "mending" }
    ]
  },
  "characters": [
    {
      "id": "<guid>",
      "name": "Ironclad",
      "type": "Warrior",
      "runsCompleted": 3,
      "currentLevel": 7,
      "currentXp": 12,
      "equippedGear": {
        "Weapon":    { "id": "<guid>", "defId": "sword_t1", "tier": 1 },
        "Armor":     { "id": "<guid>", "defId": "heavy_armor_t1", "tier": 1 },
        "Accessory": { "id": "<guid>", "defId": "accessory_t1", "tier": 1 }
      },
      "slottedSkillInstanceIds": ["<skill-instance-guid>", "<skill-instance-guid>", "<skill-instance-guid>"]
    }
  ]
}
```
Note: empty augment slots serialize as `""` inside `socketedSkillAugmentIds` and `socketedEquipmentAugmentIds`. Load methods treat `""` entries as empty slots. Old saves carrying `socketedSupportInstanceIds` are migrated to `socketedSkillAugmentIds` on load. Old `augment`/`chainInstanceId` fields are dropped on migration.

`ownedGearInstances` and `ownedSkillInstances` hold only **unequipped** instances. Equipped gear lives as full `GearItemInstance` objects in `equippedGear` (nested dicts on disk). Slotted skills remain in `ownedSkillInstances`; `slottedSkillInstanceIds` holds GUIDs referencing them. `EquipItem` moves a gear instance from `OwnedGearInstances` to `EquippedGear`, swapping the old equipped instance back. `UnequipItem` / `UnequipSkillSlot` blocked if respective inventory is at capacity.

**Migration:** On load, if old `ownedItemIds` (string list) is present, each entry is wrapped into a `GearItemInstance` with a fresh GUID and Tier = 1. Same for `ownedSkillIds` → `SkillItemInstance`. Old `equippedItems` (slot → definition ID) wraps each into a `GearItemInstance` and sets the slot directly. Old `craftingCurrency1` int → `materials["crafting_common"]` migration also handled. Migration runs once on first load.

Starter gear and starter skills are both seeded in `SeedStarterGear()`. Starter gear writes directly to `EquippedGear`; the single starter `SkillItemInstance` goes into `OwnedSkillInstances`, referenced 3× in `SlottedSkillInstanceIds`.

### Run Session (in-memory only)
Lives on the `RunSession` node. Discarded when the scene unloads. On run end, `CharacterManager.RecordRunCompletion(finalLevel, finalXp, coinsEarned)` writes the persistent state.
- Elapsed time
- Coins earned this run

Level and XP are NOT run-scoped — they live on `CharacterData` and are written back at run end.

### Future: Profile Envelope
If multi-user slots or cloud saves are ever needed, evaluate wrapping save data under a profile envelope. `CharacterManager` is the only entry point — the refactor scope is bounded (1 constant, a handful of callers).

---

## Weapon

Single weapon per character (v1). `WeaponController` manages:

**Damage model — weapon is the root of all damage.** `PlayerController` computes damage at run start (and on level-up) via `ApplyWeaponDamage()` and pushes the results into `WeaponController`. The formula:

```
weaponDmg = weapon.BaseDamage
          × archetypeMultiplier          // PhysicalDamageMultiplier or MagicDamageMultiplier per weapon type
          × (1 + (level - 1) × 0.02)    // cumulative level bonus (DamageBonusPerLevel)
          × (1 + weapon.DamageBonus)     // weapon identity bonus (e.g. Sword +10% phys)
physDmg  = weaponDmg  (if weapon.BaseDamageType == Physical, else 0)
magicDmg = weaponDmg  (if weapon.BaseDamageType == Magic,    else 0)
```

`WeaponController` receives three calls from `PlayerController.ApplyWeaponDamage()`:
- `SetDamage(physDmg, magicDmg)` — sets `_physicalDamage` and `_magicDamage`
- `SetBaseDamageType(DamageType)` — sets `_baseDamageType`; determines which damage pool is used at fire time
- `SetGlobalCritChance(float)` — aggregated flat crit chance from all non-skill sources (weapon identity + future equipment augments, rings). Replaces the old `SetWeaponCritBonus()`.

**Crit stat architecture — two pools, globally aggregated:**

Crit Chance and Crit Multiplier are universal stats. `PlayerController` is responsible for aggregating all contributions before passing them into `WeaponController`:

| Pool | Sources | How passed |
|---|---|---|
| Global Crit Chance | Weapon identity bonus + equipment augments + ring stats | `SetGlobalCritChance(float)` — once per run start / level-up |
| Per-slot Crit Chance | Skill augments on that skill (e.g. Critical Strike) | `SetSlot()` `critChanceBonus` float parameter |
| Global Crit Multiplier | Fixed 1.5× in v1; future: investable via augments | `SetCritMultiplier(float)` — once per run start (v1: always 1.5) |

At fire time: `critChance = _globalCritChance + slot.CritChanceBonus`. If `critChance > 0` and roll succeeds: `baseDmg *= _critMultiplier`.

`SkillSlot.HasCriticalStrike (bool)` is replaced by `SkillSlot.CritChanceBonus (float)` — the aggregated float from that skill's augments. This removes the bool flag and makes adding future crit-granting skill augments a parameter change, not a new flag.

**Slot state:** `_slots[3]` — internal array of 3 slot states:
```
{ SkillData Skill, float CooldownTimer, List<string> EotIds,
  bool HasSplash, bool HasPierce, bool HasMagicDamage,
  float CritChanceBonus, bool AutoActivate,
  bool AuraActive, float AuraReserved,
  float DamageMultiplier, bool IsChanneling,
  List<Node3D> ActiveZones }
```
Each slot fires independently. Empty slots (null Skill) are skipped.

Exposes: `SetDamage(float, float)`, `SetBaseDamageType(DamageType)`, `SetGlobalCritChance(float)`, `SetCritMultiplier(float)`, `SetSlot(int, SkillData, ...)`.

`SetSlot` is called once per slot at run start. `SetGlobalCritChance` and `SetCritMultiplier` are called once at run start and again on level-up (same cadence as `SetDamage`).

Emits: `SkillFired(int slotIndex, float cooldown, string delivery)` — consumed by HUD skill bar (cooldown overlay) and `PlayerController` (`OnSkillFired` selects animation: `"Ranged"` → `shot_left` OneShot, anything else → `shot_right` OneShot + TimeScale).

**Animation/VFX is delivery-driven, not skill-identity-driven.** All skills that share the same `SkillType` emit the same `delivery` string and play the same animation. Non-prototype skills cloned from a prototype inherit this automatically — no per-skill animation mapping exists. The cyclone VFX is the only exception: it is hardcoded to `SkillType.Channeled` via `IsAnySlotChanneling()`, so any Channeled skill (not just Cyclone) will show the spinning ring.

[TBD] Weapon upgrade path (stages, piercing, AoE) — deferred.

---

## Focus (Skill Resource)

All archetypes spend Focus to fire skills. `PlayerController` owns the pool; `WeaponController` deducts or reserves on fire.

### Runtime state (PlayerController)

```
float CurrentFocus      // initialized to MaxFocus at run start
float _maxFocus         // seeded from StatId.MaxFocus via statBlock
float _focusRegen       // seeded from StatId.FocusRegen via statBlock
float _totalReserved    // sum of all active Aura reservation amounts; starts at 0
```

Regen in `_PhysicsProcess`: `CurrentFocus = Min(CurrentFocus + _focusRegen × delta, _maxFocus)`. Emits `FocusChanged(CurrentFocus, _maxFocus)` each tick.

Available Focus (amount skills may spend or draw against): `Max(0, CurrentFocus - _totalReserved)`.

### Methods (called by WeaponController)

```
float GetAvailableFocus()  →  Max(0, CurrentFocus - _totalReserved)

bool TrySpendFocus(float amount):
    if GetAvailableFocus() >= amount → CurrentFocus -= amount; emit FocusChanged; return true
    return false

void ReserveFocus(float absoluteAmount)   → _totalReserved += amount; emit FocusChanged
void UnreserveFocus(float absoluteAmount) → _totalReserved = Max(0, _totalReserved - amount); emit FocusChanged
```

### Firing guards by SkillType (WeaponController)

| SkillType | Guard | On fire |
|---|---|---|
| Active | `GetAvailableFocus() >= FocusCost` | `TrySpendFocus(FocusCost)` |
| Channeled | `IsChanneling == true` + `GetAvailableFocus() >= FocusCost × Cooldown` | `TrySpendFocus(FocusCost × Cooldown)` per tick |
| Aura (toggle on) | `GetAvailableFocus() >= FocusCost × _maxFocus` | `ReserveFocus(reserveAmount)` — no per-tick spend |
| Aura (toggle off) | — | `UnreserveFocus(slot.AuraReserved)` |
| Passive | — | — |

Channeled `FocusCost` is drain/sec; `Cooldown` is tick interval — so `FocusCost × Cooldown` is drain per tick. Aura `FocusCost` is a fraction (0.0–1.0); multiply by `_maxFocus` to get the absolute reservation amount.

Auras do **not** auto-deactivate at 0 Focus — the reservation is committed at toggle time. Active and Channeled skip the tick when insufficient Focus is available.

**Channeled `IsChanneling` state:**
- Manual: `TryFireSlot` sets `IsChanneling = true`; `ReleaseSlot` (key-up) sets it false.
- AutoActivate: `_PhysicsProcess` auto-sets `IsChanneling = FindNearestEnemy(_range) != null` each frame — starts channeling when an enemy enters range, stops when none remain.

**Channeled exclusivity:** while any slot has `IsChanneling = true`, `ProcessActiveSlot` returns immediately. Active skills (Strike, Nova) deal no damage and fire no animations. Auras are unaffected.

**Aura slot state additions (WeaponController `_slots` array):**

```
bool  AuraActive    // is the toggle currently on?
float AuraReserved  // absolute Focus units reserved while active
```

### Focus Shield

All archetypes. A separate damage-absorbing pool seeded at run start.

```
_maxFocusShield     = _maxFocus × BalanceConfig.Focus.ShieldFraction   // 30%
_currentFocusShield = _maxFocusShield
```

**Damage intercept** in `PlayerController.TakeDamage()` — after resistance calculation, before HP deduction:

```
absorbed = Min(_currentFocusShield, effectiveDamage)
_currentFocusShield -= absorbed
effectiveDamage -= absorbed
emit ShieldChanged(_currentFocusShield, _maxFocusShield)
```

**Shield regen** in `_PhysicsProcess`:

```
if _currentFocusShield < _maxFocusShield:
    _currentFocusShield = Min(_currentFocusShield + ShieldRegenPerSec × delta, _maxFocusShield)
emit ShieldChanged(_currentFocusShield, _maxFocusShield)
```

`ShieldRegenPerSec` is a `BalanceConfig.Focus` constant. No `StatId` entry yet — added when augments invest into shield regen.

**MaxFocus changes mid-run** (future — not v1): ceiling = `_maxFocus × ShieldFraction`, recalculated instantly. Current shield clamped to new ceiling on decrease; increases do not auto-fill.

### Archetype starting values (BalanceConfig.Focus)

| Archetype | MaxFocus base | RegenPerSec base | Shield base |
|---|---|---|---|
| Warrior | 80 | 12 | 24 (30% of 80) |
| Rogue | 100 | 15 | 30 (30% of 100) |
| Mage | 150 | 10 | 45 (30% of 150) |

### HUD

`FocusChanged(float, float)` → Focus bar, blue, below the health bar.  
`ShieldChanged(float, float)` → Focus Shield bar, light blue, below the Focus bar. Visible for all archetypes.

---

## Skill Bar (HUD)

An `HBoxContainer` anchored **bottom-center** of the HUD. **3 cells**, one per skill slot.

Each cell contains:
- Skill icon (placeholder if slot empty)
- A grey overlay — visible when the slot is on cooldown, hidden when ready
- A `ProgressBar` that **fills from bottom** (value 0 = just fired / empty, value 1.0 = ready to fire again)

`Hud._Ready()` wires `WeaponController.SkillFired` → `OnSkillFired(int slotIndex, float cooldown)`. On fire: set the matching cell's bar to 0, show grey overlay, begin filling. Filling is handled in `_Process` (bar value increments by `delta / cooldown` each frame). When bar reaches 1.0: hide grey overlay, stop incrementing.

The skill bar is the visual feedback loop for the skill cadence — 3 independent timers give the player a read on all active slots whether firing automatically or triggered manually.

---

## Crafting

Items are never dropped — they come exclusively from crafting. Each craftable item has one `RecipeData` entry in `RecipeRegistry`.

### Data shape

```
RecipeData(Id, OutputItemId, MaterialCosts: Dictionary<string, int>)
```

`MaterialCosts` keys are material IDs (`"crafting_common"`, `"crafting_rare"`, …). v1: every recipe costs `{ "crafting_common": 1 }`.

### `CharacterManager.CraftGearItem(string recipeId) → CraftResult`

```
recipe = RecipeRegistry.Get(recipeId)
if recipe == null → InsufficientMaterials
foreach (matId, qty): if insufficient → InsufficientMaterials
if OwnedGearInstances.Count >= MaxInventory → InventoryFull
deduct materials
OwnedGearInstances.Add(new GearItemInstance { Id = NewGuid(), DefinitionId = recipe.OutputItemId, Tier = 1 })
Save(); return Success
```

### `CharacterManager.CraftSkillItem(string recipeId) → CraftResult`

Same pattern, adds `new SkillItemInstance { Id = NewGuid(), DefinitionId = recipe.OutputItemId, Tier = 1, SocketedSkillAugmentIds = [] }` to `OwnedSkillInstances`.

### `CharacterManager.UpgradeGearItem(string instanceId) → CraftResult`

```
instance = OwnedGearInstances.Find(id) ?? equippedGear lookup
if instance == null → InsufficientMaterials
if instance.Tier >= MaxTier (3) → InsufficientMaterials  (already max)
if Profile.Materials["crafting_common"] < 1 → InsufficientMaterials
Profile.Materials["crafting_common"] -= 1
instance.Tier++
Save(); return Success
```

### `CharacterManager.UpgradeSkillItem(string instanceId) → CraftResult`

Same pattern as `UpgradeGearItem`, searches both `OwnedSkillInstances` and slotted skill instances.

### `CharacterManager.CraftSkillAugmentItem(string recipeId) → CraftResult`

Same pattern as `CraftSkillItem`. Adds `new SkillAugmentInstance { Id = NewGuid(), DefinitionId = recipe.OutputItemId }` to `OwnedSkillAugmentInstances`.

### `CharacterManager.SocketSkillAugment(string skillInstanceId, int slotIndex, string augmentInstanceId) → CraftResult`

```
skill = find SkillItemInstance by id (owned or slotted)
augment = OwnedSkillAugmentInstances.Find(augmentInstanceId)
if skill == null || augment == null → InsufficientMaterials
if slotIndex >= MaxAugmentSlots(skill.Tier) → InsufficientMaterials
augmentDef = SkillAugmentRegistry.Get(augment.DefinitionId)
if skill.Tags shares no tag with augmentDef.RequiredTags → InsufficientMaterials
skill.SocketedSkillAugmentIds[slotIndex] = augmentInstanceId
Save(); return Success
```

### `CharacterManager.RemoveSkillAugment(string skillInstanceId, int slotIndex)`

Sets `skill.SocketedSkillAugmentIds[slotIndex] = ""`. Free. Save().

### `CharacterManager.CraftEquipmentAugmentItem(string recipeId) → CraftResult`

Same pattern as `CraftSkillAugmentItem`. Adds `new EquipmentAugmentInstance { Id = NewGuid(), DefinitionId = recipe.OutputItemId }` to `OwnedEquipmentAugmentInstances`.

### `CharacterManager.SocketEquipmentAugment(string gearInstanceId, int slotIndex, string augmentInstanceId) → CraftResult`

```
gear = find GearItemInstance by id (owned or equipped)
augment = OwnedEquipmentAugmentInstances.Find(augmentInstanceId)
if gear == null || augment == null → InsufficientMaterials
if slotIndex >= MaxAugmentSlots(gear.Tier) → InsufficientMaterials
augmentDef = EquipmentAugmentRegistry.Get(augment.DefinitionId)
itemDef = ItemRegistry.Get(gear.DefinitionId)
if augmentDef.RequiredTags.Length > 0 && itemDef.Tags shares no tag with augmentDef.RequiredTags → InsufficientMaterials
gear.SocketedEquipmentAugmentIds[slotIndex] = augmentInstanceId
Save(); return Success
```

### `CharacterManager.RemoveEquipmentAugment(string gearInstanceId, int slotIndex)`

Sets `gear.SocketedEquipmentAugmentIds[slotIndex] = ""`. Free. Save().

### Modify panel (CharacterScreen)

Opened via left-click → **Modify** on any item (inventory or equipped slot). Implemented as a dynamically-built modal overlay — no pre-authored scene.

Structure (built in `ShowGearModifyPanel` / `ShowSkillModifyPanel`):
- `ColorRect` (full-screen, semi-transparent, `MouseFilter=Stop`) as the overlay
- `PanelContainer` (Iron & Slate styled, centered via `Anchor 0.5/GrowBoth`, min width 380px)
  - Title: item name + tier label; **✕** button closes the overlay
  - `HSeparator`
  - **Upgrade** button — disabled when `Tier >= ItemTier.Max` or `crafting_common < 1`. On press: `UpgradeGearItem(instanceId)` / `UpgradeSkillItem(instanceId)`, close overlay, `Refresh()`
  - (if `MaxEquipmentAugSlots > 0` or `MaxSkillAugmentSlots > 0`): separator + sub-label + `HBoxContainer` of slot buttons
    - **Filled slot** → click removes augment (`RemoveEquipmentAugment` / `RemoveSkillAugment`), rebuilds row in-place
    - **Empty slot** → click opens `NewStyledPopup()` listing compatible owned augments; on pick: `SocketEquipmentAugment` / `SocketSkillAugment`, rebuilds row in-place

Augment compatibility filter (gear): `EquipmentAugmentData.RequiredTags` empty OR intersects `ItemData.Tags`. No tag gate for skill augments (any augment can socket into any skill in v1).

### Material IDs

| ID | Display name | Source |
|----|--------------|--------|
| `crafting_common` | Common | 20% enemy drop (maps to old `craftingCurrency1`) |

Higher tiers (rare, exotic) will be added when their drop sources are designed.

---

## Effects over Time (EoT)

All timed effects on enemies — whether damage or control — flow through a single EoT system. See GDD § Effects over Time for design rules.

### Data

```
EotData(Id, Name, ApplyChance, Duration, IsDamageEot, TickRate, DamagePerTick)
EotInstance { DefinitionId, TimeRemaining, TickTimer }
```

`EotRegistry` is a static catalogue. `SkillAugmentData` references EoT by id (e.g. `slow` Skill Augment → `"slow"` EoT id). The mapping is 1-to-1 in v1 but augments may reference no EoT (e.g. Splash, Pierce are purely mechanical with no timed effect).

### Application flow

```
Projectile hits EnemyController
→ foreach socketed Skill Augment on the firing skill slot:
    eot = EotRegistry.Get(augment.EotId)   // null if augment has no EoT
    if eot == null: skip
    if Random() < eot.ApplyChance:
        enemy.ApplyEot(eot, critMultiplier)   // critMultiplier = hit's crit mult (1.0 if not a crit)
```

`Projectile` carries a `List<string> SkillAugmentEotIds` and a `float CritMultiplier` (1.0f if the firing hit was not a crit; crit multiplier value if it was). Both are resolved by `WeaponController` at fire time. No registry lookups on the hot path.

### `EnemyController.ApplyEot(EotData eot, float critMultiplier = 1.0f)`

```
if _activeEots.ContainsKey(eot.Id):
    _activeEots[eot.Id].TimeRemaining = eot.Duration   // refresh duration
    if eot.IsDamageEot:
        _activeEots[eot.Id].CritMultiplier = critMultiplier  // re-stamp with new hit's crit status
    return
// first application:
_activeEots[eot.Id] = new EotInstance {
    DefinitionId   = eot.Id,
    TimeRemaining  = eot.Duration,
    TickTimer      = eot.TickRate,
    CritMultiplier = eot.IsDamageEot ? critMultiplier : 1.0f
}
ApplyEotEffect(eot)   // e.g. reduce speed for Slow
```

**Crit stamping rule:** when the applying hit was a critical hit, the EoT instance is stamped with the crit multiplier for its full duration. Re-applying with a non-crit resets `CritMultiplier` to 1.0; re-applying with a crit refreshes it at the new crit multiplier. Non-damage EoTs always store 1.0 and the field has no effect.

### `EnemyController._Process(delta)` — EoT tick

```
foreach (id, inst) in _activeEots:
    inst.TimeRemaining -= delta
    if inst.TimeRemaining <= 0:
        RemoveEotEffect(eot)
        _activeEots.Remove(id)
        continue
    if eot.IsDamageEot:
        inst.TickTimer -= delta
        if inst.TickTimer <= 0:
            TakeDamage(eot.DamagePerTick * inst.CritMultiplier, DamageType.Magic)
            inst.TickTimer = eot.TickRate
```

### Effect dispatch

Effects are applied/removed by id — no subclassing:

| EoT id | On apply | On remove |
|---|---|---|
| `slow` | `Speed *= (1 − SlowFraction)` | `Speed /= (1 − SlowFraction)` |
| `burn` | _(nothing — damage fires on tick)_ | _(nothing)_ |

`SlowFraction` is a field on `EotData` [TBD value]. Damage EoTs use `DamagePerTick` from `EotData`. New EoTs are added by extending the dispatch switch — no new classes needed.

### Signal

`EnemyController` does not emit a signal for EoT state. HUD / VFX feedback is deferred — exact signal contract TBD when visual effects are designed.

---

## VFX

Hit effects use pre-built scenes from the **EffectBlocks** pack (see `technical-assets.md`). Custom effects live under `res://src/vfx/`. All effects are spawned by C# code: instantiate, add to scene root, set `GlobalPosition`, then schedule `QueueFree` via a timer.

| Scene | Trigger | Description |
|---|---|---|
| `res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn` | `Projectile.HitEnemy()` — every hit, melee and ranged | Orange + blue billboard sparkle burst at hit position. 4 particles, 0.5s lifetime. `activate_effects()` called via `Call()` after spawn. Auto-freed after 2s via C# `CreateTimer`. ScaleMin=40, ScaleMax=80. |
| `res://PolyBlocks/EffectBlocks/assets/impacts/impact_5.tscn` | `PlayerController.OnSkillFired()` — melee attacks only (`isMelee = true`) | Swing VFX: same scene, larger scale (ScaleMin=35, ScaleMax=55). 12 particles, 0.6s lifetime. Spawned at `GlobalPosition + (0, 20, 0)` (character height). Auto-freed after 2s. |

**Spawning from C#**:
```csharp
var fx = ImpactHitScene.Instantiate<GpuParticles3D>();
var mat = (ParticleProcessMaterial)fx.ProcessMaterial.Duplicate();  // duplicate — never modify the shared resource
mat.ScaleMin = 40f;
mat.ScaleMax = 80f;
fx.ProcessMaterial = mat;
GetTree().Root.AddChild(fx);
fx.GlobalPosition = hitPos;
fx.Call("activate_effects");
GetTree().CreateTimer(2.0).Timeout += fx.QueueFree;
```
Always wrap in `try/catch` — if VFX instantiation throws, the exception must not prevent `QueueFree()` in the calling `OnBodyEntered` from running (projectile would otherwise stay alive and appear to pass through enemies).

**World scale note** — player model is scale 9. Node-level `Scale` on `GPUParticles3D` does not reliably affect rendered particle size. Set `ProcessMaterial.ScaleMin/Max` directly at runtime (duplicate first). Current hit effect: `ScaleMin = 40, ScaleMax = 80`.

---

## Damage Pipeline

### Player taking damage

`PlayerController.TakeDamage(float rawAmount, DamageType type)`

```
effectiveDamage = rawAmount × (1 − DamageReduction)
if type == Physical:
    effectiveDamage ×= (1 − PhysicalResistance)
else if type == Magic:
    effectiveDamage ×= (1 − MagicResistance)

// Focus Shield intercept — all archetypes
if _currentFocusShield > 0:
    absorbed = Min(_currentFocusShield, effectiveDamage)
    _currentFocusShield -= absorbed
    effectiveDamage -= absorbed
    emit ShieldChanged(_currentFocusShield, _maxFocusShield)

CurrentHealth -= effectiveDamage
emit HealthChanged(CurrentHealth)

// Hit feedback (D4 style — no interrupt, screen-only)
Engine.TimeScale = 0.0                                  // hit stop: ~2 frames
CreateTimer(0.05s, ignoreTimeScale: true).Timeout       // restore TimeScale to 1.0
emit PlayerHit()                                        // HUD shows screen flash
```

**Hit stop** (`Engine.TimeScale = 0`) pauses the entire game world for ~0.05s — enemies, projectiles, animations all freeze. The HUD timer uses a real-time timer (`ignoreTimeScale: true`) to restore it. No guard needed against stacking since the restore always fires.

**Screen flash** is a full-screen `ColorRect` in the HUD (red, alpha ~0.3), tweened to alpha 0 over ~0.15s. Triggered by the `PlayerHit` signal. No gameplay effect — purely visual feedback.

**Melee windup delay** — `WeaponController` delays `HitMelee` by `cooldown × MeleeWindupFraction (0.35)` via `CreateTimer`. Damage lands mid-animation at the strike frame rather than on button press. Timer captures target by reference and skips the hit if target is already dead (`IsQueuedForDeletion()`).

`DamageReduction` and `PhysicalResistance` are runtime stats on `PlayerController`, seeded at run start from the equipped armor and accessory respectively. All current chase enemies pass `DamageType.Physical`. Future ranged/magic enemies pass `DamageType.Magic`.

### Enemy taking damage

`EnemyController.TakeDamage(float rawAmount, DamageType type)`

```
effectiveDamage = rawAmount × (1 − resistance[type])
```

Resistance values per enemy type live in `EnemyData` (`PhysicalResistance`, `MagicResistance`).

### Stat seeding at run start

`PlayerController._Ready()` reads `CharacterManager.SelectedCharacter` and equipped items. After seeding, `GlobalPosition` is set by `DungeonGenerator` to `SpawnPosition` (the centre floor cell, `CellToWorld(0,0)`) — not world origin. `DungeonGenerator._Ready()` runs after `PlayerController._Ready()` (scene order), so it always finds the player in the group and moves it.

All stats are derived via the archetype multiplier formula (see Archetype Multiplier System below) — `BuildStatBlock()` returns pre-computed effective values.

```
statBlock          = character.BuildStatBlock()   // applies multiplier formula internally

MaxHealth          = statBlock.Get(MaxHp)
Speed              = statBlock.Get(Speed)
PhysicalResistance = statBlock.Get(PhysicalResistance)
MagicResistance    = statBlock.Get(MagicResistance)
DamageReduction    = hat.DamageReduction + body.DamageReduction  // flat sum, not multiplied
EffectiveRange     = weapon.PreferredDelivery == "Ranged"
                       ? (weapon.WeaponRange + hat.RangeModifier + body.RangeModifier) * GameScale.TileSize
                       : weapon.WeaponRange * GameScale.TileSize
                     // Range Modifiers only apply to ranged weapons — see GDD § Hat & Body
                     // tile values × TileSize → world units; standalone float, not part of StatId

// Weapon-rooted damage — computed in PlayerController.ApplyWeaponDamage():
archetypeMult      = weapon.BaseDamageType == Magic
                       ? archetype.MagicDamageMultiplier
                       : archetype.PhysicalDamageMultiplier
levelBonus         = 1 + (level - 1) × BalanceConfig.LevelUp.DamageBonusPerLevel
weaponDmg          = weapon.BaseDamage × archetypeMult × levelBonus × (1 + weapon.DamageBonus)
physDmg            = weapon.BaseDamageType == Physical ? weaponDmg : 0
magicDmg           = weapon.BaseDamageType == Magic    ? weaponDmg : 0

WeaponController.SetDamage(physDmg, magicDmg)
WeaponController.SetBaseDamageType(weapon.BaseDamageType)
WeaponController.SetCritMultiplier(BalanceConfig.SkillAugments.CritMultiplier)  // fixed 1.5× in v1

// Global crit chance — weapon identity + future equipment augment / ring contributions
globalCritChance   = weapon.CritChanceBonus   // + future sources aggregated here
WeaponController.SetGlobalCritChance(globalCritChance)

// Per-slot setup — skill augments resolved via AugmentResolver
for i in 0..2:
    instanceId = character.SlottedSkillInstanceIds[i]   // "" = skip
    if instanceId is non-empty:
        skill            = CharacterManager.FindSkillInstance(instanceId).Definition
        activeAugments   = AugmentResolver.Resolve(instance.SocketedSkillAugmentIds, lookup)
        slotCritChance   = sum of CritChance from any Critical Strike augments in activeAugments
        hasMagicDamage   = activeAugments contains magic_damage
        hasSplash / hasPierce / eotIds  = resolved from activeAugments
        WeaponController.SetSlot(i, skill, eotIds, hasSplash, hasPierce, hasMagicDamage, slotCritChance)
```

**`ApplyWeaponDamage` is called at run start and again on every level-up** — same cadence as `BuildStatBlock()`. `SetGlobalCritChance` and `SetCritMultiplier` follow the same cadence.

### Archetype Multiplier System

Every modifier source is amplified by the character's archetype and current level:

```
effective_stat = base_stat + modifier_total × (level × archetype_multiplier)
```

`base_stat` = archetype base value — set directly, **not** subject to the multiplier. `modifier_total` = sum of all modifier sources: level-up bonuses + item contributions. At level 1 (no modifiers yet) effective stats equal base archetype stats unchanged.

The default multiplier is `0.1` for every stat/archetype pair. Each archetype overrides only the stats that define its identity — all other pairs stay at default:

| Stat | Warrior | Rogue | Mage |
|------|---------|-------|------|
| Max HP | TBD | 0.1 | 0.1 |
| Speed | 0.1 | TBD | 0.1 |
| Physical Damage | TBD | TBD | 0.1 |
| Magic Damage | 0.1 | 0.1 | TBD |
| Physical Resistance | TBD | 0.1 | 0.1 |
| Magic Resistance | 0.1 | 0.1 | TBD |

Override values are TBD — owned by the Balancer.

**Implementation:** A static `ArchetypeMultiplierRegistry` maps `(CharacterType, StatId) → float`, returning `0.1f` for any unspecified pair. `BuildStatBlock()` calls this after accumulating flat modifiers and before returning — callers always receive effective values.

**Mid-run level-up:** `BuildStatBlock()` is called once at run start and once per level-up event (not per frame). On level-up, `PlayerController` calls `BuildStatBlock()` with the new level and reseeds all stats from the result. This keeps the formula live throughout a run at negligible cost (~10 calls over 5 minutes).

`DamageReduction` is not part of the multiplier system — it is a flat percentage from the armor item and is applied as-is.

---

## Enemy Spawner — Wave Scaling

Time-driven, no fixed waves. `EnemySpawner` recalculates each spawn:
- **Spawn rate** — starts immediately at t=0; interval = `InitialInterval / (1 + minutes * 0.5)`, clamped to `MinInterval = 0.3s`
- **Spawn position** — random floor tile from `DungeonGenerator.FloorPositions` at least `SpawnRadius * 0.5` world units from the player; falls back to a ring spawn if no dungeon is present
- **Enemy types** — v1: single type only (`Skeleton`). Pool will expand in future milestones.

| Type     | Speed | HP | Damage | Physical Resist | Model                        |
|----------|-------|----|--------|-----------------|------------------------------|
| Skeleton | 65    | 2  | 5      | 10%             | `kaykit_enemy_skeleton.glb`  |

All types receive a time-scaling bonus on top: `Speed += 5 * minutes`, `MaxHealth += 3 * (int)minutes`.

---

## Map Attributes

Each run is played on a map. Maps carry an attribute set that modifies run behaviour. The attribute set is small now and will grow.

| Attribute  | Type  | Effect                                                                     |
|------------|-------|----------------------------------------------------------------------------|
| `MapLevel` | `int` | On enemy death, `PlayerController.CollectXp(MapLevel)` is called directly — no pickup required. Stacks on top of any XP Shard drop. |

`MapLevel` is passed into the run scene at startup (e.g. via `RunSession` or a `MapData` resource — exact wiring TBD when maps are selectable).

---

## Drop System

On enemy death, two XP sources fire independently:

1. **Kill XP** — `1 × MapLevel` XP granted instantly via `PlayerController.CollectXp()`
2. **XP Shard drop** — physical `XpShard` scene spawned; player must walk over it (value = 5 XP)

Other drops hardcoded in `EnemyController.Die()`:

| Drop              | Chance | Notes                                                                  |
|-------------------|--------|------------------------------------------------------------------------|
| XP Shard            | 100%   | Always dropped; value = 5 XP                                           |
| Coin              | 25%    | `CoinPickup` auto-collected; reports to `RunSession.AddCoin()`         |
| Health pack       | 10%    | `HealthPickup` heals player for 15 HP on contact                       |
| Crafting currency | 20%    | Instant; calls `RunSession.AddCraftingCurrency1(1)` — no pickup scene  |

> Planned: large XP Shards, weighted drop tables via `EnemyData` resource.

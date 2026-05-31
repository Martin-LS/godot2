# Technical Design Document — Data, Systems & Crafting

> Part of the technical docs. See also `technical-scene.md` for scene layout, architecture overview, signals, and C# conventions.
> Living document — architecture will evolve as systems are built and playtested.

## Data / Resource Types

| Class               | Kind        | Fields                                                         |
|---------------------|-------------|----------------------------------------------------------------|
| `GearItemInstance`  | Plain C#    | Id (string, GUID), DefinitionId (string → `ItemRegistry`), Tier (int, 1–3), SocketedEquipmentAugmentIds (List\<string\> — instance GUIDs, one entry per augment slot; "" = empty; max length = augment slots for tier: 1/2/3). Authoritative tier for runtime stat scaling. |
| `SkillItemInstance` | Plain C#    | Id (string, GUID), DefinitionId (string → `SkillRegistry`), Tier (int, 1–3), SocketedSkillAugmentIds (List\<string\> — instance GUIDs, one entry per augment slot; "" = empty slot; max length = augment slots for tier: 1/2/3). |
| `ProfileData`       | Plain C#    | CoinBank, Materials (Dictionary\<string, int\>), OwnedGearInstances (List\<GearItemInstance\>), OwnedSkillInstances (List\<SkillItemInstance\>), OwnedSkillAugmentInstances (List\<SkillAugmentInstance\>), OwnedEquipmentAugmentInstances (List\<EquipmentAugmentInstance\>), MaxInventory (const = 50) — applies separately to each list. Account-shared. Migration: old `ownedItemIds`/`ownedSkillIds` string lists are wrapped into instances (new GUID, Tier = 1) on load. Old `augment`/`chainInstanceId` fields on skill instances are dropped on load. |
| `CharacterData`     | Plain C#    | Id, Name, Type (enum), RunsCompleted, CurrentLevel, CurrentXp, EquippedGear (Dictionary\<string, GearItemInstance\> — slot → full instance), SlottedSkillInstanceIds (List\<string\> — instance GUIDs; skill instances stay in `OwnedSkillInstances`). Archetype base stats computed inline in `BuildStatBlock()` — applies archetype multiplier formula before returning. |
| `CharacterType`     | C# enum     | Warrior, Rogue, Mage                                           |
| `StatId`            | C# enum     | MaxHp, Speed, PhysicalDamage, MagicDamage, PhysicalResistance, MagicResistance |
| `StatModifier`      | Plain C#    | StatId, ModifierType (FlatAdd), Value (float), ModifierSource (Level, Item) |
| `StatBlock`         | Plain C#    | Internal flat modifier list per `StatId`. `Get(StatId)` returns the sum of all flat modifiers for that stat — archetype multiplier is applied in `BuildStatBlock()` before the block is returned, so callers always get effective values. |
| `ItemData`          | C# record   | Id, Name, Slot (enum), IconPath, Tags (string[] — equipment tags for augment compatibility; e.g. `["Melee"]` for Sword, `["Heavy"]` for heavy armour, `[]` for Accessory) — plus slot-specific fields: `WeaponAffinity`, `SkillBonus (float)` for Weapon; `ArmorCategory`, `BonusHp`, `BonusSpeed`, `DamageReduction (float)` for Armor; `PhysicalResistance (float)` for Accessory. Unused fields default to zero. `Tier` removed — tier lives on `GearItemInstance`, not the definition. |
| `ItemSlot`          | C# enum     | Weapon, Armor, Accessory                                       |
| `SkillData`         | C# record   | Id, Name, Type (SkillType enum), Tags (string[]) — e.g. `["Melee","Attack"]`, `["Ranged","Attack"]`, `["Ranged","Magic","Spell"]`. Cooldown (float, seconds; 0 for Passive), Range (float), IconPath (string, default ""). No Tier — tier lives on `SkillItemInstance`. |
| `SkillType`         | C# enum     | Active, Passive                                                |
| `WeaponAffinity`    | C# enum     | None, Melee, Ranged, Magic                                     |
| `ArmorCategory`     | C# enum     | None, Heavy, Medium, Light                                     |
| `DamageType`        | C# enum     | Physical, Magic                                                |
| `ItemTier`          | C# static class (const ints) | Common = 1, Uncommon = 2, Rare = 3, Max = 3. `Label(int)` → display name. `BackgroundColor(int)` → Godot `Color`. Used for tier background colour in UI. |
| `ItemRegistry`      | Static class| `All` dict, `Get(id)`, `ForSlot(slot)` — 7 starter gear definitions. Definitions carry no tier — all instances start at Tier = 1 when crafted. |
| `SkillRegistry`     | Static class| `All` dict, `Get(id)` — v1: 3 entries: `strike` (Tags: `["Melee","Attack"]`), `arrow` (Tags: `["Ranged","Attack"]`), `bolt` (Tags: `["Ranged","Magic","Spell"]`). |
| `RecipeData`        | C# record   | Id, OutputItemId (string — definition ID), RecipeType (enum), MaterialCosts (Dictionary\<string, int\>). Crafting always produces a new instance at Tier = 1. |
| `SkillAugmentData`  | C# record   | Id (string), Name (string), RequiredTags (string[]) — skill must share at least one tag. EotId (string?, nullable) — links augment to an EoT definition; null for augments with no timed effect (e.g. Splash, Pierce). No Effect field — behaviour dispatched by Id in code. v1: Splash (`["Melee"]`, EotId: null), Pierce (`["Ranged"]`, EotId: null), Slow (`["Attack"]`, EotId: `"slow"`). |
| `SkillAugmentInstance` | Plain C# | Id (string, GUID), DefinitionId (string → `SkillAugmentRegistry`). No tier — augments are flat items in v1. |
| `SkillAugmentRegistry` | Static class | `All` dict, `Get(id)`, `All()` — static catalog of available Skill Augments. v1: 3 entries (splash, pierce, slow). |
| `EquipmentAugmentData` | C# record | Id (string), Name (string), RequiredTags (string[]) — equipment item must share at least one tag; empty = universal (works on any equipment). No Effect field — behaviour dispatched by Id in code. v1: Retaliation (`["Heavy"]`), Fortify (`["Heavy"]`), Dash Reflex (`["Light"]`), Ghost Step (`["Light"]`), Mending (`["Medium"]`), Adaptation (`["Medium"]`). |
| `EquipmentAugmentInstance` | Plain C# | Id (string, GUID), DefinitionId (string → `EquipmentAugmentRegistry`). No tier — augments are flat items in v1. |
| `EquipmentAugmentRegistry` | Static class | `All` dict, `Get(id)`, `All()` — static catalog of available Equipment Augments. v1: 6 entries (retaliation, fortify, dash_reflex, ghost_step, mending, adaptation). |
| `RecipeType`        | C# enum     | Gear, Skill, SkillAugment, EquipmentAugment                   |
| `CraftResult`       | C# enum     | Success, InsufficientMaterials, InventoryFull                  |
| `RecipeRegistry`    | Static class| `All` dict, `Get(id)`, `ForSlot(ItemSlot)`, `ForType(RecipeType)` — v1: 7 gear recipes + 3 skill recipes (Strike/Arrow/Bolt, 1× common each) + 3 SkillAugment recipes (Splash/Pierce/Slow, 1× common each) + 6 EquipmentAugment recipes (Retaliation/Fortify/DashReflex/GhostStep/Mending/Adaptation, 1× common each). |
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

- `PhysicalBaseDamage (float)` and `MagicBaseDamage (float)` — both set at run start from `CharacterData.BuildStatBlock()` after the archetype multiplier formula is applied. The equipped weapon item does **not** contribute to base damage. At fire time, the slot uses `PhysicalBaseDamage` or `MagicBaseDamage` based on whether the skill has the `Magic` tag.
- `_slots[3]` — internal array of 3 slot states, each holding `{ SkillData Skill, float CooldownTimer, float SkillBonus }`. Each slot fires independently when its timer reaches 0. Empty slots (null Skill) are skipped.
- Per slot: `SkillBonus` is non-zero only when the weapon's affinity tag appears in the slot's skill Tags array. `DamageType` per slot: skill has `Magic` tag → `Magic`, else `Physical`. `CooldownTimer` counts down independently from each slot's `Skill.Cooldown`.

Exposes: `SetDamage(float physicalDamage, float magicDamage)`, `SetSlot(int slotIndex, SkillData, float weaponSkillBonus)`.

`SetSlot` replaces `SetSkill` — called once per slot at run start. Stores `SkillData`, pre-computed `SkillBonus`, and seeds `CooldownTimer` to 0 so each slot fires immediately on the first frame.

Effective damage per shot: `BaseDamage + slot.SkillBonus`.

Emits: `SkillFired(int slotIndex, float cooldown)` when a slot fires — consumed by the HUD skill bar.

[TBD] Weapon upgrade path (stages, piercing, AoE) — deferred.

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

### Equipment Crafting tab — Create sub-tab (CharacterScreen)

Recipe list from `RecipeRegistry.ForType(RecipeType.Gear)`. Each row: button disabled when materials insufficient or inventory full. On press: `CraftGearItem(recipeId)`, then `Refresh()`.

### Equipment Crafting tab — Modify sub-tab (CharacterScreen)

Contains a single loaded-item slot (Button, 60×60), an **Upgrade** button, and an **Equipment Augment Slots** row.
- Slot is empty until player clicks it → opens inline `PopupMenu` listing all gear instances (owned + equipped across all characters)
- Once loaded: shows instance icon + tier background colour
- **Upgrade** button: disabled when no instance loaded, tier already 3, or insufficient materials. On press: `UpgradeGearItem(instanceId)`, then `Refresh()`
- **Equipment Augment Slots** row: shows one slot button per augment slot (count derived from loaded item's tier). Empty slot → opens `EquipmentAugmentPickerPanel` filtered to `OwnedEquipmentAugmentInstances` whose `EquipmentAugmentData.RequiredTags` intersect with the item's tags (or are empty). Occupied slot → `PopupMenu` (Remove — free, augment returns to inventory).

### Skill Crafting tab — Create sub-tab (CharacterScreen)

Parallel to gear Create tab. Recipe list from `RecipeRegistry.ForType(RecipeType.Skill)`. On press: `CraftSkillItem(recipeId)`.

### Skill Crafting tab — Modify sub-tab (CharacterScreen)

Contains a loaded-skill slot (Button, 60×60), an **Upgrade** button, and a **Skill Augment Slots** row.
- Slot is empty until player clicks it → opens inline `PopupMenu` listing all `OwnedSkillInstances`
- **Upgrade** button: same disabled conditions as gear upgrade
- **Skill Augment Slots** row: shows one slot button per augment slot (count derived from loaded skill's tier). Empty slot → opens `SkillAugmentPickerPanel` filtered to `OwnedSkillAugmentInstances` whose `SkillAugmentData.RequiredTags` intersect with the skill's tags. Occupied slot → `PopupMenu` (Remove — free, augment returns to inventory).

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
        enemy.ApplyEot(eot)
```

`Projectile` carries a `List<string> SkillAugmentEotIds` resolved by `WeaponController` at fire time from the skill slot's socketed Skill Augments. No registry lookups on the hot path — just a list of IDs the projectile received at instantiation.

### `EnemyController.ApplyEot(EotData eot)`

```
if _activeEots.ContainsKey(eot.Id):
    _activeEots[eot.Id].TimeRemaining = eot.Duration   // refresh
    return
// first application:
_activeEots[eot.Id] = new EotInstance { DefinitionId = eot.Id, TimeRemaining = eot.Duration, TickTimer = eot.TickRate }
ApplyEotEffect(eot)   // e.g. reduce speed for Slow
```

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
            TakeDamage(eot.DamagePerTick, DamageType.Magic)
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

## Damage Pipeline

### Player taking damage

`PlayerController.TakeDamage(float rawAmount, DamageType type)`

```
effectiveDamage = rawAmount × (1 − DamageReduction)
if type == Physical:
    effectiveDamage ×= (1 − PhysicalResistance)
else if type == Magic:
    effectiveDamage ×= (1 − MagicResistance)
CurrentHealth -= effectiveDamage
emit HealthChanged(CurrentHealth)
```

`DamageReduction` and `PhysicalResistance` are runtime stats on `PlayerController`, seeded at run start from the equipped armor and accessory respectively. All current chase enemies pass `DamageType.Physical`. Future ranged/magic enemies pass `DamageType.Magic`.

### Enemy taking damage

`EnemyController.TakeDamage(float rawAmount, DamageType type)`

```
effectiveDamage = rawAmount × (1 − resistance[type])
```

Resistance values per enemy type live in `EnemyData` (`PhysicalResistance`, `MagicResistance`).

### Stat seeding at run start

`PlayerController._Ready()` reads `CharacterManager.SelectedCharacter` and equipped items. After seeding, `GlobalPosition` is forced to `Vector3.Zero` — the map center — regardless of any saved node position in the scene file.

All stats are derived via the archetype multiplier formula (see Archetype Multiplier System below) — `BuildStatBlock()` returns pre-computed effective values.

```
statBlock          = character.BuildStatBlock()   // applies multiplier formula internally

MaxHealth          = statBlock.Get(MaxHp)
Speed              = statBlock.Get(Speed)
PhysicalDamage     = statBlock.Get(PhysicalDamage)
MagicDamage        = statBlock.Get(MagicDamage)
PhysicalResistance = statBlock.Get(PhysicalResistance)
MagicResistance    = statBlock.Get(MagicResistance)
DamageReduction    = armor.DamageReduction        // flat item stat, not multiplied

WeaponController.SetDamage(PhysicalDamage, MagicDamage)

for i in 0..2:
    instanceId = character.SlottedSkillInstanceIds[i]   // "" = skip
    if instanceId is non-empty:
        skill = CharacterManager.FindSkillInstance(instanceId).Definition
        bonus = skill.Tags.Contains(weapon.WeaponAffinity.ToString()) ? weapon.SkillBonus : 0
        WeaponController.SetSlot(i, skill, bonus)
```

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
- **Spawn position** — fixed-radius ring (350px) around the player; viewport-size-independent
- **Enemy types** — v1: single type only (`Skeleton`). Pool will expand in future milestones.

| Type     | Speed | HP | Damage | Physical Resist | Model                  |
|----------|-------|----|--------|-----------------|------------------------|
| Skeleton | 65    | 2  | 12     | 10%             | `enemy_skeleton.glb`   |

All types receive a time-scaling bonus on top: `Speed += 10 * minutes`, `MaxHealth += 5 * (int)minutes`.

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

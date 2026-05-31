# Game Design Document — godot1

> Living document — details will evolve as the game is playtested.

## Overview

A top-down horde survival game (Vampire Survivors / Diablo style). The player takes a persistent character into timed runs against escalating enemy waves. Skills can fire automatically on cooldown or be activated manually — survival is about positioning and progression, not twitch reflexes.

Every run makes the character permanently stronger: level and XP carry over, stat bonuses stack, and coins and crafting materials earned go into a shared account pool. Between runs, players craft gear from materials — building both their character and their item collection over time. Coins accumulate but have no spend mechanic yet.

The game has two intertwined goals: grow your character through runs, and build your gear through crafting.

---

## Core Mechanics

### Movement
- Top-down, 8-directional
- [TBD] Speed, acceleration values

### Combat

> **Design inspiration:** The skill system takes Path of Exile 2 as its north star — skills as socketable items, supports that modify individual skills, and trigger/chain mechanics. Our specific skills and numbers will differ, but the philosophy (deep, legible skill modification through a gem/support layer) guides all skill design decisions.

Skills drive all combat. Each skill has a **type**:

| Skill type | Behaviour |
|---|---|
| Active | Fires automatically when its cooldown expires, or can be triggered manually by the player. |
| Passive | On/off toggle. Effect is always-on while enabled. |

The **skill bar** on the run HUD shows all slotted skills and their cooldown / toggle state.

**v1:** Three skill slots, each firing independently on its own cooldown. v1 has three skills (Strike, Arrow, Bolt). Starter characters have all 3 slots pre-filled with the same skill; players can mix freely. Cooldown: 0.8s per slot.

Every skill has one or more **tags** — descriptors that other systems react to (e.g. `Melee`, `Ranged`, `Magic`, `Attack`, `Spell`). Tags are not restrictions — any character can equip any skill. Tags determine which supports are compatible with a skill and which weapon affinity bonuses apply.

**v1 tags:** `Melee`, `Ranged`, `Magic`, `Attack`, `Spell` (expand as more skills and supports are added).

Character damage scales with character level (via level-up bonuses) and archetype base damage. Weapons do not contribute base damage — they provide a flat bonus to skills with matching tags (see Gear Slots).

### Damage Types

Every damage source has a **damage type**. Every entity that can take damage has a **resistance** value per type (percentage reduction).

`effective damage = raw damage × (1 − resistance)`

**v1 damage types:** Physical, Magic

**Future expansion:** Elemental types (Fire, Lightning, Frost, etc.) will be added as the system grows — the formula and resistance model extend naturally.

Resistances are always soft (never total immunity). Exact values are TBD.

### Effects over Time (EoT)

Skills and supports can apply **Effects over Time (EoT)** to enemies on hit. All EoTs follow the same rules regardless of what the specific effect does:

Every EoT has the same four properties. All EoTs follow the same application rules:

| Property | All EoTs | Notes |
|---|---|---|
| Apply chance | Yes | % chance to apply on hit |
| Duration | Yes | How long the effect lasts; refreshes on reapply |
| Tick rate | Damage EoTs only | Ignored for non-damage EoTs |
| Damage per tick | Damage EoTs only | Ignored for non-damage EoTs |

**Application rules (all EoTs):**
- No stacking — only one instance of each EoT type per enemy at a time
- Reapplying refreshes the duration rather than stacking or being ignored

The EoT type defines *what it does* when active:

| EoT | Damage per tick? | What it does |
|---|---|---|
| Slow | No | Reduces enemy movement speed |
| Burn | Yes (Magic) | Deals damage per tick |
| Vulnerability | No | Increases damage taken by the enemy |

When designing new EoTs: if it deals damage per tick, set tick rate and damage per tick. If not, leave those blank. That is the only distinction.

v1 supports only use Slow. All future effects follow this same framework.

### Interaction
- Collectibles auto-collected on contact (XP Shards, coins, health)
- [TBD] Any interactive objects (chests, shrines, etc.)

---

## Characters

Every run requires a character. Characters are created by the player, persist between runs, and grow over time. A player may own multiple characters simultaneously and delete any they no longer want.

### Character Archetypes

| Archetype | Max HP | Speed | Physical Damage | Magic Damage | Default build               |
|-----------|--------|-------|----------------|--------------|------------------------------|
| Warrior   | 150    | 170   | 20             | 0            | Sword + Heavy armor — close-range brawler |
| Rogue     | 80     | 260   | 15             | 0            | Bow + Light armor — fast, fragile kiter   |
| Mage      | 100    | 200   | 0              | 35           | Wand + Medium armor — glass cannon        |

Stat values are TBD. Default build reflects starter gear — players are free to deviate.

#### Archetype Stat Multipliers

All stats scale through the archetype multiplier formula:

`modifier_total × (level × multiplier)`

Where `modifier_total` is the sum of all modifier sources for that stat: level-up bonuses + item contributions. Base archetype stats (the starting values at level 1) are not subject to the multiplier — they are applied directly. At level 1 (no modifiers yet) effective stats equal the archetype base stats unchanged. The default multiplier for every stat on every archetype is `0.1`. Archetypes override only the stats that define their identity — everything else stays at default.

| Stat | Warrior | Rogue | Mage |
|------|---------|-------|------|
| Max HP | `level × TBD` | default | default |
| Speed | default | `level × TBD` | default |
| Physical Damage | `level × TBD` | `level × TBD` | default |
| Magic Damage | default | default | `level × TBD` |
| Physical Resistance | `level × TBD` | default | default |
| Magic Resistance | default | default | `level × TBD` |

"default" = `level × 0.1`. Exact override values are TBD — owned by the Balancer.

### Character Lifecycle
1. **Create** — player picks a name (required) and archetype
2. **Select** — choose a character from the roster before a run
3. **Run** — character starts at their saved level; every new level gained during the run is permanent
4. **Grow** — level and XP carry over to the character; coins and crafting materials go to the shared account pool; permanent stat bonuses are applied automatically on level up
5. **Delete** — player can permanently remove a character (irreversible)

A run cannot start without a selected character.

### Controls
| Input   | Action                                      |
|---------|---------------------------------------------|
| WASD    | Move                                        |
| —       | Attack (auto — fires on cooldown)           |
| [TBD]   | Manual skill activation                     |
| ESC     | Pause                                       |

---

## In-Run Progression

### Level Up
- Killing an enemy grants **`1 XP × map level`** instantly (kill reward, no pickup required)
- Killing enemies also drops **XP Shards** — collecting them adds further XP
- Both sources fill the same XP bar
- On level up: permanent HP and damage bonuses are applied automatically, scaled by archetype and level
- Level and XP within the current level persist when the run ends; the character picks up exactly where they left off
- No popup or pause — levelling up is seamless

### Enemy Drops
| Drop               | Effect                                         | Drop chance |
|--------------------|------------------------------------------------|-------------|
| XP Shard             | Feeds level-up bar                             | 100%        |
| Coin               | Added to coin bank on run end                  | 25%         |
| Health pickup      | Restores HP instantly                          | 10%         |
| Crafting material (common) | Added to crafting-currency-1 bank on run end | 20% |
| Crafting material (higher tiers) | Added to respective material bank on run end | [TBD — rarer, tied to item tier] |

Drop rarity for crafting materials scales with tier — the more exotic the items a material can produce, the rarer it drops.

---

## Maps

Maps are the arenas where runs take place. Each map has **attributes** that modify the run.

### Map Attributes

| Attribute   | Description                                                                 |
|-------------|-----------------------------------------------------------------------------|
| Map Level   | Scales kill XP reward — killing an enemy grants `1 XP × map level` directly, on top of any XP Shard the enemy drops |

More attributes will be added in future (e.g. enemy density modifiers, environmental hazards, drop bonuses).

---

## Run Structure

- **Spawn:** Player always starts at the center of the map
- **Duration:** Fixed time limit — 5 minutes
- **Map:** Each run takes place on a map; the map's attributes apply for the full run
- **Difficulty scaling:** Enemy count, speed, and variety increase over time
- **Run end conditions:**
  - Player dies → run over
  - Timer expires → run won [boss mechanic TBD]
- **Run rewards:** Level, XP, coins, and crafting materials earned all persist; player returns to the character screen

---

## Enemies

| Type     | Behavior          | Unlocks | Physical Resist | Magic Resist | Notes                        |
|----------|-------------------|---------|-----------------|--------------|------------------------------|
| Standard | Chase player      | 0:00    | 0%              | 0%           | Balanced — grey           |
| Runner   | Chase player fast | 1:00    | 0%              | 15%          | Fragile, high speed — purple |
| Tank     | Chase player slow | 2:00    | 20%             | 0%           | High HP, high damage — orange|
| [TBD]    | Ranged attacker   | —       | —               | —            | Future type                  |
| [TBD]    | Boss              | Run end | —               | —            | Spawns when timer expires    |

All types scale with elapsed time — speed and HP increase per minute. Spawn rate also accelerates.

---

## Meta-Progression (Between Runs)

### Level Bonuses (automatic)
Each level gained during a run permanently increases the character's HP and damage. Bonuses scale with both archetype and level — each archetype grows faster in the stats that define its playstyle. These stack across all runs and are applied automatically on level-up. Exact growth coefficients are owned by the Balancer.

### Item Tiers

All items — both equipment and skills — have a **tier** that represents quality and power level. Tier is shown as the background colour of the item icon everywhere it appears (inventory, slots, pickers).

| Tier     | Colour | Notes                        |
|----------|--------|------------------------------|
| Common   | Gray   | Starter / lowest power       |
| Uncommon | Green  | Mid tier                     |
| Rare     | Blue   | Highest tier (v1)            |

Exact stat differences per tier are TBD. Higher tier also unlocks more support slots on skill items (see Supports).

---

### Gear Slots

Characters can equip up to 3 gear items (one per gear slot) and up to 3 skill items (one per skill slot). All items persist between runs. Each slot has a distinct role:

| Slot      | Role                                                             | Progression axis                    |
|-----------|------------------------------------------------------------------|-------------------------------------|
| Weapon    | Skill synergy — flat damage bonus to skills of matching affinity | Tier → larger bonus                 |
| Armor     | Survival — HP, Speed, damage reduction (%) by category          | Tier → better stats within category |
| Accessory | Mitigation — physical resistance (%)                             | Tier → higher resistance            |
| Skill ×3  | Active/passive ability used during a run                         | Tier → stronger effect / lower cooldown |

#### Skill Slots

Three skill slots map directly to the 3 skill bar slots shown during a run. Whatever is equipped in skill slots 1–3 is what fires during the run.

The same skill item can be equipped in multiple slots simultaneously. Any archetype can equip any skill — there are no restrictions. v1 skills: **Strike** (`Melee`, `Attack`), **Arrow** (`Ranged`, `Attack`), **Bolt** (`Ranged`, `Magic`, `Spell`).

Skill items are crafted (see Skill Crafting tab) and equipped from the **Skills inventory tab**.

#### Supports

> **PoE2 inspiration:** Supports are the equivalent of PoE2 support gems — they socket directly into a skill and modify how it behaves. Tag compatibility is the only gate; there are no character or archetype restrictions.

Supports are craftable items that socket into a skill item to modify it. A support can only socket into a skill if the skill has at least one matching tag.

**Support slots per tier** — upgrading a skill unlocks deeper modification, not just bigger numbers:

| Skill tier | Support slots |
|------------|--------------|
| Common     | 1            |
| Uncommon   | 2            |
| Rare       | 3            |

- **Socketing:** choose a compatible support from inventory and place it into an open support slot on the skill item
- **Removing:** free, support returns to inventory
- **Compatibility:** governed by tags — a support declares which tags it requires; the skill must have at least one

**v1 supports:**

| Support | Requires tag | Effect |
|---------|-------------|--------|
| Splash  | `Melee`     | Hit damages a small area around the target |
| Pierce  | `Ranged`    | Projectile passes through enemies |
| Slow    | `Attack`    | Applies the Slow EoT on hit (see Effects over Time) |

Exact values (splash radius, slow %, apply chance, duration) are TBD.

**Crafting cost (v1):** every support costs **1 Common material** to craft.

Support items are crafted from the **Skill Crafting tab** and live in the **Augments inventory tab**.

#### Weapon

Weapons have an **affinity** tied to one or more skill tags. Equipping a weapon gives a flat damage bonus to all skills that share at least one matching tag. Weapons contribute no base damage — they enhance skills only.

| Weapon type | Affinity tag | Enhances                  |
|-------------|--------------|---------------------------|
| Sword       | `Melee`      | Skills with Melee tag     |
| Bow         | `Ranged`     | Skills with Ranged tag    |
| Wand        | `Magic`      | Skills with Magic tag     |

Any character can equip any weapon. The affinity bonus incentivises pairing weapon with matching skills — but mixing is valid. A skill with multiple tags (e.g. `Ranged`, `Magic`) benefits from any weapon whose affinity tag it shares.

#### Armor

Armor has a **category** that defines its identity. Category is fixed per item — crafting a higher-tier heavy armor makes it stronger within that category, not a different category.

| Category | HP       | Speed   | Damage Reduction |
|----------|----------|---------|------------------|
| Heavy    | High     | Penalty | Yes (%)          |
| Medium   | Moderate | Neutral | —                |
| Light    | Low      | Bonus   | —                |

Any character can equip any armor. Heavy suits close-range builds taking hits; light suits ranged builds that kite; medium suits mixed or flexible builds.

#### Accessory

Accessories grant **physical resistance (%)**. No category — any character can equip any accessory. Tier is the only progression axis: higher-tier accessories give higher resistance. A tier 1 accessory gives low physical resistance.

#### Starter Gear

Each character starts with one item per slot, matched to their archetype:

| Archetype | Weapon          | Armor                | Accessory          | Skill slots (all 3) |
|-----------|-----------------|----------------------|--------------------|---------------------|
| Warrior   | Sword (tier 1)  | Heavy armor (tier 1) | Accessory (tier 1) | Strike ×3 |
| Rogue     | Bow (tier 1)    | Light armor (tier 1) | Accessory (tier 1) | Arrow ×3  |
| Mage      | Wand (tier 1)   | Medium armor (tier 1)| Accessory (tier 1) | Bolt ×3   |

These are default starter loadouts only — any archetype can equip any skill. Skills are pre-equipped in all 3 slots and do not appear in the Skills inventory tab.

Specific item names and exact stat values are TBD.

**Acquisition:** Gear is not dropped by enemies. New items come from crafting — each item has a recipe requiring a combination of materials (see Currencies).

**Item identity:** Each item is a unique instance with its own ID. Items **upgrade in-place** — tier increases on the existing item rather than producing a new one. The item's background colour updates to reflect its new tier (see Item Tiers).

**Inventory:** Crafted (unequipped) items go into the **account inventory** — a shared pool accessible by every character. The inventory has three tabs:

| Tab | Contents | Capacity |
|---|---|---|
| Equipment | Crafted gear (weapons, armor, accessories) | 50 items |
| Skills | Crafted skill items | 50 items |
| Augments | Crafted support items | 50 items |

Equipped items are held separately in the character's slots and do not count against inventory capacity. Each tab is visible on the Character Screen as a scrollable 5-column icon grid.

**Equipping:** Click an inventory item → popup → **Equip** to move it into its slot on the selected character (any currently equipped item swaps back to inventory). Click an occupied slot → popup → **Unequip** (returns item to inventory; blocked if inventory is full) or **Delete** (removes permanently). Empty slots open the item picker filtered to that slot type.

---

## Currencies

### Coins
Earned during runs (25% enemy drop). **Account-shared** — earned by any character, spendable by any. Spend mechanic TBD — coins accumulate but have no current use.

### Crafting Materials
Crafting materials are tiered — common through exotic. Each tier drops at a different rate during runs and enables crafting of items at the corresponding tier. **v1:** all items cost 1 Common material to craft. Future versions will use material combinations for higher-tier recipes.

| Tier    | Current name        | Drop rate | Enables                          |
|---------|---------------------|-----------|----------------------------------|
| Common  | crafting-currency-1 | 20%       | Low-tier items                   |
| [TBD]   | —                   | Rarer     | Mid-tier items                   |
| Exotic  | —                   | Very rare | Exotic / high-tier items         |

- All materials are **account-shared** — earned by any character, spendable by any character
- The more exotic the craftable item, the rarer its required materials
- Specific tiers, drop rates, and material combinations will be designed when crafting is fleshed out

---

## UI / HUD

- Health bar
- XP bar + current level
- Coin counter (this run)
- Elapsed time / countdown
- **Skill bar** — bottom-center of the HUD. 3 slots. Shows slotted skills with cooldown/toggle state:
  - Active skill on cooldown: slot is greyed out, fills from bottom as cooldown recovers
  - Active skill ready: slot fully lit
  - Passive skill: lit when toggled on, greyed when off
  - Empty slot: visually empty (no icon)
- [TBD] Minimap

### Menus
- **Main Menu** → title screen, Play button
- **Account Screen** → the account-level hub. Always the first screen after Main Menu. Contains the character roster (list characters, create new, delete). Designed to grow — future account-level info (account stats, global progress, etc.) will live alongside the roster. Selecting a character navigates to their Character Screen.
- **Character Screen** → full management hub for the selected character: inventory (left), character stats + gear + tabs (right), Start Run button
  - **Inventory** (left panel) — account-shared item pool, 5-column scrollable grid. Three tabs:
    - *Equipment tab* — crafted gear (weapon, armor, accessory), 50-item cap
    - *Skills tab* — crafted skill items, 50-item cap
    - *Augments tab* — crafted support items, 50-item cap
    - Clicking a filled slot opens a popup (Equip / Delete). Equipped items are not shown here — they live in the slots.
  - **Equipment tab** *(default)* — gear slot buttons (Weapon / Armor / Accessory) and skill slot buttons (Skill 1 / Skill 2 / Skill 3) showing equipped items. Clicking an occupied slot: popup (Unequip / Delete). Clicking an empty slot: item picker filtered to that slot type.
  - **Crafting tab** — two sub-tabs:
    - *Create* — craft new gear items from materials
    - *Modify* — load an existing gear item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material)
  - **Skill Crafting tab** — two sub-tabs:
    - *Create* — craft new skill items and support items from materials (costs 1 Common material each)
    - *Modify* — load an existing skill item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material). Socketing a support into a skill uses the same interaction as socketing a skill into a skill slot — click an open support slot on the skill item, pick a compatible support from the Augments inventory.
  - **Sigils tab** — visible, empty (reserved for future sigil system)
  - All five tabs are always visible; empty tabs are not locked or greyed out
  - Back button returns to Account Screen
- **Run results overlay** → shown at run end; return button goes back to Character Screen
- **Pause menu** — ESC during a run; second ESC or Resume button closes it; run is paused while open
  - **Resume** button — closes menu, run continues
  - **End Run** button — exits immediately to character screen; all progress from this run is discarded (level, XP, coins, crafting materials). Warning text alongside: *"All progress from this run will be lost."*
  - No confirmation step — warning text is the friction

---

## Win / Lose Conditions

| Condition     | Outcome                                                        |
|---------------|----------------------------------------------------------------|
| Player HP = 0 | Run lost — level, XP, coins, and crafting materials earned still saved |
| Timer expires | Run won — all rewards saved; [boss mechanic TBD]              |

In both cases the player is returned to the character screen. There is no death penalty — every run makes the character stronger regardless of outcome.

---

## Future Design Notes

### Archetype Defense System

Each archetype should have a fundamentally different defensive philosophy — not just different numbers on the same stat, but a different *approach* to surviving:

| Archetype | Defense type | Philosophy |
|-----------|-------------|------------|
| Warrior | Physical Resistance | Mitigation — get hit, absorb it. Rewards staying in melee range. |
| Rogue | Dodge | Avoidance — don't get hit at all. Rewards speed investment and positioning. |
| Mage | Focus Shield | Resource management — Focus absorbs damage before HP. Depleted by casting. Rewards Focus discipline. |

**Focus** is the universal skill resource — all archetypes spend it to fire skills, but each archetype interacts with it differently:

| Archetype | Focus interaction |
|-----------|-----------------|
| Warrior | Spends Focus on skills; non-magic skills cost relatively little |
| Rogue | Spends Focus on skills; agile skills moderately refill it on use |
| Mage | Spends Focus on high-cost magic skills; Focus also acts as a damage buffer (Focus Shield) before HP is hit |

The Mage tension: blasting at full rate depletes Focus quickly, leaving the shield empty. Pacing preserves the buffer. This is the Mage's core risk/reward loop.

**Dodge** for the Rogue pairs naturally with their speed archetype multiplier — speed investment increases evasion, reinforcing the kiting playstyle without adding a separate stat.

The archetype multiplier system (see Characters) maps directly onto this: each archetype's designated defense type has a high multiplier, making cross-archetype defense investment possible but inefficient.

*Not scheduled for v1. Design this when manual skill activation and Focus are on the roadmap.*

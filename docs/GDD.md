# Game Design Document — godot1

> Living document — details will evolve as the game is playtested.

## Overview

A top-down horde survival game (Vampire Survivors / Diablo style). The player takes a persistent character into timed runs against escalating enemy waves. Skills fire automatically on cooldown — survival is about positioning and progression, not twitch reflexes.

Every run makes the character permanently stronger: level and XP carry over, stat bonuses stack, and coins and crafting materials earned go into a shared account pool. Between runs, players craft gear from materials — building both their character and their item collection over time. Coins accumulate but have no spend mechanic yet.

The game has two intertwined goals: grow your character through runs, and build your gear through crafting.

---

## Core Mechanics

### Movement
- Top-down, 8-directional
- [TBD] Speed, acceleration values

### Combat

Skills drive all combat. Each skill has a **type**:

| Skill type | Behaviour |
|---|---|
| Active | Fires automatically when its cooldown expires. No player input required during the run. |
| Passive | On/off toggle. Effect is always-on while enabled. |

The **skill bar** on the run HUD shows all slotted skills and their cooldown / toggle state.

**v1:** Three skill slots, each firing independently on its own cooldown. v1 has three skills (Strike, Arrow, Bolt — one per category). Starter characters have all 3 slots pre-filled with the same skill; players can mix freely. Cooldown: 0.8s per slot.

Every skill has a **category**: Melee, Ranged-Physical, or Ranged-Magic. Category determines what weapon affinity enhances it — see Gear Slots.

Character damage scales with character level (via level-up bonuses) and archetype base damage. Weapons do not contribute base damage — they provide a flat bonus to skills of matching category (see Gear Slots).

### Damage Types

Every damage source has a **damage type**. Every entity that can take damage has a **resistance** value per type (percentage reduction).

`effective damage = raw damage × (1 − resistance)`

**v1 damage types:** Physical, Magic

**Future expansion:** Elemental types (Fire, Lightning, Frost, etc.) will be added as the system grows — the formula and resistance model extend naturally.

Resistances are always soft (never total immunity). Exact values are TBD.

### Interaction
- Collectibles auto-collected on contact (XP gems, coins, health)
- [TBD] Any interactive objects (chests, shrines, etc.)

---

## Characters

Every run requires a character. Characters are created by the player, persist between runs, and grow over time. A player may own multiple characters simultaneously and delete any they no longer want.

### Character Archetypes

| Archetype | Max HP | Speed | Base Damage | Damage Type | Default build               |
|-----------|--------|-------|-------------|-------------|-----------------------------|
| Warrior   | 150    | 170   | 20          | Physical    | Sword + Heavy armor — close-range brawler |
| Rogue     | 80     | 260   | 15          | Physical    | Bow + Light armor — fast, fragile kiter   |
| Mage      | 100    | 200   | 35          | Magic       | Wand + Medium armor — glass cannon        |

Stat values are TBD. Default build reflects starter gear — players are free to deviate.

### Character Lifecycle
1. **Create** — player picks a name (required) and archetype
2. **Select** — choose a character from the roster before a run
3. **Run** — character starts at their saved level; every new level gained during the run is permanent
4. **Grow** — level and XP carry over to the character; coins and crafting materials go to the shared account pool; permanent stat bonuses are applied automatically on level up
5. **Delete** — player can permanently remove a character (irreversible)

A run cannot start without a selected character.

### Controls
| Input | Action         |
|-------|----------------|
| WASD  | Move           |
| —     | Attack (auto)  |
| ESC   | Pause          |

---

## In-Run Progression

### Level Up
- Killing an enemy grants **`1 XP × map level`** instantly (kill reward, no pickup required)
- Killing enemies also drops **XP gems** — collecting them adds further XP
- Both sources fill the same XP bar
- On level up: automatic permanent bonuses are applied — **+5 Max Health, +1 Damage**
- Level and XP within the current level persist when the run ends; the character picks up exactly where they left off
- No popup or pause — levelling up is seamless

### Enemy Drops
| Drop               | Effect                                         | Drop chance |
|--------------------|------------------------------------------------|-------------|
| XP gem             | Feeds level-up bar                             | 100%        |
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
| Map Level   | Scales kill XP reward — killing an enemy grants `1 XP × map level` directly, on top of any XP gem the enemy drops |

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
Each level gained during a run permanently improves the character:

| Per level gained | Effect                  |
|------------------|-------------------------|
| +5 Max Health    | Permanent HP increase   |
| +1 Damage        | Permanent damage increase (applies to attack skill) |

These stack across all runs. A level-10 character has +45 HP and +9 damage above their archetype base.

### Item Tiers

All items — both equipment and skills — have a **tier** that represents quality and power level. Tier is shown as the background colour of the item icon everywhere it appears (inventory, slots, pickers).

| Tier     | Colour | Notes                        |
|----------|--------|------------------------------|
| Common   | Gray   | Starter / lowest power       |
| Uncommon | Green  | Mid tier                     |
| Rare     | Blue   | Highest tier (v1)            |

Exact stat differences per tier are TBD. Higher tier also unlocks deeper skill chains (see Skill Chains).

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

The same skill item can be equipped in multiple slots simultaneously. Any archetype can equip any skill — there are no archetype or weapon restrictions on skill slots. v1 skills: **Strike** (melee auto-attack), **Arrow** (ranged-physical auto-attack), **Bolt** (ranged-magic auto-attack).

Skill items are crafted (see Skill Crafting tab) and equipped from the **Skills inventory tab**.

#### Skill Augments

Each skill slot has one **augment slot**. Augments add an effect on top of the base skill — the same augment can be applied to any skill regardless of category.

- **Applying:** choose from the full augment list at the character screen; costs **1 Common material**; replaces any existing augment on that slot
- **Removing:** free, no material cost

Augments are not items — they are always available choices. The cost is the only gate.

**v1 augments:**

| Augment | Effect |
|---------|--------|
| Slow    | Reduces enemy movement speed on hit |

Slow percentage and duration are TBD. Burn and Pierce are deferred to a future version.

#### Skill Chains

Skills can be chained via **on-hit connections**. When the parent skill hits an enemy, it fires the chained skill. Any skill category can chain to any other — melee can chain to ranged, ranged to melee, etc.

**Chain depth** is determined by the **tier of the primary (slot-equipped) skill:**

| Skill tier | Max chain depth |
|------------|----------------|
| 1          | 1 (A → B)      |
| 2          | 2 (A → B → C)  |
| N          | N              |

**Adding a chain:** consuming an existing crafted skill item from inventory attaches it at the next chain position. The consumed item is **permanently spent** — it does not return to inventory if the chain is later removed.

**Removing a chain:** the chain slot can be cleared at any time at no cost, but the consumed skill is lost.

Each skill in a chain is itself a crafted skill item and carries its own augment. v1 has tier 1 skills only, so all chains are 1 deep in v1.

#### Weapon

Weapons have an **affinity** matching a skill category. Equipping a weapon gives a flat damage bonus to all skills of that category. Weapons contribute no base damage — they enhance skills only.

| Weapon type | Affinity        | Enhances               |
|-------------|-----------------|------------------------|
| Sword       | Melee           | Melee skills           |
| Bow         | Ranged-Physical | Ranged-Physical skills |
| Wand        | Ranged-Magic    | Ranged-Magic skills    |

Any character can equip any weapon. The affinity bonus incentivises pairing weapon with matching skills — but mixing is valid.

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

**Inventory:** Crafted (unequipped) items go into the **account inventory** — a shared pool accessible by every character. The inventory has two tabs:

| Tab | Contents | Capacity |
|---|---|---|
| Equipment | Crafted gear (weapons, armor, accessories) | 50 items |
| Skills | Crafted skill items | 50 items |

Equipped items are held separately in the character's slots and do not count against inventory capacity. Each tab is visible on the Character Screen as a scrollable 5-column icon grid.

**Equipping:** Click an inventory item → popup → **Equip** to move it into its slot on the selected character (any currently equipped item swaps back to inventory). Click an occupied slot → popup → **Unequip** (returns item to inventory; blocked if inventory is full) or **Delete** (removes permanently). Empty slots open the item picker filtered to that slot type.

---

## Currencies

### Coins
Earned during runs (25% enemy drop). **Account-shared** — earned by any character, spendable by any. Spend mechanic TBD — coins accumulate but have no current use.

### Crafting Materials
Crafting materials are tiered — common through exotic. Each tier drops at a different rate during runs and enables crafting of items at the corresponding tier. Items are crafted from **combinations** of materials, not a single currency spend.

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
  - **Inventory** (left panel) — account-shared item pool, 5-column scrollable grid. Two tabs:
    - *Equipment tab* — crafted gear (weapon, armor, accessory), 50-item cap
    - *Skills tab* — crafted skill items, 50-item cap
    - Clicking a filled slot opens a popup (Equip / Delete). Equipped items are not shown here — they live in the slots.
  - **Equipment tab** *(default)* — gear slot buttons (Weapon / Armor / Accessory) and skill slot buttons (Skill 1 / Skill 2 / Skill 3) showing equipped items. Clicking an occupied slot: popup (Unequip / Delete). Clicking an empty slot: item picker filtered to that slot type.
  - **Crafting tab** — two sub-tabs:
    - *Create* — craft new gear items from materials
    - *Modify* — load an existing gear item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material)
  - **Skill Crafting tab** — two sub-tabs:
    - *Create* — craft new skill items from materials
    - *Modify* — load an existing skill item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material); one **Augment** button to apply/change the augment (costs 1 Common material)
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

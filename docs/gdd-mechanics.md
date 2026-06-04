# Game Design Document — Mechanics & Characters

> Part of the GDD. See also `gdd-progression.md` for meta-progression, gear, crafting, and UI.
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

Every skill has one or more **tags** — descriptors that other systems react to. Tags are not restrictions — any character can equip any skill. Tags serve two distinct roles:

**Delivery tags** determine how a skill physically fires and which Weapon Range applies:
- `Melee` — skill fires as a melee contact attack using the equipped weapon
- `Ranged` — skill fires as a projectile using the equipped weapon asset (sword throw, arrow, wand throw, etc.) at the weapon's range

Skills with neither delivery tag play the weapon's default attack animation and activate exactly as their definition specifies — AoE lands at target, aura activates on self, etc.

**Descriptor tags** determine augment compatibility, damage type, and visual effects layered on top of the delivery. They do not affect animation or range: `Magic`, `Attack`, `Spell`.

**v1 tags:** `Melee`, `Ranged`, `Magic`, `Attack`, `Spell` (expand as more skills and Skill Augments are added).

Character damage scales with character level (via level-up bonuses) and archetype base damage. Weapons do not contribute base damage — they set Weapon Range and determine the visual delivery of skills (see Gear Slots).

### Damage Types

Every damage source has a **damage type**. Every entity that can take damage has a **resistance** value per type (percentage reduction).

`effective damage = raw damage × (1 − resistance)`

**v1 damage types:** Physical, Magic

**Future expansion:** Elemental types (Fire, Lightning, Frost, etc.) will be added as the system grows — the formula and resistance model extend naturally.

Resistances are always soft (never total immunity). Exact values are TBD.

### Effects over Time (EoT)

Skills and Skill Augments can apply **Effects over Time (EoT)** to enemies on hit. All EoTs follow the same rules regardless of what the specific effect does:

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

v1 Skill Augments only use Slow. All future effects follow this same framework.

### Interaction
- Collectibles auto-collected on contact (XP Shards, coins, health)
- [TBD] Any interactive objects (chests, shrines, etc.)

---

## Characters

Every run requires a character. Characters are created by the player, persist between runs, and grow over time. A player may own multiple characters simultaneously and delete any they no longer want.

### Character Archetypes

| Archetype | Max HP | Speed | Physical Damage | Magic Damage | Default build               |
|-----------|--------|-------|----------------|--------------|------------------------------|
| Warrior   | 150    | 170   | 20             | 0            | Sword + Heavy armour — close-range brawler |
| Rogue     | 80     | 260   | 15             | 0            | Bow + Light armour — fast, fragile kiter   |
| Mage      | 100    | 200   | 0              | 35           | Wand + Medium armour — glass cannon        |

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

| Type     | Behavior     | Physical Resist | Magic Resist | Notes                                      |
|----------|--------------|-----------------|--------------|--------------------------------------------|
| Skeleton | Chase player | 10%             | 0%           | v1 only enemy — bone-white voxel model     |
| [TBD]    | Chase fast   | —               | —            | Future runner-type                         |
| [TBD]    | Ranged       | —               | —            | Future ranged attacker                     |
| [TBD]    | Boss         | —               | —            | Spawns when timer expires                  |

All types scale with elapsed time — speed and HP increase per minute. Spawn rate also accelerates.

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

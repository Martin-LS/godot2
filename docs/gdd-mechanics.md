# Game Design Document — Mechanics & Characters

> Part of the GDD. See also `gdd-progression.md` for meta-progression, gear, crafting, and UI.
> Living document — details will evolve as the game is playtested.

## Overview

A top-down action RPG with horde combat (Diablo / Path of Exile 2 style). The player builds a persistent character, equips gear and skills, and takes them into timed combat runs against escalating enemy waves. Combat is skill-driven — each skill is manually activated by the player. Every skill **slot** has an **auto-activate toggle**: when enabled, that slot fires automatically on cooldown without input, allowing a more passive playstyle or hands-free filler slots while the player focuses on timing key abilities.

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
| Active | Fires when the player manually activates it (keys 1 / 2 / 3). Each **skill slot** has an **auto-activate toggle** — when on, that slot fires automatically on cooldown without manual input. Auto-activate is a convenience option, not a power reduction. |
| Passive | On/off toggle. Effect is always-on while enabled. |

The **skill bar** on the run HUD shows all slotted skills, their cooldown state, and whether auto-activate is enabled per slot.

**v1:** Three skill slots (keys 1 / 2 / 3), each with an independent cooldown. Slots can be empty — an empty slot does nothing. New characters start with **1 skill slotted** (slot 1); slots 2 and 3 are empty. Players fill them by crafting additional skill items. This makes the first craft feel meaningful — it literally opens a new slot. Cooldown: 0.8s per slot (tier 1).

**Attack / cast speed is a skill attribute, not a character stat.** There is no global attack speed multiplier on the character or on gear. A skill's cooldown belongs to the skill item — it is tuned per skill, reduced by tier upgrades, and can be modified by future Skill Augments (e.g. a Haste support). This keeps the PoE2 philosophy intact: skills are self-contained items with their own tempo, not extensions of a character stat. A Warrior's Strike and a Mage's Bolt have independent rhythms that do not interact.

Every skill has one or more **tags** — descriptors that other systems react to. Tags are not restrictions — any character can equip any skill. Tags serve two distinct roles:

**Delivery tags** determine how a skill physically fires and which Weapon Range applies:
- `Melee` — skill fires as a melee contact attack using the equipped weapon
- `Ranged` — skill fires as a projectile using the equipped weapon asset (sword throw, arrow, wand throw, etc.) at the weapon's range

Skills with no delivery tag are **weapon-adaptive**: they inherit the weapon's `PreferredDelivery` at run start. The same skill feels like a sword swing on a Warrior and an arrow on a Rogue. See Gear Slots in `gdd-progression.md` for how each weapon defines its `PreferredDelivery`.

**Descriptor tags** determine augment compatibility, damage type, and visual effects layered on top of the delivery. They do not affect animation or range: `Magic`, `Attack`, `Spell`.

**v1 tags:** `Melee`, `Attack` (expand as more skills and Skill Augments are added).

### Skills

**v1 has one skill: Strike.** All archetypes start with plain Strike in slot 1, no augments pre-socketed. Damage type and delivery are determined by the equipped weapon — a Mage's Strike fires as a Magic wand bolt; a Rogue's fires as a Physical arrow with crit (from the bow's identity bonus). This keeps the skill list minimal while letting the weapon communicate archetype identity.

#### Strike

The universal skill. Hits the nearest enemy using whatever the character has equipped — a sword swing, an arrow, a wand bolt. Rogue and Mage starters are Strike with an augment pre-socketed (see Starter Gear). As players acquire new skills, Strike slots get replaced. Strike can still be kept in any slot intentionally.

| Property | Value |
|---|---|
| Delivery | Weapon-adaptive — no delivery tag; inherits weapon's `PreferredDelivery` |
| Descriptor tags | `Attack` |
| Cooldown | 0.8s (tier 1) — lower at higher tiers |
| Damage | 1× weapon base damage |
| EoTs | None |
| Splash | No |
| Acquire | Free — slot 1 pre-filled at character creation; slots 2 and 3 start empty |

**Weapon is the root of all damage.** Each weapon has a base damage value that increases with tier. Skill damage is calculated as:

`Skill damage = Weapon base damage × Archetype damage multiplier × (1 + level damage bonus%)`

Archetype damage multipliers define how effectively each archetype converts weapon damage into output — a Warrior extracts more physical damage from a Sword than a Mage would. Level-up grants a small cumulative % damage bonus that amplifies the whole formula.

**Level damage bonus: +2% per level (cumulative).** At level 10 = +20% total. All values below are placeholder — owned by the Balancer.

### Damage Types

Every damage source has a **damage type**. Every entity that can take damage has a **resistance** value per type (percentage reduction).

`effective damage = raw damage × (1 − resistance)`

**v1 damage types:** Physical, Magic

**Future expansion:** Elemental types (Fire, Lightning, Frost, etc.) will be added as the system grows — the formula and resistance model extend naturally. Getting Magic right in v1 is the template: a new damage type means adding a resistance value per enemy, a DamageType enum entry, and a weapon or augment that produces it. Nothing else changes.

Resistances are always soft (never total immunity). Exact values are TBD.

### Critical Hits

Critical hits are **damage-type agnostic** — a crit multiplies the final damage output regardless of whether the hit is Physical, Magic, or any future damage type. This is consistent with the ARPG genre standard and means new damage types require no changes to the crit system.

**Two distinct stats govern crits:**

| Stat | Internal name | Player-facing name | What it does |
|---|---|---|---|
| Crit Chance | Crit Chance | Crit Chance | % probability that a hit is a critical hit |
| Crit Multiplier | Crit Multiplier | Crit Damage | Total multiplier applied to damage on a crit (e.g. 1.5× = +50% Crit Damage) |

> **Naming convention:** "Crit Multiplier" is used in the GDD and in code — it is unambiguous (1.5 means 1.5×). "Crit Damage" is the player-facing UI label, expressed as a bonus (+50% Crit Damage). Both refer to the same stat.

These are independent investment axes. High Crit Chance with low Crit Multiplier = consistent small bonus. Low Crit Chance with high Crit Multiplier = rare but large spikes. Committing to both is what makes a dedicated crit build.

**Crit is a More multiplier** in the augment math — applied after all other calculations (weapon base × archetype mult × level bonus × augment buckets), and does not interact with the Increased% bucket.

`Final damage (on crit) = Skill damage × Crit Multiplier`

**Crit Chance and Crit Multiplier are universal stats — no source gate.**
Any item type can contribute to either pool: weapon identity, skill augment, equipment augment, ring, or any future gear stat. All contributions stack additively into their respective totals before the damage roll. There are no restrictions on which slot type may provide crit.

**v1 scope:**
- **Crit Chance:** investable — base is 0; raised by Bow identity (+8% flat) and/or Critical Strike Skill Augment; both stack additively into the same pool
- **Crit Multiplier:** fixed at 1.5× (= +50% Crit Damage); not investable in v1

**Future:** Crit Multiplier as an investable stat — e.g. a "Brutal Strike" augment that raises the multiplier — is a deliberate post-v1 hook. At that point players have both axes to work with and can feel the tension between stacking Chance vs. Multiplier.

**EoT crit stamping:**
When a hit that applies a damage EoT is a critical hit, the **entire EoT instance is stamped with the Crit Multiplier** — it deals crit-multiplied damage per tick for its full duration. Re-applying the EoT with a non-crit hit resets it to base damage; re-applying with a crit refreshes it at crit-multiplied damage. Non-damage EoTs (Slow, Vulnerability) are unaffected by crit — only EoTs with a damage-per-tick value can be stamped.

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
- **Damage EoTs only:** if the applying hit was a critical hit, the EoT instance is stamped with the crit multiplier for its full duration (see Critical Hits)

The EoT type defines *what it does* when active:

| EoT | Damage per tick? | Crit-stampable? | What it does |
|---|---|---|---|
| Slow | No | No | Reduces enemy movement speed |
| Burn | Yes (Magic) | Yes | Deals Magic damage per tick; stamped if applied by a crit |
| Vulnerability | No | No | Increases damage taken by the enemy |

When designing new EoTs: if it deals damage per tick, set tick rate, damage per tick, and mark it crit-stampable. If not, leave those blank. That is the only distinction.

v1 Skill Augments only use Slow. All future effects follow this same framework.

### Interaction
- Collectibles auto-collected on contact (XP Shards, coins, health)
- [TBD] Any interactive objects (chests, shrines, etc.)

---

## Characters

Every run requires a character. Characters are created by the player, persist between runs, and grow over time. A player may own multiple characters simultaneously and delete any they no longer want.

### Character Archetypes

| Archetype | Max HP | Speed | Phys Damage Multiplier | Magic Damage Multiplier | Default build               |
|-----------|--------|-------|------------------------|-------------------------|------------------------------|
| Warrior   | 150    | 170   | 1.5×                   | 0.5×                    | Sword + Heavy armour — close-range brawler |
| Rogue     | 80     | 260   | 1.0×                   | 0.5×                    | Bow + Light armour — fast, fragile kiter   |
| Mage      | 100    | 200   | 0.5×                   | 1.5×                    | Wand + Medium armour — glass cannon        |

Damage multipliers apply to weapon base damage (see Damage formula under Skills). Mismatched builds (e.g. Warrior + Wand) produce ~half output — viable but suboptimal. All values are placeholder — owned by the Balancer.

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
| 1       | Activate skill slot 1                       |
| 2       | Activate skill slot 2                       |
| 3       | Activate skill slot 3                       |
| ESC     | Pause                                       |

Each skill slot has an **auto-activate toggle** (set in the character screen before the run). When auto-cast is on for a slot, it fires automatically on cooldown — the key still works as a manual override to fire early if the skill is ready.

---

## In-Run Progression

### Level Up
- Killing an enemy grants **`1 XP × map level`** instantly (kill reward, no pickup required)
- Killing enemies also drops **XP Shards** — collecting them adds further XP
- Both sources fill the same XP bar
- On level up: permanent HP bonus (flat) and a small cumulative % damage bonus are applied automatically; the damage bonus amplifies weapon base damage through the archetype multiplier formula
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

*Not scheduled for v1. Design this when Focus is on the roadmap.*

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
- **v1:** WASD only — no movement skills, no dash
- **v2:** Every character gets a dedicated non-slottable dash. Not in a skill slot — all characters have it by default. Ensures players always have one escape option without sacrificing a skill slot. Enables more aggressive encounter and skill design knowing the movement floor is guaranteed.

### Combat

> **Design inspiration:** The skill system takes Path of Exile 2 as its north star — skills as socketable items, supports that modify individual skills, and trigger/chain mechanics. Our specific skills and numbers will differ, but the philosophy (deep, legible skill modification through a gem/support layer) guides all skill design decisions.

Skills drive all combat. Each skill has a **type**:

| Skill type | Behaviour | Focus cost |
|---|---|---|
| Active | Fires on manual activation or auto-activate on cooldown. | Flat cost per activation |
| Channeled | Hold button to run, release to stop. Drains Focus continuously while held. Stops automatically at 0 Focus. Auto-cast holds the button indefinitely — will empty the Focus bar if left unchecked. Player responsibility. | Per-second drain while held |
| Aura | Toggle on/off. Effect is always-on while enabled. Permanently reserves a % of Max Focus — shrinks the available pool while active. | Reserves % of Max Focus |
| Passive | Toggle on/off. Stat or effect always-on while enabled. | None |

The **skill bar** on the run HUD shows all slotted skills, their cooldown state, and whether auto-activate is enabled per slot.

**Auto-activate design philosophy.** Some builds naturally devolve into holding all buttons simultaneously — if that is the optimal play, the game should respect the player enough to automate it and redirect their attention to something more interesting. Auto-activate is the honest version of "hold all buttons." It is not easy mode; it is the logical endpoint of certain build types. Movement is where the player's active attention lives when skills are automated — positioning, dodging, kiting, managing distance. Player errors (e.g. auto-casting a Channeled skill that drains the entire Focus bar) are lessons, not design problems to prevent.

**Auto-activate must be DPS-equivalent to manual.** A player manually pressing all skill keys on cooldown should get the same damage output as the same skills on auto-activate. Auto-activate trades attention (you stop managing the keys) for nothing else — it is pure convenience, not a power reduction. Any system that introduces artificial delays between auto-fires (e.g. a round-robin cycling delay) violates this and should not be implemented.

**v1:** Three skill slots (keys 1 / 2 / 3). Slots can be empty — an empty slot does nothing. New characters start with **1 skill slotted** (slot 1); slots 2 and 3 are empty. Players fill them by crafting additional skill items. This makes the first craft feel meaningful — it literally opens a new slot. Each skill has its own cooldown or drain rate — there is no shared slot cooldown.

**Attack / cast speed is a skill attribute, not a character stat.** There is no global attack speed multiplier on the character or on gear. A skill's cooldown belongs to the skill item — it is tuned per skill, reduced by tier upgrades, and can be modified by future Skill Augments (e.g. a Haste support). This keeps the PoE2 philosophy intact: skills are self-contained items with their own tempo, not extensions of a character stat. A Warrior's Strike and a Mage's Bolt have independent rhythms that do not interact.

Every skill has one or more **tags** — descriptors that other systems react to. Tags are not restrictions — any character can equip any skill. Tags serve two distinct roles:

**Delivery tags** determine how a skill physically fires and which Weapon Range applies:
- `Melee` — skill fires as a melee contact attack using the equipped weapon
- `Ranged` — skill fires as a projectile using the equipped weapon asset (sword throw, arrow, wand throw, etc.) at the weapon's range

Skills with no delivery tag are **weapon-adaptive**: they inherit the weapon's `PreferredDelivery` at run start. The same skill feels like a sword swing on a Warrior and an arrow on a Rogue. See Gear Slots in `gdd-progression.md` for how each weapon defines its `PreferredDelivery`.

**Descriptor tags** determine augment compatibility, damage type, and visual effects layered on top of the delivery. They do not affect animation or range: `Magic`, `Attack`, `Spell`.

**v1 tags:** `Melee`, `Attack`, `Aura` (expand as more skills and Skill Augments are added).

### Combat Facing

While **not attacking**, the character faces their movement direction. While **attacking** (OneShot animation active), the character always rotates to face the nearest enemy — even if the player is moving in the opposite direction. This ensures attacks always connect visually and lets players kite while staying engaged with targets behind them.

### Weapon Animations & Handedness

Weapons are held in different hands depending on type, which drives which animation plays for melee attacks.

| Weapon type | Hold hand (visual) | Skeleton bone | Melee animation |
|---|---|---|---|
| Sword, Axe, Club, Dagger | Right hand | `Hand_L` | `melee_right_atack` |
| Bow | Left hand | `Hand_R` | `melee_left_atack` |
| Wand | Right hand | `Hand_L` | `melee_right_atack` |

- **`melee_right_atack`** — right-arm swing/slash; used by all right-hand weapons
- **`melee_left_atack`** — left-arm horizontal sweep or butt-strike; used when a bow is equipped and a melee skill fires

The weapon's `AttachBone` property (future field on `WeaponData`) drives which bone the mesh attaches to at runtime. The `OnSkillFired` handler selects the animation based on the equipped weapon's hold hand, not the skill's delivery tag alone.

Idle and run animations are shared across all weapon types in v1.

**Attack animation speed syncs to cooldown.** The animation playback speed is set dynamically at fire time so the clip completes in exactly one cooldown window (`scale = animLength / cooldown`). Damage lands at 35% through the cooldown (the wind-up frame) rather than instantly. As attack speed increases (shorter cooldown), the animation visibly speeds up — the same feel as Diablo's attack speed scaling.

### Hit Feedback

**Design reference: Diablo 4.** No character animation or action interrupt on hit — the player stays in full control through all damage. There is no stagger system, no hit-recovery stat, no flinch animation. Defensive build variance comes entirely from Equipment Augments (see `gdd-progression.md`).

Danger is communicated through health/shield depletion, not through action interruption. This keeps horde combat fluid regardless of difficulty.

#### HP Bars

Both the player and enemies display a floating HP bar above their head.

| Entity | Visibility | Fill colour | Hex |
|---|---|---|---|
| Player | Always visible | Danger Red | `#A32D2D` |
| Enemy | Appears on hit; fades after ~2s of no damage | Muted Red | `#8C2E2E` |

Both bars use Iron Black (`#181C1F`) as the track background. Bar width scales proportionally to entity size. The player's floating bar coexists with the HUD health bar — both are always present during a run.

Enemy bars are on-hit only to preserve readability during large hordes: bars only appear where hits are landing, keeping the screen uncluttered at peak density.

#### Damage Numbers

Every hit displays a floating damage number above the struck entity's head. Numbers float upward and fade out over ~0.8s.

| Case | Colour | Hex | Size |
|---|---|---|---|
| Physical hit | Bone White | `#E8DCC8` | Normal |
| Magic hit | Ice Shimmer | `#B8D8E8` | Normal |
| Critical hit | Gold | `#D4A017` | ~50% larger |

Critical hit colour overrides the damage-type colour — a magic crit shows gold, not blue. This makes crits immediately legible regardless of damage type.

Numbers are individual per hit — no stacking. Cyclone ticks each pop their own number; this preserves tick-rate readability and lets the player feel the difference between a fast and slow attack speed.

Both player-received and enemy-received hits produce damage numbers. There is no threshold — all damage shows.

### Skills

**v1 skills: Strike, Cyclone, Damage Aura, Nova.** Four skills covering the full test matrix — Active single-target (Strike), Channeled multi-target (Cyclone), Aura persistent (Damage Aura), Active multi-target (Nova). All archetypes start with plain Strike in slot 1, no augments pre-socketed. Damage type is determined by the equipped weapon across all skills.

#### Strike

The universal skill. Hits the nearest enemy using whatever the character has equipped — a sword swing, an arrow, a wand bolt. All archetypes start with plain Strike, no augments pre-socketed. As players acquire new skills, Strike slots get replaced. Strike can still be kept in any slot intentionally.

| Property | Value |
|---|---|
| Delivery | Weapon-adaptive — no delivery tag; inherits weapon's `PreferredDelivery` |
| Descriptor tags | `Attack` |
| Cooldown | 0.8s (tier 1) — lower at higher tiers |
| Damage | 1× weapon base damage |
| EoTs | None |
| Splash | No |
| Acquire | Free — slot 1 pre-filled at character creation; slots 2 and 3 start empty |

#### Cyclone

Spin continuously in place, hitting all enemies within melee range on each tick. A Channeled skill — hold to spin, release to stop. Drains Focus while held, stops automatically at 0 Focus. Lower damage per hit than Strike; the value is continuous multi-target coverage.

| Property | Value |
|---|---|
| Type | Channeled |
| Delivery | Melee |
| Descriptor tags | `Attack` |
| Focus cost | 12 Focus/sec drain |
| Damage per hit | 0.4× weapon base damage (placeholder) |
| Tick rate | 4 hits/sec |
| Acquire | Craft |

#### Damage Aura

Pulses damage to all nearby enemies once per second while active. An Aura skill — reserves a portion of Max Focus permanently while toggled on, shrinking the pool available for other skills. Simple and testable: verifies the Aura type, Focus reservation, and area damage pulse.

| Property | Value |
|---|---|
| Type | Aura |
| Delivery | None — ambient area pulse, not weapon-based |
| Descriptor tags | `Aura` |
| Focus reservation | 25% of Max Focus |
| Damage per pulse | 0.2× weapon base damage (placeholder) |
| Pulse rate | 1/sec |
| Range | Short radius around player |
| Acquire | Craft |

#### Nova

An instant explosion centered on the player — hits all enemies within a medium radius simultaneously, then enters cooldown. The Active multi-target skill. Panic button feel — surrounded, pop it, create space.

| Property | Value |
|---|---|
| Type | Active |
| Delivery | None — centered radius burst, not weapon-based delivery |
| Descriptor tags | `Attack` |
| Focus cost | 20 Focus (flat) |
| Damage | 0.8× weapon base damage |
| Cooldown | 1.5s |
| Radius | Medium (larger than melee range) |
| Acquire | Craft |

---

**Weapon is the root of all damage.** Each weapon has a base damage value that increases with tier. Skill damage is calculated as:

`Skill damage = Weapon base damage × Archetype damage multiplier × (1 + level damage bonus%)`

Archetype damage multipliers define how effectively each archetype converts weapon damage into output — a Warrior extracts more physical damage from a Sword than a Mage would. Level-up grants a small cumulative % damage bonus that amplifies the whole formula.

**Level damage bonus: +2% per level (cumulative).** At level 10 = +20% total. All values below are placeholder — owned by the Balancer.

### Focus

Focus is the universal skill resource. All archetypes spend Focus to fire skills; skills cannot activate when Focus is empty.

**Regeneration:** Passive regen over time at a steady rate. Always recovering — no kill-based acceleration.

**At 0 Focus:** Skills cannot fire. Cooldowns still count down; auto-activate waits until enough Focus regens to cover the cost. Channeled skills stop automatically when Focus hits 0.

**Channeled skill tag:** Skills tagged `Channeled` drain Focus continuously while the button is held. Releasing the button stops the skill immediately. If Focus hits 0 the skill stops automatically regardless of input. Auto-cast holds the button indefinitely — player responsibility to not auto-cast a skill that drains their entire Focus bar.

**Skill costs (placeholder — owned by Balancer):**

| Skill | Cost |
|---|---|
| Strike | 5 Focus (flat) — effectively free; regens faster than you spend |
| Nova | 20 Focus (flat) — meaningful burst cost |
| Cyclone | 12 Focus/sec (drain while held) — expensive over time, requires management |
| Damage Aura | Reserves 25% of Max Focus — shrinks available pool while active |

**Starting values (placeholder — owned by Balancer):**

| Archetype | Max Focus | Regen/sec |
|---|---|---|
| Warrior | 80 | 12 |
| Rogue | 100 | 15 |
| Mage | 150 | 10 |

**Focus Shield**
Every archetype has a Focus Shield — a damage buffer that absorbs hits before HP. Once depleted, damage hits HP directly.

- Shield size = 30% of current Max Focus (all archetypes)
- Casting does not drain the shield — Focus pool and shield are managed independently
- Shield regens passively (slow baseline, investable through augments)
- Investable augment paths: shield regen rate (time-based recovery; rewards brief retreats) and shield on attack (hit-based recovery; rewards aggressive combat)
- If Max Focus increases (buff): shield ceiling rises, current shield stays — regen up to the new cap
- If Max Focus decreases (debuff): shield ceiling drops, current shield is immediately clamped to the new maximum

Investing in Max Focus through gear grows both the casting pool and the shield simultaneously. Natural shield sizes at base: Warrior 24, Rogue 30, Mage 45. The Mage has the largest Focus pool — and therefore the largest shield — by default.

---

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

| Archetype | Max HP | Speed | Phys Damage Multiplier | Magic Damage Multiplier | Max Focus | Focus Regen/sec | Default build |
|-----------|--------|-------|------------------------|-------------------------|-----------|-----------------|---------------|
| Warrior   | 150    | 170   | 1.5×                   | 0.5×                    | 80        | 12              | Sword + Heavy armour — close-range brawler |
| Rogue     | 80     | 260   | 1.0×                   | 0.5×                    | 100       | 15              | Bow + Light armour — fast, fragile kiter   |
| Mage      | 100    | 200   | 0.5×                   | 1.5×                    | 150       | 10              | Wand + Medium armour — glass cannon; largest Focus Shield by default |

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

All archetypes share Focus Shield as a universal defensive layer — damage hits the shield before HP. Natural shield size varies by archetype because it scales with Max Focus. Beyond the universal shield, each archetype has a primary defensive identity:

| Archetype | Primary defense | Focus Shield at base | Philosophy |
|-----------|----------------|----------------------|------------|
| Warrior | Physical Resistance | 24 (80 × 30%) | Mitigation — physical resistance reduces raw damage; shield buys extra time. Rewards staying in melee range. |
| Rogue | Dodge | 30 (100 × 30%) | Avoidance — speed investment increases evasion; shield is a fallback when avoidance fails. |
| Mage | Focus Shield | 45 (150 × 30%) | Resource management — Focus Shield is the primary defense. Investing in Max Focus grows both the casting pool and the shield simultaneously. |

Focus Shield is investable by any archetype through Equipment Augments (shield regen rate, shield on attack). This enables cross-archetype builds — a Warrior who invests in Max Focus and shield augments plays as a melee fighter with a magical damage buffer (Paladin-style), constrained naturally by their weapon (short range, physical damage primary).

**Physical Resistance (Warrior) and Dodge (Rogue) as investable stats are post-v1.** Design when the archetype multiplier system is being expanded.

**Focus Shield is v1 for all archetypes** — see Focus section under Core Mechanics.

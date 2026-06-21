# Game Design Document — Mechanics & Characters

> Part of the GDD. See also `gdd-progression.md` for meta-progression, gear, crafting, and UI.
> Living document — details will evolve as the game is playtested.

## Overview

> **Design pivot in progress.** The game is shifting away from a deliberate action RPG direction (Diablo / PoE2 style) toward a horde survival game with action RPG depth — closer to Vampire Survivors in moment-to-moment feel, with meaningful skill and item systems layered on top. Specific mechanics below are being reviewed and revised. Sections that have not yet been updated may still reflect the old direction.

A top-down horde survival game with action RPG elements. The player builds a persistent character, equips gear and a skill, and takes them into timed combat runs against escalating enemy waves. The core loop is fast, automatic, and horde-focused — the ARPG layer adds depth through the skill and item systems.

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

Skills drive all combat. Each skill has a **type**:

| Skill type | Behaviour | Focus cost |
|---|---|---|
| Active | Fires on manual activation or auto-activate on cooldown. | Flat cost per activation |
| Channeled | Hold button to run, release to stop. Drains Focus continuously while held. Stops automatically at 0 Focus. Auto-cast holds the button indefinitely — will empty the Focus bar if left unchecked. Player responsibility. | Per-second drain while held |
| Passive | Toggle on/off. Stat or effect always-on while enabled. | None |

The **skill bar** on the run HUD shows the slotted skill, its cooldown state, and whether auto-activate is enabled.

**Auto-activate.** The player can toggle their skill to fire automatically on cooldown. When enabled, movement is where the player's active attention lives — positioning, dodging, kiting. Auto-activate must be DPS-equivalent to manual: a player pressing the skill key manually on cooldown gets the same output as auto-activate. It is pure convenience, not a power reduction.

**v1:** 1 skill slot. The slot can be empty — an empty slot does nothing. Code supports multiple slots for future expansion but the HUD and design show 1. Each skill has its own cooldown or drain rate.

**Attack / cast speed is a skill attribute, not a character stat.** There is no global attack speed multiplier on the character or on gear. A skill's cooldown belongs to the skill item — it is tuned per skill and reduced by tier upgrades.

**Damage model.** The weapon provides the base damage number. The skill defines the damage type and a damage multiplier. Delivery (how the attack animates) is always driven by the equipped weapon — a Sword always swings, a Bow always shoots, a Wand always fires a bolt — regardless of which skill is equipped.

`Skill damage = Weapon base damage × Skill damage multiplier × Archetype damage multiplier (for skill's type) × (1 + level damage bonus%)`

Archetype damage multipliers apply per damage type — a Warrior gets 1.5× on physical skills and 0.5× on magic skills. Mismatched builds (e.g. Warrior equipping a magic-type skill) are viable but produce reduced output.

### Targeting

**Entity skills** fire at the **locked target** — a single enemy that has a persistent target marker on them. **Self skills** ignore the lock entirely and always fire from the player. The targeting system is always active; players on keyboard experience it as "skills just work." Controller players can redirect the lock with the right stick.

**How the lock works:**

1. **Auto-pick** — at run start, when no lock exists, or when the current target dies, the game silently picks the nearest enemy in the player's facing or movement direction.
2. **Soft lock** — the marker persists on that enemy until it dies. Skills fire at the locked target regardless of player facing or movement direction.
3. **Right stick override (controller)** — pushing the right stick sweeps the target marker through nearby enemies in that direction. Aim assist magnetises to the nearest enemy in the pushed direction. Releasing the stick holds the last selected target.
4. **Skill targeting shape** — each skill declares one of three targeting shapes (see below); the targeting system resolves the correct input per shape and per input device automatically.
5. **Character facing** — the character faces the locked target during skill casts, decoupled from movement direction (see Combat Facing).

**Priority rules:** nearest enemy wins. If multiple enemies are equidistant, the one closest to the player's current facing direction is preferred.

**Keyboard behaviour:** steps 1–2 handle everything automatically. No manual targeting input exists or is needed — the system is invisible.

**Controller behaviour:** same auto-pick foundation; right stick adds voluntary override without changing anything else.

#### Skill Targeting Shapes

Every skill declares one of three targeting shapes. The targeting system resolves the correct position or entity per shape and per input device:

| Shape | Description | Mouse (PC) | Controller / Keyboard | Player-facing? |
|---|---|---|---|---|
| **Self** | Effect originates from or is centered on the player | Player position | Player position | ✓ Yes |
| **Entity** | Effect is applied to a specific enemy; blocked if no valid target | Nearest enemy to cursor | Locked target | ✓ Yes |
| **Position** | Effect lands at a ground location; no enemy required | Cursor world position | Locked target's world position | Engine proof only |

- **Self:** no targeting input needed — always fires from the player. Compatible with auto-activate.
- **Entity:** must land on an enemy. On PC snaps to the nearest enemy to the cursor. On controller/keyboard fires at the locked target. Skill is blocked if no valid target exists. Compatible with auto-activate.
- **Position:** requires manual ground placement — does not work cleanly with auto-target. Position skills are engine proofs only and are not player-facing.

#### Range resolution per targeting shape

**This is a firm design rule — push back if a proposed skill violates it.**

| Shape | Cast range source | Rationale |
|---|---|---|
| **Entity** | Effective Range (weapon + armour + buffs) | You are reaching out to hit an enemy — your weapon's reach determines how far you can do that |
| **Position** | Skill's own `Range` field | You are placing an effect on the ground — this is a skill property, not a weapon property. A sword warrior can drop a zone as far as a wand mage if the skill allows it. |
| **Self** | Skill's own `Range` field | The skill defines its own radius — a wide Self-Duration-Tick radius on a sword warrior should not be shrunk by the sword's 1-tile reach |

- **Entity skills always use Effective Range.** A new Entity skill must not define a separate cast range — it inherits the character's gear-driven range automatically.
- **Position, Self, and Channeled skills always use their own `Range` field.** This is a skill property, not a gear property. Weapon and armour have no influence on zone placement distance or self/channeled radius.
- **Buffs that modify range** (e.g. a future Shout skill) must call `AddRangeBuffBonus` / `RemoveRangeBuffBonus` on the player — they affect Effective Range, which propagates to Entity skills only. Position/Self/Channeled ranges are unaffected.
- **Out-of-range clamping (Position skills):** if the cursor is beyond the skill's cast range, the zone lands at the range boundary in the direction of the cursor — never blocked, never silent. This matches standard ARPG behaviour (Diablo 4, PoE).

---

### Combat Facing

While **not attacking**, the character faces their movement direction. While **attacking** (OneShot animation active), the character always rotates to face the **locked target** — even if the player is moving in the opposite direction. This ensures attacks always connect visually and lets players kite while staying engaged with targets behind them.

### Weapon Animations & Handedness

Weapons are held in different hands depending on type, which drives which animation plays for melee attacks.

| Weapon type | Hold hand (visual) | Skeleton bone | Melee animation |
|---|---|---|---|
| Sword, Axe, Club, Dagger | Right hand | `Hand_R` | `melee_right_atack` |
| Bow | Left hand | `Hand_L` | `melee_left_atack` |
| Wand | Right hand | `Hand_R` | `melee_right_atack` |

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

Numbers are individual per hit — no stacking. Self-Channeled-Tick ticks each pop their own number; this preserves tick-rate readability and lets the player feel the difference between a fast and slow attack speed.

Both player-received and enemy-received hits produce damage numbers. There is no threshold — all damage shows.

### Skills

**Design rules for player-facing skills:**
- **All player-facing skills must deal direct damage.** Skills are damage delivery mechanisms — they always deal at least one hit of direct damage on activation. Debuffs, EoTs, and secondary effects (mines, traps) are added by augments, not baked into skills.
- **Player-facing skills use Entity or Self targeting only.** Position skills require manual ground placement, which does not fit the auto-target horde survival model. Position skills exist as engine proofs only and are not templates for future player-facing skill design. Self (non-enemy target) is always valid.

#### Player-Facing Prototypes

These are craftable and appear in player-facing systems. v2 will create real named skills derived from these.

All player-facing skills are **Entity** (one clean hit on locked target) or **Self** (hits everything around the player). This is a firm design principle — skills that require manual target switching or ground placement to be meaningful are not player-facing.

| Prototype | Targeting | Damage pattern | Skill type | Good for |
|---|---|---|---|---|
| Entity-Burst | Entity | Burst | Active | Basic attack — sword slash, arrow shot, wand bolt |
| Self-Channeled-Tick | Self | Tick | Channeled | Whirlwind, spinning blade, sustained spin-to-win |
| Self-Duration-Tick | Self | Tick | Active | Short AoE pulse burst — activate, ticks around you, cooldown |
| Self-Burst | Self | Burst | Active | Panic nova — surrounded, pop it, instant AoE clear |

> **Tech note — renames, not new skills:** Entity-Burst, Self-Channeled-Tick, Self-Duration-Tick, and Self-Burst are renames of the existing Strike, Cyclone, Damage Aura, and Nova implementations. Rename in code and data — do not create new skill objects. v2 will create the real named versions (Strike, Cyclone, etc.) derived from these prototypes.

All archetypes start with plain Entity-Burst in slot 1, no augments pre-socketed.

#### Engine Proof Prototypes

Retained to validate engine mechanics only. Not craftable, not player-facing. Not templates for future skill design.

| Prototype | Targeting | Damage pattern | Skill type | Proves |
|---|---|---|---|---|
| Fixed-Zone-Tick | Position | Tick | Active | Persistent ticking damage field at a ground position |
| Fixed-Zone-Burst | Position | Burst | Active | Instant remote explosion at a ground position |
| Windup-Burst | Position | Burst | Active | Telegraphed delay before detonation |
| Stackable-Zone | Position | Tick | Active | Multiple independent instances active simultaneously |
| Triggered-Zone-Burst | Position | Burst | Active | Proximity trigger — fires when enemy enters radius |
| Tracked-Tick | Entity | Tick | Active | Zone follows a specific target — requires manual target switching to spread; incompatible with auto-target. Can return as an augment effect. |
| Entity-Debuff | Entity | None | Active | Entity targeting with no damage output |
| Self-Aura-Tick | Self | Tick | Active | Old Aura mechanic — persistent passive pulse (replaced by Self-Duration-Tick) |

**Universal skill properties** — every skill in the game has these fields:

| Property | Description |
|---|---|
| Description | What this skill is designed to prove or do (v1: mechanic proof; future: player-facing flavour) |
| Kind | `Normal` = real named skill (v2+). `Prototype` = player-facing in v1, hidden in v2 when real named versions replace it. `EngineProof` = never player-facing or craftable — exists solely to validate engine mechanics. |
| Targeting shape | Self / Position / Entity — how the skill resolves its target (see Targeting) |
| Wind-up | Seconds of delay before effect lands; 0 = instant |
| Damage pattern | Burst (single hit) / Tick (over duration) / None (debuff or utility only) |
| Stack limit | Max simultaneous active instances; configurable per skill; — = not a zone skill |
| Zone tracks entity | Whether a zone follows a target entity after placement; — = not applicable |
| Duration | How long a placed zone or summon persists (seconds). `0` = permanent — lives until replaced by the stack cap or the run ends. Self skills and instant bursts always use 0. Zone and summon skills set this to prevent permanent effects (e.g. a Blizzard zone running forever would be broken). |
| Trigger radius | Detection radius that fires a trap when an enemy enters it (in tiles). `—` = not a trap skill. Default: 1 tile. |
| Arm time | Delay after placement before the trap becomes active (seconds). Prevents self-triggering. `—` = not a trap skill. |
| Trigger | How many times the trap fires before despawning. `Single` = fires once then despawns. `—` = not a trap skill. |

**Future field — Dispellable (not in v1):** whether a zone or effect can be removed before its duration expires — by an enemy cleanse ability, a player counter-skill, or a future mechanic. Not added until something in the game actually reads it. Note here so the axis is not forgotten when designing elite enemies or player utility skills.|

#### Entity-Burst

*(Renamed from Strike. Do not create a new skill — rename the existing implementation.)*

The universal starter prototype. Hits the locked target using whatever the character has equipped — a sword swing, an arrow, a wand bolt. All archetypes start with plain Entity-Burst, no augments pre-socketed. As players acquire new skills, Entity-Burst slots get replaced. Entity-Burst can still be kept in any slot intentionally.

| Property | Value |
|---|---|
| Description | Proves Entity targeting and weapon-adaptive delivery. Universal starter — fires at locked target using equipped weapon. |
| Kind | Prototype |
| Targeting shape | Entity |
| Wind-up | 0 (instant) |
| Damage pattern | Burst |
| Stack limit | — |
| Zone tracks entity | — |
| Damage type | Physical |
| Cooldown | 0.8s (tier 1) — lower at higher tiers |
| Damage | 1× weapon base damage |
| EoTs | None |
| Acquire | Free — slot 1 pre-filled at character creation |

#### Self-Channeled-Tick

*(Renamed from Cyclone. Do not create a new skill — rename the existing implementation.)*

Spin continuously in place, hitting all enemies within melee range on each tick. A Channeled skill — hold to spin, release to stop. Drains Focus while held, stops automatically at 0 Focus. Lower damage per hit than Entity-Burst; the value is continuous multi-target coverage.

| Property | Value |
|---|---|
| Description | Proves Channeled skill type with Self targeting. Continuous ticking damage while held; drains Focus over time. |
| Kind | Prototype |
| Targeting shape | Self |
| Wind-up | 0 (instant) |
| Damage pattern | Tick |
| Stack limit | — |
| Zone tracks entity | — |
| Type | Channeled |
| Damage type | Physical |
| Focus cost | 12 Focus/sec drain |
| Damage per hit | 0.4× weapon base damage (placeholder) |
| Tick rate | 4 hits/sec |
| Acquire | Craft |

#### Self-Duration-Tick

*(Renamed from Damage Aura. Do not create a new skill — rename the existing implementation.)*

Activate once — pulses magic damage to all nearby enemies repeatedly for a few seconds, then enters cooldown. Proves Active Self ticking damage over a fixed duration. Natural pairing with Heavy armour and Wand: tanky magic build that stands in the horde and lets the damage tick. Wand EoT affinity means augments (e.g. Burn) trigger frequently per tick.

| Property | Value |
|---|---|
| Description | Proves Active Self skill with ticking damage over a fixed duration. Activate → ticks damage in radius for duration → cooldown. |
| Kind | Prototype |
| Targeting shape | Self |
| Wind-up | 0 (instant) |
| Damage pattern | Tick |
| Stack limit | — |
| Zone tracks entity | — |
| Type | Active |
| Damage type | Magic (placeholder) |
| Focus cost | 15 Focus (flat, on activation — placeholder) |
| Damage per tick | 0.2× weapon base damage (placeholder) |
| Tick rate | 2/sec (placeholder) |
| Duration | 3s (placeholder) |
| Cooldown | 2s (after duration ends — placeholder) |
| Range | Short radius around player |
| Acquire | Craft |

#### Self-Burst

*(Renamed from Nova. Do not create a new skill — rename the existing implementation.)*

An instant explosion centered on the player — hits all enemies within a medium radius simultaneously, then enters cooldown. Proves Active Self burst. Panic button feel — surrounded, pop it, create space.

| Property | Value |
|---|---|
| Description | Proves Active Self burst. Instant explosion centered on player; flat Focus cost. |
| Kind | Prototype |
| Targeting shape | Self |
| Wind-up | 0 (instant) |
| Damage pattern | Burst |
| Stack limit | — |
| Zone tracks entity | — |
| Type | Active |
| Damage type | Physical (placeholder) |
| Focus cost | 20 Focus (flat) |
| Damage | 0.8× weapon base damage |
| Cooldown | 1.5s |
| Radius | Medium (larger than melee range) |
| Acquire | Craft |

---

All values (damage, cooldown, radius, tick rate, duration) are TBD — owned by the Balancer.

**Fixed-Zone-Tick**

| Property | Value |
|---|---|
| Description | Proves Position targeting with a fixed ticking zone. Zone stays where cast; enemies walk through it. |
| Good for | Skills that place a persistent damage field at a location — enemies walk into it and take repeated hits. Traps, pools, hazard zones. |
| Kind | EngineProof |
| Targeting shape | Position |
| Wind-up | 0 (instant) |
| Damage pattern | Tick |
| Tick rate | 1/sec (test value) |
| Stack limit | 1 |
| Zone tracks entity | No |
| Duration | 5s (test value) |
| Type | Active |
| Damage type | Magic |

**Fixed-Zone-Burst**

| Property | Value |
|---|---|
| Description | Proves Position targeting with a single burst hit. A remote instant explosion — Self-Burst placed at a chosen location. |
| Good for | Skills that detonate a single explosion at a chosen spot — remote instant damage with no lingering effect. |
| Kind | EngineProof |
| Targeting shape | Position |
| Wind-up | 0 (instant) |
| Damage pattern | Burst |
| Stack limit | 1 |
| Zone tracks entity | No |
| Duration | 0 — instant burst, no persistent zone |
| Type | Active |
| Damage type | Magic |

**Windup-Burst**

| Property | Value |
|---|---|
| Description | Proves wind-up mechanic. Telegraphed 1.5s delay before a high-damage burst lands at target position. Wind-up is the balancing cost. |
| Good for | Skills with a visible telegraph before a powerful hit lands — high damage that enemies can theoretically walk out of. |
| Kind | EngineProof |
| Targeting shape | Position |
| Wind-up | 1.5s |
| Damage pattern | Burst |
| Stack limit | 1 |
| Zone tracks entity | No |
| Duration | 0 — instant burst on detonation, no persistent zone |
| Type | Active |
| Damage type | Magic |

**Tracked-Tick**

| Property | Value |
|---|---|
| Description | Proves Entity targeting with a zone that follows the target. Ticks damage to the tracked enemy and all enemies within radius around them. Zone moves with the entity. |
| Good for | Skills that attach a persistent effect to an enemy — follows the target and damages it (and nearby enemies) continuously. Curses, brands, haunts. |
| Kind | EngineProof |
| Targeting shape | Entity |
| Wind-up | 0 (instant) |
| Damage pattern | Tick |
| Tick rate | 1/sec (test value) |
| Stack limit | 1 |
| Zone tracks entity | Yes |
| Duration | 5s (test value) — zone persists after target dies (stops following, keeps ticking in place until duration expires) |
| Type | Active |
| Damage type | Magic |
| AoE | Hits tracked enemy + all enemies within radius around them |

**Entity-Debuff**

| Property | Value |
|---|---|
| Description | Proves Entity targeting with no damage output. Applies a debuff directly to the locked target; effect follows the target for its duration. Retained as a mechanic proof only — not a template for future skill design (all player-facing skills must deal direct damage). |
| Good for | Engine proof of Entity targeting + debuff application. Not intended for player use. |
| Kind | EngineProof |
| Targeting shape | Entity |
| Wind-up | 0 (instant) |
| Damage pattern | None |
| Stack limit | 1 |
| Zone tracks entity | Yes |
| Duration | 6s (test value) |
| Type | Active |
| Damage type | Magic (N/A) |
| Effect | Slow (placeholder) |

**Stackable-Zone**

| Property | Value |
|---|---|
| Description | Proves configurable stack limit. Each cast places an independent ticking zone; up to the stack cap active simultaneously. |
| Good for | Skills where you want multiple independent instances active simultaneously — turrets, totems, summons, overlapping zones. |
| Kind | EngineProof |
| Targeting shape | Position |
| Wind-up | 0 (instant) |
| Damage pattern | Tick |
| Tick rate | 1/sec (test value) |
| Stack limit | 3 (test value) |
| Zone tracks entity | No |
| Duration | 10s (test value) — oldest instance despawns when a 4th is cast before duration elapses |
| Trigger radius | — |
| Arm time | — |
| Trigger | — |
| Type | Active |
| Damage type | Magic |

**Triggered-Zone-Burst**

| Property | Value |
|---|---|
| Description | Proves trigger-on-proximity mechanic. Placed at a position, dormant until an enemy enters the trigger radius, then fires once and despawns. |
| Good for | Traps, proximity mines, tripwires — placed hazards that punish enemies for moving through an area. |
| Kind | EngineProof |
| Targeting shape | Position |
| Wind-up | 0 (instant) |
| Damage pattern | Burst |
| Stack limit | 3 (test value) |
| Zone tracks entity | No |
| Duration | 30s (test value) — despawns if not triggered before expiry |
| Trigger radius | 1 tile (test value) |
| Arm time | 0.5s (test value) — prevents self-triggering immediately after placement |
| Trigger | Single (fires once, despawns) |
| Type | Active |
| Damage type | Magic |

---

**Weapon is the root of the damage number.** Each weapon has a base damage value that increases with tier. The skill defines the damage type and multiplier — Entity-Burst is physical (placeholder); future named skills define their own type. The archetype multiplier keys off the skill's damage type, not the weapon's.

**Level damage bonus: +2% per level (cumulative).** At level 10 = +20% total. All values below are placeholder — owned by the Balancer.

### Focus

Focus is the universal skill resource. All archetypes spend Focus to fire skills; skills cannot activate when Focus is empty.

**Regeneration:** Passive regen over time at a steady rate. Always recovering — no kill-based acceleration.

**At 0 Focus:** Skills cannot fire. Cooldowns still count down; auto-activate waits until enough Focus regens to cover the cost. Channeled skills stop automatically when Focus hits 0.

**Channeled skill tag:** Skills tagged `Channeled` drain Focus continuously while the button is held. Releasing the button stops the skill immediately. If Focus hits 0 the skill stops automatically regardless of input. Auto-cast holds the button indefinitely — player responsibility to not auto-cast a skill that drains their entire Focus bar.

**Skill costs (placeholder — owned by Balancer):**

| Skill | Cost |
|---|---|
| Entity-Burst | 5 Focus (flat) — effectively free; regens faster than you spend |
| Self-Burst | 20 Focus (flat) — meaningful burst cost |
| Self-Channeled-Tick | 12 Focus/sec (drain while held) — expensive over time, requires management |
| Self-Duration-Tick | 15 Focus (flat, on activation) — burst cost like Self-Burst; ticks for duration then cooldown |

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

Crit is an `on_enemy_hit_%` skill augment. When it triggers, the hit deals base skill damage × crit multiplier. Crit applies to base skill damage only — EoT damage from augments is unaffected.

`Final damage (on crit) = Skill base damage × Crit Multiplier`

The augment's trigger chance is the crit chance. The Bow identity bonus adds a flat % on top of the augment's trigger chance — a Bow with a Critical Strike augment crits more often than a Sword with the same augment.

**Crit Multiplier:** fixed at 1.5× in v1. Not investable. Future: investable via a dedicated augment.

### Effects over Time (EoT)

Skill Augments can apply **Effects over Time (EoT)** to enemies. EoTs are not applied by skills directly — they always come from augments. The augment's trigger chance determines whether the EoT is applied on a given hit; once triggered, the EoT applies at 100%.

Every EoT has the same three properties:

| Property | All EoTs | Notes |
|---|---|---|
| Duration | Yes | How long the effect lasts; refreshes on reapply |
| Tick rate | Damage EoTs only | Ignored for non-damage EoTs |
| Damage per tick | Damage EoTs only | Ignored for non-damage EoTs |

**Application rules (all EoTs):**
- No stacking — only one instance of each EoT type per enemy at a time
- Reapplying refreshes the duration rather than stacking or being ignored
- Crit does not affect EoT damage — crit only applies to base skill damage

The EoT type defines *what it does* when active:

| EoT | Damage per tick? | What it does |
|---|---|---|
| Slow | No | Reduces enemy movement speed |
| Burn | Yes (Magic) | Deals Magic damage per tick |
| Vulnerability | No | Increases damage taken by the enemy |

When designing new EoTs: if it deals damage per tick, set tick rate and damage per tick. If not, leave those blank. That is the only distinction.

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

Damage multipliers apply per skill damage type — a Warrior using a magic-type skill gets the 0.5× magic multiplier regardless of weapon equipped. Mismatched builds (archetype vs skill type) are viable but produce reduced output. All values are placeholder — owned by the Balancer.

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
| ESC     | Pause                                       |

The skill slot has an **auto-activate toggle** (set in the character screen before the run). When enabled, the skill fires automatically on cooldown — the key still works as a manual override.

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

> Map design, procedural generation, biomes, chunks, and obstacle props are documented in `docs/gdd-map.md`.

Maps are the arenas where runs take place. Each map has a **Map Level** attribute that scales kill XP — killing an enemy grants `1 XP × map level` directly, on top of any XP Shard the enemy drops.

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

### Enemy Navigation

**Always navigate.** Enemies use navmesh pathfinding at all times — they never walk into walls or get stuck on corners. Competent movement is non-negotiable for horde feel; a skeleton bumping into a pillar reads as broken, not charming.

**No separation (v1).** Enemies do not avoid each other. The blob is intentional — 30 skeletons converging on the same point is the visual threat mass that Self-Burst and Self-Channeled-Tick are designed to answer. Spreading enemies out would make horde skills feel weaker and the threat more diffuse. Light natural spreading from collision is sufficient. Revisit post-v1 if playtesting reveals a problem.

**Chokepoints are a feature.** Map corridors and doorways are intentional tactical geometry. Enemies funneling through a doorway is a core fun moment — position at the mouth of a corridor, pop a Self-Burst or Self-Duration-Tick, clear the flood. This falls out of correct pathfinding for free; no extra design work needed. Map design should treat chokepoints as a first-class tool, not an obstacle routing problem.

### Enemy Spawning

There are two categories of enemy presence in a run:

**Pre-placed enemies** — authored into room templates when the map is built. Start idle. Aggro via the proximity cluster system below.

**Wave-spawned enemies** — spawned dynamically during the run as part of escalating difficulty. Always aggro immediately on spawn — no idle state, no cluster membership. These are the horde.

#### Enemy Pool (wave-spawned)

What wave-spawned enemies can appear is defined per map by an **enemy pool** — a list of typed variants with counts and stat modifiers:

```
EnemyPoolEntry:
  EnemyType: string       // e.g. "skeleton", "archer_skeleton"
  Count: int              // drives spawn ratio (weight in the pool)
  Modifiers:
    ArmorBonus: int       // e.g. +5, +10
    HpBonus: int          // future
    SpeedBonus: int       // future
    DamageBonus: int      // future
```

The spawner draws randomly from the pool weighted by `Count`. Modifiers are applied to the enemy instance at spawn time on top of base stats. v1: one entry, `skeleton`, count 1, all modifiers zero.

**Map crafting hook:** when maps become craftable, the player configures the enemy pool — e.g. "warrior skeletons only" or "5× light skeleton + 10× heavy skeleton". The spawner consumes whatever the pool defines; no spawner changes needed.

#### Proximity Cluster System (pre-placed enemies)

Pre-placed enemies are always **idle** at map load. Clusters are not authored or pre-computed — they are an emergent property of which idle enemies are near each other at any given moment.

**How clusters form:** think of it like cells organising. Each idle enemy looks at its neighbours. If another idle enemy is within proximity range, they are connected. Any two idle enemies connected directly or through a chain of connections belong to the same cluster. This is recalculated dynamically — not stored permanently.

**Enemy states:**

| State | Behaviour |
|---|---|
| Idle | Standing still. Participates in proximity clustering. Has an individual aggro radius. |
| Chasing | Independently pursuing the player via navmesh. No cluster membership while chasing. |

**Aggro trigger:** each idle enemy checks player distance every frame. If the player enters that enemy's aggro radius → the enemy wakes → its entire connected idle cluster wakes simultaneously. Clusters produce the "whole group snaps to attention" feel without manual authoring.

**Losing the player:** if a chasing enemy loses the player (player moves beyond `BalanceConfig.Enemies.LostPlayerDistanceTiles` × tile size), the enemy returns to idle and immediately re-runs proximity scanning. It joins the nearest idle cluster or forms a new one with other nearby idle enemies. Clusters naturally reform around survivors and returning chasers.

> **Current value: 30 tiles (1080 world units).** Wave-spawned enemies need a threshold that exceeds their spawn distance (~560 units / ~15.5 tiles from the nearest room centre), or they switch to Idle immediately on spawn. Once pre-placed enemies are implemented, they should use a separate, smaller config value (6–10 tiles is the intended design range for pre-placed). `LostPlayerDistanceTiles` will be split into `WaveSpawnLostPlayerTiles` and `PrePlacedLostPlayerTiles` at that point.

**Wave-spawned enemies** are always created in the chasing state — they never participate in clustering.

**No manual clump authoring required.** Room designers place enemies; the proximity system handles grouping automatically. The only tuning lever is the proximity radius (how far apart enemies can be and still cluster together) — this can live on `MapData` to allow different maps to feel tighter or looser.

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

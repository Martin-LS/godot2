# Design Ideas — Parking Lot

> Uncommitted ideas worth remembering. Nothing here is scheduled or spec'd — this is a scratch space to capture concepts before they're lost. Pull from here when a topic becomes relevant.

---

## Weapon Design Approaches

*Context: exploring alternatives to hard weapon-skill requirements. Current design uses weapon affinity tags (Melee/Ranged/Magic) that grant flat damage bonuses to matching skills. These ideas push that further.*

---

### Idea: Weapons Grant Tags to Skills

Instead of skills requiring a weapon type, weapons *add* tags to all your skills. A sword adds `Slashing` and `Melee` tags; a bow adds `Projectile` and `Ranged`. Skills have no weapon requirement — but modifiers that require `Slashing` only activate when you're holding a sword.

The weapon shapes *how* skills behave rather than *whether* they're available. Same skill, different weapon → different modifier pool. Clean inversion of the PoE2 approach. Fits naturally into the existing tag system.

---

### Idea: Weapon as a Built-in Skill Modifier

The weapon itself is a modifier applied to compatible skills. Every weapon has a built-in mutation: a dagger adds "hits cause bleed," a staff adds "20% larger AoE," a bow adds "fires an additional projectile." The mutation only applies if the skill has a matching tag.

Any skill still fires with any weapon — the weapon just doesn't synergise unless tags match. Makes the "correct" weapon feel rewarding without hard-gating. Pairs naturally with the tag-grant idea above.

---

### Idea: Weapon Stance System

Decouple stance from equipment entirely. The player chooses a **stance** (Brawler, Marksman, Elementalist, etc.) independently of what weapon they're holding. The stance determines which skill tags are active; the weapon provides stats only.

Want Marksman stance with a sword? Fine — you're a precise melee fighter using the ranged modifier pool. Closer to Dragon's Dogma 2's Vocation system: identity comes from your chosen style, not your item slot.

---

### Idea: Dual-Nature Skills

Every skill has two modes — a melee version and a ranged version — and the equipped weapon determines which fires. "Blade Storm" swings in a close arc with a sword, fires energy projectiles with a staff. Same skill, same modifier slots, same cooldown; weapon transforms the delivery.

Modifiers work on both modes (shared tags), but some enhance only one ("increases range" does nothing in melee mode). Most interesting of the five but most work to implement.

---

### Idea: Weapon Affinity (Soft Lock)

Skills have an **affinity rating** per weapon type rather than a hard requirement. A bow skill might be 100% with bows, 60% with wands, 20% with swords. Off-affinity still works but at reduced effectiveness (damage and range scale down). Lets players make intentional tradeoffs — sometimes a slightly weaker off-affinity skill is worth it for what the weapon gives the rest of the build.

---

### Notes on Weapon Ideas

- Ideas 1 (tags) and 2 (mutation) are the most compatible with the current architecture and with each other — they could be layered on top of the existing affinity system without redesigning it.
- Ideas 3 (stance) and 4 (dual-nature) are more structural — they'd change how weapon identity works at the character level.
- All five avoid hard weapon-skill locks, which is the core design goal.

---

## Weapon Bonus (Post-v1 Expansion)

*Context: discussed when designing the weapon-adaptive skill system. Cut from v1 to keep things simple.*

Each weapon has a single passive bonus that applies to compatible skills. Examples: a bow adds an extra projectile to all `Ranged` skills; a sword adds a bleed chance to all `Melee` skills. The bonus only applies if the skill's delivery tag matches the weapon type.

This extends weapon identity beyond range — two bows of the same tier could have different bonuses, making weapon choice within a type meaningful. Pairs naturally with the tag and range system already in place.

Worth revisiting when weapon depth is being expanded.

---

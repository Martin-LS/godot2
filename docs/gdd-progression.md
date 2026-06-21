# Game Design Document — Meta-Progression, Gear & UI

> Part of the GDD. See also `gdd-mechanics.md` for combat, skills, characters, and run structure.
> Living document — details will evolve as the game is playtested.

## Meta-Progression (Between Runs)

### Level Bonuses (automatic)
Each level gained during a run permanently increases the character's HP and damage. Bonuses scale with both archetype and level — each archetype grows faster in the stats that define its playstyle. These stack across all runs and are applied automatically on level-up. Exact growth coefficients are owned by the Balancer.

### Item Tiers

All items — both equipment and skills — have a **tier** that represents quality and power level. Tier is shown as the **border colour** of the item slot everywhere it appears (inventory, slots, pickers). The slot background is always Pale Slate (`#8AA0AE`) — a neutral light gray that lets the icon art read clearly.

| Tier     | Border colour | Hex       | Notes                  |
|----------|---------------|-----------|------------------------|
| Common   | Dark Slate    | `#4A5560` | Starter / lowest power |
| Uncommon | Ash Grey      | `#6B8090` | Mid tier               |
| Rare     | Dark Gold     | `#A07810` | Highest tier (v1)      |

Border colours are taken from the Loot Rarity Border column in `color-scheme.md` for consistency across inventory, loot drops, and minimap dots.

Exact stat differences per tier are TBD. Higher tier also unlocks more augment slots on skill and equipment items (see Skill Augments, Equipment Augments).

---

### Gear Slots

Characters can equip up to 4 gear items (one per gear slot) and 1 skill item (code supports more slots for future expansion). All items persist between runs. Each slot has a distinct role:

| Slot      | Role                                                             | Progression axis                    |
|-----------|------------------------------------------------------------------|-------------------------------------|
| Weapon    | Root of base damage; sets Weapon Range; determines visual delivery of skill animations | Tier → higher base damage + higher Weapon Range |
| Hat       | Survival — HP, Speed, damage reduction (%) by category; Range Modifier by category | Tier → better stats within category |
| Body      | Survival — HP, Speed, damage reduction (%) by category; Range Modifier by category | Tier → better stats within category |
| Ring      | Mitigation — physical resistance (%)                             | Tier → higher resistance            |
| Skill ×1  | Active/passive ability used during a run (code supports more slots for future expansion) | Tier → stronger effect + lower cooldown (cooldown is a skill attribute — no character-level attack speed stat exists) |

#### Skill Slots

1 skill slot shown on the HUD and used during a run. Code supports multiple slots for future expansion. Whatever is equipped in the skill slot is what fires during the run. Any archetype can equip any skill — there are no restrictions.

v1 starter skill: **Entity-Burst** (physical type, 1.0× multiplier). All archetypes start with plain Entity-Burst — no pre-socketed augments. Weapon drives the delivery animation; skill defines the damage type.

Skill items are crafted (Craft New — accessible from an empty skill slot, left-click → Craft New; not yet implemented in v1) and equipped from the **Skills inventory tab**.

#### Skill Augments

Skill Augments are craftable items that socket into a skill item to modify it. Any augment can go into any skill slot — no archetype or skill type restrictions. Each augment type can only be equipped once per skill (no duplicates).

**Ineffective combos — visual warning:** No augment is hard-locked from any skill, but some combinations produce no effect (e.g. Pierce on a Self skill — no projectile or impact point). The UI flags these with a warning indicator on the augment socket (red/yellow exclamation, hover tooltip explaining why). Exact indicator design TBD. Keeps the system open while giving players clear feedback when a slot is being wasted.

**Skill Augment slots per tier** — upgrading a skill unlocks deeper modification, not just bigger numbers:

| Skill tier | Skill Augment slots |
|------------|--------------|
| Common     | 1            |
| Uncommon   | 2            |
| Rare       | 3            |

- **Socketing:** choose a Skill Augment from inventory and place it into an open slot on the skill item
- **Removing:** free, Skill Augment returns to inventory

**Augment tag + trigger type system.** Each augment has a functional tag (e.g. `splash`, `pierce`, `slow`, `crit`). Each augment slot has a trigger type (e.g. `on_enemy_hit_%`, `always`) that declares which augment tags it accepts and how it fires. Mechanical augments (splash, pierce) use a trigger type with no % chance — they fire on every hit. Effect augments (slow, crit, burn) use `on_enemy_hit_%` with a fixed trigger chance defined on the augment. Full tag/trigger taxonomy TBD at implementation.

**v1 Skill Augments:**

| Skill Augment   | Tag      | Trigger type       | Effect |
|-----------------|----------|--------------------|--------|
| Splash          | `splash` | `always`           | Hit damages a small area around the impact point. Meaningful on Entity skills (Melee and Ranged). No effect on Self skills — UI warns. |
| Pierce          | `pierce` | `always`           | Hit passes through the first enemy and continues. Ranged: projectile travels through. Melee: swing cuts through enemies in a line. No effect on Self skills — UI warns. |
| Slow            | `slow`   | `on_enemy_hit_%`   | Applies the Slow EoT on hit |
| Critical Strike | `crit`   | `on_enemy_hit_%`   | Hit deals base damage × 1.5×. Trigger chance = crit chance. Bow identity adds a flat bonus on top. |
| Burn            | `burn`   | `on_enemy_hit_%`   | Applies the Burn EoT on hit |

Exact values (splash radius, trigger chances, slow %, burn damage, duration) are TBD.

**Future augment pattern — mine/trap placement:** A mine augment triggers `on_enemy_hit_%` and places a proximity trap at the hit location. Successive hits place additional mines up to an active cap. The cap scales with augment tier (e.g. tier 1 = 2 active mines, tier 2 = 4, tier 3 = 6). This introduces augment-tier-scaling caps as a mechanic — design in full when crafting tiers are being expanded.

**Crafting cost (v1):** every Skill Augment costs **1 Common material** to craft.

Skill Augments are crafted (Craft New entry point TBD — not yet implemented; planned via left-click on an open augment socket) and live in the **Augments inventory tab**.

#### Equipment Tags

Armour pieces (Hat and Body) carry a **category tag** that identifies their type. The tag drives the stat profile described in Hat & Body below. No augment gating — any augment can socket into any equipment item regardless of category.

| Armour | Tag |
|---|---|
| Hat / Body (Heavy) | `Heavy` |
| Hat / Body (Medium) | `Medium` |
| Hat / Body (Light) | `Light` |

Category is fixed per item — a Heavy hat stays Heavy regardless of tier.

#### Equipment Augments

Equipment Augments are craftable items that socket into an equipment item to add a **behaviour** — not just a stat bonus, but something that changes how that piece of equipment *feels* to use. They are the gear-layer equivalent of Skill Augments.

**Design intent — Equipment Augments are the defensive build layer.** Skill Augments handle offensive variance (splash, crit, pierce, damage conversion). Equipment Augments handle defensive variance — how the character survives, recovers, and punishes attackers. There is no stagger or hit-recovery system; the hit feedback is D4-style (always in control, no interrupts). All defensive investment flows through Equipment Augments. Future equipment augments should continue in this direction: barriers, dodge, resistance boosts, shield-on-hit, recovery mechanics. Offensive Equipment Augments (weapon/ring slot) are TBD and secondary to this defensive purpose.

**Design rule:** `always` and `on_player_hit_%` augments may not deal proactive offensive damage — reactive damage (e.g. Retaliation/thorns) is acceptable. Auras via equipment augments are defensive or debuff only — a damage aura belongs on a skill augment, not armour. Offensive utility (e.g. cooldown reduction) is a grey area — flag for review when new augments are designed.

**Equipment Augment slots per tier** — mirrors the Skill Augment slot system:

| Equipment tier | Equipment Augment slots |
|----------------|------------------------|
| Common         | 1                      |
| Uncommon       | 2                      |
| Rare           | 3                      |

- **Socketing:** choose an Equipment Augment from inventory and place it into an open slot on the equipment item
- **Removing:** free, Equipment Augment returns to inventory
- Any augment can socket into any equipment item — no tag gate. Each augment type can only be equipped once per item (no duplicates).

**v1 Equipment Augments** — same augment tag + trigger type system as skill augments, but player-based triggers. Each augment has a tag and a trigger type; trigger chance is fixed per augment:

| Augment     | Tag           | Trigger type        | Behaviour |
|-------------|---------------|---------------------|-----------|
| Retaliation | `retaliation` | `on_player_hit_%`   | On hit received: deal small Physical damage to attacker |
| Fortify     | `fortify`     | `on_player_hit_%`   | On hit received: reduce damage taken from the next hit |
| Dash Reflex | `dash_reflex` | `on_player_hit_%`   | On hit received: brief speed burst |
| Ghost Step  | `ghost_step`  | `on_kill_%`         | On kill: restore a small amount of HP |
| Mending     | `mending`     | `always`            | Regenerate a small amount of HP every 3s |

Exact trigger chances and values are TBD — owned by the Balancer.

Exact values for all behaviours are TBD — owned by the Balancer.

**Aura augments:** Auras are Equipment Augments with the `aura` tag — a persistent area effect emanating from the player. The trigger type is player's choice: an `always` slot keeps the aura permanently active; an `on_player_hit_%` slot fires it reactively on taking a hit. The augment tag defines what the aura does; the trigger type defines when it runs. Focus reservation may apply as a cost for `always` aura augments — TBD when augment design is expanded.

**Crafting cost (v1):** every Equipment Augment costs **1 Common material** to craft.

Equipment Augments are crafted (Craft New entry point TBD — not yet implemented; planned via left-click on an open augment socket) and live in the **Augments inventory tab**.

#### Weapon

Weapons do two things: provide the **base damage number** for all skill damage calculations, and set **Weapon Range**. No weapon gates any skill — every skill fires regardless of what is equipped.

**Weapon is the root of the damage number.** The skill defines the damage type and multiplier on top of the weapon's base. Upgrading weapon tier is the primary way to increase damage output. Each weapon type has a **passive identity bonus** that applies when the equipped skill's damage type matches the weapon's associated type — rewarding matched builds without restricting those who don't.

**Delivery is fixed per weapon type.** The weapon always drives the attack animation — a Sword always swings, a Bow always shoots, a Wand always fires a bolt. Skills do not override delivery.

**Weapon Range** is a flat number stat visible on the weapon item. Effective Range on the character sheet reflects this after armour modifiers are applied (see Hat & Body).

Range values are expressed in **tiles** (the canonical internal distance unit). One tile = 36 Godot world units — this conversion lives in `GameScale.TileSize`.

| Weapon type | Base Damage (tier 1) | Weapon Range | Delivery | Identity bonus (tier 1) |
|-------------|----------------------|--------------|----------|------------------------|
| Sword       | 15                   | 1 tile       | `Melee`  | +10% physical damage (applies when skill is physical type) |
| Bow         | 12                   | 7 tiles      | `Ranged` | +8% crit chance (type-agnostic — applies always) |
| Wand        | 18                   | 5 tiles      | `Ranged` | +10% magic damage (applies when skill is magic type) + EoT affinity (higher base trigger chance for EoT augments) |

**Tier scaling:** tier 2 = ×1.5 base damage, tier 3 = ×2.0 base damage. Identity bonus % also scales with tier (TBD). All values are placeholder — owned by the Balancer.

Any character can equip any weapon.

**Visuals (in-run):** Weapon is rendered on the character model.

#### Hat & Body

Hat and Body are the two armour equipment slots. Each piece has a **category** that defines its identity and its equipment tag. Category is fixed per item — crafting a higher-tier Heavy hat makes it stronger within that category, not a different category.

Any character can equip any category in any slot. Slots are independent — a character can mix freely (e.g. Heavy hat, Light body).

| Category | Equipment tag | HP       | Speed   | Damage Reduction | Range Modifier     |
|----------|---------------|----------|---------|------------------|--------------------|
| Heavy    | `Heavy`       | High     | Penalty | Yes (%)          | −1.5 tiles per piece |
| Medium   | `Medium`      | Moderate | Neutral | —                | None               |
| Light    | `Light`       | Low      | Bonus   | —                | +1.5 tiles per piece |

Stats above apply per piece — each slot contributes its category's stats independently.

**Range Modifier only applies to ranged weapons** — this is a universal rule for all armour categories. If the equipped weapon's `Delivery` is `Ranged`, both hat and body Range Modifiers are added to Effective Range. If `Delivery` is `Melee`, Range Modifier has no effect — a sword's reach is not shortened by heavy plate, and a Light archer's range bonus doesn't extend a sword swing.

**Effective Range** (visible on the character sheet):
- Ranged weapon: `Weapon Range + hat Range Modifier + body Range Modifier + range buff bonus` (in tiles)
- Melee weapon: `Weapon Range` (Range Modifiers ignored)

Displayed as tiles in the UI.

**Range buffs (v2+):** Skills may temporarily or permanently modify Effective Range mid-run (e.g. a Shout that increases attack range for X seconds). Any such buff adds a flat tile bonus on top of the baseline formula. Effective Range is therefore not fixed at run start — the baseline is calculated at run start, but it is recalculated whenever a range buff is applied or expires. All skill cast distance checks use the current Effective Range at the moment of firing.

**Visuals (in-run):** Hat, Body, and Weapon are rendered on the character model. Ring has no visual representation.

Heavy suits close-range builds taking hits; Light suits ranged builds that kite; Medium suits mixed or flexible builds. Mixing categories (e.g. Heavy hat, Light body) produces a middle-ground Effective Range for ranged weapons.

#### Ring

Rings grant **physical resistance (%)**. No category, no equipment tags — any character can equip any ring, and any Equipment Augment can socket into a ring. Tier is the only progression axis: higher-tier rings give higher resistance.

#### Starter Gear

Each character starts with one item per slot, matched to their archetype:

| Archetype | Weapon         | Hat            | Body           | Ring          | Skill slot 1        |
|-----------|----------------|----------------|----------------|---------------|---------------------|
| Warrior   | Sword (tier 1) | Heavy (tier 1) | Heavy (tier 1) | Ring (tier 1) | Entity-Burst (no augment) |
| Rogue     | Bow (tier 1)   | Light (tier 1) | Light (tier 1) | Ring (tier 1) | Entity-Burst (no augment) |
| Mage      | Wand (tier 1)  | Medium (tier 1)| Medium (tier 1)| Ring (tier 1) | Entity-Burst (no augment) |

All archetypes start with Entity-Burst — physical type, 1.0× multiplier, no augments. Weapon drives the delivery animation. Crit is a Bow identity bonus, not a Rogue identity; magic damage affinity is a Wand identity, not a Mage identity.

Specific item names and exact stat values are TBD.

**Acquisition:** Gear is not dropped by enemies. New items come from crafting — each item has a recipe requiring a combination of materials (see Currencies).

**Item identity:** Each item is a unique instance with its own ID. Items **upgrade in-place** — tier increases on the existing item rather than producing a new one. The item's border colour updates to reflect its new tier (see Item Tiers).

**Inventory:** Crafted (unequipped) items go into the **account inventory** — a shared pool accessible by every character. The inventory has three tabs:

| Tab | Contents | Capacity |
|---|---|---|
| Equipment | Crafted gear (weapons, armour, accessories) | 50 items |
| Skills | Crafted skill items | 50 items |
| Augments | Crafted Skill Augments and Equipment Augments | 50 items |

Equipped items are held separately in the character's slots and do not count against inventory capacity. Each tab is visible on the Character Screen as a scrollable 5-column icon grid.

**Equipping:** Right-click an inventory item → equips to first empty valid slot; if all valid slots are occupied, swaps with slot 1 (old item returns to inventory). Right-click an occupied character slot → unequips, item returns to inventory (blocked if inventory is full). See the full interaction model under *Character Screen — Mouse/Keyboard Interaction Model*.

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

- Health bar (HUD, bottom-left)
- XP bar + current level
- Coin counter (this run)
- Elapsed time / countdown
- **Skill bar** — bottom-center of the HUD. 1 slot. Shows the slotted skill with cooldown/toggle state:
  - Active skill on cooldown: slot is greyed out, fills from bottom as cooldown recovers
  - Active skill ready: slot fully lit
  - Passive skill: lit when toggled on, greyed when off
  - Empty slot: visually empty (no icon)
- **Floating HP bar** — above both player and enemies (see Hit Feedback in `gdd-mechanics.md` for colours and visibility rules)
- **Damage numbers** — float upward from the hit point on every hit, colour-coded by damage type and crit (see Hit Feedback in `gdd-mechanics.md`)
- [TBD] Minimap

### Menus
- **Main Menu** → title screen, Play button
- **Account Screen** → the account-level hub. Always the first screen after Main Menu. Contains the character roster (list characters, create new, delete). Designed to grow — future account-level info (account stats, global progress, etc.) will live alongside the roster. Selecting a character navigates to their Character Screen.
  - **Character creation — name rules:** Required (non-empty). Alphanumeric only — no spaces or special characters. Must be unique across all characters on the account. Confirm button is disabled until all rules pass; inline error message explains which rule is violated.
- **Character Screen** → full management hub for the selected character. Two tabs: **Loadout** (default) and **Sigils**. Start Run button at the bottom. Back button returns to Account Screen.
  - **Loadout tab** — two-column layout:
    - *Left/centre* — character name, archetype, stats, and equipped slots: Weapon / Hat / Body / Ring / Skill 1
    - *Right column* — account inventory, always visible within this tab. Scrollable 5-column icon grid with three sub-tabs:
      - *Equipment* — crafted gear, 50-item cap
      - *Skills* — crafted skill items, 50-item cap
      - *Augments* — crafted Skill Augments and Equipment Augments, 50-item cap
    - Equipped items are not shown in the inventory — they live in the character slots.
  - **Sigils tab** — visible, empty (reserved for future sigil system)
  - Both tabs are always visible; empty tabs are not locked or greyed out

#### Character Screen — Mouse/Keyboard Interaction Model

Crafting is not a separate tab — it is accessed contextually through item interactions.

**Right-click = direct action (no menu):**
- Right-click an **inventory item** → equip to first empty valid slot. If all valid slots are occupied, swap: new item equips to slot 1, old item returns to inventory.
- Right-click an **equipped slot** → unequip; item returns to inventory (blocked if inventory is full).
- Right-click a **filled augment socket** → remove augment; returns to inventory.
- Right-click an **empty slot or empty socket** → no action.

**Left-click = context-sensitive menu:**
- Left-click an **inventory item** → menu: **Equip**, **Modify**, **Delete**
- Left-click an **equipped slot (filled)** → menu: **Unequip**, **Modify**, **Delete**
- Left-click an **equipped slot (empty)** → menu: **Craft New** (opens create list for that slot type), **Equip from inventory** (item picker filtered to slot type)
- Left-click an **open augment socket** → augment picker (compatible augments from inventory)
- Left-click a **filled augment socket** → menu: **Remove** (returns augment to inventory)

Equip/Unequip in the left-click menu mirrors the right-click shortcut — right-click is the fast path, left-click menu is the discoverable path for new players. Both apply the same logic (equip to first empty valid slot / swap with slot 1; unequip blocked if inventory full).

**Drag and drop:**
- Drag an inventory item onto a valid slot → equip to that specific slot (swap if occupied)
- Drag an inventory augment onto an open socket → socket it

**Modify panel** (opened from left-click → Modify on any item, from inventory or equipped slot):
- Shows the item's current stats and tier
- **Upgrade** button — increase tier (costs materials)
- Augment socket rows — drag augment from inventory or left-click socket to open augment picker
- Right-click a filled socket → removes augment directly

---

- **Run results overlay** → shown at run end; return button goes back to Character Screen
- **Pause menu** — ESC during a run; second ESC or Resume button closes it; run is paused while open
  - **Resume** button — closes menu, run continues
  - **End Run** button — exits immediately to character screen; all progress from this run is discarded (level, XP, coins, crafting materials). Warning text alongside: *"All progress from this run will be lost."*
  - No confirmation step — warning text is the friction

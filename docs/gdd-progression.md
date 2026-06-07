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

Characters can equip up to 4 gear items (one per gear slot) and up to 3 skill items (one per skill slot). All items persist between runs. Each slot has a distinct role:

| Slot      | Role                                                             | Progression axis                    |
|-----------|------------------------------------------------------------------|-------------------------------------|
| Weapon    | Root of base damage; sets Weapon Range; determines visual delivery of skill animations | Tier → higher base damage + higher Weapon Range |
| Hat       | Survival — HP, Speed, damage reduction (%) by category; Range Modifier by category | Tier → better stats within category |
| Body      | Survival — HP, Speed, damage reduction (%) by category; Range Modifier by category | Tier → better stats within category |
| Ring      | Mitigation — physical resistance (%)                             | Tier → higher resistance            |
| Skill ×3  | Active/passive ability used during a run                         | Tier → stronger effect + lower cooldown (cooldown is a skill attribute — no character-level attack speed stat exists) |

#### Skill Slots

Three skill slots map directly to the 3 skill bar slots shown during a run. Whatever is equipped in skill slots 1–3 is what fires during the run.

The same skill item can be equipped in multiple slots simultaneously. Any archetype can equip any skill — there are no restrictions. v1 has one skill: **Strike** (tags: `Attack`). All archetypes start with plain Strike — no pre-socketed augments. Weapon type determines damage type and delivery.

Skill items are crafted (Craft New — accessible from an empty skill slot, left-click → Craft New; not yet implemented in v1) and equipped from the **Skills inventory tab**.

#### Skill Augments

> **PoE2 inspiration:** Skill Augments are the equivalent of PoE2 support gems — they socket directly into a skill and modify how it behaves. Tag compatibility is the only gate; there are no character or archetype restrictions.

Skill Augments are craftable items that socket into a skill item to modify it. **There is no tag gate on socketing** — any Skill Augment can go into any skill slot. Tags on augments are descriptive only (they describe what the augment does and what kind of skill it was designed for) and do not restrict socketing.

**Skill Augment slots per tier** — upgrading a skill unlocks deeper modification, not just bigger numbers:

| Skill tier | Skill Augment slots |
|------------|--------------|
| Common     | 1            |
| Uncommon   | 2            |
| Rare       | 3            |

- **Socketing:** choose a compatible Skill Augment from inventory and place it into an open slot on the skill item
- **Removing:** free, Skill Augment returns to inventory
- **Compatibility:** governed by tags — a Skill Augment declares which tags it requires; the skill must have at least one

**Augment conflicts:**

Some augments are mutually exclusive by effect — for example, two damage type conversion augments (Magic Damage + Chaos Damage) cannot both apply. When a conflict exists:

- The augment in the **highest slot index wins** — slot 3 beats slot 2, slot 2 beats slot 1
- Losing augments are **greyed out with a red X** in the skill item UI
- Hovering or selecting a losing augment **highlights the winning augment** and shows a tooltip: *"Overridden by [Augment Name] in slot [N]"*
- Leaving a conflicting augment slotted is allowed — it is simply wasted. The player resolves it by moving their preferred augment to a higher slot.
- Slot order is the only tiebreaker. It has no effect on math — it exists solely for conflict resolution.

**Augment math model:**

Augment effects are evaluated using a bucketed formula inspired by PoE. Slot order is irrelevant to math — all operations within each bucket are commutative.

`Final value = (Base + Flat additions) × (1 + sum of all Increased%) × More₁ × More₂ × …`

| Bucket | What goes here | How it stacks |
|---|---|---|
| Flat additions | Fixed numeric additions (+10 damage, +5 range) | Summed together, applied to base first |
| Increased % | Percentage bonuses (+20% damage, +15% crit chance) | All pooled into a single sum, applied as one multiplier |
| More multipliers | Independent conditional multipliers | Each applied separately — they compound multiplicatively |

Augments are designed as flat additions or increased % bonuses. More multipliers are reserved for specific conditional effects where compounding is intentional (e.g. a crit multiplier). Raw unconditional multipliers (×2 damage) are avoided on general augments — they produce unintuitive power spikes when combined with increased% bonuses and make balancing unpredictable.

**v1 Skill Augments:**

| Skill Augment    | Designed for | Effect |
|------------------|-------------|--------|
| Splash           | `Melee`     | Hit damages a small area around the target |
| Pierce           | `Ranged`    | Projectile passes through enemies |
| Slow             | `Attack`    | Applies the Slow EoT on hit (see Effects over Time) |
| Critical Strike  | `Attack`    | Adds flat **Crit Chance**. On crit: final damage (any type) is multiplied by the **Crit Multiplier** (fixed at 1.5× in v1; shown to players as +50% Crit Damage). Damage EoTs applied by a crit hit are stamped with the Crit Multiplier for their full duration. Used as the Rogue starter augment. |
| Magic Damage     | `Attack`    | Converts damage to Magic type — used as the Mage starter augment |

Exact values (splash radius, crit chance, crit multiplier, slow %, apply chance, duration) are TBD.

**Crafting cost (v1):** every Skill Augment costs **1 Common material** to craft.

Skill Augments are crafted (Craft New entry point TBD — not yet implemented; planned via left-click on an open augment socket) and live in the **Augments inventory tab**.

#### Equipment Tags

Equipment items have **tags** — the same concept as skill tags, applied to gear. Tags determine which Equipment Augments can socket into an item. An Equipment Augment declares which tags it requires; the item must have at least one matching tag. Equipment Augments with no tag requirement can socket into any equipment item.

| Equipment | Tags |
|---|---|
| Sword | `Melee` |
| Bow | `Ranged` |
| Wand | `Magic` |
| Hat / Body (Heavy) | `Heavy` |
| Hat / Body (Medium) | `Medium` |
| Hat / Body (Light) | `Light` |
| Ring      | *(no tags — universal augments only)* |

Weapon tags intentionally reuse the skill tag names — players already know `Melee`, `Ranged`, `Magic` from skills.

#### Equipment Augments

Equipment Augments are craftable items that socket into an equipment item to add a **behaviour** — not just a stat bonus, but something that changes how that piece of equipment *feels* to use. They are the gear-layer equivalent of Skill Augments.

**Equipment Augment slots per tier** — mirrors the Skill Augment slot system:

| Equipment tier | Equipment Augment slots |
|----------------|------------------------|
| Common         | 1                      |
| Uncommon       | 2                      |
| Rare           | 3                      |

- **Socketing:** choose a compatible Equipment Augment from inventory and place it into an open slot on the equipment item
- **Removing:** free, Equipment Augment returns to inventory
- **Compatibility:** governed by equipment tags — an Equipment Augment declares which tags it requires; the item must have at least one. Augments with no tag requirement work on any equipment including accessories.

**v1 Equipment Augments:**

| Augment | Requires tag | Behaviour |
|---|---|---|
| Retaliation | `Heavy` | On hit received: deal small Physical damage to attacker |
| Fortify | `Heavy` | On hit received: reduce damage taken from the next hit |
| Dash Reflex | `Light` | On hit received: brief speed burst |
| Ghost Step | `Light` | Killing an enemy within 2s of taking a hit restores a small amount of HP |
| Mending | `Medium` | Regenerate a small amount of HP every 3s |
| Adaptation | `Medium` | On kill: reduce active skill cooldowns slightly |

Weapon and ring Equipment Augments (targeting `Melee`, `Ranged`, `Magic`, and universal tags) are TBD — designed when weapon and ring depth is expanded.

Exact values for all behaviours are TBD — owned by the Balancer.

**Crafting cost (v1):** every Equipment Augment costs **1 Common material** to craft.

Equipment Augments are crafted (Craft New entry point TBD — not yet implemented; planned via left-click on an open augment socket) and live in the **Augments inventory tab**.

#### Weapon

Weapons do three things: provide **base damage** for all skill damage calculations, set **Weapon Range** for all your skills, and define **PreferredDelivery** — the fallback delivery mode for weapon-adaptive skills (those with no delivery tag). No weapon gates any skill — every skill fires regardless of what is equipped.

**Weapon is the root of base damage.** Skill damage = weapon base damage × archetype damage multiplier × (1 + level damage bonus%). Upgrading weapon tier is the primary way to increase damage output. Each weapon type also has a **passive identity bonus** that further amplifies a specific stat — rewarding players who match weapon to playstyle without restricting those who don't.

**Weapon Range** is a flat number stat visible on the weapon item. It applies to all skills — delivery-tagged or adaptive. Effective Range on the character sheet reflects this after armour modifiers are applied (see Hat & Body).

Range values are expressed in **tiles** (the canonical internal distance unit). The arena is 24×24 tiles; the playable interior is roughly 22×22 tiles. One tile = 36 Godot world units — this conversion lives in `GameScale.TileSize` and is independent of map art. If the map pack changes, only `GameScale.TileSize` needs updating; all range values stay correct.

**PreferredDelivery** is a formal property on each weapon. When a skill has no delivery tag it is weapon-adaptive and inherits the weapon's `PreferredDelivery` at run start. Skills that carry a delivery tag always use that tag — `PreferredDelivery` is ignored.

How delivery resolves at fire time:
- `Ranged` delivery + sword → sword throw
- `Ranged` delivery + bow → arrow
- `Ranged` delivery + wand → wand bolt
- `Melee` delivery + any weapon → weapon swing / contact animation

| Weapon type | Equipment tag | Base Damage (tier 1) | Weapon Range | PreferredDelivery | Identity bonus (tier 1) |
|-------------|---------------|----------------------|--------------|-------------------|------------------------|
| Sword       | `Melee`       | 15 physical          | 1 tile       | `Melee`           | +10% physical damage   |
| Bow         | `Ranged`      | 12 physical          | 7 tiles      | `Ranged`          | +8% crit chance        |
| Wand        | `Magic`       | 18 magic             | 5 tiles      | `Ranged`          | +10% magic damage      |

**Tier scaling:** tier 2 = ×1.5 base damage, tier 3 = ×2.0 base damage. Identity bonus % also scales with tier (TBD). All values are placeholder — owned by the Balancer.

Any character can equip any weapon. Weapons carry equipment tags for Equipment Augment compatibility.

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

**Range Modifier only applies to ranged weapons** — this is a universal rule for all armour categories. If the equipped weapon's `PreferredDelivery` is `Ranged`, both hat and body Range Modifiers are added to Effective Range. If `PreferredDelivery` is `Melee`, Range Modifier has no effect regardless of armour category — a sword's reach is not shortened by heavy plate, and a Light archer's range bonus doesn't extend a sword swing.

**Effective Range** (visible on the character sheet):
- Ranged weapon: `Weapon Range + hat Range Modifier + body Range Modifier` (in tiles)
- Melee weapon: `Weapon Range` (Range Modifiers ignored)

Displayed as tiles in the UI.

**Visuals (in-run):** Hat, Body, and Weapon are rendered on the character model. Ring has no visual representation.

Heavy suits close-range builds taking hits; Light suits ranged builds that kite; Medium suits mixed or flexible builds. Mixing categories (e.g. Heavy hat, Light body) produces a middle-ground Effective Range for ranged weapons.

#### Ring

Rings grant **physical resistance (%)**. No category, no equipment tags — any character can equip any ring, and rings can only socket Equipment Augments with no tag requirement (universal augments). Tier is the only progression axis: higher-tier rings give higher resistance.

#### Starter Gear

Each character starts with one item per slot, matched to their archetype:

| Archetype | Weapon         | Hat            | Body           | Ring          | Skill slot 1        | Slots 2 & 3 |
|-----------|----------------|----------------|----------------|---------------|---------------------|-------------|
| Warrior   | Sword (tier 1) | Heavy (tier 1) | Heavy (tier 1) | Ring (tier 1) | Strike (no augment) | Empty       |
| Rogue     | Bow (tier 1)   | Light (tier 1) | Light (tier 1) | Ring (tier 1) | Strike (no augment) | Empty       |
| Mage      | Wand (tier 1)  | Medium (tier 1)| Medium (tier 1)| Ring (tier 1) | Strike (no augment) | Empty       |

New characters start with slot 1 filled and slots 2–3 empty. Filling them requires crafting additional skill items — the first craft is the natural next step after a new character's first run. Skills pre-equipped in slot 1 do not appear in the Skills inventory tab. All three starters use plain Strike with no augments — damage type and delivery are determined by the equipped weapon, not by pre-socketed augments. Crit is a bow identity (the bow's +8% crit identity bonus), not a Rogue identity; magic damage is a wand identity, not a Mage identity.

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
- **Character Screen** → full management hub for the selected character. Two tabs: **Loadout** (default) and **Sigils**. Start Run button at the bottom. Back button returns to Account Screen.
  - **Loadout tab** — two-column layout:
    - *Left/centre* — character name, archetype, stats, and equipped slots: Weapon / Hat / Body / Ring / Skill 1 / Skill 2 / Skill 3
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
- Left-click an **inventory item** → menu: **Modify**, **Delete**
- Left-click an **equipped slot (filled)** → menu: **Modify**, **Delete**
- Left-click an **equipped slot (empty)** → menu: **Craft New** (opens create list for that slot type), **Equip from inventory** (item picker filtered to slot type)
- Left-click an **open augment socket** → augment picker (compatible augments from inventory)
- Left-click a **filled augment socket** → menu: **Remove** (returns augment to inventory)

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

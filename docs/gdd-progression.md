# Game Design Document — Meta-Progression, Gear & UI

> Part of the GDD. See also `gdd-mechanics.md` for combat, skills, characters, and run structure.
> Living document — details will evolve as the game is playtested.

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

Exact stat differences per tier are TBD. Higher tier also unlocks more augment slots on skill and equipment items (see Skill Augments, Equipment Augments).

---

### Gear Slots

Characters can equip up to 4 gear items (one per gear slot) and up to 3 skill items (one per skill slot). All items persist between runs. Each slot has a distinct role:

| Slot      | Role                                                             | Progression axis                    |
|-----------|------------------------------------------------------------------|-------------------------------------|
| Weapon    | Skill synergy — flat damage bonus to skills of matching affinity | Tier → larger bonus                 |
| Hat       | Survival — HP, Speed, damage reduction (%) by category          | Tier → better stats within category |
| Body      | Survival — HP, Speed, damage reduction (%) by category          | Tier → better stats within category |
| Ring      | Mitigation — physical resistance (%)                             | Tier → higher resistance            |
| Skill ×3  | Active/passive ability used during a run                         | Tier → stronger effect / lower cooldown |

#### Skill Slots

Three skill slots map directly to the 3 skill bar slots shown during a run. Whatever is equipped in skill slots 1–3 is what fires during the run.

The same skill item can be equipped in multiple slots simultaneously. Any archetype can equip any skill — there are no restrictions. v1 skills: **Strike** (`Melee`, `Attack`), **Arrow** (`Ranged`, `Attack`), **Bolt** (`Ranged`, `Magic`, `Spell`).

Skill items are crafted (see Skill Crafting tab) and equipped from the **Skills inventory tab**.

#### Skill Augments

> **PoE2 inspiration:** Skill Augments are the equivalent of PoE2 support gems — they socket directly into a skill and modify how it behaves. Tag compatibility is the only gate; there are no character or archetype restrictions.

Skill Augments are craftable items that socket into a skill item to modify it. A Skill Augment can only socket into a skill if the skill has at least one matching tag.

**Skill Augment slots per tier** — upgrading a skill unlocks deeper modification, not just bigger numbers:

| Skill tier | Skill Augment slots |
|------------|--------------|
| Common     | 1            |
| Uncommon   | 2            |
| Rare       | 3            |

- **Socketing:** choose a compatible Skill Augment from inventory and place it into an open slot on the skill item
- **Removing:** free, Skill Augment returns to inventory
- **Compatibility:** governed by tags — a Skill Augment declares which tags it requires; the skill must have at least one

**v1 Skill Augments:**

| Skill Augment | Requires tag | Effect |
|---------|-------------|--------|
| Splash  | `Melee`     | Hit damages a small area around the target |
| Pierce  | `Ranged`    | Projectile passes through enemies |
| Slow    | `Attack`    | Applies the Slow EoT on hit (see Effects over Time) |

Exact values (splash radius, slow %, apply chance, duration) are TBD.

**Crafting cost (v1):** every Skill Augment costs **1 Common material** to craft.

Skill Augments are crafted from the **Skill Crafting tab** and live in the **Augments inventory tab**.

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

Weapon tags intentionally reuse the skill affinity tag names — players already know `Melee`, `Ranged`, `Magic` from skills.

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

Equipment Augments are crafted from the **Equipment Crafting tab** and live in the **Augments inventory tab**.

#### Weapon

Weapons have an **affinity** tied to one or more skill tags. Equipping a weapon gives a flat damage bonus to all skills that share at least one matching tag. Weapons contribute no base damage — they enhance skills only. Weapons also carry equipment tags (matching their affinity) for Equipment Augment compatibility.

| Weapon type | Affinity / Equipment tag | Enhances                  |
|-------------|--------------------------|---------------------------|
| Sword       | `Melee`                  | Skills with Melee tag     |
| Bow         | `Ranged`                 | Skills with Ranged tag    |
| Wand        | `Magic`                  | Skills with Magic tag     |

Any character can equip any weapon. The affinity bonus incentivises pairing weapon with matching skills — but mixing is valid. A skill with multiple tags (e.g. `Ranged`, `Magic`) benefits from any weapon whose affinity tag it shares.

**Visuals (in-run):** Weapon is rendered on the character model.

#### Hat & Body

Hat and Body are the two armour equipment slots. Each piece has a **category** that defines its identity and its equipment tag. Category is fixed per item — crafting a higher-tier Heavy hat makes it stronger within that category, not a different category.

Any character can equip any category in any slot. Slots are independent — a character can mix freely (e.g. Heavy hat, Light body).

| Category | Equipment tag | HP       | Speed   | Damage Reduction |
|----------|---------------|----------|---------|------------------|
| Heavy    | `Heavy`       | High     | Penalty | Yes (%)          |
| Medium   | `Medium`      | Moderate | Neutral | —                |
| Light    | `Light`       | Low      | Bonus   | —                |

Stats above apply per piece — each slot contributes its category's stats independently.

**Visuals (in-run):** Hat, Body, and Weapon are rendered on the character model. Ring has no visual representation.

Heavy suits close-range builds taking hits; Light suits ranged builds that kite; Medium suits mixed or flexible builds.

#### Ring

Rings grant **physical resistance (%)**. No category, no equipment tags — any character can equip any ring, and rings can only socket Equipment Augments with no tag requirement (universal augments). Tier is the only progression axis: higher-tier rings give higher resistance.

#### Starter Gear

Each character starts with one item per slot, matched to their archetype:

| Archetype | Weapon         | Hat            | Body           | Ring          | Skill slots (all 3) |
|-----------|----------------|----------------|----------------|---------------|---------------------|
| Warrior   | Sword (tier 1) | Heavy (tier 1) | Heavy (tier 1) | Ring (tier 1) | Strike ×3 |
| Rogue     | Bow (tier 1)   | Light (tier 1) | Light (tier 1) | Ring (tier 1) | Arrow ×3  |
| Mage      | Wand (tier 1)  | Medium (tier 1)| Medium (tier 1)| Ring (tier 1) | Bolt ×3   |

These are default starter loadouts only — any archetype can equip any skill. Skills are pre-equipped in all 3 slots and do not appear in the Skills inventory tab.

Specific item names and exact stat values are TBD.

**Acquisition:** Gear is not dropped by enemies. New items come from crafting — each item has a recipe requiring a combination of materials (see Currencies).

**Item identity:** Each item is a unique instance with its own ID. Items **upgrade in-place** — tier increases on the existing item rather than producing a new one. The item's background colour updates to reflect its new tier (see Item Tiers).

**Inventory:** Crafted (unequipped) items go into the **account inventory** — a shared pool accessible by every character. The inventory has three tabs:

| Tab | Contents | Capacity |
|---|---|---|
| Equipment | Crafted gear (weapons, armour, accessories) | 50 items |
| Skills | Crafted skill items | 50 items |
| Augments | Crafted Skill Augments and Equipment Augments | 50 items |

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
    - *Equipment tab* — crafted gear (weapon, hat, body, ring), 50-item cap
    - *Skills tab* — crafted skill items, 50-item cap
    - *Augments tab* — crafted Skill Augments and Equipment Augments, 50-item cap
    - Clicking a filled slot opens a popup (Equip / Delete). Equipped items are not shown here — they live in the slots.
  - **Loadout tab** *(default)* — where the player assembles their build for the run: gear slot buttons (Weapon / Hat / Body / Ring) and skill slot buttons (Skill 1 / Skill 2 / Skill 3) showing equipped items. Called "Loadout" rather than "Equipment" to reflect that this is where you set up everything you're taking into a run — gear and skills together. Clicking an occupied slot: popup (Unequip / Delete). Clicking an empty slot: item picker filtered to that slot type.
  - **Equipment Crafting tab** — two sub-tabs:
    - *Create* — craft new equipment items and Equipment Augments from materials (costs 1 Common material each)
    - *Modify* — load an existing equipment item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material). Socketing an Equipment Augment into equipment: click an open augment slot on the item, pick a compatible Equipment Augment from the Augments inventory.
  - **Skill Crafting tab** — two sub-tabs:
    - *Create* — craft new skill items and Skill Augments from materials (costs 1 Common material each)
    - *Modify* — load an existing skill item into the slot; one **Upgrade** button to increase its tier (costs 1 Common material). Socketing a Skill Augment into a skill: click an open augment slot on the skill item, pick a compatible Skill Augment from the Augments inventory.
  - **Sigils tab** — visible, empty (reserved for future sigil system)
  - All five tabs are always visible; empty tabs are not locked or greyed out
  - Back button returns to Account Screen
- **Run results overlay** → shown at run end; return button goes back to Character Screen
- **Pause menu** — ESC during a run; second ESC or Resume button closes it; run is paused while open
  - **Resume** button — closes menu, run continues
  - **End Run** button — exits immediately to character screen; all progress from this run is discarded (level, XP, coins, crafting materials). Warning text alongside: *"All progress from this run will be lost."*
  - No confirmation step — warning text is the friction

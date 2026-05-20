# Game Design Document — godot1

> Living document — details will evolve as the game is playtested.

## Overview

A top-down auto-attack horde survival game. The player takes a persistent character into runs against escalating enemy waves. Killing enemies earns XP; levelling up permanently improves the character. The character's level, XP, and stats carry over between runs — each run makes the character meaningfully stronger. Between runs, coins earned fund permanent upgrades. The game is character-driven: you play to grow your character, not to build a single run's loadout.

---

## Core Mechanics

### Movement
- Top-down, 8-directional
- [TBD] Speed, acceleration values

### Combat
- **Auto-attack only** — no manual firing
- Single weapon per run, upgradeable via level-up choices
- Weapon targets nearest enemy automatically on a cooldown timer

### Interaction
- Collectibles auto-collected on contact (XP gems, coins, health)
- [TBD] Any interactive objects (chests, shrines, etc.)

---

## Characters

Every run requires a character. Characters are created by the player, persist between runs, and grow over time. A player may own multiple characters simultaneously and delete any they no longer want.

### Character Archetypes

| Archetype | Max HP | Speed | Base Damage | Playstyle         |
|-----------|--------|-------|-------------|-------------------|
| Warrior   | 150    | 170   | 20          | Tanky brawler     |
| Rogue     | 80     | 260   | 15          | Fast and fragile  |
| Mage      | 100    | 200   | 35          | Glass cannon      |

### Character Lifecycle
1. **Create** — player picks a name and archetype
2. **Select** — choose a character from the roster before a run
3. **Run** — character starts at their saved level; every new level gained during the run is permanent
4. **Grow** — level, XP, and coin bank carry over between runs; permanent stat bonuses can be purchased between runs
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
- Killing enemies drops **XP gems**
- Collecting XP gems fills the XP bar
- On level up: automatic permanent bonuses are applied — **+5 Max Health, +1 Weapon Damage**
- Level and XP within the current level persist when the run ends; the character picks up exactly where they left off
- No popup or pause — levelling up is seamless

### Enemy Drops
| Drop        | Effect                              | Drop chance  |
|-------------|-------------------------------------|--------------|
| XP gem      | Feeds level-up bar                  | Common       |
| Coin        | Added to meta currency bank on run end | Uncommon  |
| Health pickup | Restores HP instantly             | Rare         |

---

## Run Structure

- **Duration:** Fixed time limit (~20-30 min) [TBD exact value]
- **Difficulty scaling:** Enemy count, speed, and variety increase over time
- **Run end conditions:**
  - Player dies → run over, partial rewards
  - Timer expires → final boss spawns → defeat boss to win run
- **Run rewards:** Coins earned carry over to meta layer

---

## Enemies

| Type    | Behavior                    | Notes      |
|---------|-----------------------------|------------|
| [TBD]   | Chase player (basic)        | Common     |
| [TBD]   | Ranged attacker             | Uncommon   |
| [TBD]   | Boss (end of run)           | Unique     |

Difficulty scales with elapsed time — more enemies, faster spawns, tougher variants.

---

## Meta-Progression (Between Runs)

### Character-Level Progression
Each character independently accumulates permanent stat bonuses between runs:

| Bonus           | Effect                      |
|-----------------|-----------------------------|
| +Max Health     | Raises HP ceiling for runs  |
| +Speed          | Increases movement speed    |
| +Damage         | Increases weapon damage     |

Bonuses are funded by currency earned in runs and are permanent to the character.

### Gear Slots
Equipment persisted between runs. Equipped before starting a run.

| Slot       | Effect type          |
|------------|----------------------|
| Weapon     | Determines starting weapon / attack style |
| Armour     | Defence / HP modifiers |
| Accessory  | Passive ability or stat modifier |

### Permanent Upgrades
Spend coins at a permanent upgrade shop (per-character). Examples:
- Max health increase
- Move speed increase
- Starting XP boost
- Coin magnet range

---

## UI / HUD

- Health bar
- XP bar + current level
- Coin counter (this run)
- Elapsed time / countdown
- [TBD] Minimap

### Menus
- **Main Menu** → title screen, Play button
- **Character Select** → list characters, create new (name + archetype), delete, start run
- Pause menu
- Run results / rewards screen
- Per-character upgrade screen (between runs)

---

## Win / Lose Conditions

| Condition        | Outcome                                  |
|------------------|------------------------------------------|
| Player HP = 0    | Run lost — partial coin rewards          |
| Timer expires    | Boss spawns — defeat to complete the run |
| Boss defeated    | Run won — full rewards                   |

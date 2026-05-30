# BALANCE.md

> Owned by the **Balancer** agent. All hardcoded game numbers live here as the single source of truth. Update this doc when tuning values; log code-side TODOs at the bottom.

---

## Player Base Stats

| Archetype | Max HP | Speed | Base Damage |
|-----------|--------|-------|-------------|
| Warrior   | 150    | 200   | 20          |
| Rogue     | 100    | 200   | 25          |
| Mage      | 100    | 200   | 35          |

---

## Level-Up Rewards (per level)

| Bonus         | Value |
|---------------|-------|
| Max HP        | +5    |
| Weapon Damage | +1    |

XP to level up: **20 XP** (flat, no scaling currently)

XP sources per kill:
- Kill XP (direct, no pickup): `1 × MapLevel`
- XP gem (pickup required): **5 XP**

---

## Run Structure

| Parameter      | Value              |
|----------------|--------------------|
| Run duration   | 5 minutes (300s)   |
| Test duration  | 5s (dev override)  |
| Map level      | 1 (default)        |

---

## Enemy Base Stats

| Type     | Unlocks | Speed | HP | Damage |
|----------|---------|-------|----|--------|
| Standard | 0:00    | 75    | 1  | 10     |
| Runner   | 1:00    | 110   | 1  | 8      |
| Tank     | 2:00    | 45    | 1  | 18     |

### Enemy Time Scaling (applied on top of base stats)

```
Speed    += 10 × elapsed_minutes
MaxHealth += 5 × floor(elapsed_minutes)
```

### Spawn Rate Scaling

```
spawn_interval = InitialInterval / (1 + elapsed_minutes × 0.5)
spawn_interval = max(spawn_interval, 0.3s)
```

Spawn radius around player: **350px**

---

## Drop Rates (on enemy death)

| Drop               | Chance | Value / Effect                          |
|--------------------|--------|-----------------------------------------|
| XP gem             | 100%   | 5 XP on pickup                          |
| Coin               | 25%    | 1 coin, added to run total              |
| Health pickup      | 10%    | Restores 15 HP on contact               |
| Crafting currency  | 20%    | 1 unit, added to run total (instant)    |

---

## Meta Upgrades (coin-funded, per character)

| Upgrade   | Bonus per tier | Max tiers | Cost per tier       |
|-----------|----------------|-----------|---------------------|
| +HP       | +10 Max HP     | 5         | 50 / 100 / 150 / …  |
| +Speed    | +10 Speed      | 5         | 50 / 100 / 150 / …  |
| +Damage   | +2 Damage      | 5         | 50 / 100 / 150 / …  |

---

## Gear Item Stats

| Item            | Slot      | HP  | Speed | Damage |
|-----------------|-----------|-----|-------|--------|
| Iron Sword      | Weapon    | —   | —     | +3     |
| Battle Axe      | Weapon    | —   | -15   | +6     |
| Enchanted Blade | Weapon    | +10 | —     | +2     |
| Leather Vest    | Armor     | +20 | —     | —      |
| Chain Mail      | Armor     | +40 | -10   | —      |
| Mage Robe       | Armor     | +15 | +15   | —      |
| Swift Ring      | Accessory | —   | +20   | —      |
| Vitality Charm  | Accessory | +30 | —     | —      |
| War Band        | Accessory | +10 | —     | +2     |

### Starter Gear by Archetype

| Archetype | Weapon          | Armor        | Accessory      |
|-----------|-----------------|--------------|----------------|
| Warrior   | Iron Sword      | Chain Mail   | War Band       |
| Rogue     | Iron Sword      | Leather Vest | Swift Ring     |
| Mage      | Enchanted Blade | Mage Robe    | Vitality Charm |

---

## Open TODOs

_Log balance-driven code changes here for the default session to pick up._

- [ ] XP-to-level scaling: currently flat 20 XP per level — consider ramping with level
- [ ] Run duration: hardcoded to 5s for testing, needs reset to 300s before playtesting
- [ ] MapLevel: hardcoded to 1, no map selection UI yet

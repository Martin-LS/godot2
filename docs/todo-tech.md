# Tech To-Do

Code, systems, UI, and art tasks only. Design decisions live in `docs/game-design-direction.md` and the GDD files.

---

## Code — Design Sync

- [x] Remove Adaptation Equipment Augment from implementation (cut from v1 design)
- [x] Skill slot count: HUD shows 1 slot (v1 design); `_skillCells` array stays at 3 for future expansion
- [ ] Rename BalanceConfig constants and WeaponController methods to match design names: Strike → EntityBurst, Cyclone → SelfChanneledTick, DamageAura → SelfDurationTick, Nova → SelfBurst. (Skill IDs in SkillRegistry already renamed; `DamageAuraReservation` in `BalanceConfig.Focus` is dead — remove it.)
- [ ] Add duplicate augment-type check to `SocketSkillAugment` and `SocketEquipmentAugment` in CharacterManager

## UI

- [ ] Ineffective augment combo warning — red/yellow exclamation on augment socket when the augment has no effect on the slotted skill (e.g. Pierce on a Self skill). Hover tooltip explains why.

## Systems / Features

- [ ] Map selection screen — currently one hardcoded arena; no selection or variety
- [ ] Craft New flow — not yet implemented (left-click empty skill/gear slot → craft new)
- [ ] Boss mechanic — run win condition triggers on timer expiry; boss is unimplemented

## Art / Assets

- [ ] Hollow Dark Forest assets — floor tile, tree trunk wall, wall corner (Blender); replace placeholder box geometry in DungeonGenerator
- [ ] Armour models — attachment offsets need tuning against current character proportions
- [ ] Weapon rotation fine-tuning — sword blade orientation may need a Blender tweak
- [ ] Cyclone animation — needs full-body spin; current partial blend looks wrong

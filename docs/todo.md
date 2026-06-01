# To-Do

Flat list. No priority order within sections — reorder as needed.  
Update this as tasks are completed or new work is identified.

---

## Visuals / Art

- [x] Armour colours — set distinct colours per tier; all three tiers use the same base model (armour_heavy.blend) recoloured: Heavy=iron dark, Medium=moss green, Light=ice blue. Hats scaled 1.1x so they wrap around the head.
- [ ] Idle animation — character freezes in T-pose (or last attack pose) when standing still. Add a subtle idle clip to `player.glb`
- [ ] Attack animation arm swing — current swing is subtle from top-down camera. Consider exaggerating UpperArm_R rotation in the attack clip
- [ ] Armour models — armour_light/medium/heavy glbs need review; chest/head attachment offsets may need tuning once armour is coloured and visible
- [ ] Enemy variety — only Skeleton in v1. GDD lists runner and ranged types as TBD

---

## Animation / Equipment

- [ ] Weapon rotation fine-tuning — sword blade currently oriented along bone -Z; may need a Blender rotation tweak depending on how it looks in-game during attack
- [x] Armour GLB split — armour_heavy/medium/light.blend each contain a combined hat+body mesh. Split each into two separate GLBs (hat_heavy.glb, hat_medium.glb, hat_light.glb, body_heavy.glb, body_medium.glb, body_light.glb), then update the hat/body path lookups in `PlayerController.cs`
- [ ] Bow and wand models — weapon_bow.glb and weapon_wand.glb need the same mesh-origin + orientation fix that was applied to weapon_sword (origins at grip, blade along correct axis)
- [ ] Rogue and Mage character models — only player.glb (Warrior) exists with rig + animation; other archetypes need models, rigs, and the same `run` / `attack` clips

---

## Gameplay / Balance

- [ ] Stat balancing — archetype multipliers all marked TBD in GDD; needs a tuning pass
- [x] Difficulty scaling — enemies currently kill a Level 10 character in ~20 seconds; too fast for testing. Tune spawn rate / HP / speed curves
- [ ] Coins — drop and accumulate but have no spend mechanic yet
- [ ] Manual skill activation — currently all skills auto-fire; GDD lists manual trigger as TBD
- [ ] Map level attribute — XP scaling by map level exists in GDD but no map selection screen or map level setting yet

---

## Systems / Features

- [ ] Pause screen — ESC is listed as pause in GDD controls but not implemented
- [ ] Boss mechanic — run win condition triggers when timer expires but boss is TBD
- [ ] Map selection screen — currently always the same flat arena
- [ ] Archetype defense system — Rogue dodge and Mage focus shield are future design (GDD future notes section)
- [ ] Higher-tier crafting materials — drop system only has common tier; rarer tiers TBD

---

## Tech / Polish

- [ ] UID warnings in log — `xp_shard.tscn`, `coin_pickup.tscn`, `health_pickup.tscn` all log invalid UID warnings on every run. Cosmetic but noisy
- [ ] Dev overlay — only has a Speed slider; consider adding god mode / invincibility toggle for testing
- [x] Delete orphaned `src/skills/AugmentData.cs` and `src/skills/AugmentRegistry.cs` — dead files superseded by the Support/SkillAugment system; nothing references them
- [x] Rename `SupportData.cs` → `SkillAugmentData.cs`, `SupportItemInstance.cs` → `SkillAugmentInstance.cs`, `SupportRegistry.cs` → `SkillAugmentRegistry.cs` — classes were renamed but filenames were not (C# doesn't require a match, but it's confusing)

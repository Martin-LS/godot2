# To-Do

Flat list. No priority order within sections — reorder as needed.  
Update this as tasks are completed or new work is identified.

---

## Visuals / Art

- [x] Armour colours — set distinct colours per tier; all three tiers use the same base model (armour_heavy.blend) recoloured: Heavy=iron dark, Medium=moss green, Light=ice blue. Hats scaled 1.1x so they wrap around the head.
- [x] Idle animation — breathing cycle added to player.glb (Chest rises 0.06 units, shoulders lift 3°, 40-frame loop). PlayerController plays "idle" when standing still instead of stopping.
- [x] Attack animation arm swing — UpperArm_R X scaled 1.5×, Z sweep channel added (−20°→+40° horizontal arc), Chest X scaled 1.5×
- [x] Cyclone VFX — spinning ring of elongated capsule particles (CapsuleMesh, radius=0.8, height=5) around the player; tangential accel + particle_flag_align_y so shards orient along the ring curve; anti-clockwise rotation driven by RotateY in WeaponController._PhysicsProcess; instant particle kill on release via Restart()+Emitting=false. Saved reference copy as effect1.tscn.
- [ ] Armour models — attachment offsets will need tuning against the new box character proportions (KayKit warrior retired). Chest/Head/Hand_R bone names match; positions differ.
- [ ] Enemy variety — only Skeleton in v1. GDD lists runner and ranged types as TBD

---

## Animation / Equipment

- [ ] Weapon rotation fine-tuning — sword blade currently oriented along bone -Z; may need a Blender rotation tweak depending on how it looks in-game during attack
- [ ] Cyclone animation — needs a full-body spin (not partial body blend). Legs running + upper body spinning 360° looks wrong. VFX ring is done (see Visuals). Animation architecture TBD: likely a separate blend layer that overrides the full body while IsChanneling. Also decide: when both Cyclone and Strike are auto-active simultaneously, does Strike OneShot interrupt the spin every 0.8s (alternating), or does Cyclone suppress Strike's animation while channeling?
- [x] Weapon attach-bone system — bow attaches to `Hand_L` (left hand holds bow, right arm draws); sword/wand attach to `Hand_R`. `AttachWeaponToSkeleton` takes a `boneName` param; call site passes `Hand_L` for `bow_t1`, `Hand_R` otherwise.
- [x] Review & approve all 3 animations in Blender before exporting GLB: run (frames 1–40), melee_atack (1–40), range_atack (1–40). All saved in player.blend. DO NOT export until approved.
- [x] Export player.glb from player.blend after animation approval (use Blender MCP, export_nla_strips=True)
- [x] idle animation — breathing cycle built in player.blend (5 bones, 40 frames: Chest X +3°, Spine X +1.5°, Head X +2°, UpperArm_L/R Z ±3°), exported to player.glb, looped via AnimationTree
- [x] Implement partial body blending in Godot AnimationTree — attack animations drive upper body only, run legs play underneath during combat movement
- [x] Attack animation synced to cooldown — dynamic TimeScale (`animLength / cooldown`); damage delayed to 35% of cooldown (windup frame) via timer in WeaponController
- [x] Combat facing — character faces nearest enemy while attack OneShot is active, even while moving
- [x] Armour GLB split — armour_heavy/medium/light.blend each contain a combined hat+body mesh. Split each into two separate GLBs (hat_heavy.glb, hat_medium.glb, hat_light.glb, body_heavy.glb, body_medium.glb, body_light.glb), then update the hat/body path lookups in `PlayerController.cs`
- [x] Bow orientation fix — bow geometry rotated 90° around Z in Blender so limbs span X (left-right) instead of Y (forward); now clearly visible from top-down camera
- [x] Wand model orientation — wand mesh vertices rotated +90° around X in Blender so rod/orb span forward (-Y Blender = Godot +Z = character facing direction); now visible from top-down camera
- [x] Rogue and Mage character models — all three archetypes now share `player_character.glb`, a custom box-geometry character with 17-bone rig and 4 animations (idle, run, attack_melee, attack_range). KayKit warrior model retired.

---

## Bugs

- [x] Channeled skill auto-activate behaves like an Active skill — fires, waits cooldown, fires again instead of holding `IsChanneling = true` continuously. Auto-activate on a Channeled slot should keep it channeling as long as there are enemies and Focus is available.
- [x] Channeled skill exclusivity — while a Channeled skill is active (`IsChanneling = true`), all Active skills (Strike, Nova) must be suppressed entirely (no damage, no animation). Auras are unaffected. The opening slot 1 skill gets one hit in before Cyclone takes over; after that Active skills are locked out until channeling stops. Fix together with the auto-activate bug above.
- [x] Channeled skill does not stop at 0 Focus — `IsChanneling` stayed true when `TrySpendFocus` failed; VFX kept spinning and skill bar stayed active. Fixed: `ProcessChanneledSlot` now sets `IsChanneling = false` when focus is exhausted.

---

## Gameplay / Balance

- [ ] Stat balancing — archetype multipliers all marked TBD in GDD; needs a tuning pass
- [x] BalanceConfig.cs — all numeric balance values extracted to `src/balance/BalanceConfig.cs`; covers weapons, armour, skills, EoTs, enemies, drops, pickups, archetypes, level-up coefficients
- [x] Difficulty scaling — enemies currently kill a Level 10 character in ~20 seconds; too fast for testing. Tune spawn rate / HP / speed curves
- [ ] Coins — drop and accumulate but have no spend mechanic yet
- [x] Manual skill activation — keys 1/2/3 fire skill slots; each slot has `AutoActivate = true` by default so skills still fire automatically on cooldown
- [ ] Map level attribute — XP scaling by map level exists in GDD but no map selection screen or map level setting yet
- [x] Weapon-adaptive skill system — skills have no weapon gate; delivery (Melee/Ranged) is determined by skill delivery tag, falling back to weapon PreferredDelivery for untagged skills (e.g. Strike)
- [x] Strike skill — universal starter skill; weapon-adaptive delivery, Attack tag, 0.8s cooldown, 1× physical damage; all 3 slots pre-filled at character creation

---

## Systems / Features

- [x] Skill data contract — new `SkillData` fields (Description, IsPrototype, TargetingShape, WindUp, DamagePattern, StackLimit, ZoneTracksEntity, Duration, TriggerRadius, ArmTime, TriggerCount), enums SkillTargetingShape/SkillDamagePattern, 4 skills renamed (strike→entity_burst, cyclone→self_channeled_tick, nova→self_burst, damage_aura→self_aura_tick), RecipeRegistry outputs updated, save migration entries added in CharacterManager.MigrateSkillId().
- [x] Targeting system runtime — `PlayerController.LockedTarget` + `TargetPosition` (Vector3). Mouse raycast to ground plane updates `TargetPosition` every frame; controller support ready (swap raycast for right-stick vector, nothing else changes). Entity skills→LockedTarget (Effective Range check); Position skills→mouse cursor clamped to skill Range (fires at boundary if out of range, never blocked); Self/Channeled→player position. LockedTarget auto-picks nearest enemy to cursor. Aim reticle (white ring) visible when a Position skill is slotted. `RecalculateEffectiveRange()` public for mid-run range buffs.
- [x] Prototype skills — library confirmed complete for v2 planning (covers all major ARPG archetypes). Full specs in gdd-mechanics.md § Base Skill Templates. Test order: Burst shapes first, then Entity targeting, then Stackable/Trap.
  - [x] Fixed-Zone-Burst — instant explosion at LockedTarget position; proves Position targeting resolves to enemy location not player
  - [x] Fixed-Zone-Tick — ticking zone at position; proves persistent zone with duration and tick damage
  - [x] Windup-Burst — 1.5s telegraphed delay then burst; proves wind-up mechanic
  - [x] Entity-Debuff — slow applied to locked target for 6s; proves Entity targeting with no damage output
  - [x] Tracked-Tick — ticking zone follows locked enemy; proves ZoneTracksEntity and entity death expiry
  - [x] Stackable-Zone — up to 3 independent ticking zones; proves StackLimit and oldest-despawn on cap
  - [x] Triggered-Zone-Burst — dormant trap fires once on enemy proximity; proves TriggerRadius/ArmTime/TriggerCount
- [x] Pause screen — ESC toggles PauseMenu (CanvasLayer, process_mode=Always); GetTree().Paused set on show/hide; Resume + End Run buttons; debug section (speed slider, range toggle) hidden in non-debug builds
- [ ] Boss mechanic — run win condition triggers when timer expires but boss is TBD
- [x] Map generation — single 24×24 KayKit dungeon arena; floor tiles, perimeter walls, corner pieces, scattered props (pillars, barrels, crates, torches); collision boundary; player spawns at centre; enemies spawn on floor tiles
- [x] Map generation rewrite — connector tile system; MapData (seed, biome, level, chunk count) generated at run start via RunConfig; DungeonGenerator builds 4–6 rooms as flat box patches connected by corridors; invisible walls on all room sides with corridor gaps; placeholder obstacle boxes (stumps, rocks, logs) scattered per room. KayKit assets removed from map.
- [ ] Hollow Dark Forest assets — floor tile, tree trunk wall, wall corner (Blender); replace placeholder box geometry in DungeonGenerator
- [ ] Enemy pathfinding — NavigationAgent3D; enemies currently walk directly toward player and collide with obstacles/walls
- [ ] Map selection screen — only one arena map; no selection or variety yet
- [ ] Archetype defense system — Rogue dodge and Mage focus shield are future design (GDD future notes section)
- [ ] Higher-tier crafting materials — drop system only has common tier; rarer tiers TBD

---

## UI / Character Screen

- [x] Character Screen redesign — GearCrafting and SkillCrafting tabs removed; Loadout tab is now two-column (character/gear left, inventory right); right-click equips/unequips; left-click shows context menu (Modify, Delete)
- [x] Modify panel popup — dark modal overlay; shows item name/tier, Upgrade button (costs 1 Common, disabled if max tier or insufficient materials), and augment socket rows (click empty slot → picker from inventory; click filled slot → removes augment)
- [x] Rarity border gap — two-panel technique: border ring (DrawCenter=false) + inset fill panel (8px offset); 3px dark gap visible between border and pale slate fill
- [x] PopupMenu theming — all popup menus use Iron & Slate colours, Exo 2 font, dark background via `NewStyledPopup()` factory
- [x] Craft New from empty slot — left-click on an empty gear or skill slot offers "Craft New" (slot-filtered recipe list) or "Equip from Inventory"; already implemented via `ShowEmptyGearSlotMenu` / `ShowCraftGearForSlotPanel`

---

## Hit Feedback

- [x] Screen flash — add full-screen `ColorRect` to `hud.tscn`; wire `PlayerHit` signal from `PlayerController`; tween alpha 0.3 → 0 over 0.15s in `Hud.cs`
- [x] Hit stop — add `Engine.TimeScale = 0` + real-time restore timer in `PlayerController.TakeDamage` (3 lines; `ignoreTimeScale: true` on the restore timer)
- [x] HP bars — floating bar above player (always on, `#A32D2D`) and enemies (on-hit, 2s timer, `#8C2E2E`); `WorldHud.cs` Node2D in CanvasLayer projects 3D positions to screen via `Camera3D.UnprojectPosition`
- [x] Damage numbers — floating text above hit entity; physical = bone white, magic = ice shimmer, crit = gold (+50% larger); individual per hit, fade over 0.8s; `DamageTaken` signal added to `EnemyController` and `PlayerController`; `isCrit` propagated through full damage pipeline (WeaponController, Projectile, EoT ticks)

---

## GDD v1 Verified Complete (2026-06-12)

- [x] Focus Shield — absorbs hits before HP (30% of Max Focus); passive regen; `ShieldChanged` signal wired to blue shield bar in HUD. All archetypes.
- [x] Equipment Augment behaviors — all 6 v1 behaviors implemented in `PlayerController.cs`: Retaliation, Fortify, Dash Reflex, Ghost Step, Mending, Adaptation.
- [x] Skill bar cooldown visualization — `FillMode=3` (bottom-to-top), fills over cooldown duration per slot; aura toggle highlights green border.
- [x] Main Menu — `MainMenu.cs` + scene; Play button navigates to Account Screen.
- [x] All 4 v1 skills implemented — Strike, Cyclone, Damage Aura, Nova.
- [x] All 5 v1 Skill Augments implemented — Splash, Pierce, Slow, Critical Strike, Magic Damage.

---

## Tech / Polish

- [x] UID warnings in log — fixed stale script UIDs in `xp_shard.tscn`, `coin_pickup.tscn`, `health_pickup.tscn`
- [x] Dev overlay — god mode toggle added; `PlayerController.GodMode = true` makes `TakeDamage` a no-op
- [x] Delete orphaned `src/skills/AugmentData.cs` and `src/skills/AugmentRegistry.cs` — dead files superseded by the Support/SkillAugment system; nothing references them
- [x] Rename `SupportData.cs` → `SkillAugmentData.cs`, `SupportItemInstance.cs` → `SkillAugmentInstance.cs`, `SupportRegistry.cs` → `SkillAugmentRegistry.cs` — classes were renamed but filenames were not (C# doesn't require a match, but it's confusing)
- [x] UI theme — custom Iron & Slate theme at `res://assets/ui/game_theme.tres`; covers panels, buttons, labels, tooltips, line edits, popups. Default font: Exo 2. Set via `gui/theme/custom`. Replaces Themey Spacey theme.
- [x] Fonts — 6 Google Fonts downloaded to `res://assets/fonts/` (Exo 2, Cinzel, Cinzel Decorative, EB Garamond, Almendra, Inter). Exo 2 active as UI default; others available for headings/lore text.
- [x] Panel borders — gold `#D4A017` 1px border on all PanelContainer, TabContainer, and Panel nodes via project theme.
- [x] Tooltip styling — `TooltipButton` (`src/ui/TooltipButton.cs`) renders a two-section custom tooltip: gold bold title on line 1, pale slate body on remaining lines. Applied to all gear/skill/augment buttons in CharacterScreen.

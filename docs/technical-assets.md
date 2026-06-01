# Technical Design Document — 3D Asset Pipeline

> Part of the technical docs. See also `technical-scene.md` for scene architecture and rendering decisions.
> This doc is the single source of truth for all custom 3D model authoring. Every character, enemy, and prop must conform to these rules so assets look like they belong to the same game.

---

## Visual Style

**Reference:** Crossy Road / voxel-art character style. Blocky, readable, deliberately low-detail. No organic curves — everything is built from rectangular primitives arranged to suggest form. The style reads well at small screen sizes and from the game's elevated camera angle.

| Property | Rule |
|---|---|
| Construction | Box/rectangular primitives only — no cylinders, spheres, or smooth surfaces |
| Shading | Flat-shaded (`shade_smooth` off). No normal maps. |
| Textures | None. Solid flat-colour materials only, one material per body region |
| Lighting response | Flat materials respond to scene lighting (not emission) — directional light gives depth |
| Scale reference | Player character imports with visuals node scale 9 in Godot (`visuals.Scale = Vector3(9,9,9)` in `PlayerController`). Author new characters to the same Blender scale as `player.blend` for consistent proportions. |

---

## Proportions

All measurements are relative to **head width = 1 unit**.

| Body Part | Width | Height | Depth |
|---|---|---|---|
| Head | 1.0 | 1.0 | 1.0 (cube) |
| Torso | 1.0 | 1.2 | 0.6 |
| Upper arm | 0.25 | 0.55 | 0.25 |
| Lower arm | 0.22 | 0.45 | 0.22 |
| Hand | 0.22 | 0.2 | 0.22 |
| Upper leg | 0.35 | 0.55 | 0.35 |
| Lower leg | 0.3 | 0.5 | 0.3 |
| Foot | 0.35 | 0.2 | 0.5 (longer forward) |

Head-to-body ratio: head ≈ 1.0 units, total height ≈ 4.0 units (chibi-leaning — readable from camera).

Arms hang slightly away from torso (0.05 gap). Legs are centred under hips, slight gap between them.

---

## Faces

Faces are painted onto the front face of the head box using a small inset darker-coloured box (or a flat plane mesh pressed into the surface).

| Feature | Shape | Colour |
|---|---|---|
| Face area | Slightly lighter rectangle on front of head, inset 0.05 | Skin tone + 10% lighter |
| Eyes | Two small square boxes (0.1 × 0.1 × 0.05), side by side | Near-black (`#1a1a1a`) |
| Mouth | Optional — thin rectangle below eyes | Near-black, or omit |
| No nose | — | — |

---

## Hair

Hair is a voxel cluster: multiple small cubes (0.15–0.2 unit) arranged to fill a volume sitting on top of and slightly overhanging the head. Hair does **not** need to be rigged — parent it as a static mesh to the head bone.

Hair volume sits ~0.05 above head top, overhangs sides by ~0.1, overhangs back by ~0.2.

---

## Colour Palette Per Character

Each character has its own palette. Record it here when authoring so future modifications match exactly.

| Character | Skin | Hair | Top | Bottom | Shoes |
|---|---|---|---|---|---|
| Player | `#f5c9a0` | `#d4a96a` | `#a8d8ea` (light blue) | `#d4e8c2` (light green) | `#f4a261` (warm orange) |
| Enemy (generic) | `#c8b8a2` | `#8b7355` | `#e8c4c4` (light red) | `#c4c4e8` (light purple) | `#b8d4b8` (light grey-green) |
| Enemy (skeleton) | `#e8e0d0` (bone) | — (no hair) | `#c8bfae` (bone dark, ribs/joints) | `#b0a898` (joint accent) | `#c8bfae` (flat foot) |

*(Placeholder palette — light flat colours across the board. Replace with final colour scheme once decided.)*

---

## Rig — Standard Bone Set

> **Status: Implemented for `player.glb`.** Enemy models are still unrigged static meshes. The spec below applies to all future humanoid characters.

Every humanoid character uses this exact bone hierarchy and naming. Do not deviate — animation sharing and code lookups depend on consistent names.

```
Root
└── Hips
    ├── Spine
    │   ├── Chest
    │   │   ├── Neck
    │   │   │   └── Head
    │   │   ├── UpperArm_L
    │   │   │   └── LowerArm_L
    │   │   │       └── Hand_L
    │   │   └── UpperArm_R
    │   │       └── LowerArm_R
    │   │           └── Hand_R
    ├── UpperLeg_L
    │   └── LowerLeg_L
    │       └── Foot_L
    └── UpperLeg_R
        └── LowerLeg_R
            └── Foot_R
```

- `Root` sits at world origin (0,0,0), Y-up
- `Hips` is the physical centre of mass, all locomotion drives from here
- All bones point along their local Y axis
- Weight painting: each body-part mesh is weight-painted 100% to its corresponding bone (no blending — blocky characters have no deformation zones)

---

## Animation Clips

> **Status: Partial — player only.** `run` (looping) and `attack` (one-shot) clips exist in `player.glb`. `AnimationPlayer` is wired in `PlayerController` — run plays while moving, attack triggers on `SkillFired`. `idle`, `walk`, `hit`, `death` are not yet authored. Enemy models have no animations. The table below is the full target spec.

These are the clip names the C# code will reference. Every character must have all clips that apply to their type. Names are case-sensitive.

| Clip | Frames | Loop | Description |
|---|---|---|---|
| `idle` | 0–59 | Yes | Subtle weight shift or gentle bob — can be near-static |
| `walk` | 60–99 | Yes | Arms swing opposite to legs, hips bob slightly |
| `run` | 100–139 | Yes | Faster walk — exaggerate arm/leg swing |
| `attack` | 140–179 | No | Weapon arm swings forward and returns |
| `hit` | 180–199 | No | Brief recoil — character rocks back |
| `death` | 200–239 | No | Falls or collapses — stays down at last frame |

All animations live as named Actions in Blender's NLA editor and are exported into the GLB in one pass.

Frame rate: **30 fps** (set in Blender scene before animating).

---

## Modular Equipment Visuals

The character model is a base body with equipment meshes added on top. Each equipped item maps to one or more meshes parented to the armature. No base body geometry is ever hidden — all equipment pieces are additive.

### Slot → Visual mapping

| Game slot | Visual pieces | Bone attached to |
|---|---|---|
| Armour (Heavy) | Chest plate (wide shoulders) + Full helm block | `Chest`, `Head` |
| Armour (Medium) | Leather vest (slight shoulder pads) + Hood/coif | `Chest`, `Head` |
| Armour (Light) | Thin padded vest + Cloth cap (low profile) | `Chest`, `Head` |
| No armour | Base body only | — |
| Weapon (Sword) | Short rectangular blade mesh | `Hand_R` |
| Weapon (Bow) | Bow frame mesh | `Hand_R` |
| Weapon (Wand) | Thin staff/rod mesh | `Hand_R` |
| Accessory | No visual representation | — |

Armour category drives both chest and hat together — equipping Heavy armour shows the heavy chest piece **and** the heavy helm. They are one asset (one GLB), not two independent pieces.

### Asset files

| Asset | File |
|---|---|
| Heavy armour | `assets/models/equipment/armour_heavy.glb` + `.blend` |
| Medium armour | `assets/models/equipment/armour_medium.glb` + `.blend` |
| Light armour | `assets/models/equipment/armour_light.glb` + `.blend` |
| Sword | `assets/models/equipment/weapon_sword.glb` + `.blend` |
| Bow | `assets/models/equipment/weapon_bow.glb` + `.blend` |
| Wand | `assets/models/equipment/weapon_wand.glb` + `.blend` |

### Runtime behaviour (Godot)

- On equip: instantiate the equipment GLB as a child of the character's skeleton node, attach to the named bone
- On unequip: remove that child node
- Equipment meshes carry no armature of their own — they are static meshes riding the character's bones
- All animations continue unchanged — equipment pieces move with the bones they're attached to

---

## Export Settings (Blender → GLB)

Run via `execute_blender_code` with these exact flags:

```python
bpy.ops.export_scene.gltf(
    filepath="<godot_project>/assets/models/characters/<name>.glb",
    export_format='GLB',
    export_apply=True,          # apply modifiers
    export_yup=True,            # Y-up for Godot
    export_animations=True,
    export_nla_strips=True,     # export all NLA Actions as separate clips
    export_frame_range=False,   # use NLA strip ranges, not scene range
    export_skins=True,
    export_all_influences=False,
    export_def_bones=False,
    export_materials='EXPORT',
    export_normals=True,
    use_selection=False,
    export_cameras=False,
    export_lights=False,
)
```

---

## File & Folder Conventions

```
assets/
  models/
    characters/
      player.glb
      player.blend
      enemy_<type>.glb
      enemy_<type>.blend
    equipment/
      weapon_<type>.glb
      weapon_<type>.blend
      armour_<category>.glb
      armour_<category>.blend
    props/
      <name>.glb
      <name>.blend
```

- One GLB per character, containing mesh + armature + all animation clips
- Blender source files (`.blend`) **are** committed alongside the GLB — they are the editable source
- Godot import settings (`.import` files) **are** committed — configure loop flags etc. once and keep them

---

## Godot Import Settings (per GLB)

After first import, configure via the Import dock:

| Setting | Value |
|---|---|
| Scale | 1.0 (model is authored to correct scale) |
| Animation → Storage | Built-in |
| Animation → FPS | 30 |
| Loop clips | `idle`, `walk`, `run` → **Loop** on; `attack`, `hit`, `death` → **Loop** off |
| Skins | On |

Commit the `.import` file after configuring — Godot regenerates it from source if deleted, which loses the loop settings.

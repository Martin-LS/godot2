# CLAUDE.md — godot1


## Project Overview

Top-down action RPG with horde combat (Diablo / Path of Exile 2 style). Godot 4.6, C#, Forward Plus renderer. 3D world with custom voxel-art style characters, perspective camera. Skills are manually activated by the player; each skill slot has an optional auto-activate toggle.

## Docs

- `docs/gdd-mechanics.md` — Game design: combat, skills, EoT, characters, enemies, run structure, win/lose, future notes
- `docs/gdd-map.md` — Game design: map generation, chunks, biomes, obstacle props, hollow dark forest
- `docs/gdd-progression.md` — Game design: meta-progression, gear, augments, crafting, currencies, UI/menus
- `docs/technical-map.md` — Architecture: map generation, MapData, RunConfig, DungeonGenerator algorithm, constants, extension points
- `docs/technical-scene.md` — Architecture: scene layout, scene flow, core systems table, signals, C# conventions, rendering, third-party tools
- `docs/technical-systems.md` — Architecture: data types, save format, crafting methods, EoT system, damage pipeline, enemy spawner, drop system
- `docs/technical-assets.md` — 3D asset pipeline: visual style, proportions, rig standard, animation clip names, Blender export settings, Godot import settings, KayKit library reference
- `docs/tech-tips.md` — Hard-won lessons: Blender↔Godot axes, mesh origins, BoneAttachment3D, AnimationPlayer quirks, bone naming, log file location
- `docs/color-scheme.md` — Iron & Slate color reference: full hex palette for world surfaces, UI, loot rarity, VFX, lighting, enemy coding, and biomes. Use this for all visual work.
- `docs/todo.md` — Pending work: visuals, animation, gameplay, systems, tech. Check and update each session.

**Read the relevant doc before making design or architectural decisions.**

At the start of every session: read `docs/todo.md`, note what's pending, and tick off anything completed during the session. Read `docs/tech-tips.md` before any 3D asset, animation, or bone work. Read `docs/color-scheme.md` before any visual, UI, VFX, or material work.

## Scope Rules

- **Bug fixes are strictly localised to the reported bug.** Change only the line(s) directly causing the issue. Do not refactor, rename, restructure, or clean up anything else — not in the same file, not in related files.
- **If a related flaw or issue is spotted while investigating, stop, report it to the user, and wait for instruction before touching it.**
- **Do not change data models, method signatures, or architecture as part of a bug fix unless explicitly asked.**
## Tools

- **Godot MCP Pro** is connected — use `mcp__godot-mcp-pro__*` tools to inspect/modify the live editor
- MCP tools are auto-approved globally
- Proactively use `play_scene`, `get_game_screenshot`, `get_output_log`, `get_editor_errors` to verify changes work before reporting done
- When debugging runtime behaviour (animation, physics, signals, gameplay logic): invoke `/godot-debug` via the Skill tool to read the log file before drawing conclusions — do not guess from code alone
- **Godot MCP Pro is the only way to do editor work.** Any task that involves creating or modifying nodes, scenes, particles, animations, materials, shaders, or any other editor resource must be done via MCP tools — never via GDScript workarounds, never by writing raw `.tscn`/`.tres` file content, never by constructing editor objects in C# `_Ready()`. If an MCP tool exists for the task, use it. If one does not exist, stop and discuss with the user before trying another approach.
- **After every `.tscn` or `.tres` file change, call `mcp__godot-mcp-pro__reload_project` immediately** so the editor picks up the change without prompting the user to reload manually.

## Blender Work

- **Always use Blender MCP** (`mcp__blender__execute_blender_code`) for any editing of Blender files — modifying animations, keyframes, meshes, or exporting GLBs. Never use headless Python scripts (`blender --background --python`) to edit or save blend files; this was the root cause of lost animations in the past.
- Running a Python script solely to open a `.blend` file is fine as a loading step. The rule is about edits.
- **Before executing any Blender MCP code, call `get_scene_info` as the very first action to verify connection.** If it fails or returns a default scene, stop immediately and tell the user to get Blender MCP working before writing any code. Do not fall back to headless scripts.

## Character Handedness

The player character is **right-handed** — weapons attach to **`Hand_R`** in code.

**Why:** `stickman.glb` uses standard Mixamo bone naming from the character's own perspective. `Hand_R` = character's anatomical right = visual right hand (screen-right from top-down camera). `Hand_L` = character's anatomical left = visual left. Confirmed at runtime: `Hand_L` produced a weapon on the left side of screen; `Hand_R` is correct.

Note: the old `player_character.glb` model had the opposite convention (`Hand_L` = visual right) due to an unusual Blender orientation. That model is retired — `stickman.glb` is now the player model.

**Weapon attachment is per-weapon-type:**
- Sword, wand → `Hand_R` (right hand; right-arm swing animation `melee_right_atack`)
- Bow → `Hand_L` (left hand holds the bow; left-arm sweep animation `melee_left_atack`)

- **Animation dominant arm: `Hand_R` channels (melee swing)**

## Blender Bone Rotation Direction (player.blend)

The character faces **-Y** in Blender (standard convention). Bone local X rotation direction depends on which way the bone points at rest — **downward and upward bones are opposite**:

| Bone group | Examples | Negative X | Positive X |
|---|---|---|---|
| Downward (-Z) | UpperLeg, LowerLeg, Foot, UpperArm, LowerArm, Hand | **Forward** (-Y world) | Backward |
| Upward (+Z) | Hips, Spine, Chest, Neck, Head | Backward | **Forward** |

**Before keying any new bone type, run the empirical tail-position test**: apply a +35° X test rotation in Blender, read `bone.tail` world Y, then undo. If Y went positive → positive X is backward → use negative X for forward motion. Never assume — always verify first.

**Do not export to Godot until the user has reviewed and approved the animation in Blender.**

## Animation

- **Always use Godot MCP Pro to set up AnimationTree** in the editor — use `create_animation_tree`, `add_state_machine_state`, `add_state_machine_transition`. Never construct AnimationTree nodes, states, or transitions in C# code.
- AnimationTree built dynamically in C# `_Ready()` silently breaks (`Travel()` fails, `GetCurrentNode()` never updates). Pre-creating the node in the scene via MCP/editor fixes this.
- C# code is limited to: setting `animTree.AnimPlayer = animTree.GetPathTo(animPlayer)`, setting `animTree.Active = true`, and calling `Travel()` for state changes. Use `GetCurrentNode()` for state checks — no manual bool flags.
- Set animation loop modes at runtime in C# (GLB import silently ignores `.import` subresource loop flags). Do not call `AnimationPlayer.Play()` directly when AnimationTree is active.

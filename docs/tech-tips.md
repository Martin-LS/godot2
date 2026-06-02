# Tech Tips — Hard-Won Lessons

Lessons learned the expensive way. Read before touching animations, bones, or 3D assets.

---

## Blender → Godot coordinate system

| Blender | Godot (after Y-up GLB export) |
|---|---|
| +X | +X |
| +Y (forward) | -Z (forward) |
| +Z (up) | +Y (up) |

**Rule**: if a mesh looks sideways or vertical in Godot but correct in Blender, the axes are wrong.  
**Fix**: rotate the mesh in Blender to compensate, then re-export. Do not fix with code transforms.

---

## Blender mesh origins must be at the grip/pivot point

When you export a mesh as GLB, Godot creates a node at the object's **origin**, not at the mesh's geometric center. If the origin is at world `(0,0,0)` but the mesh geometry is at `(-2, 0, 0)`, the Godot node will be at `(0,0,0)` but the mesh will render 2 units to the side.

**Fix in Blender**: `Object → Set Origin → Origin to Geometry` (or manually place origin at the intended attach point, e.g. the sword grip).  
**Check in Godot**: print `MeshInstance3D.get_aabb().position` — if it's far from `(0,0,0)` the origin is wrong.

---

## BoneAttachment3D: the child node inherits the bone's full rotation

A `BoneAttachment3D` child node's world axes = bone's local axes in world space. The mesh must be oriented in **bone-local space**, not world space.

For `Hand_R` bone (Godot rest pose):
- Bone local X = world right `(1,0,0)`
- Bone local Y = world down `(0,-1,0)`
- Bone local Z = world forward `(0,0,-1)`

A sword blade along the child node's local **-Z** will map to world **+Z** (backward, toward camera). Orient the blade along local **+Z** to point forward.

**Don't fight bone rotation with position-only tracking** — that removes rotation from the attachment entirely and the weapon won't swing during animation.

---

## Bone names get `_2` suffix when a mesh node has the same name

Godot's GLB importer appends `_2` to a bone name if a `MeshInstance3D` in the same scene shares that name (e.g. a mesh named `Hand_R` causes the bone to be renamed `Hand_R_2`).

`Skeleton3D.FindBone("Hand_R")` returns -1 in this case. Always use a fallback:

```csharp
private static int FindBone(Skeleton3D skeleton, string name)
{
    int idx = skeleton.FindBone(name);
    return idx >= 0 ? idx : skeleton.FindBone(name + "_2");
}
```

---

## AnimationPlayer autoplay survives `Stop()` in `_Ready()`

Godot re-triggers autoplay via a deferred notification after `_Ready()` completes. Calling `_animPlayer.Stop()` directly in `_Ready()` has no effect.

**Fix**:
```csharp
_animPlayer.Autoplay = "";           // clear the property
_animPlayer.CallDeferred("stop");    // runs after all deferred notifications
```

Also set loop mode at runtime — the `.import` file `_subresources` loop flags are silently ignored:
```csharp
_animPlayer.GetAnimation("run").LoopMode    = Animation.LoopModeEnum.Linear;
_animPlayer.GetAnimation("attack").LoopMode = Animation.LoopModeEnum.None;
```

---

## AnimationTree must be set up in the Godot editor, not in C# `_Ready`

Creating an `AnimationTree` dynamically in C# `_Ready()` causes `Travel()` and `Start()` to silently fail — `GetCurrentNode()` never updates from the initial state.

**Fix**: Create the `AnimationTree` node in the Godot editor (or via MCP). In `_Ready`, only configure its `AnimPlayer` path and set `Active = true`:

```csharp
var animTree = GetNodeOrNull<AnimationTree>("AnimationTree");
if (animTree != null)
{
    animTree.AnimPlayer = animTree.GetPathTo(animPlayer); // animPlayer found via FindChild
    animTree.Active = true;
    _smPlayback = (AnimationNodeStateMachinePlayback)animTree.Get("parameters/playback");
}
```

When the AnimationTree node pre-exists in the scene, `Travel()` works correctly and `GetCurrentNode()` reflects the live state.

**Setting AnimPlayer path at runtime**: When the AnimationPlayer is inside a dynamically-loaded GLB, use `animTree.GetPathTo(animPlayer)` to resolve the correct path — do not hardcode it.

**State machine setup** (via MCP `add_state_machine_state` / `add_state_machine_transition`):
- States: `idle`, `run`, `attack`
- `Start → idle`: advance_mode=auto
- `idle ↔ run`: advance_mode=enabled (Travel-driven)
- `idle → attack`, `run → attack`: advance_mode=enabled
- `attack → idle`: switch_mode=at_end, advance_mode=auto (auto-returns when clip finishes)

**C# usage**:
- `_PhysicsProcess`: if current != "attack" → Travel("run") or Travel("idle")
- `OnSkillFired`: Travel("attack")
- No bool flag needed — state machine tracks state

---

## Godot log file location (Windows)

```
%APPDATA%\Godot\app_userdata\<project_name>\logs\godot.log
```

Enable in Project Settings: `debug/file_logging/enable_file_logging = true`.

The MCP output panel (`get_output_log`) sometimes lags or shows stale data. Read the file directly via PowerShell for reliable results:

```powershell
Get-Content "$env:APPDATA\Godot\app_userdata\godot1\logs\godot.log" | Select-Object -Last 100
# or filter:
Get-Content "$env:APPDATA\Godot\app_userdata\godot1\logs\godot.log" | Select-String "ANIM"
```

Use the `/godot-debug` skill (`.claude/commands/godot-debug.md`) to automate this.

---

## GLB reimport after Blender re-export

After re-exporting a `.glb` from Blender, Godot caches the old version. The game will run with the old mesh until you force a reimport.

**Fastest method** (via MCP):
```gdscript
var fs = EditorInterface.get_resource_filesystem()
fs.scan()
fs.reimport_files(["res://assets/models/equipment/weapon_sword.glb"])
```

Alternatively delete the `.import` file and let Godot regenerate on next editor focus.

---

## BoneAttachment3D vs `GetBoneGlobalPose` position tracking

| | BoneAttachment3D | Per-frame position tracking |
|---|---|---|
| Position | ✅ follows bone | ✅ follows bone |
| Rotation | ✅ follows bone | ❌ not applied |
| Weapon swings during attack | ✅ | ❌ |
| Complexity | Low | Medium |
| When to use | Equipment, always | Never (was a workaround) |

**Use `BoneAttachment3D` for all equipment.** Position-only tracking was a dead-end workaround for a mesh-origin problem that should be fixed in Blender instead.

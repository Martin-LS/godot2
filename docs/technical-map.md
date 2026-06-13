# Technical Design Document — Map Generation

> Part of the technical docs. See also `gdd-map.md` for design intent, biomes, and chunk types.
> This doc covers the runtime architecture: data flow, generator algorithm, file locations, and extension points.

---

## Overview

Maps are generated at run start from a `MapData` object. `MapData` is created in the character screen, stored in a static `RunConfig` holder, and consumed by `DungeonGenerator` when the run scene loads. Every random decision in the generator is driven by `MapData.Seed`, making any map reproducible from its seed.

---

## Data Flow

```
CharacterScreen (Start Run pressed)
  → MapData.GenerateRandom()        // picks seed, biome, level, chunk count
  → RunConfig.Pending = mapData     // static holder survives scene change
  → ChangeSceneToFile("main.tscn")

DungeonGenerator._Ready()
  → RunConfig.Pending               // reads MapData; falls back to random if null
  → new Random(mapData.Seed)        // all RNG seeded from here
  → builds rooms, corridors, obstacles
```

`RunConfig.Pending` is intentionally not cleared after reading — it stays available for any other system in the run scene that needs map parameters (e.g. enemy spawner adjusting spawn count by map level).

---

## Key Classes

### `MapData` — `src/world/MapData.cs`

| Property | Type | Description |
|---|---|---|
| `Seed` | `int` | Drives all random decisions in the generator |
| `Biome` | `MapBiome` | Determines visual theme and future asset library |
| `Level` | `int` | Map difficulty; feeds into XP scaling and future modifiers |
| `ChunkCount` | `int` | Number of rooms to generate (v1: 4–6) |

`MapData.GenerateRandom(level)` creates a new instance with a random seed via `System.Random` (not Godot's RNG, so it works before the scene is loaded).

### `MapBiome` — `src/world/MapBiome.cs`

Enum of available biomes. v1: `HollowDarkForest` only.

### `RunConfig` — `src/world/RunConfig.cs`

Static class with a single `Pending` property. Bridges the scene change — set before `ChangeSceneToFile`, read in `DungeonGenerator._Ready()`.

### `DungeonGenerator` — `src/world/DungeonGenerator.cs`

`Node3D` placed in `main.tscn`. Builds the entire map in `_Ready()`. Exposes:
- `SpawnPosition` — world position of the player spawn point (centre of room 0)
- `GetSpawnPointNear(reference, minDist)` — returns a random room centre at least `minDist` away; used by `EnemySpawner`

---

## Generator Algorithm

### Room placement

Rooms are tracked on an integer grid. Each grid cell is `GridStep` world units apart:

```
GridStep = RoomSize + CorridorLength  (currently 200 + 100 = 300 world units)
```

1. Place spawn room at grid `(0, 0)`
2. For each additional room up to `ChunkCount`:
   - Shuffle existing rooms and try each in random order
   - For each room, shuffle the 4 cardinal directions and try each
   - If the neighbour grid cell is unoccupied, place a new room there and record a corridor
3. Repeat until target count reached or no valid placements remain

Room world centre: `(gx × GridStep, 0, gz × GridStep)`

Corridor world centre: midpoint between the two connected room centres

### Floor patches

Each room and corridor becomes a flat `BoxMesh` (`MeshInstance3D`) plus a matching `CollisionShape3D` on a shared `StaticBody3D`. All floor patches share one `StandardMaterial3D` (the biome floor colour).

### Perimeter collision

After all patches are placed, the axis-aligned bounding box of all rooms is computed. Four invisible `StaticBody3D` wall boxes are placed just outside the bounds to keep players and enemies inside the map.

### Obstacle scatter

The spawn room (grid `0, 0`) is always kept clear. Each other room gets a random scatter of placeholder obstacles within 65% of the room radius:

| Obstacle | Mesh size (world units) | Material |
|---|---|---|
| Dead tree stump | 18 × 55 × 18 | `#2e1008` |
| Boulder cluster | 38 × 28 × 38 | `#3a3028` |
| Fallen log | 15 × 18 × 90 | `#3c2818` |

Each obstacle has its own `StaticBody3D` + `BoxShape3D` for collision. Logs are placed at a random Y rotation.

---

## Constants (DungeonGenerator)

| Constant | Value | Notes |
|---|---|---|
| `RoomSize` | 200f | Square side length in world units |
| `CorridorWidth` | 60f | Narrow dimension of a corridor |
| `CorridorLength` | 100f | Long dimension of a corridor (the gap between rooms) |
| `GridStep` | 300f | Distance between room centres (RoomSize + CorridorLength) |
| `FloorThick` | 2f | Height of floor box mesh |
| `WallHeight` | 200f | Height of invisible perimeter collision walls |

All values are placeholder — tweak freely once the map feels right to play.

---

## File Locations

```
src/world/
  MapBiome.cs          — biome enum
  MapData.cs           — map parameters; GenerateRandom() factory
  RunConfig.cs         — static Pending holder for scene-change handoff
  DungeonGenerator.cs  — Node3D; builds map in _Ready()
```

---

## Extension Points

### Adding real chunk assets (future)

When Blender chunk assets are ready, `DungeonGenerator` swaps the procedural `BoxMesh` patches for instanced `PackedScene` chunks. The room grid and corridor logic stays identical — only the rendering step changes.

Each chunk asset will have tagged connector slots (position + facing direction) baked into the scene. The generator matches connector facings when snapping chunks together, replacing the current fixed `GridStep` offset with connector-aware positioning.

### Adding biome asset libraries (future)

`MapBiome` drives which asset library the generator loads. Add a switch on `mapData.Biome` in `DungeonGenerator._Ready()` to select the correct floor material, wall scenes, and obstacle prop scenes.

### Making maps craftable (future)

`MapData` is designed to eventually be a craftable item. Map affixes (enemy density, drop bonuses, environmental hazards) map directly to future `MapData` properties. The generator reads `RunConfig.Pending` regardless of how `MapData` was created — hand-generated or player-crafted, the flow is identical.

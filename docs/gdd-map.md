# Game Design Document — Maps

> Part of the GDD. See also `gdd-mechanics.md` for run structure, enemies, and map attributes.
> Living document — details will evolve as the generator and chunk library are built out.

---

## Overview

Maps are the arenas where runs take place. Each map is procedurally assembled from pre-authored chunks at run start, producing a unique layout every run. The map's biome determines the visual theme, chunk library, obstacle set, and future enemy set.

---

## Map Attributes

| Attribute | Description |
|---|---|
| Map Level | Scales kill XP reward — killing an enemy grants `1 XP × map level` directly, on top of any XP Shard the enemy drops |

More attributes will be added in future (e.g. enemy density modifiers, environmental hazards, drop bonuses).

---

## Procedural Generation

Maps are assembled from pre-authored **chunks** — floor sections with defined connector slots on their edges. The generator stitches chunks together at run time to produce a unique layout each run.

### Chunks

A chunk is a pre-authored asset: a floor section with **connector slots** tagged on its edges. A connector slot defines a position and facing direction — "this edge has an opening facing north." The generator matches open connectors between chunks and snaps them together at runtime.

**v1 chunk set:**

| Chunk | Connectors | Description |
|---|---|---|
| Spawn Room | 2–4 sides | Square open area; player always starts at centre |
| Corridor | 2 opposite sides | Narrow passage connecting two rooms |
| Arena Room | 2–4 sides | Larger open combat space |
| Dead End | 1 side | Caps any open connector that has no matching chunk |

### Generation Algorithm

1. Place the **Spawn Room** at world centre
2. For each open connector, find a chunk from the library whose connector facing matches
3. Snap it on, mark both connectors used
4. Repeat until target chunk count is reached (v1: 4–6 chunks)
5. Seal remaining open connectors with a Dead End or wall cap
6. Scatter obstacles within each chunk per its density zone rules

### Obstacle Density Zones

Each chunk is divided into three zones. Scatter rules differ per zone:

| Zone | Area | Obstacle density |
|---|---|---|
| Inner | Central ~30% | Low — keeps combat space open |
| Mid | Middle ring | Medium — main placement area |
| Outer | Outer ~20% | High — dense toward the boundary |

A fixed clear radius around the player spawn point in the Spawn Room is always obstacle-free.

---

## Obstacle Props

Obstacles are scattered by the generator — not baked into chunk geometry. All block both player and enemy movement.

| Prop | Notes |
|---|---|
| Dead tree stump | Tall vertical silhouette |
| Boulder cluster | 1–3 overlapping rocks |
| Fallen log | Elongated — creates natural lanes |

---

## Biomes

Each biome has its own chunk library, obstacle set, and visual theme. The generator selects a biome at run start and uses only that biome's assets.

| Biome | Status | Notes |
|---|---|---|
| Hollow Dark Forest | v1 — first implementation | Dark earth floor, tree trunk walls, dead wood props |
| Iron Crypts | Future | Stone dungeon theme — replaces current placeholder dungeon |

### Hollow Dark Forest

Dead forest clearing ringed by ancient trees packed so dense they form a natural wall. No torchlight — the only light is cold moonlight and whatever ambient glow the props provide.

**Palette:**

| Material | Face | Hex |
|---|---|---|
| Dead Earth (floor) | Shadow | `#1E1A14` |
| | Base | `#2E2618` |
| | Highlight | `#403820` |
| Dark Bark (walls) | Shadow | `#1A1210` |
| | Base | `#2E2010` |
| | Highlight | `#4A3020` |
| Root accent | — | `#5A4020` |
| Moss accent | — | `#3A4830` |

**Assets:**

| Asset | Type | Status |
|---|---|---|
| Floor tile | Chunk floor | Pending |
| Tree trunk wall | Boundary wall | Pending |
| Tree trunk corner | Boundary corner | Pending |
| Dead tree stump | Obstacle prop | Pending |
| Boulder cluster | Obstacle prop | Pending |
| Fallen log | Obstacle prop | Pending |

---

## Future Scaling

- Add more chunk shapes for layout variety
- Add guaranteed anchor chunks — boss room, shrine, objective
- Generate a main path first (spawn → boss), then fill branches
- Map selection screen — player picks biome before a run
- Per-biome enemy sets — different enemies favour different biomes

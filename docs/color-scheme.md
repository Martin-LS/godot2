# Iron & Slate — Color Scheme Reference

> Color system for a voxel-geometry ARPG (Diablo/PoE style). All colors are hardcoded hex. Use this file as a reference when generating materials, textures, UI elements, lighting, VFX, or any visual asset.

---

## Design Philosophy

- World surfaces are **desaturated and cool-toned** — stone, metal, ash, dirt
- Accent colors are **warm and rare** — gold for loot/progression, red for danger, amber for fire
- The environment should never fight with the UI; muted world = readable HUD
- Voxel geometry carries visual interest through **material variation**, not color saturation
- Players should be trained to react to **gold** as a signal (rare loot, level-up, key events)

---

## Core Palette

### Backgrounds & World Surfaces

| Name | Hex | Usage |
|---|---|---|
| Void Black | `#0E1114` | Deepest shadow, caves, void zones |
| Iron Black | `#181C1F` | Base UI background, deep environment |
| Deep Slate | `#1E252B` | Dungeon floors, dark stone |
| Surface Slate | `#252C32` | UI panels, raised stone, wood in shadow |
| Stone | `#3A4650` | Mid-tone stone, cobblestone, rough walls |
| Worn Stone | `#4A5560` | Cracked rock, old masonry, rubble |
| Ash Grey | `#6B8090` | Aged wood, weathered surfaces, fog |
| Pale Slate | `#8AA0AE` | Dusty surfaces, UI secondary text, fog mid |
| Mist | `#B0C4CE` | High-altitude fog, distant terrain, sky base |
| Bone White | `#E8DCC8` | Primary UI text, parchment, skulls, moonlight |
| Off White | `#F2EDE3` | Brightest highlights, sunlit stone edges |

### Gold — Loot, Progression & Key UI

> Reserved exclusively for rare items, XP bars, quest markers, and legendary effects. Do not use on common world surfaces.

| Name | Hex | Usage |
|---|---|---|
| Deep Gold | `#6B4F08` | Shadow on gold surfaces, darkest gold shadow |
| Dark Gold | `#8C680A` | Gold in shadow, underside of coins |
| Gold | `#D4A017` | Primary rare loot color, XP bar, quest markers |
| Bright Gold | `#E8B820` | Gold item glow, highlighted rare drops |
| Light Gold | `#F2CC5A` | Gold UI accents, shiniest surface highlights |
| Pale Gold | `#F7E09A` | Brightest gold specular, legendary item shimmer |

### Red — Health, Danger & Blood

| Name | Hex | Usage |
|---|---|---|
| Deep Crimson | `#3A0E0E` | Dried blood on voxels, darkest shadow |
| Dark Red | `#5C1A1A` | Health bar (critical low), shadowed blood |
| Muted Red | `#8C2E2E` | Enemy health bars, damage indicators |
| Danger Red | `#A32D2D` | Player health bar fill, trap zones |
| Bright Red | `#C43030` | Active damage, hit flash |
| Blood Red | `#D44040` | Fresh blood FX, wound effects |
| Alert Red | `#E85050` | UI critical warning, screen damage vignette |

### Amber — Fire, Torches & Heat

| Name | Hex | Usage |
|---|---|---|
| Char | `#3D1A06` | Burnt wood blocks, deep fire shadow |
| Dark Ember | `#6B3010` | Ember cores, scorched stone |
| Ember | `#A84E1A` | Torch base light, fire shadow |
| Flame | `#C96820` | Mid-flame, fire VFX |
| Bright Flame | `#E88A28` | Torch light, fire highlight |
| Fire Tip | `#F5A830` | Flame tips, brightest fire |
| Heat Shimmer | `#FACCAA` | Heat haze, fire light on pale surfaces |

---

## Material Palette (Voxel Blocks)

These are the actual block face colors for geometry. Each material has a **shadow**, **base**, and **highlight** face for voxel-style lighting.

### Stone

| Face | Hex |
|---|---|
| Shadow | `#252C32` |
| Base | `#3A4650` |
| Highlight | `#4A5560` |

### Granite / Dark Rock

| Face | Hex |
|---|---|
| Shadow | `#1E252B` |
| Base | `#2E3840` |
| Highlight | `#3C4C58` |

### Sandstone

| Face | Hex |
|---|---|
| Shadow | `#4A3C28` |
| Base | `#7A6444` |
| Highlight | `#A08C6A` |

### Iron Ore Vein

| Face | Hex |
|---|---|
| Shadow | `#2A2E32` |
| Base | `#4A4E52` |
| Highlight | `#787C80` |
| Vein accent | `#A0A4A8` |

### Gold Ore Vein

| Face | Hex |
|---|---|
| Shadow | `#3A3010` |
| Base | `#6B5A18` |
| Highlight | `#A08820` |
| Vein accent | `#D4A017` |

### Wood (Old)

| Face | Hex |
|---|---|
| Shadow | `#2A1E14` |
| Base | `#4A3424` |
| Highlight | `#6B4E38` |

### Wood (Charred)

| Face | Hex |
|---|---|
| Shadow | `#1A1210` |
| Base | `#2E2420` |
| Highlight | `#3C3028` |

### Dirt / Soil

| Face | Hex |
|---|---|
| Shadow | `#2A2018` |
| Base | `#4A3828` |
| Highlight | `#6A5440` |

### Moss-Covered Stone

| Face | Hex |
|---|---|
| Shadow | `#222C24` |
| Base | `#364838` |
| Highlight | `#4A6050` |
| Moss accent | `#5A7852` |

### Ice / Frozen

| Face | Hex |
|---|---|
| Shadow | `#2A3840` |
| Base | `#4A6878` |
| Highlight | `#7AA0B4` |
| Shimmer | `#B8D8E8` |

---

## Loot Rarity Colors

Used on item names, inventory borders, ground drop glows, and minimap dots.

| Rarity | Name Hex | Border Hex | Glow Hex | Usage |
|---|---|---|---|---|
| Common | `#6B8090` | `#4A5560` | — | Vendor trash, basic drops |
| Uncommon | `#8AA0AE` | `#6B8090` | — | Early-game upgrades |
| Rare | `#D4A017` | `#A07810` | `#E8B820` | Build-defining gear |
| Unique | `#E8DCC8` | `#A09880` | `#F2EDE3` | One-of-a-kind items |
| Legendary | `#E88A28` | `#A85A10` | `#F5A830` | Endgame chase items |
| Set Item | `#3A7A4A` | `#285A38` | `#5AAA6A` | Set-completion gear |

---

## Armour Tiers

Three-face (shadow/base/highlight) material colors for each armour tier 3D model.

### Light Armour — Padded / Cloth

| Face | Hex | Name |
|---|---|---|
| Shadow | `#4A6878` | Ice Base |
| Base | `#7AA0B4` | Ice Highlight |
| Highlight | `#B8D8E8` | Ice Shimmer |

### Medium Armour — Scale / Leather

| Face | Hex | Name |
|---|---|---|
| Shadow | `#364838` | Moss Stone Base |
| Base | `#4A6050` | Moss Stone Highlight |
| Highlight | `#5A7852` | Moss Accent |

### Heavy Armour — Dark Plate with Gold Trim

| Face | Hex | Name |
|---|---|---|
| Shadow | `#181C1F` | Iron Black |
| Base | `#2A2E32` | Iron Ore Shadow |
| Highlight | `#4A4E52` | Iron Ore Base |
| Trim accent | `#8C680A` | Dark Gold |

---

## UI Color System

### Panel Layers

| Layer | Hex | Usage |
|---|---|---|
| Background | `#181C1F` | Outermost HUD background |
| Surface | `#252C32` | Panel fills, inventory background |
| Raised | `#2E3840` | Hovered slots, selected items |
| Border | `#3A4650` | All panel borders (0.5–1px) |
| Border highlight | `#4A5560` | Active/hovered border |

### Text Hierarchy

| Role | Hex | Usage |
|---|---|---|
| Primary text | `#E8DCC8` | Main readable text |
| Secondary text | `#8AA0AE` | Labels, stat names, hints |
| Muted text | `#6B8090` | Section headers, disabled |
| Gold text | `#D4A017` | Item names (rare), affix values |
| Danger text | `#E85050` | Warnings, negative effects |

### Bars

| Bar | Fill Hex | Track Hex |
|---|---|---|
| Health | `#A32D2D` | `#181C1F` |
| Stamina / Endurance | `#4A5560` | `#181C1F` |
| XP | `#D4A017` | `#181C1F` |
| Armour / Shield | `#6B8090` | `#181C1F` |
| Poison / DoT | `#4A7A30` | `#181C1F` |
| Mana (if applicable) | `#3A5A8C` | `#181C1F` |

---

## Lighting & Environment

### Ambient Light Tones

| Environment | Hex | Notes |
|---|---|---|
| Overcast outdoor | `#8898A8` | Cool grey-blue, desaturated sunlight |
| Torchlit dungeon | `#A06030` | Warm amber cast on surfaces |
| Deep cave (no light) | `#1A2028` | Near-black blue-grey |
| Moonlit exterior | `#4A5870` | Cool blue-grey, silver-tinted |
| Cursed zone | `#3A4830` | Sickly green-grey cast |
| Fire zone | `#6A3818` | Deep ember ambient |
| Frozen zone | `#3A5870` | Cold blue ambient |

### Shadow Colors (tint to add to geometry shadows)

| Surface Type | Shadow Tint Hex |
|---|---|
| Stone in torchlight | `#2A1A10` |
| Stone in daylight | `#1E252B` |
| Wood in shadow | `#1A1210` |
| Ground in cave | `#0E1114` |

---

## VFX & Skill Effects

| Effect | Primary Hex | Secondary Hex | Notes |
|---|---|---|---|
| Physical hit | `#C43030` | `#8C2E2E` | Blood splat, impact |
| Fire / burn | `#E88A28` | `#C96820` | Flame particles |
| Ice / freeze | `#7AA0B4` | `#4A6878` | Frost shards |
| Poison | `#5AAA6A` | `#3A7A4A` | Drip, cloud |
| Lightning | `#E8DCC8` | `#D4A017` | White-gold flash |
| Curse / dark magic | `#5A3870` | `#3A2450` | Purple-black smoke |
| Heal | `#60A870` | `#3A7A4A` | Green-white motes |
| Loot drop | `#D4A017` | `#F2CC5A` | Gold shimmer burst |
| Level up | `#E8DCC8` | `#D4A017` | White-gold radial |
| Death | `#1A1210` | `#3A2820` | Dark ash fade |

---

## Enemy Color Coding

Use these accent colors on enemy geometry/materials to signal threat type at a glance.

| Enemy Type | Accent Hex | Usage |
|---|---|---|
| Standard | `#6B8090` | Neutral slate, no special threat |
| Elite | `#8AA0AE` | Brighter slate, small accent trim |
| Rare | `#D4A017` | Gold trim or highlight blocks |
| Boss | `#E8DCC8` | Bone-white with dark form |
| Undead | `#7A8870` | Pale desaturated green-grey |
| Demon | `#8C2E2E` | Dark red with ember accents |
| Construct / Golem | `#4A5560` | Full iron, no organic tones |
| Poison mob | `#4A7A30` | Sickly green blocks |
| Ice mob | `#4A6878` | Icy blue-grey |
| Fire mob | `#A84E1A` | Ember-orange highlight blocks |

---

## Biome Quick Reference

Each biome shifts the ambient tone but keeps the core Iron & Slate base for consistency.

| Biome | Base blocks | Ambient tint | Accent |
|---|---|---|---|
| Iron Crypts (default) | Stone `#3A4650`, Iron `#4A4E52` | `#1A2028` | Gold `#D4A017` |
| Ashen Wastes | Charred `#2E2420`, Ash `#6B8090` | `#2A2018` | Ember `#C96820` |
| Frozen Depths | Ice `#4A6878`, Stone `#2E3840` | `#3A5870` | Shimmer `#B8D8E8` |
| Rotwood Forest | Moss stone `#364838`, Dark wood `#4A3424` | `#222C24` | Poison `#5AAA6A` |
| Sunken Ruins | Sandstone `#7A6444`, Wet stone `#2E3840` | `#3A4038` | Pale gold `#F7E09A` |
| Demon Forge | Scorched `#3D1A06`, Iron `#2A2E32` | `#2A1A10` | Blood `#C43030` |

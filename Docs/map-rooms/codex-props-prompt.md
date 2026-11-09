# Task: original top-down props and surfaces for the floor-plan map

Where Darkness Dwells is a dark-fantasy roguelite deckbuilder set in the Black Keep, a fortress after a failed siege.
The map is a top-down floor plan of the Keep. Rooms are stone rectangles of different sizes, and each is dressed with a
few props suited to its type. Read `Docs/ui-art-upgrade/style-guide.md` sections 1 and 2 for the art direction and
palette ("The Siege Ledger": soot-blue slate, restrained worn brass, ivory, cold shadow with restrained warm light), and
look at `Docs/ui-art-upgrade/samples/02-floor-plan-concept.png`, the approved concept for the map.

Use your built-in image generation tool to make the images below.

## Rules

- **Text prompts only.** Use no reference images except the concept above, and only for mood. Never look at, copy or
  imitate anything in `Unity/Assets/Synty/`, `Unity/Assets/Polygon*/` or `Unity/Assets/LocalArt/`. Those are licensed
  3D assets, and this public repository must not contain derivatives of them. Don't imitate any existing game's art.
- **Strictly top-down orthographic view**, looking straight down, as on an architect's plan. No perspective, no
  isometric angle, no visible side faces.
- **One consistent set:** painted, stylised, with clear shapes that still read at 32×32 pixels. Muted dark stone, wood
  and iron with sparing worn brass and the occasional deep red cloth. Soft shadow directly beneath each object only, as
  if lit from above. No line-art or cartoon look, and no photorealism.
- **No text, lettering, logos, signatures, watermarks, frames or borders** anywhere.
- **Props:** one object per image, centred, filling about 80% of a square canvas, on a perfectly flat, pure magenta
  `#FF00FF` background with no gradient, texture, floor or shadow on the background. The magenta will be keyed out, so
  the object must contain no magenta or pink. If your tool can produce a real transparent background instead, do that.
- **Surfaces:** fill the whole square edge to edge, and make them tile seamlessly in both directions.
- Look at each image after generating it, and regenerate it if it breaks a rule (perspective, text, a non-flat
  background, magenta in the object, or a clear mismatch with the set).

## Surfaces (seamless, square)

| File | Description |
| --- | --- |
| `floor-flagstones.png` | Worn dark slate flagstones in irregular rectangles, thin dark mortar, subtle cracks, very low contrast so text and props stay readable on top |
| `floor-boards.png` | Dark weathered wooden floorboards, running horizontally, low contrast |
| `wall-stone.png` | Top-down view of the top of a thick stone wall: rough cut grey-blue blocks in a band, read as a wall from above, tiling horizontally and vertically |

## Props (one per image, on magenta)

| File | Object | Used in |
| --- | --- | --- |
| `prop-banner.png` | A tattered deep-red heraldic banner lying flat against a wall, seen from above, with a plain bell emblem shape (no letters) | Entrance, Elite |
| `prop-bench.png` | A heavy wooden bench | Entrance, Event |
| `prop-rubble.png` | A pile of fallen stone blocks and dust | Entrance, Combat |
| `prop-pillar.png` | A round stone pillar, seen from directly above, one broken edge | any |
| `prop-weapon-rack.png` | A wooden weapon rack holding spears and a sword | Combat, Elite |
| `prop-barrels.png` | Two barrels and a crate | Combat, Investigation |
| `prop-bones.png` | A scattered skeleton and a cracked shield | Combat, Secret |
| `prop-armour-stand.png` | A battered suit of armour on a stand | Elite |
| `prop-chains.png` | Iron shackles and chains bolted to a ring in the floor | Elite, Boss |
| `prop-bookshelf.png` | A tall bookshelf with old books, seen from above | Investigation |
| `prop-desk.png` | A writing desk with scattered papers, a candle and an inkpot | Investigation |
| `prop-lectern.png` | A lectern with a large open book | Investigation, Event |
| `prop-table.png` | A long table with two stools | Event |
| `prop-rug.png` | A worn dark-red rug with a plain border pattern | Event, Sanctuary |
| `prop-candles.png` | A cluster of lit candles on the floor | Event, Secret |
| `prop-bedroll.png` | A bedroll with a blanket and a pack | Sanctuary |
| `prop-brazier.png` | An iron brazier with a small warm fire | Sanctuary, Boss |
| `prop-shrine.png` | A small stone shrine with a bell on it | Sanctuary |
| `prop-throne.png` | A tall dark stone throne with worn brass trim | Boss |
| `prop-altar.png` | A stone altar stained dark red, with a chalice | Boss, Secret |
| `prop-sarcophagus.png` | A stone sarcophagus with a carved lid | Secret |
| `prop-grate.png` | A round iron drain grate set in stone | Secret |

## Output

- Save every image in `Docs/map-rooms/generated/` with the file names above, at the size the tool produces.
- Write `Docs/map-rooms/generated/prompts.md` with the exact prompt for each image.
- Change nothing outside `Docs/map-rooms/`, and make no git commits.
- In your final reply, list the files you made, any you regenerated and why, whether props have real transparency or
  the magenta background, and anything you couldn't do.

# UI analysis: Where Darkness Dwells

Written for the UI art upgrade, branch `ui-art-upgrade/2026-11-06`. Paths are relative to the repository root.

## Summary

The UI has **no art assets at all**. Everything on screen is flat colour, borders and rounded corners from USS, with the
default font. So this upgrade creates art for the first time; it doesn't replace existing images.

## Framework

- **UI Toolkit only** (UXML and USS), as `CLAUDE.md` requires. No uGUI or IMGUI in the game.
- One panel settings asset, `Unity/Assets/UI/CombatPanelSettings.asset`: scale with screen size, reference resolution
  1920x1080, match 0.5.
- Theme: `Unity/Assets/UI/WddRuntimeTheme.tss`, which only imports `unity-theme://default`.
- Consequences for art in UI Toolkit:
  - Images are applied from USS with `background-image: url("...")` and scaled with `-unity-background-scale-mode`.
  - 9-slice is set in USS with `-unity-slice-left/right/top/bottom` (and `-unity-slice-scale`), or from a Sprite's borders.
  - Fonts are applied with `-unity-font-definition` and need a Font or a Font Asset.
  - USS has no gradients, drop shadows or blend modes. Gradients have to be images. `Menus.uss` fakes one with six
    overlapping bands (`.title-shade-1` to `-6`).
  - State styling is by pseudo-class (`:hover`, `:active`, `:disabled`) and by classes added from code (such as
    `.is-reachable`, `.is-selected`, `.is-unplayable`). These class names are used by C# and must not be renamed.

## Screens, HUDs and popups

Scene: `Unity/Assets/Scenes/Run.unity` is the game. `Combat.unity` is a test fight. `SampleScene.unity` is Unity's leftover.

| Screen | File and element | Styles | Notes |
| --- | --- | --- | --- |
| Title | `UI/Menus.uxml` `title-view` | `Menus.uss` | Uses the title backdrop when present; otherwise plain `rgb(16,13,18)` |
| Pause | `UI/Menus.uxml` `pause-view` | `Menus.uss` `.menu-panel` | Overlay |
| Settings | `UI/Menus.uxml` `settings-view` | `Menus.uss`, `.menu-toggle` | Default Unity toggle graphic |
| Top bar (run and combat) | `RunScreen.uxml`, `CombatScreen.uxml` `.top-bar` | `CombatScreen.uss` | Includes party health chips, deck count, Journal button |
| Party selection | `RunScreen.uxml` `party-view` | `.party-hero`, `.is-chosen` | Choose 3 of 6 heroes; no portraits |
| Map, floor plan (default) | `RunScreen.uxml` `map-view`, `Game/FloorPlanView.cs` | `.plan-board .tile .door .door-tag .tile-stairs .tile-passage .tile-<kind>` | Flat tiles; built in code |
| Map, chart (debug) | `Game/MapView.cs` | `.room .room-<kind> .map-entrance .room-glow` | Lines drawn with Painter2D, colours hard-coded at `MapView.cs:27-31` |
| Reward | `RunScreen.uxml` `reward-view` | `.card-row`, `.found-clue` | |
| Sanctuary | `RunScreen.uxml` `sanctuary-view`, `reflect-panel` | `.choice-button`, `.deck-grid` | |
| Investigation | `RunScreen.uxml` `investigation-view` | `.searcher`, `.choice-row` | |
| Secret room | `RunScreen.uxml` `secret-view` | `.choice-button` | |
| Event | `RunScreen.uxml` `event-view` | `.choice-button` | |
| Remove a card | `RunScreen.uxml` `remove-view` | `.deck-grid` | |
| Found (clue or keepsake) | `RunScreen.uxml` `found-view` | `.view-title` | |
| Run end | `RunScreen.uxml` `end-view` | `.run-summary` | |
| Unwritten room | `RunScreen.uxml` `unwritten-view` | | Fallback |
| Journal popup | `RunScreen.uxml` `journal-panel` | `.journal-page .journal-entry` | |
| Combat | `UI/CombatScreen.uxml` | `CombatScreen.uss` | Battlefield, log panel, hand, Resolve, End activation, Discard, floaters, end panel |
| Hero panel | `UI/HeroPanel.uxml` | `.combatant.hero`, `.combatant-accent`, `.bar`, `.badge` | Sits over the 3D fight stage when present (`.has-stage`) |
| Enemy panel | `UI/EnemyPanel.uxml` | `.combatant.enemy`, `.intent`, `.cancel-intent` | Intents are text only |
| Card | `UI/Card.uxml` | `.card`, `.card-cost`, `.card-owners`, `.owner-<colour>`, `.is-unplayable`, `.is-selected`, `.is-boosted`, `.is-discounted` | 150x210 at reference; dual cards show two colours |
| Web loading page | `Unity/Assets/WebGLTemplates/WhereDarknessDwells/index.html` | Inline CSS | Georgia serif, gold `#c9a35a`, red bar `#8c1c22`; already more styled than the game UI |

## UI art assets

| Asset | Path | Size | Import | 9-slice | Atlas | Used by |
| --- | --- | --- | --- | --- | --- | --- |
| None committed | | | | | | |
| Title backdrop (local only) | `Unity/Assets/LocalArt/Resources/TitleBackdrop.jpg` | 2560x1440 | Texture | None | None | Title. Gitignored, rendered from Synty. Must remain optional |
| Floor tiles (local only) | `Unity/Assets/LocalArt/Resources/FloorTiles/` | | | | | Gitignored, Synty-derived. Must remain optional |
| Fight stage (local only) | `Unity/Assets/LocalArt/Resources/CombatStage.prefab` | | | | | 3D set behind combat. UI must read over it and without it |

`Unity/Assets/TutorialInfo/Icons/URP.png` is Unity's template leftover, not game UI.

## Fonts, colours and theme

- **Fonts:** none in the project. All text uses Unity's default, with bold and italic through `-unity-font-style`.
- **No shared theme:** colours are literal `rgb()` values repeated across three USS files. USS variables aren't used.
- **Main colours:**
  - Gold accent `rgb(201,162,74)` (used 16 times), pale gold `rgb(232,212,138)`
  - Text `rgb(240,232,214)`, `rgb(222,214,200)`, `rgb(190,182,172)`, muted `rgb(150,140,146)`
  - Surfaces `rgb(16,13,18)`, `rgb(22,18,25)`, `rgb(30,25,33)`, `rgb(34,28,36)`, button `rgb(58,46,60)`
- **Hero colours** (accent, then card background). These carry meaning: they're how the UI shows who owns a card.
  - Iron, Bram `rgb(196,138,58)` / `rgb(52,40,30)`
  - Thorn, Wren `rgb(96,150,72)` / `rgb(32,46,32)`
  - Ash, Sister Ashe `rgb(150,112,190)` / `rgb(42,34,52)`
  - Bone, Corvin `rgb(214,204,180)` / `rgb(50,47,40)`
  - Steel, Mara `rgb(120,156,196)` / `rgb(30,38,50)`
  - Bronze, Hollis `rgb(176,110,62)` / `rgb(50,34,26)`
- **Room colours** (borders): combat `rgb(120,60,60)`, elite `rgb(200,70,60)`, investigation `rgb(90,120,170)`, event
  `rgb(130,110,160)`, sanctuary `rgb(110,160,100)`, secret `rgb(90,160,170)`, boss `rgb(201,162,74)`, entrance
  `rgb(150,140,146)`.

## Resolutions and platforms

- Reference 1920x1080, landscape 16:9, mouse first.
- macOS and Windows standalone, and WebGL on GitHub Pages. Web download size matters (the last zip was about 41 MB).

## Inconsistencies and placeholder art

- Every panel, button and card is a flat rounded rectangle. `GAME_DESIGN.md` says these are placeholders.
- Bram's Iron and Hollis's Bronze accents are close together, as are the card backgrounds of Iron and Bronze.
- The combat room `rgb(120,60,60)` and elite `rgb(200,70,60)` differ only in brightness.
- **Room types have no icons**, though `GAME_DESIGN.md` says rooms show a type icon. Only border colour and a word differ.
- **Statuses and intents have no icons.** Block, Guard, Exposed, Weak, Bleed, Hex and intents are all text.
- **Heroes have no portraits,** though `GAME_DESIGN.md` says card frames show the owners' portraits.
- Toggles use Unity's default graphic. Scrollbars are Unity's default too.
- The title's fade is six bands. `MapView.cs` duplicates palette values in C#.
- The default font doesn't match the serif web loading page.
- Text is sized in many one-off values.

## Genre and tone

Single-player dark-fantasy roguelite deckbuilder with room exploration and a mystery. A party of three enters the Black
Keep, a fortress with a history: a siege, a steward named Vael who bargained with something sealed beneath it, and a
plague. Direction from `GAME_DESIGN.md`: "Stylised dark fantasy: old stone, restrained warm light against cold shadow, and
strong silhouettes." Candle-wax seals, chapel bells, blood and old ledgers recur in the writing. The 3D art is Synty's
low-poly, faceted style with deep blue shadow and amber light. The title backdrop is a moonlit keep with crimson and
candlelight accents. Each hero's colour should run through their cards, portrait frame and activation highlight.

## Constraints on new art

- **The repository is public.** Art in `Unity/Assets/UI/` gets committed, so it must be original or under a licence that
  allows redistribution. Nothing may be copied, traced or derived from `Unity/Assets/Synty/` (including the local
  `InterfaceCore` and `InterfaceFantasyMenus` packs) or `Unity/Assets/LocalArt/`, and it mustn't imitate them closely.
- Fonts need a licence that allows redistribution (such as the SIL Open Font License), with the licence file committed.
- A fresh clone has no Synty art, so the UI must look finished with no 3D stage or backdrop behind it.
- Card text must stay readable at 150x210. Hero colours must stay distinguishable, including on dual cards.
- Keep the web build small: prefer small tiled or 9-sliced textures, and share them between components.

# The Siege Ledger — production art

Created for Where Darkness Dwells on 6 November 2026. All 93 PNGs are original, code-authored geometry, exported by `Sources/generate.py` using Python 3's standard library. No image generator, stock graphics, sample pixels, third-party artwork, Synty files, LocalArt files, screenshots, model renders or traced silhouettes were used. No text or numerals are baked into the images. The approved style guide supplies the palette, inventory, source dimensions and slice measurements; its concept images were not used as image inputs.

The editable source is the generator: independent surface, edge, repair and silhouette drawing operations, with named geometry for each mark. Run from the repository root:

```
python3 Unity/Assets/UI/Art/Sources/generate.py
python3 Unity/Assets/UI/Art/Sources/audit.py
```

The generator preserves existing metadata and GUIDs. The guide's L/T/R/B measurements are converted to Unity's L/B/R/T Sprite borders. USS explicitly repeats the **same** four slice borders and scale 1 for every sliced image, so texture-backed USS URLs and Sprite-backed references agree. The audit rejects disagreement; icons, medals, coins and title images remain unsliced. Uncompressed sRGB single Sprites, bilinear filtering, alpha transparency, no mipmaps, full-rect meshes and four-pixel sprite extrusion are specified in metadata. Actual import and UI Toolkit atlas behaviour still require Unity verification.

| Group | How it was made |
| --- | --- |
| `Surfaces/slate-field.png` | Original quiet slate field with a broad, low-contrast seam. Deliberately opaque; text gets opaque USS backing. |
| `Frames/` | Transparent chamfered iron rules, binding seams, small brass repairs; clear centres. Focus uses independent corner brackets. Portrait frame overlays the live RenderTexture without replacing it. |
| `Controls/` | Original cut plates, shallow button lips, recessed checkbox/rails and grips. White angular check. Value fill is neutral white for USS tinting. |
| `Cards/` | Transparent folio binding; unsliced neutral paper coin and open medal rim. Cost is live text and the source coin tints to paper/benefit. |
| `Bars/` | Transparent iron trough and quiet crimson strip. Real health controls the fill width; numbers remain live. |
| `Map/` | Original slab boundary, passage rails, shut/open/travelled thresholds, opaque name/tag plates, chart plaque and alpha rim. No exits are invented in room walls. Local pictures/paving remain optional and are not included. |
| `Icons/room-*.png` | Independently drawn portcullis, sword, notched helm, ledger, register leaf, candle, water/steps and sealed door. White silhouettes with antialiased alpha. |
| Other `Icons/` | Independently drawn status, intent, navigation, outcome and state silhouettes. Block shares the shield vocabulary; actual intent kind determines the displayed icon. Unused vocabulary does not introduce new game actions. |
| `Icons/hero-*.png` | Twelve original signature silhouettes: shield, split leaf, eye, stitched cross, blades, bell, cut cord, cinders, quill/leaf, split star, veiled coronet, square-flame torch. No painted portraits. |
| `Journal/entry.png` | Original open text well with binding seam and a small repair. Unknown entries use the empty folio rather than undiscovered clue art. |
| `Title/keep-ledger.png` | Original abstract architectural ledger emboss, constructed from nested walls and quiet rules. This is a cover motif, not a playable map or a drawing of the local backdrop. Deliberately opaque. |
| `Title/title-fade.png` | Original horizontal void/alpha ramp: first 60% solid, remainder fades to zero alpha. Stretched unsliced; the other five existing band elements are visually empty. Title text also has an opaque cover folio. |

All icon pixels with nonzero alpha have RGB 255/255/255, including antialiased edges. Every frame has real transparent pixels, not a checkerboard. The two intentionally opaque images are the slate surface and title cover. The soft reachable rim peaks below full opacity by design.

## Fonts and scope

Both families are in `Fonts/` under the SIL Open Font License 1.1, with their full notices beside them (`Cinzel-OFL.txt`, `SourceSans3-LICENSE.md`). There is no font package or runtime download.

- **Source Sans 3 Regular and Semibold:** unmodified static TTFs from Adobe's `3.052R` release (`TTF-source-sans-3.052R.zip`). "Source" is a Reserved Font Name, so these files must not be modified or subset under that name.
- **Cinzel SemiBold:** upstream publishes no static SemiBold, so this is a static instance at `wght=600` made with fontTools from `ofl/cinzel/Cinzel[wght].ttf` in `google/fonts`, with its name table set to match. Cinzel's licence declares no Reserved Font Name.
- **Unity bindings:** dynamic SDF Font Assets. `-unity-font-style: bold` resolves to the Semibold file through the Regular asset's weight table. Cinzel headings set `normal`, because the file is already SemiBold. Italic text is synthesised, since no italic file is shipped.

Presentation bindings in `Views`, `CombatScreen`, `RunFlow`, `MapView`, `FloorPlanView` and `GameMenus` add non-picking artwork and use existing definitions, health, room types, status stacks, intentions, ending result and slider values. The party screen scrolls when the roster/detail content needs more room. `HeroPortraits`, stage/animation code and audio callbacks are unchanged. Floor-only optional paving now also works when a room picture is missing. No Core/content, tests, scenes or project settings changed. The web HTML template is outside this UI-only change; its separate loading-page pass remains pending.

## Validation and acceptance still needed

Static audit checks every PNG's dimensions, RGBA pixels, alpha extrema, white silhouettes, exact Sprite borders and import flags; metadata GUIDs; relative USS URLs; UXML well-formedness and preservation of all original element names; and the guide's C# class contracts. An art contact sheet was visually inspected outside Unity. These checks do not compile C# or run Unity's USS parser.

Current exports total approximately 33 KiB on disk and 4.03 MiB of RGBA pixels before runtime atlas padding/copies. These are **not measured web build or resident-memory results**. Measure the incremental compressed WebGL payload against the guide's 2 MiB art / 3 MiB with fonts budgets, and atlas residency against 16 MiB. No runtime sample sheets are shipped.

`dotnet test tests/WhereDarknessDwells.Tests` could not run: `dotnet` is not installed. Unity is unavailable. Editor acceptance remains open:

- Import/recompile; check Console for C#, UXML and USS errors. In UI Debugger confirm Sprite slices, slider/toggle/scroller hierarchy, built-in checked visibility, keyboard focus and pointer hit areas. Check atlas edge bleed and thin frames.
- Title with and without optional backdrop; all menu states, toggles and scrollbars. Three volume sliders at 0/mid/1, keyboard/drag/disabled/focus, live audio levels and persisted values after reopening settings.
- Twelve heroes at 0–3 choices, rejected fourth choice, live/missing/mixed portraits and camera disposal; detail panel with longest starter/shared-card text and reachable confirmation control.
- Every real card at 150×210, all 66 dual pairs, longest rules/names, selected+unplayable+boosted+discounted combinations, grayscale and colour-vision simulations. No rules text was shortened or font reduced; overflow is visible rather than silently clipped. Actual font metrics/copy fit need rendered inspection; if copy exceeds the allocation, approve shorter equivalent copy or a larger presentation before accepting it.
- Combat with and without stage, brightest lighting, every action/guard/fall, normal/fast animations, panel collision, intent targets/statuses, vague/cancelled intentions, floaters/log/hand and health contrast. Vague intentions must not reveal target marks or numeric outcomes.
- All narrative/reward/removal/found/fallback screens, both endings, journal found/unknown/keepsakes. Floor plan/F2 chart/F3 whole-plan, cellar, current/travelled/open doors, and floor-only/picture-only/both/no local images, including rotated labels over bright pictures.
- Windows/macOS/WebGL at 1920×1080 and 1280×720. The guide leaves 720p card readability an explicit acceptance decision; this work does not certify it. Rerun typography/card-fit checks when the chosen fonts arrive. Verify the separate web loading page before release.

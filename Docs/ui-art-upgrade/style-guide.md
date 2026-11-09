# Where Darkness Dwells — UI style guide

**Direction: The Siege Ledger.** Approved direction for Milestone 6.5, 6 November 2026. This document specifies future work; it changes no game behaviour, assets or class contracts. Read alongside [ui-analysis.md](ui-analysis.md). Measurements are UI Toolkit reference pixels at 1920×1080 unless identified as source-image pixels.

## Changes after the merges

- Expanded ownership to twelve heroes and all 66 content pairs, with six additional accent/background tokens, calculated contrasts and signature marks. Existing tokens and colour-class names remain.
- Updated the two-row party roster and live card-detail panel. Keep the live model cameras; use signature marks for missing models and all other former portrait slots. Removed the six proposed bust commissions.
- Added framing and opaque labels over optional floor paving/room pictures, with the same complete plain-tile fallback.
- Specified all three existing volume sliders and their state assets. Checked plaques and feedback against the animated combat stage.
- Extended the upgrade schedule, selector contracts and section 9 acceptance checks. Existing samples remain historical concepts; no new sample is supplied.

## 1. Art direction

The expedition is reconstructing a household's last accounts. The UI should feel like the Keep's steward's ledger has been dismantled into useful expedition equipment: soot-dark boards, ruled inventory slips, worn brass corner straps, chapel-bell impressions and broken wax seals. The siege appears in repaired edges; the bargain appears in interrupted seals, not ubiquitous magic circles. This is an interpretation of the existing history, not new lore or mechanics.

Three material roles make the interface recognisable: **slate is structure**, **ledger paper is information**, **brass is action or route**. Use mostly dark ledger leaves, with pale paper reserved for cost counters and discovery emblems. A small red seal is a warning or narrative punctuation. Hero colours remain ownership; gold alone never replaces an owner's colour. One deliberate nick in a corner is enough wear. Let large quiet areas and strong silhouettes relate to the stylised stage without copying its geometry.

Avoid generic baroque fantasy frames, excessive skulls, thorn filigree, parchment under every label, neon magic glows, smeared blood behind text, simulated tiny handwriting, photographic noise, and bevels that make every surface look clickable. Do not reproduce another game's map tile design. The floor plan is an original architectural drawing of the existing graph.

All new graphics must be original commissions, original generated concepts refined into original production art, or explicitly redistributable assets with notices. No Synty or LocalArt images, meshes, screenshots, rendered props, silhouettes traced from them, or closely imitated frames may enter this asset set. None were used as image inputs for these samples. Keep a provenance record for each production source. No proprietary material is necessary for the finished title, flat combat view or map.

## 2. Colour system

Declare these custom properties on **both `.screen` and `.menus`** in a future shared theme loaded before the three existing stylesheets. They are sibling document roots; one root's variables do not theme another document. USS uses `var(--token)`; this is a proposed specification, not a patch.

```css
.screen, .menus {
    --void: #10151C;
    --recess: #151C24;
    --panel: #1C2630;
    --raised: #293644;
    --hover: #344656;
    --pressed: #202B36;
    --selected: #3C3526;
    --disabled: #20262D;
    --paper: #E9DCBD;
    --ink: #10151C;
    --text: #F1E8D5;
    --text-secondary: #B9C2CC;
    --text-muted: #ABB5C1;
    --brass: #D6AF63;
    --focus: #F5DDA3;
    --edge: #647486;
    --edge-quiet: #465362;
    --danger: #F49A91;
    --benefit: #A2D8AF;
    --health: #68323F;
    --health-track: #30212B;
    --warning-surface: #402A30;
    --badge-surface: #263442;
    --scrim: rgba(16, 21, 28, 0.88);

    --hero-iron: #E6B34F;
    --hero-thorn: #71C596;
    --hero-ash: #B39AE8;
    --hero-bone: #DCD5BE;
    --hero-steel: #79BFE8;
    --hero-bronze: #E78E79;
    --card-iron: #342B1B;
    --card-thorn: #1B3028;
    --card-ash: #2C253C;
    --card-bone: #302E27;
    --card-steel: #1D2C3A;
    --card-bronze: #392420;

    --hero-smoke: #B4B8C7;
    --card-smoke: #292B34;
    --hero-ember: #F5A36E;
    --card-ember: #39281F;
    --hero-ink: #92A8EC;
    --card-ink: #222B40;
    --hero-rose: #EAA6CA;
    --card-rose: #372636;
    --hero-verdigris: #6DCECA;
    --card-verdigris: #183332;
    --hero-tallow: #D8D17D;
    --card-tallow: #32301E;

    --room-combat: #E58CA0;
    --room-elite: #F0B15C;
    --room-investigation: #85B8EB;
    --room-event: #B6A0E5;
    --room-sanctuary: #8AC99A;
    --room-secret: #70CED0;
    --room-boss: #D6AF63;
    --room-entrance: #B9C2CC;
}
```

Bram's Iron becomes a clear yellow amber; Hollis's Bronze becomes **oxidised copper-red**, retaining its material name and `.owner-bronze` contract. Their backgrounds also separate yellow-brown from red-brown. Wren is green, Ashe violet, Corvin warm ivory, Mara sky-blue. Never tint the whole hand in the active hero's colour. Keep all twelve `.owner-*` contracts unchanged; the mapping below comes from `StarterContent.Heroes()` and is shared by card fills, `.swatch.owner-*`, `.combatant-accent` and `.owner-* .party-portrait`.

Combat rooms become dusty rose with a single sword; Elites become orange with a notched helm. This fixes the old red/darker-red pair by hue as well as value. Boss brass is distinguished from Elite by seal icon, label and larger treatment. Room colour and ownership colour occupy separate contexts; neither alone encodes meaning.

### Contrast rules and checked pairs

Ratios below use WCAG relative sRGB luminance `(Llight + .05)/(Ldark + .05)`, rounded to two decimals. Target **4.5:1 for every meaningful text size**, 3:1 for essential icon silhouettes and state outlines. Muted does not mean illegible. Use text tokens on dark surfaces and ink on pale counters. Do not put white text on hero stripes. The generated concepts are not colour measurement references.

| Opaque surface | Primary text | Secondary text | Muted text |
| --- | ---: | ---: | ---: |
| `--void` | 15.05:1 | 10.17:1 | 8.82:1 |
| `--recess` | 14.09:1 | 9.52:1 | 8.26:1 |
| `--panel` | 12.59:1 | 8.51:1 | 7.38:1 |
| `--raised` | 10.11:1 | 6.83:1 | 5.93:1 |
| `--hover` | 8.00:1 | 5.40:1 | 4.69:1 |
| `--pressed` | 11.81:1 | 7.98:1 | 6.92:1 |
| `--selected` | 9.97:1 | 6.74:1 | 5.85:1 |
| `--disabled` | 12.53:1 | 8.46:1 | 7.35:1 |
| `--warning-surface` | 10.83:1 | 7.32:1 | 6.35:1 |
| `--badge-surface` | 10.44:1 | 7.05:1 | 6.12:1 |
| `--health` | 8.15:1 | 5.50:1 | 4.78:1 |
| `--health-track` | 12.51:1 | 8.45:1 | 7.34:1 |
| `--card-iron` | 11.44:1 | 7.73:1 | 6.71:1 |
| `--card-thorn` | 11.50:1 | 7.77:1 | 6.74:1 |
| `--card-ash` | 12.00:1 | 8.11:1 | 7.04:1 |
| `--card-bone` | 11.16:1 | 7.54:1 | 6.54:1 |
| `--card-steel` | 11.70:1 | 7.91:1 | 6.86:1 |
| `--card-bronze` | 11.92:1 | 8.05:1 | 6.99:1 |
| `--card-smoke` | 11.58:1 | 7.82:1 | 6.79:1 |
| `--card-ember` | 11.52:1 | 7.78:1 | 6.75:1 |
| `--card-ink` | 11.59:1 | 7.83:1 | 6.80:1 |
| `--card-rose` | 11.55:1 | 7.80:1 | 6.77:1 |
| `--card-verdigris` | 11.06:1 | 7.48:1 | 6.49:1 |
| `--card-tallow` | 10.93:1 | 7.39:1 | 6.41:1 |

| Pale fill or accent | Foreground / backing | Contrast |
| --- | --- | ---: |
| `--paper` | `--ink` text | 13.47:1 |
| `--benefit` | `--ink` text | 11.32:1 |
| `--hero-iron` | `--ink` text | 9.53:1 |
| `--hero-thorn` | `--ink` text | 8.84:1 |
| `--hero-ash` | `--ink` text | 7.59:1 |
| `--hero-bone` | `--ink` text | 12.48:1 |
| `--hero-steel` | `--ink` text | 9.10:1 |
| `--hero-bronze` | `--ink` text | 7.45:1 |
| `--brass` icon / edge | `--panel` backing | 7.43:1 |
| `--focus` icon / edge | `--panel` backing | 11.51:1 |
| `--danger` icon / edge | `--panel` backing | 7.21:1 |
| `--benefit` icon / edge | `--panel` backing | 9.48:1 |
| `--hero-iron` icon / edge | `--panel` backing | 7.98:1 |
| `--hero-thorn` icon / edge | `--panel` backing | 7.40:1 |
| `--hero-ash` icon / edge | `--panel` backing | 6.36:1 |
| `--hero-bone` icon / edge | `--panel` backing | 10.45:1 |
| `--hero-steel` icon / edge | `--panel` backing | 7.62:1 |
| `--hero-bronze` icon / edge | `--panel` backing | 6.24:1 |
| `--room-combat` icon / edge | `--panel` backing | 6.27:1 |
| `--room-elite` icon / edge | `--panel` backing | 8.14:1 |
| `--room-investigation` icon / edge | `--panel` backing | 7.34:1 |
| `--room-event` icon / edge | `--panel` backing | 6.68:1 |
| `--room-sanctuary` icon / edge | `--panel` backing | 7.96:1 |
| `--room-secret` icon / edge | `--panel` backing | 8.35:1 |
| `--room-boss` icon / edge | `--panel` backing | 7.43:1 |
| `--room-entrance` icon / edge | `--panel` backing | 8.51:1 |

Additional pairs calculated from the exact new hex values with the same sRGB transfer function (linearise each channel, then L = 0.2126R + 0.7152G + 0.0722B); no sample-image measurements:

| New accent | Foreground / backing | Contrast |
| --- | --- | ---: |
| `--hero-smoke` | `--ink` text | 9.27:1 |
| `--hero-smoke` icon / edge | `--panel` backing | 7.76:1 |
| `--hero-ember` | `--ink` text | 9.03:1 |
| `--hero-ember` icon / edge | `--panel` backing | 7.56:1 |
| `--hero-ink` | `--ink` text | 7.88:1 |
| `--hero-ink` icon / edge | `--panel` backing | 6.60:1 |
| `--hero-rose` | `--ink` text | 9.43:1 |
| `--hero-rose` icon / edge | `--panel` backing | 7.89:1 |
| `--hero-verdigris` | `--ink` text | 9.89:1 |
| `--hero-verdigris` icon / edge | `--panel` backing | 8.28:1 |
| `--hero-tallow` | `--ink` text | 11.64:1 |
| `--hero-tallow` icon / edge | `--panel` backing | 9.74:1 |

Every dark surface listed above is opaque wherever text is drawn, including `.has-stage .combatant`, `.log-panel`, `.intent` and `.door-tag`. The scrim is decorative separation, not the text's contrast backing. Transparent corners and margins may show the stage; the label area cannot. Fill health bars with `--health`, not the brighter warning accent, so white numbers remain readable across both filled and empty regions. Text on all hero card backgrounds uses `--text` and `--text-secondary`; dual cards use neutral `--panel` beneath text. Text on room tiles uses these same neutral tokens, not room-coloured body copy. Coloured icons and narrow rails use hero/room accents against `--panel` or darker; hover and selected fills retain a separate opaque icon well when needed.

Colour is reinforced by **owner name + signature mark**, including two named marks on dual cards. These are UI identity marks, not statuses. Author every mark at 64×64; use an owner-medal frame at card sizes. Painted hero portraits were added later and replace the medal in combat panels; see `Docs/portraits/portrait-plan.md`.

| Hero | Existing class | Signature mark |
| --- | --- | --- |
| Bram | `.owner-iron` | Vertical shield |
| Wren | `.owner-thorn` | Split leaf |
| Sister Ashe | `.owner-ash` | Open eye |
| Corvin | `.owner-bone` | Stitched cross |
| Mara | `.owner-steel` | Paired blades |
| Hollis | `.owner-bronze` | Bell |
| Silas | `.owner-smoke` | Knotted cord with one cut end |
| Isidore | `.owner-ember` | Three rising cinders |
| Aldous | `.owner-ink` | Quill over a ruled leaf |
| Vesna | `.owner-rose` | Split four-point star |
| Lady Isolde | `.owner-verdigris` | Veil beneath a broken coronet |
| Tam | `.owner-tallow` | Torch with a square flame |

The closest practical confusions are Iron/Tallow (yellow), Thorn/Verdigris (green/teal), Bronze/Ember (warm red/orange), Ash/Ink (violet/blue), Bone/Smoke (pale neutrals) and Rose/Bronze (pink/copper). This is not twelve reliably distinct hues, especially under colour-vision deficiencies or grayscale. Tallow/Bone also approach pale cream; Steel/Ink share blue. Names and distinct silhouettes are mandatory, never optional colour substitutes. Ink is a bright blue accent despite its material name; all twelve keep dark information wells.

Dual cards retain two equal rails with a 2px void divider and two marks; never blend colours. A calculation over the two-owner `CardDef` entries in `StarterContent.cs` finds **66 distinct unordered pairs**: 15 original-six, 15 new-six, 36 mixed Back to Back pairs, matching 12×11/2. Test every content pair in grayscale and protan/deutan/tritan simulations at 150×210, with names and marks legible. Each selected three-hero party still brings only its three pair cards.

## 3. Typography and small-card discipline

Use **Cinzel SemiBold (600)** for title and screen headings only; its carved capitals evoke a chapel inscription. Use **Source Sans 3 Regular (400) and Semibold (600)** for all rules, narrative, controls and numbers. Avoid decorative Cinzel variants and all-caps card rules. Both families are under SIL Open Font License 1.1: [Cinzel source/download and licence](https://github.com/google/fonts/tree/main/ofl/cinzel), [Cinzel OFL](https://github.com/google/fonts/blob/main/ofl/cinzel/OFL.txt), [Source Sans releases and static TTFs](https://github.com/adobe-fonts/source-sans/releases), [Source Sans licence](https://github.com/adobe-fonts/source-sans/blob/release/LICENSE.md). Licence sources checked 6 November 2026. Ship the relevant full licence and copyright notices beside each font and in distributed notices; observe reserved font names if modifying fonts.

Prefer static TTF weights, not an untested variable-font runtime dependency. If Cinzel download supplies only a variable font, obtain a static upstream release or prepare a compliant named static instance during art production. Import a Font/Font Asset through Unity's text system and bind with `-unity-font-definition`; no CSS `font-family` stack or browser `@font-face` in USS. Do not add a font package.

| Role | Typeface | Size / intended line box | Selectors |
| --- | --- | --- | --- |
| Game title | Cinzel 600 | 60 / 72 | `.game-title` |
| Screen/end title | Cinzel 600 | 40 / 48 | `.view-title`, `.end-title` |
| Menu heading | Cinzel 600 | 28 / 36 | `.menu-heading` |
| Section / fighter name | Source Sans 600 | 20 / 26 | `.panel-heading`, `.combatant-name` |
| Narrative | Source Sans 400 | 18 / 25 | `.view-text`, `.end-text` |
| Buttons / intent headline | Source Sans 600 | 16 / 21 | `.unity-button`, `.intent-text` |
| Rules / log / stats | Source Sans 400 | 14 / 18 | `.card-text`, `.log-line`, `.bar-text`, `.status` |
| Card name | Source Sans 600 | 15 / 18 | `.card-name` |
| Supporting labels | Source Sans 400 | 13 / 17 | `.card-target`, `.card-owner-names`, `.seed`, `.piles`, `.map-legend` |
| Cost | Source Sans 600 | 20 / 26 | `.card-cost` |
| Resolve counter | Source Sans 600 | 48 / 56 | `.resolve` |

Line boxes are layout targets, not a claim that browser CSS `line-height` is available in USS. Verify actual Unity font metrics, use label sizing/padding and the text system's supported spacing controls. Avoid forced line breaks based on one OS's default font.

At **150×210**, keep 2px frame, 8px horizontal inner padding, 8px owner rail, 4px gap, a header at most 40px (two name lines beside a 30px cost), target 17px, rules allocation about 80px (four 18px lines plus breathing room), and a 28px owner footer including 20px marks. No card illustration window at this size. For dual names, use two short owner names at 13px on a separate footer line if necessary; the footer may grow to 36px using spare vertical padding. Do not reduce the 14px rules font to make copy fit. Audit the longest real names and rules, including discounts, upgrades and dual owners. If they exceed the budget, propose shorter equivalent wording or a larger card presentation before implementation; never silently clip game rules as the placeholder does. Hover lift is supplementary, not the only readable state.

If fonts cannot be downloaded, keep Unity's bundled default runtime face for every role with these sizes and bold via `-unity-font-style`. Supply plain text headings, not baked text images or an OS Georgia dependency. Record this as temporary typography, retain the material art and spacing, and rerun the card-fit audit when the chosen fonts arrive. No runtime font network requests. At 1280×720 the default reference scaling makes a 14px card rule roughly 9.3 physical pixels: this guide does **not** certify that as readable. Acceptance requires a UI scale/larger hand treatment or an explicit supported-size decision, without shrinking rules or hiding them in tooltips.

## 4. Materials, edges and slicing

Use squared folio corners with a small 2–4px chamfer painted into the image, not large pill radii. Reserve circles for cost and identity medals. Panel corners have one brass stitch and a slate cut; buttons have an inset iron plate with a narrow lower lip. Cards have a quiet outer binding, no elaborate crown. Journal has a left binding seam, not an illustration of a whole desk.

Textures stay at 2–3% luminance variation **outside text wells**. Tiny ruled marks live in margins. Optional local room pictures can sit within the same framing; they never replace an information well. Do not tile a complex stone mural across a variable-height room. Floor tiles are neutral slabs with a repeatable quiet centre; room identity comes from a coloured border, icon and name. Narratives reuse a folio and a 64px room emblem rather than full-screen paintings.

All slices below are **L/T/R/B source pixels**, with `-unity-slice-scale: 1` at reference size. Corners remain fixed, edge strips stretch in one direction, centre stretches in both. Thus centres must be nearly flat; don't put round rivets or an icon in a stretched strip. Minimum destination size must exceed opposing borders. Set all four slice properties; never combine conflicting Sprite borders and USS borders. Use a transparent frame over an opaque USS fill when tint would otherwise colour text wells. The optional floor paving already repeats through `Pave` using `backgroundRepeat` and `backgroundSize`; preserve its scale. For new shared slice art, no repeated tiling is required: small low-detail grain can be baked into each shared slice; if repetition is later needed, validate supported UI Toolkit background repeat properties first. Icons, marks, seals and cost coins are **unsliced**, `scale-to-fit`; title fade is stretched, not sliced. Door sprites preserve end caps; passages stretch only a featureless middle. Trim no sprite border pixels during atlas packing.

## 5. Component specifications

### Buttons and universal states

Apply the base system to `.unity-button` and retain specialised sizes for `.big-button` (240×48), `.choice-button` (360×64, wrapping), `.searcher` (260px wide), `.end-button` (170×44), `.discard-button` (170×32), `.activate-button`, `.again-button`, `.cancel-intent`, `.journal-button`, `.map-style-button`, `.whole-plan-button`. Use 12px horizontal inset; multi-line choices need enough vertical padding to avoid clipping.

| State | Surface / text | Shape and cue |
| --- | --- | --- |
| Normal | raised / text | 1px edge, inset lip; no glow |
| Hover `:hover` | hover / text | brass edge; 120ms colour change; same dimensions |
| Active `:active` | pressed / text | lip disappears, content translates down 1px; no layout reflow |
| Disabled `:disabled` | disabled / text-muted | quiet edge, no lift; retain full text opacity |
| Selected `.is-selected` | selected / text | 2px focus border + inset brass check/tab; persistent after hover ends |
| Keyboard `:focus` | current surface / text | visible 2px focus outline via border or separate non-picking frame; never remove focus feedback |

Disabled wins over hover/active; selected persists through hover; focus remains visible on selected controls. Only classes already toggled by code acquire automatic behaviour. `.choice-button.is-selected` and `.discard-button.is-selected` already exist; do not assume every button receives `.is-selected`. No clickability implied on passive plaques.

### Panels and screen composition

`.menu-panel`, `.end-box`, `.journal-page`, `.map-side`, `.log-panel` use a shared folio frame with different padding, not unique giant images. Standard padding 24px, compact 12px; gaps 8/16/24. Modal scrims cover the whole document; preserve existing input capture and hidden logic. The fight middle remains open over the stage; hand, log, top bar and fighter plaques have opaque text wells. Without the stage, `.screen` gets the slate field and restrained architectural watermark, so it is a complete designed screen. Do not invent a fake 3D scene requirement.

### Cards

`.card` stays 150×210; `.card-owners .swatch` supplies ownership rails. Single owner uses the matching tinted card surface; dual uses panel fill, **two equal unsuppressed rails** and marks. Mark elements require a future presentation-only UXML/C# addition; current card UXML has no portrait slot. Keep `owners`, `cost`, `name`, `target`, `text`, `owner-names` query names intact. Proposed mark children are additive and bind from existing owner definitions.

| State/class | Treatment |
| --- | --- |
| Hero-owned `.card.owner-*` | Coloured 8px top rail, neutral binding, matching mark rim and named footer; no tinted text |
| Dual | Equal split rails separated by void; two 20px marks; both names; no single-owner background dominance |
| Hover | Lift 10px in 120ms, brass edge; retain readable text |
| `.is-unplayable` | Recess text well, muted readable text, cost unchanged in clarity; keep ownership rails at full saturation/opacity, no hover lift; existing hint states reason |
| `.is-selected` | Lift 24px and 2px focus frame; keep ownership rails and card text surface |
| `.is-highlighted` | Lift 12px and focus edge for the existing highlight; selected lift wins if both apply |
| `.is-boosted` | Benefit-coloured corner notch / small upward chevron outside rules; do not replace hero rail or selected border |
| `.is-discounted` | Benefit-filled cost coin with ink number, small downward notch; continue displaying the real payable number, not an invented discount value |

Priority is compositional: ownership always visible, selected owns outer frame, boosted uses its own corner, discount owns coin. Unplayable removes interactive lift and uses muted text but does not erase any of those meanings. Future notches may need additive decorative children; don't rely on CSS pseudo-elements. Cost contrast takes precedence over all effects.

### Heroes, enemies, bars and feedback

`.combatant.hero`: 230px flat / 200px `.has-stage` width, 12px inset, owner accent and mark at left of name, title secondary. `.is-active` adds owner-coloured rail plus an explicit “Active” label and brass inset marker; `.is-targetable` adds focus corners, hover changes fill only (avoid projected-panel collision from scaling). `.is-acting` has a danger side tick for the existing action, not a permanent enemy glow. `.is-down` retains full-opacity readable name, 0 health and “Down”; dim mark only, replacing current whole-panel dimming in future styling.

`.combatant.enemy`: iron-black folio with a warning inset for intent, no hero ownership rail. Keep name, health, statuses and optional `.cancel-intent` in that order. Fighters now idle, attack, gesture, flinch, stagger, guard and fall in place; fallen models can remain lying down. Keep opaque plaques, intent wells, log and hand readable throughout motion and bright lighting, with no art that assumes a static silhouette or that downed models disappear. Preserve event-timed stage actions, floating numbers and normal/fast timing. Respect stage projection/collision code, never set a fixed screen coordinate in USS. No portrait is required for enemies in the first pass; the name and intent have priority.

`.bar`: 22px-high recessed trough with 1px rim; `.bar-fill` is a flat dark crimson horizontal fill, `.bar-text` a centred 14px semibold number over either region. Do not round health or fake preview state in art. Block remains a separate shield badge, not a second health fill. `.incoming-badge` says Incoming N; `.is-lethal` adds broken-heart/warning triangle outline and danger edge, retaining warning-surface fill rather than bright red beneath white text.

`.badge`, `.status`: minimum 24px height, 16px icon + 4px gap + 14px label/count, 6px inset, badge-surface fill. Preserve labels, not icon-only stacks. `.block-badge` uses shield; `.guard-badge` uses linked shields; status-specific selectors use coloured **icons/edges**, never unreadable saturated fills. Wrap badges with 4px gaps; never overlap health. For dynamically generated Riposte/Fury, retain the actual class generated by code; adding art mapping must not rename status strings.

`.intent`: 6px inset, warning-surface, 20px action icon, 16px action plus amount; `.intent-target` 14px owner name/mark; `.intent-outcome` 14px danger text. A target is never colour alone. `.is-vague` shows the existing vague line and veiled-bell icon only: **no numbers, target mark or outcome that leaks knowledge**. `.is-cancelled` keeps readable secondary text, a broken seal and “Cancelled” wording supplied by presentation code; no CSS strikethrough assumption. `.cancel-intent.is-unavailable` uses disabled styling at full text opacity. Do not change eligibility rules.

`.floater-damage`, `.floater-block`, `.floater-heal`, `.floater-status`: danger, Steel, benefit and brass respectively, with an opaque small recess plaque so they read against bright stage light. Retain the existing 1.2s movement/fade and `.floater-gone`; fast-animation setting continues controlling existing timing. `.log-line` 14px secondary, round headings brass, terminal lines primary; shared custom scrollbar. Avoid constant shimmer.

### Floor-plan tiles, doors and chart

`.plan-board`: void board, no preview of unknown rooms. `.tile`: thin slab-wall frame, quiet panel fill, room-colour edge; icon and `.tile-name` label centre on a quiet well. Art adapts to **arbitrary rectangles**; not one square image per whole room. `.tile-passage` is visibly part of the same stone room with no decorative door. Geometry and actual door positions continue coming from FloorPlan, never a mockup. Don't paint nonexistent exits into a texture.

With optional images, `Dress` places a scale-to-fit `.tile-art` square centrally, its side the room's narrower dimension, without cropping/stretching the picture. `Pave` repeats the local floor at the supplied slab scale; passages pave too. Preserve that inline image/size/repeat binding. `.tile.has-art` denotes paving, not necessarily every picture: a picture can exist without a floor, so `.tile-name` always needs an opaque panel label well, including `.has-art .tile-name`; outline/shadow alone is insufficient. Keep labels above pictures, including the rotated label in a tall one-cell-wide room. `.tile-stairs` also needs an opaque well. Use a separate transparent non-picking room-wall overlay above floor/picture and below labels, rather than overwriting the tile's background image. Clip pictures inside the walls; keep type-colour inset rail and current mark visible over bright imagery. `.tile.has-art.is-current` and `.is-current > .tile-art` can retain brighter image tint; visited pictures may dim, never their text. Doors render above room frames, and the `.map-lines` tag layer stays above pictures and doors; no prop or frame may hide the threshold or block a tag hit target. Plain original `.tile` and `.tile-passage` fills get exactly the same frames, labels, states and doors when images are absent. No new room paintings or local-art derivatives are commissioned.

`.tile.is-current` keeps the room accent inset and adds focus outer edge plus current-location mark. `.is-new` retains the existing arrival animation; `.is-dark` is recess, unknown geometry remains hidden; `.is-unexplored` is a debug-only dark plate with a “not entered” mark instead of lowering text opacity. Preserve `.tile-entrance`, `.tile-combat`, `.tile-elite`, `.tile-investigation`, `.tile-event`, `.tile-sanctuary`, `.tile-secret`, `.tile-boss`.

`.door` shut: iron latch and no bright rim. `.door.is-open`: pale brass jamb and an open gap; `.door.is-travelled`: brass threshold plus a short route tick. Travelled is historical, open means available; if both occur, the open gap and travelled tick coexist. `.door-tag`: opaque selected-tone label, room icon + known type only, 2px edge; hover focus rim, active pressed fill. `.is-pulse` may animate decorative rim opacity but **label opacity stays 1**, replacing current text pulsing. Existing door hit geometry must not shrink to the thin visible sprite. Minimum 32px hit target where layout permits; never extend an invisible target across an unrelated door.

`.tile-stairs`, `.stairs-tag`: stair icon and existing text; `.cellar-title` “Beneath the Keep” appears only when revealed. Cellar gets the same frame, not an arbitrary new map node. West entrance/east lair and all mystery visibility rules remain intact.

Chart `.room`: 128×56 iron plaque with 20px room icon and name; `.room-boss` 150×72. `.is-reachable` adds brass corner tabs; hover raises fill, active presses it. `.is-visited` adds a route tick, `.is-current` focus perimeter, `.is-closed` readable muted name and shut notch rather than whole-element opacity. `.map-entrance.is-start` has the current mark. `.room-glow` is a prepainted soft edge or supported border opacity, not USS shadow. Existing Painter2D links: future code should map unvisited to edge-quiet, available to brass, travelled to focus, with line styles/markers as extra distinctions. These C# literals cannot be fixed by USS alone; list this as a later presentation task, not an accomplished change.

### Settings volume sliders

The existing `.menu-slider` controls `#master-volume` (label “Volume”), `#music-volume` and `#effects-volume` are 0–1 continuous Sliders, already bound live and restored by `GameMenus`. Preserve their values and callbacks. Keep the existing 300px control width and 130px minimum label allocation where it fits, 18px primary labels, minimum 32px interaction height. Scope `.menu-slider > .unity-label`, `.menu-slider .unity-base-slider__tracker`, `.unity-base-slider__dragger-border` and `.unity-base-slider__dragger` after checking Unity 6's generated hierarchy. Scrollbar rules must not accidentally restyle audio controls.

Track: 8px recess trough with 1px edge rim. Fill: brass strip from minimum to the real thumb position, inside trough; add a non-picking value-bound child if Unity supplies no fill element (do not invent an existing fill selector). Dragger: 20×28px slate grip with 2px edge and two quiet cut lines, with a larger built-in hit area. Normal uses raised grip; hover uses hover grip and brass border; active/dragging uses pressed grip, focus border and inset lip; keyboard focus adds a persistent 2px focus bracket to the whole slider without obscuring label or thumb. Disabled uses disabled grip, edge-quiet track and text-muted label, full text opacity; retain the actual value position and a muted readable fill. Minimum/zero has no fill and a fully visible thumb; maximum has full fill without hiding end caps. Focus coexists with hover/dragging; disabled wins over pointer states. No selected/checked state is implied for a continuous value. Preserve built-in click, drag and keyboard behaviour; no new audio mechanics or promised numeric readout.

### Toggles, scrollbars, journal and top bar

`.menu-toggle`: 28×28 iron checkbox, 16px ivory check on checked, label 18px. Hover brass rim, active pressed inset, disabled muted label and quiet rim, selected/checked persistent check; `:focus` focus rim. Scope internal `.unity-toggle__input` / `.unity-toggle__checkmark` through `.menu-toggle` and confirm actual Unity 6 generated hierarchy in UI Debugger. `:checked` belongs to the Toggle state; don't invent `.is-checked`. Long “Map as a chart…” label wraps and panel height grows.

ScrollView vertical rail: 10px recess track, minimum 28px thumb with 2px inner clearance and edge-colour fill; hover brass, dragging focus, disabled quiet, keyboard focus outlined. Style scoped `.unity-scroller`, `.unity-base-slider__tracker`, `.unity-base-slider__dragger` after hierarchy verification. Preserve built-in scrolling, wheel, keyboard and horizontal handling. Do not paint nonfunctional arrows; retain and style built-in arrow buttons if shown.

`.journal-panel`: scrim; `.journal-page` 820px wide, max-height 90%, dark paper with 32px binding margin, 28px other inset. `.journal-scroll` handles overflow, not clipped entries. `.journal-entry`: 3px investigation rail, found clue name 20px brass, body 16px primary and effect 14px secondary if separated by future markup. `.is-unknown` has an empty seal and readable “Unknown”/existing placeholder, no clue-shaped illustration or spoiler title. Keepsakes use keyring mark and existing entry text. Close remains reachable below scrolling content.

`.top-bar`: 48px opaque recess with 1px brass rule, 20px horizontal inset, flex layout. `.place` brass; `.round` and `.prompt` primary; `.health-chip` owner mark, name and health, not just a coloured dot; `.deck-count` deck/keyring icon plus existing counts; `.seed` 13px muted; `.journal-button` ledger icon and text. Keep run and combat content as currently bound: combat UXML does not currently include all run chips/Journal. Do not imply adding a new control to combat is an art-only change. Never truncate critical health or turn guidance to keep a seed visible.

### Title, endings and remaining screens

Title uses an original architectural **ledger-cover watermark** against void even without a local backdrop. `.has-backdrop` retains existing left-column composition when optional art is present. Replace the six-band illusion visually with one 256×16 horizontal alpha ramp on `.title-shade-1`, width 70%, full height; make `.title-shade-2` through `-6` visually empty without deleting or renaming them. First 60% of ramp is solid void, next 40% fades to transparent. Restrict menu text to the opaque portion or add an opaque folio under it; a fade alone cannot guarantee contrast against arbitrary imagery. No CSS gradient. `.game-title` is live text, not baked artwork.

Combat `.end-panel .end-box`: opaque folio over scrim, 520px width, 28px inset, title and real outcome text; shared 64px seal with closed/cracked treatments for victory/defeat. Run `#end-view`: narrative folio, `.run-summary` 16px secondary, clue count shown from state. Standard and true ending share assets; a broken-seal accent may distinguish the true ending **only from the existing ending result**, never from client-side inference. The New expedition / Fight again buttons remain real controls.

| Screen | Composition and reused material |
| --- | --- |
| Title / Pause / Settings | Cover watermark + shade / compact menu folio / same folio with custom toggles and three volume sliders; preserve web Quit hiding |
| Party | Twelve compact `.party-hero` folios in two rows of six, live `.party-portrait` or signature mark, names and owner rails; shared `.party-detail` below lists health, four starters and relevant dual cards. See roster specification below. |
| Reward | Three standard cards on slate; Victory heading, optional `.found-clue` on ledger slip with clue/keyring emblem; Skip subordinate but readable |
| Sanctuary / Reflect | Candle emblem over shared narrative folio, Rest and Reflect buttons; Reflect uses standard cards and custom scroll rail, no unique deck art |
| Investigation | Open ledger emblem, narrative, explicit hero/cost `.searcher` buttons; `.is-unavailable` uses muted readable text, not 45% whole opacity |
| Secret room | Cistern stair/water emblem; same folio and two choices, costs remain live text |
| Event | Torn register emblem; narrative, existing choices, selected choice rim and hero-payment step use button system; result remains real text |
| Remove a card | Torn-card emblem, `.deck-grid-scroll` and standard cards; label confirms this is removal, not a reward |
| Found clue / Keepsake | Open seal or keyring emblem on ledger slip; actual name/effect text, Return button |
| Unwritten room | Plain empty folio emblem, live fallback explanation and Return button; fully themed rather than Unity default |
| Run end / combat end | Shared seal and folio, outcome-specific text and restrained edge, no giant win/lose painting |
| Web loading | Proposed reuse of cover watermark, ink/ivory/brass palette and dark health-red progress fill; Georgia remains a local browser fallback until separately packaged web fonts are justified. HTML/CSS selectors are not USS; implementation lies outside this documentation task |

### Party roster and live portraits

`#party-view` retains `#party-heroes` / `.party-heroes`: wrapping row, max-width 1200px, two rows of six at reference size. Keep `.party-hero` at 172px with 5px vertical / 7px horizontal margins; full-opacity name (15px semibold), title (13px secondary), owner rail and check on `.is-chosen`. Hover uses hover fill and brass edge; active pressed fill; keyboard focus uses the universal focus frame when focusable. Do not imply keyboard selection is already wired to these ClickEvent panels. Replace current whole-panel opacity dimming with readable surfaces. At narrower sizes validate fit/scrolling rather than clipping twelve choices or shrinking names.

`.party-portrait` keeps its 150px-high scale-to-fit **live full-length** RenderTexture from `HeroPortraits` (256×512, one camera per available model, idle motion). Add a transparent folio frame as a non-picking overlay; don't replace the background image that holds the live texture. Use owner card fill behind the figure and a narrow accent rim. Preserve camera disposal when leaving party. `.party-portrait-initial` is the current fallback Label: replace its initial visually with the hero's 64×64 signature mark in a future presentation binding, preserving the class/query contract and readable hero name outside the frame. Do not capture, export or commit licensed model renders. Mixed live/missing-model rosters must look intentional. Every other former card/panel portrait slot uses the same mark, including card footers, fighter names, health chips and intent targets; owner-medal holds a mark.

`.party-detail` is an opaque panel folio below the roster (max-width 1160px, 12px inset); `.party-detail-name` uses 16px semibold primary/brass heading, `.party-detail-cards` 14px primary with 18px line allocation and wrapping. `#party-detail-name` and `#party-detail-cards` remain live Labels. Pointer enter shows that hero; pointer leave restores the last chosen hero, or the existing prompt if none. Show four own starting cards plus duals with already chosen partners, exactly as `ShowPartyDetail` binds them; do not show all eleven possible partners. Allow height for three shared cards when pointing at an unselected hero with three chosen. Keep `#party-hint` and `#enter-keep` reachable at 0–3 selections; no card summaries on each compact roster tile.

## 6. Icon language and complete production inventory

Original monochrome cut-metal silhouettes: 2px minimum strokes at 24px display, 2px breathing space, no internal detail below 2px. Author at 64×64 PNG RGBA, display at 16/20/24 or 32px; larger room emblems at 48–64. White source can be tinted by supported UI image tint; keep secondary colour in a separate element. Each icon remains accompanied by a live label and, where appropriate, a number. Never use emoji/font glyphs as final art. Use the same shield for status Block and Block intent, with contextual labels.

| Category | File stem(s) under `Icons/` | What it shows / usage |
| --- | --- | --- |
| Rooms | `room-entrance`, `room-combat`, `room-elite`, `room-investigation` | Open portcullis; single nicked sword; notched closed helm; open ledger with one ruled line |
| Rooms | `room-event`, `room-sanctuary`, `room-secret`, `room-boss` | Torn hanging register leaf; single chapel candle; descending steps above water line; sealed iron door with broken wax ring |
| Statuses | `block`, `guarded`, `exposed`, `weak` | Solid shield; two linked shields; split shield with outward rays; bent sword with falling tip |
| Statuses | `bleed`, `hex`, `riposte`, `fury` | One drop with cut notch; open eye inside broken ring; returning hooked blade; three rising blade teeth |
| Intents | `attack`, `attack-all`, `block`, `heal` | Forward sword; three fanned swords; same shield; upright cross with rising small ray |
| Intents | `apply-status`, `buff`, `debuff`, `wait`, `unknown`, `cancelled` | Ring with one square stack; rising chevron; falling chevron; resting bell; bell behind closed veil; snapped seal bar |
| Resources | `resolve`, `block` | Four-cut candle spark in a round counter; shield (shared) |
| Navigation | `deck`, `draw`, `discard`, `journal`, `keepsake` | Three offset cards; card with inward arrow; card with outward arrow; bound ledger with tab; keyring |
| Evidence / outcomes | `clue`, `seal-whole`, `seal-broken`, `defeat`, `remove-card`, `empty-folio` | Open wax seal; intact seal; split seal; extinguished candle; torn card; blank bound leaf |
| UI / map | `check`, `lock`, `current`, `stairs`, `warning`, `boost`, `discount` | Angular check; shut latch; triangular expedition pin; three steps; triangle and cut; upward notch; downward notch |
| Hero identity (additional) | `hero-smoke`, `hero-ember`, `hero-ink`, `hero-rose`, `hero-verdigris`, `hero-tallow` | Cut cord; cinders; quill/leaf; split star; veiled coronet; torch (see identity table) |
| Hero identity | `hero-iron`, `hero-thorn`, `hero-ash`, `hero-bone`, `hero-steel`, `hero-bronze` | Shield; split leaf; open eye; stitched cross; paired blades; bell |

Status-application intent pairs `apply-status` with the relevant status silhouette and textual amount. Attack + Block or Attack + Hex uses a compact two-icon group, not a new ambiguous glyph. Heal, Fury/buff, Weak/debuff and wait are visual vocabulary for existing data; do not add actions. In particular **Stagger is not a new status badge**: show the action's existing Weak/result text. For vague boss intentions use only `unknown` and the provided line. Final mapping should enumerate actual intent data during implementation so no enemy falls back to a misleading attack symbol.

## 7. Per-asset upgrade schedule

There are no committed UI images to replace. **Every “new” path below is proposed under `Unity/Assets/UI/Art/`; nothing there is created by this task.** Dimensions are source pixels; slice notation L/T/R/B. `0` means unsliced. Transparent frame assets sit above the token fill; draw decoration behind text. Rows with multiple named files explicitly specify separate exports with the same dimensions/slice spec. No baked words or numerals.

| Current → proposed path | New art | Dimensions | Slice | Existing consuming selector / integration |
| --- | --- | --- | --- | --- |
| new `Art/Surfaces/slate-field.png` | Quiet cold slate, broad seam watermark | 256×256 | 24/24/24/24 | `.screen`, `.title-view`, `.plan-board` |
| new `Art/Frames/folio.png` | Thin binding, repaired brass corners, clear centre | 128×128 | 16/16/16/16 | `.menu-panel`, `.journal-page`, `.end-box`, `.map-side`, `.party-detail`; `.party-detail-name` and `.party-detail-cards` use live text, no separate images |
| new `Art/Frames/narrative.png` | Wider binding seam, one small stitched corner | 128×128 | 24/16/16/16 | `#sanctuary-view`, `#investigation-view`, `#secret-view`, `#event-view`, `#found-view`, `#unwritten-view`, `#end-view` via future inset decorative child or background on existing view |
| new `Art/Frames/strip.png` | Single thin iron rule, near-flat centre | 64×64 | 4/4/4/4 | `.top-bar`, `.bottom`, `.log-panel`, `.found-clue`, `.run-summary` |
| new `Art/Controls/button.png` | Cut iron plate with shallow lip | 96×48 | 10/10/10/12 | `.unity-button` including all choice, menu and combat buttons |
| new `Art/Controls/button-hover.png` | Same silhouette, warm narrow rim | 96×48 | 10/10/10/12 | `.unity-button:hover` |
| new `Art/Controls/button-pressed.png` | Same corners, compressed lip | 96×48 | 10/10/10/12 | `.unity-button:active` |
| new `Art/Controls/button-disabled.png` | Cool quiet rim, no bright fitting | 96×48 | 10/10/10/12 | `.unity-button:disabled`, `.searcher.is-unavailable`, `.cancel-intent.is-unavailable` |
| new `Art/Frames/focus.png` | Transparent corner brackets; centre transparent | 64×64 | 8/8/8/8 | `.choice-button.is-selected`, `.discard-button.is-selected`, `.party-hero.is-chosen`, `.card.is-selected`, focused controls; additive frame if base background occupied |
| new `Art/Cards/binding.png` | Minimal folio binding, 2px visual edge | 150×210 | 10/10/10/10 | `.card` in combat, reward, reflect and remove grids |
| new `Art/Cards/cost.png` | Pale paper counter with iron rim, no number | 64×64 | 0 | `.card-cost` (number remains live); neutral source/tint for discount |
| new `Art/Cards/owner-medal.png` | Small flat signature-mark rim with clear centre | 48×48 | 0 | `.card-owner-names` footer's proposed mark children, `.health-chip`, `.party-hero` |
| new `Art/Frames/portrait-frame.png` | Transparent folio border over live model or fallback mark; clear centre | 96×96 | 10/10/10/10 | `.party-portrait` via non-picking overlay, preserve RenderTexture |
| new `Art/Frames/fighter.png` | Compact repaired slate plaque, clear name well | 96×96 | 10/10/10/10 | `.combatant.hero`, `.combatant.enemy`, `.party-hero` |
| new `Art/Frames/intent.png` | Narrow warning slip with cut left corner | 64×64 | 8/8/8/8 | `.intent`, `.intent.is-vague`, `.intent.is-cancelled` via fill/icon changes |
| new `Art/Frames/badge.png` | Tiny square-cut metal backing | 48×48 | 6/6/6/6 | `.badge`, `.status`, `.floater` |
| new `Art/Bars/health-track.png` | Recessed iron trough | 64×24 | 5/4/5/4 | `.bar` |
| new `Art/Bars/health-fill.png` | Flat dark red strip with quiet lower lip | 32×16 | 2/2/2/2 | `.bar-fill`; clip to real width |
| new `Art/Map/room-wall.png` | Original top-down slab boundary, no painted exits | 96×96 | 12/12/12/12 | `.tile`, all eight `.tile-<kind>` variants |
| new `Art/Map/name-plate.png` | Opaque panel backing, quiet rim, no baked label | 64×32 | 6/6/6/6 | `.tile-name`, `.has-art .tile-name`, `.tile-stairs`; same over plain tiles and pictures |
| new `Art/Map/passage.png` | Thin side rails, unmarked floor centre | 48×48 | 4/4/4/4 | `.tile-passage` |
| new `Art/Map/door-shut.png` | Closed latch across threshold, no glow | 24×48 | 4/8/4/8 | `.door` (vertical-wall orientation; rotate for alternate orientation only if geometry needs it) |
| new `Art/Map/door-open.png` | Pale jambs and visible centre gap | 24×48 | 4/8/4/8 | `.door.is-open` |
| new `Art/Map/door-travelled.png` | Brass threshold and route tick | 24×48 | 4/8/4/8 | `.door.is-travelled`; tick overlay when also open |
| new `Art/Map/door-tag.png` | Quiet opaque destination label plate | 64×48 | 8/8/8/8 | `.door-tag`, `.stairs-tag` |
| new `Art/Map/chart-room.png` | Small iron itinerary plaque | 96×56 | 10/10/10/10 | `.room`, `.room-boss`, `.map-entrance` |
| new `Art/Map/reachable-rim.png` | Soft prepainted brass outer rim, no text | 96×64 | 16/16/16/16 | `.room-glow` |
| new `Art/Controls/toggle-box.png` | Recessed square checkbox | 32×32 | 6/6/6/6 | `.menu-toggle .unity-toggle__input` after hierarchy check |
| new `Art/Controls/toggle-check.png` | Angular ivory cut mark | 32×32 | 0 | `.menu-toggle .unity-toggle__checkmark` in checked state |
| new `Art/Controls/volume-track.png` | Recess trough, neutral end caps | 96×16 | 5/4/5/4 | Scoped `.menu-slider .unity-base-slider__tracker` |
| new `Art/Controls/volume-fill.png` | Neutral tintable strip, flat centre | 32×8 | 2/2/2/2 | Proposed value-bound non-picking child in all three `.menu-slider` tracks |
| new `Art/Controls/volume-grip.png` | Slate grip with cut lines, tint for normal/hover/drag/disabled | 20×28 | 4/4/4/4 | Scoped `.menu-slider .unity-base-slider__dragger`; focus.png for focus/drag frame, border wrapper kept clear |
| new `Art/Controls/scroll-track.png` | Narrow dark channel | 12×48 | 3/6/3/6 | Scoped `.unity-base-slider__tracker` in `.log`, `.journal-scroll`, `.deck-grid-scroll` |
| new `Art/Controls/scroll-thumb.png` | Slate slider with tiny grip | 12×48 | 3/8/3/8 | Scoped `.unity-base-slider__dragger`; hover/active tints |
| new `Art/Journal/entry.png` | Unruled text well, left binding stitch | 96×64 | 12/8/8/8 | `.journal-entry`, `.journal-entry.is-unknown` |
| new `Art/Title/keep-ledger.png` | Original abstract keep ground-plan embossed in a ledger cover; no local reference | 1024×576 | 0 | `.title-view` background; scale-and-crop; optional cover motif on flat `.screen` |
| new `Art/Title/title-fade.png` | Void-to-transparent alpha ramp, opaque text end | 256×16 | 0, stretch | `.title-shade-1`; other five shade bands empty |
| new `Art/Icons/room-entrance.png` | Open portcullis; original white silhouette | 64×64 | 0 | `.tile-entrance, .map-entrance` via additive icon child; preserve parent selector |
| new `Art/Icons/room-combat.png` | Single nicked sword; original white silhouette | 64×64 | 0 | `.tile-combat, .room-combat` via additive icon child; preserve parent selector |
| new `Art/Icons/room-elite.png` | Notched closed helm; original white silhouette | 64×64 | 0 | `.tile-elite, .room-elite` via additive icon child; preserve parent selector |
| new `Art/Icons/room-investigation.png` | Open ledger; original white silhouette | 64×64 | 0 | `.tile-investigation, .room-investigation` via additive icon child; preserve parent selector |
| new `Art/Icons/room-event.png` | Torn register leaf; original white silhouette | 64×64 | 0 | `.tile-event, .room-event` via additive icon child; preserve parent selector |
| new `Art/Icons/room-sanctuary.png` | Single candle; original white silhouette | 64×64 | 0 | `.tile-sanctuary, .room-sanctuary` via additive icon child; preserve parent selector |
| new `Art/Icons/room-secret.png` | Steps over water; original white silhouette | 64×64 | 0 | `.tile-secret, .room-secret` via additive icon child; preserve parent selector |
| new `Art/Icons/room-boss.png` | Sealed iron door; original white silhouette | 64×64 | 0 | `.tile-boss, .room-boss` via additive icon child; preserve parent selector |
| new `Art/Icons/block.png` | Solid shield; original white silhouette | 64×64 | 0 | `.block-badge, .intent` via additive icon child; preserve parent selector |
| new `Art/Icons/guarded.png` | Linked shields; original white silhouette | 64×64 | 0 | `.guard-badge` via additive icon child; preserve parent selector |
| new `Art/Icons/exposed.png` | Split shield and rays; original white silhouette | 64×64 | 0 | `.status-exposed` via additive icon child; preserve parent selector |
| new `Art/Icons/weak.png` | Bent sword; original white silhouette | 64×64 | 0 | `.status-weak` via additive icon child; preserve parent selector |
| new `Art/Icons/bleed.png` | Notched drop; original white silhouette | 64×64 | 0 | `.status-bleed` via additive icon child; preserve parent selector |
| new `Art/Icons/hex.png` | Eye in broken ring; original white silhouette | 64×64 | 0 | `.status-hex` via additive icon child; preserve parent selector |
| new `Art/Icons/riposte.png` | Returning hooked blade; original white silhouette | 64×64 | 0 | `.statuses .status` via additive icon child; preserve parent selector |
| new `Art/Icons/fury.png` | Three blade teeth; original white silhouette | 64×64 | 0 | `.statuses .status` via additive icon child; preserve parent selector |
| new `Art/Icons/attack.png` | Forward sword; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/attack-all.png` | Fanned swords; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/heal.png` | Cross and rising ray; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/apply-status.png` | Stack ring; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/buff.png` | Rising chevron; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/debuff.png` | Falling chevron; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/wait.png` | Resting bell; original white silhouette | 64×64 | 0 | `.intent` via additive icon child; preserve parent selector |
| new `Art/Icons/unknown.png` | Veiled bell; original white silhouette | 64×64 | 0 | `.intent.is-vague` via additive icon child; preserve parent selector |
| new `Art/Icons/cancelled.png` | Snapped seal bar; original white silhouette | 64×64 | 0 | `.intent.is-cancelled` via additive icon child; preserve parent selector |
| new `Art/Icons/resolve.png` | Four-cut spark; original white silhouette | 64×64 | 0 | `.resolve-caption, .card-cost` via additive icon child; preserve parent selector |
| new `Art/Icons/deck.png` | Offset cards; original white silhouette | 64×64 | 0 | `.deck-count` via additive icon child; preserve parent selector |
| new `Art/Icons/draw.png` | Card with inward arrow; original white silhouette | 64×64 | 0 | `.piles` via additive icon child; preserve parent selector |
| new `Art/Icons/discard.png` | Card with outward arrow; original white silhouette | 64×64 | 0 | `.discard-button, .piles` via additive icon child; preserve parent selector |
| new `Art/Icons/journal.png` | Bound ledger; original white silhouette | 64×64 | 0 | `.journal-button` via additive icon child; preserve parent selector |
| new `Art/Icons/keepsake.png` | Keyring; original white silhouette | 64×64 | 0 | `.deck-count, .journal-entry` via additive icon child; preserve parent selector |
| new `Art/Icons/clue.png` | Opened seal; original white silhouette | 64×64 | 0 | `.found-clue, .journal-entry` via additive icon child; preserve parent selector |
| new `Art/Icons/seal-whole.png` | Intact seal; original white silhouette | 64×64 | 0 | `.end-title, #found-view` via additive icon child; preserve parent selector |
| new `Art/Icons/seal-broken.png` | Split seal; original white silhouette | 64×64 | 0 | `#end-view, #found-view` via additive icon child; preserve parent selector |
| new `Art/Icons/defeat.png` | Extinguished candle; original white silhouette | 64×64 | 0 | `.end-title, .combatant-state` via additive icon child; preserve parent selector |
| new `Art/Icons/remove-card.png` | Torn card; original white silhouette | 64×64 | 0 | `#remove-view, .reflect-panel` via additive icon child; preserve parent selector |
| new `Art/Icons/empty-folio.png` | Blank bound leaf; original white silhouette | 64×64 | 0 | `#unwritten-view, .journal-entry.is-unknown` via additive icon child; preserve parent selector |
| new `Art/Icons/check.png` | Angular check; original white silhouette | 64×64 | 0 | `.party-hero.is-chosen, .choice-button.is-selected` via additive icon child; preserve parent selector |
| new `Art/Icons/lock.png` | Shut latch; original white silhouette | 64×64 | 0 | `.card.is-unplayable` via additive icon child; preserve parent selector |
| new `Art/Icons/current.png` | Expedition triangle; original white silhouette | 64×64 | 0 | `.tile.is-current, .room.is-current` via additive icon child; preserve parent selector |
| new `Art/Icons/stairs.png` | Three steps; original white silhouette | 64×64 | 0 | `.tile-stairs, .stairs-tag` via additive icon child; preserve parent selector |
| new `Art/Icons/warning.png` | Cut triangle; original white silhouette | 64×64 | 0 | `.incoming-badge.is-lethal` via additive icon child; preserve parent selector |
| new `Art/Icons/boost.png` | Upward notch; original white silhouette | 64×64 | 0 | `.card.is-boosted` via additive icon child; preserve parent selector |
| new `Art/Icons/discount.png` | Downward notch; original white silhouette | 64×64 | 0 | `.card.is-discounted` via additive icon child; preserve parent selector |
| new `Art/Icons/hero-iron.png` | Shield; original white silhouette | 64×64 | 0 | `.owner-iron` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-thorn.png` | Split leaf; original white silhouette | 64×64 | 0 | `.owner-thorn` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-ash.png` | Open eye; original white silhouette | 64×64 | 0 | `.owner-ash` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-bone.png` | Stitched cross; original white silhouette | 64×64 | 0 | `.owner-bone` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-steel.png` | Paired blades; original white silhouette | 64×64 | 0 | `.owner-steel` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-bronze.png` | Bell; original white silhouette | 64×64 | 0 | `.owner-bronze` via additive mark child, including fallback `.party-portrait-initial`; preserve parent selector |
| new `Art/Icons/hero-smoke.png` | Silas: knotted cord with one cut end; original white silhouette | 64×64 | 0 | `.owner-smoke` mark consumers, including `.party-portrait-initial` replacement binding |
| new `Art/Icons/hero-ember.png` | Isidore: three rising cinders; original white silhouette | 64×64 | 0 | `.owner-ember` mark consumers, including `.party-portrait-initial` replacement binding |
| new `Art/Icons/hero-ink.png` | Aldous: quill over a ruled leaf; original white silhouette | 64×64 | 0 | `.owner-ink` mark consumers, including `.party-portrait-initial` replacement binding |
| new `Art/Icons/hero-rose.png` | Vesna: split four-point star; original white silhouette | 64×64 | 0 | `.owner-rose` mark consumers, including `.party-portrait-initial` replacement binding |
| new `Art/Icons/hero-verdigris.png` | Lady Isolde: veil beneath a broken coronet; original white silhouette | 64×64 | 0 | `.owner-verdigris` mark consumers, including `.party-portrait-initial` replacement binding |
| new `Art/Icons/hero-tallow.png` | Tam: torch with a square flame; original white silhouette | 64×64 | 0 | `.owner-tallow` mark consumers, including `.party-portrait-initial` replacement binding |
| `Unity/Assets/LocalArt/Resources/TitleBackdrop.jpg` → optional unchanged | No extraction or replacement derived from it; independent cover above is the redistributable default | Existing 2560×1440 | 0 | `.title-view.has-backdrop` optional runtime override only |
| `Unity/Assets/LocalArt/Resources/FloorTiles/` → optional unchanged | Neutral original room-wall set above supplies complete public map | Existing sizes unspecified | Not reused | Existing `.tile.has-art`, `.tile-art`, `.has-art .tile-name`, `.tile.has-art.is-current`, `.is-current > .tile-art`; preserve plain `.tile` fallback and public frame layer |
| `Unity/Assets/LocalArt/Resources/CombatStage.prefab` → optional unchanged | 3D stage is not a UI texture; flat slate mode and opaque plaques remain complete | N/A | N/A | `.has-stage` selectors only |
| new `Art/Fonts/Cinzel-SemiBold.ttf` + licence | Heading font from upstream; no texture lettering | Font, N/A | N/A | `.game-title`, `.view-title`, `.menu-heading`, `.end-title` via Font Asset |
| new `Art/Fonts/SourceSans3-Regular.ttf`, `SourceSans3-Semibold.ttf` + licence | Readable rules/numbers and UI weights | Font, N/A | N/A | `.screen`, `.menus` inherited; explicit semibold heading/control overrides |
| Web template existing inline CSS → shared export of `Art/Title/keep-ledger.png` | Reuse independent cover and palette, no new web painting | 1024×576 | 0 | Web-only `body`/loading UI; confirm exact HTML selectors in later web pass, not USS |

The `Art/` shorthand in the table always expands to `Unity/Assets/UI/Art/`. Reuse is intentional: reward, Sanctuary/Reflect, Investigation, secret, event, removal, found, fallback, both endings, title, pause, settings, map/chart, party, journal and combat all have a specified treatment without dozens of unique backgrounds. Hero backgrounds and room colours are USS fills, not twelve/eight redundant texture exports. State overlays and icons may need extra non-picking VisualElements; don't overwrite a frame background with an icon on the same element.

## 8. UI Toolkit implementation and delivery notes

- Retain all existing C#-driven classes and UXML query names. In particular preserve `.hidden`, `.has-art`, `.has-stage`, `.has-backdrop`, every `.owner-*`, `.is-active`, `.is-down`, `.is-targetable`, `.is-acting`, `.is-unplayable`, `.is-selected`, `.is-highlighted`, `.is-boosted`, `.is-discounted`, `.is-chosen`, `.is-reachable`, `.is-visited`, `.is-current`, `.is-closed`, `.is-start`, `.is-pulse`, `.is-new`, `.is-unexplored`, `.is-dark`, `.is-travelled`, `.is-open`, `.is-vague`, `.is-cancelled`, `.is-unavailable`, `.is-lethal`, `.is-unknown`, `.is-error` and `.floater-gone`. Restyle their effects; don't rename the contracts.
- Use `background-image: url(...)`, `-unity-background-scale-mode`, `-unity-slice-left/top/right/bottom`, `-unity-slice-scale`, solid colours, borders, flex sizing, supported translate/scale/opacity transitions and `-unity-font-definition`. No browser gradients, box-shadow, blend modes, filters, CSS Grid, pseudo-elements, CSS masks, `clip-path`, automatic font stacks or web animations. Painted ramp/rim assets replace gradients/shadows. Decorative images use `picking-mode="Ignore"` in UXML or equivalent Game presentation code.
- Example future rule (path relative to a USS in `Assets/UI/`):

```css
.menu-panel {
    background-color: var(--panel);
    background-image: url("Art/Frames/folio.png");
    -unity-background-scale-mode: stretch-to-fill;
    -unity-slice-left: 16;
    -unity-slice-top: 16;
    -unity-slice-right: 16;
    -unity-slice-bottom: 16;
    -unity-slice-scale: 1;
    padding: 24px;
}
```

- Style order matters: base → ownership/type → hover → active → selected/focus → disabled exceptions, with explicit combined rules for cards. Card boosted notches and discount coins remain independent. Preserve `.hidden` display none, including when specialising screens. `#event-view` is a UXML **name selector**, not a class invented from its name.
- C# currently creates swatches, statuses, floor tiles and chart rooms. Icons and marks need small additive presentation bindings; CSS cannot discover an owner, count, target, action or clue. Values still come from real state and commands. This proposal adds no Core logic or mechanics. Do not load optional proprietary art as the only background. Audit inline C# styles that may override USS, including MapView palette and optional art loaders, during implementation.
- Import PNGs as UI-suitable textures/Sprites, sRGB, alpha transparency, no mipmaps, bilinear filtering; inspect thin edges for atlas bleed with 4px padding/extrusion. UI Toolkit runtime atlas usage must be confirmed in the Editor; do not assume a SpriteAtlas alone guarantees UI Toolkit batching. Separate the cover from small shared controls. Do not ship sample sheets in runtime assets.
- Target **≤2 MiB additional compressed web download for UI art**, excluding fonts, and **≤3 MiB total including the initial Latin font set**; budgets require build measurement, not PNG file size alone. Aim for ≤16 MiB additional resident UI textures/atlases. One 1024×1024 RGBA atlas costs about 4 MiB before mipmaps; one 1024×576 cover about 2.25 MiB. Avoid per-card paintings and full-screen 4K backgrounds. Add glyphs deliberately for actual punctuation, digits and hero names; validate missing glyph fallbacks and static/dynamic atlas behaviour. Licence notices must survive font subsetting.
- Production source deliveries: layered editable originals, exported PNGs, font notices, provenance manifest, and matching Unity `.meta` files when importing in a later task. All art paths here are proposals; no Unity assets or `.meta` files are created now.

## 9. Samples and acceptance

See [samples/README.md](samples/README.md) for generated concepts, prompts, limitations and the precision companion. Concepts show material hierarchy, not trustworthy card text, exact state wiring, exact graph geometry or final font rendering. The numeric specification above takes precedence over generated details.

The Siege Ledger direction is approved; these additions retain its materials, typography and original tokens. Before accepting an implementation: inspect title with and without backdrop, flat combat and brightest stage lighting through attacks, casts, flinches, heavy-hit staggers, guard and persistent fallen bodies at normal/fast speed; panel collision, readable intents/log/hand and event-timed floaters; all twelve owners/all 66 content dual pairs at 150×210; long card rules, unplayable+boosted+discounted+selected combinations; all button states and keyboard focus; party selection at 0, 1, 2 and 3 choices, overflow click rejection, all twelve names/marks, live/missing/mixed portraits and camera disposal, pointed-at/last-chosen/empty detail panel including the longest shared-card list; every narrative/reward/fallback/end screen; Journal unknown/found/keepsake entries; floor plan unknown/reachable/travelled/cellar and F2/F3 debug views; floor-only, picture-only, both and no-image tiles, bright pictures under rotated labels/stairs, current/visited image tints and unobscured doors; all three sliders at zero/mid/max, hover/drag/disabled/focus and keyboard operation, live level changes and persisted values; toggles and scrollbars; loading page on web. Test Windows/macOS/WebGL at 1920×1080 and 1280×720, with a readability decision for scaled cards. Check contrast against **rendered** texture pixels, colour-vision simulations, safe atlas slices and measured web payload. No Unity rendering, runtime readability or build validation is claimed by this documentation-only deliverable.

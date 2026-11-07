# Task: the other nine hero portraits

David approved the three samples in `Docs/portraits/samples/` (`bram.png`, `isolde.png`, `tam.png`) as the style for
all twelve heroes. Make the other nine so all twelve look like one set.

Read `Docs/portraits/samples/prompts.md` first. For each new portrait, reuse its shared text exactly (use case, asset
type, art direction, composition, lighting, background, the no-frame/no-text rule and the single-mark rule) and change
only the **Subject** paragraph and the background colour. Use your built-in image generation tool.

## Rules

- **Text prompts only.** Use no reference images. Never look at, copy or imitate anything in `Unity/Assets/Synty/`,
  `Unity/Assets/Polygon*/` or `Unity/Assets/LocalArt/`. They are licensed 3D models, and this public repository must
  not contain derivatives of them. Don't imitate any existing game's art.
- Match the samples' framing: same head size and eye height, three-quarter view turned slightly to image left.
- Each hero's accent colour appears modestly in clothing or a clasp. Their mark appears **exactly once**, small, attached
  to clothing as a clasp, pin or embroidered emblem.
- No frame, border, text, lettering, logo, signature or watermark.
- Look at each image after generating it. Regenerate if it breaks a rule: a missing or doubled mark, text, a frame,
  framing that clearly doesn't match the samples, or a clearly wrong age or gender.

## The nine heroes

| File | Hero | Subject | Accent / background wash | Mark |
| --- | --- | --- | --- | --- |
| `wren.png` | Wren, the Hunter | Lean, sharp-eyed woman in her thirties, weathered face, hair tied back, an asymmetrical hood and a leather hunting jerkin; alert and focused | green `#71C596` / `#1B3028` | a leaf split down its midrib |
| `ashe.png` | Sister Ashe, the Occultist | Woman in her forties, a former nun, plain grey-violet veil and habit, a clasp at the throat, calm unsettling gaze | violet `#B39AE8` / `#2C253C` | an open eye |
| `corvin.png` | Corvin, the Penitent | Gaunt man in his forties, a healer who pays in his own blood, cloth wrap over the lower face, stitched high collar, bandaged hands not shown, tired kind eyes | warm ivory `#DCD5BE` / `#302E27` | a stitched cross |
| `mara.png` | Mara, the Duelist | Poised woman in her late twenties, swept-back dark hair, a fitted fencing doublet with a high collar, a clasp at the shoulder, cool appraising look | sky blue `#79BFE8` / `#1D2C3A` | two crossed blades |
| `hollis.png` | Hollis, the Bellwright | Sturdy man in his fifties, a bell-founder, heavy leather work apron over a shirt, scorched sleeves, broad hands not shown, patient expression | copper-red `#E78E79` / `#392420` | a bell |
| `silas.png` | Silas, the Cutpurse | Lean wiry man in his thirties, hood pushed back, a loose scarf at the throat, glancing sideways, a faint wry half-smile | pale smoke grey `#B4B8C7` / `#292B34` | a knotted cord with one cut end |
| `isidore.png` | Isidore, the Exile | Gaunt man in his sixties, a fire sorcerer, a travel-worn scorched mantle, lit faintly warm from below as well as the shared key light, intense hollow eyes | ember orange `#F5A36E` / `#39281F` | three rising cinders |
| `aldous.png` | Aldous, the Archivist | Elderly man in his seventies, a scholar wizard, high collar, spectacles optional, ink-stained fingers resting at the chin, a quill tucked behind one ear | ink blue `#92A8EC` / `#222B40` | a quill over a ruled leaf |
| `vesna.png` | Vesna, the Seer | Woman in her forties, a patterned headscarf and heavy earrings, eyes looking past the viewer, knowing and wary | rose `#EAA6CA` / `#372636` | a four-point star split down the middle |

## Output

- Save the images in `Docs/portraits/samples/` with the file names above, at the size the tool produces.
- Add each exact prompt to `Docs/portraits/samples/prompts.md` under its file name, after the existing three.
- Don't change the existing three images. Change nothing outside `Docs/portraits/`, and make no git commits.
- In your final reply, list the files you made, any you regenerated and why, and anything you couldn't do.

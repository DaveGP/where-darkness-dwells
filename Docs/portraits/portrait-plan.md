# Hero portraits

Twelve painted hero busts, made after the UI art upgrade. The portraits are in `samples/` and the game, and the
decisions are at the end of this file.

The first six descriptions come from the first version of the style guide. The last six are new, based on each hero's
title, role and mark in `GAME_DESIGN.md` and `style-guide.md`.

| Hero | Colour | Planned portrait | Mark to work in |
| --- | --- | --- | --- |
| Bram, the Warden | Iron | Broad warden bust with a square collar | Vertical shield |
| Wren, the Hunter | Thorn | Hunter bust with an asymmetrical hood | Split leaf |
| Sister Ashe, the Occultist | Ash | Occultist bust with a plain veil and a clasp | Open eye |
| Corvin, the Penitent | Bone | Penitent bust with a cloth face wrap and stitched collar | Stitched cross |
| Mara, the Duelist | Steel | Duelist bust with swept hair and a clasp | Paired blades |
| Hollis, the Bellwright | Bronze | Bellwright bust with a work apron and a clasp | Bell |
| Silas, the Cutpurse | Smoke | Lean thief bust, hood pushed back and a scarf loose at the throat, glancing sideways | Knotted cord with one cut end |
| Isidore, the Exile | Ember | Gaunt sorcerer bust in a travel-worn, scorched mantle, lit faintly from below | Three rising cinders |
| Aldous, the Archivist | Ink | Elderly scholar bust with a high collar, ink-stained fingers at the chin and a quill behind the ear | Quill over a ruled leaf |
| Vesna, the Seer | Rose | Seer bust with a patterned headscarf and heavy earrings, eyes looking past the viewer | Split four-point star |
| Lady Isolde, the Widow | Verdigris | Upright noblewoman bust in a mourning veil, wearing a broken coronet | Veil beneath a broken coronet |
| Tam, the Torchbearer | Tallow | Stocky labourer bust with a plain tunic and soot on the cheek, lit warmly from one side by a torch | Torch with a square flame |

## Rules

- 128x128 source images, unsliced, as the first guide specified. Decide the final size when the art is tried in the game.
- The repository is public, so portraits must be original. Never copy, trace or closely imitate the Synty models in
  `Unity/Assets/Synty/` or `Unity/Assets/LocalArt/`, even where a hero uses one of them on the fight stage.
- All twelve should look like one set: same framing, lighting and level of detail, in the Siege Ledger style.

## Decided (David, 2026-11-07)

- **How:** painted by Codex's image generation from text prompts only, so they can be committed. The prompts are in
  `samples/prompts.md`, and the requests to Codex are the `codex-*` files here.
- **Where:** the combat hero panels, the party screen, and the party row in the journal and on the run-end screen.
  Cards keep the signature marks.
- **Party screen:** the paintings replace the live 3D models everywhere, not only when the models are missing.
- **Style test:** Bram, Lady Isolde and Tam were approved first, then the other nine were made to match.
- **Heroes:** Wren, Sister Ashe, Mara, Vesna and Lady Isolde are women; the rest are men, matching their models.

`samples/` keeps 512px copies for reference. The game uses 256px copies in `Unity/Assets/UI/Art/Portraits/`.

# Task: three sample hero portraits for Where Darkness Dwells

Where Darkness Dwells is a dark-fantasy roguelite deckbuilder set in the Black Keep, a fortress after a failed siege.
Its UI style, "The Siege Ledger", is described in `Docs/ui-art-upgrade/style-guide.md`: soot-blue slate, restrained
brass, ivory text, cold shadow with warm restrained light, strong silhouettes. Read section 1 (art direction) and
section 2 (colour) of that guide, and `Docs/portraits/portrait-plan.md`, before you start.

Use your built-in image generation tool to make **three original painted bust portraits**, one per hero below. They
are a style test: David will approve the style before the other nine are made, so all three must clearly look like
one set.

## Rules

- **Text prompts only.** Use no reference images of any kind. In particular, never look at, copy, trace or imitate
  anything in `Unity/Assets/Synty/`, `Unity/Assets/Polygon*/` or `Unity/Assets/LocalArt/`. Those are licensed 3D
  models and this public repository must not contain derivatives of them. Don't imitate any existing game's art.
- **One consistent set:** square, head and shoulders, the face at three-quarter view and roughly the same size and
  height in every frame, eyes about 40% from the top. Painted, stylised dark fantasy with clear shapes that still read
  at 96×96 pixels. No line-art or anime look and no photorealism.
- **Lighting:** cool slate-blue ambient shadow, and one restrained warm key light from the front left.
- **Background:** a plain, dark, softly mottled wash of the hero's card colour (given below), darker at the edges. No
  scenery, no frame, no border, no text, no lettering, no logo, no signature, no watermark.
- **Identity:** the hero's accent colour appears in clothing or a clasp, and their mark (described below) appears once,
  small, as a clasp, pin or embroidered emblem, not as a floating symbol.
- Tone: grim and weary but human and dignified. No gore.

## The three heroes

| File | Hero | Description | Accent / card colour | Mark |
| --- | --- | --- | --- | --- |
| `bram.png` | Bram, the Warden (man, protector and shield-bearer) | Broad, weathered man in his forties, short greying beard, square-collared padded gambeson under a dented iron gorget, a steady protective look | amber `#E6B34F` / `#342B1B` | a vertical kite shield |
| `isolde.png` | Lady Isolde, the Widow (noblewoman) | Upright noblewoman in her fifties in a black mourning veil, high lace collar, wearing a broken coronet, composed and cold | verdigris `#6DCECA` / `#183332` | a veil beneath a broken coronet |
| `tam.png` | Tam, the Torchbearer (peasant labourer) | Stocky young labourer, plain undyed tunic, soot on one cheek, cropped hair, lit warmly from one side by an unseen torch | tallow `#D8D17D` / `#32301E` | a torch with a square flame |

## Output

- Save the three images in `Docs/portraits/samples/` as `bram.png`, `isolde.png` and `tam.png`, square, at the size the
  tool produces (1024×1024 is ideal).
- Write `Docs/portraits/samples/prompts.md` with the exact prompt you used for each image.
- Change nothing outside `Docs/portraits/`, and make no git commits.
- In your final reply, list the files you made and anything you couldn't do.

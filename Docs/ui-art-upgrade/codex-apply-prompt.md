Apply the approved UI style guide to Where Darkness Dwells. David approved `Docs/ui-art-upgrade/style-guide.md` ("The Siege Ledger"). Read it in full, along with `Docs/ui-art-upgrade/ui-analysis.md`, `Docs/ui-art-upgrade/samples/README.md` and `AGENTS.md`. The guide's numbers take precedence over the concept images. Don't copy invented text or mechanics from the samples.

What to do:
1. Create the art the guide lists under `Unity/Assets/UI/Art/`: frames, controls, sliders, card parts, bars, map pieces, icons, all twelve hero marks, title art. Don't create painted portraits: the guide removed them. Use the exact paths, dimensions and 9-slice borders in its upgrade table. Use your image generation tool and/or scripted drawing. Icons must be clean white silhouettes on real transparency, and frames must have real alpha, so check the alpha channel of every PNG. Bake no text into any image.
2. Restyle the UI to match: `Unity/Assets/UI/*.uss` and the theme, including palette tokens declared on both `.screen` and `.menus`, the typography sizes, all component states, and the title fade.
3. Add the elements icons and hero marks need, in UXML or in the view code in `Unity/Assets/Scripts/Game/`. These must be small, additive and presentation-only, with `picking-mode` Ignore on decorative elements. Values must come from existing state. Also move the hard-coded line colours in `MapView.cs` to the guide's palette.
4. Fonts: add Cinzel and Source Sans 3 with their licence files if you can download them. If you can't reach the network, use the guide's fallback (Unity's default face at the guide's sizes) and report it. Don't add a package.
5. Write `Unity/Assets/UI/Art/PROVENANCE.md`, saying how each asset group was made.

Rules:
- Overwrite existing images at the same path where possible, keeping their `.meta` files so references survive. Almost everything here is new.
- Every new asset and folder needs a valid `.meta` file with a unique, randomly generated 32-hex GUID. PNGs: TextureImporter set to Sprite type (single), sRGB, alpha is transparency, no mipmaps, bilinear, uncompressed or a sensible default, with sprite borders matching the guide's 9-slice values where the guide gives them (L/B/R/T order in `spriteBorder` is x=left, y=bottom, z=right, w=top). Check that the USS `url()` paths are correct relative to each USS file. Don't set conflicting Sprite and USS slicing; follow the guide.
- Change only UI art, UI styling and the references they need. No gameplay code changes. Don't touch `Unity/Assets/Scripts/Core`, `Unity/Assets/Scripts/Content`, `Unity/Assets/Content`, the tests, scenes, or project settings.
- Keep the live party portraits in `HeroPortraits.cs`, and the fight stage and its animations, working as they are. Leave the audio code alone.
- Keep every existing C#-driven class name and UXML element name. The full list is in section 8 of the guide. Keep the fallbacks: the game must look finished in a fresh clone with no `Unity/Assets/Synty/` or `Unity/Assets/LocalArt/`, and optional `TitleBackdrop`, `FloorTiles` and `CombatStage` must still work and read clearly.
- The repository is public. Never copy, trace, derive from or closely imitate anything in `Unity/Assets/Synty/` or `Unity/Assets/LocalArt/`, and don't modify those folders. Stay within the guide's web size budget.
- Use only USS features the guide allows. No gradients, box-shadow, `:before`/`:after`, or browser CSS. Unity's USS parser logs errors on unsupported properties, so check each property you use.
- Unity isn't available to you, so you can't compile or see the result. Review your C# carefully by reading it, and run `dotnet test tests/WhereDarknessDwells.Tests` if the SDK is available. Don't claim anything was verified in Unity.
- Make no git commits.

Finish by writing, as your final message, a list of everything you changed, anything you couldn't finish, and what needs checking in the Unity Editor.

You are the art director for Where Darkness Dwells, a Unity UI Toolkit game. Produce a UI style guide. Do not change the game yet.

1. Read `Docs/ui-art-upgrade/ui-analysis.md` in full. Read `AGENTS.md`, the "Art direction" section of `GAME_DESIGN.md`, the USS files in `Unity/Assets/UI/`, and `Unity/Assets/UI/*.uxml`.

2. Write `Docs/ui-art-upgrade/style-guide.md`, covering:
   - Art direction: a distinctive concept for this game's UI, drawn from the Keep's history and materials. Say what to avoid.
   - Colour palette: named tokens with exact values, as USS variables, including the six hero colours and the room colours. Fix the similar pairs named in the analysis, with contrast checks for text on each surface.
   - Typography: specific typefaces with their licences and where to get them, a type scale, and a fallback plan if a font can't be downloaded. Card text must be readable at 150x210.
   - Materials and shapes: edges, corners, ornament, texture, and how 9-slicing works for each.
   - A spec for each component: buttons with every state (normal, hover, active, disabled, selected), panels, cards (hero-owned, dual, unplayable, selected, boosted, discounted), hero and enemy panels, health bars, badges and statuses, intents, room tiles and doors for the floor plan, chart rooms, toggles, scrollbars, the journal, the top bar, the title fade, and the end panels.
   - Icons: a full list covering room types, statuses, intents, Resolve, Block, deck and journal, with what each one shows.
   - A per-asset upgrade table: current path, or "new" with the proposed path under `Unity/Assets/UI/Art/`; what the new art should look like; dimensions; 9-slice borders in pixels; and which USS selector uses it. The project has almost no existing art, so most rows are new. The table must cover every screen in the analysis.
   - UI Toolkit implementation notes: use only what USS supports, as described in the analysis. C# adds class names, so they must not be renamed.

3. Add 3 to 5 sample images in `Docs/ui-art-upgrade/samples/` where you can generate them. Good subjects are a mocked-up combat screen, the floor-plan map, a card set, and a button and panel sheet. If you can't generate raster images, write SVG or HTML mockups there and say so in your reply.

Hard rules:
- The repository is public. All proposed art must be original or under a licence that allows redistribution. Never copy, trace, derive from or closely imitate anything in `Unity/Assets/Synty/` or `Unity/Assets/LocalArt/`. You may look at them only to understand the game's overall tone.
- The UI must look finished in a fresh clone with no Synty art, and also read clearly over the 3D fight stage.
- Hero colours are how players tell who owns a card, so they must stay distinct, including on dual cards.
- Keep web download size in mind: favour small, shared, 9-sliced or tiled textures.
- Change nothing outside `Docs/ui-art-upgrade/`, and make no git commits.

End with a short reply listing what you wrote and anything you couldn't do.

Applied The Siege Ledger UI changes without committing.

- Created all 93 specified PNGs: frames, controls, sliders, card parts, bars, map pieces, icons, twelve hero marks and title art. Added editable drawing sources and matching `.meta` files.
- Restyled the three USS files and runtime theme, including shared palette tokens, typography sizes, component states and title fade.
- Added presentation bindings in `Views`, `CombatScreen`, `RunFlow`, `MapView`, `FloorPlanView` and `GameMenus`. Preserved live portraits, stage animations and audio callbacks.
- Added [PROVENANCE.md](/Users/davidpearson/where-darkness-dwells/Unity/Assets/UI/Art/PROVENANCE.md) with asset origins, reproduction instructions and the full acceptance checklist.
- Static checks passed for PNG dimensions/alpha, white silhouettes, slicing consistency, GUID uniqueness, USS paths, UXML names and diff whitespace.

Couldn’t complete font downloads because DNS failed; Unity’s default face uses the approved sizes. Core tests couldn’t run because `dotnet` is unavailable.

Unity Editor checks still needed:

- Import/recompile and inspect Console errors, slices, controls and keyboard focus.
- Check every screen, all twelve heroes, all 66 dual pairs and long card text.
- Test optional-art fallbacks, combat animations, map states and persisted sliders.
- Verify readability at 1080p/720p, atlas behaviour and measured WebGL download/memory budgets.

No Unity compilation or rendering verification is claimed.
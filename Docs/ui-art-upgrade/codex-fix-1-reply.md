1. **Map tags:** Regenerated `door-tag.png` with an opaque, clear centre. Centred icons beside captions, including stairs.
2. **Map top bar:** Reserved widths for counters, seed and Journal. Long health chips keep their text; the bar can wrap when needed.
3. **Combat controls:** Added explicit widths and spacing so Resolve, buttons and single-line piles fit the bottom panel.
4. **Badges/statuses:** Fixed icon padding and alignment. Longer guard and lethal-warning text can wrap.
5. **Toggles:** Disabled input flex growth, constrained boxes to 28×28, and centred the checked mark.
6. **Title:** Generated a 1536px smooth fade, widened the folio for the 60px single-line title, and tightened content spacing.
7. **Settings:** Made its scrim fully opaque, hiding underlying run screens without changing flow.

Changed the three USS files, `Views.cs` presentation helper, generator and generated art. Existing GUIDs and `1px` slice scales were preserved. No commits made.

Static asset/reference checks and `git diff --check` passed. **Not verified in Unity.** Please recheck at both resolutions, especially long names, checked toggles, combat piles with set-aside cards, and the title with/without its backdrop. No requested item was left unaddressed; rendered validation remains.
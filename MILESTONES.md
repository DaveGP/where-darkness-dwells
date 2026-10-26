# Where Darkness Dwells: milestones

**Current milestone: 6.5, presentation (started).** Milestones 1 to 6 were merged on 2026-10-17.

Each milestone is one branch and one PR. A milestone is done only when its acceptance criteria have been
checked, not just when the code is written.

## Milestone 0: audit and plan

**Audit (2026-10-17).** The repository was empty. It had no Unity project, assets, scripts or settings, so
nothing was there to keep or replace. The Synty packs David owns haven't been imported yet, so which packs
are available is still to be checked in Milestone 6.

Done:
- [x] `GAME_DESIGN.md`: the smallest complete game, the combat rules, the map, the first villain and the open
  questions.
- [x] `CLAUDE.md`: the architecture, the rules every change follows, how to test, and the asset licence rule.
- [x] `MILESTONES.md`: this roadmap.
- [x] `.gitignore` for Unity, with Synty folders excluded.

Waiting on David:
- [x] Review the proposed decisions in `GAME_DESIGN.md` (heroes, turn structure, villain). Agreed 2026-10-17.
- [x] Create the Unity project in this repository. It's in `Unity/` (6000.6.5f1, Universal 3D template).
- [x] Check the Editor can be driven from a Claude session on David's Mac: it recompiles with 0 errors and
  lists tests.

## Milestone 1: playable combat prototype

The work is split so the rules can be built and tested before the Unity project exists.

**1a. Combat rules in Core (no Editor needed)**
- Content definitions for heroes, cards and enemies. Seeded `Rng`.
- `CombatState`: draw, hand of 6, activating a hero, Resolve, card eligibility with reasons, playing cards,
  statuses, enemy intents, the enemy phase, downed heroes, victory and defeat.
- Starter content written in code: 3 heroes, 12 hero cards, 3 dual cards, 2 enemy types (a melee brute and
  a caster that applies Weak).
- A .NET test project and a CI workflow.

Status: done. The code is in `Assets/Scripts/Core` and the tests are in `Assets/Tests/EditMode`.

**1b. Combat screen in Unity**
- `ScriptableObject` assets for that content, plus `ContentDatabase` with validation.
- A UI Toolkit combat screen:
  - hero panels showing health, Block, statuses and an activation button,
  - enemies with their intent and target,
  - the hand of cards, coloured by owner, with unplayable cards dimmed and the reason shown on hover,
  - a Resolve counter and an "End activation" button,
  - a combat log, and win and lose panels.
- Placeholder art only.

Status: written. The pieces:
- `Assets/Content`: one asset per hero, card and enemy, and `ContentDatabase.asset`, which lists them. They were
  generated from `StarterContent`, and an Editor test (`Assets/Tests/Content`) fails if the two ever differ.
- `Assets/Scripts/Game/CombatScreen.cs` and `Assets/UI`: the screen. It calls only the `CombatState` commands and
  redraws from the state after each one. Combat log sentences come from `CombatLogText` in Core, which is tested.
- **Where Darkness Dwells > Build Combat Scene** creates `Assets/Scenes/Combat.unity` and its panel settings, so
  the scene can be rebuilt the same way on any machine.
- The test fight is set on the Combat Screen object: the party, the deck (one entry per card copy), the enemies,
  and a seed. A seed of 0 picks a new one each fight; the seed is shown at the top right so a fight can be
  replayed.

Checked in David's Editor (2026-10-17): it compiles with no warnings, 241 EditMode tests pass (238 rules tests
and 3 content asset tests), the scene builds, and a fight starts and plays through activation, a targeted
card, a refused card and the enemy phase with no Console errors. Still to check by David: playing a fight to
victory or defeat, and how the screen looks. David played it and approved the look on 2026-10-17. Done.

**Acceptance criteria**
- David can press Play, complete one encounter and win or lose it.
- Only cards owned by the active hero are playable, and dual cards work with either owner. Every rejected
  play shows its reason.
- Tests cover: eligibility, Resolve spending, damage order (Weak, then Exposed, then Block), Guarded
  redirection, Bleed ticking, a downed hero's cards being set aside, victory and defeat, and identical results
  from the same seed and commands.

## Milestone 2: a combat system worth building on

Status: done, merged on 2026-10-17 (PR #5).
- `tools/Balance` plays the first fight with two scripted players. Over 200 seeds, a careful player (one that
  looks ahead at the enemy phase) wins every fight in about 6 rounds with 73% of the party's health left. A
  careless one (random choices) wins 2 in 3, takes about 12 rounds and ends with 19%. Choices matter, so the
  "too easy" reading from Milestone 1's simpler bot was wrong. David chose to keep this difficulty for the first
  fight; harder fights come with Milestone 3's encounters.
- Open questions 1 and 2 are answered in `GAME_DESIGN.md` from the same tool. Question 3 (how downed heroes
  return) only matters across a run, so it moves to Milestone 3.
- Card use is even: in careful play every card is played about once a fight (Stand Guard least, at 0.5) and
  each hero gets 30 to 38% of activations, so no card or hero needed retuning yet.
- Feedback, checked in David's Editor (284 EditMode tests pass):
  - enemy intents show what they would really do after Block, Weak, Exposed and Guard
    (`CombatState.PreviewEnemyPhase`);
  - heroes show their incoming damage and warn when it would bring them down;
  - damage, Block and new statuses float up from the panels, with each enemy's action on its own beat;
  - hovering a hero's Activate button lifts their cards, and cards that are stronger right now (an Exposed
    target for Exploit, Hex for Unravel) are outlined in green.

- Tune the hero identities, card effects, cross-hero synergies, costs and enemy intents.
- Answer open questions 1 to 3 in `GAME_DESIGN.md` through playtests.
- Add combat feedback: damage numbers, intent previews, and highlighting the cards a choice enables.
- **Acceptance criteria:** an encounter takes real decisions, and the rules are covered well enough that new
  content won't break them.

## Milestone 3: a complete run

- Party screen, seeded map generation and map screen, room flow, card rewards, Sanctuary, a placeholder boss,
  and a run-end screen.
- **Acceptance criteria:** start a run, choose a route, clear rooms, take rewards, and reach the boss or
  defeat.

Status: done, merged on 2026-10-17 (PR #6). David played a run to victory over Vael in the Editor.

**3a. Run rules in Core**
- `Map/MapGenerator` builds the floor from the run's "map" stream and `Run/RunState` carries the party's health
  and deck from room to room. Every command is validated, and the same seed and choices give the same run.
- The content a run needs is in `StarterContent` and `Assets/Content`: 15 reward cards (4 per hero and 3 dual),
  3 more enemies, 2 elites, a placeholder Vael, and the encounter table in `Expedition.asset`. They all use the
  effects the starting cards already have.
- Defaults chosen where the design left a gap:
  - Investigation and Event rooms appear on the map but are empty until Milestones 4 and 5.
  - Elites give a card choice; their Keepsake comes with Milestone 5.
  - The first layer is always an ordinary fight.
- `tools/Balance` now plays whole runs (`-- run 300`). With the tuned content, the careful player wins 63% of runs
  and reaches Vael in 94%. The careless player wins none, and most of its runs end on layers 1 to 4. Open question
  3 was answered with the same tool: David chose to bring fallen heroes back with a quarter of their health.

**3b. Run screens in Unity**
- **Where Darkness Dwells > Build Run Scene** creates `Assets/Scenes/Run.unity` and makes it the first scene in
  the build. `RunFlow` shows the party, map, reward, Sanctuary, empty rooms and the run's end, and hands each fight
  to the combat screen, which hands it back when it's over. `MapView` draws the map from the run's state.
- The Combat scene still plays one test fight on its own.

Follow-up from David's playtest: the map's starting point wasn't clear. Mark the entrance and make the rooms you
can enter stand out more (Milestone 6 at the latest).

## Milestone 4: the mystery

- Clue definitions and the clue log, Investigation rooms, the journal, Vael as the boss, clue effects on the
  boss fight and map, and the two endings.
- **Acceptance criteria:** clues give useful information and measurably change the boss fight or the ending.

Status: done, merged on 2026-10-17 (PR #7). All 318 EditMode tests pass in David's Editor. He played through
two Investigations without errors. The boss fight with clues and the endings still need a playthrough.

**4a. The mystery in Core**
- `Mystery/MysteryDefinitions.cs` describes a villain as data: its boss, its clues, each clue's effects, its
  secret room and its two endings. `RunState` and `Combat/CombatModifiers.cs` interpret the effect kinds;
  nothing checks which villain it is. `StarterContent.Vael()` is the first villain, as `GAME_DESIGN.md` lists.
- Investigation rooms each hold a different clue that fits their layer. `RunState.TrySearch(heroId)` costs the
  chosen hero health, and `TryUseSecret()` handles the Cistern.
- The boss fight gets Vael's display name, vague intents, bonus damage, starting health and the "Speak his
  name" cancel from the clues found (`RunState.BossModifiers`).
- Changed while building it: every room on layer 4 leads down to the Cistern, not just one. With one entrance,
  the door was found in 28% of runs but the Cistern only reached in 3%.

**4b. Tuning**
- `tools/Balance -- run` now plays the careful player twice, ignoring clues and seeking them, and reports boss
  wins by the number of clues found. `-- boss` measures each clue on its own in the boss fight.
- The careful player now clears small, dangerous enemies first. Before, it poured damage into Vael and made
  every clue that weakened him look harmful.
- Vael: 140 health, Blood Tithe heals 24 (150 health from Milestone 5). Blood Thralls: 10 health, Claw 4, Feed 6.
- Results over 300 runs: ignoring clues, the careful player beats Vael in 34% of the runs that reach him.
  Seeking clues, it beats him in 24% with one clue, 49% with two, 66% with three and 75% with four. In the boss
  fight alone, the Confession and the Silvered Bells matter most, the Cistern next, and His True Name least.
- The tool can't measure the Ledger: its player sees the exact intents either way.

**4c. Screens in Unity**
- Investigation: the scene, one button per hero to search and pay, and Leave. The clue's journal text follows.
- The Cistern appears along the bottom of the map once its door is found, and has its own room screen.
- A Journal button in the top bar lists every clue, found or not. An Elite's clue shows on its reward screen.
- In the boss fight, vague intents show only their line, with no numbers, target or outcome, and the heroes'
  incoming damage leaves them out. "Speak his name" is a button on Vael's panel.
- The end screen shows the normal or the true ending, and the clues found.

## Milestone 5: events and replayability

- About 8 events (some changed by clues), Keepsakes, a 4th hero and party choice, more cards and enemies.
- **Acceptance criteria:** two runs with different parties and routes feel noticeably different.

Status: done, merged as PR #8 (with the card cost contrast in PR #9). The plan is in `GAME_DESIGN.md` under
"Events, Keepsakes and the party".

- **5a.** Keepsakes and events in Core, with tests, and the clue sources moved back to their events. Done.
- **5b.** Three new heroes (David chose all three) and party choice of 3 from 6, 12 dual cards, 12 reward
  cards and two enemies. Then tune each party of three with the balance tool. Done; David chose Bloodrage.
- **5c.** Event and Keepsake screens, and the party screen with a choice of three from six. Done.

**5c. Screens in Unity**
- Party: all six heroes in two rows, each with their four starting cards. Click to choose three; Enter the Keep
  stays disabled until three are chosen.
- Event: the scene and one button per choice open to the party. A choice that needs a clue names it. A choice
  that falls on one hero asks which, with what it costs. The result shows before the map or the fight, or on the
  reward or card-removal screen that follows.
- Keepsakes: a count in the top bar, an entry each in the Journal, and an Elite's Keepsake on its reward screen.
  A card made cheaper by a Keepsake shows its cost in green.
- Assets: `KeepsakeAsset`, `EventAsset`, and the expedition's `heroes`, `keepsakes` and `events` lists (the
  `party` field was renamed, keeping its data). The content test compares them with `StarterContent`.

**5b. Tuning**
- `tools/Balance -- parties` plays every party of three and reports wins per party and per hero, and any
  stalemate (a fight still going after 100 rounds).
- Corvin's healing was too strong and Hollis dealt too little damage. After tuning (Mend heals 4, Hollis has
  32 health and more damage on his bells), parties with each hero win 45 to 57% of the time for the careful
  player seeking clues, over 100 runs per party.
- Vael: 150 health and Bloodrage (Fury 3 every sixth step, chosen by David). The scripted players now value Events,
  and Events are 22 in 100 rooms (Combat 42, Investigation 18, Sanctuary 8), so a run sees about 1.4.
- Results over 200 runs with the first party: 38% wins ignoring clues and 47.5% seeking them (31% with one
  clue, 56% with two, 64% with three, 86% with four). The Thrall's Confession is found in 15% of runs.
- One stalemate remains in 2,000 runs, and it is the scripted player's fault: it kept blocking instead of
  finishing two enemies on 1 health.

## Milestone 6: menus, saving and the map

David split the old Milestone 6 in two (2026-10-17). This one comes first.

- Title, pause and settings menus, and saving between rooms. How saving works is in `GAME_DESIGN.md` under
  "Saving".
- The map marks the starting point and makes the rooms you can enter stand out, with an entrance marker and a
  glow (David's feedback from the Milestone 3 playtest).
- **Acceptance criteria:** quit in the middle of a run, start the game again, choose Continue and carry on from
  the same map with the same party, deck, Keepsakes and clues.

Status: done, merged as PR #10. Discarding a card (once per activation, chosen by David) followed in its own PR.

- **Core:** `RunState.TrySnapshot` and `RunState.TryRestore` with a plain `RunSnapshot`, and tests that a restored
  run plays on exactly as the original did.
- **Game:** `SaveFile` (JSON in the player's data folder), `GameSettings` (PlayerPrefs), and `GameMenus` with
  `Menus.uxml`, a third UI document drawn over the run and combat screens. Build Run Scene adds it, so the Run
  scene has to be rebuilt once.
- **Map:** an Entrance marker left of the first layer, and a pulsing glow behind every room the party can enter.

## Milestone 6.5: presentation

- Check which Synty packs are imported, then pick one cohesive set.
- Room backdrops, a combat stage, lighting, music and sound (with a volume setting), animation and feedback.
- A short tutorial encounter.

Status: started. The title screen shows a lair backdrop rendered on David's Mac from the Dark Fantasy crypt. The
image (`Assets/LocalArt/Resources/TitleBackdrop.jpg`) and the scene it is rendered from (`Assets/LocalArt/LairBackdrop.unity`)
are licensed art, so they are gitignored. Without them the title keeps its plain background. The Dark Fantasy and Fantasy
Characters materials were converted to URP on David's Mac; re-importing those packs undoes that.

Enemy models: each enemy asset has an optional `model` prefab, so a later scene can show the enemy as a character.
They are from the Dark Fantasy pack: Hollow Sentry (Skeleton_HeavyArmor), Siege Revenant (Skeleton_LightArmor),
Drowned Retainer (Skeleton_Flesh), Vael (DarkLord), Chapel Matron (Priest_Female), Chapel Wretch (Witch_Female),
Bell Ringer (Gravedigger), Blood Thrall (Demon). The Bloodbound Knight is a red, unmasked DarkLord variant in
`Assets/LocalArt/Enemies/`. The new Crypt Demon is Demon_Male and the Gargoyle is Gargoyle_01. They replace the Gaol Hound and
Cistern Leech in fights, which have no models because the pack has no animal rigs. In a fresh
clone the references are missing, so the field reads as empty and a placeholder is used.
Heroes have the same optional `model` (David's picks): Bram and Wren are the Dark Fantasy Hunters (male and female), Corvin
the Plague Doctor, and from Fantasy Characters, Sister Ashe the Witch, Mara the Druid and Hollis the Bard. The party screen shows each hero's model at full length down the left of their card, breathing through the stage's idle
loop, lit warm from the front and cold from behind over a backdrop in the hero's colour (`Game/HeroPortraits.cs`). Without the
model, the card shows the hero's initial there instead.

Six more heroes (David's request, 2026-10-26), one for each unused character model: Silas (Rogue), Isidore (Sorcerer),
Aldous (Wizard), Vesna (Gypsy), Lady Isolde (Queen) and Tam (Peasant). The design is in `GAME_DESIGN.md` under "Six more
heroes". The party screen is now a roster of twelve, with a card panel below it. The new content is 6 heroes and 99 cards: 24
starters, 24 rewards, 15 bespoke duals and 36 Back to Back duals, in `StarterContent` and as assets.
Balance (`tools/Balance -- parties 100`, after tuning): parties with each hero win 45 to 54% of the time, an average of 50%.
Parties of only the first six win 51%, only the new six 44%, and mixed 50%. The weakest party is Aldous, Vesna and
Isolde (25%), three support heroes together. Tuning raised Vesna (13% at first) and Aldous (21%): their first decks dealt
almost no damage. Back to Back went from Block and a draw to 5 damage and 4 Block, which lifted mixed parties from 38%.

Fight stage: when `Assets/LocalArt/Resources/CombatStage.prefab` is there, each fight is shown on a 3D set behind the
combat screen (`Game/CombatStage.cs`). It is the Background-Layout crypt with three hero places on the left of the aisle
and three enemy places on the right, its own camera, and the crypt's ambient light and fog. Every fighter plays one
idle loop (`LocalArt/Combat/CombatIdle.controller`), and fighters who are down are hidden. A fighter without a model
gets a capsule. On the stage, each fighter's panel sits just above its head (projected from the stage camera), or beside
it when there's no room above, and panels are pushed apart so they never overlap. Without the prefab the fight keeps its
flat background and its usual rows. Attack animations come later.

Floor-plan map (David's request, 2026-10-25): the map is now drawn as a floor plan of the Keep, in the style of Betrayal
at House on the Hill, with the chart kept as a debug view (a button on the map, F2, or Settings). How it works is in `GAME_DESIGN.md` under
"The floor plan". The layout is `Core/Map/FloorPlan.cs` and `RunState.FloorPlan`, tested over 1,000 seeds in `FloorPlanTests`.
The view is `Game/FloorPlanView.cs`. The tiles are placeholders: flat stone colours with each room type's border.
Synty room art can come later.

## Milestone 7: release preparation

- Full playthroughs, bug fixing, balance, onboarding, release builds, credits, icon, screenshots and a store
  page.
- Set the company and product name in Player Settings. They are still Unity's defaults, and the save file's
  folder is named after them.
- **Done means** a new player can start the game, learn it, finish a run and reach an ending without help.

Started early (2026-10-18): a web build (**Where Darkness Dwells > Build for Web**) and a workflow that publishes a
release's zip to GitHub Pages. See README. The Quit button is hidden on the web.

## Creating the Unity project

1. In Unity Hub choose **New project**, editor **6000.6.x**, template **Universal 3D**. Create it in any
   temporary folder, because Hub won't create a project inside a folder that already has files.
2. Close Unity. Move the new project's `Assets`, `Packages` and `ProjectSettings` folders into your clone of
   this repository.
3. In Hub choose **Add > Add project from disk** and pick the clone. Open it once, then commit `Assets`,
   `Packages` and `ProjectSettings` (with all `.meta` files) and push.
4. Import the Synty packs whenever you like. `.gitignore` keeps them out of the repository.

## Known limitations and technical debt

- A run is saved only between rooms. Quitting during a fight or an event starts that room again on Continue.
- Cloud sessions have no Unity Editor. Unity-side code is checked by David in the Editor, and Core is checked
  by tests.

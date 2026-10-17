# Where Darkness Dwells: milestones

**Current milestone: 1b, combat screen (in progress).** 1a, the combat rules, is in review. David agreed the
Milestone 0 design on 2026-10-17.

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

Status: written and tested. The code is in `Assets/Scripts/Core` and the tests are in `Assets/Tests/EditMode`.
.NET runs 235 tests, including 200 seeded fights that an autoplay bot plays to the end. Still to check: the
same tests in David's Editor.

Known from the bot: it wins all 500 seeds it plays, in about 7 rounds. The first fight is too easy, which is a
Milestone 2 tuning job.

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

Still to check in the Editor: that it compiles, the scene builds, and a fight can be played to the end.

**Acceptance criteria**
- David can press Play, complete one encounter and win or lose it.
- Only cards owned by the active hero are playable, and dual cards work with either owner. Every rejected
  play shows its reason.
- Tests cover: eligibility, Resolve spending, damage order (Weak, then Exposed, then Block), Guarded
  redirection, Bleed ticking, a downed hero's cards being set aside, victory and defeat, and identical results
  from the same seed and commands.

## Milestone 2: a combat system worth building on

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

## Milestone 4: the mystery

- Clue definitions and the clue log, Investigation rooms, the journal, Vael as the boss, clue effects on the
  boss fight and map, and the two endings.
- **Acceptance criteria:** clues give useful information and measurably change the boss fight or the ending.

## Milestone 5: events and replayability

- About 8 events (some changed by clues), Keepsakes, a 4th hero and party choice, more cards and enemies.
- **Acceptance criteria:** two runs with different parties and routes feel noticeably different.

## Milestone 6: presentation and polish

- Check which Synty packs are imported, then pick one cohesive set.
- Room backdrops, a combat stage, lighting, music and sound, animation and feedback.
- Title, pause and settings menus, and saving between rooms. A short tutorial encounter.

## Milestone 7: release preparation

- Full playthroughs, bug fixing, balance, onboarding, release builds, credits, icon, screenshots and a store
  page.
- **Done means** a new player can start the game, learn it, finish a run and reach an ending without help.

## Creating the Unity project

1. In Unity Hub choose **New project**, editor **6000.6.x**, template **Universal 3D**. Create it in any
   temporary folder, because Hub won't create a project inside a folder that already has files.
2. Close Unity. Move the new project's `Assets`, `Packages` and `ProjectSettings` folders into your clone of
   this repository.
3. In Hub choose **Add > Add project from disk** and pick the clone. Open it once, then commit `Assets`,
   `Packages` and `ProjectSettings` (with all `.meta` files) and push.
4. Import the Synty packs whenever you like. `.gitignore` keeps them out of the repository.

## Known limitations and technical debt

- The combat screen doesn't animate events yet. It writes them to the log and redraws, so the enemy phase
  happens at once. Damage numbers and other feedback are Milestone 2.
- Cloud sessions have no Unity Editor. Unity-side code is checked by David in the Editor, and Core is checked
  by tests.

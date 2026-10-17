# Where Darkness Dwells: milestones

**Current milestone: 0, project audit and plan (waiting on David's review).**

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
- [ ] Review the proposed decisions in `GAME_DESIGN.md` (heroes, turn structure, villain).
- [ ] Create the Unity project in this repository (see "Creating the Unity project" below).

## Milestone 1: playable combat prototype

The work is split so the rules can be built and tested before the Unity project exists.

**1a. Combat rules in Core (no Editor needed)**
- Content definitions for heroes, cards and enemies. Seeded `Rng`.
- `CombatState`: draw, hand of 6, activating a hero, Resolve, card eligibility with reasons, playing cards,
  statuses, enemy intents, the enemy phase, downed heroes, victory and defeat.
- Starter content written in code: 3 heroes, 12 hero cards, 3 dual cards, 2 enemy types (a melee brute and
  a caster that applies Weak).
- A .NET test project and a CI workflow.

**1b. Combat screen in Unity**
- `ScriptableObject` assets for that content, plus `ContentDatabase` with validation.
- A UI Toolkit combat screen:
  - hero panels showing health, Block, statuses and an activation button,
  - enemies with their intent and target,
  - the hand of cards, coloured by owner, with unplayable cards dimmed and the reason shown on hover,
  - a Resolve counter and an "End activation" button,
  - a combat log, and win and lose panels.
- Placeholder art only.

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

- Cloud sessions have no Unity Editor. Unity-side code is checked by David in the Editor, and Core is checked
  by tests.

# Where Darkness Dwells: working in this repository

A Unity dark-fantasy roguelite deckbuilder. The design is in `GAME_DESIGN.md` and the roadmap and current
milestone are in `MILESTONES.md`. Read both before changing anything.

## Project facts

- **Unity 6 (6000.6.x) with URP.** The Built-in pipeline is deprecated, and Synty packs ship URP materials.
- **UI: UI Toolkit** (UXML layouts and USS styles). They are plain text, so they can be reviewed in diffs,
  written without the Editor, and edited by David in UI Builder. Use uGUI or world-space UI only for
  something UI Toolkit can't do, and record the reason in `MILESTONES.md`.
- **Input:** the Input System package.
- **Platforms:** Windows and macOS, landscape 16:9, mouse first.
- **No new packages without a reason** written in the PR description.

## Architecture

The Unity project lives in `Unity/`. Paths below are relative to it, and the .NET test project sits at the
repository root in `tests/`.

The game rules are plain C# with no Unity references, so they can be tested in milliseconds without the
Editor. Unity only shows the state and passes player choices back to the rules.

```
Assets/Scripts/Core      Rules. No UnityEngine (asmdef: noEngineReferences).
  Content/               Immutable definitions: CardDef, HeroDef, EnemyDef, ClueDef, VillainDef, EventDef.
  Combat/                CombatState, card resolution, statuses, damage, enemy intents, combat events.
  Run/                   RunState: party health, deck (card instances), keepsakes, clues found, map position.
  Map/                   Map graph and the seeded generator.
  Mystery/               Clue log and how found clues change the boss and the ending.
  Events/                Narrative event choices and their consequences.
  Rng.cs                 Seeded random numbers with separate streams for map, rewards and combat shuffles.
Assets/Scripts/Content   ScriptableObject authoring types (CardAsset, HeroAsset ...) and ContentDatabase,
                         which converts them to Core definitions and validates ids and references.
Assets/Scripts/Game      Unity layer: game flow (title > party > map > room > end), screens, input, audio, save.
Assets/UI                UXML and USS.
Assets/Content           The ScriptableObject assets: cards, heroes, enemies, clues, villains, events.
Assets/Tests/EditMode    NUnit tests for Core. tests/WhereDarknessDwells.Tests runs the same files in .NET.
```

### Rules every change follows

1. **Definitions never change at runtime.** Run state holds card instances (instance id plus definition id
   plus upgrades) and refers to definitions by id. Editing a card during a run must not edit its asset.
2. **The UI never changes game state.** It calls a command such as `combat.TryPlay(cardId, targetId)`, which
   returns either a rejection reason or a list of `CombatEvent`s. The UI animates those events and then
   redraws from the state. What's on screen always comes from the real state.
3. **All randomness goes through `Rng`,** seeded from the run seed. The same seed plus the same commands
   gives the same result. A test checks this.
4. **Validate every command.** An invalid action returns a reason the UI can show, such as "Wren isn't
   active" or "Needs 2 Resolve". It never throws for something the player can do, and it never half-applies.
5. **Content is data.** Story-specific behaviour (a clue's effect, a boss's modifiers) is described by data
   that Core interprets. Don't write `if (villain == "Vael")` anywhere.
6. **Keep it small.** No manager classes that do everything, no deep inheritance, no abstraction until there
   is a second user for it.
7. **Logging:** Core returns events and doesn't log. The Game layer logs one line per state transition
   (room entered, combat won) and every rejected command or error. Never log per frame.

## Tests and checks

- **Core tests:** `dotnet test tests/WhereDarknessDwells.Tests`. These run in CI on every push and in cloud
  sessions, where there is no Unity Editor.
- **Unity:** Window > General > Test Runner > EditMode runs the same tests inside Unity. On David's Mac, a
  Claude session can drive his open Editor with the Unity CLI (`unity recompile`, `unity test --mode EditMode`)
  through the `com.unity.pipeline` package.
- **Before a PR:** Core tests pass, a review of the diff for state changes made outside Core, and new assets
  committed with their `.meta` files.
- **What a cloud session can't check:** compilation of the Unity layer, scenes, serialization, and how
  anything looks. Say exactly what still needs checking in the Editor, and give David the manual test steps.
  Never claim something builds or works without evidence.

## Working agreement

- **One milestone per branch and PR.** David reviews and merges. Don't push to `main` except the first
  documentation commit.
- **Small, explained changes.** Name the files a change touches and the systems David should understand.
  Where it helps him learn, offer to let him write a key system himself and then review it.
- **Don't add mechanics that `GAME_DESIGN.md` doesn't agree on.** Propose them first.
- **Keep the docs current.** When a decision changes, update `GAME_DESIGN.md` and the milestone status in
  `MILESTONES.md`. Don't repeat the same facts across the files.

## Third-party assets

- **Synty assets are licensed to David and must not be committed.** The repo is public and the licence
  doesn't allow redistribution. `.gitignore` excludes `Unity/Assets/Synty/` and `Unity/Assets/Polygon*/` (with their `.meta` files). Run `git status` after every import. If a pack
  imports somewhere else, add that folder to `.gitignore` before committing.
- Because of that, cloud sessions and fresh clones won't have the Synty art. Scenes and prefabs must still
  load without it, falling back to placeholders, so the game always runs from a clean clone.

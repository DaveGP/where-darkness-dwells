using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// The run screens: party, map, reward, Sanctuary, Investigation, the secret room, unwritten rooms, the journal and
    /// the run's end. It shows a
    /// <see cref="RunState"/> and turns clicks into its Try* commands, then shows whichever screen the run's phase
    /// calls for. Fights are handed to the <see cref="CombatScreen"/>, which hands them back when they are over.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RunFlow : MonoBehaviour
    {
        [SerializeField] ContentDatabase content;
        [SerializeField] CombatScreen combatScreen;
        [SerializeField] VisualTreeAsset heroPanel;
        [SerializeField] VisualTreeAsset cardView;
        [Tooltip("0 picks a new seed for every expedition. Any other number replays the same map, rewards and fights.")]
        [SerializeField] int seed;

        ContentLibrary _library;
        ExpeditionDef _expedition;
        RunState _run;
        // What the party just found, shown once before the map: a title and its text. Null when there is nothing new.
        (string title, string text)? _found;

        VisualElement _root, _partyView, _mapView, _rewardView, _sanctuaryView, _investigationView, _secretView, _foundView, _unwrittenView, _endView;
        VisualElement _partyHealth, _partyHeroes, _map, _rewardCards, _sanctuaryChoices, _reflectPanel, _reflectGrid, _searchers, _journalPanel, _journalEntries;
        Label _depth, _deckCount, _seedLabel, _mapHint, _rewardHint, _rewardClue, _clueCount, _unwrittenTitle, _unwrittenText, _endTitle, _endText, _endSummary;
        Label _investigationText, _searchHelp, _investigationHint, _secretTitle, _secretText, _foundTitle, _foundText;
        Button _rest, _newExpedition, _useSecret, _journal;

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _partyView = _root.Q("party-view");
            _mapView = _root.Q("map-view");
            _rewardView = _root.Q("reward-view");
            _sanctuaryView = _root.Q("sanctuary-view");
            _investigationView = _root.Q("investigation-view");
            _secretView = _root.Q("secret-view");
            _foundView = _root.Q("found-view");
            _unwrittenView = _root.Q("unwritten-view");
            _endView = _root.Q("end-view");
            _partyHealth = _root.Q("party-health");
            _partyHeroes = _root.Q("party-heroes");
            _map = _root.Q("map");
            _rewardCards = _root.Q("reward-cards");
            _sanctuaryChoices = _root.Q("sanctuary-choices");
            _reflectPanel = _root.Q("reflect-panel");
            _reflectGrid = _root.Q("reflect-grid");
            _searchers = _root.Q("searchers");
            _journalPanel = _root.Q("journal-panel");
            _journalEntries = _root.Q("journal-entries");
            _depth = _root.Q<Label>("depth");
            _deckCount = _root.Q<Label>("deck-count");
            _seedLabel = _root.Q<Label>("seed");
            _mapHint = _root.Q<Label>("map-hint");
            _rewardHint = _root.Q<Label>("reward-hint");
            _rewardClue = _root.Q<Label>("reward-clue");
            _clueCount = _root.Q<Label>("clue-count");
            _investigationText = _root.Q<Label>("investigation-text");
            _searchHelp = _root.Q<Label>("search-help");
            _investigationHint = _root.Q<Label>("investigation-hint");
            _secretTitle = _root.Q<Label>("secret-title");
            _secretText = _root.Q<Label>("secret-text");
            _foundTitle = _root.Q<Label>("found-title");
            _foundText = _root.Q<Label>("found-text");
            _unwrittenTitle = _root.Q<Label>("unwritten-title");
            _unwrittenText = _root.Q<Label>("unwritten-text");
            _endTitle = _root.Q<Label>("run-end-title");
            _endText = _root.Q<Label>("run-end-text");
            _endSummary = _root.Q<Label>("run-end-summary");
            _rest = _root.Q<Button>("rest");
            _newExpedition = _root.Q<Button>("new-expedition");
            _useSecret = _root.Q<Button>("use-secret");
            _journal = _root.Q<Button>("journal");

            _root.Q<Button>("enter-keep").clicked += StartRun;
            _root.Q<Button>("skip-reward").clicked += () => Apply(() => _run.TryTakeReward(null), "Skipped the reward.");
            _rest.clicked += () => Apply(() => _run.TryRest(), "Rested.");
            _root.Q<Button>("reflect").clicked += () => ShowReflect(true);
            _root.Q<Button>("reflect-back").clicked += () => ShowReflect(false);
            _root.Q<Button>("leave-room").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the room.");
            _root.Q<Button>("leave-investigation").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the Investigation without searching.");
            _root.Q<Button>("leave-secret").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the secret room.");
            _useSecret.clicked += UseSecret;
            _root.Q<Button>("found-continue").clicked += () =>
            {
                _found = null;
                ShowPhase();
            };
            _journal.clicked += () => ShowJournal(true);
            _root.Q<Button>("journal-close").clicked += () => ShowJournal(false);
            _newExpedition.clicked += ShowParty;
            // The map spreads its rooms over its measured size, so draw it again whenever that changes.
            _map.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (e.oldRect.size != e.newRect.size && _run?.Phase == RunPhase.ChooseRoom) ShowMap();
            });

            var problems = new List<string>();
            if (content == null) problems.Add("The run screen has no content database.");
            else _library = content.Build(out problems);
            if (combatScreen == null) problems.Add("The run screen has no combat screen.");
            if (heroPanel == null || cardView == null) problems.Add("The run screen is missing a layout.");
            if (problems.Count > 0)
            {
                ShowProblems(problems);
                return;
            }
            _expedition = content.Expedition.ToDef();
            ShowParty();
        }

        // ---------- starting ----------

        void ShowParty()
        {
            _run = null;
            _partyHeroes.Clear();
            foreach (string id in _expedition.Party)
            {
                HeroDef hero = _library.Hero(id);
                VisualElement view = Views.Copy(heroPanel, "hero");
                view.AddToClassList("party-hero");
                view.AddToClassList(Views.OwnerClass(hero.Colour));
                view.Q<Label>("name").text = hero.Name;
                view.Q<Label>("title").text = hero.Title;
                view.Q<Label>("health").text = $"{hero.MaxHealth} / {hero.MaxHealth}";
                foreach (string hidden in new[] { "block", "guard", "incoming", "activate", "state" }) view.Q(hidden).AddToClassList("hidden");
                _partyHeroes.Add(view);
            }
            DrawTopBar();
            Show(_partyView);
        }

        void StartRun()
        {
            _found = null;
            // A short seed, so it can be typed back into the Seed field to replay the expedition.
            ulong runSeed = seed != 0 ? (ulong)seed : (ulong)(Environment.TickCount & 0xFFFFF) + 1;
            _run = new RunState(_library, _expedition, runSeed);
            Debug.Log($"[Run] Expedition started: seed {runSeed}, {_run.Map.Rooms.Count} rooms.");
            ShowPhase();
        }

        void ShowProblems(IEnumerable<string> problems)
        {
            foreach (string p in problems) Debug.LogError($"[Run] {p}");
            _endTitle.text = "The game can't start";
            _endText.text = string.Join("\n", problems);
            _endSummary.text = "";
            _newExpedition.AddToClassList("hidden");
            Show(_endView);
        }

        // ---------- commands ----------

        /// <summary>Runs a command, logs it or the reason it was refused, and shows the screen for the run's new phase.</summary>
        void Apply(Func<RunResult> command, string done)
        {
            if (_run == null) return;
            RunResult result = command();
            if (!result.Ok)
            {
                Debug.Log($"[Run] Refused: {result.Reason}");
                _mapHint.text = result.Reason;
                _rewardHint.text = result.Reason;
                _investigationHint.text = result.Reason;
                return;
            }
            Debug.Log($"[Run] {done}");
            ShowPhase();
        }

        void EnterRoom(MapRoom room) => Apply(() => _run.TryEnterRoom(room.Id), $"Entered {MapView.Label(room.Kind)} on layer {room.Layer + 1}" +
            (room.EncounterId != null ? $" ({room.EncounterId})." : "."));

        void OnCombatOver()
        {
            CombatPhase outcome = _run.Combat.Phase;
            Apply(() => _run.TryFinishCombat(), outcome == CombatPhase.Won ? $"Fight won in {_run.Combat.Round} rounds." : "Fight lost.");
            if (_run.RewardClue != null) Debug.Log($"[Run] Clue found after the fight: {_run.RewardClue}.");
        }

        void Search(string heroId, ClueDef clue) => Apply(() =>
        {
            RunResult result = _run.TrySearch(heroId);
            if (result.Ok) _found = (clue.Name, clue.Journal);
            return result;
        }, $"{heroId} searched and found the clue {clue.Id}.");

        void UseSecret()
        {
            SecretRoomDef secret = _run.Villain.SecretRoom;
            Apply(() =>
            {
                RunResult result = _run.TryUseSecret();
                if (result.Ok) _found = (secret.Name, $"It is done. {BossName()} will start the fight with {secret.BossHealthLossPercent}% less health.");
                return result;
            }, $"Used the secret room: {secret.ActionLabel}.");
        }

        /// <summary>The boss by the name the party knows it by.</summary>
        string BossName() => _run.HasEffect(ClueEffectKind.RevealBoss) ? _library.Enemy(_run.Villain.BossEnemyId).Name : _run.Villain.HiddenName;

        // ---------- screens ----------

        void ShowPhase()
        {
            DrawTopBar();
            if (_run.Phase == RunPhase.ChooseRoom && _found != null)
            {
                ShowFound();
                return;
            }
            switch (_run.Phase)
            {
                case RunPhase.ChooseRoom: ShowMap(); break;
                case RunPhase.Combat:
                    _root.style.display = DisplayStyle.None;
                    combatScreen.Show(_run.Combat, _run.Seed, () =>
                    {
                        _root.style.display = DisplayStyle.Flex;
                        OnCombatOver();
                    });
                    break;
                case RunPhase.Reward: ShowReward(); break;
                case RunPhase.Sanctuary: ShowSanctuary(); break;
                case RunPhase.Investigation: ShowInvestigation(); break;
                case RunPhase.Secret: ShowSecret(); break;
                case RunPhase.Unwritten: ShowUnwritten(); break;
                default: ShowEnd(); break;
            }
        }

        void Show(VisualElement view)
        {
            _root.style.display = DisplayStyle.Flex;
            foreach (VisualElement v in new[] { _partyView, _mapView, _rewardView, _sanctuaryView, _investigationView, _secretView, _foundView, _unwrittenView, _endView })
                v.EnableInClassList("hidden", v != view);
            ShowJournal(false);
        }

        void ShowMap()
        {
            _mapHint.text = _run.CurrentRoom == null ? "Pick one of the rooms on the left to begin." : "Pick a lit room.";
            MapView.Draw(_map, _run, EnterRoom, room =>
                _mapHint.text = room == null ? "" : $"{MapView.Label(_run, room)}. {MapView.Help(room.Kind)}");
            _clueCount.text = _run.Villain == null ? "" : $"Clues found: {_run.CluesFound.Count} of {_run.Villain.Clues.Count}. See the Journal.";
            Show(_mapView);
        }

        void ShowReward()
        {
            _rewardHint.text = "";
            ClueDef clue = _run.RewardClue == null ? null : _run.Villain.Clue(_run.RewardClue);
            _rewardClue.text = clue == null ? "" : $"Among the fallen you find a clue: {clue.Name}. {clue.Journal}";
            _rewardClue.EnableInClassList("hidden", clue == null);
            _rewardCards.Clear();
            foreach (string id in _run.RewardOffer)
            {
                CardDef def = _library.Card(id);
                VisualElement view = Views.Card(cardView, def, _library);
                view.RegisterCallback<ClickEvent>(_ => Apply(() => _run.TryTakeReward(id), $"Took {id}."));
                view.RegisterCallback<PointerEnterEvent>(_ => _rewardHint.text = $"Add {def.Name} to the deck.");
                view.RegisterCallback<PointerLeaveEvent>(_ => _rewardHint.text = "");
                _rewardCards.Add(view);
            }
            Show(_rewardView);
        }

        void ShowSanctuary()
        {
            IEnumerable<string> heals = _run.Party.Select(h =>
                $"{_library.Hero(h).Name} +{Math.Min(_run.RestHealAmount(h), _run.MaxHealth(h) - _run.Health(h))}");
            _rest.text = "Rest: " + string.Join(", ", heals);
            ShowReflect(false);
            Show(_sanctuaryView);
        }

        void ShowReflect(bool reflecting)
        {
            _sanctuaryChoices.EnableInClassList("hidden", reflecting);
            _reflectPanel.EnableInClassList("hidden", !reflecting);
            _reflectGrid.Clear();
            if (!reflecting) return;
            foreach (CardInstance card in _run.Deck)
            {
                CardInstance c = card;
                VisualElement view = Views.Card(cardView, _run.CardOf(c), _library);
                view.RegisterCallback<ClickEvent>(_ => Apply(() => _run.TryReflect(c.Id), $"Reflected: removed {c}."));
                _reflectGrid.Add(view);
            }
        }

        void ShowInvestigation()
        {
            ClueDef clue = _run.ClueHere;
            _investigationHint.text = "";
            _searchers.Clear();
            if (clue == null)
            {
                _investigationText.text = "Dust, a broken lectern, and the smell of old candle wax. Someone searched this room long before you. There is nothing left to find.";
                _searchHelp.text = "";
            }
            else
            {
                _investigationText.text = clue.Scene;
                _searchHelp.text = $"{clue.SearchLabel}: one hero pays {clue.SearchCost} health to find what is here. Who goes?";
                foreach (string id in _run.Party)
                {
                    string hero = id;
                    var button = new Button(() => Search(hero, clue))
                    {
                        text = $"{_library.Hero(hero).Name} ({_run.Health(hero)} health)\n{clue.SearchLabel}: -{clue.SearchCost}",
                    };
                    button.AddToClassList("choice-button");
                    button.AddToClassList("searcher");
                    button.EnableInClassList("is-unavailable", _run.Health(hero) <= clue.SearchCost);
                    _searchers.Add(button);
                }
            }
            Show(_investigationView);
        }

        void ShowSecret()
        {
            SecretRoomDef secret = _run.Villain.SecretRoom;
            _secretTitle.text = secret.Name;
            _secretText.text = secret.Scene;
            _useSecret.text = $"{secret.ActionLabel}: each hero loses {secret.HealthCostPerHero} health";
            Show(_secretView);
        }

        void ShowFound()
        {
            _foundTitle.text = _found.Value.title;
            _foundText.text = _found.Value.text;
            Show(_foundView);
        }

        /// <summary>The journal: every clue about the villain, found ones with their text and the rest as blanks.</summary>
        void ShowJournal(bool open)
        {
            _journalPanel.EnableInClassList("hidden", !open);
            _journalEntries.Clear();
            if (!open || _run?.Villain == null) return;
            foreach (ClueDef clue in _run.Villain.Clues)
            {
                bool found = _run.CluesFound.Contains(clue.Id);
                var entry = new VisualElement();
                entry.AddToClassList("journal-entry");
                entry.EnableInClassList("is-unknown", !found);
                var name = new Label(found ? clue.Name : "Not found yet");
                name.AddToClassList("journal-entry-name");
                entry.Add(name);
                if (found)
                {
                    var text = new Label(clue.Journal);
                    text.AddToClassList("journal-entry-text");
                    entry.Add(text);
                }
                _journalEntries.Add(entry);
            }
            if (_run.SecretUsed)
            {
                var entry = new VisualElement();
                entry.AddToClassList("journal-entry");
                var name = new Label(_run.Villain.SecretRoom.Name);
                name.AddToClassList("journal-entry-name");
                entry.Add(name);
                var text = new Label($"{_run.Villain.SecretRoom.ActionLabel}: done. {BossName()} starts the fight with {_run.Villain.SecretRoom.BossHealthLossPercent}% less health.");
                text.AddToClassList("journal-entry-text");
                entry.Add(text);
                _journalEntries.Add(entry);
            }
        }

        void ShowUnwritten()
        {
            _unwrittenTitle.text = "Event";
            _unwrittenText.text = "Someone was here not long ago. There is no one to meet yet: events arrive in Milestone 5.";
            Show(_unwrittenView);
        }

        void ShowEnd()
        {
            bool won = _run.Phase == RunPhase.Won;
            Debug.Log($"[Run] Expedition {(won ? "won" : "lost")}: seed {_run.Seed}, {_run.Route.Count} rooms, {_run.FightsWon} fights won, " +
                $"{_run.CluesFound.Count} clues{(_run.TrueEnding ? ", true ending" : "")}.");
            _endTitle.text = !won ? "Defeat" : _run.TrueEnding ? "The True Ending" : "Victory";
            _endText.text = won
                ? _run.EndingText ?? "The thing at the heart of the Keep is still. For now, the Black Keep is quiet."
                : $"The darkness takes the party on layer {_run.CurrentRoom.Layer + 1}.";
            _endSummary.text = $"Rooms entered {_run.Route.Count}   Fights won {_run.FightsWon}   " +
                (_run.Villain != null ? $"Clues {_run.CluesFound.Count} of {_run.Villain.Clues.Count}   " : "") +
                $"Deck {_run.Deck.Count} cards   Seed {_run.Seed}";
            _newExpedition.RemoveFromClassList("hidden");
            Show(_endView);
        }

        void DrawTopBar()
        {
            _partyHealth.Clear();
            _depth.text = "";
            _deckCount.text = "";
            _seedLabel.text = "";
            _journal.EnableInClassList("hidden", _run?.Villain == null);
            if (_run == null) return;

            MapRoom room = _run.CurrentRoom;
            _depth.text = room == null ? "Outside the Keep" : room.Kind == RoomKind.Boss ? "The Lair"
                : room.Kind == RoomKind.Secret ? $"{_run.Villain.SecretRoom.Name}, below layer {room.Layer + 1}"
                : $"Layer {room.Layer + 1} of {_run.Map.LayerCount}";
            if (_run.Villain != null) _journal.text = $"Journal {_run.CluesFound.Count}/{_run.Villain.Clues.Count}";
            foreach (string id in _run.Party)
            {
                HeroDef hero = _library.Hero(id);
                var chip = new VisualElement();
                chip.AddToClassList("health-chip");
                var swatch = new VisualElement();
                swatch.AddToClassList("health-chip-swatch");
                swatch.AddToClassList("swatch");
                swatch.AddToClassList(Views.OwnerClass(hero.Colour));
                chip.Add(swatch);
                var text = new Label($"{hero.Name} {_run.Health(id)} / {_run.MaxHealth(id)}");
                text.AddToClassList("health-chip-text");
                chip.Add(text);
                _partyHealth.Add(chip);
            }
            _deckCount.text = $"Deck {_run.Deck.Count}";
            _seedLabel.text = $"Seed {_run.Seed}";
        }
    }
}

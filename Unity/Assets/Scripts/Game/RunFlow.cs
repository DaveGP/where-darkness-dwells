using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// The run screens: party, map, reward, Sanctuary, Investigation, the secret room, events, card removal, unwritten
    /// rooms, the journal and the run's end. It shows a
    /// <see cref="RunState"/> and turns clicks into its Try* commands, then shows whichever screen the run's phase
    /// calls for. Fights are handed to the <see cref="CombatScreen"/>, which hands them back when they are over.
    /// The game opens on the <see cref="GameMenus"/> title. The run is saved each time the party is back on the map
    /// and just before it enters a room, and the save is deleted when the run ends.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class RunFlow : MonoBehaviour
    {
        [SerializeField] ContentDatabase content;
        [SerializeField] CombatScreen combatScreen;
        [SerializeField] GameMenus menus;
        [SerializeField] VisualTreeAsset heroPanel;
        [SerializeField] VisualTreeAsset cardView;
        [Tooltip("0 picks a new seed for every expedition. Any other number replays the same map, rewards and fights.")]
        [SerializeField] int seed;

        ContentLibrary _library;
        ExpeditionDef _expedition;
        RunState _run;
        // What the party just found, shown once before the map: a title and its text. Null when there is nothing new.
        (string title, string text)? _found;
        // The heroes ticked on the party screen, in the order they were picked.
        readonly List<string> _chosen = new List<string>();
        // The event choice waiting for the player to say which hero it falls on; -1 when none.
        int _pendingChoice = -1;
        // Set once OnEnable found everything it needs.
        bool _ready;

        VisualElement _root, _partyView, _mapView, _rewardView, _sanctuaryView, _investigationView, _secretView, _eventView, _removeView, _foundView, _unwrittenView, _endView;
        VisualElement _partyHealth, _partyHeroes, _map, _rewardCards, _sanctuaryChoices, _reflectPanel, _reflectGrid, _searchers, _journalPanel, _journalEntries;
        VisualElement _eventChoices, _eventHeroes, _removeGrid;
        Label _depth, _deckCount, _keepsakeCount, _seedLabel, _mapHint, _mapLegend, _rewardHint, _rewardClue, _clueCount, _unwrittenTitle, _unwrittenText, _endTitle, _endText, _endSummary;
        Label _investigationText, _searchHelp, _investigationHint, _secretTitle, _secretText, _foundTitle, _foundText;
        Label _partyHint, _eventTitle, _eventScene, _eventHeroHelp, _eventHint, _removeText, _removeHint;
        Button _rest, _newExpedition, _useSecret, _journal, _enterKeep, _foundContinue, _mapStyle, _wholePlan;
        // Debug: draw the whole floor plan as if explored. Only for this session; it changes nothing in the run.
        bool _showWholePlan;

        void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _partyView = _root.Q("party-view");
            _mapView = _root.Q("map-view");
            _rewardView = _root.Q("reward-view");
            _sanctuaryView = _root.Q("sanctuary-view");
            _investigationView = _root.Q("investigation-view");
            _secretView = _root.Q("secret-view");
            _eventView = _root.Q("event-view");
            _removeView = _root.Q("remove-view");
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
            _eventChoices = _root.Q("event-choices");
            _eventHeroes = _root.Q("event-heroes");
            _removeGrid = _root.Q("remove-grid");
            _keepsakeCount = _root.Q<Label>("keepsake-count");
            _partyHint = _root.Q<Label>("party-hint");
            _eventTitle = _root.Q<Label>("event-title");
            _eventScene = _root.Q<Label>("event-scene");
            _eventHeroHelp = _root.Q<Label>("event-hero-help");
            _eventHint = _root.Q<Label>("event-hint");
            _removeText = _root.Q<Label>("remove-text");
            _removeHint = _root.Q<Label>("remove-hint");
            _depth = _root.Q<Label>("depth");
            _deckCount = _root.Q<Label>("deck-count");
            _seedLabel = _root.Q<Label>("seed");
            _mapHint = _root.Q<Label>("map-hint");
            _mapLegend = _root.Q<Label>("map-legend");
            _mapStyle = _root.Q<Button>("map-style");
            _wholePlan = _root.Q<Button>("whole-plan");
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
            _enterKeep = _root.Q<Button>("enter-keep");
            _foundContinue = _root.Q<Button>("found-continue");

            _enterKeep.clicked += StartRun;
            _root.Q<Button>("skip-reward").clicked += () => Apply(() => _run.TryTakeReward(null), "Skipped the reward.");
            _rest.clicked += () => Apply(() => _run.TryRest(), "Rested.");
            _root.Q<Button>("reflect").clicked += () => ShowReflect(true);
            _root.Q<Button>("reflect-back").clicked += () => ShowReflect(false);
            _root.Q<Button>("leave-room").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the room.");
            _root.Q<Button>("leave-investigation").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the Investigation without searching.");
            _root.Q<Button>("leave-secret").clicked += () => Apply(() => _run.TryLeaveRoom(), "Left the secret room.");
            _useSecret.clicked += UseSecret;
            _foundContinue.clicked += () =>
            {
                _found = null;
                ShowPhase();
            };
            _journal.clicked += () =>
            {
                GameAudio.Play("journal_open");
                ShowJournal(true);
            };
            _mapStyle.clicked += SwitchMapStyle;
            _wholePlan.clicked += SwitchWholePlan;
            _root.Q<Button>("journal-close").clicked += () =>
            {
                GameAudio.Play("journal_close");
                ShowJournal(false);
            };
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
            if (menus == null) problems.Add("The run screen has no menus. Rebuild the Run scene (Where Darkness Dwells > Build Run Scene).");
            if (heroPanel == null || cardView == null) problems.Add("The run screen is missing a layout.");
            if (problems.Count > 0)
            {
                ShowProblems(problems);
                return;
            }
            _expedition = content.Expedition.ToDef();
            menus.ContinueChosen += ContinueRun;
            menus.NewExpeditionChosen += ShowParty;
            menus.QuitToTitleChosen += QuitToTitle;
            menus.EscapeTaken = () => combatScreen.UsesEscape;
            GameSettings.MapStyleChanged += RedrawMap;
            GameAudio.ClickSounds(_root);
            _ready = true;
        }

        // The title is shown from Start, once the menus have run their own OnEnable.
        void Start()
        {
            if (_ready) ShowTitle();
        }

        void OnDisable()
        {
            if (menus == null) return;
            menus.ContinueChosen -= ContinueRun;
            menus.NewExpeditionChosen -= ShowParty;
            menus.QuitToTitleChosen -= QuitToTitle;
            GameSettings.MapStyleChanged -= RedrawMap;
        }

        // F2 swaps the map between the floor plan and the chart, and F3 shows the whole floor plan, for debugging. Both
        // only change how the run is drawn.
        void Update()
        {
            if (Keyboard.current == null || _run?.Phase != RunPhase.ChooseRoom || menus.IsOpen) return;
            if (Keyboard.current.f2Key.wasPressedThisFrame) SwitchMapStyle();
            else if (Keyboard.current.f3Key.wasPressedThisFrame && !GameSettings.ChartMap) SwitchWholePlan();
        }

        void SwitchWholePlan()
        {
            _showWholePlan = !_showWholePlan;
            Debug.Log($"[Run] Whole floor plan {(_showWholePlan ? "shown" : "hidden")}.");
            RedrawMap();
        }

        void SwitchMapStyle()
        {
            GameSettings.ChartMap = !GameSettings.ChartMap;
            Debug.Log($"[Run] Map drawn as {(GameSettings.ChartMap ? "the chart" : "the floor plan")}.");
        }

        void RedrawMap()
        {
            if (_run?.Phase == RunPhase.ChooseRoom) ShowMap();
        }

        // ---------- starting ----------

        void ShowTitle(string note = null)
        {
            _run = null;
            DrawTopBar();
            menus.ShowTitle(SaveFile.Exists, note);
        }

        /// <summary>Carries on the saved run. A save that can't be loaded is deleted, and the title says why.</summary>
        void ContinueRun()
        {
            RunSnapshot save = SaveFile.Read(out string problem);
            RunResult result = save == null ? null : RunState.TryRestore(_library, _expedition, save, out _run);
            if (save == null || !result.Ok)
            {
                string why = problem ?? result.Reason;
                Debug.LogWarning($"[Save] {why} The save was deleted.");
                SaveFile.Delete();
                ShowTitle($"{why} Start a new expedition instead.");
                return;
            }
            _found = null;
            _pendingChoice = -1;
            menus.CanPause = true;
            Debug.Log($"[Run] Expedition continued: seed {_run.Seed}, {_run.Route.Count} rooms in" +
                (save.EnteringRoomId >= 0 ? ", entering the room it was left in." : "."));
            ShowPhase();
        }

        /// <summary>Leaves the run, even in the middle of a room. The save from the map or the room's door is kept.</summary>
        void QuitToTitle()
        {
            if (_run?.Phase == RunPhase.Combat) combatScreen.Abandon();
            Debug.Log("[Run] Quit to the title.");
            _root.style.display = DisplayStyle.Flex;
            ShowTitle();
        }

        void Save()
        {
            RunResult result = _run.TrySnapshot(out RunSnapshot snapshot);
            if (!result.Ok)
            {
                Debug.LogError($"[Save] Couldn't save: {result.Reason}");
                return;
            }
            SaveFile.Write(snapshot);
            Debug.Log($"[Save] Saved on the map, {_run.Route.Count} rooms in.");
        }

        void ShowParty()
        {
            _run = null;
            menus.CanPause = true;
            _chosen.Clear();
            DrawParty();
            DrawTopBar();
            Show(_partyView);
        }

        /// <summary>Every hero to choose from, with the chosen ones marked. Clicking a hero picks them or puts them back.</summary>
        void DrawParty()
        {
            _partyHeroes.Clear();
            foreach (string id in _expedition.Heroes)
            {
                HeroDef hero = _library.Hero(id);
                VisualElement view = Views.Copy(heroPanel, "hero");
                view.AddToClassList("party-hero");
                view.AddToClassList(Views.OwnerClass(hero.Colour));
                view.EnableInClassList("is-chosen", _chosen.Contains(id));
                view.Q<Label>("name").text = hero.Name;
                view.Q<Label>("title").text = hero.Title;
                view.Q<Label>("health").text = $"{hero.MaxHealth} / {hero.MaxHealth}";
                foreach (string hidden in new[] { "block", "guard", "incoming", "activate", "state" }) view.Q(hidden).AddToClassList("hidden");
                // The hero's own starting cards, so the choice says what each one does.
                var cards = new Label(string.Join("\n", _expedition.StartingDeck.Distinct().Select(_library.Card)
                    .Where(c => c.Owners.Count == 1 && c.IsOwnedBy(id)).Select(c => $"{c.Name}: {c.Text}")));
                cards.AddToClassList("party-hero-cards");
                view.Add(cards);
                view.RegisterCallback<ClickEvent>(_ => ToggleHero(id));
                _partyHeroes.Add(view);
            }
            string why = RunState.WhyCantChoose(_expedition, _chosen);
            _enterKeep.SetEnabled(why == null);
            _partyHint.text = why == null ? $"{string.Join(", ", _chosen.Select(h => _library.Hero(h).Name))} go in."
                : $"{why} Chosen: {_chosen.Count} of {RunState.PartySize}.";
        }

        void ToggleHero(string id)
        {
            if (_chosen.Contains(id)) _chosen.Remove(id);
            else if (_chosen.Count < RunState.PartySize) _chosen.Add(id);
            else
            {
                _partyHint.text = $"Three heroes are chosen. Click one of them to leave them behind first.";
                return;
            }
            DrawParty();
        }

        void StartRun()
        {
            string why = RunState.WhyCantChoose(_expedition, _chosen);
            if (why != null)
            {
                Debug.Log($"[Run] Refused: {why}");
                _partyHint.text = why;
                return;
            }
            _found = null;
            _pendingChoice = -1;
            // A short seed, so it can be typed back into the Seed field to replay the expedition.
            ulong runSeed = seed != 0 ? (ulong)seed : (ulong)(Environment.TickCount & 0xFFFFF) + 1;
            // The party in the order the screen lists them, whatever order they were clicked in.
            List<string> party = _expedition.Heroes.Where(_chosen.Contains).ToList();
            _run = new RunState(_library, _expedition, runSeed, party);
            Debug.Log($"[Run] Expedition started: seed {runSeed}, party {string.Join(", ", party)}, {_run.Map.Rooms.Count} rooms.");
            // A new expedition replaces the saved one.
            Save();
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
                _eventHint.text = result.Reason;
                _removeHint.text = result.Reason;
                return;
            }
            Debug.Log($"[Run] {done}");
            if (_run.CanSave) Save();
            else if (_run.IsOver) SaveFile.Delete();
            ShowPhase();
        }

        void EnterRoom(MapRoom room)
        {
            GameAudio.Play("door");
            // Saved at the door, so quitting inside the room brings the party back to the same room, not around it.
            if (_run.TrySnapshot(out RunSnapshot atDoor, room.Id).Ok) SaveFile.Write(atDoor);
            Apply(() => _run.TryEnterRoom(room.Id), $"Entered {MapView.Label(room.Kind)} on layer {room.Layer + 1}" +
                (room.EncounterId != null ? $" ({room.EncounterId})." : "."));
            // Refused, so the party is still outside: save them there again.
            if (_run.CanSave) Save();
        }

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

        void ChooseEvent(int index, string heroId)
        {
            EventDef ev = _run.CurrentEvent;
            EventChoice choice = ev.Choices[index];
            Apply(() =>
            {
                RunResult result = _run.TryChooseEvent(index, heroId);
                if (!result.Ok) return result;
                _pendingChoice = -1;
                // Shown before the map or the fight. A card choice or removal shows it on its own screen.
                if (_run.Phase == RunPhase.ChooseRoom || _run.Phase == RunPhase.Combat) _found = (ev.Title, _run.EventResult);
                return result;
            }, $"Event {ev.Id}: chose \"{choice.Label}\"{(heroId != null ? $" for {heroId}" : "")}.");
        }

        /// <summary>The boss by the name the party knows it by.</summary>
        string BossName() => _run.HasEffect(ClueEffectKind.RevealBoss) ? _library.Enemy(_run.Villain.BossEnemyId).Name : _run.Villain.HiddenName;

        // ---------- screens ----------

        void ShowPhase()
        {
            DrawTopBar();
            if ((_run.Phase == RunPhase.ChooseRoom || _run.Phase == RunPhase.Combat) && _found != null)
            {
                ShowFound();
                return;
            }
            switch (_run.Phase)
            {
                case RunPhase.ChooseRoom: ShowMap(); break;
                case RunPhase.Combat:
                    _root.style.display = DisplayStyle.None;
                    GameAudio.Play(_run.CurrentRoom?.Kind == RoomKind.Boss ? GameAudio.Track.Lair : GameAudio.Track.Battle);
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
                case RunPhase.Event: ShowEvent(); break;
                case RunPhase.RemoveCard: ShowRemoveCard(); break;
                case RunPhase.Unwritten: ShowUnwritten(); break;
                default: ShowEnd(); break;
            }
        }

        void Show(VisualElement view)
        {
            // The map has its own ambience, the party screen and the ending share the title's, and every room has the rooms'.
            GameAudio.Play(view == _mapView ? GameAudio.Track.Map
                : view == _partyView || view == _endView ? GameAudio.Track.Title : GameAudio.Track.Rooms);
            _root.style.display = DisplayStyle.Flex;
            foreach (VisualElement v in new[] { _partyView, _mapView, _rewardView, _sanctuaryView, _investigationView, _secretView, _eventView, _removeView, _foundView, _unwrittenView, _endView })
                v.EnableInClassList("hidden", v != view);
            ShowJournal(false);
        }

        void ShowMap()
        {
            Action<MapRoom> hover = room => _mapHint.text = room == null ? "" : $"{MapView.Label(_run, room)}. {MapView.Help(room.Kind)}";
            if (GameSettings.ChartMap)
            {
                _mapHint.text = _run.CurrentRoom == null ? "Start at the Entrance: pick one of the glowing rooms beside it." : "Pick a glowing room.";
                _mapLegend.text = "Rooms you can enter next glow. Your route so far is marked in gold.";
                MapView.Draw(_map, _run, EnterRoom, hover);
                _mapStyle.text = "Show the floor plan (F2)";
                _wholePlan.AddToClassList("hidden");
            }
            else
            {
                _mapHint.text = FloorPlanView.Hint(_run);
                _mapLegend.text = FloorPlanView.Legend;
                FloorPlanView.Draw(_map, _run, EnterRoom, hover, _showWholePlan);
                _mapStyle.text = "Show the chart (F2)";
                _wholePlan.RemoveFromClassList("hidden");
                _wholePlan.text = _showWholePlan ? "Hide unexplored rooms (F3)" : "Show the whole plan (F3)";
            }
            _clueCount.text = _run.Villain == null ? "" : $"Clues found: {_run.CluesFound.Count} of {_run.Villain.Clues.Count}. See the Journal.";
            Show(_mapView);
        }

        void ShowReward()
        {
            _rewardHint.text = "";
            // What else this reward brought: an event's result, an Elite's clue or Keepsake.
            var found = new List<string>();
            if (_run.Route.Count > 0 && _run.CurrentRoom.Kind == RoomKind.Event && !string.IsNullOrEmpty(_run.EventResult)) found.Add(_run.EventResult);
            ClueDef clue = _run.RewardClue == null ? null : _run.Villain.Clue(_run.RewardClue);
            if (clue != null) found.Add($"Among the fallen you find a clue: {clue.Name}. {clue.Journal}");
            KeepsakeDef keepsake = _run.RewardKeepsake == null ? null : _expedition.Keepsake(_run.RewardKeepsake);
            if (keepsake != null) found.Add($"You take a Keepsake: {keepsake.Name}. {keepsake.Text}");
            _rewardClue.text = string.Join("\n\n", found);
            _rewardClue.EnableInClassList("hidden", found.Count == 0);
            _rewardCards.Clear();
            foreach (string id in _run.RewardOffer)
            {
                CardDef def = _library.Card(id);
                VisualElement view = Views.Card(cardView, def, _library);
                view.RegisterCallback<ClickEvent>(_ =>
                {
                    GameAudio.Play("take_card");
                    Apply(() => _run.TryTakeReward(id), $"Took {id}.");
                });
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

        void ShowEvent()
        {
            EventDef ev = _run.CurrentEvent;
            _eventTitle.text = ev.Title;
            _eventScene.text = ev.Scene;
            _eventHint.text = "";
            _eventChoices.Clear();
            for (int i = 0; i < ev.Choices.Count; i++)
            {
                EventChoice choice = ev.Choices[i];
                if (!_run.IsChoiceShown(choice)) continue;
                int index = i;
                // A choice that needs a clue says so, so the player sees what their clues opened up.
                string because = choice.RequiresClue != null ? $"\n({_run.Villain.Clue(choice.RequiresClue).Name})" : "";
                var button = new Button(() =>
                {
                    if (!choice.NeedsAHero) ChooseEvent(index, null);
                    else
                    {
                        _pendingChoice = index;
                        ShowEvent();
                    }
                }) { text = choice.Label + because };
                button.AddToClassList("choice-button");
                button.EnableInClassList("is-selected", index == _pendingChoice);
                _eventChoices.Add(button);
            }

            // Who the chosen choice falls on: one button per hero, with what it costs them.
            _eventHeroes.Clear();
            EventChoice pending = _pendingChoice >= 0 ? ev.Choices[_pendingChoice] : null;
            _eventHeroHelp.text = pending == null ? ""
                : pending.HeroCost > 0 ? $"{pending.Label}: who pays {pending.HeroCost} health?" : $"{pending.Label}: which hero?";
            if (pending != null)
            {
                foreach (string id in _run.Party)
                {
                    string hero = id;
                    var button = new Button(() => ChooseEvent(_pendingChoice, hero))
                    {
                        text = $"{_library.Hero(hero).Name} ({_run.Health(hero)} / {_run.MaxHealth(hero)} health)" +
                            (pending.HeroCost > 0 ? $"\n-{pending.HeroCost}" : ""),
                    };
                    button.AddToClassList("choice-button");
                    button.AddToClassList("searcher");
                    button.EnableInClassList("is-unavailable", _run.Health(hero) <= pending.HeroCost);
                    _eventHeroes.Add(button);
                }
            }
            Show(_eventView);
        }

        void ShowRemoveCard()
        {
            _removeText.text = string.IsNullOrEmpty(_run.EventResult) ? "Choose a card to remove from the deck." : _run.EventResult;
            _removeHint.text = "";
            _removeGrid.Clear();
            foreach (CardInstance card in _run.Deck)
            {
                CardInstance c = card;
                VisualElement view = Views.Card(cardView, _run.CardOf(c), _library);
                view.RegisterCallback<ClickEvent>(_ => Apply(() => _run.TryReflect(c.Id), $"Removed {c} from the deck."));
                _removeGrid.Add(view);
            }
            Show(_removeView);
        }

        void ShowFound()
        {
            _foundTitle.text = _found.Value.title;
            _foundText.text = _found.Value.text;
            _foundContinue.text = _run.Phase == RunPhase.Combat ? "Fight" : "Return to the map";
            Show(_foundView);
        }

        /// <summary>The journal: every clue about the villain, found ones with their text and the rest as blanks, then the Keepsakes.</summary>
        void ShowJournal(bool open)
        {
            _journalPanel.EnableInClassList("hidden", !open);
            _journalEntries.Clear();
            if (!open || _run == null) return;
            if (_run.Villain != null) AddClues();
            AddKeepsakes();
        }

        void AddKeepsakes()
        {
            foreach (string id in _run.Keepsakes)
            {
                KeepsakeDef keepsake = _expedition.Keepsake(id);
                var entry = new VisualElement();
                entry.AddToClassList("journal-entry");
                entry.AddToClassList("is-keepsake");
                var name = new Label($"Keepsake: {keepsake.Name}");
                name.AddToClassList("journal-entry-name");
                entry.Add(name);
                var text = new Label(keepsake.Text);
                text.AddToClassList("journal-entry-text");
                entry.Add(text);
                _journalEntries.Add(entry);
            }
        }

        void AddClues()
        {
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
            _unwrittenTitle.text = "An empty room";
            _unwrittenText.text = "Someone was here not long ago: a cold candle, a dropped glove. Whoever it was has gone.";
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
                $"Keepsakes {_run.Keepsakes.Count}   Deck {_run.Deck.Count} cards   Seed {_run.Seed}";
            _newExpedition.RemoveFromClassList("hidden");
            Show(_endView);
        }

        void DrawTopBar()
        {
            _partyHealth.Clear();
            _depth.text = "";
            _deckCount.text = "";
            _keepsakeCount.text = "";
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
            _keepsakeCount.text = _run.Keepsakes.Count == 0 ? "" : $"Keepsakes {_run.Keepsakes.Count}";
            _seedLabel.text = $"Seed {_run.Seed}";
        }
    }
}

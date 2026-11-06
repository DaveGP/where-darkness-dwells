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
    /// The combat screen. It shows a <see cref="CombatState"/> and turns clicks into its Try* commands. It never
    /// changes the fight itself: after every command it adds the events to the log and redraws from the state.
    /// The only state it keeps is the card the player has picked and is choosing a target for.
    /// In a run, <see cref="RunFlow"/> hands it each fight with <see cref="Show"/>. Played on its own in the Combat
    /// scene, it starts the test fight below.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatScreen : MonoBehaviour
    {
        [SerializeField] ContentDatabase content;

        [Header("Test fight, when the Combat scene is played on its own")]
        [Tooltip("Off when a run hands this screen its fights.")]
        [SerializeField] bool startTestFight = true;
        [SerializeField] HeroAsset[] party = new HeroAsset[0];
        [Tooltip("One entry per copy of a card.")]
        [SerializeField] CardAsset[] deck = new CardAsset[0];
        [SerializeField] EnemyAsset[] enemies = new EnemyAsset[0];
        [Tooltip("0 picks a new seed for every fight. Any other number replays the same fight.")]
        [SerializeField] int seed;

        [Header("Layouts")]
        [SerializeField] VisualTreeAsset heroPanel;
        [SerializeField] VisualTreeAsset enemyPanel;
        [SerializeField] VisualTreeAsset cardView;

        static readonly Dictionary<StatusKind, string> StatusHelp = new Dictionary<StatusKind, string>
        {
            { StatusKind.Exposed, "Exposed: takes 50% more attack damage. Loses 1 stack at the end of each round." },
            { StatusKind.Weak, "Weak: deals 25% less attack damage. Loses 1 stack at the end of each round." },
            { StatusKind.Bleed, "Bleed: loses that much health at the end of the round, ignoring Block, then loses 1 stack." },
            { StatusKind.Hex, "Hex: does nothing by itself. Sister Ashe's cards use it up. Never fades." },
            { StatusKind.Riposte, "Riposte: when an enemy's attack hits this hero and they're still standing, the attacker takes that much damage. Gone at the next player turn." },
            { StatusKind.Fury, "Fury: each of its attacks deals that much more damage. Never fades." },
        };

        ContentLibrary _library;
        // The 3D set behind the fight, when the local art for one is on this machine (see CombatStage).
        CombatStage _stage;
        Camera _hiddenCamera;
        CombatState _combat;
        ulong _seed;
        Action _onFinished; // Set while showing a run's fight: called when the player leaves the end panel.
        VisualElement _root;
        int _selectedCard; // Instance id of the card waiting for a target; 0 when none.
        bool _discarding; // True while the player is choosing a card to discard.
        IReadOnlyList<CombatEvent> _preview = new CombatEvent[0]; // What the enemies would do if they acted now.
        readonly Dictionary<string, VisualElement> _views = new Dictionary<string, VisualElement>();
        readonly List<KeyValuePair<CardDef, VisualElement>> _cardViews = new List<KeyValuePair<CardDef, VisualElement>>();

        Label _round, _prompt, _seedLabel, _hint, _resolve, _piles, _endTitle, _endText;
        VisualElement _enemyRow, _heroRow, _hand, _endPanel, _floaters;
        ScrollView _log;
        Button _endActivation, _discard, _again;

        void OnEnable()
        {
            VisualElement root = _root = GetComponent<UIDocument>().rootVisualElement;
            _round = root.Q<Label>("round");
            _prompt = root.Q<Label>("prompt");
            _seedLabel = root.Q<Label>("seed");
            _hint = root.Q<Label>("hint");
            _resolve = root.Q<Label>("resolve");
            _piles = root.Q<Label>("piles");
            _endTitle = root.Q<Label>("end-title");
            _endText = root.Q<Label>("end-text");
            _enemyRow = root.Q("enemies");
            _heroRow = root.Q("heroes");
            _hand = root.Q("hand");
            _endPanel = root.Q("end-panel");
            _floaters = root.Q("floaters");
            root.Q(className: "battlefield").RegisterCallback<GeometryChangedEvent>(OnPanelLaidOut);
            _log = root.Q<ScrollView>("log");
            _endActivation = root.Q<Button>("end-activation");
            _discard = root.Q<Button>("discard");
            _again = root.Q<Button>("again");

            _endActivation.clicked += () =>
            {
                if (_combat == null) return;
                _selectedCard = 0;
                _discarding = false;
                Apply(() => _combat.TryEndActivation());
            };
            _discard.clicked += OnDiscardClicked;
            _again.clicked += OnEndButton;
            // Right-click anywhere cancels choosing a target.
            root.RegisterCallback<PointerDownEvent>(e => { if (e.button == 1) CancelTarget(); }, TrickleDown.TrickleDown);

            List<string> problems = new List<string>();
            if (content == null) problems.Add("The combat screen has no content database.");
            else _library = content.Build(out problems);
            if (heroPanel == null || enemyPanel == null || cardView == null) problems.Add("The combat screen is missing a layout.");
            if (problems.Count > 0)
            {
                ShowProblems(problems);
                return;
            }
            if (startTestFight) NewFight();
            else _root.style.display = DisplayStyle.None;
        }

        /// <summary>
        /// Shows a fight the run has already started, and calls <paramref name="onFinished"/> once it is over and the
        /// player presses Continue. The screen hides itself again before the call.
        /// </summary>
        public void Show(CombatState combat, ulong seed, Action onFinished)
        {
            _combat = combat;
            _seed = seed;
            _onFinished = onFinished;
            _selectedCard = 0;
            _log.Clear();
            foreach (CombatEvent e in combat.Log)
            {
                string line = CombatLogText.Describe(e, combat);
                if (line != null) AddLogLine(line, e.Kind);
            }
            _endPanel.AddToClassList("hidden");
            _again.text = "Continue";
            _root.style.display = DisplayStyle.Flex;
            SetHint("", false);
            ShowStage();
            Redraw();
        }

        void OnEndButton()
        {
            if (_onFinished == null)
            {
                NewFight();
                return;
            }
            Action finished = _onFinished;
            _onFinished = null;
            _combat = null;
            _root.style.display = DisplayStyle.None;
            HideStage();
            finished();
        }

        /// <summary>
        /// Leaves the fight without finishing it, when the player quits to the title. The run is thrown away with it;
        /// the save from before the room brings the fight back from its start.
        /// </summary>
        public void Abandon()
        {
            _onFinished = null;
            _combat = null;
            _selectedCard = 0;
            _root.style.display = DisplayStyle.None;
            HideStage();
        }

        void OnDisable() => HideStage();

        // ---------- the 3D stage ----------

        void ShowStage()
        {
            if (_stage != null) return;
            var prefab = Resources.Load<CombatStage>("CombatStage");
            if (prefab == null) return;
            _hiddenCamera = Camera.main;
            if (_hiddenCamera != null) _hiddenCamera.enabled = false;
            _stage = Instantiate(prefab);
            _root.AddToClassList("has-stage");
            // The rows cover the whole battlefield on a stage, so let clicks through to the panels in both of them.
            _heroRow.pickingMode = PickingMode.Ignore;
            _enemyRow.pickingMode = PickingMode.Ignore;
            Debug.Log("[Combat] Showing the fight on the 3D stage.");
        }

        // Puts each fighter's panel just above its head on the stage, or beside it when there's no room above, and keeps
        // the panels from overlapping. Runs after each redraw and when the screen changes size. Placing a panel moves it
        // but doesn't resize it, so only a change of size places them again; reacting to the moves would loop.
        void OnPanelLaidOut(GeometryChangedEvent e)
        {
            if (e.oldRect.size == e.newRect.size) return;
            PlacePanelsOnStage();
            // Once more on the next frame, outside this layout pass, in case the stage hadn't drawn its fighters yet.
            _heroRow.schedule.Execute(PlacePanelsOnStage);
        }

        void PlacePanelsOnStage()
        {
            if (_stage == null || _combat == null) return;
            // Both rows cover the battlefield on a stage, so their panels share one set of coordinates.
            Rect area = _heroRow.worldBound;
            if (_heroRow.panel == null || float.IsNaN(area.width) || area.width <= 0f) return;

            // Draw the stage into the battlefield, whatever the window's shape, so the fighters stand where the panels expect.
            Rect screen = _root.worldBound;
            _stage.SetViewport(new Rect(area.xMin / screen.width, 1f - area.yMax / screen.height, area.width / screen.width, area.height / screen.height));
            const float gap = 8f, clearance = 44f;

            var panels = new List<(VisualElement view, float left, float top, float width, float height)>();
            foreach (Combatant fighter in _combat.Heroes.Concat(_combat.Enemies))
            {
                if (!_views.TryGetValue(fighter.Id, out VisualElement view)) continue;
                Vector2? head = _stage.HeadInView(fighter.Id);
                if (head == null) continue;
                // The stage draws into the battlefield, so its view maps straight onto the rows (whose y runs downwards).
                Vector2 point = new Vector2(head.Value.x * area.width, (1f - head.Value.y) * area.height);
                float width = view.resolvedStyle.width, height = view.resolvedStyle.height;
                if (float.IsNaN(width) || float.IsNaN(height)) return;
                float left = point.x - width / 2f, top = point.y - height - gap;
                if (top >= -height / 4f) top = Mathf.Max(top, 0f);
                else
                {
                    // No room above the head: stand the panel beside the fighter instead, on the side facing the middle.
                    left = fighter.IsHero ? point.x + clearance : point.x - clearance - width;
                    top = Mathf.Clamp(point.y, gap, area.height - height - gap);
                }
                panels.Add((view, left, top, width, height));
            }

            panels.Sort((a, b) => a.left.CompareTo(b.left));
            for (int i = 1; i < panels.Count; i++)
            {
                var previous = panels[i - 1];
                float minLeft = previous.left + previous.width + gap;
                bool sameBand = panels[i].top < previous.top + previous.height && previous.top < panels[i].top + panels[i].height;
                if (sameBand && panels[i].left < minLeft) panels[i] = (panels[i].view, minLeft, panels[i].top, panels[i].width, panels[i].height);
            }
            // If the last panels were pushed off the right edge, slide the whole run back to the left.
            if (panels.Count > 0)
            {
                var last = panels[panels.Count - 1];
                float overflow = last.left + last.width + gap - area.width;
                if (overflow > 0f) for (int i = 0; i < panels.Count; i++) panels[i] = (panels[i].view, panels[i].left - overflow, panels[i].top, panels[i].width, panels[i].height);
            }
            foreach (var (view, left, top, width, _) in panels)
            {
                view.style.position = Position.Absolute;
                view.style.left = Mathf.Max(left, gap);
                view.style.top = top;
            }
        }

        void HideStage()
        {
            if (_stage == null) return;
            Destroy(_stage.gameObject);
            _stage = null;
            if (_hiddenCamera != null) _hiddenCamera.enabled = true;
            _hiddenCamera = null;
            _root?.RemoveFromClassList("has-stage");
            if (_heroRow != null) _heroRow.pickingMode = PickingMode.Position;
            if (_enemyRow != null) _enemyRow.pickingMode = PickingMode.Position;
        }

        /// <summary>True when Esc belongs to this screen this frame (cancelling a card's target), so it doesn't also pause.</summary>
        public bool UsesEscape => _selectedCard != 0 || _discarding || _escapeFrame == Time.frameCount;

        int _escapeFrame = -1;

        void Update()
        {
            if ((_selectedCard != 0 || _discarding) && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                _escapeFrame = Time.frameCount;
                CancelTarget();
            }
        }

        // ---------- starting a fight ----------

        void NewFight()
        {
            // A short seed, so it can be typed back into the Seed field to replay the fight.
            _seed = seed != 0 ? (ulong)seed : (ulong)(Environment.TickCount & 0xFFFFF) + 1;
            _selectedCard = 0;
            _log.Clear();
            _endPanel.AddToClassList("hidden");

            try
            {
                if (party.Any(h => h == null) || deck.Any(c => c == null) || enemies.Any(e => e == null))
                    throw new ArgumentException("The test fight has an empty slot in its party, deck or enemies.");
                List<CardInstance> cards = deck.Select((c, i) => new CardInstance(i + 1, c.Id)).ToList();
                _combat = new CombatState(_library, party.Select(h => h.Id).ToList(), cards,
                    enemies.Select(e => e.Id).ToList(), new Rng(_seed).Stream("combat"));
            }
            catch (Exception e) when (e is ArgumentException || e is KeyNotFoundException)
            {
                ShowProblems(new[] { e.Message });
                return;
            }

            Debug.Log($"[Combat] Fight started: {string.Join(", ", party.Select(h => h.Id))} against {string.Join(", ", enemies.Select(e => e.Id))}, seed {_seed}.");
            ShowStage();
            Apply(() => _combat.Start());
        }

        void ShowProblems(IEnumerable<string> problems)
        {
            foreach (string p in problems) Debug.LogError($"[Combat] {p}");
            _endTitle.text = "The fight can't start";
            _endText.text = string.Join("\n", problems);
            if (_root != null) _root.style.display = DisplayStyle.Flex;
            _again.AddToClassList("hidden");
            _endPanel.RemoveFromClassList("hidden");
        }

        // ---------- commands ----------

        /// <summary>
        /// Runs a command and shows its result: the reason it was refused, or its events in the log, a fresh redraw,
        /// and floating numbers for what changed.
        /// </summary>
        bool Apply(Func<CombatResult> command)
        {
            Dictionary<string, int> stacksBefore = StatusStacks();
            CombatResult result = command();
            if (!result.Ok)
            {
                Refuse(result.Reason);
                return false;
            }

            foreach (CombatEvent e in result.Events)
            {
                string line = CombatLogText.Describe(e, _combat);
                if (line != null) AddLogLine(line, e.Kind);
                if (e.Kind == CombatEventKind.CombatWon) Debug.Log($"[Combat] Won in round {_combat.Round}, seed {_seed}.");
                if (e.Kind == CombatEventKind.CombatLost) Debug.Log($"[Combat] Lost in round {_combat.Round}, seed {_seed}.");
            }
            SetHint("", false);
            Redraw();
            ShowFloaters(result.Events, stacksBefore);
            ShowOnStage(result.Events);
            return true;
        }

        void Refuse(string reason)
        {
            Debug.Log($"[Combat] Refused: {reason}");
            SetHint(reason, true);
        }

        void OnCardClicked(CardInstance card)
        {
            if (_discarding)
            {
                Discard(card.Id);
                return;
            }
            if (_selectedCard == card.Id)
            {
                CancelTarget();
                return;
            }

            string reason = _combat.WhyCantPlay(card.Id);
            if (reason != null)
            {
                Refuse(reason);
                return;
            }

            if (_combat.CardOf(card).Target == CardTarget.None)
            {
                _selectedCard = 0;
                Apply(() => _combat.TryPlay(card.Id));
                return;
            }

            _selectedCard = card.Id;
            Redraw();
        }

        void OnCombatantClicked(Combatant target)
        {
            if (_selectedCard == 0) return;
            int card = _selectedCard;
            Apply(() =>
            {
                CombatResult result = _combat.TryPlay(card, target.Id);
                if (result.Ok) _selectedCard = 0;
                return result;
            });
        }

        void CancelTarget()
        {
            if (_selectedCard == 0 && !_discarding) return;
            _selectedCard = 0;
            _discarding = false;
            Redraw();
        }

        /// <summary>Discards the card waiting for a target, or else asks which card to discard.</summary>
        void OnDiscardClicked()
        {
            if (_combat == null) return;
            if (_selectedCard != 0)
            {
                Discard(_selectedCard);
                return;
            }
            string reason = _combat.Hand.Count == 0 ? "There's nothing in your hand to discard." : _combat.WhyCantDiscard(_combat.Hand[0].Id);
            if (reason != null)
            {
                Refuse(reason);
                return;
            }
            _discarding = !_discarding;
            Redraw();
        }

        void Discard(int cardId)
        {
            Apply(() =>
            {
                CombatResult result = _combat.TryDiscard(cardId);
                if (result.Ok)
                {
                    _selectedCard = 0;
                    _discarding = false;
                }
                return result;
            });
        }

        // ---------- drawing ----------

        void Redraw()
        {
            CardDef selected = _selectedCard == 0 ? null : _combat.CardOf(_combat.FindCard(_selectedCard));
            _preview = _combat.PreviewEnemyPhase();
            if (_stage != null) _stage.Show(_combat, content);
            _views.Clear();
            _cardViews.Clear();

            _round.text = $"Round {_combat.Round}";
            _seedLabel.text = $"Seed {_seed}";
            _prompt.text = Prompt(selected);
            _resolve.text = _combat.Phase == CombatPhase.Activation ? _combat.Resolve.ToString() : "-";
            _endActivation.SetEnabled(_combat.Phase == CombatPhase.Activation);
            _discard.SetEnabled(_combat.Phase == CombatPhase.Activation && !_combat.HasDiscarded && _combat.Hand.Count > 0);
            _discard.text = selected != null ? $"Discard {selected.Name}" : _discarding ? "Cancel discard" : "Discard a card";
            _discard.EnableInClassList("is-selected", _discarding);
            _piles.text = $"Draw {_combat.DrawPile.Count}   Discard {_combat.DiscardPile.Count}" +
                (_combat.SetAside.Count > 0 ? $"   Set aside {_combat.SetAside.Count}" : "");

            _enemyRow.Clear();
            foreach (Combatant enemy in _combat.Enemies) _enemyRow.Add(_views[enemy.Id] = EnemyView(enemy, selected));
            _heroRow.Clear();
            foreach (Combatant hero in _combat.Heroes) _heroRow.Add(_views[hero.Id] = HeroView(hero, selected));
            // On a stage the panels are placed once they have been laid out and their sizes are known.
            if (_stage != null) foreach (VisualElement view in _views.Values) view.RegisterCallback<GeometryChangedEvent>(OnPanelLaidOut);
            _hand.Clear();
            foreach (CardInstance card in _combat.Hand) _hand.Add(CardView(card));

            if (_combat.IsOver)
            {
                bool won = _combat.Phase == CombatPhase.Won;
                _endTitle.text = won ? "Victory" : "Defeat";
                _endText.text = won
                    ? $"The room falls silent after {_combat.Round} rounds."
                    : "The darkness takes the party.";
                _again.RemoveFromClassList("hidden");
                _endPanel.RemoveFromClassList("hidden");
            }
        }

        string Prompt(CardDef selected)
        {
            switch (_combat.Phase)
            {
                case CombatPhase.ChooseHero: return "Choose a hero to activate.";
                case CombatPhase.Activation:
                    if (_discarding) return "Choose a card to discard. The next draw replaces it. Right-click or Esc to cancel.";
                    if (selected != null)
                        return $"Choose {(selected.Target == CardTarget.Enemy ? "an enemy" : "an ally")} for {selected.Name}. Right-click or Esc to cancel.";
                    return $"{_combat.Find(_combat.ActiveHeroId).Name} is active. Play their cards, then end the activation.";
                default: return "";
            }
        }

        VisualElement HeroView(Combatant hero, CardDef selected)
        {
            VisualElement view = Views.Copy(heroPanel, "hero");
            view.AddToClassList(Views.OwnerClass(hero.Hero.Colour));
            view.Q<Label>("name").text = hero.Name;
            view.Q<Label>("title").text = hero.Hero.Title;
            DrawHealth(view, hero);
            DrawBlockAndStatuses(view, hero);

            Label guard = view.Q<Label>("guard");
            guard.text = hero.GuardedBy == null ? "" : $"Guarded by {_combat.Find(hero.GuardedBy).Name}";
            guard.EnableInClassList("hidden", hero.GuardedBy == null);

            // What the enemies' shown intents would take off this hero if the activation ended now. Enemies the party
            // only sees vaguely are left out, and so is whether the hero would fall when one of them is in the fight.
            int incoming = _preview.Where(e => e.Kind == CombatEventKind.Damaged && e.TargetId == hero.Id && !IsVague(e.SourceId)).Sum(e => e.Amount);
            bool falls = _combat.Enemies.Any(_combat.IsVague)
                ? incoming >= hero.Health
                : _preview.Any(e => e.Kind == CombatEventKind.HeroDowned && e.SourceId == hero.Id);
            Label incomingBadge = view.Q<Label>("incoming");
            incomingBadge.text = falls ? $"Incoming {incoming}: would fall" : $"Incoming {incoming}";
            incomingBadge.EnableInClassList("hidden", incoming == 0 || hero.IsDown);
            incomingBadge.EnableInClassList("is-lethal", falls);

            bool active = hero.Id == _combat.ActiveHeroId;
            Button activate = view.Q<Button>("activate");
            activate.EnableInClassList("hidden", _combat.Phase != CombatPhase.ChooseHero || hero.IsDown);
            activate.clicked += () => Apply(() => _combat.TryActivate(hero.Id));
            activate.RegisterCallback<PointerEnterEvent>(_ => HighlightCardsOf(hero));
            activate.RegisterCallback<PointerLeaveEvent>(_ => HighlightCardsOf(null));

            Label state = view.Q<Label>("state");
            state.text = hero.IsDown ? "Down" : active ? "Active" : "";
            state.EnableInClassList("hidden", !hero.IsDown && !active);
            view.EnableInClassList("is-active", active);
            view.EnableInClassList("is-down", hero.IsDown);

            bool targetable = selected != null && selected.Target == CardTarget.Ally && !hero.IsDown;
            if (targetable && hero.Id == _combat.ActiveHeroId && selected.Effects.Any(e => e.Kind == EffectKind.Guard)) targetable = false;
            view.EnableInClassList("is-targetable", targetable);
            view.RegisterCallback<ClickEvent>(_ => OnCombatantClicked(hero));
            return view;
        }

        VisualElement EnemyView(Combatant enemy, CardDef selected)
        {
            VisualElement view = Views.Copy(enemyPanel, "enemy");
            view.Q<Label>("name").text = enemy.Name;
            DrawHealth(view, enemy);
            DrawBlockAndStatuses(view, enemy);

            bool showIntent = !enemy.IsDown && !_combat.IsOver && enemy.Intent != null;
            bool vague = _combat.IsVague(enemy);
            VisualElement intent = view.Q(className: "intent");
            intent.EnableInClassList("hidden", !showIntent);
            intent.EnableInClassList("is-vague", vague);
            intent.EnableInClassList("is-cancelled", enemy.IntentCancelled);
            if (showIntent)
            {
                // A vague intent shows only its line: no numbers, no target and no outcome.
                view.Q<Label>("intent").text = _combat.IntentText(enemy, enemy.Intent) + (enemy.IntentCancelled ? " (cancelled)" : "");
                view.Q<Label>("intent-target").text = vague || enemy.IntentCancelled ? "" : IntentTargetText(enemy);
            }
            Label outcome = view.Q<Label>("intent-outcome");
            outcome.text = showIntent && !vague ? OutcomeText(enemy) : "";
            outcome.EnableInClassList("hidden", outcome.text.Length == 0);
            DrawCancel(view, enemy);

            Label state = view.Q<Label>("state");
            state.text = "Defeated";
            state.EnableInClassList("hidden", !enemy.IsDown);
            view.EnableInClassList("is-down", enemy.IsDown);
            view.EnableInClassList("is-targetable", selected != null && selected.Target == CardTarget.Enemy && !enemy.IsDown);
            view.RegisterCallback<ClickEvent>(_ => OnCombatantClicked(enemy));
            return view;
        }

        /// <summary>
        /// The button for the run's cancel ability (such as "Speak his name"), on the enemy it works on. It stays
        /// visible but dimmed while it can't be used, and says why on hover.
        /// </summary>
        void DrawCancel(VisualElement view, Combatant enemy)
        {
            CancelAbility cancel = _combat.Cancel;
            Button button = view.Q<Button>("cancel-intent");
            bool shown = cancel != null && enemy.Enemy.Id == cancel.EnemyId && !enemy.IsDown && !_combat.IsOver && _combat.CancelsLeft > 0;
            button.EnableInClassList("hidden", !shown);
            if (!shown) return;
            string reason = _combat.WhyCantCancel(enemy.Id);
            button.text = cancel.Label;
            button.EnableInClassList("is-unavailable", reason != null);
            string help = reason ?? $"{cancel.Label}: cancel this {cancel.Kind.ToString().ToLowerInvariant()}. Once per fight.";
            button.RegisterCallback<PointerEnterEvent>(_ => SetHint(help, false));
            button.RegisterCallback<PointerLeaveEvent>(_ => SetHint("", false));
            button.clicked += () => Apply(() => _combat.TryCancelIntent(enemy.Id));
        }

        bool IsVague(string combatantId) => combatantId != null && _combat.IsVague(_combat.Find(combatantId));

        /// <summary>What this enemy's intent would really do now, after Block, Weak, Exposed and Guard.</summary>
        string OutcomeText(Combatant enemy)
        {
            IEnumerable<string> hits = _preview
                .Where(e => e.Kind == CombatEventKind.Damaged && e.SourceId == enemy.Id)
                .Select(e => e.Amount == 0 ? $"{_combat.Find(e.TargetId).Name} blocks it all"
                    : $"{_combat.Find(e.TargetId).Name} loses {e.Amount}" + (e.Blocked > 0 ? $" ({e.Blocked} blocked)" : ""));
            return string.Join(", ", hits);
        }

        string IntentTargetText(Combatant enemy)
        {
            switch (enemy.Intent.Target)
            {
                case IntentTarget.AllHeroes: return "at every hero";
                case IntentTarget.Self: return "on itself";
                default: return enemy.IntentTargetId == null ? "" : $"at {_combat.Find(enemy.IntentTargetId).Name}";
            }
        }

        VisualElement CardView(CardInstance card)
        {
            CardDef def = _combat.CardOf(card);
            VisualElement view = Views.Card(cardView, def, _library);
            // A Keepsake can make the first card of a fight cheaper.
            int cost = _combat.CostOf(card);
            view.Q<Label>("cost").text = cost.ToString();
            view.EnableInClassList("is-discounted", cost < def.Cost);

            string reason = _combat.WhyCantPlay(card.Id);
            view.EnableInClassList("is-unplayable", reason != null && !_discarding);
            view.EnableInClassList("is-selected", card.Id == _selectedCard);
            string boost = Boost(def);
            view.EnableInClassList("is-boosted", boost != null);
            _cardViews.Add(new KeyValuePair<CardDef, VisualElement>(def, view));

            string help = _discarding ? $"Click to discard {def.Name}." : reason ?? (def.Target == CardTarget.None ? $"Click to play {def.Name}." : $"Click {def.Name}, then choose {(def.Target == CardTarget.Enemy ? "an enemy" : "an ally")}.");
            if (boost != null) help += " " + boost;
            // Guard is explained here rather than on the card, so the card's text stays short enough to fit.
            if (def.Effects.Any(e => e.Kind == EffectKind.Guard)) help += " Guard: attacks aimed at that ally hit the guard instead.";
            view.RegisterCallback<PointerEnterEvent>(_ => SetHint(help, false));
            view.RegisterCallback<PointerLeaveEvent>(_ => SetHint("", false));
            view.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                OnCardClicked(card);
            });
            return view;
        }

        /// <summary>Why the card is stronger than usual right now, or null. Read from its effects, not its name.</summary>
        string Boost(CardDef def)
        {
            List<Combatant> standing = _combat.Enemies.Where(e => !e.IsDown).ToList();
            if (def.Effects.Any(e => e.BonusIfExposed > 0) && standing.Any(e => e.Status(StatusKind.Exposed) > 0))
                return "An enemy is Exposed, so it hits harder.";
            if (def.Effects.Any(e => e.Kind == EffectKind.ConsumeHex) && standing.Any(e => e.Status(StatusKind.Hex) > 0))
                return $"{string.Join(" and ", standing.Where(e => e.Status(StatusKind.Hex) > 0).Select(e => $"{e.Name} has {e.Status(StatusKind.Hex)} Hex"))}.";
            return null;
        }

        /// <summary>While the pointer is on a hero's Activate button, lifts the cards that hero could play.</summary>
        void HighlightCardsOf(Combatant hero)
        {
            foreach (KeyValuePair<CardDef, VisualElement> card in _cardViews)
                card.Value.EnableInClassList("is-highlighted", hero != null && card.Key.IsOwnedBy(hero.Id));
            if (hero == null) { SetHint("", false); return; }
            int count = _cardViews.Count(c => c.Key.IsOwnedBy(hero.Id));
            SetHint(count == 0 ? $"{hero.Name} has no cards in hand." : $"{hero.Name} can play {count} of these cards.", false);
        }

        // ---------- floating numbers ----------

        const long FloaterGapMs = 450;

        Dictionary<string, int> StatusStacks()
        {
            var stacks = new Dictionary<string, int>();
            if (_combat == null) return stacks;
            foreach (Combatant c in _combat.Heroes.Concat(_combat.Enemies))
                foreach (KeyValuePair<StatusKind, int> s in c.Statuses) stacks[$"{c.Id}/{s.Key}"] = s.Value;
            return stacks;
        }

        /// <summary>
        /// Floats damage, Block and new statuses up from the panels they hit. Each enemy's action gets its own beat,
        /// so the enemy phase can be followed. Statuses only float when they grow, not when they tick down.
        /// </summary>
        void ShowFloaters(IReadOnlyList<CombatEvent> events, Dictionary<string, int> stacks)
        {
            int beat = 0;
            var perTarget = new Dictionary<string, int>();
            foreach (CombatEvent e in events)
            {
                string text = null, style = null;
                switch (e.Kind)
                {
                    case CombatEventKind.EnemyActed:
                        beat++;
                        Flash(e.SourceId, beat);
                        break;
                    case CombatEventKind.Damaged:
                        text = e.Amount > 0 ? $"-{e.Amount}" : "Blocked";
                        style = e.Amount > 0 ? "floater-damage" : "floater-block";
                        break;
                    case CombatEventKind.BlockGained:
                        text = $"+{e.Amount} Block";
                        style = "floater-block";
                        break;
                    case CombatEventKind.Healed:
                        text = $"+{e.Amount}";
                        style = "floater-heal";
                        break;
                    case CombatEventKind.StatusChanged:
                        string key = $"{e.TargetId}/{e.Status}";
                        stacks.TryGetValue(key, out int before);
                        stacks[key] = e.Amount;
                        if (e.Amount > before) { text = $"{e.Status} {e.Amount}"; style = "floater-status"; }
                        break;
                }
                if (text == null || e.TargetId == null) continue;

                string slot = $"{e.TargetId}/{beat}";
                perTarget.TryGetValue(slot, out int row);
                perTarget[slot] = row + 1;
                Float(e.TargetId, text, style, beat, row);
            }
        }

        void Float(string targetId, string text, string style, int beat, int row)
        {
            _floaters.schedule.Execute(() =>
            {
                // Looked up when it runs, because a redraw replaces the panels.
                if (!_views.TryGetValue(targetId, out VisualElement view) || view.panel == null) return;
                Rect area = view.worldBound;
                Rect origin = _floaters.worldBound;
                var label = new Label(text) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("floater");
                label.AddToClassList(style);
                label.style.left = area.center.x - origin.x - 60;
                label.style.top = area.y - origin.y + 40 + row * 26;
                _floaters.Add(label);
                label.style.transitionDuration = new StyleList<TimeValue>(new List<TimeValue> { new TimeValue(1.2f * GameSettings.AnimationScale) });
                label.schedule.Execute(() => label.AddToClassList("floater-gone")).StartingIn(30);
                label.schedule.Execute(label.RemoveFromHierarchy).StartingIn(Scaled(1400));
            }).StartingIn(60 + Scaled(beat * FloaterGapMs));
        }

        void Flash(string enemyId, int beat)
        {
            _floaters.schedule.Execute(() =>
            {
                if (!_views.TryGetValue(enemyId, out VisualElement view)) return;
                view.AddToClassList("is-acting");
                view.schedule.Execute(() => view.RemoveFromClassList("is-acting")).StartingIn(Scaled(380));
            }).StartingIn(Scaled(beat * FloaterGapMs));
        }

        // ---------- the stage ----------

        /// <summary>How long after a swing starts its blow lands, and the target flinches.</summary>
        const long HitDelayMs = 300;

        /// <summary>
        /// Shows the events on the 3D stage, on the same beats as the floating numbers. Whoever plays a card or acts
        /// swings if it hits someone, and makes a gesture if it doesn't. Whoever is hit flinches, or staggers under a
        /// heavy blow. Block, a blow that Block stopped, and guarding an ally raise a guard. The fallen fall and stay down.
        /// </summary>
        void ShowOnStage(IReadOnlyList<CombatEvent> events)
        {
            if (_stage == null) return;
            int beat = 0;
            string actor = null;
            bool swung = false;
            for (int i = 0; i < events.Count; i++)
            {
                CombatEvent e = events[i];
                switch (e.Kind)
                {
                    case CombatEventKind.CardPlayed:
                    case CombatEventKind.EnemyActed:
                        if (e.Kind == CombatEventKind.EnemyActed) beat++;
                        actor = e.SourceId;
                        swung = false;
                        // The swing waits for the first blow; an action that hits no one is a gesture instead.
                        bool hits = events.Skip(i + 1)
                            .TakeWhile(n => n.Kind != CombatEventKind.CardPlayed && n.Kind != CombatEventKind.EnemyActed)
                            .Any(n => n.Kind == CombatEventKind.Damaged && n.SourceId == actor && n.TargetId != actor);
                        if (!hits) Stage(actor, CombatStage.FighterAction.Cast, beat, 0);
                        break;
                    case CombatEventKind.Damaged:
                        if (e.SourceId != null && e.SourceId == actor && e.TargetId != actor && !swung)
                        {
                            Stage(actor, CombatStage.FighterAction.Attack, beat, 0);
                            swung = true;
                        }
                        // Paying health is no blow.
                        if (e.TargetId == e.SourceId) break;
                        Stage(e.TargetId, e.Amount == 0 ? CombatStage.FighterAction.Guard : e.Amount >= 10 ? CombatStage.FighterAction.Stagger : CombatStage.FighterAction.Hit, beat, HitDelayMs);
                        break;
                    case CombatEventKind.BlockGained:
                        Stage(e.TargetId, CombatStage.FighterAction.Guard, beat, 150);
                        break;
                    case CombatEventKind.Guarded:
                        Stage(e.SourceId, CombatStage.FighterAction.Guard, beat, 150);
                        break;
                    case CombatEventKind.HeroDowned:
                    case CombatEventKind.EnemyDefeated:
                        Stage(e.SourceId, CombatStage.FighterAction.Death, beat, HitDelayMs + 150);
                        break;
                }
            }
        }

        void Stage(string fighterId, CombatStage.FighterAction action, int beat, long offsetMs)
        {
            if (fighterId == null) return;
            _floaters.schedule.Execute(() => _stage?.Act(fighterId, action, 1f / GameSettings.AnimationScale))
                .StartingIn(Scaled(beat * FloaterGapMs + offsetMs));
        }

        /// <summary>A delay in milliseconds at the player's animation speed.</summary>
        static long Scaled(long ms) => (long)(ms * GameSettings.AnimationScale);

        void DrawHealth(VisualElement view, Combatant c)
        {
            view.Q<Label>("health").text = $"{c.Health} / {c.MaxHealth}";
            view.Q("health-fill").style.width = Length.Percent(100f * c.Health / c.MaxHealth);
        }

        void DrawBlockAndStatuses(VisualElement view, Combatant c)
        {
            Label block = view.Q<Label>("block");
            block.text = $"Block {c.Block}";
            block.EnableInClassList("hidden", c.Block == 0);

            VisualElement statuses = view.Q("statuses");
            foreach (KeyValuePair<StatusKind, int> status in c.Statuses.OrderBy(s => s.Key))
            {
                var chip = new Label($"{status.Key} {status.Value}");
                chip.AddToClassList("status");
                chip.AddToClassList($"status-{status.Key.ToString().ToLowerInvariant()}");
                string help = StatusHelp[status.Key];
                chip.RegisterCallback<PointerEnterEvent>(_ => SetHint(help, false));
                chip.RegisterCallback<PointerLeaveEvent>(_ => SetHint("", false));
                statuses.Add(chip);
            }
        }

        void SetHint(string text, bool error)
        {
            _hint.text = text;
            _hint.EnableInClassList("is-error", error);
        }

        void AddLogLine(string text, CombatEventKind kind)
        {
            var line = new Label(text);
            line.AddToClassList("log-line");
            if (kind == CombatEventKind.RoundStarted) line.AddToClassList("log-round");
            if (kind == CombatEventKind.CombatWon || kind == CombatEventKind.CombatLost) line.AddToClassList("log-end");
            _log.Add(line);
            _log.schedule.Execute(() => _log.ScrollTo(line));
        }
    }
}

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
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class CombatScreen : MonoBehaviour
    {
        [SerializeField] ContentDatabase content;

        [Header("Test fight (until the map exists in Milestone 3)")]
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
        };

        ContentLibrary _library;
        CombatState _combat;
        ulong _seed;
        int _selectedCard; // Instance id of the card waiting for a target; 0 when none.

        Label _round, _prompt, _seedLabel, _hint, _resolve, _piles, _endTitle, _endText;
        VisualElement _enemyRow, _heroRow, _hand, _endPanel;
        ScrollView _log;
        Button _endActivation, _again;

        void OnEnable()
        {
            VisualElement root = GetComponent<UIDocument>().rootVisualElement;
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
            _log = root.Q<ScrollView>("log");
            _endActivation = root.Q<Button>("end-activation");
            _again = root.Q<Button>("again");

            _endActivation.clicked += () =>
            {
                if (_combat == null) return;
                _selectedCard = 0;
                Apply(_combat.TryEndActivation());
            };
            _again.clicked += NewFight;
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
            NewFight();
        }

        void Update()
        {
            if (_selectedCard != 0 && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) CancelTarget();
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
            Apply(_combat.Start());
        }

        void ShowProblems(IEnumerable<string> problems)
        {
            foreach (string p in problems) Debug.LogError($"[Combat] {p}");
            _endTitle.text = "The fight can't start";
            _endText.text = string.Join("\n", problems);
            _again.AddToClassList("hidden");
            _endPanel.RemoveFromClassList("hidden");
        }

        // ---------- commands ----------

        /// <summary>Shows a command's result: the reason it was refused, or its events in the log and a fresh redraw.</summary>
        bool Apply(CombatResult result)
        {
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
            return true;
        }

        void Refuse(string reason)
        {
            Debug.Log($"[Combat] Refused: {reason}");
            SetHint(reason, true);
        }

        void OnCardClicked(CardInstance card)
        {
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
                Apply(_combat.TryPlay(card.Id));
                return;
            }

            _selectedCard = card.Id;
            Redraw();
        }

        void OnCombatantClicked(Combatant target)
        {
            if (_selectedCard == 0) return;
            CombatResult result = _combat.TryPlay(_selectedCard, target.Id);
            if (result.Ok) _selectedCard = 0;
            Apply(result);
        }

        void CancelTarget()
        {
            if (_selectedCard == 0) return;
            _selectedCard = 0;
            Redraw();
        }

        // ---------- drawing ----------

        void Redraw()
        {
            CardDef selected = _selectedCard == 0 ? null : _combat.CardOf(_combat.FindCard(_selectedCard));

            _round.text = $"Round {_combat.Round}";
            _seedLabel.text = $"Seed {_seed}";
            _prompt.text = Prompt(selected);
            _resolve.text = _combat.Phase == CombatPhase.Activation ? _combat.Resolve.ToString() : "-";
            _endActivation.SetEnabled(_combat.Phase == CombatPhase.Activation);
            _piles.text = $"Draw {_combat.DrawPile.Count}   Discard {_combat.DiscardPile.Count}" +
                (_combat.SetAside.Count > 0 ? $"   Set aside {_combat.SetAside.Count}" : "");

            _enemyRow.Clear();
            foreach (Combatant enemy in _combat.Enemies) _enemyRow.Add(EnemyView(enemy, selected));
            _heroRow.Clear();
            foreach (Combatant hero in _combat.Heroes) _heroRow.Add(HeroView(hero, selected));
            _hand.Clear();
            foreach (CardInstance card in _combat.Hand) _hand.Add(CardView(card));

            if (_combat.IsOver)
            {
                bool won = _combat.Phase == CombatPhase.Won;
                _endTitle.text = won ? "Victory" : "Defeat";
                _endText.text = won
                    ? $"The chapel falls silent after {_combat.Round} rounds."
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
                    if (selected != null)
                        return $"Choose {(selected.Target == CardTarget.Enemy ? "an enemy" : "an ally")} for {selected.Name}. Right-click or Esc to cancel.";
                    return $"{_combat.Find(_combat.ActiveHeroId).Name} is active. Play their cards, then end the activation.";
                default: return "";
            }
        }

        VisualElement HeroView(Combatant hero, CardDef selected)
        {
            VisualElement view = Copy(heroPanel, "hero");
            view.AddToClassList(OwnerClass(hero.Hero.Colour));
            view.Q<Label>("name").text = hero.Name;
            view.Q<Label>("title").text = hero.Hero.Title;
            DrawHealth(view, hero);
            DrawBlockAndStatuses(view, hero);

            Label guard = view.Q<Label>("guard");
            guard.text = hero.GuardedBy == null ? "" : $"Guarded by {_combat.Find(hero.GuardedBy).Name}";
            guard.EnableInClassList("hidden", hero.GuardedBy == null);

            bool active = hero.Id == _combat.ActiveHeroId;
            Button activate = view.Q<Button>("activate");
            activate.EnableInClassList("hidden", _combat.Phase != CombatPhase.ChooseHero || hero.IsDown);
            activate.clicked += () => Apply(_combat.TryActivate(hero.Id));

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
            VisualElement view = Copy(enemyPanel, "enemy");
            view.Q<Label>("name").text = enemy.Name;
            DrawHealth(view, enemy);
            DrawBlockAndStatuses(view, enemy);

            bool showIntent = !enemy.IsDown && !_combat.IsOver && enemy.Intent != null;
            view.Q(className: "intent").EnableInClassList("hidden", !showIntent);
            if (showIntent)
            {
                view.Q<Label>("intent").text = enemy.Intent.Description;
                view.Q<Label>("intent-target").text = IntentTargetText(enemy);
            }

            Label state = view.Q<Label>("state");
            state.text = "Defeated";
            state.EnableInClassList("hidden", !enemy.IsDown);
            view.EnableInClassList("is-down", enemy.IsDown);
            view.EnableInClassList("is-targetable", selected != null && selected.Target == CardTarget.Enemy && !enemy.IsDown);
            view.RegisterCallback<ClickEvent>(_ => OnCombatantClicked(enemy));
            return view;
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
            VisualElement view = Copy(cardView, "card");
            view.Q<Label>("cost").text = def.Cost.ToString();
            view.Q<Label>("name").text = def.Name;
            view.Q<Label>("text").text = def.Text;
            view.Q<Label>("target").text = def.Target == CardTarget.Enemy ? "Target an enemy" : def.Target == CardTarget.Ally ? "Target an ally" : "";

            List<HeroDef> owners = def.Owners.Select(id => _library.Hero(id)).ToList();
            view.Q<Label>("owner-names").text = string.Join(" or ", owners.Select(o => o.Name));
            VisualElement swatches = view.Q("owners");
            foreach (HeroDef owner in owners)
            {
                var swatch = new VisualElement();
                swatch.AddToClassList("swatch");
                swatch.AddToClassList(OwnerClass(owner.Colour));
                swatches.Add(swatch);
            }
            if (owners.Count == 1) view.AddToClassList(OwnerClass(owners[0].Colour));
            else view.AddToClassList("is-dual");

            string reason = _combat.WhyCantPlay(card.Id);
            view.EnableInClassList("is-unplayable", reason != null);
            view.EnableInClassList("is-selected", card.Id == _selectedCard);

            string help = reason ?? (def.Target == CardTarget.None ? $"Click to play {def.Name}." : $"Click {def.Name}, then choose {(def.Target == CardTarget.Enemy ? "an enemy" : "an ally")}.");
            view.RegisterCallback<PointerEnterEvent>(_ => SetHint(help, false));
            view.RegisterCallback<PointerLeaveEvent>(_ => SetHint("", false));
            view.RegisterCallback<ClickEvent>(e =>
            {
                e.StopPropagation();
                OnCardClicked(card);
            });
            return view;
        }

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

        /// <summary>A copy of a layout's root element, such as the "card" element of Card.uxml.</summary>
        static VisualElement Copy(VisualTreeAsset layout, string rootClass) => layout.Instantiate().Q(className: rootClass);

        static string OwnerClass(string colour) => $"owner-{(colour ?? "").ToLowerInvariant()}";
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    public enum RunPhase
    {
        /// <summary>On the map, choosing the next room.</summary>
        ChooseRoom,
        /// <summary>In a fight. The player plays it through <see cref="RunState.Combat"/>, then calls <see cref="RunState.TryFinishCombat"/>.</summary>
        Combat,
        /// <summary>A fight was won: take one of the offered cards or skip.</summary>
        Reward,
        /// <summary>Rest or Reflect.</summary>
        Sanctuary,
        /// <summary>Search the room for its clue at a cost, or leave.</summary>
        Investigation,
        /// <summary>The villain's secret room: act there at a cost, or leave.</summary>
        Secret,
        /// <summary>An Event room: choose one of <see cref="RunState.EventChoices"/>.</summary>
        Event,
        /// <summary>An event asked the party to remove a card from the deck: choose it with <see cref="RunState.TryReflect"/>.</summary>
        RemoveCard,
        /// <summary>An Event room with no event left to tell. The party just carries on.</summary>
        Unwritten,
        Won,
        Lost,
    }

    /// <summary>
    /// One expedition into the Keep: the map, where the party is, their health and their deck. The interface reads
    /// it and calls the Try* commands; a refused command returns a reason and changes nothing.
    /// </summary>
    public sealed class RunState
    {
        public const int RewardChoices = 3;
        /// <summary>How many heroes go into the Keep.</summary>
        public const int PartySize = 3;
        /// <summary>A hero who fell in a won fight stands back up with this share of their maximum health, rounded down (at least 1).</summary>
        public const int StandUpPercent = 25;
        /// <summary>Resting heals each hero this share of their maximum health, rounded down.</summary>
        public const int RestHealPercent = 30;

        readonly ContentLibrary _content;
        readonly Rng _seedRng;
        readonly Rng _rewardRng;
        readonly List<CardInstance> _deck;
        readonly Dictionary<string, int> _health;
        readonly List<int> _route = new List<int>();
        readonly List<string> _clues = new List<string>();
        readonly List<string> _keepsakes = new List<string>();
        readonly List<string> _eventsSeen = new List<string>();
        readonly Dictionary<string, int> _maxHealthBonus = new Dictionary<string, int>();
        List<string> _offer = new List<string>();
        int _nextCardId;

        /// <param name="party">The chosen heroes; null for the expedition's first <see cref="PartySize"/>. Check it with <see cref="WhyCantChoose"/>.</param>
        public RunState(ContentLibrary content, ExpeditionDef expedition, ulong seed, IReadOnlyList<string> party = null)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
            IReadOnlyList<string> problems = content.Validate(expedition);
            if (problems.Count > 0) throw new ArgumentException("The expedition can't be used: " + string.Join(" ", problems), nameof(expedition));
            party = party ?? expedition.Heroes.Take(PartySize).ToList();
            string wrong = WhyCantChoose(expedition, party);
            if (wrong != null) throw new ArgumentException(wrong, nameof(party));
            Party = party.ToArray();

            Seed = seed;
            _seedRng = new Rng(seed);
            _rewardRng = _seedRng.Stream("rewards");
            Map = MapGenerator.Generate(expedition, _seedRng.Stream("map"));
            _deck = StartingCards(content, expedition, Party).Select((id, i) => new CardInstance(i + 1, id)).ToList();
            _nextCardId = _deck.Count + 1;
            _health = Party.ToDictionary(id => id, id => content.Hero(id).MaxHealth);
        }

        /// <summary>Why this party can't go into the Keep, written for the player, or null if it can.</summary>
        public static string WhyCantChoose(ExpeditionDef expedition, IReadOnlyList<string> party)
        {
            if (party == null || party.Count != PartySize) return $"Choose {PartySize} heroes.";
            if (party.Distinct().Count() != party.Count) return "Choose each hero only once.";
            if (party.Any(h => !expedition.Heroes.Contains(h))) return "Choose heroes from the list.";
            return null;
        }

        /// <summary>The party's starting deck: each hero's own cards and the dual cards whose owners are all in the party.</summary>
        public static IEnumerable<string> StartingCards(ContentLibrary content, ExpeditionDef expedition, IReadOnlyList<string> party) =>
            expedition.StartingDeck.Where(id => content.Card(id).Owners.All(party.Contains));

        public ExpeditionDef Expedition { get; }
        public ulong Seed { get; }
        public MapGraph Map { get; }
        public RunPhase Phase { get; private set; } = RunPhase.ChooseRoom;

        /// <summary>The room the party is in or last left; null before the first room.</summary>
        public MapRoom CurrentRoom { get; private set; }
        /// <summary>The rooms entered so far, in order.</summary>
        public IReadOnlyList<int> Route => _route;
        /// <summary>The fight in progress, or the one just finished until the reward or the run's end is dealt with.</summary>
        public CombatState Combat { get; private set; }
        /// <summary>The card ids to choose from in the Reward phase.</summary>
        public IReadOnlyList<string> RewardOffer => _offer;

        /// <summary>The heroes in this run, in the order they were chosen.</summary>
        public IReadOnlyList<string> Party { get; }
        public IReadOnlyList<CardInstance> Deck => _deck;
        public int Health(string heroId) => _health.TryGetValue(heroId, out int h) ? h : 0;
        public int MaxHealth(string heroId) => _content.Hero(heroId).MaxHealth + (_maxHealthBonus.TryGetValue(heroId, out int bonus) ? bonus : 0);
        public CardDef CardOf(CardInstance card) => _content.Card(card.DefId);

        public bool IsOver => Phase == RunPhase.Won || Phase == RunPhase.Lost;
        public int FightsWon { get; private set; }

        public VillainDef Villain => Expedition.Villain;
        /// <summary>The clues found so far, in the order they were found.</summary>
        public IReadOnlyList<string> CluesFound => _clues;
        /// <summary>The clue the last won fight gave (an Elite's), shown with the reward; null if none.</summary>
        public string RewardClue { get; private set; }
        public bool SecretRevealed => Map.Secret != null && _clues.Any(id => Villain.Clue(id).Has(ClueEffectKind.RevealSecretRoom));
        /// <summary>True once the party has acted in the secret room.</summary>
        public bool SecretUsed { get; private set; }
        /// <summary>After a won run: whether it earned the villain's true ending.</summary>
        public bool TrueEnding => Phase == RunPhase.Won && HasEffect(ClueEffectKind.TrueEnding);
        public string EndingText => Phase != RunPhase.Won || Villain == null ? null : TrueEnding ? Villain.TrueEnding : Villain.Ending;

        public bool HasEffect(ClueEffectKind kind) => Effects(kind).Any();

        /// <summary>The Keepsakes found so far, in the order they were found.</summary>
        public IReadOnlyList<string> Keepsakes => _keepsakes;
        /// <summary>The Keepsake the last won fight gave (an Elite's), shown with the reward; null if none.</summary>
        public string RewardKeepsake { get; private set; }

        /// <summary>The event in the Event room the party is in, or null.</summary>
        public EventDef CurrentEvent { get; private set; }
        /// <summary>What the last event choice did, for the interface to show once. Null when there is nothing to show.</summary>
        public string EventResult { get; private set; }

        /// <summary>Whether a choice of the current event is shown, given the clues found so far.</summary>
        public bool IsChoiceShown(EventChoice choice) =>
            (choice.RequiresClue == null || _clues.Contains(choice.RequiresClue)) &&
            (choice.HiddenByClue == null || !_clues.Contains(choice.HiddenByClue));

        int KeepsakeTotal(KeepsakeEffectKind kind) =>
            _keepsakes.Select(Expedition.Keepsake).Where(k => k.Kind == kind).Sum(k => k.Amount);

        IEnumerable<ClueEffect> Effects(ClueEffectKind kind) =>
            _clues.SelectMany(id => Villain.Clue(id).Effects).Where(e => e.Kind == kind);

        /// <summary>The rooms the party can enter next: the first layer at the start, then the exits of the current room.</summary>
        public IEnumerable<MapRoom> ReachableRooms =>
            Phase != RunPhase.ChooseRoom ? Enumerable.Empty<MapRoom>()
            : CurrentRoom == null ? Map.Layer(0)
            : Map.NextOf(CurrentRoom, SecretRevealed);

        // ---------- commands ----------

        public RunResult TryEnterRoom(int roomId)
        {
            if (Phase != RunPhase.ChooseRoom) return RunResult.Refused(IsOver ? "The expedition is over." : "Finish this room first.");
            MapRoom room = Map.Room(roomId);
            if (room == null) return RunResult.Refused("There is no such room.");
            if (!ReachableRooms.Contains(room)) return RunResult.Refused("That room isn't connected to this one.");

            if (room.EncounterId != null)
            {
                EncounterDef encounter = Expedition.Encounter(room.EncounterId);
                bool boss = room.Kind == RoomKind.Boss;
                IReadOnlyList<string> enemies = boss ? BossEnemies() : encounter.EnemyIds;
                RunResult started = StartFight(room, enemies, boss);
                if (!started.Ok) return started;
            }

            EventDef nextEvent = null;
            if (room.Kind == RoomKind.Event)
            {
                List<EventDef> unseen = Expedition.Events.Where(e => !_eventsSeen.Contains(e.Id)).ToList();
                if (unseen.Count > 0) nextEvent = unseen[_rewardRng.Next(unseen.Count)];
            }

            CurrentRoom = room;
            _route.Add(room.Id);
            switch (room.Kind)
            {
                case RoomKind.Combat:
                case RoomKind.Elite:
                case RoomKind.Boss: Phase = RunPhase.Combat; break;
                case RoomKind.Sanctuary: Phase = RunPhase.Sanctuary; break;
                case RoomKind.Investigation: Phase = RunPhase.Investigation; break;
                case RoomKind.Secret: Phase = RunPhase.Secret; break;
                default:
                    CurrentEvent = nextEvent;
                    EventResult = null;
                    if (nextEvent != null) _eventsSeen.Add(nextEvent.Id);
                    Phase = nextEvent != null ? RunPhase.Event : RunPhase.Unwritten;
                    break;
            }
            return RunResult.Success;
        }

        RunResult StartFight(MapRoom room, IReadOnlyList<string> enemies, bool boss)
        {
            var combat = new CombatState(_content, Party, _deck, enemies, _seedRng.Stream("combat " + room.Id), _health,
                boss ? BossModifiers() : FightModifiers());
            CombatResult start = combat.Start();
            if (!start.Ok) return RunResult.Refused(start.Reason);
            Combat = combat;
            return RunResult.Success;
        }

        /// <summary>
        /// After the fight is won or lost: carries the party's health back into the run, then offers a reward, wins
        /// the expedition after the boss, or ends it if the party fell. Downed heroes stand back up with a quarter of their health.
        /// </summary>
        public RunResult TryFinishCombat()
        {
            if (Phase != RunPhase.Combat) return RunResult.Refused("There is no fight to finish.");
            if (!Combat.IsOver) return RunResult.Refused("The fight isn't over yet.");

            if (Combat.Phase == CombatPhase.Lost)
            {
                foreach (Combatant hero in Combat.Heroes) _health[hero.Id] = 0;
                Phase = RunPhase.Lost;
                return RunResult.Success;
            }

            int heal = KeepsakeTotal(KeepsakeEffectKind.HealAfterWin);
            foreach (Combatant hero in Combat.Heroes)
                _health[hero.Id] = hero.IsDown ? StandUpHealth(hero.Id) : Math.Min(MaxHealth(hero.Id), hero.Health + heal);
            FightsWon++;
            if (CurrentRoom.Kind == RoomKind.Boss)
            {
                Phase = RunPhase.Won;
                return RunResult.Success;
            }

            // An Elite gives a Keepsake, and can carry a clue, such as the villain's true name.
            RewardClue = null;
            RewardKeepsake = null;
            if (CurrentRoom.Kind == RoomKind.Elite) RewardKeepsake = GainKeepsake(null);
            if (CurrentRoom.Kind == RoomKind.Elite && Villain != null)
            {
                ClueDef clue = Villain.Clues.FirstOrDefault(c => (c.Sources & ClueSource.Elite) != 0 && !_clues.Contains(c.Id));
                if (clue != null)
                {
                    _clues.Add(clue.Id);
                    RewardClue = clue.Id;
                }
            }

            OfferCards();
            return RunResult.Success;
        }

        void OfferCards()
        {
            // Only cards someone in the party can play.
            List<string> pool = Expedition.RewardCards.Where(id => _content.Card(id).Owners.Any(Party.Contains)).ToList();
            _rewardRng.Shuffle(pool);
            _offer = pool.Take(RewardChoices).ToList();
            Phase = RunPhase.Reward;
        }

        /// <summary>
        /// Adds a Keepsake to the party: <paramref name="id"/>, or a random one they don't have when it is null.
        /// Returns its id, or null when there is none left to give.
        /// </summary>
        string GainKeepsake(string id)
        {
            if (id == null)
            {
                List<string> left = Expedition.Keepsakes.Select(k => k.Id).Where(k => !_keepsakes.Contains(k)).ToList();
                if (left.Count == 0) return null;
                id = left[_rewardRng.Next(left.Count)];
            }
            if (_keepsakes.Contains(id)) return null;
            _keepsakes.Add(id);
            KeepsakeDef keepsake = Expedition.Keepsake(id);
            if (keepsake.Kind == KeepsakeEffectKind.MaxHealth)
                foreach (string hero in Party) RaiseMaxHealth(hero, keepsake.Amount);
            return id;
        }

        void RaiseMaxHealth(string heroId, int amount)
        {
            _maxHealthBonus[heroId] = (_maxHealthBonus.TryGetValue(heroId, out int bonus) ? bonus : 0) + amount;
            _health[heroId] = Math.Min(MaxHealth(heroId), _health[heroId] + amount);
        }

        /// <summary>
        /// Event: takes choice <paramref name="choice"/> (an index into the event's choices). A choice that affects
        /// one hero needs <paramref name="heroId"/>. Its results apply at once; a card choice, card removal or fight
        /// follows as its own phase.
        /// </summary>
        public RunResult TryChooseEvent(int choice, string heroId = null)
        {
            if (Phase != RunPhase.Event) return RunResult.Refused("There's no event here.");
            if (choice < 0 || choice >= CurrentEvent.Choices.Count) return RunResult.Refused("There is no such choice.");
            EventChoice picked = CurrentEvent.Choices[choice];
            if (!IsChoiceShown(picked)) return RunResult.Refused("That choice isn't open to you.");
            if (picked.NeedsAHero)
            {
                if (heroId == null || !Party.Contains(heroId)) return RunResult.Refused("Choose a hero from your party.");
                if (_health[heroId] <= picked.HeroCost)
                    return RunResult.Refused($"{_content.Hero(heroId).Name} is too hurt for that: it costs {picked.HeroCost} health.");
            }

            var lines = new List<string> { picked.Result };
            EventOutcome step = null;
            foreach (EventOutcome o in picked.Outcomes)
            {
                switch (o.Kind)
                {
                    case EventOutcomeKind.HealParty:
                        foreach (string h in Party) _health[h] = Math.Min(MaxHealth(h), _health[h] + o.Amount);
                        break;
                    case EventOutcomeKind.HurtParty:
                        foreach (string h in Party) _health[h] = Math.Max(1, _health[h] - o.Amount);
                        break;
                    case EventOutcomeKind.HealHero:
                        _health[heroId] = Math.Min(MaxHealth(heroId), _health[heroId] + o.Amount);
                        break;
                    case EventOutcomeKind.HurtHero:
                        _health[heroId] -= o.Amount;
                        break;
                    case EventOutcomeKind.MaxHealthHero:
                        RaiseMaxHealth(heroId, o.Amount);
                        break;
                    case EventOutcomeKind.GainClue:
                        if (!_clues.Contains(o.Id))
                        {
                            _clues.Add(o.Id);
                            lines.Add($"Clue found: {Villain.Clue(o.Id).Name}. {Villain.Clue(o.Id).Journal}");
                        }
                        break;
                    case EventOutcomeKind.GainKeepsake:
                        string keepsake = GainKeepsake(o.Id);
                        if (keepsake != null) lines.Add($"Keepsake found: {Expedition.Keepsake(keepsake).Name}. {Expedition.Keepsake(keepsake).Text}");
                        break;
                    default:
                        step = o;
                        break;
                }
            }
            EventResult = string.Join("\n\n", lines.Where(l => !string.IsNullOrEmpty(l)));

            if (step == null) Phase = RunPhase.ChooseRoom;
            else if (step.Kind == EventOutcomeKind.GainCard) OfferCards();
            else if (step.Kind == EventOutcomeKind.RemoveCard) Phase = RunPhase.RemoveCard;
            else
            {
                EncounterDef encounter = step.Id != null ? Expedition.Encounter(step.Id) : PickNormalEncounter(CurrentRoom.Layer);
                RunResult started = StartFight(CurrentRoom, encounter.EnemyIds, boss: false);
                if (!started.Ok) return started;
                Phase = RunPhase.Combat;
            }
            return RunResult.Success;
        }

        EncounterDef PickNormalEncounter(int layer)
        {
            List<EncounterDef> fits = Expedition.Encounters.Where(e => e.Kind == EncounterKind.Normal && e.FitsLayer(layer)).ToList();
            return fits[_rewardRng.Next(fits.Count)];
        }

        /// <summary>Adds one of the offered cards to the deck, or skips the reward when <paramref name="cardId"/> is null.</summary>
        public RunResult TryTakeReward(string cardId)
        {
            if (Phase != RunPhase.Reward) return RunResult.Refused("There is no reward to take.");
            if (cardId != null && !_offer.Contains(cardId)) return RunResult.Refused("That card isn't on offer.");
            if (cardId != null) _deck.Add(new CardInstance(_nextCardId++, cardId));
            _offer = new List<string>();
            RewardClue = null;
            RewardKeepsake = null;
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>The clue in the Investigation room the party is in, or null if it holds none or it was already found.</summary>
        public ClueDef ClueHere =>
            Phase == RunPhase.Investigation && CurrentRoom.ClueId != null && !_clues.Contains(CurrentRoom.ClueId) ? Villain.Clue(CurrentRoom.ClueId) : null;

        /// <summary>Investigation: <paramref name="heroId"/> pays the search's health cost and the party finds the clue.</summary>
        public RunResult TrySearch(string heroId)
        {
            if (Phase != RunPhase.Investigation) return RunResult.Refused("There's nothing to search here.");
            ClueDef clue = ClueHere;
            if (clue == null) return RunResult.Refused("There's nothing more to find in this room.");
            if (!Party.Contains(heroId)) return RunResult.Refused("Choose a hero from your party.");
            string name = _content.Hero(heroId).Name;
            if (_health[heroId] <= clue.SearchCost) return RunResult.Refused($"{name} is too hurt to search: it costs {clue.SearchCost} health.");
            _health[heroId] -= clue.SearchCost;
            _clues.Add(clue.Id);
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>Secret room: every hero pays its health cost (none drops below 1) and the boss is weakened.</summary>
        public RunResult TryUseSecret()
        {
            if (Phase != RunPhase.Secret) return RunResult.Refused("There's nothing to do here.");
            foreach (string hero in Party) _health[hero] = Math.Max(1, _health[hero] - Villain.SecretRoom.HealthCostPerHero);
            SecretUsed = true;
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>Who the boss fight is against, once the clues found so far are counted.</summary>
        internal IReadOnlyList<string> BossEnemies()
        {
            IReadOnlyList<string> all = Expedition.Boss.EnemyIds;
            return Villain != null && HasEffect(ClueEffectKind.RemoveBossAllies) ? all.Where(id => id == Villain.BossEnemyId).ToList() : all;
        }

        /// <summary>For tests: the party has found this clue, however they came by it.</summary>
        internal void GiveClue(string clueId)
        {
            if (Villain?.Clue(clueId) == null) throw new ArgumentException($"No clue '{clueId}'.");
            if (!_clues.Contains(clueId)) _clues.Add(clueId);
        }

        /// <summary>For tests: sets a hero's health between rooms.</summary>
        internal void SetHealthForTest(string heroId, int health) => _health[heroId] = health;

        /// <summary>What the run brings to an ordinary fight: its Keepsakes and the heroes' extra maximum health.</summary>
        internal CombatModifiers FightModifiers() =>
            new CombatModifiers(keepsakes: _keepsakes.Select(Expedition.Keepsake), maxHealthBonus: _maxHealthBonus);

        /// <summary>For tests: the party has this Keepsake.</summary>
        internal void GiveKeepsake(string id) => GainKeepsake(id);

        /// <summary>What the clues found so far change in the boss fight, as well as what every fight gets.</summary>
        internal CombatModifiers BossModifiers()
        {
            if (Villain == null) return FightModifiers();
            string boss = Villain.BossEnemyId;
            bool known = HasEffect(ClueEffectKind.RevealBoss);
            int bonus = Effects(ClueEffectKind.BonusDamageToBoss).Sum(e => e.Amount);
            ClueEffect cancel = Effects(ClueEffectKind.CancelBossIntent).FirstOrDefault();
            return new CombatModifiers(
                names: known ? null : new Dictionary<string, string> { { boss, Villain.HiddenName } },
                vagueIntents: known ? null : new[] { boss },
                bonusDamageTo: bonus == 0 ? null : new Dictionary<string, int> { { boss, bonus } },
                healthLossPercent: SecretUsed ? new Dictionary<string, int> { { boss, Villain.SecretRoom.BossHealthLossPercent } } : null,
                cancel: cancel == null ? null : new CancelAbility(cancel.Label, boss, cancel.CancelKind),
                keepsakes: _keepsakes.Select(Expedition.Keepsake),
                maxHealthBonus: _maxHealthBonus);
        }

        /// <summary>Sanctuary: every hero heals 30% of their maximum health.</summary>
        public RunResult TryRest()
        {
            if (Phase != RunPhase.Sanctuary) return RunResult.Refused("You can only rest in a Sanctuary.");
            foreach (string hero in Party) _health[hero] = Math.Min(MaxHealth(hero), _health[hero] + RestHealAmount(hero));
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        public int StandUpHealth(string heroId) => Math.Max(1, MaxHealth(heroId) * StandUpPercent / 100);

        public int RestHealAmount(string heroId) => MaxHealth(heroId) * RestHealPercent / 100;

        /// <summary>Sanctuary, or an event that asks for it: remove one card from the deck for the rest of the run.</summary>
        public RunResult TryReflect(int cardInstanceId)
        {
            if (Phase != RunPhase.Sanctuary && Phase != RunPhase.RemoveCard) return RunResult.Refused("You can only reflect in a Sanctuary.");
            CardInstance card = _deck.FirstOrDefault(c => c.Id == cardInstanceId);
            if (card == null) return RunResult.Refused("That card isn't in the deck.");
            if (_deck.Count == 1) return RunResult.Refused("The deck needs at least one card.");
            _deck.Remove(card);
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>Leaves an Investigation, secret or Event room without doing anything there.</summary>
        public RunResult TryLeaveRoom()
        {
            if (Phase != RunPhase.Unwritten && Phase != RunPhase.Investigation && Phase != RunPhase.Secret) return RunResult.Refused("There's nothing to leave.");
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }
    }

    public sealed class RunResult
    {
        public readonly bool Ok;
        /// <summary>Why the command was refused, written for the player. Null when it succeeded.</summary>
        public readonly string Reason;

        RunResult(bool ok, string reason)
        {
            Ok = ok;
            Reason = reason;
        }

        internal static readonly RunResult Success = new RunResult(true, null);
        internal static RunResult Refused(string reason) => new RunResult(false, reason);
    }
}

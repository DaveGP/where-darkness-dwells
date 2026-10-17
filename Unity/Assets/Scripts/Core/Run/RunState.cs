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
        /// <summary>An Investigation or Event room, which later milestones fill in. The party just carries on.</summary>
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
        /// <summary>Resting heals each hero this share of their maximum health, rounded down.</summary>
        public const int RestHealPercent = 30;

        readonly ContentLibrary _content;
        readonly Rng _seedRng;
        readonly Rng _rewardRng;
        readonly List<CardInstance> _deck;
        readonly Dictionary<string, int> _health;
        readonly List<int> _route = new List<int>();
        List<string> _offer = new List<string>();
        int _nextCardId;

        public RunState(ContentLibrary content, ExpeditionDef expedition, ulong seed)
        {
            _content = content ?? throw new ArgumentNullException(nameof(content));
            Expedition = expedition ?? throw new ArgumentNullException(nameof(expedition));
            IReadOnlyList<string> problems = content.Validate(expedition);
            if (problems.Count > 0) throw new ArgumentException("The expedition can't be used: " + string.Join(" ", problems), nameof(expedition));

            Seed = seed;
            _seedRng = new Rng(seed);
            _rewardRng = _seedRng.Stream("rewards");
            Map = MapGenerator.Generate(expedition, _seedRng.Stream("map"));
            _deck = expedition.StartingDeck.Select((id, i) => new CardInstance(i + 1, id)).ToList();
            _nextCardId = _deck.Count + 1;
            _health = expedition.Party.ToDictionary(id => id, id => content.Hero(id).MaxHealth);
        }

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

        public IReadOnlyList<string> Party => Expedition.Party;
        public IReadOnlyList<CardInstance> Deck => _deck;
        public int Health(string heroId) => _health.TryGetValue(heroId, out int h) ? h : 0;
        public int MaxHealth(string heroId) => _content.Hero(heroId).MaxHealth;
        public CardDef CardOf(CardInstance card) => _content.Card(card.DefId);

        public bool IsOver => Phase == RunPhase.Won || Phase == RunPhase.Lost;
        public int FightsWon { get; private set; }

        /// <summary>The rooms the party can enter next: the first layer at the start, then the exits of the current room.</summary>
        public IEnumerable<MapRoom> ReachableRooms =>
            Phase != RunPhase.ChooseRoom ? Enumerable.Empty<MapRoom>()
            : CurrentRoom == null ? Map.Layer(0)
            : Map.NextOf(CurrentRoom);

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
                var combat = new CombatState(_content, Party, _deck, encounter.EnemyIds, _seedRng.Stream("combat " + room.Id), _health);
                CombatResult start = combat.Start();
                if (!start.Ok) return RunResult.Refused(start.Reason);
                Combat = combat;
            }

            CurrentRoom = room;
            _route.Add(room.Id);
            switch (room.Kind)
            {
                case RoomKind.Combat:
                case RoomKind.Elite:
                case RoomKind.Boss: Phase = RunPhase.Combat; break;
                case RoomKind.Sanctuary: Phase = RunPhase.Sanctuary; break;
                default: Phase = RunPhase.Unwritten; break;
            }
            return RunResult.Success;
        }

        /// <summary>
        /// After the fight is won or lost: carries the party's health back into the run, then offers a reward, wins
        /// the expedition after the boss, or ends it if the party fell. Downed heroes stand back up with 1 health.
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

            foreach (Combatant hero in Combat.Heroes) _health[hero.Id] = Math.Max(1, hero.Health);
            FightsWon++;
            if (CurrentRoom.Kind == RoomKind.Boss)
            {
                Phase = RunPhase.Won;
                return RunResult.Success;
            }

            List<string> pool = Expedition.RewardCards.ToList();
            _rewardRng.Shuffle(pool);
            _offer = pool.Take(RewardChoices).ToList();
            Phase = RunPhase.Reward;
            return RunResult.Success;
        }

        /// <summary>Adds one of the offered cards to the deck, or skips the reward when <paramref name="cardId"/> is null.</summary>
        public RunResult TryTakeReward(string cardId)
        {
            if (Phase != RunPhase.Reward) return RunResult.Refused("There is no reward to take.");
            if (cardId != null && !_offer.Contains(cardId)) return RunResult.Refused("That card isn't on offer.");
            if (cardId != null) _deck.Add(new CardInstance(_nextCardId++, cardId));
            _offer = new List<string>();
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>Sanctuary: every hero heals 30% of their maximum health.</summary>
        public RunResult TryRest()
        {
            if (Phase != RunPhase.Sanctuary) return RunResult.Refused("You can only rest in a Sanctuary.");
            foreach (string hero in Party) _health[hero] = Math.Min(MaxHealth(hero), _health[hero] + RestHealAmount(hero));
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        public int RestHealAmount(string heroId) => MaxHealth(heroId) * RestHealPercent / 100;

        /// <summary>Sanctuary: remove one card from the deck for the rest of the run.</summary>
        public RunResult TryReflect(int cardInstanceId)
        {
            if (Phase != RunPhase.Sanctuary) return RunResult.Refused("You can only reflect in a Sanctuary.");
            CardInstance card = _deck.FirstOrDefault(c => c.Id == cardInstanceId);
            if (card == null) return RunResult.Refused("That card isn't in the deck.");
            if (_deck.Count == 1) return RunResult.Refused("The deck needs at least one card.");
            _deck.Remove(card);
            Phase = RunPhase.ChooseRoom;
            return RunResult.Success;
        }

        /// <summary>Leaves an Investigation or Event room, which has nothing in it until a later milestone.</summary>
        public RunResult TryLeaveRoom()
        {
            if (Phase != RunPhase.Unwritten) return RunResult.Refused("There's nothing to leave.");
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

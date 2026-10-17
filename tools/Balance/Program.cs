using System;
using System.Collections.Generic;
using System.Linq;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Balance
{
    /// <summary>
    /// Plays the game many times with two scripted players and prints how it went: "careful" looks one enemy phase
    /// ahead before every choice, and "careless" plays at random.
    ///
    /// From the repository root: dotnet run -c Release --project tools/Balance -- [fight|run] [seeds]
    ///   fight: the first encounter only. A good fight is one the careful player usually wins with some damage taken.
    ///   run:   whole expeditions through the map to Vael.
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            string mode = args.Length > 0 ? args[0] : "run";
            int seeds = args.Length > 1 ? int.Parse(args[1]) : mode == "fight" ? 300 : 100;
            ContentLibrary content = StarterContent.Library();
            if (mode == "fight")
            {
                FightReport("careful", seeds, seed => Fight.Play(content, seed, Careful.Choose));
                FightReport("careless", seeds, seed => Fight.Play(content, seed, Careless.Choose));
            }
            else
            {
                RunReport("careful", seeds, seed => Expedition.Play(content, seed, Careful.Choose, Careful.ChooseRoom, Careful.ChooseReward, Careful.AtSanctuary));
                RunReport("careless", seeds, seed => Expedition.Play(content, seed, Careless.Choose, Careless.ChooseRoom, Careless.ChooseReward, Careless.AtSanctuary));
            }
        }

        static void FightReport(string name, int seeds, Func<int, Fight.Result> play)
        {
            List<Fight.Result> results = Enumerable.Range(1, seeds).Select(play).ToList();
            List<Fight.Result> wins = results.Where(r => r.Won).ToList();
            Console.WriteLine($"{name,-9} wins {100.0 * wins.Count / seeds,5:0.0}%" +
                $"  rounds {results.Average(r => r.Rounds),4:0.0}" +
                $"  party health left on a win {(wins.Count == 0 ? 0 : wins.Average(r => r.HealthLeftPercent)),3:0}%" +
                $"  heroes down on a win {(wins.Count == 0 ? 0 : wins.Average(r => r.HeroesDown)),3:0.00}" +
                $"  close wins (under 25% health) {100.0 * wins.Count(r => r.HealthLeftPercent < 25) / seeds,4:0.0}%");

            var activations = results.SelectMany(r => r.Final.Log.Where(e => e.Kind == CombatEventKind.HeroActivated).Select(e => e.SourceId))
                .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            int total = activations.Values.Sum();
            Console.WriteLine("          activations: " + string.Join(", ", activations.OrderByDescending(a => a.Value).Select(a => $"{a.Key} {100.0 * a.Value / total:0}%")));
            Console.WriteLine("          plays per fight: " + string.Join(", ", PlaysPerFight(results.Select(r => r.Final), seeds)
                .Where(p => StarterContent.StarterCards().Any(c => c.Id == p.Key)).Select(p => $"{p.Key} {p.Value:0.0}")));
        }

        static void RunReport(string name, int seeds, Func<int, Expedition.Result> play)
        {
            List<Expedition.Result> runs = Enumerable.Range(1, seeds).Select(play).ToList();
            int won = runs.Count(r => r.Won);
            int reachedBoss = runs.Count(r => r.ReachedBoss);
            Console.WriteLine($"{name,-9} wins {100.0 * won / seeds,5:0.0}%  reach Vael {100.0 * reachedBoss / seeds,5:0.0}%" +
                $"  fights won {runs.Average(r => r.FightsWon),4:0.0}  rounds per fight {runs.SelectMany(r => r.Fights).Average(f => f.Round),4:0.0}");

            var bossHealth = runs.Where(r => r.ReachedBoss).Select(r => r.HealthBeforeBossPercent).ToList();
            if (bossHealth.Count > 0) Console.WriteLine($"          party health entering Vael {bossHealth.Average(),3:0}%  (lowest {bossHealth.Min()}%)");

            var lostAt = runs.Where(r => !r.Won).GroupBy(r => r.EndedIn).OrderBy(g => g.Key)
                .Select(g => $"{g.Key} {100.0 * g.Count() / seeds:0}%");
            Console.WriteLine("          losses: " + string.Join(", ", lostAt));

            var kinds = runs.SelectMany(r => r.FightKinds).GroupBy(k => k.kind)
                .Select(g => $"{g.Key} {100.0 * g.Count(k => k.won) / g.Count():0}% of {g.Count()}");
            Console.WriteLine("          fights won by room: " + string.Join(", ", kinds));

            Console.WriteLine("          by encounter (won, health lost): " + string.Join(", ", runs.SelectMany(r => r.Encounters).GroupBy(e => e.encounter)
                .Select(g => $"{g.Key} {100.0 * g.Count(e => e.won) / g.Count():0}% {g.Average(e => e.lostPercent):0}% ({g.Count()})")));

            // Open question 3: what happens to a hero who stood back up with 1 health.
            int stoodUp = runs.Sum(r => r.StoodUp), downedAgain = runs.Sum(r => r.DownedAgainNextFight);
            Console.WriteLine($"          heroes stood up with 1 health {stoodUp / (double)seeds:0.0} a run; down again in the next fight {(stoodUp == 0 ? 0 : 100.0 * downedAgain / stoodUp):0}% of the time");

            var taken = runs.SelectMany(r => r.CardsTaken).GroupBy(c => c).ToDictionary(g => g.Key, g => g.Count());
            Console.WriteLine("          reward cards taken: " + string.Join(", ", StarterContent.RewardCards().Select(c => $"{c.Id} {(taken.TryGetValue(c.Id, out int n) ? n : 0)}")));
            Console.WriteLine("          plays per fight (rewards): " + string.Join(", ", PlaysPerFight(runs.SelectMany(r => r.Fights), runs.Sum(r => r.Fights.Count))
                .Where(p => StarterContent.RewardCards().Any(c => c.Id == p.Key)).Select(p => $"{p.Key} {p.Value:0.00}")));
        }

        static Dictionary<string, double> PlaysPerFight(IEnumerable<CombatState> fights, int count) =>
            fights.SelectMany(f => f.Log.Where(e => e.Kind == CombatEventKind.CardPlayed).Select(e => f.CardOf(f.FindCard(e.CardInstanceId)).Id))
                .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count() / (double)count);
    }

    /// <summary>One player command in a fight.</summary>
    public sealed class Command
    {
        public readonly string ActivateHero;
        public readonly int Card;
        public readonly string Target;
        public readonly bool EndActivation;

        Command(string activateHero, int card, string target, bool end)
        {
            ActivateHero = activateHero;
            Card = card;
            Target = target;
            EndActivation = end;
        }

        public static Command Activate(string heroId) => new Command(heroId, 0, null, false);
        public static Command Play(int card, string target) => new Command(null, card, target, false);
        public static readonly Command End = new Command(null, 0, null, true);

        public CombatResult ApplyTo(CombatState combat) =>
            ActivateHero != null ? combat.TryActivate(ActivateHero)
            : EndActivation ? combat.TryEndActivation()
            : combat.TryPlay(Card, Target);
    }

    /// <summary>A player in a fight: given the fight now, the next command.</summary>
    public delegate Command Policy(CombatState now, Random random);

    public static class Fight
    {
        public sealed class Result
        {
            public bool Won;
            public int Rounds;
            public int HealthLeftPercent;
            public int HeroesDown;
            public CombatState Final;
        }

        public static Result Play(ContentLibrary content, int seed, Policy policy)
        {
            var combat = new CombatState(content, StarterContent.Party, StarterContent.StartingDeck(),
                StarterContent.FirstEncounter, new Rng((ulong)seed).Stream("combat"));
            combat.Start();
            PlayOut(combat, policy, new Random(seed));
            return new Result
            {
                Won = combat.Phase == CombatPhase.Won,
                Rounds = combat.Round,
                HealthLeftPercent = 100 * combat.Heroes.Sum(h => h.Health) / combat.Heroes.Sum(h => h.MaxHealth),
                HeroesDown = combat.Heroes.Count(h => h.IsDown),
                Final = combat,
            };
        }

        public static void PlayOut(CombatState combat, Policy policy, Random random)
        {
            for (int step = 0; step < 3000 && !combat.IsOver; step++)
                if (!policy(combat, random).ApplyTo(combat).Ok) throw new InvalidOperationException("The player chose a refused command.");
            if (!combat.IsOver) throw new InvalidOperationException("A fight never finished.");
        }

        /// <summary>Every card play the rules would accept right now, with each valid target.</summary>
        public static IEnumerable<Command> LegalPlays(CombatState combat)
        {
            foreach (CardInstance card in combat.Hand)
            {
                if (!combat.CanPlay(card.Id)) continue;
                CardDef def = combat.CardOf(card);
                if (def.Target == CardTarget.None) yield return Command.Play(card.Id, null);
                else
                {
                    IEnumerable<Combatant> targets = def.Target == CardTarget.Enemy ? combat.Enemies : combat.Heroes;
                    foreach (Combatant t in targets.Where(t => !t.IsDown))
                    {
                        if (t.Id == combat.ActiveHeroId && def.Effects.Any(e => e.Kind == EffectKind.Guard)) continue;
                        yield return Command.Play(card.Id, t.Id);
                    }
                }
            }
        }
    }

    public static class Expedition
    {
        public sealed class Result
        {
            public bool Won;
            public bool ReachedBoss;
            public int FightsWon;
            public string EndedIn;
            public int HealthBeforeBossPercent;
            public int StoodUp;
            public int DownedAgainNextFight;
            public List<CombatState> Fights = new List<CombatState>();
            public List<(RoomKind kind, bool won)> FightKinds = new List<(RoomKind, bool)>();
            /// <summary>Each fight's encounter and the share of the party's maximum health it cost.</summary>
            public List<(string encounter, int lostPercent, bool won)> Encounters = new List<(string, int, bool)>();
            public List<string> CardsTaken = new List<string>();
        }

        public static Result Play(ContentLibrary content, int seed, Policy fight, Func<RunState, Random, MapRoom> room,
            Func<RunState, Random, string> reward, Action<RunState, Random> sanctuary)
        {
            var run = new RunState(content, StarterContent.Expedition(), (ulong)seed);
            var random = new Random(seed);
            var result = new Result();
            var stoodUp = new HashSet<string>();
            for (int step = 0; step < 200 && !run.IsOver; step++)
            {
                switch (run.Phase)
                {
                    case RunPhase.ChooseRoom:
                        MapRoom next = room(run, random);
                        if (next.Kind == RoomKind.Boss)
                        {
                            result.ReachedBoss = true;
                            result.HealthBeforeBossPercent = 100 * run.Party.Sum(run.Health) / run.Party.Sum(run.MaxHealth);
                        }
                        Must(run.TryEnterRoom(next.Id));
                        break;
                    case RunPhase.Combat:
                        int before = run.Party.Sum(run.Health);
                        Fight.PlayOut(run.Combat, fight, random);
                        result.Encounters.Add((run.CurrentRoom.EncounterId, 100 * (before - run.Combat.Heroes.Sum(h => h.Health)) / run.Party.Sum(run.MaxHealth), run.Combat.Phase == CombatPhase.Won));
                        result.Fights.Add(run.Combat);
                        result.FightKinds.Add((run.CurrentRoom.Kind, run.Combat.Phase == CombatPhase.Won));
                        result.DownedAgainNextFight += run.Combat.Heroes.Count(h => h.IsDown && stoodUp.Contains(h.Id));
                        stoodUp.Clear();
                        foreach (Combatant h in run.Combat.Heroes.Where(h => h.IsDown)) stoodUp.Add(h.Id);
                        Must(run.TryFinishCombat());
                        if (run.Phase != RunPhase.Lost) result.StoodUp += stoodUp.Count;
                        break;
                    case RunPhase.Reward:
                        string card = reward(run, random);
                        if (card != null) result.CardsTaken.Add(card);
                        Must(run.TryTakeReward(card));
                        break;
                    case RunPhase.Sanctuary:
                        sanctuary(run, random);
                        break;
                    case RunPhase.Unwritten:
                        Must(run.TryLeaveRoom());
                        break;
                }
            }
            if (!run.IsOver) throw new InvalidOperationException($"Seed {seed} never finished.");
            result.Won = run.Phase == RunPhase.Won;
            result.FightsWon = run.FightsWon;
            result.EndedIn = run.Phase == RunPhase.Won ? "won" : run.CurrentRoom.Kind == RoomKind.Boss ? "Vael" : $"layer {run.CurrentRoom.Layer + 1} {run.CurrentRoom.Kind}";
            return result;
        }

        static void Must(RunResult r)
        {
            if (!r.Ok) throw new InvalidOperationException("The run refused a command: " + r.Reason);
        }

        public static int HealthPercent(RunState run) => 100 * run.Party.Sum(run.Health) / run.Party.Sum(run.MaxHealth);
    }

    /// <summary>
    /// Tries each choice on a copy of the fight, finishes the turn greedily, plays the enemy phase that follows, and
    /// keeps the choice that leaves the party best off. Between rooms it rests when hurt and avoids Elites when weak.
    /// </summary>
    public static class Careful
    {
        public static Command Choose(CombatState now, Random random)
        {
            IEnumerable<Command> options = now.Phase == CombatPhase.ChooseHero
                ? now.Heroes.Where(h => !h.IsDown).Select(h => Command.Activate(h.Id))
                : Fight.LegalPlays(now).Append(Command.End);

            Command best = null;
            double bestScore = double.MinValue;
            foreach (Command option in options)
            {
                CombatState after = now.Clone();
                option.ApplyTo(after);
                // Finish the turn greedily so a choice is judged by what it leads to, not by the bare choice.
                if (option.ActivateHero != null) FinishActivation(after);
                else if (!option.EndActivation && !after.IsOver) Command.End.ApplyTo(after);
                double score = Score(after);
                if (score > bestScore) { bestScore = score; best = option; }
            }
            return best;
        }

        static void FinishActivation(CombatState combat)
        {
            for (int i = 0; i < 12 && !combat.IsOver && combat.Phase == CombatPhase.Activation; i++)
            {
                Command next = Choose(combat, null);
                next.ApplyTo(combat);
                if (next.EndActivation) break;
            }
        }

        /// <summary>How good a position is for the party. Hex and Bleed count for the damage they will do later.</summary>
        public static double Score(CombatState c)
        {
            if (c.Phase == CombatPhase.Won) return 10000 + c.Heroes.Sum(h => h.Health) * 10;
            if (c.Phase == CombatPhase.Lost) return -10000 + c.Round;
            double score = 0;
            foreach (Combatant h in c.Heroes) score += h.IsDown ? -40 : h.Health * 1.5 + Math.Min(h.Block, 12) * 0.3;
            foreach (Combatant e in c.Enemies.Where(e => !e.IsDown))
            {
                int bleed = e.Status(StatusKind.Bleed);
                score -= e.Health * 1.2 + 15;
                score += bleed * (bleed + 1) / 2 * 1.0 + e.Status(StatusKind.Hex) * 2.5 + e.Status(StatusKind.Exposed) * 2 + e.Status(StatusKind.Weak) * 2;
            }
            return score;
        }

        public static MapRoom ChooseRoom(RunState run, Random random)
        {
            int health = Expedition.HealthPercent(run);
            double Value(MapRoom r)
            {
                switch (r.Kind)
                {
                    case RoomKind.Sanctuary: return health < 65 ? 3 : 0.5;
                    case RoomKind.Elite: return health > 75 ? 2 : -2;
                    case RoomKind.Combat: return 1;
                    default: return 0;
                }
            }
            return run.ReachableRooms.OrderByDescending(Value).ThenBy(r => r.Column).First();
        }

        /// <summary>Takes the offered card for the hero with the fewest cards in the deck, so every hero stays useful.</summary>
        public static string ChooseReward(RunState run, Random random) =>
            run.RewardOffer.OrderBy(id => Library.Card(id).Owners.Min(owner => run.Deck.Count(c => run.CardOf(c).IsOwnedBy(owner)))).First();

        static readonly ContentLibrary Library = StarterContent.Library();

        public static void AtSanctuary(RunState run, Random random)
        {
            int missing = run.Party.Sum(h => run.MaxHealth(h) - run.Health(h));
            CardInstance weakest = run.Deck.FirstOrDefault(c => c.DefId == "stand_guard");
            if (missing < 15 && weakest != null) run.TryReflect(weakest.Id);
            else run.TryRest();
        }
    }

    /// <summary>Plays random cards on random targets, picks random rooms and cards, and always rests.</summary>
    public static class Careless
    {
        public static Command Choose(CombatState now, Random random)
        {
            if (now.Phase == CombatPhase.ChooseHero)
            {
                List<Combatant> standing = now.Heroes.Where(h => !h.IsDown).ToList();
                return Command.Activate(standing[random.Next(standing.Count)].Id);
            }
            List<Command> plays = Fight.LegalPlays(now).ToList();
            if (plays.Count == 0 || random.NextDouble() < 0.15) return Command.End;
            return plays[random.Next(plays.Count)];
        }

        public static MapRoom ChooseRoom(RunState run, Random random)
        {
            List<MapRoom> rooms = run.ReachableRooms.ToList();
            return rooms[random.Next(rooms.Count)];
        }

        public static string ChooseReward(RunState run, Random random) => run.RewardOffer[random.Next(run.RewardOffer.Count)];

        public static void AtSanctuary(RunState run, Random random) => run.TryRest();
    }
}

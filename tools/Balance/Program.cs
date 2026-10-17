using System;
using System.Collections.Generic;
using System.Linq;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Balance
{
    /// <summary>
    /// Plays the first encounter many times and prints how it went for two scripted players:
    /// "careful" looks one enemy phase ahead before every choice, and "careless" plays at random.
    /// A good fight is one the careful player usually wins with some damage taken, and the careless one often loses.
    ///
    /// From the repository root: dotnet run -c Release --project tools/Balance [-- seeds]
    /// </summary>
    public static class Program
    {
        public static void Main(string[] args)
        {
            int seeds = args.Length > 0 ? int.Parse(args[0]) : 300;
            ContentLibrary content = StarterContent.Library();
            Report("careful", seeds, seed => Fight.Play(content, seed, Careful.Choose));
            Report("careless", seeds, seed => Fight.Play(content, seed, Careless.Choose));
        }

        static void Report(string name, int seeds, Func<int, Fight.Result> play)
        {
            List<Fight.Result> results = Enumerable.Range(1, seeds).Select(play).ToList();
            List<Fight.Result> wins = results.Where(r => r.Won).ToList();
            Console.WriteLine($"{name,-9} wins {100.0 * wins.Count / seeds,5:0.0}%" +
                $"  rounds {results.Average(r => r.Rounds),4:0.0}" +
                $"  party health left on a win {(wins.Count == 0 ? 0 : wins.Average(r => r.HealthLeftPercent)),3:0}%" +
                $"  heroes down on a win {(wins.Count == 0 ? 0 : wins.Average(r => r.HeroesDown)),3:0.00}" +
                $"  close wins (under 25% health) {100.0 * wins.Count(r => r.HealthLeftPercent < 25) / seeds,4:0.0}%");

            var activations = results.SelectMany(r => r.Commands.Where(c => c.ActivateHero != null).Select(c => c.ActivateHero))
                .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count());
            int total = activations.Values.Sum();
            Console.WriteLine("          activations: " + string.Join(", ", activations.OrderByDescending(a => a.Value).Select(a => $"{a.Key} {100.0 * a.Value / total:0}%")));

            // How often each card is played when it's in hand during its owner's activation is hard to get from the log;
            // plays per fight is a fair first look.
            var plays = results.SelectMany(r => r.Final.Log.Where(e => e.Kind == CombatEventKind.CardPlayed)
                    .Select(e => r.Final.CardOf(r.Final.FindCard(e.CardInstanceId)).Id))
                .GroupBy(id => id).ToDictionary(g => g.Key, g => g.Count() / (double)seeds);
            Console.WriteLine("          plays per fight: " + string.Join(", ", StarterContent.Cards().Select(c => $"{c.Id} {(plays.TryGetValue(c.Id, out double n) ? n : 0):0.0}")));
        }
    }

    /// <summary>One player command, so a fight can be replayed from its seed to try different choices.</summary>
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

    public static class Fight
    {
        public sealed class Result
        {
            public bool Won;
            public int Rounds;
            public int HealthLeftPercent;
            public int HeroesDown;
            public List<Command> Commands;
            public CombatState Final;
        }

        /// <summary>A player: given the fight so far, the next command.</summary>
        public delegate Command Policy(ContentLibrary content, int seed, List<Command> history, CombatState now, Random random);

        public static CombatState Replay(ContentLibrary content, int seed, IEnumerable<Command> commands)
        {
            var combat = new CombatState(content, StarterContent.Party, StarterContent.StartingDeck(),
                StarterContent.FirstEncounter, new Rng((ulong)seed).Stream("combat"));
            combat.Start();
            foreach (Command c in commands)
                if (!c.ApplyTo(combat).Ok) throw new InvalidOperationException("A replayed command was refused.");
            return combat;
        }

        public static Result Play(ContentLibrary content, int seed, Policy policy)
        {
            var history = new List<Command>();
            CombatState combat = Replay(content, seed, history);
            var random = new Random(seed);
            for (int step = 0; step < 2000 && !combat.IsOver; step++)
            {
                Command next = policy(content, seed, history, combat, random);
                if (!next.ApplyTo(combat).Ok) throw new InvalidOperationException("The player chose a refused command.");
                history.Add(next);
            }
            if (!combat.IsOver) throw new InvalidOperationException($"Seed {seed} never finished.");

            return new Result
            {
                Won = combat.Phase == CombatPhase.Won,
                Rounds = combat.Round,
                HealthLeftPercent = 100 * combat.Heroes.Sum(h => h.Health) / combat.Heroes.Sum(h => h.MaxHealth),
                HeroesDown = combat.Heroes.Count(h => h.IsDown),
                Commands = history,
                Final = combat,
            };
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

    /// <summary>
    /// Tries each choice, plays the enemy phase that follows, and keeps the choice that leaves the party best off.
    /// Not a perfect player, but much better than a careless one.
    /// </summary>
    public static class Careful
    {
        public static Command Choose(ContentLibrary content, int seed, List<Command> history, CombatState now, Random random)
        {
            IEnumerable<Command> options = now.Phase == CombatPhase.ChooseHero
                ? now.Heroes.Where(h => !h.IsDown).Select(h => Command.Activate(h.Id))
                : Fight.LegalPlays(now).Append(Command.End);

            Command best = null;
            double bestScore = double.MinValue;
            foreach (Command option in options)
            {
                var tried = new List<Command>(history) { option };
                CombatState after = Fight.Replay(content, seed, tried);
                // Finish the turn greedily so an activation is judged by what it leads to, not by the bare choice.
                if (option.ActivateHero != null) FinishActivation(content, seed, tried, ref after);
                else if (!option.EndActivation && !after.IsOver) { tried.Add(Command.End); after = Fight.Replay(content, seed, tried); }
                double score = Score(after);
                if (score > bestScore) { bestScore = score; best = option; }
            }
            return best;
        }

        static void FinishActivation(ContentLibrary content, int seed, List<Command> tried, ref CombatState after)
        {
            for (int i = 0; i < 12 && !after.IsOver && after.Phase == CombatPhase.Activation; i++)
            {
                CombatState current = after;
                Command next = Choose(content, seed, tried, current, null);
                tried.Add(next);
                after = Fight.Replay(content, seed, tried);
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
    }

    /// <summary>Activates a random hero and plays random cards on random targets, sometimes stopping early.</summary>
    public static class Careless
    {
        public static Command Choose(ContentLibrary content, int seed, List<Command> history, CombatState now, Random random)
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
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class PreviewTests
    {
        static CombatState StartingFight(int seed)
        {
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, StarterContent.StartingDeck(),
                StarterContent.FirstEncounter, new Rng((ulong)seed));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static string Describe(CombatEvent e) => $"{e.Kind} {e.SourceId} {e.TargetId} {e.Amount} {e.Blocked} {e.Status}";

        [Test]
        public void ACloneCarriesOnIndependentlyAndIdentically()
        {
            CombatState original = StartingFight(5);
            int logBefore = original.Log.Count;
            CombatState a = original.Clone();
            CombatState b = original.Clone();

            foreach (CombatState copy in new[] { a, b })
            {
                Assert.That(copy.TryActivate(StarterContent.Wren).Ok, Is.True);
                Assert.That(copy.TryEndActivation().Ok, Is.True);
            }

            Assert.That(original.Log.Count, Is.EqualTo(logBefore));
            Assert.That(original.Phase, Is.EqualTo(CombatPhase.ChooseHero));
            Assert.That(a.Log.Select(Describe), Is.EqualTo(b.Log.Select(Describe)));
            Assert.That(a.Hand.Select(c => c.Id), Is.EqualTo(b.Hand.Select(c => c.Id)), "The copies shuffle and draw the same way.");
        }

        [Test]
        public void ThePreviewIsWhatTheEnemiesThenDo([Range(1, 40)] int seed)
        {
            CombatState combat = StartingFight(seed);
            for (int turn = 0; turn < 30 && !combat.IsOver; turn++)
            {
                Combatant hero = combat.Heroes.First(h => !h.IsDown);
                combat.TryActivate(hero.Id);
                foreach (CardInstance card in combat.Hand.ToList())
                {
                    CardDef def = combat.CardOf(card);
                    string target = def.Target == CardTarget.Enemy ? combat.Enemies.FirstOrDefault(e => !e.IsDown)?.Id
                        : def.Target == CardTarget.Ally ? combat.Heroes.FirstOrDefault(h => !h.IsDown && h.Id != hero.Id)?.Id
                        : null;
                    combat.TryPlay(card.Id, target);
                }
                if (combat.IsOver) break;

                List<string> preview = combat.PreviewEnemyPhase().Select(Describe).ToList();
                int logBefore = combat.Log.Count;
                List<string> actual = combat.TryEndActivation().Events
                    .SkipWhile(e => e.Kind == CombatEventKind.ActivationEnded || e.Kind == CombatEventKind.ResolveChanged)
                    .Take(preview.Count).Select(Describe).ToList();

                Assert.That(actual, Is.EqualTo(preview), $"Seed {seed}, round {combat.Round}");
                Assert.That(combat.Log.Count, Is.GreaterThan(logBefore));
            }
        }

        [Test]
        public void ThePreviewChangesNothing()
        {
            CombatState combat = StartingFight(3);
            string before = string.Join("|", combat.Heroes.Concat(combat.Enemies).Select(c => $"{c.Health},{c.Block},{c.Statuses.Count}"));
            int log = combat.Log.Count;
            combat.PreviewEnemyPhase();
            string after = string.Join("|", combat.Heroes.Concat(combat.Enemies).Select(c => $"{c.Health},{c.Block},{c.Statuses.Count}"));
            Assert.That(after, Is.EqualTo(before));
            Assert.That(combat.Log.Count, Is.EqualTo(log));
        }

        [Test]
        public void ThePreviewCountsBlockAndGuard()
        {
            var deck = new[] { "shield_wall", "stand_guard" }.Select((id, i) => new CardInstance(i + 1, id)).ToList();
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, deck, new[] { "hollow_sentry" }, new Rng(7));
            combat.Start();
            Combatant sentry = combat.Enemies[0];
            sentry.Intent = IntentDef.Attack(10, IntentTarget.RandomHero, "Test attack");
            sentry.IntentTargetId = StarterContent.Wren;

            combat.TryActivate(StarterContent.Bram);
            combat.TryPlay(2, StarterContent.Wren); // Stand Guard: Bram takes Wren's hits and gains 4 Block.

            CombatEvent hit = combat.PreviewEnemyPhase().Single(e => e.Kind == CombatEventKind.Damaged);
            Assert.That(hit.TargetId, Is.EqualTo(StarterContent.Bram));
            Assert.That(hit.Amount, Is.EqualTo(6));
            Assert.That(hit.Blocked, Is.EqualTo(4));
        }
    }
}

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class CombatLogTextTests
    {
        static CombatState Fight(params string[] cards)
        {
            var deck = cards.Select((id, i) => new CardInstance(i + 1, id)).ToList();
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, deck, new[] { "hollow_sentry" }, new Rng(7));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static List<string> Lines(CombatResult result, CombatState combat)
        {
            Assert.That(result.Ok, Is.True, result.Reason);
            return result.Events.Select(e => CombatLogText.Describe(e, combat)).Where(line => line != null).ToList();
        }

        [Test]
        public void APlayedCardIsDescribedWithNamesNotIds()
        {
            CombatState combat = Fight("quick_shot");
            combat.TryActivate(StarterContent.Wren);
            List<string> lines = Lines(combat.TryPlay(1, "hollow_sentry#0"), combat);
            Assert.That(lines, Is.EqualTo(new[] { "Wren plays Quick Shot on Hollow Sentry.", "Wren hits Hollow Sentry for 4." }));
        }

        [Test]
        public void TheEnemyPhaseNamesTheActionThatWasUsedNotTheNextOne()
        {
            CombatState combat = Fight("quick_shot");
            Combatant sentry = combat.Enemies[0];
            string target = sentry.IntentTargetId;
            combat.TryActivate(StarterContent.Wren);
            List<string> lines = Lines(combat.TryEndActivation(), combat);

            Assert.That(lines, Has.Member("Hollow Sentry uses Strike 6."));
            Assert.That(lines, Has.Member("Hollow Sentry readies Raise shield 8."));
            Assert.That(lines.Any(l => l.StartsWith("Hollow Sentry hits " + combat.Find(target).Name)), Is.True);
        }

        [Test]
        public void EveryEventInManyFightsCanBeDescribed()
        {
            foreach (int seed in Enumerable.Range(1, 20))
            {
                var combat = new CombatState(StarterContent.Library(), StarterContent.Party, StarterContent.StartingDeck(),
                    StarterContent.FirstEncounter, new Rng((ulong)seed));
                combat.Start();
                for (int turn = 0; turn < 60 && !combat.IsOver; turn++)
                {
                    Combatant hero = combat.Heroes.First(h => !h.IsDown);
                    combat.TryActivate(hero.Id);
                    foreach (CardInstance card in combat.Hand.ToList())
                    {
                        CardDef def = combat.CardOf(card);
                        string targetId = def.Target == CardTarget.Enemy ? combat.Enemies.FirstOrDefault(e => !e.IsDown)?.Id
                            : def.Target == CardTarget.Ally ? combat.Heroes.FirstOrDefault(h => !h.IsDown && h.Id != hero.Id)?.Id
                            : null;
                        combat.TryPlay(card.Id, targetId);
                    }
                    combat.TryEndActivation();
                }

                foreach (CombatEvent e in combat.Log)
                {
                    string line = CombatLogText.Describe(e, combat);
                    if (line == null) continue;
                    Assert.That(line, Does.Not.Contain("#"), e.ToString());
                    Assert.That(line, Does.Not.Contain("_"), e.ToString());
                }
            }
        }
    }
}

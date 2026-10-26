using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class CombatTests
    {
        const string Bram = StarterContent.Bram, Wren = StarterContent.Wren, Ashe = StarterContent.Ashe;

        /// <summary>A started fight whose deck is just the named cards, so with six or fewer they're all in hand.</summary>
        static CombatState Fight(string[] cards, params string[] enemies)
        {
            if (enemies.Length == 0) enemies = new[] { "hollow_sentry" };
            var deck = cards.Select((id, i) => new CardInstance(i + 1, id)).ToList();
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, deck, enemies, new Rng(7));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static int CardId(CombatState combat, string defId) => combat.Hand.Concat(combat.DrawPile).Concat(combat.DiscardPile).First(c => c.DefId == defId).Id;

        static Combatant Hero(CombatState combat, string id) => combat.Heroes.First(h => h.Id == id);

        static Combatant Enemy(CombatState combat, int index = 0) => combat.Enemies[index];

        /// <summary>Makes the enemy's shown action a plain attack on one hero, so a test knows exactly what comes next.</summary>
        static void SetAttack(Combatant enemy, int amount, string heroId)
        {
            enemy.Intent = IntentDef.Attack(amount, IntentTarget.RandomHero, "Test attack");
            enemy.IntentTargetId = heroId;
        }

        static void Ok(CombatResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        // ---------- content ----------

        [Test]
        public void StarterContentIsValid()
        {
            Assert.That(StarterContent.Library().Validate(), Is.Empty);
        }

        [Test]
        public void ValidationCatchesBrokenContent()
        {
            var library = new ContentLibrary(
                StarterContent.Heroes(),
                new[]
                {
                    new CardDef("a", "A", new[] { "nobody" }, 1, CardTarget.Enemy, "", EffectDef.Damage(1)),
                    new CardDef("a", "A again", new[] { Bram }, 1, CardTarget.Enemy, "", EffectDef.Damage(1)),
                    new CardDef("b", "B", new[] { Bram }, 1, CardTarget.Ally, "", EffectDef.Damage(1)),
                    new CardDef("c", "C", new[] { Bram, Wren, Ashe }, 1, CardTarget.None, "", EffectDef.GainResolve(1)),
                },
                new[] { new EnemyDef("e", "E", 10) });

            IReadOnlyList<string> problems = library.Validate();
            Assert.That(problems, Has.Some.Contains("unknown hero 'nobody'"));
            Assert.That(problems, Has.Some.Contains("share the id 'a'"));
            Assert.That(problems, Has.Some.Contains("'b' deals damage to its target"));
            Assert.That(problems, Has.Some.Contains("one or two owners"));
            Assert.That(problems, Has.Some.Contains("no actions"));
        }

        [Test]
        public void StartingDeckHasFourCardsPerHeroAndThreeDualCards()
        {
            ContentLibrary library = StarterContent.Library();
            List<CardDef> deck = StarterContent.StartingDeck().Select(c => library.Card(c.DefId)).ToList();
            Assert.That(deck, Has.Count.EqualTo(15));
            foreach (string hero in StarterContent.Party)
                Assert.That(deck.Count(c => !c.IsDual && c.IsOwnedBy(hero)), Is.EqualTo(4), hero);
            Assert.That(deck.Count(c => c.IsDual), Is.EqualTo(3));
        }

        // ---------- turns and eligibility ----------

        [Test]
        public void StartDealsAHandAndShowsEveryIntent()
        {
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, StarterContent.StartingDeck(),
                StarterContent.FirstEncounter, new Rng(1));
            Ok(combat.Start());

            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.ChooseHero));
            Assert.That(combat.Hand, Has.Count.EqualTo(CombatState.HandSize));
            Assert.That(combat.DrawPile, Has.Count.EqualTo(15 - CombatState.HandSize));
            foreach (Combatant enemy in combat.Enemies)
            {
                Assert.That(enemy.Intent, Is.Not.Null);
                if (enemy.Intent.Target == IntentTarget.RandomHero || enemy.Intent.Target == IntentTarget.HealthiestHero)
                    Assert.That(combat.Heroes.Select(h => h.Id), Does.Contain(enemy.IntentTargetId));
            }
        }

        [Test]
        public void CardsNeedAnActiveOwner()
        {
            CombatState combat = Fight(new[] { "shield_bash", "quick_shot" });
            int bash = CardId(combat, "shield_bash");

            Assert.That(combat.WhyCantPlay(bash), Is.EqualTo("Activate Bram to play Shield Bash."));
            Ok(combat.TryActivate(Wren));
            Assert.That(combat.WhyCantPlay(bash), Is.EqualTo("Only Bram can play Shield Bash."));
            Assert.That(combat.CanPlay(CardId(combat, "quick_shot")), Is.True);
        }

        [Test]
        public void DualCardsWorkForEitherOwnerOnly()
        {
            CombatState combat = Fight(new[] { "pin_down" });
            int pin = CardId(combat, "pin_down");

            Ok(combat.TryActivate(Ashe));
            Assert.That(combat.WhyCantPlay(pin), Is.EqualTo("Only Bram or Wren can play Pin Down."));
            Ok(combat.TryEndActivation());

            Ok(combat.TryActivate(Bram));
            Assert.That(combat.CanPlay(pin), Is.True);
            Ok(combat.TryEndActivation());

            Ok(combat.TryActivate(Wren));
            Assert.That(combat.CanPlay(pin), Is.True);
        }

        [Test]
        public void CardsAreFreeSoTheActiveHeroCanPlayAllTheirCards()
        {
            CombatState combat = Fight(new[] { "exploit", "volley", "quick_shot", "barbed_arrow" });
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryPlay(CardId(combat, "exploit"), Enemy(combat).Id));
            Ok(combat.TryPlay(CardId(combat, "volley")));
            Ok(combat.TryPlay(CardId(combat, "quick_shot"), Enemy(combat).Id));
            Ok(combat.TryPlay(CardId(combat, "barbed_arrow"), Enemy(combat).Id));
            Assert.That(combat.CardsPlayed, Is.EqualTo(4));
        }

        [Test]
        public void EveryCardIsFreeAndNothingGainsResolve()
        {
            foreach (CardDef card in StarterContent.Cards())
            {
                Assert.That(card.Cost, Is.Zero, card.Id);
                Assert.That(card.Effects.Any(e => e.Kind == EffectKind.GainResolve), Is.False, card.Id);
            }
        }

        [Test]
        public void RefusedCommandsChangeNothing()
        {
            CombatState combat = Fight(new[] { "quick_shot", "shield_wall" });
            Ok(combat.TryActivate(Wren));
            int logLength = combat.Log.Count, health = Enemy(combat).Health;

            Assert.That(combat.TryPlay(CardId(combat, "shield_wall"), Wren).Ok, Is.False); // Bram's card
            Assert.That(combat.TryPlay(CardId(combat, "quick_shot"), Bram).Ok, Is.False);  // a hero isn't an enemy
            Assert.That(combat.TryPlay(CardId(combat, "quick_shot")).Ok, Is.False);        // no target
            Assert.That(combat.TryPlay(999, Enemy(combat).Id).Ok, Is.False);              // not in hand
            Assert.That(combat.TryActivate(Bram).Ok, Is.False);                           // already activating

            Assert.That(combat.Hand, Has.Count.EqualTo(2));
            Assert.That(combat.CardsPlayed, Is.EqualTo(0));
            Assert.That(Enemy(combat).Health, Is.EqualTo(health));
            Assert.That(combat.Log, Has.Count.EqualTo(logLength));
        }

        [Test]
        public void UnplayedCardsStayInHandAndTheHandRefillsToSix()
        {
            CombatState combat = Fight(new[] { "quick_shot", "volley", "exploit", "hex_bolt", "wither", "unravel", "shield_wall", "stagger" });
            List<int> before = combat.Hand.Select(c => c.Id).ToList();
            CardInstance shot = combat.Hand.FirstOrDefault(c => c.DefId == "quick_shot" || c.DefId == "volley");
            Assert.That(shot, Is.Not.Null);

            Ok(combat.TryActivate(Wren));
            Ok(combat.TryPlay(shot.Id, shot.DefId == "quick_shot" ? Enemy(combat).Id : null));
            Ok(combat.TryEndActivation());

            Assert.That(combat.Hand, Has.Count.EqualTo(CombatState.HandSize));
            foreach (int id in before.Where(id => id != shot.Id)) Assert.That(combat.Hand.Select(c => c.Id), Does.Contain(id));
            Assert.That(combat.Hand.Select(c => c.Id), Has.No.Member(shot.Id));
        }

        [Test]
        public void DarkInsightDrawsTwoCards()
        {
            CombatState combat = Fight(new[] { "dark_insight", "hex_bolt", "wither", "unravel", "quick_shot", "volley", "exploit" });
            Assert.That(combat.Hand, Has.Count.EqualTo(6));
            int insight = combat.Hand.Concat(combat.DrawPile).First(c => c.DefId == "dark_insight").Id;
            if (combat.Hand.All(c => c.Id != insight)) Assert.Ignore("Seed 7 left Dark Insight in the draw pile.");

            Ok(combat.TryActivate(Ashe));
            Ok(combat.TryPlay(insight));
            Assert.That(combat.Hand, Has.Count.EqualTo(6)); // played one, and only one card was left to draw
            Assert.That(combat.DrawPile, Is.Empty);
        }

        // ---------- damage and statuses ----------

        [TestCase(10, false, false, 10)]
        [TestCase(10, true, false, 7)]   // 7.5 rounds down
        [TestCase(10, false, true, 15)]
        [TestCase(10, true, true, 11)]   // 10 x 0.75 x 1.5 = 11.25, rounded once at the end
        [TestCase(1, true, false, 0)]
        [TestCase(0, false, true, 0)]
        public void DamageAppliesWeakThenExposedAndRoundsDown(int baseDamage, bool weak, bool exposed, int expected)
        {
            Assert.That(DamageRules.Attack(baseDamage, weak, exposed), Is.EqualTo(expected));
        }

        [Test]
        public void BlockAbsorbsDamageBeforeHealth()
        {
            CombatState combat = Fight(new[] { "quick_shot" });
            Combatant sentry = Enemy(combat);
            sentry.Block = 2;
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryPlay(CardId(combat, "quick_shot"), sentry.Id));

            Assert.That(sentry.Block, Is.EqualTo(0));
            Assert.That(sentry.Health, Is.EqualTo(sentry.MaxHealth - 2));
            CombatEvent hit = combat.Log.Last(e => e.Kind == CombatEventKind.Damaged);
            Assert.That(hit.Amount, Is.EqualTo(2));
            Assert.That(hit.Blocked, Is.EqualTo(2));
        }

        [Test]
        public void PinDownSetsUpExploitInOneActivation()
        {
            CombatState combat = Fight(new[] { "pin_down", "exploit" });
            Combatant sentry = Enemy(combat);
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryPlay(CardId(combat, "pin_down"), sentry.Id));
            Assert.That(sentry.Status(StatusKind.Exposed), Is.EqualTo(1));
            Ok(combat.TryPlay(CardId(combat, "exploit"), sentry.Id));

            // Pin Down 3, then Exploit (3 + 3 for Exposed) x 1.5 = 9.
            Assert.That(sentry.Health, Is.EqualTo(sentry.MaxHealth - 3 - 9));
        }

        [Test]
        public void ShieldBashExposureLastsIntoTheNextTurn()
        {
            CombatState combat = Fight(new[] { "shield_bash" });
            Combatant sentry = Enemy(combat);
            Ok(combat.TryActivate(Bram));
            Ok(combat.TryPlay(CardId(combat, "shield_bash"), sentry.Id));
            Assert.That(sentry.Status(StatusKind.Exposed), Is.EqualTo(2));
            Ok(combat.TryEndActivation());
            Assert.That(sentry.Status(StatusKind.Exposed), Is.EqualTo(1), "one stack left for Wren's turn");
        }

        [Test]
        public void GuardedHeroesHaveAttacksRedirectedToTheGuardian()
        {
            CombatState combat = Fight(new[] { "stand_guard" });
            Ok(combat.TryActivate(Bram));
            Ok(combat.TryPlay(CardId(combat, "stand_guard"), Wren));
            Assert.That(Hero(combat, Wren).GuardedBy, Is.EqualTo(Bram));
            Assert.That(Hero(combat, Bram).Block, Is.EqualTo(2));

            SetAttack(Enemy(combat), 6, Wren);
            Ok(combat.TryEndActivation());

            Assert.That(Hero(combat, Wren).Health, Is.EqualTo(Hero(combat, Wren).MaxHealth));
            Assert.That(Hero(combat, Bram).Health, Is.EqualTo(Hero(combat, Bram).MaxHealth - 4)); // 6 minus 2 Block
            Assert.That(combat.Log.Any(e => e.Kind == CombatEventKind.Redirected && e.SourceId == Bram && e.TargetId == Wren));
            Assert.That(Hero(combat, Wren).GuardedBy, Is.Null, "guarding ends at the next player turn");
        }

        [Test]
        public void AHeroCannotGuardThemselves()
        {
            CombatState combat = Fight(new[] { "stand_guard" });
            Ok(combat.TryActivate(Bram));
            CombatResult result = combat.TryPlay(CardId(combat, "stand_guard"), Bram);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Reason, Does.Contain("can't guard themselves"));
        }

        [Test]
        public void BleedHurtsAtTheEndOfTheRoundThenFades()
        {
            CombatState combat = Fight(new[] { "barbed_arrow" });
            Combatant sentry = Enemy(combat);
            sentry.IntentStep = 1; // Raising its shield must not stop Bleed.
            sentry.Intent = sentry.Enemy.Intents[1];
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryPlay(CardId(combat, "barbed_arrow"), sentry.Id));
            Ok(combat.TryEndActivation());

            Assert.That(sentry.Health, Is.EqualTo(sentry.MaxHealth - 2 - 2), "2 from the arrow, 2 Bleed through Block");
            Assert.That(sentry.Status(StatusKind.Bleed), Is.EqualTo(1));
        }

        [Test]
        public void WeakFadesOneStackPerRound()
        {
            CombatState combat = Fight(new[] { "stagger" });
            Combatant sentry = Enemy(combat);
            Ok(combat.TryActivate(Bram));
            Ok(combat.TryPlay(CardId(combat, "stagger"), sentry.Id));
            SetAttack(sentry, 8, Ashe);
            Ok(combat.TryEndActivation());

            Assert.That(Hero(combat, Ashe).Health, Is.EqualTo(Hero(combat, Ashe).MaxHealth - 6), "Weak: 8 x 0.75");
            Assert.That(sentry.Status(StatusKind.Weak), Is.EqualTo(1));
        }

        [Test]
        public void UnravelConsumesHexForDamage()
        {
            CombatState combat = Fight(new[] { "wither", "unravel" });
            Combatant sentry = Enemy(combat);
            Ok(combat.TryActivate(Ashe));
            Ok(combat.TryPlay(CardId(combat, "wither"), sentry.Id));
            Ok(combat.TryPlay(CardId(combat, "unravel"), sentry.Id));

            Assert.That(sentry.Status(StatusKind.Hex), Is.EqualTo(0));
            Assert.That(sentry.Health, Is.EqualTo(sentry.MaxHealth - 4)); // 2 stacks x 2
        }

        [Test]
        public void WardingSigilWeakensEveryEnemy()
        {
            CombatState combat = Fight(new[] { "warding_sigil" }, StarterContent.FirstEncounter);
            Ok(combat.TryActivate(Ashe));
            Ok(combat.TryPlay(CardId(combat, "warding_sigil"), Wren));
            Assert.That(Hero(combat, Wren).Block, Is.EqualTo(3));
            Assert.That(combat.Enemies.All(e => e.Status(StatusKind.Weak) == 1));
        }

        // ---------- falling, winning and losing ----------

        [Test]
        public void ADownedHerosCardsAreSetAsideButDualCardsStay()
        {
            CombatState combat = Fight(new[] { "quick_shot", "volley", "pin_down", "shield_wall" });
            Combatant wren = Hero(combat, Wren);
            wren.Health = 3;
            SetAttack(Enemy(combat), 6, Wren);
            Ok(combat.TryActivate(Bram));
            Ok(combat.TryEndActivation());

            Assert.That(wren.IsDown, Is.True);
            Assert.That(combat.SetAside.Select(c => c.DefId), Is.EquivalentTo(new[] { "quick_shot", "volley" }));
            Assert.That(combat.Hand.Select(c => c.DefId), Does.Contain("pin_down"));
            Assert.That(combat.TryActivate(Wren).Reason, Is.EqualTo("Wren is down and can't act."));
            Ok(combat.TryActivate(Bram));
            Assert.That(combat.CanPlay(CardId(combat, "pin_down")), Is.True);
        }

        [Test]
        public void AttacksAimedAtADownedHeroTurnOnTheHealthiest()
        {
            CombatState combat = Fight(new[] { "quick_shot" }, "hollow_sentry", "chapel_wretch");
            Hero(combat, Wren).Health = 2;
            SetAttack(Enemy(combat, 0), 6, Wren);
            SetAttack(Enemy(combat, 1), 5, Wren);
            Ok(combat.TryActivate(Ashe));
            Ok(combat.TryEndActivation());

            Assert.That(Hero(combat, Wren).IsDown, Is.True);
            Assert.That(Hero(combat, Bram).Health, Is.EqualTo(Hero(combat, Bram).MaxHealth - 5));
        }

        [Test]
        public void DefeatingEveryEnemyWinsAndEndsTheFight()
        {
            CombatState combat = Fight(new[] { "quick_shot", "volley" });
            Enemy(combat).Health = 4;
            Ok(combat.TryActivate(Wren));
            CombatResult result = combat.TryPlay(CardId(combat, "quick_shot"), Enemy(combat).Id);

            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.Won));
            Assert.That(result.Events.Select(e => e.Kind), Does.Contain(CombatEventKind.EnemyDefeated));
            Assert.That(result.Events.Last().Kind, Is.EqualTo(CombatEventKind.CombatWon));
            Assert.That(combat.TryPlay(CardId(combat, "volley")).Reason, Is.EqualTo("The fight is over."));
            Assert.That(combat.TryEndActivation().Reason, Is.EqualTo("The fight is over."));
        }

        [Test]
        public void TheWholePartyFallingLosesTheFight()
        {
            CombatState combat = Fight(new[] { "quick_shot" }, "chapel_wretch");
            foreach (Combatant hero in combat.Heroes) hero.Health = 3;
            Combatant wretch = Enemy(combat);
            wretch.Intent = wretch.Enemy.Intents[1]; // Wail 4 to all
            wretch.IntentTargetId = null;
            Ok(combat.TryActivate(Bram));
            Ok(combat.TryEndActivation());

            Assert.That(combat.Phase, Is.EqualTo(CombatPhase.Lost));
            Assert.That(combat.Log.Last().Kind, Is.EqualTo(CombatEventKind.CombatLost));
            Assert.That(combat.TryActivate(Bram).Ok, Is.False);
        }

        // ---------- determinism ----------

        [Test]
        public void TheSameSeedAndChoicesGiveTheSameFight()
        {
            Assert.That(Autoplay(42), Is.EqualTo(Autoplay(42)));
            Assert.That(Autoplay(42), Is.Not.EqualTo(Autoplay(43)));
        }

        [Test]
        public void ManyAutoplayedFightsAlwaysFinish([Range(1, 200)] int seed)
        {
            Autoplay(seed);
        }

        /// <summary>
        /// Plays a whole fight with a simple fixed strategy and returns its log as text. It activates the hero
        /// with the most playable Resolve in hand and plays cards left to right.
        /// </summary>
        static string Autoplay(int seed)
        {
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, StarterContent.StartingDeck(),
                StarterContent.FirstEncounter, new Rng((ulong)seed));
            Ok(combat.Start());

            for (int turn = 0; turn < 100 && !combat.IsOver; turn++)
            {
                Combatant hero = combat.Heroes.Where(h => !h.IsDown)
                    .OrderByDescending(h => combat.Hand.Where(c => combat.CardOf(c).IsOwnedBy(h.Id)).Sum(c => combat.CardOf(c).Cost + 1))
                    .First();
                Ok(combat.TryActivate(hero.Id));

                bool played = true;
                while (played && !combat.IsOver)
                {
                    played = false;
                    foreach (CardInstance card in combat.Hand.ToList())
                    {
                        if (!combat.CanPlay(card.Id)) continue;
                        CardDef def = combat.CardOf(card);
                        string target = null;
                        if (def.Target == CardTarget.Enemy) target = combat.Enemies.First(e => !e.IsDown).Id;
                        if (def.Target == CardTarget.Ally)
                        {
                            bool guards = def.Effects.Any(e => e.Kind == EffectKind.Guard);
                            Combatant ally = combat.Heroes.Where(h => !h.IsDown && !(guards && h.Id == hero.Id)).OrderBy(h => h.Health).FirstOrDefault();
                            if (ally == null) continue;
                            target = ally.Id;
                        }
                        CombatResult result = combat.TryPlay(card.Id, target);
                        if (!result.Ok) continue;
                        played = true;
                        break;
                    }
                }
                if (!combat.IsOver) Ok(combat.TryEndActivation());
            }

            Assert.That(combat.IsOver, Is.True, $"Seed {seed} didn't finish within 100 turns.");
            return string.Join("\n", combat.Log);
        }
    }
}

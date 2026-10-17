using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class EventAndKeepsakeTests
    {
        static readonly ContentLibrary Library = StarterContent.Library();

        static void Ok(RunResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        static KeepsakeDef Keepsake(string id) => StarterContent.Keepsakes().Single(k => k.Id == id);

        /// <summary>The first fight with these Keepsakes, started.</summary>
        static CombatState Fight(params string[] keepsakes)
        {
            var combat = new CombatState(Library, StarterContent.Party, StarterContent.StartingDeck(), StarterContent.FirstEncounter,
                new Rng(3), modifiers: new CombatModifiers(keepsakes: keepsakes.Select(Keepsake)));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        /// <summary>
        /// A run whose only event is <paramref name="eventId"/>, standing in the first Event room on the map. Rooms on
        /// earlier layers hold no events, so it is still untold.
        /// </summary>
        static RunState AtEvent(string eventId, params string[] clues)
        {
            ExpeditionDef full = StarterContent.Expedition();
            var expedition = new ExpeditionDef(full.Id, full.Heroes, full.StartingDeck, full.RewardCards, full.Encounters, full.Villain,
                full.Keepsakes, full.Events.Where(e => e.Id == eventId));
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                var run = new RunState(Library, expedition, seed);
                foreach (string clue in clues) run.GiveClue(clue);
                MapRoom room = run.Map.Rooms.Where(r => r.Kind == RoomKind.Event).OrderBy(r => r.Layer).FirstOrDefault();
                if (room != null && RunStateTests.Walk(run, room) && run.Phase == RunPhase.Event) return run;
            }
            Assert.Fail($"No seed reached the {eventId} event.");
            return null;
        }

        static int Choice(RunState run, string label)
        {
            for (int i = 0; i < run.CurrentEvent.Choices.Count; i++)
                if (run.CurrentEvent.Choices[i].Label == label) return i;
            Assert.Fail($"{run.CurrentEvent} has no choice '{label}'.");
            return -1;
        }

        // ---------- Keepsakes in a fight ----------

        [Test]
        public void TheSiegeLanternGivesEveryHeroBlockAtTheStart()
        {
            Assert.That(Fight("siege_lantern").Heroes.Select(h => h.Block), Is.All.EqualTo(4));
            Assert.That(Fight().Heroes.Select(h => h.Block), Is.All.EqualTo(0));
        }

        [Test]
        public void CaltropsAndTheVeilMarkEveryEnemyAtTheStart()
        {
            CombatState combat = Fight("rusted_caltrops", "widows_veil");
            foreach (Combatant enemy in combat.Enemies)
            {
                Assert.That(enemy.Status(StatusKind.Bleed), Is.EqualTo(2));
                Assert.That(enemy.Status(StatusKind.Weak), Is.EqualTo(1));
            }
        }

        [Test]
        public void TheMarkedDeckDrawsTwoMoreOnTheFirstTurnOnly()
        {
            CombatState combat = Fight("marked_deck");
            Assert.That(combat.Hand.Count, Is.EqualTo(CombatState.HandSize + 2));
        }

        [Test]
        public void TheKeyringMakesOnlyTheFirstCardCheaper()
        {
            CombatState combat = Fight("gaolers_keyring");
            Assert.That(combat.TryActivate(StarterContent.Bram).Ok, Is.True);
            CardInstance first = combat.Hand.First(c => combat.CardOf(c).IsOwnedBy(StarterContent.Bram) && combat.CardOf(c).Cost > 0);
            Assert.That(combat.CostOf(first), Is.EqualTo(combat.CardOf(first).Cost - 1));
            string target = combat.CardOf(first).Target == CardTarget.Enemy ? combat.Enemies[0].Id
                : combat.CardOf(first).Target == CardTarget.Ally ? StarterContent.Wren : null;
            Assert.That(combat.TryPlay(first.Id, target).Ok, Is.True);
            Assert.That(combat.Resolve, Is.EqualTo(CombatState.ResolvePerActivation - combat.CardOf(first).Cost + 1));
            Assert.That(combat.Hand.Select(c => combat.CostOf(c)), Is.EqualTo(combat.Hand.Select(c => combat.CardOf(c).Cost)));
        }

        [Test]
        public void TheSealRingGivesTheFirstActivationOneMoreResolve()
        {
            CombatState combat = Fight("seal_ring");
            Assert.That(combat.TryActivate(StarterContent.Bram).Ok, Is.True);
            Assert.That(combat.Resolve, Is.EqualTo(CombatState.ResolvePerActivation + 1));
            Assert.That(combat.TryEndActivation().Ok, Is.True);
            Assert.That(combat.TryActivate(StarterContent.Wren).Ok, Is.True);
            Assert.That(combat.Resolve, Is.EqualTo(CombatState.ResolvePerActivation));
        }

        // ---------- Keepsakes in a run ----------

        [Test]
        public void TheBoneCharmRaisesMaximumHealthInTheRunAndItsFights()
        {
            var run = new RunState(Library, StarterContent.Expedition(), 4);
            int bram = run.MaxHealth(StarterContent.Bram);
            run.SetHealthForTest(StarterContent.Bram, 10);
            run.GiveKeepsake("bone_charm");
            Assert.That(run.MaxHealth(StarterContent.Bram), Is.EqualTo(bram + 4));
            Assert.That(run.Health(StarterContent.Bram), Is.EqualTo(14));
            Ok(run.TryEnterRoom(run.ReachableRooms.First().Id));
            Assert.That(run.Combat.Find(StarterContent.Bram).MaxHealth, Is.EqualTo(bram + 4));
        }

        [Test]
        public void TheCandleHealsStandingHeroesAfterAWin()
        {
            foreach (ulong seed in Enumerable.Range(1, 50).Select(s => (ulong)s))
            {
                var run = new RunState(Library, StarterContent.Expedition(), seed);
                run.GiveKeepsake("chapel_candle");
                Ok(run.TryEnterRoom(run.ReachableRooms.First().Id));
                RunStateTests.PlayFight(run.Combat);
                if (run.Combat.Phase != CombatPhase.Won) continue;
                Ok(run.TryFinishCombat());
                foreach (Combatant hero in run.Combat.Heroes)
                    Assert.That(run.Health(hero.Id), Is.EqualTo(hero.IsDown ? run.StandUpHealth(hero.Id) : System.Math.Min(hero.MaxHealth, hero.Health + 3)));
                return;
            }
            Assert.Fail("No seed won the first fight.");
        }

        [Test]
        public void AnEliteGivesAKeepsakeThePartyDoesntHave()
        {
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                var run = new RunState(Library, StarterContent.Expedition(), seed);
                foreach (KeepsakeDef k in StarterContent.Keepsakes().Skip(1)) run.GiveKeepsake(k.Id);
                MapRoom elite = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Elite);
                if (elite == null || !RunStateTests.Walk(run, elite)) continue;
                RunStateTests.PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase != RunPhase.Reward) continue;
                Assert.That(run.RewardKeepsake, Is.EqualTo(StarterContent.Keepsakes()[0].Id), "Only one Keepsake was left to give.");
                Assert.That(run.Keepsakes.Distinct().Count(), Is.EqualTo(run.Keepsakes.Count));
                return;
            }
            Assert.Fail("No seed won an Elite fight.");
        }

        // ---------- events ----------

        [Test]
        public void NoEventIsToldTwiceInARun()
        {
            foreach (ulong seed in Enumerable.Range(1, 40).Select(s => (ulong)s))
            {
                var run = new RunState(Library, StarterContent.Expedition(), seed);
                var told = new List<string>();
                for (int step = 0; step < 300 && !run.IsOver; step++)
                {
                    if (run.Phase == RunPhase.Event) told.Add(run.CurrentEvent.Id);
                    StepSimply(run);
                }
                Assert.That(told.Distinct().Count(), Is.EqualTo(told.Count), $"Seed {seed}: {string.Join(", ", told)}");
            }
        }

        /// <summary>One step of a plain run: first room, first event choice, leave everything else.</summary>
        static void StepSimply(RunState run)
        {
            switch (run.Phase)
            {
                case RunPhase.ChooseRoom: Ok(run.TryEnterRoom(run.ReachableRooms.First().Id)); break;
                case RunPhase.Combat:
                    RunStateTests.PlayFight(run.Combat);
                    Ok(run.TryFinishCombat());
                    break;
                case RunPhase.Reward: Ok(run.TryTakeReward(null)); break;
                case RunPhase.Sanctuary: Ok(run.TryRest()); break;
                case RunPhase.Event: RunStateTests.ChooseFirst(run); break;
                case RunPhase.RemoveCard: Ok(run.TryReflect(run.Deck[0].Id)); break;
                case RunPhase.Secret: Ok(run.TryUseSecret()); break;
                default: Ok(run.TryLeaveRoom()); break;
            }
        }

        [Test]
        public void AnEventRoomWithNoEventLeftIsEmpty()
        {
            ExpeditionDef full = StarterContent.Expedition();
            var expedition = new ExpeditionDef(full.Id, full.Heroes, full.StartingDeck, full.RewardCards, full.Encounters, full.Villain, full.Keepsakes);
            foreach (ulong seed in Enumerable.Range(1, 50).Select(s => (ulong)s))
            {
                var run = new RunState(Library, expedition, seed);
                MapRoom room = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Event);
                if (room == null || !RunStateTests.Walk(run, room)) continue;
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Unwritten));
                Ok(run.TryLeaveRoom());
                return;
            }
            Assert.Fail("No seed reached an Event room.");
        }

        [Test]
        public void LooseningTheChainsCostsTheChosenHeroAndGivesTheConfession()
        {
            RunState run = AtEvent("chained_thrall");
            Assert.That(run.CurrentEvent.Choices.Count(run.IsChoiceShown), Is.EqualTo(2), "Naming his master needs the Ledger.");
            int before = run.Health(StarterContent.Wren);
            Ok(run.TryChooseEvent(Choice(run, "Loosen his chains"), StarterContent.Wren));
            Assert.That(run.Health(StarterContent.Wren), Is.EqualTo(before - 3));
            Assert.That(run.CluesFound, Has.Member("thrall_confession"));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(run.EventResult, Does.Contain("The Thrall's Confession"));
        }

        [Test]
        public void WithTheLedgerTheThrallConfessesForFree()
        {
            RunState run = AtEvent("chained_thrall", "ledger");
            var health = run.Party.ToDictionary(h => h, run.Health);
            Ok(run.TryChooseEvent(Choice(run, "Name his master")));
            Assert.That(run.CluesFound, Has.Member("thrall_confession"));
            Assert.That(run.Party.Select(run.Health), Is.EqualTo(run.Party.Select(h => health[h])));
        }

        [Test]
        public void AChoiceThatIsntShownOrThatAHeroCantPayIsRefused()
        {
            RunState run = AtEvent("chained_thrall");
            RunResult hidden = run.TryChooseEvent(Choice(run, "Name his master"));
            Assert.That(hidden.Ok, Is.False);
            Assert.That(hidden.Reason, Is.EqualTo("That choice isn't open to you."));

            run.SetHealthForTest(StarterContent.Wren, 3);
            RunResult hurt = run.TryChooseEvent(Choice(run, "Loosen his chains"), StarterContent.Wren);
            Assert.That(hurt.Reason, Is.EqualTo("Wren is too hurt for that: it costs 3 health."));
            Assert.That(run.TryChooseEvent(Choice(run, "Loosen his chains")).Reason, Is.EqualTo("Choose a hero from your party."));
            Assert.That(run.Health(StarterContent.Wren), Is.EqualTo(3));
            Assert.That(run.CluesFound, Has.No.Member("thrall_confession"));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Event));
        }

        [Test]
        public void TradingWithTheWidowRemovesACardAndGivesTheBells()
        {
            RunState run = AtEvent("bell_widow");
            int deck = run.Deck.Count;
            Ok(run.TryChooseEvent(Choice(run, "Trade her something from the deck")));
            Assert.That(run.CluesFound, Has.Member("silvered_bells"));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.RemoveCard));
            Ok(run.TryReflect(run.Deck[0].Id));
            Assert.That(run.Deck.Count, Is.EqualTo(deck - 1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
        }

        [Test]
        public void TheWellsBucketGivesAKeepsakeAndAFight()
        {
            RunState run = AtEvent("drowned_well");
            Ok(run.TryChooseEvent(Choice(run, "Draw up the bucket")));
            Assert.That(run.Keepsakes.Count, Is.EqualTo(1));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Combat));
            Assert.That(run.Combat.Enemies.Select(e => e.Enemy.Id), Is.EqualTo(new[] { "drowned_retainer", "hollow_sentry" }));
            Assert.That(run.Combat.Modifiers.Keepsakes.Select(k => k.Id), Is.EqualTo(run.Keepsakes));
        }

        [Test]
        public void TheQuartermastersShelfOffersCards()
        {
            RunState run = AtEvent("quartermaster");
            Ok(run.TryChooseEvent(Choice(run, "Take from the shelf")));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Reward));
            Assert.That(run.RewardOffer.Count, Is.EqualTo(RunState.RewardChoices));
        }

        [Test]
        public void DiggingTheGravesHurtsEveryoneButLeavesThemStanding()
        {
            RunState run = AtEvent("siege_graves");
            run.SetHealthForTest(StarterContent.Ashe, 2);
            int wren = run.Health(StarterContent.Wren);
            Ok(run.TryChooseEvent(Choice(run, "Dig")));
            Assert.That(run.Health(StarterContent.Ashe), Is.EqualTo(1));
            Assert.That(run.Health(StarterContent.Wren), Is.EqualTo(wren - 3));
            Assert.That(run.Keepsakes.Count, Is.EqualTo(1));
        }

        [Test]
        public void TheMirrorRaisesOneHerosMaximumHealth()
        {
            RunState run = AtEvent("mirror_hall");
            int max = run.MaxHealth(StarterContent.Ashe), wren = run.MaxHealth(StarterContent.Wren);
            Ok(run.TryChooseEvent(Choice(run, "Face the mirror"), StarterContent.Ashe));
            Assert.That(run.MaxHealth(StarterContent.Ashe), Is.EqualTo(max + 4));
            Assert.That(run.MaxHealth(StarterContent.Wren), Is.EqualTo(wren));
        }

        [Test]
        public void EveryClueCanBeFoundSomewhere()
        {
            ExpeditionDef expedition = StarterContent.Expedition();
            foreach (ClueDef clue in expedition.Villain.Clues)
            {
                bool inEvents = expedition.Events.Any(e => e.Choices.Any(c => c.Outcomes.Any(o => o.Kind == EventOutcomeKind.GainClue && o.Id == clue.Id)));
                bool found = (clue.Sources & (ClueSource.Investigation | ClueSource.Elite)) != 0 || inEvents;
                Assert.That(found, Is.True, $"Nothing gives {clue}.");
                if ((clue.Sources & ClueSource.Event) != 0) Assert.That(inEvents, Is.True, $"{clue} says it comes from events, but none gives it.");
            }
        }

        [Test]
        public void ABrokenEventIsRefusedWithItsProblems()
        {
            ExpeditionDef full = StarterContent.Expedition();
            var broken = new EventDef("broken", "Broken", "Nothing here.",
                new EventChoice("Too much", "", outcomes: new[] { new EventOutcome(EventOutcomeKind.GainCard), new EventOutcome(EventOutcomeKind.Fight) }),
                new EventChoice("Wrong clue", "", requiresClue: "nothing", outcomes: new EventOutcome(EventOutcomeKind.GainClue, id: "ledger")));
            var expedition = new ExpeditionDef(full.Id, full.Heroes, full.StartingDeck, full.RewardCards, full.Encounters, full.Villain,
                full.Keepsakes, full.Events.Append(broken));
            IReadOnlyList<string> problems = Library.Validate(expedition);
            Assert.That(problems, Has.Member("The choice 'Too much' in event 'broken' needs more than one extra step; keep it to one."));
            Assert.That(problems, Has.Member("The choice 'Wrong clue' in event 'broken' depends on unknown clue 'nothing'."));
            Assert.That(problems, Has.Member("The choice 'Wrong clue' in event 'broken' gives clue 'ledger', which isn't found in events."));
            Assert.That(Library.Validate(full), Is.Empty);
        }
    }
}

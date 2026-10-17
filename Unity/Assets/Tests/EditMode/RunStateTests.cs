using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class RunStateTests
    {
        static RunState NewRun(ulong seed = 11) => new RunState(StarterContent.Library(), StarterContent.Expedition(), seed);

        static void Ok(RunResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        /// <summary>Plays the current fight with a simple policy: each standing hero in turn plays whatever they can.</summary>
        internal static void PlayFight(CombatState combat)
        {
            for (int turn = 0; turn < 200 && !combat.IsOver; turn++)
            {
                Combatant hero = combat.Heroes.Where(h => !h.IsDown).OrderBy(h => (turn + combat.Heroes.ToList().IndexOf(h)) % 3).First();
                combat.TryActivate(hero.Id);
                foreach (CardInstance card in combat.Hand.ToList())
                {
                    CardDef def = combat.CardOf(card);
                    string target = def.Target == CardTarget.Enemy ? combat.Enemies.FirstOrDefault(e => !e.IsDown)?.Id
                        : def.Target == CardTarget.Ally ? combat.Heroes.FirstOrDefault(h => !h.IsDown && h.Id != hero.Id)?.Id
                        : null;
                    combat.TryPlay(card.Id, target);
                    if (combat.IsOver) return;
                }
                combat.TryEndActivation();
            }
        }

        /// <summary>
        /// Plays a whole run, always taking the leftmost reachable room and the first card offered. With
        /// <paramref name="stopAfterRooms"/>, stops on the map once the party has entered that many rooms.
        /// </summary>
        internal static List<string> PlayRun(RunState run, int stopAfterRooms = int.MaxValue)
        {
            var log = new List<string>();
            for (int step = 0; step < 100 && !run.IsOver; step++)
            {
                if (run.Phase == RunPhase.ChooseRoom && run.Route.Count >= stopAfterRooms) break;
                switch (run.Phase)
                {
                    case RunPhase.ChooseRoom:
                        MapRoom room = run.ReachableRooms.First();
                        Ok(run.TryEnterRoom(room.Id));
                        log.Add(room.ToString());
                        break;
                    case RunPhase.Combat:
                        PlayFight(run.Combat);
                        Ok(run.TryFinishCombat());
                        log.Add(run.Combat.Phase + " " + string.Join(",", run.Party.Select(run.Health)));
                        break;
                    case RunPhase.Reward:
                        log.Add("Took " + run.RewardOffer[0]);
                        Ok(run.TryTakeReward(run.RewardOffer[0]));
                        break;
                    case RunPhase.Sanctuary: Ok(run.TryRest()); break;
                    case RunPhase.Investigation:
                        string searcher = run.ClueHere == null ? null : run.Party.FirstOrDefault(h => run.Health(h) > run.ClueHere.SearchCost);
                        if (searcher != null) log.Add("Found " + run.ClueHere.Id);
                        Ok(searcher != null ? run.TrySearch(searcher) : run.TryLeaveRoom());
                        break;
                    case RunPhase.Secret: Ok(run.TryUseSecret()); break;
                    case RunPhase.Event: log.Add("Chose " + ChooseFirst(run)); break;
                    case RunPhase.RemoveCard: Ok(run.TryReflect(run.Deck[run.Deck.Count - 1].Id)); break;
                    case RunPhase.Unwritten: Ok(run.TryLeaveRoom()); break;
                }
            }
            log.Add(run.Phase.ToString());
            return log;
        }

        [Test]
        public void ARunStartsOnTheMapWithTheStartingDeckAndFullHealth()
        {
            RunState run = NewRun();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(run.CurrentRoom, Is.Null);
            Assert.That(run.Deck.Select(c => c.DefId), Is.EqualTo(StarterContent.StartingDeck().Select(c => c.DefId)));
            Assert.That(run.Party.All(h => run.Health(h) == run.MaxHealth(h)), Is.True);
            Assert.That(run.ReachableRooms, Is.EquivalentTo(run.Map.Layer(0)));
        }

        [Test]
        public void OnlyConnectedRoomsCanBeEntered()
        {
            RunState run = NewRun();
            MapRoom deep = run.Map.Layer(3).First();
            RunResult refused = run.TryEnterRoom(deep.Id);
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Reason, Is.EqualTo("That room isn't connected to this one."));
            Assert.That(run.TryEnterRoom(9999).Reason, Is.EqualTo("There is no such room."));
            Assert.That(run.CurrentRoom, Is.Null);

            MapRoom first = run.Map.Layer(0).First();
            Ok(run.TryEnterRoom(first.Id));
            Assert.That(run.TryEnterRoom(first.Next[0]).Reason, Is.EqualTo("Finish this room first."));
        }

        [Test]
        public void AFightUsesTheRoomsEncounterAndTheRunsDeck()
        {
            RunState run = NewRun();
            MapRoom room = run.Map.Layer(0).First();
            Ok(run.TryEnterRoom(room.Id));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Combat));
            Assert.That(run.Combat.Enemies.Select(e => e.Enemy.Id), Is.EqualTo(run.Expedition.Encounter(room.EncounterId).EnemyIds));
            Assert.That(run.Combat.Hand.Concat(run.Combat.DrawPile).Select(c => c.Id), Is.EquivalentTo(run.Deck.Select(c => c.Id)));
            Assert.That(run.TryFinishCombat().Reason, Is.EqualTo("The fight isn't over yet."));
        }

        [Test]
        public void HealthCarriesOverAndDownedHeroesStandWithAQuarter()
        {
            foreach (ulong seed in Enumerable.Range(1, 30).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                Ok(run.TryEnterRoom(run.Map.Layer(0).First().Id));
                PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase == RunPhase.Lost) continue;

                foreach (Combatant hero in run.Combat.Heroes)
                    Assert.That(run.Health(hero.Id), Is.EqualTo(hero.IsDown ? hero.MaxHealth / 4 : hero.Health), $"Seed {seed}, {hero}");

                Ok(run.TryTakeReward(null));
                MapRoom next = run.ReachableRooms.FirstOrDefault(r => r.EncounterId != null);
                if (next == null) continue;
                Ok(run.TryEnterRoom(next.Id));
                foreach (Combatant hero in run.Combat.Heroes)
                    Assert.That(hero.Health, Is.EqualTo(run.Health(hero.Id)), $"Seed {seed}, {hero}");
            }
        }

        [Test]
        public void ARewardAddsTheChosenCardOrNothing()
        {
            RunState run = WinFirstFight();
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Reward));
            Assert.That(run.RewardOffer, Has.Count.EqualTo(RunState.RewardChoices));
            Assert.That(run.RewardOffer.Distinct().Count(), Is.EqualTo(RunState.RewardChoices));
            Assert.That(run.RewardOffer.All(StarterContent.Expedition().RewardCards.Contains), Is.True);

            Assert.That(run.TryTakeReward("quick_shot").Reason, Is.EqualTo("That card isn't on offer."));
            string chosen = run.RewardOffer[1];
            int before = run.Deck.Count;
            Ok(run.TryTakeReward(chosen));
            Assert.That(run.Deck, Has.Count.EqualTo(before + 1));
            Assert.That(run.Deck.Last().DefId, Is.EqualTo(chosen));
            Assert.That(run.Deck.Select(c => c.Id).Distinct().Count(), Is.EqualTo(run.Deck.Count), "Every card instance has its own id.");
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(run.TryTakeReward(null).Ok, Is.False, "A reward can only be taken once.");
        }

        [Test]
        public void SkippingARewardLeavesTheDeckAlone()
        {
            RunState run = WinFirstFight();
            int before = run.Deck.Count;
            Ok(run.TryTakeReward(null));
            Assert.That(run.Deck, Has.Count.EqualTo(before));
        }

        [Test]
        public void RestingHealsThirtyPercentAndNeverPastMaximum()
        {
            RunState run = RunToSanctuary(out _);
            var before = run.Party.ToDictionary(h => h, run.Health);
            Ok(run.TryRest());
            foreach (string hero in run.Party)
                Assert.That(run.Health(hero), Is.EqualTo(System.Math.Min(run.MaxHealth(hero), before[hero] + run.MaxHealth(hero) * 30 / 100)));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(run.TryRest().Reason, Is.EqualTo("You can only rest in a Sanctuary."));
        }

        [Test]
        public void ReflectingRemovesOneCard()
        {
            RunState run = RunToSanctuary(out _);
            CardInstance card = run.Deck[3];
            int before = run.Deck.Count;
            Assert.That(run.TryReflect(9999).Reason, Is.EqualTo("That card isn't in the deck."));
            Ok(run.TryReflect(card.Id));
            Assert.That(run.Deck, Has.Count.EqualTo(before - 1));
            Assert.That(run.Deck, Has.No.Member(card));
            Assert.That(run.TryReflect(run.Deck[0].Id).Ok, Is.False, "Only one card per Sanctuary.");
        }

        [Test]
        public void EventRoomsHoldAnEvent()
        {
            foreach (ulong seed in Enumerable.Range(1, 50).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                MapRoom room = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Event);
                if (room == null || !Walk(run, room)) continue;
                Assert.That(run.Phase, Is.EqualTo(RunPhase.Event));
                Assert.That(run.CurrentEvent, Is.Not.Null);
                Assert.That(run.TryLeaveRoom().Ok, Is.False, "An event is left by one of its own choices.");
                return;
            }
            Assert.Fail("No seed reached an Event room.");
        }

        [Test]
        public void ARunEndsInVictoryAtTheBossOrDefeat()
        {
            var endings = new HashSet<RunPhase>();
            foreach (ulong seed in Enumerable.Range(1, 40).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                List<string> log = PlayRun(run);
                Assert.That(run.IsOver, Is.True, $"Seed {seed}: " + string.Join(" | ", log));
                endings.Add(run.Phase);
                if (run.Phase == RunPhase.Won)
                {
                    Assert.That(run.CurrentRoom.Kind, Is.EqualTo(RoomKind.Boss));
                    Assert.That(run.Route, Has.Count.EqualTo(run.Map.LayerCount + 1));
                }
                Assert.That(run.TryEnterRoom(run.Map.Boss.Id).Reason, Is.EqualTo("The expedition is over."));
            }
            Assert.That(endings, Has.Member(RunPhase.Lost), "The simple policy should lose some runs.");
        }

        [Test]
        public void TheSameSeedAndChoicesGiveTheSameRun()
        {
            Assert.That(PlayRun(NewRun(77)), Is.EqualTo(PlayRun(NewRun(77))));
        }

        [Test]
        public void ABrokenExpeditionIsRefusedWithItsProblems()
        {
            var broken = new ExpeditionDef("broken", StarterContent.Party, new[] { "quick_shot" }, new[] { "bulwark" },
                new[] { new EncounterDef("lonely", EncounterKind.Normal, 0, 6, "nobody") });
            IReadOnlyList<string> problems = StarterContent.Library().Validate(broken);
            Assert.That(problems, Has.Member("Expedition 'broken' needs at least 3 reward cards to offer a choice."));
            Assert.That(problems, Has.Member("Encounter 'lonely' has unknown enemy 'nobody'."));
            Assert.That(problems, Has.Member("Expedition 'broken' needs exactly one boss encounter, not 0."));
            Assert.That(problems, Has.Member("Expedition 'broken' has no elite encounter for layer 3."));
            Assert.Throws<System.ArgumentException>(() => new RunState(StarterContent.Library(), broken, 1));
        }

        static RunState WinFirstFight()
        {
            foreach (ulong seed in Enumerable.Range(1, 50).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                Ok(run.TryEnterRoom(run.Map.Layer(0).First().Id));
                PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase == RunPhase.Reward) return run;
            }
            Assert.Fail("The simple policy never won a first fight.");
            return null;
        }

        static RunState RunToSanctuary(out MapRoom sanctuary)
        {
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                sanctuary = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Sanctuary && r.Layer >= 2);
                if (sanctuary != null && Walk(run, sanctuary) && run.Party.Any(h => run.Health(h) < run.MaxHealth(h))) return run;
            }
            Assert.Fail("No seed reached a Sanctuary hurt.");
            sanctuary = null;
            return null;
        }

        /// <summary>Walks the shortest connected route to <paramref name="goal"/>, winning fights and skipping rewards. False if the party fell.</summary>
        internal static bool Walk(RunState run, MapRoom goal)
        {
            while (run.CurrentRoom != goal)
            {
                MapRoom next = run.ReachableRooms.FirstOrDefault(r => r == goal || Leads(run.Map, r, goal));
                if (next == null) return false;
                Ok(run.TryEnterRoom(next.Id));
                if (next == goal) return true;
                switch (run.Phase)
                {
                    case RunPhase.Combat:
                        PlayFight(run.Combat);
                        Ok(run.TryFinishCombat());
                        if (run.Phase == RunPhase.Lost) return false;
                        Ok(run.TryTakeReward(null));
                        break;
                    case RunPhase.Sanctuary: Ok(run.TryReflect(run.Deck[run.Deck.Count - 1].Id)); break;
                    case RunPhase.Event:
                        ChooseFirst(run);
                        if (!Finish(run)) return false;
                        break;
                    default: Ok(run.TryLeaveRoom()); break;
                }
            }
            return true;
        }

        /// <summary>Takes the event's first choice that is shown and that the healthiest hero can pay for. Returns its label.</summary>
        internal static string ChooseFirst(RunState run)
        {
            string hero = run.Party.OrderByDescending(run.Health).First();
            for (int i = 0; i < run.CurrentEvent.Choices.Count; i++)
            {
                EventChoice choice = run.CurrentEvent.Choices[i];
                if (!run.IsChoiceShown(choice) || (choice.NeedsAHero && run.Health(hero) <= choice.HeroCost)) continue;
                Ok(run.TryChooseEvent(i, hero));
                return choice.Label;
            }
            Assert.Fail($"No choice in {run.CurrentEvent} can be taken.");
            return null;
        }

        /// <summary>Plays out whatever an event choice led to (a card choice, a removal or a fight). False if the party falls.</summary>
        internal static bool Finish(RunState run)
        {
            if (run.Phase == RunPhase.Combat)
            {
                PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase == RunPhase.Lost) return false;
            }
            if (run.Phase == RunPhase.Reward) Ok(run.TryTakeReward(null));
            if (run.Phase == RunPhase.RemoveCard) Ok(run.TryReflect(run.Deck[run.Deck.Count - 1].Id));
            return true;
        }

        static bool Leads(MapGraph map, MapRoom from, MapRoom goal) =>
            from.Layer < goal.Layer && map.NextOf(from).Any(n => n == goal || Leads(map, n, goal));
    }
}

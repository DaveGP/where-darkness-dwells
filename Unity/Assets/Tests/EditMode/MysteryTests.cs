using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class MysteryTests
    {
        const string Vael = StarterContent.VaelId;

        static RunState NewRun(ulong seed = 11) => new RunState(StarterContent.Library(), StarterContent.Expedition(), seed);

        static void Ok(RunResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        /// <summary>A boss fight with the modifiers a run would give it, without walking the map to get there.</summary>
        static CombatState BossFight(RunState run)
        {
            var combat = new CombatState(StarterContent.Library(), run.Party, StarterContent.StartingDeck(), run.BossEnemies(),
                new Rng(5), modifiers: run.BossModifiers());
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static Combatant Boss(CombatState combat) => combat.Enemies.Single(e => e.Enemy.Id == Vael);

        [Test]
        public void EachInvestigationHoldsADifferentClueThatFitsItsLayer()
        {
            VillainDef villain = StarterContent.Vael();
            foreach (int seed in Enumerable.Range(1, 300))
            {
                MapGraph map = MapGenerator.Generate(StarterContent.Expedition(), new Rng((ulong)seed).Stream("map"));
                List<MapRoom> holding = map.Rooms.Where(r => r.ClueId != null).ToList();
                Assert.That(holding.All(r => r.Kind == RoomKind.Investigation), Is.True, $"Seed {seed}");
                Assert.That(holding.Select(r => r.ClueId).Distinct().Count(), Is.EqualTo(holding.Count), $"Seed {seed}: a clue placed twice");
                foreach (MapRoom room in holding)
                {
                    ClueDef clue = villain.Clue(room.ClueId);
                    Assert.That(room.Layer, Is.InRange(clue.MinLayer, clue.MaxLayer), $"Seed {seed}: {clue} on {room}");
                }
                Assert.That(map.Secret, Is.Not.Null);
                Assert.That(map.Rooms, Has.No.Member(map.Secret), "The secret room isn't on the visible map.");
                Assert.That(map.SecretFrom, Is.EquivalentTo(map.Rooms.Where(r => r.Layer == map.Secret.Layer - 1).Select(r => r.Id)),
                    $"Seed {seed}: every room on the layer before leads to the secret room");
            }
        }

        [Test]
        public void SearchingCostsTheChosenHeroAndFindsTheClue()
        {
            RunState run = ToInvestigation(out MapRoom room);
            ClueDef clue = run.ClueHere;
            Assert.That(clue, Is.Not.Null);
            int before = run.Health(StarterContent.Bram);
            Ok(run.TrySearch(StarterContent.Bram));
            Assert.That(run.Health(StarterContent.Bram), Is.EqualTo(before - clue.SearchCost));
            Assert.That(run.CluesFound, Is.EqualTo(new[] { clue.Id }));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(run.TrySearch(StarterContent.Bram).Ok, Is.False);
        }

        [Test]
        public void LeavingAnInvestigationFindsNothing()
        {
            RunState run = ToInvestigation(out _);
            Ok(run.TryLeaveRoom());
            Assert.That(run.CluesFound, Is.Empty);
        }

        [Test]
        public void AHeroTooHurtToPayCantSearch()
        {
            RunState run = ToInvestigation(out _);
            string hero = StarterContent.Wren;
            int cost = run.ClueHere.SearchCost;
            while (run.Health(hero) > cost) SetHealth(run, hero, cost);
            RunResult refused = run.TrySearch(hero);
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Reason, Is.EqualTo($"Wren is too hurt to search: it costs {cost} health."));
            Assert.That(run.CluesFound, Is.Empty);
        }

        [Test]
        public void TheSecretRoomAppearsOnlyAfterItsClue()
        {
            // The test policy plays simply and can lose on the way, so use the first seed where it gets there.
            RunState run = Enumerable.Range(1, 100).Select(seed => NewRun((ulong)seed))
                .FirstOrDefault(r => RunStateTests.Walk(r, r.Map.Room(r.Map.SecretFrom.First())) && Clear(r));
            Assert.That(run, Is.Not.Null, "The test route should get there on some seed.");
            Assert.That(run.ReachableRooms, Has.No.Member(run.Map.Secret));

            run.GiveClue("cistern_door");
            Assert.That(run.ReachableRooms, Has.Member(run.Map.Secret));
            var health = run.Party.ToDictionary(h => h, run.Health);
            Ok(run.TryEnterRoom(run.Map.Secret.Id));
            Assert.That(run.Phase, Is.EqualTo(RunPhase.Secret));
            Ok(run.TryUseSecret());
            foreach (string hero in run.Party) Assert.That(run.Health(hero), Is.EqualTo(System.Math.Max(1, health[hero] - 4)));
            Assert.That(run.ReachableRooms, Is.EquivalentTo(run.Map.NextOf(run.Map.Secret)));

            Combatant boss = Boss(BossFight(run));
            Assert.That(boss.Health, Is.EqualTo(boss.MaxHealth - boss.MaxHealth * 30 / 100));
        }

        [Test]
        public void WithoutTheLedgerTheBossIsNamelessAndVague()
        {
            CombatState combat = BossFight(NewRun());
            Combatant boss = Boss(combat);
            Assert.That(boss.Name, Is.EqualTo("The Master of the Keep"));
            Assert.That(combat.IsVague(boss), Is.True);
            Assert.That(combat.IntentText(boss, boss.Intent), Is.EqualTo(boss.Intent.Vague));
            List<string> lines = combat.Log.Select(e => CombatLogText.Describe(e, combat)).Where(l => l != null).ToList();
            Assert.That(lines.Any(l => l.Contains("Vael")), Is.False, string.Join("\n", lines));
            Assert.That(lines, Has.Member($"The Master of the Keep readies {boss.Intent.Vague} at {combat.Find(boss.IntentTargetId).Name}."));

            RunState known = NewRun();
            known.GiveClue("ledger");
            CombatState revealed = BossFight(known);
            Assert.That(Boss(revealed).Name, Is.EqualTo("Vael, the Bloodbound Steward"));
            Assert.That(revealed.IsVague(Boss(revealed)), Is.False);
        }

        [Test]
        public void TheConfessionSendsTheThrallsAway()
        {
            Assert.That(BossFight(NewRun()).Enemies.Select(e => e.Enemy.Id), Is.EqualTo(new[] { "blood_thrall", Vael, "blood_thrall" }));
            RunState run = NewRun();
            run.GiveClue("thrall_confession");
            Assert.That(BossFight(run).Enemies.Select(e => e.Enemy.Id), Is.EqualTo(new[] { Vael }));
        }

        [Test]
        public void SilverAddsTwoBeforeExposed()
        {
            RunState run = NewRun();
            run.GiveClue("silvered_bells");
            run.GiveClue("thrall_confession");
            var deck = new[] { new CardInstance(1, "quick_shot"), new CardInstance(2, "quick_shot") };
            var combat = new CombatState(StarterContent.Library(), run.Party, deck, run.BossEnemies(), new Rng(5), modifiers: run.BossModifiers());
            combat.Start();
            Combatant boss = Boss(combat);
            combat.TryActivate(StarterContent.Wren);
            int before = boss.Health;
            Assert.That(combat.TryPlay(1, boss.Id).Ok, Is.True);
            Assert.That(before - boss.Health, Is.EqualTo(8), "Quick Shot deals 6, plus 2.");

            boss.SetStatus(StatusKind.Exposed, 1);
            before = boss.Health;
            combat.TryPlay(2, boss.Id);
            Assert.That(before - boss.Health, Is.EqualTo(12), "(6 + 2) x 1.5.");
        }

        [Test]
        public void SpeakingHisNameCancelsOneBloodTithe()
        {
            RunState run = NewRun();
            run.GiveClue("true_name");
            run.GiveClue("thrall_confession");
            CombatState combat = BossFight(run);
            Combatant boss = Boss(combat);
            Assert.That(combat.Cancel.Label, Is.EqualTo("Speak his name"));
            Assert.That(combat.WhyCantCancel(boss.Id), Is.EqualTo("Speak his name only stops a heal."), "Its first intent is an attack.");

            // Play to his Blood Tithe, hurting him first so the heal would matter.
            boss.Health -= 40;
            while (boss.Intent.Kind != IntentKind.Heal)
            {
                combat.TryActivate(combat.Heroes.First(h => !h.IsDown).Id);
                combat.TryEndActivation();
            }
            int hurt = boss.Health;
            Assert.That(combat.PreviewEnemyPhase().Any(e => e.Kind == CombatEventKind.Healed), Is.True);
            Assert.That(combat.TryCancelIntent(boss.Id).Ok, Is.True);
            Assert.That(combat.PreviewEnemyPhase().Any(e => e.Kind == CombatEventKind.Healed), Is.False, "The preview knows it's cancelled.");
            combat.TryActivate(combat.Heroes.First(h => !h.IsDown).Id);
            combat.TryEndActivation();
            Assert.That(boss.Health, Is.LessThanOrEqualTo(hurt), "He didn't heal.");
            Assert.That(combat.CancelsLeft, Is.EqualTo(0));
            Assert.That(combat.WhyCantCancel(boss.Id), Is.EqualTo("Speak his name has already been used this fight."));
        }

        [Test]
        public void WithoutHisNameThereIsNoCancel()
        {
            CombatState combat = BossFight(NewRun());
            Assert.That(combat.Cancel, Is.Null);
            Assert.That(combat.TryCancelIntent(Boss(combat).Id).Reason, Is.EqualTo("The party has no way to do that yet."));
        }

        [Test]
        public void ABossThatHealsNeverPassesItsMaximum()
        {
            RunState run = NewRun();
            run.GiveClue("thrall_confession");
            CombatState combat = BossFight(run);
            Combatant boss = Boss(combat);
            for (int i = 0; i < 6; i++)
            {
                combat.TryActivate(combat.Heroes.First(h => !h.IsDown).Id);
                combat.TryEndActivation();
                Assert.That(boss.Health, Is.LessThanOrEqualTo(boss.MaxHealth));
            }
        }

        [Test]
        public void TheBrokenSealGivesTheTrueEnding()
        {
            int won = 0, trueEndings = 0;
            foreach (ulong seed in Enumerable.Range(1, 60).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                RunStateTests.PlayRun(run);
                if (run.Phase != RunPhase.Won) continue;
                won++;
                bool seal = run.CluesFound.Contains("broken_seal");
                Assert.That(run.TrueEnding, Is.EqualTo(seal));
                Assert.That(run.EndingText, Is.EqualTo(seal ? run.Villain.TrueEnding : run.Villain.Ending));
                if (seal) trueEndings++;
            }
            Assert.That(won, Is.GreaterThan(0), "The simple policy should win some runs.");
        }

        [Test]
        public void TheFirstEliteBeatenGivesHisTrueName()
        {
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                MapRoom elite = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Elite);
                if (elite == null || !RunStateTests.Walk(run, elite)) continue;
                RunStateTests.PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase != RunPhase.Reward) continue;
                Assert.That(run.RewardClue, Is.EqualTo("true_name"));
                Assert.That(run.CluesFound, Has.Member("true_name"));
                return;
            }
            Assert.Fail("No seed won an Elite fight.");
        }

        [Test]
        public void ABrokenVillainIsRefusedWithItsProblems()
        {
            ExpeditionDef good = StarterContent.Expedition();
            var villain = new VillainDef("broken", "nobody", "Someone",
                new[]
                {
                    new ClueDef("late_door", "A Late Door", ClueSource.Investigation, 0, 5, 1, "A door.", "Open it", "It opens.",
                        new ClueEffect(ClueEffectKind.RevealSecretRoom)),
                    new ClueDef("blank", "A Blank Page", ClueSource.Investigation, 0, 6, 1, "", "", "Nothing.",
                        new ClueEffect(ClueEffectKind.CancelBossIntent, cancelKind: IntentKind.Heal)),
                },
                new SecretRoomDef("A Cellar", 4, "Dark.", "Look", 1, 10), "The end.", "The true end.");
            var broken = new ExpeditionDef("broken", good.Party, good.StartingDeck, good.RewardCards, good.Encounters, villain);

            IReadOnlyList<string> problems = StarterContent.Library().Validate(broken);
            Assert.That(problems, Has.Member("Villain 'broken' has unknown boss enemy 'nobody'."));
            Assert.That(problems, Has.Member("Clue 'late_door' can be found after the secret room it reveals."));
            Assert.That(problems, Has.Member("Clue 'blank' can be found in an Investigation, so it needs a scene and a search label."));
            Assert.That(problems, Has.Member("Clue 'blank' grants a cancel with no button label."));
            Assert.That(StarterContent.Library().Validate(good), Is.Empty);
        }

        static RunState ToInvestigation(out MapRoom room)
        {
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                room = run.Map.Rooms.FirstOrDefault(r => r.ClueId != null && r.Layer <= 2);
                if (room != null && RunStateTests.Walk(run, room) && run.Phase == RunPhase.Investigation) return run;
            }
            Assert.Fail("No seed reached an Investigation with a clue.");
            room = null;
            return null;
        }

        /// <summary>Finishes whatever room the party is standing in.</summary>
        /// <summary>Finishes the room the run is in, fighting if it has to. False if the party falls.</summary>
        static bool Clear(RunState run)
        {
            if (run.Phase == RunPhase.Combat)
            {
                RunStateTests.PlayFight(run.Combat);
                Ok(run.TryFinishCombat());
                if (run.Phase == RunPhase.Lost) return false;
                Ok(run.TryTakeReward(null));
            }
            if (run.Phase != RunPhase.ChooseRoom) Ok(Settle(run));
            return true;
        }

        static RunResult Settle(RunState run) => run.Phase == RunPhase.Sanctuary ? run.TryRest() : run.TryLeaveRoom();

        static void SetHealth(RunState run, string hero, int health) => run.SetHealthForTest(hero, health);
    }
}

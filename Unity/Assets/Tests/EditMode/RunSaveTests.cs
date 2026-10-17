using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class RunSaveTests
    {
        static RunState NewRun(ulong seed) => new RunState(StarterContent.Library(), StarterContent.Expedition(), seed);

        static void Ok(RunResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        static RunSnapshot Save(RunState run, int enteringRoomId = -1)
        {
            Ok(run.TrySnapshot(out RunSnapshot save, enteringRoomId));
            return save;
        }

        static RunState Restore(RunSnapshot save)
        {
            Ok(RunState.TryRestore(StarterContent.Library(), StarterContent.Expedition(), save, out RunState run));
            return run;
        }

        static string Refusal(RunSnapshot save)
        {
            RunResult result = RunState.TryRestore(StarterContent.Library(), StarterContent.Expedition(), save, out RunState run);
            Assert.That(result.Ok, Is.False);
            Assert.That(run, Is.Null);
            return result.Reason;
        }

        /// <summary>A run that has been through a few rooms and is still going, so there is something to save.</summary>
        static RunState MidRun(int rooms = 3)
        {
            foreach (ulong seed in Enumerable.Range(1, 50).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                RunStateTests.PlayRun(run, rooms);
                if (!run.IsOver && run.Route.Count == rooms) return run;
            }
            Assert.Fail("The simple policy never got through the first rooms.");
            return null;
        }

        [Test]
        public void ARestoredRunIsTheSameRun()
        {
            RunState run = MidRun();
            run.GiveKeepsake("bone_charm");
            RunState back = Restore(Save(run));

            Assert.That(back.Phase, Is.EqualTo(RunPhase.ChooseRoom));
            Assert.That(back.Seed, Is.EqualTo(run.Seed));
            Assert.That(back.Party, Is.EqualTo(run.Party));
            Assert.That(back.Party.Select(back.Health), Is.EqualTo(run.Party.Select(run.Health)));
            Assert.That(back.Party.Select(back.MaxHealth), Is.EqualTo(run.Party.Select(run.MaxHealth)));
            Assert.That(back.Deck.Select(c => c.ToString()), Is.EqualTo(run.Deck.Select(c => c.ToString())));
            Assert.That(back.Keepsakes, Is.EqualTo(run.Keepsakes));
            Assert.That(back.CluesFound, Is.EqualTo(run.CluesFound));
            Assert.That(back.Route, Is.EqualTo(run.Route));
            Assert.That(back.CurrentRoom.Id, Is.EqualTo(run.CurrentRoom.Id));
            Assert.That(back.ReachableRooms.Select(r => r.Id), Is.EqualTo(run.ReachableRooms.Select(r => r.Id)));
            Assert.That(back.FightsWon, Is.EqualTo(run.FightsWon));
        }

        [Test]
        public void ARestoredRunPlaysOnExactlyAsTheOriginal()
        {
            foreach (int rooms in new[] { 1, 3, 5 })
            {
                RunState run = MidRun(rooms);
                RunState back = Restore(Save(run));
                Assert.That(RunStateTests.PlayRun(back), Is.EqualTo(RunStateTests.PlayRun(run)), $"After {rooms} rooms.");
                Assert.That(back.Deck.Select(c => c.ToString()), Is.EqualTo(run.Deck.Select(c => c.ToString())));
            }
        }

        [Test]
        public void ASaveTakenOnTheWayIntoARoomEntersItAgain()
        {
            RunState run = MidRun();
            MapRoom room = run.ReachableRooms.First();
            RunSnapshot save = Save(run, room.Id);
            Ok(run.TryEnterRoom(room.Id));

            RunState back = Restore(save);
            Assert.That(back.CurrentRoom.Id, Is.EqualTo(room.Id));
            Assert.That(back.Phase, Is.EqualTo(run.Phase));
            Assert.That(back.CurrentEvent?.Id, Is.EqualTo(run.CurrentEvent?.Id));
            if (run.Phase == RunPhase.Combat)
            {
                Assert.That(back.Combat.Enemies.Select(e => e.Id), Is.EqualTo(run.Combat.Enemies.Select(e => e.Id)));
                Assert.That(back.Combat.Hand.Select(c => c.ToString()), Is.EqualTo(run.Combat.Hand.Select(c => c.ToString())));
            }
            Assert.That(RunStateTests.PlayRun(back), Is.EqualTo(RunStateTests.PlayRun(run)));
        }

        [Test]
        public void TheSameEventComesBackAfterQuittingInsideIt()
        {
            foreach (ulong seed in Enumerable.Range(1, 100).Select(s => (ulong)s))
            {
                RunState run = NewRun(seed);
                MapRoom eventRoom = run.Map.Rooms.FirstOrDefault(r => r.Kind == RoomKind.Event && r.Layer >= 1);
                if (eventRoom == null) continue;
                MapRoom before = run.Map.Rooms.FirstOrDefault(r => r.Layer == eventRoom.Layer - 1 && r.Next.Contains(eventRoom.Id));
                if (before == null || !RunStateTests.Walk(run, before) || !RunStateTests.Finish(run)) continue;
                if (run.Phase != RunPhase.ChooseRoom)
                {
                    if (run.Phase == RunPhase.Event) RunStateTests.ChooseFirst(run);
                    else if (run.Phase == RunPhase.Sanctuary) Ok(run.TryRest());
                    else if (run.Phase == RunPhase.Combat) { RunStateTests.PlayFight(run.Combat); Ok(run.TryFinishCombat()); }
                    else Ok(run.TryLeaveRoom());
                    if (!RunStateTests.Finish(run) || run.Phase != RunPhase.ChooseRoom) continue;
                }

                RunSnapshot save = Save(run, eventRoom.Id);
                Ok(run.TryEnterRoom(eventRoom.Id));
                Assert.That(run.CurrentEvent, Is.Not.Null);
                for (int i = 0; i < 3; i++) Assert.That(Restore(save).CurrentEvent.Id, Is.EqualTo(run.CurrentEvent.Id));
                return;
            }
            Assert.Fail("No seed reached an Event room past the first layer.");
        }

        [Test]
        public void TheRunCanOnlyBeSavedOnTheMap()
        {
            RunState run = NewRun(3);
            Ok(run.TryEnterRoom(run.Map.Layer(0).First().Id));
            RunResult result = run.TrySnapshot(out RunSnapshot save);
            Assert.That(result.Ok, Is.False);
            Assert.That(result.Reason, Is.EqualTo("The run can only be saved on the map."));
            Assert.That(save, Is.Null);
        }

        [Test]
        public void ASaveOnTheWayIntoAnUnconnectedRoomIsRefused()
        {
            RunState run = NewRun(3);
            int far = run.Map.Layer(3).First().Id;
            Assert.That(run.TrySnapshot(out _, far).Reason, Is.EqualTo("That room isn't connected to this one."));
        }

        [Test]
        public void ASaveThatNoLongerFitsTheContentIsRefused()
        {
            RunState run = MidRun();

            RunSnapshot save = Save(run);
            save.Deck[0].DefId = "a_card_that_was_removed";
            Assert.That(Refusal(save), Is.EqualTo("The saved expedition can't be loaded: the deck holds a card that doesn't exist."));

            save = Save(run);
            save.Version = RunSnapshot.CurrentVersion + 1;
            Assert.That(Refusal(save), Does.EndWith("it was made by another version of the game."));

            save = Save(run);
            save.Route.Reverse();
            Assert.That(Refusal(save), Does.EndWith("its route doesn't fit the map."));

            save = Save(run);
            save.Clues.Add("nobody_knows");
            Assert.That(Refusal(save), Does.EndWith("it holds a clue that doesn't exist."));

            save = Save(run);
            save.Heroes[0].Health = 999;
            Assert.That(Refusal(save), Does.EndWith("health is out of range."));

            save = Save(run);
            save.Heroes.RemoveAt(0);
            Assert.That(Refusal(save), Does.EndWith("Choose 3 heroes."));

            save = Save(run);
            save.Seed = "not a number";
            Assert.That(Refusal(save), Does.EndWith("its seed is unreadable."));

            Assert.That(Refusal(null), Does.EndWith("there is no save."));
        }

        [Test]
        public void AFinishedRunCantBeSaved()
        {
            RunState run = NewRun(5);
            RunStateTests.PlayRun(run);
            Assert.That(run.IsOver, Is.True);
            Assert.That(run.TrySnapshot(out _).Reason, Is.EqualTo("The expedition is over."));
        }
    }
}

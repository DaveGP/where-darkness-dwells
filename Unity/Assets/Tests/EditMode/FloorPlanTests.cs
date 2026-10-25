using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class FloorPlanTests
    {
        static RunState NewRun(ulong seed) => new RunState(StarterContent.Library(), StarterContent.Expedition(), seed);

        static IEnumerable<ulong> Seeds() => Enumerable.Range(1, 1000).Select(s => (ulong)s);

        /// <summary>Every link the map has, with the Entrance's links to the first layer.</summary>
        static IEnumerable<(int from, int to)> Links(MapGraph map) =>
            map.Layer(0).Select(r => (FloorPlan.EntranceId, r.Id))
                .Concat(map.Rooms.SelectMany(r => r.Next.Select(n => (r.Id, n))));

        static string Describe(FloorPlan plan) =>
            string.Join(";", plan.Rooms.Select(r => $"{r.RoomId}:{r.X},{r.Y},{r.Width}x{r.Height}:{string.Join(",", r.Passages)}"))
            + "|" + string.Join(";", plan.Doors.Select(d => $"{d.FromId}>{d.ToId}@{d.From}{d.To}"));

        [Test]
        public void EveryLinkIsOneDoorBetweenNeighbouringSquaresOfItsTwoRooms()
        {
            foreach (ulong seed in Seeds())
            {
                RunState run = NewRun(seed);
                FloorPlan plan = run.FloorPlan;
                var owner = plan.Rooms.SelectMany(r => r.Cells.Select(c => (c, r.RoomId))).ToDictionary(p => p.c, p => p.RoomId);
                foreach ((int from, int to) in Links(run.Map))
                {
                    List<FloorDoor> doors = plan.Doors.Where(d => d.FromId == from && d.ToId == to).ToList();
                    Assert.That(doors.Count, Is.EqualTo(1), $"Seed {seed}: link {from} > {to}");
                    FloorDoor door = doors[0];
                    Assert.That(door.From.IsBeside(door.To), Is.True, $"Seed {seed}: door {from} > {to}");
                    Assert.That(owner.TryGetValue(door.From, out int a) && a == from, Is.True, $"Seed {seed}: door {from} > {to} doesn't start in {from}");
                    Assert.That(owner.TryGetValue(door.To, out int b) && b == to, Is.True, $"Seed {seed}: door {from} > {to} doesn't open into {to}");
                }
            }
        }

        [Test]
        public void RoomsThatArentLinkedShareNoDoor()
        {
            foreach (ulong seed in Seeds())
            {
                RunState run = NewRun(seed);
                var links = new HashSet<(int, int)>(Links(run.Map));
                foreach (FloorDoor door in run.FloorPlan.Doors)
                    Assert.That(links.Contains((door.FromId, door.ToId)), Is.True, $"Seed {seed}: door {door.FromId} > {door.ToId} isn't a link");
                Assert.That(run.FloorPlan.Doors.Count, Is.EqualTo(links.Count), $"Seed {seed}");
            }
        }

        [Test]
        public void NoSquareBelongsToTwoRooms()
        {
            foreach (ulong seed in Seeds())
            {
                FloorPlan plan = NewRun(seed).FloorPlan;
                var seen = new Dictionary<Cell, int>();
                foreach (FloorRoom room in plan.Rooms)
                foreach (Cell cell in room.Cells)
                {
                    Assert.That(seen.TryGetValue(cell, out int other), Is.False, $"Seed {seed}: {cell} is in {room.RoomId} and {other}");
                    seen[cell] = room.RoomId;
                    Assert.That(cell.X >= 0 && cell.X < plan.Width && cell.Y >= 0 && cell.Y < plan.Height, Is.True, $"Seed {seed}: {cell} is off the plan");
                }
            }
        }

        [Test]
        public void EachRoomIsInOnePiece()
        {
            foreach (ulong seed in Seeds())
            foreach (FloorRoom room in NewRun(seed).FloorPlan.Rooms)
            {
                var cells = new HashSet<Cell>(room.Cells);
                var reached = new HashSet<Cell> { cells.First() };
                var open = new Stack<Cell>(reached);
                while (open.Count > 0)
                {
                    Cell c = open.Pop();
                    foreach (Cell n in cells.Where(n => n.IsBeside(c) && reached.Add(n))) open.Push(n);
                }
                Assert.That(reached.Count, Is.EqualTo(cells.Count), $"Seed {seed}: room {room.RoomId} is in pieces");
            }
        }

        [Test]
        public void EveryRoomButTheSecretOneIsOnThePlan()
        {
            foreach (ulong seed in Seeds().Take(100))
            {
                RunState run = NewRun(seed);
                Assert.That(run.FloorPlan.Entrance, Is.Not.Null);
                foreach (MapRoom room in run.Map.Rooms) Assert.That(run.FloorPlan.Room(room.Id), Is.Not.Null, $"Seed {seed}: {room}");
                if (run.Map.Secret != null) Assert.That(run.FloorPlan.Room(run.Map.Secret.Id), Is.Null);
            }
        }

        [Test]
        public void TheSameSeedGivesTheSamePlan()
        {
            foreach (ulong seed in Seeds().Take(200))
                Assert.That(Describe(NewRun(seed).FloorPlan), Is.EqualTo(Describe(NewRun(seed).FloorPlan)), $"Seed {seed}");
            Assert.That(Describe(NewRun(42).FloorPlan), Is.Not.EqualTo(Describe(NewRun(43).FloorPlan)));
        }

        [Test]
        public void ThePlanStaysSmallEnoughForTheScreen()
        {
            foreach (ulong seed in Seeds())
            {
                FloorPlan plan = NewRun(seed).FloorPlan;
                Assert.That(plan.Width, Is.EqualTo(17), $"Seed {seed}");
                Assert.That(plan.Height, Is.LessThanOrEqualTo(30), $"Seed {seed}");
            }
        }

        [Test]
        public void LayingThePlanChangesNothingInTheRun()
        {
            RunState with = NewRun(7), without = NewRun(7);
            _ = with.FloorPlan;
            List<string> a = RunStateTests.PlayRun(with), b = RunStateTests.PlayRun(without);
            Assert.That(a, Is.EqualTo(b));
        }
    }
}

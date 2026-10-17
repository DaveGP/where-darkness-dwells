using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class MapGeneratorTests
    {
        static MapGraph Map(int seed) => MapGenerator.Generate(StarterContent.Expedition(), new Rng((ulong)seed).Stream("map"));

        static IEnumerable<int> Seeds() => Enumerable.Range(1, 300);

        [Test]
        public void TheExpeditionIsValid()
        {
            Assert.That(StarterContent.Library().Validate(StarterContent.Expedition()), Is.Empty);
        }

        [Test]
        public void TheSameSeedGivesTheSameMap()
        {
            string Describe(MapGraph m) => string.Join(";", m.Rooms.Select(r => $"{r.Id}:{r.Kind}:{r.EncounterId}:{string.Join(",", r.Next)}"));
            Assert.That(Describe(Map(42)), Is.EqualTo(Describe(Map(42))));
            Assert.That(Describe(Map(42)), Is.Not.EqualTo(Describe(Map(43))));
        }

        [Test]
        public void LayersAndExitsStayInRange()
        {
            foreach (int seed in Seeds()) CheckLayersAndExitsStayInRange(Map(seed), seed);
        }

        static void CheckLayersAndExitsStayInRange(MapGraph map, int seed)
        {
            Assert.That(map.LayerCount, Is.EqualTo(MapGenerator.Layers));
            for (int layer = 0; layer < map.LayerCount; layer++)
            {
                List<MapRoom> rooms = map.Layer(layer).ToList();
                Assert.That(rooms.Count, Is.InRange(MapGenerator.MinRoomsPerLayer, MapGenerator.MaxRoomsPerLayer), $"Seed {seed}");
                foreach (MapRoom room in rooms)
                {
                    Assert.That(room.Next.Count, Is.InRange(1, MapGenerator.MaxExits), $"Seed {seed}: {room}");
                    Assert.That(map.NextOf(room).All(n => n.Layer == layer + 1), Is.True, $"Seed {seed}: {room}");
                }
            }
            Assert.That(map.Boss.Next, Is.Empty, $"Seed {seed}");
            Assert.That(map.Layer(map.LayerCount - 1).All(r => r.Next.Single() == map.Boss.Id), Is.True, $"Seed {seed}");
        }

        [Test]
        public void EveryRoomCanBeReachedAndExitsNeverCross()
        {
            foreach (int seed in Seeds()) CheckEveryRoomCanBeReachedAndExitsNeverCross(Map(seed), seed);
        }

        static void CheckEveryRoomCanBeReachedAndExitsNeverCross(MapGraph map, int seed)
        {
            foreach (MapRoom room in map.Rooms.Where(r => r.Layer > 0))
                Assert.That(map.Rooms.Any(r => r.Next.Contains(room.Id)), Is.True, $"Seed {seed}: nothing leads to {room}.");

            foreach (MapRoom a in map.Rooms)
            foreach (MapRoom b in map.Rooms.Where(r => r.Layer == a.Layer && r.Column > a.Column))
            {
                int furthestA = map.NextOf(a).Max(r => r.Column);
                int nearestB = map.NextOf(b).Min(r => r.Column);
                Assert.That(furthestA, Is.LessThanOrEqualTo(nearestB), $"Seed {seed}: exits of {a} and {b} cross.");
            }
        }

        [Test]
        public void RoomKindsFollowTheRules()
        {
            foreach (int seed in Seeds()) CheckRoomKindsFollowTheRules(Map(seed), seed);
        }

        static void CheckRoomKindsFollowTheRules(MapGraph map, int seed)
        {
            Assert.That(map.Layer(0).All(r => r.Kind == RoomKind.Combat), Is.True, $"Seed {seed}: the first layer is ordinary fights.");
            Assert.That(map.Rooms.Where(r => r.Layer < MapGenerator.FirstEliteLayer).Any(r => r.Kind == RoomKind.Elite), Is.False, $"Seed {seed}");
            Assert.That(map.Rooms.Count(r => r.Kind == RoomKind.Boss), Is.EqualTo(1), $"Seed {seed}");

            ExpeditionDef expedition = StarterContent.Expedition();
            foreach (MapRoom room in map.Rooms)
            {
                bool fights = room.Kind == RoomKind.Combat || room.Kind == RoomKind.Elite || room.Kind == RoomKind.Boss;
                Assert.That(room.EncounterId != null, Is.EqualTo(fights), $"Seed {seed}: {room}");
                if (!fights) continue;
                EncounterDef encounter = expedition.Encounter(room.EncounterId);
                EncounterKind expected = room.Kind == RoomKind.Elite ? EncounterKind.Elite : room.Kind == RoomKind.Boss ? EncounterKind.Boss : EncounterKind.Normal;
                Assert.That(encounter.Kind, Is.EqualTo(expected), $"Seed {seed}: {room}");
                if (expected != EncounterKind.Boss) Assert.That(encounter.FitsLayer(room.Layer), Is.True, $"Seed {seed}: {room}");
            }
        }

        [Test]
        public void SomeRoutePassesTwoInvestigationsAndASanctuary()
        {
            foreach (int seed in Seeds()) CheckSomeRoutePassesTwoInvestigationsAndASanctuary(Map(seed), seed);
        }

        static void CheckSomeRoutePassesTwoInvestigationsAndASanctuary(MapGraph map, int seed)
        {
            // Every (investigations, sanctuaries) count a route from each room can collect, capped at what the rule needs.
            var counts = new Dictionary<int, HashSet<(int inv, int sanc)>>();
            foreach (MapRoom room in map.Rooms.OrderByDescending(r => r.Layer))
            {
                int inv = room.Kind == RoomKind.Investigation ? 1 : 0, sanc = room.Kind == RoomKind.Sanctuary ? 1 : 0;
                IEnumerable<(int inv, int sanc)> after = room.Next.Count == 0 ? new[] { (0, 0) } : room.Next.SelectMany(id => counts[id]);
                counts[room.Id] = new HashSet<(int, int)>(after.Select(a => (System.Math.Min(2, a.inv + inv), System.Math.Min(1, a.sanc + sanc))));
            }
            Assert.That(map.Layer(0).Any(r => counts[r.Id].Contains((2, 1))), Is.True, $"Seed {seed}");
        }

        [Test]
        public void RoomKindsAreMixedAcrossManyMaps()
        {
            var counts = Seeds().SelectMany(s => Map(s).Rooms).GroupBy(r => r.Kind).ToDictionary(g => g.Key, g => g.Count());
            foreach (RoomKind kind in new[] { RoomKind.Combat, RoomKind.Elite, RoomKind.Investigation, RoomKind.Event, RoomKind.Sanctuary })
                Assert.That(counts.ContainsKey(kind), Is.True, kind.ToString());
        }
    }
}

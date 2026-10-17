using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// Builds a floor map from the run's "map" stream, following the rules in GAME_DESIGN.md: 7 layers of 2 to 4
    /// rooms, 1 to 3 exits per room, every room reachable, no Elite on the first two layers, and at least one route
    /// through 2 Investigation rooms and a Sanctuary. The first layer is always ordinary fights.
    /// </summary>
    public static class MapGenerator
    {
        public const int Layers = 7;
        public const int MinRoomsPerLayer = 2;
        public const int MaxRoomsPerLayer = 4;
        public const int MaxExits = 3;
        public const int FirstEliteLayer = 2;
        public const int InvestigationsOnRoute = 2;

        // How often each kind of room appears after the first layer, out of 100.
        static readonly (RoomKind kind, int weight)[] Weights =
        {
            (RoomKind.Combat, 45),
            (RoomKind.Investigation, 20),
            (RoomKind.Event, 15),
            (RoomKind.Sanctuary, 10),
            (RoomKind.Elite, 10),
        };

        public static MapGraph Generate(ExpeditionDef expedition, Rng rng)
        {
            if (expedition == null) throw new ArgumentNullException(nameof(expedition));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            var rooms = new List<MapRoom>();
            var layers = new List<List<MapRoom>>();
            for (int layer = 0; layer < Layers; layer++)
            {
                int count = rng.Next(MinRoomsPerLayer, MaxRoomsPerLayer + 1);
                var row = new List<MapRoom>();
                for (int column = 0; column < count; column++)
                {
                    var room = new MapRoom(rooms.Count, layer, column);
                    rooms.Add(room);
                    row.Add(room);
                }
                layers.Add(row);
            }
            var boss = new MapRoom(rooms.Count, Layers, 0) { Kind = RoomKind.Boss, EncounterId = expedition.Boss?.Id };
            rooms.Add(boss);

            for (int layer = 0; layer + 1 < Layers; layer++) Connect(layers[layer], layers[layer + 1], rng);
            foreach (MapRoom room in layers[Layers - 1]) room.NextIds.Add(boss.Id);

            foreach (MapRoom room in rooms.Where(r => r.Layer > 0 && r.Kind != RoomKind.Boss)) room.Kind = PickKind(room.Layer, rng);
            var map = new MapGraph(rooms);
            GuaranteeRoute(map, layers, rng);
            foreach (MapRoom room in rooms.Where(r => r.Kind == RoomKind.Combat || r.Kind == RoomKind.Elite))
                room.EncounterId = PickEncounter(expedition, room, rng);
            if (expedition.Villain != null)
            {
                PlaceClues(expedition.Villain, rooms, rng);
                if (expedition.Villain.SecretRoom != null) AddSecretRoom(map, layers, expedition.Villain.SecretRoom, rooms.Count);
            }
            return map;
        }

        /// <summary>
        /// Links two layers with a "staircase": starting from the two leftmost rooms, each step moves right on one
        /// layer or both and adds a link. Every room gets at least one link in and out, and no two links cross.
        /// </summary>
        static void Connect(List<MapRoom> from, List<MapRoom> to, Rng rng)
        {
            for (int attempt = 0; attempt < 50; attempt++)
            {
                var links = new List<(int a, int b)> { (0, 0) };
                int i = 0, j = 0;
                while (i < from.Count - 1 || j < to.Count - 1)
                {
                    int step = i == from.Count - 1 ? 1 : j == to.Count - 1 ? 0 : rng.Next(3);
                    if (step != 1) i++;
                    if (step != 0) j++;
                    links.Add((i, j));
                }
                if (links.GroupBy(l => l.a).Any(g => g.Count() > MaxExits)) continue;
                foreach ((int a, int b) in links) from[a].NextIds.Add(to[b].Id);
                return;
            }
            // Unreachable in practice with at most 4 rooms a layer, but always leave a valid map.
            for (int a = 0; a < from.Count; a++) from[a].NextIds.Add(to[a * to.Count / from.Count].Id);
            for (int b = 0; b < to.Count; b++)
                if (!from.Any(r => r.NextIds.Contains(to[b].Id))) from[b * from.Count / to.Count].NextIds.Add(to[b].Id);
        }

        static RoomKind PickKind(int layer, Rng rng)
        {
            int roll = rng.Next(100);
            foreach ((RoomKind kind, int weight) in Weights)
            {
                if (roll < weight) return kind == RoomKind.Elite && layer < FirstEliteLayer ? RoomKind.Combat : kind;
                roll -= weight;
            }
            return RoomKind.Combat;
        }

        /// <summary>Picks one random route and turns some of its plain rooms into the Investigations and Sanctuary it lacks.</summary>
        static void GuaranteeRoute(MapGraph map, List<List<MapRoom>> layers, Rng rng)
        {
            var route = new List<MapRoom> { layers[0][rng.Next(layers[0].Count)] };
            while (route.Count < Layers)
            {
                IReadOnlyList<int> next = route[route.Count - 1].Next;
                route.Add(map.Room(next[rng.Next(next.Count)]));
            }

            // Plain fights and events change first; an Elite only when nothing else is left.
            List<MapRoom> plain = route.Where(r => r.Layer > 0 && r.Kind != RoomKind.Investigation && r.Kind != RoomKind.Sanctuary).ToList();
            rng.Shuffle(plain);
            plain = plain.OrderBy(r => r.Kind == RoomKind.Elite).ToList();
            int stop = 0;
            if (!route.Any(r => r.Kind == RoomKind.Sanctuary)) plain[stop++].Kind = RoomKind.Sanctuary;
            for (int found = route.Count(r => r.Kind == RoomKind.Investigation); found < InvestigationsOnRoute; found++)
                plain[stop++].Kind = RoomKind.Investigation;
        }

        /// <summary>Gives each Investigation room, in random order, a different clue that can be found on its layer.</summary>
        static void PlaceClues(VillainDef villain, List<MapRoom> rooms, Rng rng)
        {
            List<MapRoom> investigations = rooms.Where(r => r.Kind == RoomKind.Investigation).ToList();
            rng.Shuffle(investigations);
            var unplaced = villain.Clues.Where(c => (c.Sources & ClueSource.Investigation) != 0).ToList();
            foreach (MapRoom room in investigations)
            {
                List<ClueDef> fits = unplaced.Where(c => room.Layer >= c.MinLayer && room.Layer <= c.MaxLayer).ToList();
                if (fits.Count == 0) continue;
                ClueDef clue = fits[rng.Next(fits.Count)];
                room.ClueId = clue.Id;
                unplaced.Remove(clue);
            }
        }

        /// <summary>
        /// Puts the secret room below the last room of its layer, linked from the last room of the layer before and
        /// to the last room of the layer after, so its links can't cross any others.
        /// </summary>
        static void AddSecretRoom(MapGraph map, List<List<MapRoom>> layers, SecretRoomDef secret, int id)
        {
            List<MapRoom> layer = layers[secret.Layer];
            var room = new MapRoom(id, secret.Layer, layer.Count) { Kind = RoomKind.Secret };
            room.NextIds.Add(layers[secret.Layer + 1].Last().Id);
            map.Secret = room;
            map._secretFrom.Add(layers[secret.Layer - 1].Last().Id);
        }

        static string PickEncounter(ExpeditionDef expedition, MapRoom room, Rng rng)
        {
            EncounterKind kind = room.Kind == RoomKind.Elite ? EncounterKind.Elite : EncounterKind.Normal;
            List<EncounterDef> fits = expedition.Encounters.Where(e => e.Kind == kind && e.FitsLayer(room.Layer)).ToList();
            if (fits.Count == 0) throw new InvalidOperationException($"No {kind} encounter fits layer {room.Layer + 1}. Validate the expedition first.");
            return fits[rng.Next(fits.Count)].Id;
        }
    }
}

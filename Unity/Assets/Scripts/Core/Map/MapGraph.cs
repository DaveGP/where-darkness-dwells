using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    public enum RoomKind { Combat, Elite, Investigation, Event, Sanctuary, Boss, Secret }

    /// <summary>One room on the floor map. Its kind and encounter are fixed when the map is generated.</summary>
    public sealed class MapRoom
    {
        internal readonly List<int> NextIds = new List<int>();

        public readonly int Id;
        /// <summary>0 is the first layer. The boss sits alone on the layer after the last one.</summary>
        public readonly int Layer;
        /// <summary>Position in its layer, left to right. Exits never cross, so the map can be drawn without tangles.</summary>
        public readonly int Column;

        public RoomKind Kind { get; internal set; }
        /// <summary>The fight in a Combat, Elite or Boss room; null for other rooms.</summary>
        public string EncounterId { get; internal set; }
        /// <summary>The clue an Investigation room holds; null if it holds none.</summary>
        public string ClueId { get; internal set; }
        /// <summary>The rooms on the next layer this one leads to.</summary>
        public IReadOnlyList<int> Next => NextIds;

        internal MapRoom(int id, int layer, int column)
        {
            Id = id;
            Layer = layer;
            Column = column;
        }

        public override string ToString() => $"{Kind} {Layer + 1}.{Column + 1}";
    }

    /// <summary>The floor map for one run. It doesn't change after it is generated; the run records where the party is.</summary>
    public sealed class MapGraph
    {
        readonly List<MapRoom> _rooms;

        internal MapGraph(List<MapRoom> rooms) => _rooms = rooms;

        /// <summary>The rooms everyone can see. The secret room isn't one of them.</summary>
        public IReadOnlyList<MapRoom> Rooms => _rooms;

        /// <summary>The villain's secret room, or null. It only joins the map once a clue reveals it.</summary>
        public MapRoom Secret { get; internal set; }
        /// <summary>The rooms that lead to the secret room once it is revealed.</summary>
        public IReadOnlyList<int> SecretFrom => _secretFrom;
        internal readonly List<int> _secretFrom = new List<int>();

        /// <summary>The number of layers before the boss.</summary>
        public int LayerCount => Boss.Layer;

        public MapRoom Boss => _rooms.First(r => r.Kind == RoomKind.Boss);

        public MapRoom Room(int id) => Secret != null && Secret.Id == id ? Secret : _rooms.FirstOrDefault(r => r.Id == id);

        public IEnumerable<MapRoom> Layer(int layer) => _rooms.Where(r => r.Layer == layer).OrderBy(r => r.Column);

        /// <summary>The rooms this one leads to, including the secret room when <paramref name="withSecret"/> is set.</summary>
        public IEnumerable<MapRoom> NextOf(MapRoom room, bool withSecret = false) =>
            room.NextIds.Select(Room).Concat(withSecret && _secretFrom.Contains(room.Id) ? new[] { Secret } : new MapRoom[0]);
    }
}

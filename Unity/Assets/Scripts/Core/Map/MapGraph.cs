using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    public enum RoomKind { Combat, Elite, Investigation, Event, Sanctuary, Boss }

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

        public IReadOnlyList<MapRoom> Rooms => _rooms;

        /// <summary>The number of layers before the boss.</summary>
        public int LayerCount => Boss.Layer;

        public MapRoom Boss => _rooms.First(r => r.Kind == RoomKind.Boss);

        public MapRoom Room(int id) => _rooms.FirstOrDefault(r => r.Id == id);

        public IEnumerable<MapRoom> Layer(int layer) => _rooms.Where(r => r.Layer == layer).OrderBy(r => r.Column);

        public IEnumerable<MapRoom> NextOf(MapRoom room) => room.NextIds.Select(Room);
    }
}

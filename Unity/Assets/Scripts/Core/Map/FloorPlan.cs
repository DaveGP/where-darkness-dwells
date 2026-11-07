using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    /// <summary>A square on the floor plan. X grows east (towards the boss), Y grows south.</summary>
    public readonly struct Cell : IEquatable<Cell>
    {
        public readonly int X, Y;

        public Cell(int x, int y)
        {
            X = x;
            Y = y;
        }

        public bool IsBeside(Cell other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y) == 1;

        public bool Equals(Cell other) => X == other.X && Y == other.Y;
        public override bool Equals(object obj) => obj is Cell c && Equals(c);
        public override int GetHashCode() => X * 397 ^ Y;
        public override string ToString() => $"({X}, {Y})";
    }

    /// <summary>
    /// One room's place on the floor plan: a rectangle, plus one-square passages that carry its doors to the far wall
    /// of its band when the rectangle is narrower than the band. The passages are part of the room.
    /// </summary>
    public sealed class FloorRoom
    {
        internal readonly List<Cell> _passages = new List<Cell>();

        /// <summary>The map room's id, or <see cref="FloorPlan.EntranceId"/>.</summary>
        public readonly int RoomId;
        public readonly int X, Y, Width, Height;
        /// <summary>
        /// A number from the plan's random stream that the view turns into the room's look: which props it has and
        /// where, and its wall features. It changes nothing in the run.
        /// </summary>
        public readonly int Design;

        public IReadOnlyList<Cell> Passages => _passages;

        public IEnumerable<Cell> Cells
        {
            get
            {
                for (int y = Y; y < Y + Height; y++)
                for (int x = X; x < X + Width; x++)
                    yield return new Cell(x, y);
                foreach (Cell c in _passages) yield return c;
            }
        }

        internal FloorRoom(int roomId, int x, int y, int width, int height, int design)
        {
            RoomId = roomId;
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Design = design;
        }
    }

    /// <summary>A door for one link of the map: <see cref="From"/> is the square on the west side, in the room the link leaves.</summary>
    public sealed class FloorDoor
    {
        public readonly int FromId, ToId;
        public readonly Cell From, To;

        internal FloorDoor(int fromId, int toId, Cell from, Cell to)
        {
            FromId = fromId;
            ToId = toId;
            From = from;
            To = to;
        }
    }

    /// <summary>
    /// The map laid out as a floor plan of the Keep, another way of drawing the same <see cref="MapGraph"/>. The
    /// Entrance is on the west edge and the boss on the east. Each layer has its own band of columns, and every link
    /// in the map is one door through the wall between two bands, so rooms that aren't linked never share a door.
    /// The secret room isn't on the plan: it lies beneath it, reached by stairs.
    ///
    /// Rows are found by solving difference constraints (each a "this row is at least that row plus n"): the doors in
    /// one wall run top to bottom in the order of the links, a room covers the rows of all its doors, and rooms in a
    /// band don't overlap. The generator's links never cross, so the constraints never loop and always solve; the
    /// smallest solution keeps the plan compact. The <see cref="Rng"/> only adds slack and picks room sizes and designs.
    ///
    /// Rooms are two or three squares wide and two or more tall, at least as big as their kind needs (<see cref="Smallest"/>). Each band
    /// is as wide as its widest room.
    /// </summary>
    public sealed class FloorPlan
    {
        /// <summary>The <see cref="FloorRoom.RoomId"/> and <see cref="FloorDoor.FromId"/> of the Entrance.</summary>
        public const int EntranceId = -1;
        public const int MaxRoomWidth = 3;

        /// <summary>
        /// The smallest a room of this kind is, in squares, and the widest it can be. The Lair is the biggest room, and
        /// Elites and Sanctuaries are never narrow. A null kind is the Entrance.
        /// </summary>
        public static (int width, int height, int maxWidth) Smallest(RoomKind? kind)
        {
            switch (kind)
            {
                case null: return (2, 2, 2);
                case RoomKind.Boss: return (3, 3, 3);
                case RoomKind.Elite: return (2, 2, 3);
                case RoomKind.Sanctuary: return (2, 2, 2);
                default: return (2, 2, MaxRoomWidth);
            }
        }

        readonly Dictionary<int, FloorRoom> _rooms;

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<FloorDoor> Doors { get; }
        /// <summary>The Entrance first, then the map's rooms in id order.</summary>
        public IEnumerable<FloorRoom> Rooms => _rooms.Values.OrderBy(r => r.RoomId);
        public FloorRoom Entrance => _rooms[EntranceId];

        /// <summary>The room's place on the plan, or null for a room that isn't on it (the secret room).</summary>
        public FloorRoom Room(int roomId) => _rooms.TryGetValue(roomId, out FloorRoom r) ? r : null;

        public FloorDoor Door(int fromId, int toId) => Doors.FirstOrDefault(d => d.FromId == fromId && d.ToId == toId);

        FloorPlan(Dictionary<int, FloorRoom> rooms, List<FloorDoor> doors)
        {
            _rooms = rooms;
            Doors = doors;
            List<Cell> cells = rooms.Values.SelectMany(r => r.Cells).ToList();
            Width = cells.Max(c => c.X) + 1;
            Height = cells.Max(c => c.Y) + 1;
        }

        public static FloorPlan Lay(MapGraph map, Rng rng)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (rng == null) throw new ArgumentNullException(nameof(rng));

            // Band 0 is the Entrance, band b holds layer b - 1, and the last band the boss.
            var bands = new List<List<int>> { new List<int> { EntranceId } };
            for (int layer = 0; layer <= map.LayerCount; layer++) bands.Add(map.Layer(layer).Select(r => r.Id).ToList());

            // The links through the wall after each band, in order from top to bottom: by the room they leave, then
            // the room they reach. Because links never cross, this order is the same seen from either side.
            var walls = new List<List<(int from, int to)>>();
            for (int b = 0; b + 1 < bands.Count; b++)
            {
                List<int> next = bands[b + 1];
                walls.Add(bands[b]
                    .SelectMany(id => (id == EntranceId ? next : map.Room(id).Next).Select(to => (from: id, to)))
                    .OrderBy(l => bands[b].IndexOf(l.from)).ThenBy(l => next.IndexOf(l.to))
                    .ToList());
            }

            // Each room's width, and the height it needs at least: its kind's smallest, sometimes a square more.
            var width = new Dictionary<int, int>();
            var minHeight = new Dictionary<int, int>();
            foreach (int id in bands.SelectMany(b => b))
            {
                (int w, int h, int maxW) = Smallest(id == EntranceId ? (RoomKind?)null : map.Room(id).Kind);
                width[id] = rng.Next(w, maxW + 1);
                minHeight[id] = Math.Max(h, width[id] - 1) + rng.Next(2);
            }

            var solver = new RowSolver();
            var doorRow = walls.Select(w => w.Select(_ => solver.Add()).ToArray()).ToArray();
            var top = new Dictionary<int, int>();
            var bottom = new Dictionary<int, int>();
            for (int w = 0; w < walls.Count; w++)
                for (int k = 1; k < walls[w].Count; k++) solver.AtLeast(doorRow[w][k], doorRow[w][k - 1], 1);
            for (int b = 0; b < bands.Count; b++)
            {
                for (int j = 0; j < bands[b].Count; j++)
                {
                    int id = bands[b][j];
                    int lo = top[id] = solver.Add(), hi = bottom[id] = solver.Add();
                    foreach (int door in DoorsOf(id, b))
                    {
                        solver.AtLeast(door, lo, 0);
                        solver.AtLeast(hi, door, 0);
                    }
                    // A room is as tall as its kind needs, and usually sits right below the one above it.
                    solver.AtLeast(hi, lo, minHeight[id] - 1);
                    if (j > 0) solver.AtLeast(lo, bottom[bands[b][j - 1]], rng.Next(4) == 0 ? 2 : 1);
                }
            }
            solver.Solve();

            IEnumerable<int> DoorsOf(int id, int band)
            {
                if (band > 0)
                    for (int k = 0; k < walls[band - 1].Count; k++)
                        if (walls[band - 1][k].to == id) yield return doorRow[band - 1][k];
                if (band < walls.Count)
                    for (int k = 0; k < walls[band].Count; k++)
                        if (walls[band][k].from == id) yield return doorRow[band][k];
            }

            // Columns: each band is as wide as its widest room. A narrower room stands somewhere across the band.
            // A room's rows are its own in its band, but a room spread over many rows by its doors would be a long
            // strip, so its hall keeps to a compact height and corridors, part of the room, reach the doors beyond.
            var rooms = new Dictionary<int, FloorRoom>();
            int x0 = 0;
            var bandX = new int[bands.Count];
            for (int b = 0; b < bands.Count; b++)
            {
                int bandWidth = bands[b].Max(id => width[id]);
                bandX[b] = x0;
                foreach (int id in bands[b])
                {
                    int w = width[id];
                    int x = x0 + rng.Next(bandWidth - w + 1);
                    int y0 = solver.Value(top[id]), y1 = solver.Value(bottom[id]);
                    // The doors on each side: west ones come in from the band before, east ones lead on.
                    var westDoors = b == 0 ? new List<int>() : Enumerable.Range(0, walls[b - 1].Count)
                        .Where(k => walls[b - 1][k].to == id).Select(k => solver.Value(doorRow[b - 1][k])).ToList();
                    var eastDoors = b == walls.Count ? new List<int>() : Enumerable.Range(0, walls[b].Count)
                        .Where(k => walls[b][k].from == id).Select(k => solver.Value(doorRow[b][k])).ToList();
                    int span = y1 - y0 + 1;
                    int height = Math.Min(span, Math.Max(minHeight[id], w + 2));
                    // The hall sits over the middle of its doors, as far as its rows allow.
                    List<int> all = westDoors.Concat(eastDoors).OrderBy(r => r).ToList();
                    int middle = all.Count == 0 ? (y0 + y1) / 2 : all[all.Count / 2];
                    int hallTop = Math.Max(y0, Math.Min(y1 - height + 1, middle - height / 2));
                    var room = new FloorRoom(id, x, hallTop, w, height, rng.Next(1 << 30));
                    var passages = new HashSet<Cell>();
                    void Corridor(int row, bool west)
                    {
                        // Along the door's row from the band's wall to the hall's side, then up or down beside the hall.
                        bool inHall = row >= hallTop && row < hallTop + height;
                        int side = west ? x : x + w - 1;
                        int from = west ? x0 : (inHall ? x + w : side), to = west ? (inHall ? x - 1 : side) : x0 + bandWidth - 1;
                        for (int c = from; c <= to; c++) passages.Add(new Cell(c, row));
                        if (inHall) return;
                        int step = row < hallTop ? 1 : -1;
                        for (int r = row + step; r < hallTop || r >= hallTop + height; r += step) passages.Add(new Cell(side, r));
                    }
                    foreach (int row in westDoors) Corridor(row, true);
                    foreach (int row in eastDoors) Corridor(row, false);
                    room._passages.AddRange(passages.Where(c => !(c.X >= x && c.X < x + w && c.Y >= hallTop && c.Y < hallTop + height))
                        .OrderBy(c => c.Y).ThenBy(c => c.X));
                    rooms.Add(id, room);
                }
                x0 += bandWidth;
            }

            var doors = new List<FloorDoor>();
            for (int w = 0; w < walls.Count; w++)
                for (int k = 0; k < walls[w].Count; k++)
                {
                    int y = solver.Value(doorRow[w][k]);
                    doors.Add(new FloorDoor(walls[w][k].from, walls[w][k].to, new Cell(bandX[w + 1] - 1, y), new Cell(bandX[w + 1], y)));
                }
            return new FloorPlan(rooms, doors);
        }

        /// <summary>
        /// Smallest non-negative rows that satisfy every "row a is at least row b plus n", found by relaxing the
        /// constraints until nothing moves (a longest-path search).
        /// </summary>
        sealed class RowSolver
        {
            readonly List<int> _rows = new List<int>();
            readonly List<(int a, int b, int gap)> _rules = new List<(int, int, int)>();

            public int Add()
            {
                _rows.Add(0);
                return _rows.Count - 1;
            }

            public void AtLeast(int a, int b, int gap) => _rules.Add((a, b, gap));

            public int Value(int v) => _rows[v];

            public void Solve()
            {
                for (int pass = 0; pass <= _rows.Count; pass++)
                {
                    bool moved = false;
                    foreach ((int a, int b, int gap) in _rules)
                        if (_rows[a] < _rows[b] + gap)
                        {
                            _rows[a] = _rows[b] + gap;
                            moved = true;
                        }
                    if (!moved) return;
                }
                throw new InvalidOperationException("The map's links cross, so it can't be laid out as a floor plan.");
            }
        }
    }
}

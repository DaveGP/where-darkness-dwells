using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// Draws the map as the floor plan of the Keep (<see cref="FloorPlan"/>), the other way of showing the same run as
    /// <see cref="MapView"/>. Only the Entrance and the rooms the party has entered are lit; the rest of the Keep is
    /// dark. The doors the party can open now glow, with what lies behind them written in the dark beyond. The
    /// secret room is a cellar under the plan, reached by stairs from the rooms that lead down to it.
    ///
    /// Rooms are drawn at a readable size, so a large plan scrolls: it centres on the party's room each time they
    /// move, and can be dragged or scrolled to look around. Each room is stone-walled and dressed with props for its
    /// kind, chosen and placed from its <see cref="FloorRoom.Design"/>, so the same seed always looks the same.
    /// </summary>
    static class FloorPlanView
    {
        /// <summary>A square's size when following the party, in reference pixels.</summary>
        const float RoomTile = 84;
        /// <summary>Room below the plan for the cellar, once a clue has revealed it.</summary>
        const float CellarRow = 96;
        const long PulseMs = 800;
        /// <summary>Space kept around the plan's edge, so a door tag at the boss's wall stays visible.</summary>
        const float Margin = 24;
        /// <summary>How far the pointer moves before a press becomes a drag instead of a click.</summary>
        const float DragStart = 6;

        /// <summary>How many rooms the route had at the last draw, so only a newly entered room is laid with a flourish.</summary>
        static int _drawnRoute = -1;
        /// <summary>Where the plan sits in its viewport, kept between draws until the party moves.</summary>
        static Vector2? _pan;

        /// <summary>The props each kind of room is dressed with, from Resources/MapArt. Story-free decoration only.</summary>
        static readonly Dictionary<string, string[]> Props = new Dictionary<string, string[]>
        {
            ["entrance"] = new[] { "banner", "bench", "rubble", "pillar" },
            ["combat"] = new[] { "weapon-rack", "rubble", "bones", "barrels", "pillar" },
            ["elite"] = new[] { "banner", "armour-stand", "chains", "weapon-rack", "pillar" },
            ["investigation"] = new[] { "bookshelf", "desk", "lectern", "barrels" },
            ["event"] = new[] { "table", "rug", "candles", "bench", "lectern" },
            ["sanctuary"] = new[] { "bedroll", "brazier", "shrine", "rug" },
            ["boss"] = new[] { "throne", "altar", "chains", "brazier", "pillar" },
            ["secret"] = new[] { "sarcophagus", "grate", "bones", "candles", "altar" },
        };

        public static string Hint(RunState run) => run.CurrentRoom == null
            ? "You stand in the Entrance. Pick one of its glowing doors."
            : run.CurrentRoom.Kind == RoomKind.Secret ? "Take the stairs back up." : "Pick a glowing door.";

        public const string Legend = "Doors you can open glow, with the room behind them written beyond. Your route is lit and its doors are gold. Drag or scroll to look around.";

        /// <param name="whole">Draw every room and door as if the Keep were explored, dimming the rooms not entered. A debug view.</param>
        public static void Draw(VisualElement canvas, RunState run, Action<MapRoom> onClick, Action<MapRoom> onHover, bool whole = false)
        {
            canvas.Clear();
            FloorPlan plan = run.FloorPlan;
            MapGraph map = run.Map;
            var visited = new HashSet<int>(run.Route);
            var reachable = new HashSet<int>(run.ReachableRooms.Select(r => r.Id));
            int here = run.CurrentRoom?.Id ?? FloorPlan.EntranceId;
            bool IsLit(int id) => id == FloorPlan.EntranceId || visited.Contains(id);
            bool IsShown(int id) => whole || IsLit(id);
            MapRoom secret = run.SecretRevealed || whole ? map.Secret : null;
            // Before its first layout, assume a typical size; the caller draws again once it is measured.
            Rect area = canvas.contentRect;
            bool sized = !float.IsNaN(area.width) && area.width >= 1;
            bool fresh = _drawnRoute != run.Route.Count;
            if (sized) _drawnRoute = run.Route.Count;
            if (fresh) _pan = null;
            float width = sized ? area.width : 1100;
            float height = float.IsNaN(area.height) || area.height < 1 ? 800 : area.height;
            float viewHeight = secret == null ? height : height - CellarRow;
            // Following the party, squares keep their size and the plan scrolls. The whole plan shrinks to fit instead.
            float fit = Mathf.Min((width - 2 * Margin) / plan.Width, (viewHeight - 2 * Margin) / plan.Height);
            float tile = whole ? Mathf.Min(RoomTile, fit) : RoomTile;
            var size = new Vector2(tile * plan.Width + 2 * Margin, tile * plan.Height + 2 * Margin);
            Rect Square(Cell c) => new Rect(Margin + c.X * tile, Margin + c.Y * tile, tile, tile);

            var viewport = new VisualElement();
            viewport.AddToClassList("plan-viewport");
            Place(viewport, new Rect(0, 0, width, viewHeight));
            canvas.Add(viewport);
            var content = new VisualElement { pickingMode = PickingMode.Ignore };
            content.AddToClassList("plan-content");
            Place(content, new Rect(0, 0, size.x, size.y));
            viewport.Add(content);

            var board = new VisualElement { pickingMode = PickingMode.Ignore };
            board.AddToClassList("plan-board");
            Place(board, new Rect(Margin - 4, Margin - 4, tile * plan.Width + 8, tile * plan.Height + 8));
            content.Add(board);

            foreach (FloorRoom room in plan.Rooms.Where(r => IsShown(r.RoomId)))
            {
                MapRoom mapRoom = room.RoomId == FloorPlan.EntranceId ? null : map.Room(room.RoomId);
                string kind = mapRoom == null ? "entrance" : mapRoom.Kind.ToString().ToLowerInvariant();
                bool isHere = room.RoomId == here;
                bool isNew = fresh && isHere && mapRoom != null;
                bool unexplored = !IsLit(room.RoomId);
                // Corridors: each square narrows to a passage, open towards the room's own squares beside it.
                var own = new HashSet<Cell>(room.Cells);
                float inset = tile * 0.24f, rim = Mathf.Max(3, tile * 0.08f);
                foreach (Cell passage in room.Passages)
                {
                    bool n = own.Contains(new Cell(passage.X, passage.Y - 1)), s = own.Contains(new Cell(passage.X, passage.Y + 1));
                    bool wst = own.Contains(new Cell(passage.X - 1, passage.Y)), e = own.Contains(new Cell(passage.X + 1, passage.Y));
                    // A door's square opens through the band's wall too.
                    wst |= plan.Doors.Any(d => d.To.Equals(passage));
                    e |= plan.Doors.Any(d => d.From.Equals(passage));
                    Rect r = Square(passage);
                    var piece = new Rect(r.x + (wst ? 0 : inset), r.y + (n ? 0 : inset),
                        r.width - (wst ? 0 : inset) - (e ? 0 : inset), r.height - (n ? 0 : inset) - (s ? 0 : inset));
                    var hall = Tile($"tile-{kind}", isHere, isNew);
                    hall.AddToClassList("tile-passage");
                    hall.EnableInClassList("is-unexplored", unexplored);
                    Place(hall, piece);
                    Pave(hall, tile);
                    if (!n) Wall(hall, 0, 0, piece.width, rim, tile);
                    if (!s) Wall(hall, 0, piece.height - rim, piece.width, rim, tile);
                    if (!wst) Wall(hall, 0, 0, rim, piece.height, tile);
                    if (!e) Wall(hall, piece.width - rim, 0, rim, piece.height, tile);
                    content.Add(hall);
                }
                float w = room.Width * tile, h = room.Height * tile;
                var body = Tile($"tile-{kind}", isHere, isNew);
                body.EnableInClassList("is-unexplored", unexplored);
                Place(body, new Rect(Margin + room.X * tile, Margin + room.Y * tile, w, h));
                Pave(body, tile);
                Dress(body, kind, room, plan, tile);
                var name = new Label(mapRoom == null ? "Entrance" : MapView.Label(run, mapRoom)) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("tile-name");
                // A tall room one square wide reads its name sideways, so the name has the room's height to fit in.
                bool sideways = room.Width == 1 && room.Height > 1;
                float across = sideways ? h : w;
                name.style.fontSize = Mathf.Clamp(Mathf.Min(tile * 0.24f, (across - 16) / (name.text.Length * 0.6f)), 8, 18);
                if (sideways)
                {
                    Place(name, new Rect((tile - across) / 2, (h - tile) / 2, across, tile));
                    name.style.rotate = new Rotate(-90);
                }
                Views.Icon(body, "room-" + kind, "tile-room-icon");
                body.Add(name);
                if (mapRoom != null && secret != null && (map.SecretFrom.Contains(mapRoom.Id) || secret.Next.Contains(mapRoom.Id)))
                {
                    var stairs = new Label(map.SecretFrom.Contains(mapRoom.Id) ? "Stairs down" : "Stairs up") { pickingMode = PickingMode.Ignore };
                    stairs.AddToClassList("tile-stairs");
                    stairs.style.fontSize = Mathf.Clamp(tile * 0.17f, 9, 13);
                    Views.LabelIcon(stairs, "stairs");
                    body.Add(stairs);
                }
                body.Q(className: "current-mark").BringToFront();
                body.Q(className: "selection-frame").BringToFront();
                content.Add(body);
            }

            // Doors on lit rooms: gold on the route, glowing where the party can go now, shut everywhere else.
            var open = new VisualElement { pickingMode = PickingMode.Ignore };
            open.AddToClassList("map-lines");
            foreach (FloorDoor door in plan.Doors.Where(d => IsShown(d.FromId) || IsShown(d.ToId)))
            {
                bool travelled = IsLit(door.FromId) && visited.Contains(door.ToId);
                bool canOpen = door.FromId == here && reachable.Contains(door.ToId);
                var mark = new VisualElement { pickingMode = PickingMode.Ignore };
                mark.AddToClassList("door");
                Views.Icon(mark, "check", "route-mark");
                mark.EnableInClassList("is-travelled", travelled);
                mark.EnableInClassList("is-open", canOpen);
                Rect from = Square(door.From);
                Place(mark, new Rect(from.xMax - tile * 0.09f, from.y + tile * 0.25f, tile * 0.18f, tile * 0.5f));
                content.Add(mark);
                if (!canOpen) continue;

                MapRoom behind = map.Room(door.ToId);
                var tag = new Button(() => onClick(behind)) { text = MapView.Label(run, behind) };
                tag.AddToClassList("door-tag");
                Views.LabelIcon(tag, "room-" + behind.Kind.ToString().ToLowerInvariant());
                tag.AddToClassList($"tile-{behind.Kind.ToString().ToLowerInvariant()}");
                tag.style.fontSize = Mathf.Clamp(tile * 0.18f, 9, 15);
                Rect to = Square(door.To);
                Place(tag, new Rect(to.x + tile * 0.12f, to.y + tile * 0.14f, Mathf.Max(tile * 1.6f, 112), tile * 0.72f));
                tag.RegisterCallback<PointerEnterEvent>(_ => onHover(behind));
                tag.RegisterCallback<PointerLeaveEvent>(_ => onHover(null));
                open.Add(tag);
            }
            content.Add(open);

            // Where the plan sits: centred when it fits, otherwise on the party's room until they look elsewhere.
            FloorRoom current = plan.Room(here) ?? plan.Entrance;
            Vector2 centre = new Vector2(Margin + (current.X + current.Width / 2f) * tile, Margin + (current.Y + current.Height / 2f) * tile);
            Vector2 Clamp(Vector2 p) => new Vector2(
                size.x <= width ? (width - size.x) / 2 : Mathf.Clamp(p.x, width - size.x, 0),
                size.y <= viewHeight ? (viewHeight - size.y) / 2 : Mathf.Clamp(p.y, viewHeight - size.y, 0));
            void Move(Vector2 p)
            {
                _pan = Clamp(p);
                content.style.translate = new Translate(_pan.Value.x, _pan.Value.y);
            }
            Move(_pan ?? new Vector2(width / 2, viewHeight / 2) - centre);
            LetDrag(viewport, () => _pan ?? Vector2.zero, Move);

            if (secret != null) DrawCellar(canvas, run, secret, new Rect(0, height - CellarRow + 8, width, CellarRow - 8), visited, reachable, here, open, onClick, onHover);
            open.schedule.Execute(() => open.ToggleInClassList("is-pulse")).Every(PulseMs);
        }

        /// <summary>Dragging the plan or turning the wheel moves it. A press that barely moves is still a click.</summary>
        static void LetDrag(VisualElement viewport, Func<Vector2> at, Action<Vector2> move)
        {
            Vector2? pressed = null;
            Vector2 start = Vector2.zero;
            bool dragging = false;
            viewport.RegisterCallback<PointerDownEvent>(e =>
            {
                if (e.button != 0) return;
                pressed = e.position;
                start = at();
                dragging = false;
            }, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (pressed == null) return;
                Vector2 by = (Vector2)e.position - pressed.Value;
                if (!dragging && by.magnitude < DragStart) return;
                if (!dragging)
                {
                    dragging = true;
                    viewport.CapturePointer(e.pointerId);
                    viewport.AddToClassList("is-dragging");
                }
                move(start + by);
            }, TrickleDown.TrickleDown);
            viewport.RegisterCallback<PointerUpEvent>(e =>
            {
                if (dragging)
                {
                    viewport.ReleasePointer(e.pointerId);
                    viewport.RemoveFromClassList("is-dragging");
                    e.StopPropagation();
                }
                pressed = null;
                dragging = false;
            }, TrickleDown.TrickleDown);
            viewport.RegisterCallback<WheelEvent>(e =>
            {
                Vector2 by = e.shiftKey ? new Vector2(e.delta.y, 0) : new Vector2(e.delta.x, e.delta.y);
                move(at() - by * 24);
                e.StopPropagation();
            });
        }

        /// <summary>The secret room, beneath the plan: lit once entered, with stairs down to it and back up.</summary>
        static void DrawCellar(VisualElement canvas, RunState run, MapRoom secret, Rect area, HashSet<int> visited, HashSet<int> reachable,
            int here, VisualElement open, Action<MapRoom> onClick, Action<MapRoom> onHover)
        {
            var title = new Label("Beneath the Keep") { pickingMode = PickingMode.Ignore };
            title.AddToClassList("cellar-title");
            Place(title, new Rect(area.x, area.y, 200, 24));
            canvas.Add(title);

            var cellar = Tile("tile-secret", here == secret.Id, false);
            cellar.EnableInClassList("is-dark", !visited.Contains(secret.Id));
            Place(cellar, new Rect(area.x, area.y + 26, 180, area.height - 30));
            Pave(cellar, area.height - 30);
            var name = new Label(MapView.Label(run, secret)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("tile-name");
            Views.Decoration(cellar, "room-wall");
            Views.Icon(cellar, "room-secret", "tile-room-icon");
            cellar.Add(name);
            canvas.Add(cellar);

            // Down from a room that leads to it, or back up from it. Kept outside the plan's viewport, so they
            // don't scroll away. The cellar's own buttons are added to the canvas, not to the pulsing door layer.
            List<MapRoom> stairs = reachable.Contains(secret.Id) ? new List<MapRoom> { secret }
                : here == secret.Id ? run.ReachableRooms.ToList() : new List<MapRoom>();
            float x = area.x + 196;
            foreach (MapRoom to in stairs)
            {
                MapRoom r = to;
                var button = new Button(() => onClick(r)) { text = r == secret ? $"Stairs down: {MapView.Label(run, r)}" : $"Stairs up: {MapView.Label(run, r)}" };
                button.AddToClassList("door-tag");
                button.AddToClassList("stairs-tag");
                Views.LabelIcon(button, "stairs");
                Place(button, new Rect(x, area.y + 34, 220, 44));
                button.RegisterCallback<PointerEnterEvent>(_ => onHover(r));
                button.RegisterCallback<PointerLeaveEvent>(_ => onHover(null));
                canvas.Add(button);
                x += 232;
            }
        }

        /// <summary>The generated map art in Resources/MapArt, by file name. Original art, committed, so every clone has it.</summary>
        static Dictionary<string, Texture2D> _art;

        static Texture2D Art(string name)
        {
            if (_art == null) _art = Resources.LoadAll<Texture2D>("MapArt").ToDictionary(t => t.name);
            return _art.TryGetValue(name, out Texture2D found) ? found : null;
        }

        /// <summary>Lays the flagstone floor over a tile, two slabs to a square, so floors match in scale whatever the room's shape.</summary>
        static void Pave(VisualElement tile, float square)
        {
            Texture2D floor = Art("floor-flagstones");
            if (floor == null) return;
            tile.style.backgroundImage = floor;
            tile.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            tile.style.backgroundSize = new BackgroundSize(square * 1.5f, square * 1.5f);
            tile.AddToClassList("has-art");
        }

        /// <summary>A strip of stone wall laid over a tile.</summary>
        static void Wall(VisualElement tile, float x, float y, float width, float height, float square)
        {
            var wall = new VisualElement { pickingMode = PickingMode.Ignore };
            wall.AddToClassList("stone-wall");
            Place(wall, new Rect(x, y, width, height));
            Texture2D stone = Art("wall-stone");
            if (stone != null)
            {
                wall.style.backgroundImage = stone;
                wall.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
                wall.style.backgroundSize = new BackgroundSize(square * 0.6f, square * 0.6f);
            }
            tile.Add(wall);
        }

        /// <summary>
        /// Walls the room in stone, with a buttress or alcove or two, and sets a few props for its kind against the
        /// walls, clear of the doors and of the name in the middle. Everything comes from the room's design number.
        /// </summary>
        static void Dress(VisualElement body, string kind, FloorRoom room, FloorPlan plan, float tile)
        {
            var pick = new Rng((ulong)room.Design);
            float w = room.Width * tile, h = room.Height * tile, t = Mathf.Max(4, tile * 0.18f);

            // Squares on the room's edge, each with the wall it backs onto. Squares beside a door are left clear.
            var doorSquares = new HashSet<Cell>(plan.Doors.SelectMany(d => new[] { d.From, d.To }));
            var spots = new List<(Cell cell, int wall)>();
            for (int y = 0; y < room.Height; y++)
            for (int x = 0; x < room.Width; x++)
            {
                var cell = new Cell(room.X + x, room.Y + y);
                if (doorSquares.Contains(cell)) continue;
                if (y == 0) spots.Add((cell, 0));
                else if (x == room.Width - 1) spots.Add((cell, 1));
                else if (y == room.Height - 1) spots.Add((cell, 2));
                else if (x == 0) spots.Add((cell, 3));
            }
            // The middle of the room carries its name, so a room big enough keeps its props to the corners and walls.
            if (room.Width >= 3 && room.Height >= 3)
                spots.RemoveAll(s => s.cell.X == room.X + room.Width / 2 && s.cell.Y == room.Y + room.Height / 2);

            string[] choices = Props.TryGetValue(kind, out string[] list) ? list : new string[0];
            int count = Mathf.Min(spots.Count, choices.Length, 1 + room.Width * room.Height / 3);
            List<string> props = choices.ToList();
            pick.Shuffle(props);
            pick.Shuffle(spots);
            props = props.Take(count).ToList();
            List<(Cell cell, int wall)> places = spots.Take(count).ToList();
            for (int i = 0; i < props.Count; i++)
            {
                Texture2D art = Art("prop-" + props[i]);
                if (art == null) continue;
                (Cell cell, int wallSide) = places[i];
                float s = tile * 0.64f;
                float cx = (cell.X - room.X + 0.5f) * tile, cy = (cell.Y - room.Y + 0.5f) * tile;
                // Pushed back against its wall, and turned to face into the room.
                float back = (tile - s) / 2 - t;
                if (wallSide == 0) cy -= back;
                else if (wallSide == 1) cx += back;
                else if (wallSide == 2) cy += back;
                else cx -= back;
                var prop = new VisualElement { pickingMode = PickingMode.Ignore };
                prop.AddToClassList("tile-prop");
                prop.style.backgroundImage = art;
                prop.style.rotate = new Rotate(wallSide * 90);
                Place(prop, new Rect(cx - s / 2, cy - s / 2, s, s));
                body.Add(prop);
            }

            // Stone walls round the room, then a buttress or two standing out from them.
            Wall(body, 0, 0, w, t, tile);
            Wall(body, 0, h - t, w, t, tile);
            Wall(body, 0, 0, t, h, tile);
            Wall(body, w - t, 0, t, h, tile);
            int buttresses = pick.Next(3);
            for (int i = 0; i < buttresses; i++)
            {
                bool across = pick.Next(2) == 0;
                float along = (pick.Next(room.Width * 2 - 1) + 1) * tile / 2;
                float down = (pick.Next(room.Height * 2 - 1) + 1) * tile / 2;
                if (across) Wall(body, along - t, pick.Next(2) == 0 ? t : h - 2 * t, 2 * t, t, tile);
                else Wall(body, pick.Next(2) == 0 ? t : w - 2 * t, down - t, t, 2 * t, tile);
            }
        }

        static VisualElement Tile(string kindClass, bool isHere, bool isNew)
        {
            var tile = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.AddToClassList("tile");
            Views.Icon(tile, "current", "current-mark");
            Views.Icon(tile, "empty-folio", "unexplored-mark");
            Views.Decoration(tile, "selection-frame");
            tile.AddToClassList(kindClass);
            tile.EnableInClassList("is-current", isHere);
            if (isNew)
            {
                // Laid against the door: starts small and settles into place on the next frame.
                tile.AddToClassList("is-new");
                tile.schedule.Execute(() => tile.RemoveFromClassList("is-new")).StartingIn(30);
            }
            return tile;
        }

        static void Place(VisualElement e, Rect r)
        {
            e.style.position = Position.Absolute;
            e.style.left = r.x;
            e.style.top = r.y;
            e.style.width = r.width;
            e.style.height = r.height;
        }
    }
}

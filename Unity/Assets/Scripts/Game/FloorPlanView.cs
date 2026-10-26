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
    /// </summary>
    static class FloorPlanView
    {
        const float MaxTile = 64;
        /// <summary>Room below the plan for the cellar, once a clue has revealed it.</summary>
        const float CellarRow = 96;
        const long PulseMs = 800;

        /// <summary>How many rooms the route had at the last draw, so only a newly entered room is laid with a flourish.</summary>
        static int _drawnRoute = -1;

        public static string Hint(RunState run) => run.CurrentRoom == null
            ? "You stand in the Entrance. Pick one of its glowing doors."
            : run.CurrentRoom.Kind == RoomKind.Secret ? "Take the stairs back up." : "Pick a glowing door.";

        public const string Legend = "Doors you can open glow, with the room behind them written beyond. Your route is lit and its doors are gold.";

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
            // Fit the plan to the canvas. Before its first layout, assume a typical size; the caller draws again.
            Rect area = canvas.contentRect;
            bool sized = !float.IsNaN(area.width) && area.width >= 1;
            bool fresh = _drawnRoute != run.Route.Count;
            if (sized) _drawnRoute = run.Route.Count;
            float width = float.IsNaN(area.width) || area.width < 1 ? 1100 : area.width;
            float height = float.IsNaN(area.height) || area.height < 1 ? 800 : area.height;
            float planHeight = secret == null ? height : height - CellarRow;
            float tile = Mathf.Min(MaxTile, width / plan.Width, planHeight / plan.Height);
            var origin = new Vector2((width - tile * plan.Width) / 2, (planHeight - tile * plan.Height) / 2);
            Rect Square(Cell c) => new Rect(origin.x + c.X * tile, origin.y + c.Y * tile, tile, tile);

            var board = new VisualElement { pickingMode = PickingMode.Ignore };
            board.AddToClassList("plan-board");
            Place(board, new Rect(origin.x - 4, origin.y - 4, tile * plan.Width + 8, tile * plan.Height + 8));
            canvas.Add(board);

            foreach (FloorRoom room in plan.Rooms.Where(r => IsShown(r.RoomId)))
            {
                MapRoom mapRoom = room.RoomId == FloorPlan.EntranceId ? null : map.Room(room.RoomId);
                string kind = mapRoom == null ? "entrance" : mapRoom.Kind.ToString().ToLowerInvariant();
                bool isHere = room.RoomId == here;
                bool isNew = fresh && isHere && mapRoom != null;
                bool unexplored = !IsLit(room.RoomId);
                foreach (Cell passage in room.Passages)
                {
                    var hall = Tile($"tile-{kind}", isHere, isNew);
                    hall.AddToClassList("tile-passage");
                    hall.EnableInClassList("is-unexplored", unexplored);
                    Rect r = Square(passage);
                    Place(hall, new Rect(r.x, r.y + tile * 0.22f, r.width, r.height * 0.56f));
                    Pave(hall, tile * 2.5f / 6f);
                    canvas.Add(hall);
                }
                var body = Tile($"tile-{kind}", isHere, isNew);
                body.EnableInClassList("is-unexplored", unexplored);
                Place(body, new Rect(origin.x + room.X * tile, origin.y + room.Y * tile, room.Width * tile, room.Height * tile));
                Dress(body, kind, room.RoomId, room.Width * tile, room.Height * tile);
                var name = new Label(mapRoom == null ? "Entrance" : MapView.Label(run, mapRoom)) { pickingMode = PickingMode.Ignore };
                name.AddToClassList("tile-name");
                // A tall room one square wide reads its name sideways, so the name has the room's height to fit in.
                bool sideways = room.Width == 1 && room.Height > 1;
                float across = sideways ? room.Height * tile : room.Width * tile;
                name.style.fontSize = Mathf.Clamp(Mathf.Min(tile * 0.3f, (across - 8) / (name.text.Length * 0.6f)), 8, 18);
                if (sideways)
                {
                    Place(name, new Rect((tile - across) / 2, (room.Height * tile - tile) / 2, across, tile));
                    name.style.rotate = new Rotate(-90);
                }
                body.Add(name);
                if (mapRoom != null && secret != null && (map.SecretFrom.Contains(mapRoom.Id) || secret.Next.Contains(mapRoom.Id)))
                {
                    var stairs = new Label(map.SecretFrom.Contains(mapRoom.Id) ? "Stairs down" : "Stairs up") { pickingMode = PickingMode.Ignore };
                    stairs.AddToClassList("tile-stairs");
                    stairs.style.fontSize = Mathf.Clamp(tile * 0.2f, 9, 13);
                    body.Add(stairs);
                }
                canvas.Add(body);
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
                mark.EnableInClassList("is-travelled", travelled);
                mark.EnableInClassList("is-open", canOpen);
                Rect from = Square(door.From);
                Place(mark, new Rect(from.xMax - tile * 0.09f, from.y + tile * 0.25f, tile * 0.18f, tile * 0.5f));
                canvas.Add(mark);
                if (!canOpen) continue;

                MapRoom behind = map.Room(door.ToId);
                var tag = new Button(() => onClick(behind)) { text = MapView.Label(run, behind) };
                tag.AddToClassList("door-tag");
                tag.AddToClassList($"tile-{behind.Kind.ToString().ToLowerInvariant()}");
                tag.style.fontSize = Mathf.Clamp(tile * 0.2f, 9, 15);
                Rect to = Square(door.To);
                Place(tag, new Rect(to.x + tile * 0.12f, to.y + tile * 0.1f, tile * 1.6f, tile * 0.8f));
                tag.RegisterCallback<PointerEnterEvent>(_ => onHover(behind));
                tag.RegisterCallback<PointerLeaveEvent>(_ => onHover(null));
                open.Add(tag);
            }
            canvas.Add(open);

            if (secret != null) DrawCellar(canvas, run, secret, new Rect(0, height - CellarRow + 8, width, CellarRow - 8), visited, reachable, here, open, onClick, onHover);
            open.schedule.Execute(() => open.ToggleInClassList("is-pulse")).Every(PulseMs);
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
            Dress(cellar, "secret", secret.Id, 180, area.height - 30);
            var name = new Label(MapView.Label(run, secret)) { pickingMode = PickingMode.Ignore };
            name.AddToClassList("tile-name");
            cellar.Add(name);
            canvas.Add(cellar);

            // Down from a room that leads to it, or back up from it.
            List<MapRoom> stairs = reachable.Contains(secret.Id) ? new List<MapRoom> { secret }
                : here == secret.Id ? run.ReachableRooms.ToList() : new List<MapRoom>();
            float x = area.x + 196;
            foreach (MapRoom to in stairs)
            {
                MapRoom r = to;
                var button = new Button(() => onClick(r)) { text = r == secret ? $"Stairs down: {MapView.Label(run, r)}" : $"Stairs up: {MapView.Label(run, r)}" };
                button.AddToClassList("door-tag");
                button.AddToClassList("stairs-tag");
                Place(button, new Rect(x, area.y + 34, 220, 44));
                button.RegisterCallback<PointerEnterEvent>(_ => onHover(r));
                button.RegisterCallback<PointerLeaveEvent>(_ => onHover(null));
                open.Add(button);
                x += 232;
            }
        }

        /// <summary>
        /// The rendered art (Where Darkness Dwells > Render Floor Tiles) by name before the dash: "combat" for combat-1 and
        /// combat-2, and "floor" for the stone floor. It is licensed art, so a fresh clone has none and the tiles stay plain.
        /// </summary>
        static Dictionary<string, Texture2D[]> _art;

        static Texture2D[] Art(string kind)
        {
            if (_art == null)
                _art = Resources.LoadAll<Texture2D>("FloorTiles").GroupBy(t => t.name.Split('-')[0])
                    .ToDictionary(g => g.Key, g => g.OrderBy(t => t.name).ToArray());
            return _art.TryGetValue(kind, out Texture2D[] found) ? found : new Texture2D[0];
        }

        /// <summary>
        /// Lays the stone floor over a tile, one slab to <paramref name="slab"/> pixels, so floors match in scale whatever
        /// the room's shape.
        /// </summary>
        static void Pave(VisualElement tile, float slab)
        {
            Texture2D[] floor = Art("floor");
            if (floor.Length == 0) return;
            tile.style.backgroundImage = floor[0];
            tile.style.backgroundRepeat = new BackgroundRepeat(Repeat.Repeat, Repeat.Repeat);
            tile.style.backgroundSize = new BackgroundSize(slab, slab);
            tile.AddToClassList("has-art");
        }

        /// <summary>
        /// Paves the room and sets its kind's picture in the middle, as wide as the room's narrower side so it is never
        /// stretched or cropped. Rooms of a kind take turns between its pictures by id, so neighbours differ.
        /// </summary>
        static void Dress(VisualElement tile, string kind, int roomId, float width, float height)
        {
            Texture2D[] variants = Art(kind);
            if (variants.Length == 0) return;
            // The pictures show about 6 m of floor, and a slab is 2.5 m.
            float side = Mathf.Min(width, height);
            Pave(tile, side * 2.5f / 6f);
            var picture = new VisualElement { pickingMode = PickingMode.Ignore };
            picture.AddToClassList("tile-art");
            picture.style.backgroundImage = variants[Math.Abs(roomId) % variants.Length];
            Place(picture, new Rect((width - side) / 2 - 2, (height - side) / 2 - 2, side, side));
            tile.Insert(0, picture);
        }

        static VisualElement Tile(string kindClass, bool isHere, bool isNew)
        {
            var tile = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.AddToClassList("tile");
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

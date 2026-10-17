using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>
    /// Draws the floor map from the run's state: layers left to right with the boss at the far right, one button per
    /// room, and the links between them. Rooms the party can enter now are lit; rooms it can no longer reach are dimmed.
    /// </summary>
    static class MapView
    {
        const float Margin = 90;
        const float MaxRoomGap = 190;

        static readonly Color TravelledLine = new Color32(201, 162, 74, 255);
        static readonly Color OpenLine = new Color32(232, 222, 200, 255);
        static readonly Color AheadLine = new Color32(110, 96, 106, 255);
        static readonly Color ClosedLine = new Color32(48, 40, 50, 255);

        public static string Label(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat: return "Fight";
                case RoomKind.Elite: return "Elite";
                case RoomKind.Investigation: return "Investigate";
                case RoomKind.Event: return "Event";
                case RoomKind.Sanctuary: return "Sanctuary";
                default: return "The Lair";
            }
        }

        public static string Help(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat: return "A fight. Win it to choose 1 of 3 cards for the deck.";
                case RoomKind.Elite: return "A harder fight. Win it to choose 1 of 3 cards for the deck.";
                case RoomKind.Investigation: return "Search the room for clues. Clues arrive in Milestone 4; for now the room is empty.";
                case RoomKind.Event: return "A short scene with choices. Events arrive in Milestone 5; for now the room is empty.";
                case RoomKind.Sanctuary: return "Rest to heal everyone 30% of their health, or Reflect to remove a card from the deck.";
                default: return "Whatever waits at the heart of the Keep. Win here to end the expedition.";
            }
        }

        public static void Draw(VisualElement canvas, RunState run, Action<MapRoom> onClick, Action<MapRoom> onHover)
        {
            canvas.Clear();
            MapGraph map = run.Map;
            var reachable = new HashSet<MapRoom>(run.ReachableRooms);
            var visited = new HashSet<int>(run.Route);
            HashSet<MapRoom> ahead = Ahead(run);

            // Spread the rooms over the canvas's measured size. Before its first layout, assume a typical size; the
            // caller draws again when the size is known.
            Rect area = canvas.contentRect;
            float width = float.IsNaN(area.width) || area.width < 1 ? 1500 : area.width;
            float height = float.IsNaN(area.height) || area.height < 1 ? 900 : area.height;
            float roomGap = Math.Min(MaxRoomGap, (height - 80) / (MapGenerator.MaxRoomsPerLayer - 1));
            Vector2 Centre(MapRoom r)
            {
                int count = r.Kind == RoomKind.Boss ? 1 : map.Layer(r.Layer).Count();
                float x = Margin + r.Layer * (width - 2 * Margin) / map.LayerCount;
                float y = height / 2 + (r.Column - (count - 1) / 2f) * roomGap;
                return new Vector2(x, y);
            }

            var lines = new VisualElement { pickingMode = PickingMode.Ignore };
            lines.AddToClassList("map-lines");
            lines.generateVisualContent += context =>
            {
                Painter2D painter = context.painter2D;
                painter.lineWidth = 3;
                foreach (MapRoom from in map.Rooms)
                foreach (MapRoom to in map.NextOf(from))
                {
                    bool travelled = visited.Contains(from.Id) && visited.Contains(to.Id);
                    bool open = from == run.CurrentRoom && reachable.Contains(to);
                    bool stillAhead = (from == run.CurrentRoom || ahead.Contains(from)) && ahead.Contains(to);
                    painter.strokeColor = travelled ? TravelledLine : open ? OpenLine : stillAhead ? AheadLine : ClosedLine;
                    painter.BeginPath();
                    painter.MoveTo(Centre(from));
                    painter.LineTo(Centre(to));
                    painter.Stroke();
                }
            };
            canvas.Add(lines);

            foreach (MapRoom room in map.Rooms)
            {
                MapRoom r = room;
                var button = new Button(() => onClick(r)) { text = Label(r.Kind) };
                button.AddToClassList("room");
                button.AddToClassList($"room-{r.Kind.ToString().ToLowerInvariant()}");
                button.EnableInClassList("is-reachable", reachable.Contains(r));
                button.EnableInClassList("is-visited", visited.Contains(r.Id));
                button.EnableInClassList("is-current", r == run.CurrentRoom);
                button.EnableInClassList("is-closed", !reachable.Contains(r) && !visited.Contains(r.Id) && !ahead.Contains(r));
                float w = r.Kind == RoomKind.Boss ? 150 : 128, h = r.Kind == RoomKind.Boss ? 72 : 56;
                Vector2 c = Centre(r);
                button.style.left = c.x - w / 2;
                button.style.top = c.y - h / 2;
                button.RegisterCallback<PointerEnterEvent>(_ => onHover(r));
                button.RegisterCallback<PointerLeaveEvent>(_ => onHover(null));
                canvas.Add(button);
            }
        }

        /// <summary>Every room the party could still reach from where it stands.</summary>
        static HashSet<MapRoom> Ahead(RunState run)
        {
            var ahead = new HashSet<MapRoom>();
            var open = new Stack<MapRoom>(run.CurrentRoom == null ? run.Map.Layer(0) : run.Map.NextOf(run.CurrentRoom));
            while (open.Count > 0)
            {
                MapRoom room = open.Pop();
                if (!ahead.Add(room)) continue;
                foreach (MapRoom next in run.Map.NextOf(room)) open.Push(next);
            }
            return ahead;
        }
    }
}

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
    /// room, and the links between them. The Entrance sits left of the first layer, where every route starts. Rooms the
    /// party can enter now glow and pulse; rooms it can no longer reach are dimmed. Once a clue reveals the villain's
    /// secret room, it is drawn along the bottom edge under its layer.
    /// </summary>
    static class MapView
    {
        /// <summary>Room on the left for the Entrance, and on the right for the boss.</summary>
        const float Margin = 190;
        const float EntranceX = 64;
        const float EntranceWidth = 104, EntranceHeight = 48;
        /// <summary>How far a reachable room's glow reaches past its edges, and how often it pulses.</summary>
        const float GlowSpread = 10;
        const long PulseMs = 800;
        const float MaxRoomGap = 190;

        static readonly Color TravelledLine = new Color32(201, 162, 74, 255);
        static readonly Color OpenLine = new Color32(232, 222, 200, 255);
        static readonly Color AheadLine = new Color32(110, 96, 106, 255);
        static readonly Color ClosedLine = new Color32(48, 40, 50, 255);
        static readonly Color SecretLine = new Color32(90, 160, 170, 255);
        /// <summary>Room for the secret room under the others, when it is shown.</summary>
        const float SecretRow = 110;

        /// <summary>The room's button text: its kind, or the secret room's own name.</summary>
        public static string Label(RunState run, MapRoom room) =>
            room.Kind == RoomKind.Secret && run.Villain?.SecretRoom != null ? run.Villain.SecretRoom.Name : Label(room.Kind);

        public static string Label(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat: return "Fight";
                case RoomKind.Elite: return "Elite";
                case RoomKind.Investigation: return "Investigate";
                case RoomKind.Event: return "Event";
                case RoomKind.Sanctuary: return "Sanctuary";
                case RoomKind.Secret: return "Hidden room";
                default: return "The Lair";
            }
        }

        public static string Help(RoomKind kind)
        {
            switch (kind)
            {
                case RoomKind.Combat: return "A fight. Win it to choose 1 of 3 cards for the deck.";
                case RoomKind.Elite: return "A harder fight. Win it to choose 1 of 3 cards for the deck.";
                case RoomKind.Investigation: return "Search the room for a clue about who rules the Keep. Searching costs one hero some health.";
                case RoomKind.Event: return "A short scene with choices. Some cost health, some give Keepsakes, cards or clues, and some start a fight.";
                case RoomKind.Sanctuary: return "Rest to heal everyone 30% of their health, or Reflect to remove a card from the deck.";
                case RoomKind.Secret: return "A hidden room your clues led you to. Every room on the layer before leads down to it.";
                default: return "Whatever waits at the heart of the Keep. Win here to end the expedition.";
            }
        }

        public static void Draw(VisualElement canvas, RunState run, Action<MapRoom> onClick, Action<MapRoom> onHover)
        {
            canvas.Clear();
            MapGraph map = run.Map;
            var reachable = new HashSet<MapRoom>(run.ReachableRooms);
            MapRoom secret = run.SecretRevealed ? map.Secret : null;
            var visited = new HashSet<int>(run.Route);
            HashSet<MapRoom> ahead = Ahead(run, run.SecretRevealed);

            // Spread the rooms over the canvas's measured size. Before its first layout, assume a typical size; the
            // caller draws again when the size is known.
            Rect area = canvas.contentRect;
            float width = float.IsNaN(area.width) || area.width < 1 ? 1500 : area.width;
            float fullHeight = float.IsNaN(area.height) || area.height < 1 ? 900 : area.height;
            float height = secret == null ? fullHeight : fullHeight - SecretRow;
            float roomGap = Math.Min(MaxRoomGap, (height - 80) / (MapGenerator.MaxRoomsPerLayer - 1));
            Vector2 Centre(MapRoom r)
            {
                float x = Margin + r.Layer * (width - Margin - 90) / map.LayerCount;
                if (r.Kind == RoomKind.Secret) return new Vector2(x, fullHeight - SecretRow / 2);
                int count = r.Kind == RoomKind.Boss ? 1 : map.Layer(r.Layer).Count();
                float y = height / 2 + (r.Column - (count - 1) / 2f) * roomGap;
                return new Vector2(x, y);
            }
            var shown = secret == null ? map.Rooms.ToList() : map.Rooms.Append(secret).ToList();
            var entrance = new Vector2(EntranceX, height / 2);
            bool atStart = run.CurrentRoom == null;
            MapRoom firstRoom = run.Route.Count > 0 ? map.Room(run.Route[0]) : null;

            var lines = new VisualElement { pickingMode = PickingMode.Ignore };
            lines.AddToClassList("map-lines");
            lines.generateVisualContent += context =>
            {
                Painter2D painter = context.painter2D;
                painter.lineWidth = 3;
                // From the Entrance to the first layer: open at the start, then only the way the party went in.
                foreach (MapRoom to in map.Layer(0))
                {
                    painter.strokeColor = to == firstRoom ? TravelledLine : atStart ? OpenLine : ClosedLine;
                    painter.BeginPath();
                    painter.MoveTo(entrance);
                    painter.LineTo(Centre(to));
                    painter.Stroke();
                }
                foreach (MapRoom from in shown)
                foreach (MapRoom to in map.NextOf(from, withSecret: secret != null))
                {
                    bool travelled = visited.Contains(from.Id) && visited.Contains(to.Id);
                    bool open = from == run.CurrentRoom && reachable.Contains(to);
                    bool stillAhead = (from == run.CurrentRoom || ahead.Contains(from)) && ahead.Contains(to);
                    bool hidden = from == secret || to == secret;
                    painter.strokeColor = travelled ? TravelledLine : open ? OpenLine
                        : hidden && (stillAhead || from == secret) ? SecretLine : stillAhead ? AheadLine : ClosedLine;
                    painter.BeginPath();
                    painter.MoveTo(Centre(from));
                    painter.LineTo(Centre(to));
                    painter.Stroke();
                }
            };
            canvas.Add(lines);

            var start = new Label("Entrance") { pickingMode = PickingMode.Ignore };
            start.AddToClassList("map-entrance");
            start.EnableInClassList("is-start", atStart);
            start.style.left = entrance.x - EntranceWidth / 2;
            start.style.top = entrance.y - EntranceHeight / 2;
            canvas.Add(start);

            // A glow behind each room the party can enter, pulsing so the next step stands out. The pulse is scheduled
            // on the glow layer, so it stops when the next draw clears the canvas.
            var glows = new VisualElement { pickingMode = PickingMode.Ignore };
            glows.AddToClassList("map-lines");
            foreach (MapRoom r in reachable)
            {
                float w = r.Kind == RoomKind.Boss ? 150 : 128, h = r.Kind == RoomKind.Boss ? 72 : 56;
                Vector2 c = Centre(r);
                var glow = new VisualElement { pickingMode = PickingMode.Ignore };
                glow.AddToClassList("room-glow");
                glow.style.left = c.x - w / 2 - GlowSpread;
                glow.style.top = c.y - h / 2 - GlowSpread;
                glow.style.width = w + 2 * GlowSpread;
                glow.style.height = h + 2 * GlowSpread;
                glows.Add(glow);
            }
            glows.schedule.Execute(() => glows.ToggleInClassList("is-pulse")).Every(PulseMs);
            canvas.Add(glows);

            foreach (MapRoom room in shown)
            {
                MapRoom r = room;
                var button = new Button(() => onClick(r)) { text = Label(run, r) };
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
        static HashSet<MapRoom> Ahead(RunState run, bool withSecret)
        {
            var ahead = new HashSet<MapRoom>();
            var open = new Stack<MapRoom>(run.CurrentRoom == null ? run.Map.Layer(0) : run.Map.NextOf(run.CurrentRoom, withSecret));
            while (open.Count > 0)
            {
                MapRoom room = open.Pop();
                if (!ahead.Add(room)) continue;
                foreach (MapRoom next in run.Map.NextOf(room, withSecret)) open.Push(next);
            }
            return ahead;
        }
    }
}

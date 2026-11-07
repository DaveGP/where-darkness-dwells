using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using WhereDarknessDwells.Content;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>Small pieces of interface shared by the combat screen and the run screens.</summary>
    static class Views
    {
        /// <summary>A copy of a layout's root element, such as the "card" element of Card.uxml.</summary>
        public static VisualElement Copy(VisualTreeAsset layout, string rootClass) => layout.Instantiate().Q(className: rootClass);

        public static string OwnerClass(string colour) => $"owner-{(colour ?? "").ToLowerInvariant()}";

        /// <summary>Decorative, non-picking art; the surrounding live label remains authoritative.</summary>
        public static VisualElement Icon(VisualElement parent, string stem, string extraClass = null)
        {
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("ui-icon");
            icon.AddToClassList("icon-" + stem);
            if (extraClass != null) icon.AddToClassList(extraClass);
            parent.Add(icon);
            return icon;
        }

        public static VisualElement Decoration(VisualElement parent, string className)
        {
            var element = new VisualElement { pickingMode = PickingMode.Ignore };
            element.AddToClassList(className);
            parent.Add(element);
            return element;
        }

        public static VisualElement OwnerMark(VisualElement parent, string colour)
        {
            var medal = Decoration(parent, "owner-medal");
            var mark = Decoration(medal, "hero-mark");
            mark.AddToClassList(OwnerClass(colour));
            return medal;
        }

        /// <summary>
        /// The hero's painted bust in a frame of their colour, or their signature mark when the hero has no portrait.
        /// Decorative: the hero's name is always shown beside it.
        /// </summary>
        public static VisualElement Portrait(ContentDatabase content, HeroDef hero, string extraClass)
        {
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList("hero-portrait");
            frame.AddToClassList(extraClass);
            frame.AddToClassList(OwnerClass(hero.Colour));
            Texture2D painting = content.Heroes.FirstOrDefault(h => h.Id == hero.Id)?.Portrait;
            if (painting != null) frame.style.backgroundImage = Background.FromTexture2D(painting);
            else
            {
                frame.AddToClassList("is-missing");
                Decoration(frame, "hero-mark").AddToClassList(OwnerClass(hero.Colour));
            }
            Decoration(frame, "portrait-frame");
            return frame;
        }

        public static void SetEmblem(VisualElement view, string stem)
        {
            var emblem = view.Q(className: "narrative-emblem");
            if (emblem == null) return;
            foreach (string previous in new[] { "seal-whole", "seal-broken", "defeat", "clue", "keepsake", "room-secret" })
                emblem.RemoveFromClassList("icon-" + previous);
            emblem.AddToClassList("icon-" + stem);
        }

        public static void LabelIcon(VisualElement label, string stem)
        {
            label.AddToClassList("with-icon");
            Icon(label, stem, "label-icon");
            // These map captions are static. Lay out their text and icon as one centred group.
            // Live labels elsewhere retain their text binding on the original element.
            if ((label.ClassListContains("door-tag") || label.ClassListContains("tile-stairs"))
                && label is TextElement caption)
            {
                var text = new Label(caption.text) { pickingMode = PickingMode.Ignore };
                text.AddToClassList("tag-caption");
                caption.text = "";
                caption.Add(text);
            }
        }

        /// <summary>A card drawn from Card.uxml: cost, name, target, text, and its owners' colours and names.</summary>
        public static VisualElement Card(VisualTreeAsset layout, CardDef def, ContentLibrary library)
        {
            VisualElement view = Copy(layout, "card");
            view.Q<Label>("cost").text = def.Cost.ToString();
            view.Q<Label>("name").text = def.Name;
            view.Q<Label>("text").text = def.Text;
            view.Q<Label>("target").text = def.Target == CardTarget.Enemy ? "Target an enemy" : def.Target == CardTarget.Ally ? "Target an ally" : "";

            List<HeroDef> owners = def.Owners.Select(library.Hero).ToList();
            view.Q<Label>("owner-names").text = string.Join(" or ", owners.Select(o => o.Name));
            VisualElement swatches = view.Q("owners");
            var marks = view.Q(className: "owner-marks");
            foreach (HeroDef owner in owners)
            {
                var swatch = new VisualElement { pickingMode = PickingMode.Ignore };
                OwnerMark(marks, owner.Colour);
                swatch.AddToClassList("swatch");
                swatch.AddToClassList(OwnerClass(owner.Colour));
                swatches.Add(swatch);
            }
            if (owners.Count == 1) view.AddToClassList(OwnerClass(owners[0].Colour));
            else view.AddToClassList("is-dual");
            return view;
        }
    }
}

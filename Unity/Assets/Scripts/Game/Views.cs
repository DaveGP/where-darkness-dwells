using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Game
{
    /// <summary>Small pieces of interface shared by the combat screen and the run screens.</summary>
    static class Views
    {
        /// <summary>A copy of a layout's root element, such as the "card" element of Card.uxml.</summary>
        public static VisualElement Copy(VisualTreeAsset layout, string rootClass) => layout.Instantiate().Q(className: rootClass);

        public static string OwnerClass(string colour) => $"owner-{(colour ?? "").ToLowerInvariant()}";

        /// <summary>A card drawn from Card.uxml: name, target, text, and its owners' colours and names. Cards are free, so the cost coin is hidden.</summary>
        public static VisualElement Card(VisualTreeAsset layout, CardDef def, ContentLibrary library)
        {
            VisualElement view = Copy(layout, "card");
            view.Q("cost").AddToClassList("hidden");
            view.Q<Label>("name").text = def.Name;
            view.Q<Label>("text").text = def.Text;
            view.Q<Label>("target").text = def.Target == CardTarget.Enemy ? "Target an enemy" : def.Target == CardTarget.Ally ? "Target an ally" : "";

            List<HeroDef> owners = def.Owners.Select(library.Hero).ToList();
            view.Q<Label>("owner-names").text = string.Join(" or ", owners.Select(o => o.Name));
            VisualElement swatches = view.Q("owners");
            foreach (HeroDef owner in owners)
            {
                var swatch = new VisualElement();
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

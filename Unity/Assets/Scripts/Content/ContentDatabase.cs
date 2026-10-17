using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>
    /// Lists every hero, card and enemy asset in the game, and the expedition a run uses, and turns them into the Core
    /// definitions. The game builds it once at startup and refuses to start if anything is wrong.
    /// </summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Content Database", fileName = "ContentDatabase")]
    public sealed class ContentDatabase : ScriptableObject
    {
        [SerializeField] HeroAsset[] heroes = new HeroAsset[0];
        [SerializeField] CardAsset[] cards = new CardAsset[0];
        [SerializeField] EnemyAsset[] enemies = new EnemyAsset[0];
        [SerializeField] ExpeditionAsset expedition;

        public IReadOnlyList<HeroAsset> Heroes => heroes;
        public IReadOnlyList<CardAsset> Cards => cards;
        public IReadOnlyList<EnemyAsset> Enemies => enemies;
        public ExpeditionAsset Expedition => expedition;

        /// <summary>
        /// The definitions, plus every problem found in them, one readable line each. Only use the library when
        /// <paramref name="problems"/> is empty.
        /// </summary>
        public ContentLibrary Build(out List<string> problems)
        {
            problems = new List<string>();
            EmptySlots(heroes, "hero", problems);
            EmptySlots(cards, "card", problems);
            EmptySlots(enemies, "enemy", problems);

            foreach (CardAsset card in cards.Where(c => c != null))
            {
                foreach (HeroAsset owner in card.Owners ?? new HeroAsset[0])
                {
                    if (owner == null) problems.Add($"Card '{card.Id}' ({card.name}) has an empty owner slot.");
                    else if (!heroes.Contains(owner)) problems.Add($"Card '{card.Id}' ({card.name}) belongs to {owner.name}, which isn't in the content database.");
                }
            }

            var library = new ContentLibrary(
                heroes.Where(h => h != null).Select(h => h.ToDef()),
                cards.Where(c => c != null).Select(c => c.ToDef()),
                enemies.Where(e => e != null).Select(e => e.ToDef()));
            problems.AddRange(library.Validate());
            if (expedition == null) problems.Add("The content database has no expedition.");
            else problems.AddRange(library.Validate(expedition.ToDef()));
            return library;
        }

        static void EmptySlots<T>(T[] assets, string kind, List<string> problems) where T : Object
        {
            for (int i = 0; i < assets.Length; i++)
                if (assets[i] == null) problems.Add($"The {kind} list in the content database has an empty slot ({i + 1}).");
        }
    }
}

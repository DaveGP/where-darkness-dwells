using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Hero", fileName = "Hero")]
    public sealed class HeroAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used. Cards and saves refer to the hero by it.")]
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField] string title;
        [Tooltip("The hero's card colour, such as Iron. The combat screen styles it with the USS class owner-iron.")]
        [SerializeField] string colour;
        [SerializeField, Min(1)] int maxHealth = 30;
        [Tooltip("The character shown for this hero. Optional: the Synty prefabs aren't in the repository, so a fresh clone has none and shows a placeholder.")]
        [SerializeField] GameObject model;
        [Tooltip("The hero's painted bust, shown on the party screen, the combat panels, the journal and the run's end. Original art in the repository, so every clone has it.")]
        [SerializeField] Texture2D portrait;

        public string Id => id;
        /// <summary>The hero's character prefab, or null when it has none or the art isn't on this machine.</summary>
        public GameObject Model => model;
        /// <summary>The hero's painted bust, or null when it has none.</summary>
        public Texture2D Portrait => portrait;

        public HeroDef ToDef() => new HeroDef(id, displayName, title, colour, maxHealth);
    }
}

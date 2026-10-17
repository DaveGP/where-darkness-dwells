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

        public string Id => id;

        public HeroDef ToDef() => new HeroDef(id, displayName, title, colour, maxHealth);
    }
}

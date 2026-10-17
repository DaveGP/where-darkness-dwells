using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Card", fileName = "Card")]
    public sealed class CardAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used. Decks and saves refer to the card by it.")]
        [SerializeField] string id;
        [SerializeField] string displayName;
        [Tooltip("One hero, or two for a dual card.")]
        [SerializeField] HeroAsset[] owners = new HeroAsset[1];
        [SerializeField, Min(0)] int cost = 1;
        [SerializeField] CardTarget target;
        [SerializeField, TextArea] string text;
        [Tooltip("Applied in order when the card is played.")]
        [SerializeField] EffectData[] effects = new EffectData[0];

        public string Id => id;
        public HeroAsset[] Owners => owners;

        /// <summary>Empty owner slots are left out here; <see cref="ContentDatabase"/> reports them.</summary>
        public CardDef ToDef() => new CardDef(id, displayName,
            (owners ?? new HeroAsset[0]).Where(o => o != null).Select(o => o.Id), cost, target, text,
            (effects ?? new EffectData[0]).Select(e => e.ToDef()).ToArray());
    }

    [Serializable]
    public sealed class EffectData
    {
        public EffectKind kind;
        public EffectScope scope;
        [Min(0)] public int amount;
        [Tooltip("Only used by Apply Status.")]
        public StatusKind status;
        [Tooltip("Only used by Damage: extra damage when the target is Exposed.")]
        [Min(0)] public int bonusIfExposed;

        public EffectDef ToDef() => new EffectDef(kind, scope, amount, status, bonusIfExposed);
    }
}

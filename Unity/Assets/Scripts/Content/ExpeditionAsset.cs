using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>What a run is built from: the party, the starting deck, the reward cards, the encounter table and the villain.</summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Expedition", fileName = "Expedition")]
    public sealed class ExpeditionAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        [SerializeField] string id;
        [SerializeField] HeroAsset[] party = new HeroAsset[0];
        [Tooltip("One entry per copy of a card.")]
        [SerializeField] CardAsset[] startingDeck = new CardAsset[0];
        [Tooltip("Cards that can be offered after a won fight.")]
        [SerializeField] CardAsset[] rewardCards = new CardAsset[0];
        [SerializeField] EncounterData[] encounters = new EncounterData[0];
        [Tooltip("Who is behind it all. Empty for an expedition without a mystery.")]
        [SerializeField] VillainAsset villain;

        public string Id => id;

        /// <summary>Empty slots are left out here as unknown ids, so validation reports them.</summary>
        public ExpeditionDef ToDef() => new ExpeditionDef(id,
            (party ?? new HeroAsset[0]).Select(h => h != null ? h.Id : "(empty slot)"),
            (startingDeck ?? new CardAsset[0]).Select(c => c != null ? c.Id : "(empty slot)"),
            (rewardCards ?? new CardAsset[0]).Select(c => c != null ? c.Id : "(empty slot)"),
            (encounters ?? new EncounterData[0]).Select(e => e.ToDef()),
            villain != null ? villain.ToDef() : null);
    }

    [Serializable]
    public sealed class EncounterData
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        public string id;
        public EncounterKind kind;
        [Tooltip("The map layers it can appear on, from 0 (the first layer) to 6. Ignored for the boss.")]
        [Min(0)] public int minLayer;
        [Min(0)] public int maxLayer = 6;
        [Tooltip("Left to right. The same enemy can be listed more than once.")]
        public EnemyAsset[] enemies = new EnemyAsset[0];

        public EncounterDef ToDef() => new EncounterDef(id, kind, minLayer, maxLayer,
            (enemies ?? new EnemyAsset[0]).Select(e => e != null ? e.Id : "(empty slot)").ToArray());
    }
}

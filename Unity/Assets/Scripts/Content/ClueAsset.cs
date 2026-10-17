using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>One clue about a villain: where it can be found, what searching for it costs, and what it changes.</summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Clue", fileName = "Clue")]
    public sealed class ClueAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        [SerializeField] string id;
        [SerializeField] string displayName;
        [Tooltip("Where it can be found. More than one can be ticked.")]
        [SerializeField] ClueSource sources = ClueSource.Investigation;
        [Tooltip("The map layers an Investigation can hold it on, from 0 (the first layer) to 6.")]
        [SerializeField, Min(0)] int minLayer;
        [SerializeField, Min(0)] int maxLayer = 6;
        [Tooltip("Health the searching hero loses.")]
        [SerializeField, Min(0)] int searchCost = 3;
        [Tooltip("The Investigation room before searching, under 60 words.")]
        [SerializeField, TextArea(3, 6)] string scene;
        [Tooltip("The search button, such as \"Force the drawer\".")]
        [SerializeField] string searchLabel;
        [Tooltip("The journal entry: what the clue says and what it changes.")]
        [SerializeField, TextArea(3, 6)] string journal;
        [SerializeField] ClueEffectData[] effects = new ClueEffectData[0];

        public string Id => id;

        public ClueDef ToDef() => new ClueDef(id, displayName, sources, minLayer, maxLayer, searchCost, scene, searchLabel, journal,
            (effects ?? new ClueEffectData[0]).Select(e => e.ToDef()).ToArray());
    }

    [Serializable]
    public sealed class ClueEffectData
    {
        public ClueEffectKind kind;
        [Tooltip("Only used by Bonus Damage To Boss.")]
        [Min(0)] public int amount;
        [Tooltip("Only used by Cancel Boss Intent: the kind of intent it stops.")]
        public IntentKind cancelKind;
        [Tooltip("Only used by Cancel Boss Intent: the button text, such as \"Speak his name\".")]
        public string label;

        public ClueEffect ToDef() => new ClueEffect(kind, amount, cancelKind, string.IsNullOrEmpty(label) ? null : label);
    }
}

using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>A short scene in an Event room, and its choices.</summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Event", fileName = "Event")]
    public sealed class EventAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        [SerializeField] string id;
        [SerializeField] string title;
        [Tooltip("The scene, under 60 words.")]
        [SerializeField, TextArea(3, 6)] string scene;
        [SerializeField] EventChoiceData[] choices = new EventChoiceData[0];

        public string Id => id;

        public EventDef ToDef() => new EventDef(id, title, scene, (choices ?? new EventChoiceData[0]).Select(c => c.ToDef()).ToArray());
    }

    [Serializable]
    public sealed class EventChoiceData
    {
        [Tooltip("The button, such as \"Loosen his chains\".")]
        public string label;
        [Tooltip("What happens, shown after choosing.")]
        [TextArea(2, 4)] public string result;
        [Tooltip("The choice only appears once this clue is found. Empty for always.")]
        public ClueAsset requiresClue;
        [Tooltip("The choice disappears once this clue is found. Empty for never.")]
        public ClueAsset hiddenByClue;
        public EventOutcomeData[] outcomes = new EventOutcomeData[0];

        public EventChoice ToDef() => new EventChoice(label, result, requiresClue != null ? requiresClue.Id : null,
            hiddenByClue != null ? hiddenByClue.Id : null, (outcomes ?? new EventOutcomeData[0]).Select(o => o.ToDef()).ToArray());
    }

    [Serializable]
    public sealed class EventOutcomeData
    {
        public EventOutcomeKind kind;
        [Tooltip("Health healed or lost, or maximum health gained. Unused by the other kinds.")]
        [Min(0)] public int amount;
        [Tooltip("Gain Clue: the clue's id. Gain Keepsake: the Keepsake's id, or empty for a random one. " +
                 "Fight: the encounter's id, or empty for a normal fight on this layer. Unused by the other kinds.")]
        public string id;

        public EventOutcome ToDef() => new EventOutcome(kind, amount, string.IsNullOrEmpty(id) ? null : id);
    }
}

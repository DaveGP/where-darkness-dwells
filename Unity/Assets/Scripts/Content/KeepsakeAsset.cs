using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    /// <summary>A passive item the party keeps for the rest of the run, given by an Elite or an event.</summary>
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Keepsake", fileName = "Keepsake")]
    public sealed class KeepsakeAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used. Events and saves refer to the Keepsake by it.")]
        [SerializeField] string id;
        [SerializeField] string displayName;
        [Tooltip("What it does, as the player reads it.")]
        [SerializeField, TextArea] string text;
        [SerializeField] KeepsakeEffectKind kind;
        [SerializeField, Min(0)] int amount = 1;
        [Tooltip("Only used by Enemy Start Status.")]
        [SerializeField] StatusKind status;

        public string Id => id;

        public KeepsakeDef ToDef() => new KeepsakeDef(id, displayName, text, kind, amount, status);
    }
}

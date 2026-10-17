using System;
using System.Linq;
using UnityEngine;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Content
{
    [CreateAssetMenu(menuName = "Where Darkness Dwells/Enemy", fileName = "Enemy")]
    public sealed class EnemyAsset : ScriptableObject
    {
        [Tooltip("Unique, lower case, never changed once used.")]
        [SerializeField] string id;
        [SerializeField] string displayName;
        [SerializeField, Min(1)] int maxHealth = 20;
        [Tooltip("The enemy's actions, used in order and then repeated.")]
        [SerializeField] IntentData[] intents = new IntentData[0];

        public string Id => id;

        public EnemyDef ToDef() => new EnemyDef(id, displayName, maxHealth,
            (intents ?? new IntentData[0]).Select(i => i.ToDef()).ToArray());
    }

    [Serializable]
    public sealed class IntentData
    {
        [Tooltip("Shown above the enemy, such as \"Strike 6\".")]
        public string description;
        public IntentKind kind;
        public IntentTarget target;
        [Min(0)] public int amount;
        [Tooltip("Only used by Apply Status.")]
        public StatusKind status;
        [Tooltip("What the party sees instead while the enemy is hidden from them, such as \"He drinks\". Empty for most enemies.")]
        public string vague;

        public IntentDef ToDef() => new IntentDef(kind, target, amount, description, status, string.IsNullOrEmpty(vague) ? null : vague);
    }
}

using System.Collections.Generic;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// How the run changes one fight: what clues have revealed about an enemy and what they let the party do. Every
    /// table is keyed by enemy definition id. A fight with no modifiers plays by the plain rules.
    /// </summary>
    public sealed class CombatModifiers
    {
        static readonly Dictionary<string, string> NoNames = new Dictionary<string, string>();
        static readonly Dictionary<string, int> NoNumbers = new Dictionary<string, int>();

        /// <summary>A name to show instead of the enemy's own, such as "The Steward".</summary>
        public readonly IReadOnlyDictionary<string, string> Names;
        /// <summary>Enemies whose intents show their vague text, with no numbers.</summary>
        public readonly IReadOnlyCollection<string> VagueIntents;
        /// <summary>Extra damage every party attack deals to the enemy, added before Weak and Exposed.</summary>
        public readonly IReadOnlyDictionary<string, int> BonusDamageTo;
        /// <summary>The share of its health the enemy starts without.</summary>
        public readonly IReadOnlyDictionary<string, int> HealthLossPercent;
        /// <summary>A once-per-fight ability to cancel one intent, or null.</summary>
        public readonly CancelAbility Cancel;
        /// <summary>The party's Keepsakes. The fight applies the ones that act during a fight.</summary>
        public readonly IReadOnlyList<KeepsakeDef> Keepsakes;
        /// <summary>Maximum health a hero has gained in the run, keyed by hero id.</summary>
        public readonly IReadOnlyDictionary<string, int> MaxHealthBonus;

        public CombatModifiers(IReadOnlyDictionary<string, string> names = null, IReadOnlyCollection<string> vagueIntents = null,
            IReadOnlyDictionary<string, int> bonusDamageTo = null, IReadOnlyDictionary<string, int> healthLossPercent = null,
            CancelAbility cancel = null, IEnumerable<KeepsakeDef> keepsakes = null, IReadOnlyDictionary<string, int> maxHealthBonus = null)
        {
            MaxHealthBonus = maxHealthBonus ?? NoNumbers;
            Keepsakes = keepsakes == null ? new KeepsakeDef[0] : new List<KeepsakeDef>(keepsakes).ToArray();
            Names = names ?? NoNames;
            VagueIntents = vagueIntents ?? new string[0];
            BonusDamageTo = bonusDamageTo ?? NoNumbers;
            HealthLossPercent = healthLossPercent ?? NoNumbers;
            Cancel = cancel;
        }

        public static readonly CombatModifiers None = new CombatModifiers();

        /// <summary>The total Amount of the party's Keepsakes of one kind.</summary>
        public int Keepsake(KeepsakeEffectKind kind)
        {
            int total = 0;
            foreach (KeepsakeDef k in Keepsakes) if (k.Kind == kind) total += k.Amount;
            return total;
        }
    }

    /// <summary>Lets the party cancel an intent of one kind from one enemy, a set number of times per fight.</summary>
    public sealed class CancelAbility
    {
        /// <summary>The button text, such as "Speak his name".</summary>
        public readonly string Label;
        public readonly string EnemyId;
        public readonly IntentKind Kind;
        public readonly int Uses;

        public CancelAbility(string label, string enemyId, IntentKind kind, int uses = 1)
        {
            Label = label;
            EnemyId = enemyId;
            Kind = kind;
            Uses = uses;
        }
    }
}

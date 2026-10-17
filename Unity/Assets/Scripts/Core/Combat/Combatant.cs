using System.Collections.Generic;

namespace WhereDarknessDwells.Core
{
    /// <summary>A hero or an enemy in a fight. Only <see cref="CombatState"/> changes it; everything else reads it.</summary>
    public sealed class Combatant
    {
        readonly Dictionary<StatusKind, int> _statuses = new Dictionary<StatusKind, int>();

        public readonly string Id;
        public readonly string Name;
        /// <summary>Set for heroes, null for enemies.</summary>
        public readonly HeroDef Hero;
        /// <summary>Set for enemies, null for heroes.</summary>
        public readonly EnemyDef Enemy;
        public readonly int MaxHealth;

        public int Health { get; internal set; }
        public int Block { get; internal set; }

        /// <summary>For a hero: the hero currently guarding them, whose Block and health take attacks aimed here.</summary>
        public string GuardedBy { get; internal set; }

        /// <summary>For an enemy: the action it will take this round and who it's aimed at (null for all heroes or itself).</summary>
        public IntentDef Intent { get; internal set; }
        public string IntentTargetId { get; internal set; }
        internal int IntentStep;
        /// <summary>For an enemy: the party cancelled the intent it is showing, so it does nothing when it acts.</summary>
        public bool IntentCancelled { get; internal set; }

        Combatant(string id, string name, HeroDef hero, EnemyDef enemy, int maxHealth, int health)
        {
            Id = id;
            Name = name;
            Hero = hero;
            Enemy = enemy;
            MaxHealth = maxHealth;
            Health = health;
        }

        internal static Combatant ForHero(HeroDef hero, int health) => new Combatant(hero.Id, hero.Name, hero, null, hero.MaxHealth, health);

        internal static Combatant ForEnemy(EnemyDef enemy, int index, string name, int health) =>
            new Combatant($"{enemy.Id}#{index}", name ?? enemy.Name, null, enemy, enemy.MaxHealth, health);

        internal Combatant Copy()
        {
            var copy = new Combatant(Id, Name, Hero, Enemy, MaxHealth, Health)
            {
                Block = Block,
                GuardedBy = GuardedBy,
                Intent = Intent,
                IntentTargetId = IntentTargetId,
                IntentStep = IntentStep,
                IntentCancelled = IntentCancelled,
            };
            foreach (KeyValuePair<StatusKind, int> s in _statuses) copy._statuses[s.Key] = s.Value;
            return copy;
        }

        public bool IsHero => Hero != null;

        /// <summary>A hero at 0 health is down; an enemy at 0 health is defeated. Either way they take no further part.</summary>
        public bool IsDown => Health <= 0;

        public int Status(StatusKind kind) => _statuses.TryGetValue(kind, out int n) ? n : 0;

        public IReadOnlyDictionary<StatusKind, int> Statuses => _statuses;

        internal void SetStatus(StatusKind kind, int stacks)
        {
            if (stacks <= 0) _statuses.Remove(kind);
            else _statuses[kind] = stacks;
        }

        public override string ToString() => Name;
    }
}

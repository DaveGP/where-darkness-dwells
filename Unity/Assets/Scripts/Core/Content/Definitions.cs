using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    // Content definitions: read-only descriptions of heroes, cards and enemies. They never change during a run;
    // run and combat state refer to them by id. In Unity they are built from ScriptableObject assets.

    /// <summary>Conditions that come in stacks. Block and Guarded are tracked separately because they work differently.</summary>
    public enum StatusKind { Exposed, Weak, Bleed, Hex }

    public sealed class HeroDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly string Title;
        /// <summary>The hero's card colour, such as "Iron". The interface maps it to an actual colour.</summary>
        public readonly string Colour;
        public readonly int MaxHealth;

        public HeroDef(string id, string name, string title, string colour, int maxHealth)
        {
            Id = id;
            Name = name;
            Title = title;
            Colour = colour;
            MaxHealth = maxHealth;
        }

        public override string ToString() => Name;
    }

    /// <summary>What a card is aimed at when played.</summary>
    public enum CardTarget { None, Enemy, Ally }

    public enum EffectKind
    {
        /// <summary>Deal Amount damage, plus BonusIfExposed when the target is Exposed.</summary>
        Damage,
        Block,
        ApplyStatus,
        /// <summary>Attacks aimed at the target hit the acting hero instead, until the next player turn.</summary>
        Guard,
        GainResolve,
        Draw,
        /// <summary>Remove all Hex from the target and deal Amount damage per stack removed.</summary>
        ConsumeHex,
    }

    /// <summary>Who an effect lands on.</summary>
    public enum EffectScope
    {
        /// <summary>The card's chosen target.</summary>
        Target,
        AllEnemies,
        /// <summary>The hero playing the card.</summary>
        ActingHero,
    }

    public sealed class EffectDef
    {
        public readonly EffectKind Kind;
        public readonly EffectScope Scope;
        public readonly int Amount;
        public readonly StatusKind Status;
        public readonly int BonusIfExposed;

        public EffectDef(EffectKind kind, EffectScope scope, int amount, StatusKind status = default, int bonusIfExposed = 0)
        {
            Kind = kind;
            Scope = scope;
            Amount = amount;
            Status = status;
            BonusIfExposed = bonusIfExposed;
        }

        public static EffectDef Damage(int amount, int bonusIfExposed = 0) => new EffectDef(EffectKind.Damage, EffectScope.Target, amount, bonusIfExposed: bonusIfExposed);
        public static EffectDef DamageAll(int amount) => new EffectDef(EffectKind.Damage, EffectScope.AllEnemies, amount);
        public static EffectDef Block(int amount, EffectScope scope = EffectScope.Target) => new EffectDef(EffectKind.Block, scope, amount);
        public static EffectDef Apply(StatusKind status, int stacks, EffectScope scope = EffectScope.Target) => new EffectDef(EffectKind.ApplyStatus, scope, stacks, status);
        public static EffectDef Guard() => new EffectDef(EffectKind.Guard, EffectScope.Target, 0);
        public static EffectDef GainResolve(int amount) => new EffectDef(EffectKind.GainResolve, EffectScope.ActingHero, amount);
        public static EffectDef Draw(int cards) => new EffectDef(EffectKind.Draw, EffectScope.ActingHero, cards);
        public static EffectDef ConsumeHex(int damagePerStack) => new EffectDef(EffectKind.ConsumeHex, EffectScope.Target, damagePerStack);
    }

    public sealed class CardDef
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>One hero id, or two for a dual card. The card can be played while any owner is active.</summary>
        public readonly IReadOnlyList<string> Owners;
        public readonly int Cost;
        public readonly CardTarget Target;
        public readonly IReadOnlyList<EffectDef> Effects;
        /// <summary>The rules text printed on the card.</summary>
        public readonly string Text;

        public CardDef(string id, string name, IEnumerable<string> owners, int cost, CardTarget target, string text, params EffectDef[] effects)
        {
            Id = id;
            Name = name;
            Owners = (owners ?? Enumerable.Empty<string>()).ToArray();
            Cost = cost;
            Target = target;
            Text = text;
            Effects = effects ?? new EffectDef[0];
        }

        public bool IsDual => Owners.Count == 2;
        public bool IsOwnedBy(string heroId) => Owners.Contains(heroId);

        public override string ToString() => Name;
    }

    public enum IntentKind { Attack, Block, ApplyStatus }

    /// <summary>Who an enemy's action is aimed at. The target is chosen when the intent is shown, so the player sees it.</summary>
    public enum IntentTarget { RandomHero, HealthiestHero, AllHeroes, Self }

    public sealed class IntentDef
    {
        public readonly IntentKind Kind;
        public readonly IntentTarget Target;
        public readonly int Amount;
        public readonly StatusKind Status;
        /// <summary>Short text shown above the enemy, such as "Strike 7".</summary>
        public readonly string Description;

        public IntentDef(IntentKind kind, IntentTarget target, int amount, string description, StatusKind status = default)
        {
            Kind = kind;
            Target = target;
            Amount = amount;
            Description = description;
            Status = status;
        }

        public static IntentDef Attack(int amount, IntentTarget target, string description) => new IntentDef(IntentKind.Attack, target, amount, description);
        public static IntentDef Block(int amount, string description) => new IntentDef(IntentKind.Block, IntentTarget.Self, amount, description);
        public static IntentDef Apply(StatusKind status, int stacks, IntentTarget target, string description) => new IntentDef(IntentKind.ApplyStatus, target, stacks, description, status);
    }

    public sealed class EnemyDef
    {
        public readonly string Id;
        public readonly string Name;
        public readonly int MaxHealth;
        /// <summary>The enemy's actions, used in order and then repeated.</summary>
        public readonly IReadOnlyList<IntentDef> Intents;

        public EnemyDef(string id, string name, int maxHealth, params IntentDef[] intents)
        {
            Id = id;
            Name = name;
            MaxHealth = maxHealth;
            Intents = intents ?? new IntentDef[0];
        }

        public override string ToString() => Name;
    }

    public enum EncounterKind { Normal, Elite, Boss }

    /// <summary>A group of enemies that can be met in one room, and how deep in the Keep it can appear.</summary>
    public sealed class EncounterDef
    {
        public readonly string Id;
        public readonly EncounterKind Kind;
        /// <summary>The map layers it can appear on, counting the first layer as 0. Ignored for the boss.</summary>
        public readonly int MinLayer;
        public readonly int MaxLayer;
        /// <summary>The enemies, left to right. The same enemy can appear more than once.</summary>
        public readonly IReadOnlyList<string> EnemyIds;

        public EncounterDef(string id, EncounterKind kind, int minLayer, int maxLayer, params string[] enemyIds)
        {
            Id = id;
            Kind = kind;
            MinLayer = minLayer;
            MaxLayer = maxLayer;
            EnemyIds = enemyIds ?? new string[0];
        }

        public bool FitsLayer(int layer) => layer >= MinLayer && layer <= MaxLayer;

        public override string ToString() => Id;
    }

    /// <summary>
    /// Everything a run is built from: the party, the starting deck, the cards that can be offered as rewards, and
    /// the encounters for each kind of room. Later villains add their own boss and clues here.
    /// </summary>
    public sealed class ExpeditionDef
    {
        public readonly string Id;
        public readonly IReadOnlyList<string> Party;
        /// <summary>Card ids, one entry per copy.</summary>
        public readonly IReadOnlyList<string> StartingDeck;
        public readonly IReadOnlyList<string> RewardCards;
        public readonly IReadOnlyList<EncounterDef> Encounters;

        public ExpeditionDef(string id, IEnumerable<string> party, IEnumerable<string> startingDeck,
            IEnumerable<string> rewardCards, IEnumerable<EncounterDef> encounters)
        {
            Id = id;
            Party = (party ?? Enumerable.Empty<string>()).ToArray();
            StartingDeck = (startingDeck ?? Enumerable.Empty<string>()).ToArray();
            RewardCards = (rewardCards ?? Enumerable.Empty<string>()).ToArray();
            Encounters = (encounters ?? Enumerable.Empty<EncounterDef>()).ToArray();
        }

        public EncounterDef Encounter(string id) => Encounters.FirstOrDefault(e => e.Id == id);

        public EncounterDef Boss => Encounters.FirstOrDefault(e => e.Kind == EncounterKind.Boss);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    // Content definitions: read-only descriptions of heroes, cards and enemies. They never change during a run;
    // run and combat state refer to them by id. In Unity they are built from ScriptableObject assets.

    /// <summary>
    /// Conditions that come in stacks. Block and Guarded are tracked separately because they work differently.
    /// Riposte is a hero's: when an enemy's attack hits them, the attacker takes that much damage. It is removed
    /// at the start of the next player turn, like Block. Fury is an enemy's: each of its attacks deals that much
    /// more damage, and like Hex it never fades.
    /// </summary>
    public enum StatusKind { Exposed, Weak, Bleed, Hex, Riposte, Fury }

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
        /// <summary>Unused while cards are free: there is no Resolve. Kept so the kinds after it keep their numbers in saved assets.</summary>
        GainResolve,
        Draw,
        /// <summary>Remove all Hex from the target and deal Amount damage per stack removed.</summary>
        ConsumeHex,
        /// <summary>Heal Amount, never past maximum health.</summary>
        Heal,
        /// <summary>Remove every stack of Status.</summary>
        Cleanse,
        /// <summary>The acting hero loses Amount health, ignoring Block. It never drops them below 1.</summary>
        LoseHealth,
    }

    /// <summary>Who an effect lands on.</summary>
    public enum EffectScope
    {
        /// <summary>The card's chosen target.</summary>
        Target,
        AllEnemies,
        /// <summary>The hero playing the card.</summary>
        ActingHero,
        /// <summary>Every standing hero, the acting one included.</summary>
        AllHeroes,
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
        public static EffectDef Heal(int amount, EffectScope scope = EffectScope.Target) => new EffectDef(EffectKind.Heal, scope, amount);
        public static EffectDef Cleanse(StatusKind status, EffectScope scope = EffectScope.Target) => new EffectDef(EffectKind.Cleanse, scope, 0, status);
        public static EffectDef LoseHealth(int amount) => new EffectDef(EffectKind.LoseHealth, EffectScope.ActingHero, amount);
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

    public enum IntentKind { Attack, Block, ApplyStatus, Heal }

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
        /// <summary>What the party sees instead while they don't understand this enemy yet, such as "Blood gathers". Optional.</summary>
        public readonly string Vague;

        public IntentDef(IntentKind kind, IntentTarget target, int amount, string description, StatusKind status = default, string vague = null)
        {
            Kind = kind;
            Target = target;
            Amount = amount;
            Description = description;
            Status = status;
            Vague = vague;
        }

        /// <summary>The same action, with the text shown while it is vague.</summary>
        public IntentDef Vaguely(string vague) => new IntentDef(Kind, Target, Amount, Description, Status, vague);

        public static IntentDef Attack(int amount, IntentTarget target, string description) => new IntentDef(IntentKind.Attack, target, amount, description);
        public static IntentDef Block(int amount, string description) => new IntentDef(IntentKind.Block, IntentTarget.Self, amount, description);
        public static IntentDef Apply(StatusKind status, int stacks, IntentTarget target, string description) => new IntentDef(IntentKind.ApplyStatus, target, stacks, description, status);
        /// <summary>The enemy restores this much of its own health.</summary>
        public static IntentDef Heal(int amount, string description) => new IntentDef(IntentKind.Heal, IntentTarget.Self, amount, description);
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

    public enum KeepsakeEffectKind
    {
        /// <summary>The hand is refilled to Amount more cards at the start of each turn.</summary>
        LargerHand,
        /// <summary>Every hero starts each fight with Amount Block.</summary>
        StartBlock,
        /// <summary>After each won fight, every hero who is still standing heals Amount.</summary>
        HealAfterWin,
        /// <summary>At the start of each fight, every enemy gets Amount stacks of Status.</summary>
        EnemyStartStatus,
        /// <summary>The active hero may discard Amount more cards each activation.</summary>
        ExtraDiscard,
        /// <summary>Draw Amount more cards on the first turn of each fight.</summary>
        FirstTurnDraw,
        /// <summary>Every hero's maximum health goes up by Amount, and they heal Amount when it is found.</summary>
        MaxHealth,
    }

    /// <summary>A passive item the party keeps for the rest of the run. Its one effect is a kind Core understands.</summary>
    public sealed class KeepsakeDef
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>What it does, as the player reads it.</summary>
        public readonly string Text;
        public readonly KeepsakeEffectKind Kind;
        public readonly int Amount;
        /// <summary>Only used by <see cref="KeepsakeEffectKind.EnemyStartStatus"/>.</summary>
        public readonly StatusKind Status;

        public KeepsakeDef(string id, string name, string text, KeepsakeEffectKind kind, int amount, StatusKind status = default)
        {
            Id = id;
            Name = name;
            Text = text;
            Kind = kind;
            Amount = amount;
            Status = status;
        }

        public override string ToString() => Name;
    }

    /// <summary>
    /// Everything a run is built from: the party, the starting deck, the cards that can be offered as rewards, the
    /// encounters for each kind of room, the Keepsakes and events that can turn up, and the villain behind it all.
    /// </summary>
    public sealed class ExpeditionDef
    {
        public readonly string Id;
        /// <summary>The heroes the party is chosen from, in the order the party screen shows them.</summary>
        public readonly IReadOnlyList<string> Heroes;
        /// <summary>
        /// Every starting card, one entry per copy. A run's deck takes the chosen heroes' own cards and the dual
        /// cards whose owners are all in the party (see <see cref="RunState.StartingCards"/>).
        /// </summary>
        public readonly IReadOnlyList<string> StartingDeck;
        public readonly IReadOnlyList<string> RewardCards;
        public readonly IReadOnlyList<EncounterDef> Encounters;
        /// <summary>The villain behind this expedition: its clues and what they change. Null for a run without a mystery.</summary>
        public readonly VillainDef Villain;
        /// <summary>The Keepsakes Elites and events can give, each at most once a run.</summary>
        public readonly IReadOnlyList<KeepsakeDef> Keepsakes;
        /// <summary>The events Event rooms draw from, each at most once a run.</summary>
        public readonly IReadOnlyList<EventDef> Events;

        public ExpeditionDef(string id, IEnumerable<string> heroes, IEnumerable<string> startingDeck,
            IEnumerable<string> rewardCards, IEnumerable<EncounterDef> encounters, VillainDef villain = null,
            IEnumerable<KeepsakeDef> keepsakes = null, IEnumerable<EventDef> events = null)
        {
            Villain = villain;
            Keepsakes = (keepsakes ?? Enumerable.Empty<KeepsakeDef>()).ToArray();
            Events = (events ?? Enumerable.Empty<EventDef>()).ToArray();
            Id = id;
            Heroes = (heroes ?? Enumerable.Empty<string>()).ToArray();
            StartingDeck = (startingDeck ?? Enumerable.Empty<string>()).ToArray();
            RewardCards = (rewardCards ?? Enumerable.Empty<string>()).ToArray();
            Encounters = (encounters ?? Enumerable.Empty<EncounterDef>()).ToArray();
        }

        public EncounterDef Encounter(string id) => Encounters.FirstOrDefault(e => e.Id == id);

        public KeepsakeDef Keepsake(string id) => Keepsakes.FirstOrDefault(k => k.Id == id);

        public EncounterDef Boss => Encounters.FirstOrDefault(e => e.Kind == EncounterKind.Boss);
    }
}

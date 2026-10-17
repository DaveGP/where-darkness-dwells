using System.Collections.Generic;

namespace WhereDarknessDwells.Core
{
    public enum CombatEventKind
    {
        RoundStarted,
        CardsDrawn,
        DeckShuffled,
        HeroActivated,
        ResolveChanged,
        CardPlayed,
        Damaged,
        BlockGained,
        StatusChanged,
        Guarded,
        Redirected,
        IntentShown,
        EnemyActed,
        HeroDowned,
        EnemyDefeated,
        CardsSetAside,
        ActivationEnded,
        CombatWon,
        CombatLost,
    }

    /// <summary>
    /// One thing that happened in a fight, in order. The interface animates these, then redraws from the state.
    /// Fields that don't apply to an event are left empty.
    /// </summary>
    public sealed class CombatEvent
    {
        public readonly CombatEventKind Kind;
        public readonly string SourceId;
        public readonly string TargetId;
        /// <summary>Damage taken by health, Block gained, cards drawn, new Resolve, new status stacks, or the round number.</summary>
        public readonly int Amount;
        /// <summary>For damage: how much Block absorbed.</summary>
        public readonly int Blocked;
        public readonly StatusKind Status;
        public readonly int CardInstanceId;
        /// <summary>For IntentShown and EnemyActed: the action, as it was when the event happened.</summary>
        public readonly IntentDef Intent;

        public CombatEvent(CombatEventKind kind, string sourceId = null, string targetId = null, int amount = 0,
            int blocked = 0, StatusKind status = default, int cardInstanceId = 0, IntentDef intent = null)
        {
            Kind = kind;
            SourceId = sourceId;
            TargetId = targetId;
            Amount = amount;
            Blocked = blocked;
            Status = status;
            CardInstanceId = cardInstanceId;
            Intent = intent;
        }

        public override string ToString()
        {
            switch (Kind)
            {
                case CombatEventKind.Damaged: return $"{SourceId} hits {TargetId} for {Amount} ({Blocked} blocked)";
                case CombatEventKind.StatusChanged: return $"{TargetId} {Status} is now {Amount}";
                case CombatEventKind.CardPlayed: return $"{SourceId} plays card #{CardInstanceId} on {TargetId ?? "-"}";
                default: return $"{Kind} {SourceId} {TargetId} {Amount}".TrimEnd();
            }
        }
    }

    /// <summary>The answer to a player command: either a reason it was refused, or what happened.</summary>
    public sealed class CombatResult
    {
        static readonly CombatEvent[] NoEvents = new CombatEvent[0];

        public readonly bool Ok;
        /// <summary>Why the command was refused, written for the player. Null when it succeeded.</summary>
        public readonly string Reason;
        public readonly IReadOnlyList<CombatEvent> Events;

        CombatResult(bool ok, string reason, IReadOnlyList<CombatEvent> events)
        {
            Ok = ok;
            Reason = reason;
            Events = events;
        }

        internal static CombatResult Success(IReadOnlyList<CombatEvent> events) => new CombatResult(true, null, events);
        internal static CombatResult Refused(string reason) => new CombatResult(false, reason, NoEvents);
    }
}

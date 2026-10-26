namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// Turns combat events into the sentences shown in the combat log. Call it straight after the command that
    /// produced the events, so names and cards are looked up in the same fight.
    /// </summary>
    public static class CombatLogText
    {
        /// <summary>A line for the log, or null for events the player doesn't need to read (cards drawn).</summary>
        public static string Describe(CombatEvent e, CombatState combat)
        {
            string source = Name(combat, e.SourceId);
            string target = Name(combat, e.TargetId);

            switch (e.Kind)
            {
                case CombatEventKind.RoundStarted: return $"Round {e.Amount}";
                case CombatEventKind.DeckShuffled: return "The discard pile is shuffled into the draw pile.";
                case CombatEventKind.HeroActivated: return $"{source} is active.";
                case CombatEventKind.ActivationEnded: return $"{source}'s activation ends. The enemies act.";

                case CombatEventKind.CardPlayed:
                {
                    CardInstance card = combat.FindCard(e.CardInstanceId);
                    string name = card == null ? "a card" : combat.CardOf(card).Name;
                    return e.TargetId == null || e.TargetId == e.SourceId
                        ? $"{source} plays {name}."
                        : $"{source} plays {name} on {target}.";
                }

                case CombatEventKind.CardDiscarded:
                {
                    CardInstance card = combat.FindCard(e.CardInstanceId);
                    return $"{source} discards {(card == null ? "a card" : combat.CardOf(card).Name)}.";
                }

                case CombatEventKind.Damaged:
                {
                    string blocked = e.Blocked > 0 ? $" ({e.Blocked} blocked)" : "";
                    return e.SourceId == null
                        ? $"{target} bleeds for {e.Amount}."
                        : e.SourceId == e.TargetId ? $"{target} pays {e.Amount} health."
                        : $"{source} hits {target} for {e.Amount}{blocked}.";
                }

                case CombatEventKind.BlockGained: return $"{target} gains {e.Amount} Block.";
                case CombatEventKind.StatusChanged:
                    return e.Amount > 0 ? $"{target} has {e.Status} {e.Amount}." : $"{target} is no longer {e.Status}.";
                case CombatEventKind.Guarded: return $"{source} guards {target}.";
                case CombatEventKind.Redirected: return $"{source} takes the blow aimed at {target}.";

                case CombatEventKind.IntentShown:
                {
                    string action = combat.IntentText(combat.Find(e.SourceId), e.Intent) ?? "an action";
                    return e.TargetId == null ? $"{source} readies {action}." : $"{source} readies {action} at {target}.";
                }

                case CombatEventKind.EnemyActed: return $"{source} uses {combat.IntentText(combat.Find(e.SourceId), e.Intent) ?? "an action"}.";
                case CombatEventKind.IntentCancelled:
                    return $"{combat.Cancel?.Label ?? "The party acts"}: {source}'s {combat.IntentText(combat.Find(e.SourceId), e.Intent) ?? "action"} is cancelled.";
                case CombatEventKind.Healed: return $"{target} heals {e.Amount}.";
                case CombatEventKind.HeroDowned: return $"{source} is down.";
                case CombatEventKind.EnemyDefeated: return $"{source} is defeated.";
                case CombatEventKind.CardsSetAside:
                    return e.Amount == 1 ? $"1 card is set aside while {source} is down." : $"{e.Amount} cards are set aside while {source} is down.";
                case CombatEventKind.CombatWon: return "Victory.";
                case CombatEventKind.CombatLost: return "The party has fallen.";
                default: return null;
            }
        }

        static string Name(CombatState combat, string id) => id == null ? null : combat.Find(id)?.Name ?? id;
    }
}

using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    // Events as data: a short scene and its choices, each with results from the small set below. RunState
    // interprets them; nothing checks which event it is.

    public enum EventOutcomeKind
    {
        /// <summary>Every hero heals Amount.</summary>
        HealParty,
        /// <summary>Every hero loses Amount health. None drops below 1.</summary>
        HurtParty,
        /// <summary>The hero the player chooses heals Amount.</summary>
        HealHero,
        /// <summary>The hero the player chooses loses Amount health. A hero who would drop to 0 can't be chosen.</summary>
        HurtHero,
        /// <summary>The hero the player chooses gains Amount maximum health, and heals Amount.</summary>
        MaxHealthHero,
        /// <summary>The party finds the villain's clue Id.</summary>
        GainClue,
        /// <summary>The party gains Keepsake Id, or a random one it doesn't have when Id is null.</summary>
        GainKeepsake,
        /// <summary>The party chooses one of 3 reward cards for the deck, or skips.</summary>
        GainCard,
        /// <summary>The party removes a card of its choice from the deck.</summary>
        RemoveCard,
        /// <summary>A fight with encounter Id, or with a normal encounter for this layer when Id is null.</summary>
        Fight,
    }

    public sealed class EventOutcome
    {
        public readonly EventOutcomeKind Kind;
        public readonly int Amount;
        public readonly string Id;

        public EventOutcome(EventOutcomeKind kind, int amount = 0, string id = null)
        {
            Kind = kind;
            Amount = amount;
            Id = id;
        }

        /// <summary>True for the results that need another screen: a card choice, a card removal or a fight.</summary>
        public bool NeedsAStep => Kind == EventOutcomeKind.GainCard || Kind == EventOutcomeKind.RemoveCard || Kind == EventOutcomeKind.Fight;

        /// <summary>True for the results that apply to the hero the player chooses.</summary>
        public bool NeedsAHero => Kind == EventOutcomeKind.HealHero || Kind == EventOutcomeKind.HurtHero || Kind == EventOutcomeKind.MaxHealthHero;
    }

    public sealed class EventChoice
    {
        /// <summary>The button, such as "Loosen his chains".</summary>
        public readonly string Label;
        /// <summary>What happens, shown after choosing.</summary>
        public readonly string Result;
        /// <summary>The choice only appears once this clue is found; null for always.</summary>
        public readonly string RequiresClue;
        /// <summary>The choice disappears once this clue is found; null for never.</summary>
        public readonly string HiddenByClue;
        public readonly IReadOnlyList<EventOutcome> Outcomes;

        public EventChoice(string label, string result, string requiresClue = null, string hiddenByClue = null, params EventOutcome[] outcomes)
        {
            Label = label;
            Result = result;
            RequiresClue = requiresClue;
            HiddenByClue = hiddenByClue;
            Outcomes = outcomes ?? new EventOutcome[0];
        }

        public bool NeedsAHero => Outcomes.Any(o => o.NeedsAHero);

        /// <summary>The most health the chosen hero loses, so the interface can say who can afford it.</summary>
        public int HeroCost => Outcomes.Where(o => o.Kind == EventOutcomeKind.HurtHero).Sum(o => o.Amount);
    }

    public sealed class EventDef
    {
        public readonly string Id;
        public readonly string Title;
        /// <summary>The scene, under 60 words.</summary>
        public readonly string Scene;
        public readonly IReadOnlyList<EventChoice> Choices;

        public EventDef(string id, string title, string scene, params EventChoice[] choices)
        {
            Id = id;
            Title = title;
            Scene = scene;
            Choices = choices ?? new EventChoice[0];
        }

        public override string ToString() => Title;
    }
}

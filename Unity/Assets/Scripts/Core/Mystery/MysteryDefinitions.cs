using System;
using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    // The mystery as data: a villain, its clues, and what each clue changes. Core interprets the effect kinds below;
    // nothing anywhere checks which villain it is.

    /// <summary>Where a clue can be found.</summary>
    [Flags]
    public enum ClueSource { Investigation = 1, Elite = 2, Event = 4 }

    public enum ClueEffectKind
    {
        /// <summary>The boss is shown by its real name, and its intents are exact instead of vague.</summary>
        RevealBoss,
        /// <summary>Every attack the party makes on the boss deals Amount more damage, before Weak and Exposed.</summary>
        BonusDamageToBoss,
        /// <summary>The villain's secret room appears on the map.</summary>
        RevealSecretRoom,
        /// <summary>Only the boss itself joins the boss fight.</summary>
        RemoveBossAllies,
        /// <summary>Once per boss fight, the party can cancel one boss intent of kind CancelKind, using the button Label.</summary>
        CancelBossIntent,
        /// <summary>Winning gives the villain's true ending.</summary>
        TrueEnding,
    }

    public sealed class ClueEffect
    {
        public readonly ClueEffectKind Kind;
        public readonly int Amount;
        public readonly IntentKind CancelKind;
        public readonly string Label;

        public ClueEffect(ClueEffectKind kind, int amount = 0, IntentKind cancelKind = default, string label = null)
        {
            Kind = kind;
            Amount = amount;
            CancelKind = cancelKind;
            Label = label;
        }
    }

    public sealed class ClueDef
    {
        public readonly string Id;
        public readonly string Name;
        /// <summary>The Investigation scene before searching, under 60 words.</summary>
        public readonly string Scene;
        /// <summary>The search choice, such as "Pry open the desk".</summary>
        public readonly string SearchLabel;
        /// <summary>Health the searching hero loses. A hero can't search if it would bring them to 0.</summary>
        public readonly int SearchCost;
        /// <summary>The journal entry: what the clue says and what it changes.</summary>
        public readonly string Journal;
        public readonly ClueSource Sources;
        /// <summary>The map layers an Investigation can hold it on, counting the first layer as 0.</summary>
        public readonly int MinLayer;
        public readonly int MaxLayer;
        public readonly IReadOnlyList<ClueEffect> Effects;

        public ClueDef(string id, string name, ClueSource sources, int minLayer, int maxLayer, int searchCost,
            string scene, string searchLabel, string journal, params ClueEffect[] effects)
        {
            Id = id;
            Name = name;
            Sources = sources;
            MinLayer = minLayer;
            MaxLayer = maxLayer;
            SearchCost = searchCost;
            Scene = scene;
            SearchLabel = searchLabel;
            Journal = journal;
            Effects = effects ?? new ClueEffect[0];
        }

        public bool Has(ClueEffectKind kind) => Effects.Any(e => e.Kind == kind);

        public override string ToString() => Name;
    }

    /// <summary>A room that only appears on the map once a clue reveals it.</summary>
    public sealed class SecretRoomDef
    {
        public readonly string Name;
        /// <summary>The map layer it sits on, counting the first as 0. It links from the layer before and to the layer after.</summary>
        public readonly int Layer;
        public readonly string Scene;
        public readonly string ActionLabel;
        /// <summary>Health each hero loses when the party acts here. No hero drops below 1.</summary>
        public readonly int HealthCostPerHero;
        /// <summary>The boss starts its fight with this share of its health missing.</summary>
        public readonly int BossHealthLossPercent;

        public SecretRoomDef(string name, int layer, string scene, string actionLabel, int healthCostPerHero, int bossHealthLossPercent)
        {
            Name = name;
            Layer = layer;
            Scene = scene;
            ActionLabel = actionLabel;
            HealthCostPerHero = healthCostPerHero;
            BossHealthLossPercent = bossHealthLossPercent;
        }
    }

    public sealed class VillainDef
    {
        public readonly string Id;
        /// <summary>The boss enemy in the expedition's boss encounter. Any other enemy there is one of its allies.</summary>
        public readonly string BossEnemyId;
        /// <summary>The name the boss goes by until the party learns who it really is.</summary>
        public readonly string HiddenName;
        public readonly IReadOnlyList<ClueDef> Clues;
        public readonly SecretRoomDef SecretRoom;
        public readonly string Ending;
        public readonly string TrueEnding;

        public VillainDef(string id, string bossEnemyId, string hiddenName, IEnumerable<ClueDef> clues, SecretRoomDef secretRoom,
            string ending, string trueEnding)
        {
            Id = id;
            BossEnemyId = bossEnemyId;
            HiddenName = hiddenName;
            Clues = (clues ?? Enumerable.Empty<ClueDef>()).ToArray();
            SecretRoom = secretRoom;
            Ending = ending;
            TrueEnding = trueEnding;
        }

        public ClueDef Clue(string id) => Clues.FirstOrDefault(c => c.Id == id);
    }
}

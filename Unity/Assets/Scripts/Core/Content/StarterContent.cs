using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// The game's content, written in code so the rules can be tested without Unity. In Unity this same content
    /// becomes ScriptableObject assets; this class stays as the test fixture.
    /// </summary>
    public static class StarterContent
    {
        public const string Bram = "bram";
        public const string Wren = "wren";
        public const string Ashe = "ashe";

        public static readonly string[] Party = { Bram, Wren, Ashe };

        /// <summary>The first test fight: a slow, heavy hitter and a caster that weakens the party.</summary>
        public static readonly string[] FirstEncounter = { "hollow_sentry", "chapel_wretch" };

        public static ContentLibrary Library() => new ContentLibrary(Heroes(), Cards(), Enemies());

        public static IEnumerable<HeroDef> Heroes() => new[]
        {
            new HeroDef(Bram, "Bram", "the Warden", "Iron", 40),
            new HeroDef(Wren, "Wren", "the Hunter", "Thorn", 28),
            new HeroDef(Ashe, "Sister Ashe", "the Occultist", "Ash", 30),
        };

        public static IEnumerable<CardDef> Cards() => StarterCards().Concat(RewardCards());

        /// <summary>The 15 cards of the starting deck.</summary>
        public static IEnumerable<CardDef> StarterCards() => new[]
        {
            // Bram: protect the party and set enemies up.
            new CardDef("shield_wall", "Shield Wall", new[] { Bram }, 1, CardTarget.Ally,
                "Give an ally 7 Block.",
                EffectDef.Block(7)),
            new CardDef("stand_guard", "Stand Guard", new[] { Bram }, 1, CardTarget.Ally,
                "Guard another ally until your next turn: attacks aimed at them hit Bram. Bram gains 4 Block.",
                EffectDef.Guard(), EffectDef.Block(4, EffectScope.ActingHero)),
            new CardDef("shield_bash", "Shield Bash", new[] { Bram }, 1, CardTarget.Enemy,
                "Deal 4 damage. Apply 2 Exposed.",
                EffectDef.Damage(4), EffectDef.Apply(StatusKind.Exposed, 2)),
            new CardDef("stagger", "Stagger", new[] { Bram }, 2, CardTarget.Enemy,
                "Deal 3 damage. Apply 2 Weak.",
                EffectDef.Damage(3), EffectDef.Apply(StatusKind.Weak, 2)),

            // Wren: damage, especially against Exposed enemies.
            new CardDef("quick_shot", "Quick Shot", new[] { Wren }, 1, CardTarget.Enemy,
                "Deal 6 damage.",
                EffectDef.Damage(6)),
            new CardDef("barbed_arrow", "Barbed Arrow", new[] { Wren }, 1, CardTarget.Enemy,
                "Deal 3 damage. Apply 3 Bleed.",
                EffectDef.Damage(3), EffectDef.Apply(StatusKind.Bleed, 3)),
            new CardDef("exploit", "Exploit", new[] { Wren }, 2, CardTarget.Enemy,
                "Deal 7 damage, or 14 if the target is Exposed.",
                EffectDef.Damage(7, bonusIfExposed: 7)),
            new CardDef("volley", "Volley", new[] { Wren }, 2, CardTarget.None,
                "Deal 4 damage to every enemy.",
                EffectDef.DamageAll(4)),

            // Sister Ashe: Hex, Weak and resources.
            new CardDef("hex_bolt", "Hex Bolt", new[] { Ashe }, 1, CardTarget.Enemy,
                "Deal 3 damage. Apply 2 Hex.",
                EffectDef.Damage(3), EffectDef.Apply(StatusKind.Hex, 2)),
            new CardDef("wither", "Wither", new[] { Ashe }, 1, CardTarget.Enemy,
                "Apply 3 Hex and 1 Weak.",
                EffectDef.Apply(StatusKind.Hex, 3), EffectDef.Apply(StatusKind.Weak, 1)),
            new CardDef("dark_insight", "Dark Insight", new[] { Ashe }, 0, CardTarget.None,
                "Gain 1 Resolve. Draw a card.",
                EffectDef.GainResolve(1), EffectDef.Draw(1)),
            new CardDef("unravel", "Unravel", new[] { Ashe }, 2, CardTarget.Enemy,
                "Remove all Hex from an enemy. Deal 4 damage for each stack removed.",
                EffectDef.ConsumeHex(4)),

            // Dual cards: playable while either owner is active.
            new CardDef("pin_down", "Pin Down", new[] { Bram, Wren }, 1, CardTarget.Enemy,
                "Deal 5 damage. Apply 1 Exposed.",
                EffectDef.Damage(5), EffectDef.Apply(StatusKind.Exposed, 1)),
            new CardDef("hunters_hex", "Hunter's Hex", new[] { Wren, Ashe }, 1, CardTarget.Enemy,
                "Deal 4 damage. Apply 2 Hex.",
                EffectDef.Damage(4), EffectDef.Apply(StatusKind.Hex, 2)),
            new CardDef("warding_sigil", "Warding Sigil", new[] { Ashe, Bram }, 1, CardTarget.Ally,
                "Give an ally 5 Block. Apply 1 Weak to every enemy.",
                EffectDef.Block(5), EffectDef.Apply(StatusKind.Weak, 1, EffectScope.AllEnemies)),
        };

        /// <summary>Cards that can be offered after a won fight. They use the same effects as the starting cards.</summary>
        public static IEnumerable<CardDef> RewardCards() => new[]
        {
            // Bram
            new CardDef("bulwark", "Bulwark", new[] { Bram }, 2, CardTarget.Ally,
                "Give an ally 12 Block.",
                EffectDef.Block(12)),
            new CardDef("iron_rebuke", "Iron Rebuke", new[] { Bram }, 1, CardTarget.Enemy,
                "Deal 6 damage. Bram gains 4 Block.",
                EffectDef.Damage(6), EffectDef.Block(4, EffectScope.ActingHero)),
            new CardDef("hold_the_line", "Hold the Line", new[] { Bram }, 2, CardTarget.Ally,
                "Guard another ally until your next turn. Bram gains 10 Block.",
                EffectDef.Guard(), EffectDef.Block(10, EffectScope.ActingHero)),
            new CardDef("crushing_blow", "Crushing Blow", new[] { Bram }, 2, CardTarget.Enemy,
                "Deal 9 damage. Apply 1 Weak.",
                EffectDef.Damage(9), EffectDef.Apply(StatusKind.Weak, 1)),

            // Wren
            new CardDef("aimed_shot", "Aimed Shot", new[] { Wren }, 2, CardTarget.Enemy,
                "Deal 11 damage, or 17 if the target is Exposed.",
                EffectDef.Damage(11, bonusIfExposed: 6)),
            new CardDef("open_vein", "Open Vein", new[] { Wren }, 1, CardTarget.Enemy,
                "Apply 5 Bleed.",
                EffectDef.Apply(StatusKind.Bleed, 5)),
            new CardDef("mark_prey", "Mark Prey", new[] { Wren }, 0, CardTarget.Enemy,
                "Apply 1 Exposed.",
                EffectDef.Apply(StatusKind.Exposed, 1)),
            new CardDef("arrow_storm", "Arrow Storm", new[] { Wren }, 3, CardTarget.None,
                "Deal 6 damage to every enemy. Apply 2 Bleed to every enemy.",
                EffectDef.DamageAll(6), EffectDef.Apply(StatusKind.Bleed, 2, EffectScope.AllEnemies)),

            // Sister Ashe
            new CardDef("grave_whisper", "Grave Whisper", new[] { Ashe }, 1, CardTarget.None,
                "Draw 2 cards.",
                EffectDef.Draw(2)),
            new CardDef("creeping_rot", "Creeping Rot", new[] { Ashe }, 1, CardTarget.None,
                "Apply 2 Hex to every enemy.",
                EffectDef.Apply(StatusKind.Hex, 2, EffectScope.AllEnemies)),
            new CardDef("sap_strength", "Sap Strength", new[] { Ashe }, 1, CardTarget.Enemy,
                "Apply 2 Weak and 1 Hex.",
                EffectDef.Apply(StatusKind.Weak, 2), EffectDef.Apply(StatusKind.Hex, 1)),
            new CardDef("ward_of_ash", "Ward of Ash", new[] { Ashe }, 1, CardTarget.Ally,
                "Give an ally 6 Block. Draw a card.",
                EffectDef.Block(6), EffectDef.Draw(1)),

            // Dual cards
            new CardDef("shieldbreaker", "Shieldbreaker", new[] { Bram, Wren }, 2, CardTarget.Enemy,
                "Deal 8 damage. Apply 2 Exposed.",
                EffectDef.Damage(8), EffectDef.Apply(StatusKind.Exposed, 2)),
            new CardDef("witchfire_arrows", "Witchfire Arrows", new[] { Wren, Ashe }, 2, CardTarget.None,
                "Deal 3 damage to every enemy. Apply 2 Hex to every enemy.",
                EffectDef.DamageAll(3), EffectDef.Apply(StatusKind.Hex, 2, EffectScope.AllEnemies)),
            new CardDef("consecrate", "Consecrate", new[] { Ashe, Bram }, 2, CardTarget.Ally,
                "Give an ally 8 Block. Apply 1 Weak to every enemy.",
                EffectDef.Block(8), EffectDef.Apply(StatusKind.Weak, 1, EffectScope.AllEnemies)),
        };

        public static IEnumerable<EnemyDef> Enemies() => new[]
        {
            new EnemyDef("hollow_sentry", "Hollow Sentry", 34,
                IntentDef.Attack(6, IntentTarget.RandomHero, "Strike 6"),
                IntentDef.Block(8, "Raise shield 8"),
                IntentDef.Attack(12, IntentTarget.HealthiestHero, "Cleave 12")),
            new EnemyDef("chapel_wretch", "Chapel Wretch", 22,
                IntentDef.Apply(StatusKind.Weak, 2, IntentTarget.HealthiestHero, "Curse: Weak 2"),
                IntentDef.Attack(4, IntentTarget.AllHeroes, "Wail 4 to all"),
                IntentDef.Attack(6, IntentTarget.RandomHero, "Claw 6")),

            // Milestone 3: the rest of the floor.
            new EnemyDef("gaol_hound", "Gaol Hound", 18,
                IntentDef.Attack(5, IntentTarget.RandomHero, "Bite 5"),
                IntentDef.Apply(StatusKind.Bleed, 3, IntentTarget.RandomHero, "Rend: Bleed 3"),
                IntentDef.Attack(8, IntentTarget.RandomHero, "Lunge 8")),
            new EnemyDef("bell_ringer", "Bell Ringer", 26,
                IntentDef.Apply(StatusKind.Exposed, 1, IntentTarget.AllHeroes, "Toll: Exposed 1 to all"),
                IntentDef.Attack(9, IntentTarget.HealthiestHero, "Hammer 9"),
                IntentDef.Block(6, "Brace 6")),
            new EnemyDef("drowned_retainer", "Drowned Retainer", 30,
                IntentDef.Attack(6, IntentTarget.RandomHero, "Grasp 6"),
                IntentDef.Block(10, "Silt armour 10"),
                IntentDef.Attack(10, IntentTarget.RandomHero, "Drag under 10")),

            // Elites
            new EnemyDef("bloodbound_knight", "Bloodbound Knight", 70,
                IntentDef.Attack(13, IntentTarget.HealthiestHero, "Cleave 13"),
                IntentDef.Apply(StatusKind.Bleed, 4, IntentTarget.RandomHero, "Blood oath: Bleed 4"),
                IntentDef.Attack(7, IntentTarget.AllHeroes, "Sweep 7 to all"),
                IntentDef.Block(14, "Crimson guard 14")),
            new EnemyDef("chapel_matron", "Chapel Matron", 52,
                IntentDef.Apply(StatusKind.Weak, 2, IntentTarget.AllHeroes, "Hymn: Weak 2 to all"),
                IntentDef.Attack(12, IntentTarget.RandomHero, "Smite 12"),
                IntentDef.Attack(5, IntentTarget.AllHeroes, "Lament 5 to all")),

            // The boss. Until the party finds the Ledger they only see the vague lines.
            new EnemyDef(VaelId, "Vael, the Bloodbound Steward", 140,
                IntentDef.Attack(10, IntentTarget.RandomHero, "Blood lash 10").Vaguely("Blood gathers in his hand"),
                IntentDef.Heal(24, "Blood Tithe: heal 24").Vaguely("He drinks"),
                IntentDef.Apply(StatusKind.Bleed, 2, IntentTarget.AllHeroes, "Hemorrhage: Bleed 2 to all").Vaguely("The air turns red"),
                IntentDef.Block(14, "Sanguine ward 14").Vaguely("He wraps himself in red"),
                IntentDef.Attack(16, IntentTarget.HealthiestHero, "Execute 16").Vaguely("He chooses one of you")),
            new EnemyDef("blood_thrall", "Blood Thrall", 10,
                IntentDef.Attack(4, IntentTarget.RandomHero, "Claw 4"),
                IntentDef.Attack(4, IntentTarget.RandomHero, "Claw 4"),
                IntentDef.Attack(6, IntentTarget.HealthiestHero, "Feed 6")),
        };

        /// <summary>The Milestone 3 floor of the Keep: the fixed party, the starting deck and every encounter.</summary>
        public static ExpeditionDef Expedition() => new ExpeditionDef("black_keep",
            Party,
            StarterCards().Select(c => c.Id),
            RewardCards().Select(c => c.Id),
            new[]
            {
                new EncounterDef("sentry_and_wretch", EncounterKind.Normal, 0, 2, FirstEncounter),
                new EncounterDef("hound_pair", EncounterKind.Normal, 0, 2, "gaol_hound", "gaol_hound"),
                new EncounterDef("ringer_and_wretch", EncounterKind.Normal, 1, 4, "bell_ringer", "chapel_wretch"),
                new EncounterDef("drowned_watch", EncounterKind.Normal, 3, 6, "drowned_retainer", "hollow_sentry"),
                new EncounterDef("pack_and_bell", EncounterKind.Normal, 3, 6, "gaol_hound", "bell_ringer"),
                new EncounterDef("retainers", EncounterKind.Normal, 4, 6, "drowned_retainer", "drowned_retainer"),
                new EncounterDef("bloodbound_knight", EncounterKind.Elite, 2, 6, "bloodbound_knight"),
                new EncounterDef("matron_and_wretch", EncounterKind.Elite, 2, 6, "chapel_matron", "chapel_wretch"),
                new EncounterDef("vael", EncounterKind.Boss, 0, 0, "blood_thrall", VaelId, "blood_thrall"),
            },
            Vael());

        public const string VaelId = "vael";

        /// <summary>The first villain: the clues about Vael, the Cistern, and the two endings (GAME_DESIGN.md).</summary>
        public static VillainDef Vael() => new VillainDef("vael", VaelId, "The Master of the Keep",
            new[]
            {
                new ClueDef("ledger", "The Steward's Ledger", ClueSource.Investigation, 0, 6, 3,
                    "The steward's study. Accounts cover every wall, each entry signed V. A locked drawer holds the last ledger.",
                    "Force the drawer",
                    "Vael, the steward, paid the thing beneath the chapel in blood, one name a week. You know who rules the Keep now, and you'll see his every move.",
                    new ClueEffect(ClueEffectKind.RevealBoss)),
                new ClueDef("silvered_bells", "Silvered Chapel Bells", ClueSource.Investigation | ClueSource.Event, 0, 6, 4,
                    "The bell tower. The bells were silvered against the siege's dead, then cut down. One still hangs, out of reach.",
                    "Climb for the bell",
                    "Silver hurts what Vael has become. Every attack on him deals 2 more damage.",
                    new ClueEffect(ClueEffectKind.BonusDamageToBoss, 2)),
                new ClueDef("cistern_door", "The Cistern Door", ClueSource.Investigation, 0, 3, 3,
                    "Water drips through the floor. Under a rotten rug, an iron door, chained shut, leads down toward the cistern.",
                    "Break the chain",
                    "Something of Vael's is kept in the cistern below the Keep. The Cistern is now on your map.",
                    new ClueEffect(ClueEffectKind.RevealSecretRoom)),
                new ClueDef("thrall_confession", "The Thrall's Confession", ClueSource.Investigation | ClueSource.Event, 1, 6, 2,
                    "A chained servant, half turned, begs you not to kill him. He served the steward. He'll talk if you loosen the chain.",
                    "Loosen his chains",
                    "The thrall told you how Vael calls his servants, and you broke the call. Vael will fight alone.",
                    new ClueEffect(ClueEffectKind.RemoveBossAllies)),
                new ClueDef("true_name", "His True Name", ClueSource.Elite | ClueSource.Event | ClueSource.Investigation, 2, 6, 4,
                    "The chapel register. Its last page is torn but legible: the steward's name before he took the Keep's.",
                    "Read it aloud",
                    "The name he was born with still binds him. Once per fight, speak it to stop his Blood Tithe.",
                    new ClueEffect(ClueEffectKind.CancelBossIntent, cancelKind: IntentKind.Heal, label: "Speak his name")),
                new ClueDef("broken_seal", "The Broken Seal", ClueSource.Investigation, 4, 6, 5,
                    "Behind the altar, a seal of black wax as wide as a door. It is cracked from the inside.",
                    "Touch the seal",
                    "Vael didn't make his bargain alone. Whatever answered him is still beneath the chapel.",
                    new ClueEffect(ClueEffectKind.TrueEnding)),
            },
            new SecretRoomDef("The Cistern", 4,
                "Black water, and in it, still beating, a heart bound in silver wire. Vael's. He hid it where the siege couldn't reach.",
                "Destroy the heart", 4, 30),
            "Vael falls, and the Keep is quiet. In the village they'll say the curse lifted on its own.",
            "Vael falls. Beneath the chapel the broken seal breathes out, and something that was never Vael turns over in its sleep. The Keep is quiet. It won't stay quiet.");

        /// <summary>The 15-card starting deck: one of each starter card, numbered 1 to 15.</summary>
        public static List<CardInstance> StartingDeck() =>
            StarterCards().Select((c, i) => new CardInstance(i + 1, c.Id)).ToList();
    }
}

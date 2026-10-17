using System.Collections.Generic;
using System.Linq;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// The Milestone 1 content, written in code so the rules can be tested without Unity. In Unity this same
    /// content becomes ScriptableObject assets; this class stays as the test fixture.
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

        public static IEnumerable<CardDef> Cards() => new[]
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
        };

        /// <summary>The 15-card starting deck: one of each card above, numbered 1 to 15.</summary>
        public static List<CardInstance> StartingDeck() =>
            Cards().Select((c, i) => new CardInstance(i + 1, c.Id)).ToList();
    }
}

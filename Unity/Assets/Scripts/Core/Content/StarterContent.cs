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
        public const string Corvin = "corvin";
        public const string Mara = "mara";
        public const string Hollis = "hollis";

        public static readonly string[] Party = { Bram, Wren, Ashe };

        /// <summary>The first test fight: a slow, heavy hitter and a caster that weakens the party.</summary>
        public static readonly string[] FirstEncounter = { "hollow_sentry", "chapel_wretch" };

        public static ContentLibrary Library() => new ContentLibrary(Heroes(), Cards(), Enemies());

        public static IEnumerable<HeroDef> Heroes() => new[]
        {
            new HeroDef(Bram, "Bram", "the Warden", "Iron", 40),
            new HeroDef(Wren, "Wren", "the Hunter", "Thorn", 28),
            new HeroDef(Ashe, "Sister Ashe", "the Occultist", "Ash", 30),
            new HeroDef(Corvin, "Corvin", "the Penitent", "Bone", 32),
            new HeroDef(Mara, "Mara", "the Duelist", "Steel", 34),
            new HeroDef(Hollis, "Hollis", "the Bellwright", "Bronze", 32),
        };

        /// <summary>Every hero the party can be chosen from, in the order the party screen shows them.</summary>
        public static readonly string[] AllHeroes = { Bram, Wren, Ashe, Corvin, Mara, Hollis };

        public static IEnumerable<CardDef> Cards() => StarterCards().Concat(RewardCards());

        /// <summary>
        /// Every starting card: four for each hero and one dual card for each pair. A party of three starts with 15 of
        /// them (<see cref="RunState.StartingCards"/>).
        /// </summary>
        public static IEnumerable<CardDef> StarterCards() => FirstStarters().Concat(NewStarters());

        /// <summary>The first three heroes' 15 starting cards.</summary>
        static IEnumerable<CardDef> FirstStarters() => new[]
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

        /// <summary>The Milestone 5 heroes' starting cards, and the dual cards for every pair with one of them in it.</summary>
        static IEnumerable<CardDef> NewStarters() => new[]
        {
            // Corvin: heals the party and pays for it in his own health.
            new CardDef("mend", "Mend", new[] { Corvin }, 1, CardTarget.Ally,
                "Heal an ally 4.",
                EffectDef.Heal(4)),
            new CardDef("blood_offering", "Blood Offering", new[] { Corvin }, 0, CardTarget.None,
                "Corvin pays 4 health. Gain 2 Resolve.",
                EffectDef.LoseHealth(4), EffectDef.GainResolve(2)),
            new CardDef("penance_strike", "Penance Strike", new[] { Corvin }, 1, CardTarget.Enemy,
                "Deal 6 damage. Corvin pays 2 health.",
                EffectDef.Damage(6), EffectDef.LoseHealth(2)),
            new CardDef("cleansing_rite", "Cleansing Rite", new[] { Corvin }, 1, CardTarget.Ally,
                "Remove all Bleed and Weak from an ally. Heal them 2.",
                EffectDef.Cleanse(StatusKind.Bleed), EffectDef.Cleanse(StatusKind.Weak), EffectDef.Heal(2)),

            // Mara: precise strikes, and Riposte to punish whoever attacks her.
            new CardDef("lunge", "Lunge", new[] { Mara }, 1, CardTarget.Enemy,
                "Deal 7 damage.",
                EffectDef.Damage(7)),
            new CardDef("en_garde", "En Garde", new[] { Mara }, 1, CardTarget.None,
                "Mara gains 4 Block and 4 Riposte.",
                EffectDef.Block(4, EffectScope.ActingHero), EffectDef.Apply(StatusKind.Riposte, 4, EffectScope.ActingHero)),
            new CardDef("feint", "Feint", new[] { Mara }, 1, CardTarget.Enemy,
                "Deal 2 damage. Apply 1 Exposed. Draw a card.",
                EffectDef.Damage(2), EffectDef.Apply(StatusKind.Exposed, 1), EffectDef.Draw(1)),
            new CardDef("flurry", "Flurry", new[] { Mara }, 2, CardTarget.Enemy,
                "Deal 3 damage three times.",
                EffectDef.Damage(3), EffectDef.Damage(3), EffectDef.Damage(3)),

            // Hollis: rings Weak and Exposed over every enemy at once.
            new CardDef("toll", "Toll", new[] { Hollis }, 1, CardTarget.None,
                "Deal 2 damage to every enemy. Apply 1 Weak to every enemy. Draw a card.",
                EffectDef.DamageAll(2), EffectDef.Apply(StatusKind.Weak, 1, EffectScope.AllEnemies), EffectDef.Draw(1)),
            new CardDef("knell", "Knell", new[] { Hollis }, 2, CardTarget.None,
                "Deal 5 damage to every enemy. Apply 1 Exposed to every enemy.",
                EffectDef.DamageAll(5), EffectDef.Apply(StatusKind.Exposed, 1, EffectScope.AllEnemies)),
            new CardDef("sanctuary_peal", "Sanctuary Peal", new[] { Hollis }, 1, CardTarget.None,
                "Give every hero 4 Block.",
                EffectDef.Block(4, EffectScope.AllHeroes)),
            new CardDef("cracked_bell", "Cracked Bell", new[] { Hollis }, 1, CardTarget.Enemy,
                "Deal 7 damage. Apply 1 Weak.",
                EffectDef.Damage(7), EffectDef.Apply(StatusKind.Weak, 1)),

            // Dual cards for every new pair.
            new CardDef("shared_burden", "Shared Burden", new[] { Corvin, Bram }, 1, CardTarget.Ally,
                "Give an ally 5 Block. Heal them 2.",
                EffectDef.Block(5), EffectDef.Heal(2)),
            new CardDef("bloodletting", "Bloodletting", new[] { Corvin, Wren }, 1, CardTarget.Enemy,
                "Apply 4 Bleed. Heal yourself 2.",
                EffectDef.Apply(StatusKind.Bleed, 4), EffectDef.Heal(2, EffectScope.ActingHero)),
            new CardDef("last_rites", "Last Rites", new[] { Corvin, Ashe }, 1, CardTarget.Enemy,
                "Apply 2 Hex. Heal every hero 2.",
                EffectDef.Apply(StatusKind.Hex, 2), EffectDef.Heal(2, EffectScope.AllHeroes)),
            new CardDef("second_wind", "Second Wind", new[] { Corvin, Mara }, 1, CardTarget.Ally,
                "Heal an ally 4. They gain 3 Riposte.",
                EffectDef.Heal(4), EffectDef.Apply(StatusKind.Riposte, 3)),
            new CardDef("vespers", "Vespers", new[] { Corvin, Hollis }, 2, CardTarget.None,
                "Heal every hero 3. Apply 1 Weak to every enemy.",
                EffectDef.Heal(3, EffectScope.AllHeroes), EffectDef.Apply(StatusKind.Weak, 1, EffectScope.AllEnemies)),
            new CardDef("blade_and_shield", "Blade and Shield", new[] { Mara, Bram }, 1, CardTarget.Enemy,
                "Deal 5 damage. Gain 3 Block.",
                EffectDef.Damage(5), EffectDef.Block(3, EffectScope.ActingHero)),
            new CardDef("crossing_strikes", "Crossing Strikes", new[] { Mara, Wren }, 1, CardTarget.Enemy,
                "Deal 4 damage twice.",
                EffectDef.Damage(4), EffectDef.Damage(4)),
            new CardDef("cursed_edge", "Cursed Edge", new[] { Mara, Ashe }, 1, CardTarget.Enemy,
                "Deal 4 damage. Apply 2 Hex. Gain 2 Riposte.",
                EffectDef.Damage(4), EffectDef.Apply(StatusKind.Hex, 2), EffectDef.Apply(StatusKind.Riposte, 2, EffectScope.ActingHero)),
            new CardDef("call_to_arms", "Call to Arms", new[] { Mara, Hollis }, 1, CardTarget.None,
                "Every hero gains 3 Riposte.",
                EffectDef.Apply(StatusKind.Riposte, 3, EffectScope.AllHeroes)),
            new CardDef("alarum", "Alarum", new[] { Hollis, Bram }, 1, CardTarget.Ally,
                "Guard another ally until your next turn. Apply 1 Weak to every enemy.",
                EffectDef.Guard(), EffectDef.Apply(StatusKind.Weak, 1, EffectScope.AllEnemies)),
            new CardDef("ringing_shot", "Ringing Shot", new[] { Hollis, Wren }, 1, CardTarget.Enemy,
                "Deal 6 damage. Apply 1 Exposed to every enemy.",
                EffectDef.Damage(6), EffectDef.Apply(StatusKind.Exposed, 1, EffectScope.AllEnemies)),
            new CardDef("dirge", "Dirge", new[] { Hollis, Ashe }, 1, CardTarget.None,
                "Apply 2 Hex to every enemy.",
                EffectDef.Apply(StatusKind.Hex, 2, EffectScope.AllEnemies)),
        };

        /// <summary>Cards that can be offered after a won fight. A run only offers the ones its party can play.</summary>
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

            // Corvin
            new CardDef("zeal", "Flagellant's Zeal", new[] { Corvin }, 0, CardTarget.None,
                "Corvin pays 4 health. Gain 2 Resolve. Draw a card.",
                EffectDef.LoseHealth(4), EffectDef.GainResolve(2), EffectDef.Draw(1)),
            new CardDef("sanctify", "Sanctify", new[] { Corvin }, 2, CardTarget.None,
                "Heal every hero 5.",
                EffectDef.Heal(5, EffectScope.AllHeroes)),
            new CardDef("martyrs_ward", "Martyr's Ward", new[] { Corvin }, 1, CardTarget.Ally,
                "Corvin pays 3 health. Give an ally 10 Block.",
                EffectDef.LoseHealth(3), EffectDef.Block(10)),
            new CardDef("absolution", "Absolution", new[] { Corvin }, 2, CardTarget.Enemy,
                "Deal 12 damage. Corvin pays 4 health.",
                EffectDef.Damage(12), EffectDef.LoseHealth(4)),

            // Mara
            new CardDef("riposte_stance", "Riposte Stance", new[] { Mara }, 1, CardTarget.None,
                "Mara gains 8 Riposte.",
                EffectDef.Apply(StatusKind.Riposte, 8, EffectScope.ActingHero)),
            new CardDef("coup_de_grace", "Coup de Grace", new[] { Mara }, 2, CardTarget.Enemy,
                "Deal 9 damage, or 18 if the target is Exposed.",
                EffectDef.Damage(9, bonusIfExposed: 9)),
            new CardDef("disarm", "Disarm", new[] { Mara }, 1, CardTarget.Enemy,
                "Deal 3 damage. Apply 2 Weak.",
                EffectDef.Damage(3), EffectDef.Apply(StatusKind.Weak, 2)),
            new CardDef("whirling_blade", "Whirling Blade", new[] { Mara }, 2, CardTarget.None,
                "Deal 5 damage to every enemy.",
                EffectDef.DamageAll(5)),

            // Hollis
            new CardDef("great_bell", "The Great Bell", new[] { Hollis }, 3, CardTarget.None,
                "Deal 8 damage to every enemy. Apply 2 Weak to every enemy.",
                EffectDef.DamageAll(8), EffectDef.Apply(StatusKind.Weak, 2, EffectScope.AllEnemies)),
            new CardDef("muffle", "Muffle", new[] { Hollis }, 0, CardTarget.Enemy,
                "Deal 3 damage. Apply 1 Weak.",
                EffectDef.Damage(3), EffectDef.Apply(StatusKind.Weak, 1)),
            new CardDef("peal_of_warning", "Peal of Warning", new[] { Hollis }, 1, CardTarget.None,
                "Give every hero 5 Block.",
                EffectDef.Block(5, EffectScope.AllHeroes)),
            new CardDef("shatter_note", "Shatter Note", new[] { Hollis }, 2, CardTarget.None,
                "Apply 2 Exposed to every enemy.",
                EffectDef.Apply(StatusKind.Exposed, 2, EffectScope.AllEnemies)),
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

            // Milestone 5
            new EnemyDef("cistern_leech", "Cistern Leech", 14,
                IntentDef.Apply(StatusKind.Bleed, 2, IntentTarget.RandomHero, "Latch: Bleed 2"),
                IntentDef.Attack(5, IntentTarget.RandomHero, "Bite 5"),
                IntentDef.Heal(6, "Gorge: heal 6")),
            new EnemyDef("siege_revenant", "Siege Revenant", 32,
                IntentDef.Attack(8, IntentTarget.RandomHero, "Old wound 8"),
                IntentDef.Block(10, "Close ranks 10"),
                IntentDef.Attack(4, IntentTarget.AllHeroes, "Rally 4 to all")),

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
            new EnemyDef(VaelId, "Vael, the Bloodbound Steward", 150,
                IntentDef.Attack(10, IntentTarget.RandomHero, "Blood lash 10").Vaguely("Blood gathers in his hand"),
                IntentDef.Heal(24, "Blood Tithe: heal 24").Vaguely("He drinks"),
                IntentDef.Apply(StatusKind.Bleed, 2, IntentTarget.AllHeroes, "Hemorrhage: Bleed 2 to all").Vaguely("The air turns red"),
                IntentDef.Block(14, "Sanguine ward 14").Vaguely("He wraps himself in red"),
                IntentDef.Attack(16, IntentTarget.HealthiestHero, "Execute 16").Vaguely("He chooses one of you"),
                // Each time his pattern comes round he hits harder, so a party that outheals him can't stall forever.
                IntentDef.Apply(StatusKind.Fury, 3, IntentTarget.Self, "Bloodrage: Fury 3").Vaguely("His veins darken")),
            new EnemyDef("blood_thrall", "Blood Thrall", 10,
                IntentDef.Attack(4, IntentTarget.RandomHero, "Claw 4"),
                IntentDef.Attack(4, IntentTarget.RandomHero, "Claw 4"),
                IntentDef.Attack(6, IntentTarget.HealthiestHero, "Feed 6")),
        };

        /// <summary>The floor of the Keep: the heroes to choose from, every starting and reward card, the encounters, Vael, the Keepsakes and the events.</summary>
        public static ExpeditionDef Expedition() => new ExpeditionDef("black_keep",
            AllHeroes,
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
                new EncounterDef("leech_swarm", EncounterKind.Normal, 1, 4, "cistern_leech", "cistern_leech", "cistern_leech"),
                new EncounterDef("revenant_and_leech", EncounterKind.Normal, 3, 6, "siege_revenant", "cistern_leech"),
                new EncounterDef("bloodbound_knight", EncounterKind.Elite, 2, 6, "bloodbound_knight"),
                new EncounterDef("matron_and_wretch", EncounterKind.Elite, 2, 6, "chapel_matron", "chapel_wretch"),
                new EncounterDef("vael", EncounterKind.Boss, 0, 0, "blood_thrall", VaelId, "blood_thrall"),
            },
            Vael(),
            Keepsakes(),
            Events());

        /// <summary>The Milestone 5 Keepsakes (GAME_DESIGN.md).</summary>
        public static KeepsakeDef[] Keepsakes() => new[]
        {
            new KeepsakeDef("gaolers_keyring", "Gaoler's Keyring", "The first card each fight costs 1 less.", KeepsakeEffectKind.FirstCardDiscount, 1),
            new KeepsakeDef("siege_lantern", "Siege Lantern", "Every hero starts each fight with 4 Block.", KeepsakeEffectKind.StartBlock, 4),
            new KeepsakeDef("chapel_candle", "Chapel Candle", "After each won fight, every standing hero heals 3.", KeepsakeEffectKind.HealAfterWin, 3),
            new KeepsakeDef("rusted_caltrops", "Rusted Caltrops", "At the start of each fight, every enemy gets Bleed 2.", KeepsakeEffectKind.EnemyStartStatus, 2, StatusKind.Bleed),
            new KeepsakeDef("widows_veil", "Widow's Veil", "At the start of each fight, every enemy gets Weak 1.", KeepsakeEffectKind.EnemyStartStatus, 1, StatusKind.Weak),
            new KeepsakeDef("seal_ring", "Steward's Seal-ring", "The first activation each fight gets 1 more Resolve.", KeepsakeEffectKind.FirstActivationResolve, 1),
            new KeepsakeDef("marked_deck", "Marked Deck", "Draw 2 more cards on the first turn of each fight.", KeepsakeEffectKind.FirstTurnDraw, 2),
            new KeepsakeDef("bone_charm", "Bone Charm", "Every hero's maximum health goes up by 4.", KeepsakeEffectKind.MaxHealth, 4),
        };

        static EventOutcome Heal(int n) => new EventOutcome(EventOutcomeKind.HealParty, n);
        static EventOutcome HurtAll(int n) => new EventOutcome(EventOutcomeKind.HurtParty, n);
        static EventOutcome HurtHero(int n) => new EventOutcome(EventOutcomeKind.HurtHero, n);
        static EventOutcome Clue(string id) => new EventOutcome(EventOutcomeKind.GainClue, id: id);
        static EventOutcome Keepsake(string id = null) => new EventOutcome(EventOutcomeKind.GainKeepsake, id: id);
        static readonly EventOutcome NewCard = new EventOutcome(EventOutcomeKind.GainCard);
        static readonly EventOutcome LoseCard = new EventOutcome(EventOutcomeKind.RemoveCard);
        static EventOutcome Fight(string encounter = null) => new EventOutcome(EventOutcomeKind.Fight, id: encounter);

        /// <summary>The Milestone 5 events (GAME_DESIGN.md). Each scene stays under 60 words.</summary>
        public static EventDef[] Events() => new[]
        {
            new EventDef("chained_thrall", "The Chained Thrall",
                "A servant chained to a pillar, half turned, grey to the elbows. He begs you not to kill him. He served the steward, he says, and he knows how the master calls his own.",
                new EventChoice("Loosen his chains", "He clutches at whoever frees him, then talks.", hiddenByClue: "thrall_confession",
                    outcomes: new[] { HurtHero(3), Clue("thrall_confession") }),
                new EventChoice("Name his master", "At Vael's name he goes still, and tells you everything.", requiresClue: "ledger", hiddenByClue: "thrall_confession",
                    outcomes: Clue("thrall_confession")),
                new EventChoice("Leave him", "His begging follows you down the corridor.")),
            new EventDef("chapel_register", "The Chapel Register",
                "The chapel's register lies open on the lectern, every birth and burial of the Keep. The last page is torn, and the ink there is still wet.",
                new EventChoice("Read the torn page", "The words cut whoever reads them. One is a name the steward hasn't used in years.", hiddenByClue: "true_name",
                    outcomes: new[] { HurtHero(4), Clue("true_name") }),
                new EventChoice("Burn the register", "Whatever it held, no one will use it now. You leave something of your own in the flames.",
                    outcomes: LoseCard)),
            new EventDef("bell_widow", "The Bell-founder's Widow",
                "An old woman sits among the broken bells of the founder's yard. Her husband silvered them for the siege. She will trade, she says. Everyone trades in the Keep.",
                new EventChoice("Trade her something from the deck", "She hands over a silvered clapper and takes what you give without looking at it.", hiddenByClue: "silvered_bells",
                    outcomes: new[] { LoseCard, Clue("silvered_bells") }),
                new EventChoice("Take her veil", "She lifts the black veil from her face and gives it to you. Her eyes are white.",
                    outcomes: Keepsake("widows_veil")),
                new EventChoice("Leave her", "She goes back to polishing a bell that will never ring.")),
            new EventDef("drowned_well", "The Drowned Well",
                "A well in the lower court, its water clear and cold. Something glints at the bottom of the bucket rope. Something else moves in the dark below it.",
                new EventChoice("Drink", "The water is clean. It is the first clean thing in the Keep.", outcomes: Heal(6)),
                new EventChoice("Draw up the bucket", "You pull the rope. What was holding the other end comes up with it.",
                    outcomes: new[] { Keepsake(), Fight("drowned_watch") })),
            new EventDef("quartermaster", "The Quartermaster's Store",
                "The siege stores, picked over and rotting. Behind a false wall the quartermaster kept his own supplies, and the shelf is still stocked. The rest is behind a rusted grate.",
                new EventChoice("Take from the shelf", "Not everything here has gone bad.", outcomes: NewCard),
                new EventChoice("Force the grate", "The grate gives, and so does a hand behind it.", outcomes: new[] { HurtHero(6), Keepsake() })),
            new EventDef("siege_graves", "The Siege Graves",
                "The dead of the siege were buried in the inner yard in a hurry, and shallow. Some graves have been opened from below.",
                new EventChoice("Dig", "The dead kept their things. Getting them is cold, slow work.", outcomes: new[] { HurtAll(3), Keepsake() }),
                new EventChoice("Pray over them", "You say the words the chaplain would have said, and leave something with them.", outcomes: LoseCard),
                new EventChoice("Lay them to rest", "The seal is broken; nothing holds them now. You give them what peace you can, and some of it comes back to you.",
                    requiresClue: "broken_seal", outcomes: Heal(8))),
            new EventDef("survivor", "The Survivor",
                "A young woman steps out of a doorway, unhurt and very clean. Her family is trapped below, she says. She knows a way. Please, follow her.",
                new EventChoice("Follow her", "She leads you to a dead end, and they are waiting there.", outcomes: Fight()),
                new EventChoice("Send her away", "She doesn't argue. She just goes back into the dark."),
                new EventChoice("Name her master", "She knows the name in the Ledger, and she runs, dropping what she carried.",
                    requiresClue: "ledger", outcomes: Keepsake())),
            new EventDef("mirror_hall", "The Mirror Hall",
                "A long gallery of mirrors, all but one smashed. In the last one your reflections stand a little straighter than you do.",
                new EventChoice("Face the mirror", "One of you stares until the reflection looks away. They come back stronger.",
                    outcomes: new EventOutcome(EventOutcomeKind.MaxHealthHero, 4)),
                new EventChoice("Break it", "One of you shatters it barehanded. Something you carried breaks with it.",
                    outcomes: new[] { HurtHero(5), LoseCard })),
        };

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
                new ClueDef("thrall_confession", "The Thrall's Confession", ClueSource.Event, 1, 6, 2,
                    "A chained servant, half turned, begs you not to kill him. He served the steward. He'll talk if you loosen the chain.",
                    "Loosen his chains",
                    "The thrall told you how Vael calls his servants, and you broke the call. Vael will fight alone.",
                    new ClueEffect(ClueEffectKind.RemoveBossAllies)),
                new ClueDef("true_name", "His True Name", ClueSource.Elite | ClueSource.Event, 2, 6, 4,
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
        /// <summary>A party's 15-card starting deck; the first three heroes when <paramref name="party"/> is null.</summary>
        public static List<CardInstance> StartingDeck(IReadOnlyList<string> party = null) =>
            RunState.StartingCards(Library(), Expedition(), party ?? Party).Select((id, i) => new CardInstance(i + 1, id)).ToList();
    }
}

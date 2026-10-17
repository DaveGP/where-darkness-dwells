using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    /// <summary>Choosing three of the six heroes, and the Milestone 5 heroes' new effects.</summary>
    public class PartyTests
    {
        static readonly ContentLibrary Library = StarterContent.Library();

        static IEnumerable<string[]> EveryParty()
        {
            string[] all = StarterContent.AllHeroes;
            for (int a = 0; a < all.Length; a++)
            for (int b = a + 1; b < all.Length; b++)
            for (int c = b + 1; c < all.Length; c++)
                yield return new[] { all[a], all[b], all[c] };
        }

        [Test]
        public void EveryPartyStartsWithFifteenCardsTheyCanAllPlay()
        {
            Assert.That(EveryParty().Count(), Is.EqualTo(20));
            foreach (string[] party in EveryParty())
            {
                var run = new RunState(Library, StarterContent.Expedition(), 1, party);
                List<CardDef> deck = run.Deck.Select(run.CardOf).ToList();
                string name = string.Join(", ", party);
                Assert.That(deck.Count, Is.EqualTo(15), name);
                Assert.That(deck.All(c => c.Owners.All(party.Contains)), Is.True, name);
                foreach (string hero in party) Assert.That(deck.Count(c => c.Owners.Count == 1 && c.IsOwnedBy(hero)), Is.EqualTo(4), $"{name}: {hero}");
                Assert.That(deck.Count(c => c.Owners.Count == 2), Is.EqualTo(3), name);
                Assert.That(run.Party, Is.EqualTo(party));
            }
        }

        [Test]
        public void RewardsOnlyOfferCardsThePartyCanPlay()
        {
            string[] party = { StarterContent.Corvin, StarterContent.Mara, StarterContent.Hollis };
            foreach (ulong seed in Enumerable.Range(1, 30).Select(s => (ulong)s))
            {
                var run = new RunState(Library, StarterContent.Expedition(), seed, party);
                Assert.That(run.TryEnterRoom(run.ReachableRooms.First().Id).Ok, Is.True);
                RunStateTests.PlayFight(run.Combat);
                Assert.That(run.TryFinishCombat().Ok, Is.True);
                if (run.Phase != RunPhase.Reward) continue;
                foreach (string id in run.RewardOffer) Assert.That(Library.Card(id).Owners.Any(party.Contains), Is.True, id);
                return;
            }
            Assert.Fail("No seed won the first fight.");
        }

        [Test]
        public void APartyMustBeThreeDifferentHeroesFromTheList()
        {
            ExpeditionDef x = StarterContent.Expedition();
            Assert.That(RunState.WhyCantChoose(x, new[] { StarterContent.Bram, StarterContent.Wren }), Is.EqualTo("Choose 3 heroes."));
            Assert.That(RunState.WhyCantChoose(x, new[] { StarterContent.Bram, StarterContent.Bram, StarterContent.Wren }), Is.EqualTo("Choose each hero only once."));
            Assert.That(RunState.WhyCantChoose(x, new[] { StarterContent.Bram, StarterContent.Wren, "vael" }), Is.EqualTo("Choose heroes from the list."));
            Assert.That(RunState.WhyCantChoose(x, new[] { StarterContent.Bram, StarterContent.Wren, StarterContent.Mara }), Is.Null);
            Assert.Throws<System.ArgumentException>(() => new RunState(Library, x, 1, new[] { StarterContent.Bram }));
        }

        // ---------- the new effects ----------

        static CombatState Fight(string[] party, params string[] enemies)
        {
            var combat = new CombatState(Library, party, StarterContent.StartingDeck(party), enemies, new Rng(2));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static CardInstance InHand(CombatState combat, string cardId)
        {
            CardInstance card = combat.FindCard(combat.DrawPile.Concat(combat.DiscardPile).Concat(combat.Hand).First(c => c.DefId == cardId).Id);
            combat.PutInHandForTest(card.Id);
            return card;
        }

        [Test]
        public void MendHealsAnAllyButNotPastTheirMaximum()
        {
            string[] party = { StarterContent.Corvin, StarterContent.Bram, StarterContent.Wren };
            CombatState combat = Fight(party, "hollow_sentry");
            combat.Find(StarterContent.Wren).Health = 20;
            Assert.That(combat.TryActivate(StarterContent.Corvin).Ok, Is.True);
            Assert.That(combat.TryPlay(InHand(combat, "mend").Id, StarterContent.Wren).Ok, Is.True);
            Assert.That(combat.Find(StarterContent.Wren).Health, Is.EqualTo(26));
            combat.Find(StarterContent.Wren).Health = 25;
            Assert.That(combat.TryPlay(InHand(combat, "mend").Id, StarterContent.Wren).Ok, Is.True);
            Assert.That(combat.Find(StarterContent.Wren).Health, Is.EqualTo(28), "Wren's maximum is 28.");
        }

        [Test]
        public void PayingHealthNeverDropsCorvinBelowOne()
        {
            string[] party = { StarterContent.Corvin, StarterContent.Bram, StarterContent.Wren };
            CombatState combat = Fight(party, "hollow_sentry");
            combat.Find(StarterContent.Corvin).Health = 2;
            Assert.That(combat.TryActivate(StarterContent.Corvin).Ok, Is.True);
            Assert.That(combat.TryPlay(InHand(combat, "blood_offering").Id).Ok, Is.True);
            Assert.That(combat.Find(StarterContent.Corvin).Health, Is.EqualTo(1));
            Assert.That(combat.Resolve, Is.EqualTo(CombatState.ResolvePerActivation + 2));
            Assert.That(combat.Log.Select(e => CombatLogText.Describe(e, combat)), Has.Member("Corvin pays 1 health."));
        }

        [Test]
        public void CleansingRiteRemovesBleedAndWeak()
        {
            string[] party = { StarterContent.Corvin, StarterContent.Bram, StarterContent.Wren };
            CombatState combat = Fight(party, "hollow_sentry");
            Combatant wren = combat.Find(StarterContent.Wren);
            wren.SetStatus(StatusKind.Bleed, 3);
            wren.SetStatus(StatusKind.Weak, 1);
            wren.Health = 10;
            Assert.That(combat.TryActivate(StarterContent.Corvin).Ok, Is.True);
            Assert.That(combat.TryPlay(InHand(combat, "cleansing_rite").Id, StarterContent.Wren).Ok, Is.True);
            Assert.That(wren.Statuses, Is.Empty);
            Assert.That(wren.Health, Is.EqualTo(13));
        }

        [Test]
        public void RiposteStrikesBackAtAnAttackerAndFadesNextTurn()
        {
            string[] party = { StarterContent.Mara, StarterContent.Bram, StarterContent.Wren };
            CombatState combat = Fight(party, "gaol_hound");
            Combatant mara = combat.Find(StarterContent.Mara), hound = combat.Enemies[0];
            Assert.That(combat.TryActivate(StarterContent.Mara).Ok, Is.True);
            Assert.That(combat.TryPlay(InHand(combat, "en_garde").Id).Ok, Is.True);
            Assert.That(mara.Status(StatusKind.Riposte), Is.EqualTo(4));
            hound.IntentTargetId = mara.Id; // Bite 5, aimed at Mara.
            int before = hound.Health;
            Assert.That(combat.TryEndActivation().Ok, Is.True);
            Assert.That(hound.Health, Is.EqualTo(before - 4));
            Assert.That(mara.Status(StatusKind.Riposte), Is.EqualTo(0), "Riposte lasts until the next player turn, like Block.");
        }

        [Test]
        public void ABellCanReachEveryHeroOrEveryEnemy()
        {
            string[] party = { StarterContent.Hollis, StarterContent.Bram, StarterContent.Wren };
            CombatState combat = Fight(party, "gaol_hound", "gaol_hound");
            Assert.That(combat.TryActivate(StarterContent.Hollis).Ok, Is.True);
            Assert.That(combat.TryPlay(InHand(combat, "sanctuary_peal").Id).Ok, Is.True);
            Assert.That(combat.Heroes.Select(h => h.Block), Is.All.EqualTo(3));
            Assert.That(combat.TryPlay(InHand(combat, "knell").Id).Ok, Is.True);
            Assert.That(combat.Enemies.Select(e => e.Status(StatusKind.Exposed)), Is.All.EqualTo(1));
            Assert.That(combat.Enemies.Select(e => e.Health), Is.All.EqualTo(15));
        }
    }
}

using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class DiscardTests
    {
        const string Bram = StarterContent.Bram, Wren = StarterContent.Wren;

        /// <summary>A started fight with eight cards: six in hand and two left to draw.</summary>
        static CombatState Fight()
        {
            // Mostly Bram's cards, so the hand surely holds one for Wren to discard.
            string[] cards = { "shield_wall", "shield_wall", "shield_wall", "shield_wall", "shield_wall", "shield_wall", "quick_shot", "volley" };
            var deck = cards.Select((id, i) => new CardInstance(i + 1, id)).ToList();
            var combat = new CombatState(StarterContent.Library(), StarterContent.Party, deck, new[] { "hollow_sentry" }, new Rng(7));
            Assert.That(combat.Start().Ok, Is.True);
            return combat;
        }

        static void Ok(CombatResult result) => Assert.That(result.Ok, Is.True, result.Reason);

        [Test]
        public void TheActiveHeroCanDiscardAnyCardForFree()
        {
            CombatState combat = Fight();
            Ok(combat.TryActivate(Wren));
            // Bram's card, discarded while Wren is active.
            CardInstance card = combat.Hand.First(c => c.DefId == "shield_wall");

            CombatResult result = combat.TryDiscard(card.Id);
            Ok(result);
            Assert.That(combat.Hand, Does.Not.Contain(card));
            Assert.That(combat.DiscardPile, Does.Contain(card));
            Assert.That(combat.Resolve, Is.EqualTo(3));
            Assert.That(combat.CardsPlayed, Is.EqualTo(0));
            CombatEvent e = result.Events.Single();
            Assert.That(e.Kind, Is.EqualTo(CombatEventKind.CardDiscarded));
            Assert.That(CombatLogText.Describe(e, combat), Is.EqualTo("Wren discards Shield Wall."));
        }

        [Test]
        public void OnlyOneCardCanBeDiscardedPerActivation()
        {
            CombatState combat = Fight();
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryDiscard(combat.Hand[0].Id));
            int hand = combat.Hand.Count, log = combat.Log.Count;

            CombatResult refused = combat.TryDiscard(combat.Hand[0].Id);
            Assert.That(refused.Ok, Is.False);
            Assert.That(refused.Reason, Is.EqualTo("Wren has already discarded a card this activation."));
            Assert.That(combat.Hand.Count, Is.EqualTo(hand));
            Assert.That(combat.Log.Count, Is.EqualTo(log));

            Ok(combat.TryEndActivation());
            Ok(combat.TryActivate(Bram));
            Assert.That(combat.HasDiscarded, Is.False);
            Ok(combat.TryDiscard(combat.Hand[0].Id));
        }

        [Test]
        public void TheNextDrawFillsTheDiscardedCardsPlace()
        {
            CombatState combat = Fight();
            Ok(combat.TryActivate(Wren));
            Ok(combat.TryDiscard(combat.Hand[0].Id));
            Assert.That(combat.Hand.Count, Is.EqualTo(CombatState.HandSize - 1));
            Ok(combat.TryEndActivation());
            Assert.That(combat.Hand.Count, Is.EqualTo(CombatState.HandSize));
        }

        [Test]
        public void DiscardingNeedsAnActiveHeroAndACardInHand()
        {
            CombatState combat = Fight();
            Assert.That(combat.TryDiscard(combat.Hand[0].Id).Reason, Is.EqualTo("Activate a hero to discard a card."));
            Ok(combat.TryActivate(Wren));
            Assert.That(combat.TryDiscard(combat.DrawPile[0].Id).Reason, Is.EqualTo("That card isn't in your hand."));
        }
    }
}

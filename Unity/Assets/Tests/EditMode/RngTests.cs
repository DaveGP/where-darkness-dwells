using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using WhereDarknessDwells.Core;

namespace WhereDarknessDwells.Tests
{
    public class RngTests
    {
        static List<int> Take(Rng rng, int count) => Enumerable.Range(0, count).Select(_ => rng.Next(1000)).ToList();

        [Test]
        public void TheSameSeedGivesTheSameNumbers()
        {
            Assert.That(Take(new Rng(5), 20), Is.EqualTo(Take(new Rng(5), 20)));
            Assert.That(Take(new Rng(5), 20), Is.Not.EqualTo(Take(new Rng(6), 20)));
        }

        [Test]
        public void StreamsDontDependOnHowMuchTheParentWasUsed()
        {
            var fresh = new Rng(9);
            var used = new Rng(9);
            Take(used, 50);
            Assert.That(Take(fresh.Stream("combat"), 10), Is.EqualTo(Take(used.Stream("combat"), 10)));
            Assert.That(Take(fresh.Stream("combat"), 10), Is.Not.EqualTo(Take(fresh.Stream("map"), 10)));
        }

        [Test]
        public void ARestoredGeneratorCarriesOnWhereItLeftOff()
        {
            var rng = new Rng(3);
            Take(rng, 7);
            Rng restored = Rng.Restore(3, rng.State);
            Assert.That(Take(restored, 10), Is.EqualTo(Take(rng, 10)));
        }

        [Test]
        public void NumbersStayInRange()
        {
            var rng = new Rng(11);
            for (int i = 0; i < 1000; i++)
            {
                int n = rng.Next(3, 7);
                Assert.That(n, Is.InRange(3, 6));
            }
        }

        [Test]
        public void ShuffleKeepsEveryItem()
        {
            var items = Enumerable.Range(0, 30).ToList();
            new Rng(4).Shuffle(items);
            Assert.That(items, Is.EquivalentTo(Enumerable.Range(0, 30)));
            Assert.That(items, Is.Not.EqualTo(Enumerable.Range(0, 30)));
        }
    }
}

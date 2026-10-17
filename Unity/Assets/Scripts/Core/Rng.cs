using System;
using System.Collections.Generic;

namespace WhereDarknessDwells.Core
{
    /// <summary>
    /// The game's only source of randomness: a small seeded generator (SplitMix64) that gives the same numbers on
    /// every platform. A run has one seed; each system takes its own named stream so, for example, opening an extra
    /// event can't change which cards a later combat draws.
    /// </summary>
    public sealed class Rng
    {
        readonly ulong _seed;
        ulong _state;

        public Rng(ulong seed) : this(seed, seed) { }

        Rng(ulong seed, ulong state)
        {
            _seed = seed;
            _state = state;
        }

        /// <summary>The current position, so a saved run can carry on with exactly the same numbers.</summary>
        public ulong State => _state;

        public static Rng Restore(ulong seed, ulong state) => new Rng(seed, state);

        /// <summary>
        /// An independent generator for one system, such as "map" or "combat". The same seed and name always give
        /// the same stream, however much this generator has been used.
        /// </summary>
        public Rng Stream(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("A stream needs a name.", nameof(name));
            ulong hash = 14695981039346656037UL; // FNV-1a
            foreach (char c in name)
            {
                hash ^= c;
                hash *= 1099511628211UL;
            }
            return new Rng(Mix(_seed ^ hash));
        }

        /// <summary>A number from 0 up to but not including <paramref name="maxExclusive"/>.</summary>
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return (int)(NextULong() % (ulong)maxExclusive);
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            return minInclusive + Next(maxExclusive - minInclusive);
        }

        public void Shuffle<T>(IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T t = list[i];
                list[i] = list[j];
                list[j] = t;
            }
        }

        ulong NextULong()
        {
            _state += 0x9E3779B97F4A7C15UL;
            return Mix(_state);
        }

        static ulong Mix(ulong z)
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }
}

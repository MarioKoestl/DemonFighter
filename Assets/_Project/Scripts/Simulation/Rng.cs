#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// The only source of randomness in the simulation: a seeded SplitMix64 generator owned by the run, so the same
    /// seed and the same commands always produce the same run (ARCHITECTURE, "Deterministic given seed plus commands").
    /// Own implementation instead of System.Random, so the sequence is identical on every runtime and version.
    /// </summary>
    public sealed class Rng
    {
        private const ulong Gamma = 0x9E3779B97F4A7C15UL;
        private const float UnitFromTwentyFourBits = 1f / 16777216f;

        private ulong _state;

        /// <summary>Starts the sequence that belongs to the seed.</summary>
        public Rng(int seed)
            : this(seed, unchecked((ulong)(long)seed))
        {
        }

        /// <summary>Restores a generator mid-sequence, for resuming a saved run.</summary>
        public Rng(int seed, ulong state)
        {
            Seed = seed;
            _state = state;
        }

        /// <summary>The seed the run started from; shown to the player and shareable (GAME_DESIGN, "Seeds").</summary>
        public int Seed { get; }

        /// <summary>Position in the sequence; store it with the run to resume the exact same sequence.</summary>
        public ulong State => _state;

        /// <summary>Next 64 random bits.</summary>
        public ulong NextUInt64()
        {
            unchecked
            {
                _state += Gamma;
                ulong z = _state;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>Next 32 random bits.</summary>
        public uint NextUInt32() => (uint)(NextUInt64() >> 32);

        /// <summary>Integer in [minInclusive, maxExclusive), uniform without modulo bias.</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (minInclusive >= maxExclusive)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    maxExclusive,
                    "maxExclusive must be greater than minInclusive.");
            }

            uint range = unchecked((uint)((long)maxExclusive - minInclusive));
            uint x = NextUInt32();
            ulong product = (ulong)x * range;
            uint low = unchecked((uint)product);
            if (low < range)
            {
                // Lemire's rejection step: redraw while the low word falls into the biased remainder.
                uint threshold = unchecked(0u - range) % range;
                while (low < threshold)
                {
                    x = NextUInt32();
                    product = (ulong)x * range;
                    low = unchecked((uint)product);
                }
            }

            return unchecked((int)(minInclusive + (long)(product >> 32)));
        }

        /// <summary>Float in [0, 1) with 24 bits of resolution.</summary>
        public float NextFloat() => (NextUInt64() >> 40) * UnitFromTwentyFourBits;

        /// <summary>Float in [minInclusive, maxExclusive); rounding can make the upper bound reachable for tiny ranges.</summary>
        public float NextFloat(float minInclusive, float maxExclusive)
        {
            if (!(minInclusive < maxExclusive))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maxExclusive),
                    maxExclusive,
                    "maxExclusive must be greater than minInclusive.");
            }

            return minInclusive + NextFloat() * (maxExclusive - minInclusive);
        }

        /// <summary>True with the given probability; 0 is never true and 1 is always true without consuming randomness.</summary>
        public bool NextBool(float probability)
        {
            if (probability < 0f || probability > 1f || float.IsNaN(probability))
            {
                throw new ArgumentOutOfRangeException(nameof(probability), probability, "Probability must be in [0, 1].");
            }

            if (probability <= 0f)
            {
                return false;
            }

            if (probability >= 1f)
            {
                return true;
            }

            return NextFloat() < probability;
        }
    }
}

#nullable enable
using System;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Everything that exists in one run. Owned by the simulation and changed only through ticks and commands, so it
    /// can be saved whole and rebuilt from the seed plus the command stream (ARCHITECTURE, "The simulation").
    /// In M0 it holds the seed, the clock, the random source and the id sequences; entities follow in M1 and M2.
    /// </summary>
    public sealed class RunState
    {
        /// <summary>Starts a run from a seed with the default tick rate.</summary>
        public RunState(int seed)
            : this(seed, SimulationConfig.Default)
        {
        }

        /// <summary>Starts a run from a seed with an explicit tick rate.</summary>
        public RunState(int seed, SimulationConfig config)
        {
            Seed = seed;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Rng = new Rng(seed);
            DemonIds = new IdSequence();
            FoodIds = new IdSequence();
        }

        /// <summary>The seed this run was generated from; same seed and same commands give the same run.</summary>
        public int Seed { get; }

        /// <summary>Tick timing of this run.</summary>
        public SimulationConfig Config { get; }

        /// <summary>The only random source of this run.</summary>
        public Rng Rng { get; }

        /// <summary>Issues demon ids; issued here so they stay unique for the whole run.</summary>
        public IdSequence DemonIds { get; }

        /// <summary>Issues food ids; issued here so they stay unique for the whole run.</summary>
        public IdSequence FoodIds { get; }

        /// <summary>Number of fixed steps applied since the run started.</summary>
        public long Tick { get; private set; }

        /// <summary>Simulated seconds elapsed, derived from the tick count so it can never drift from it.</summary>
        public float Time => Tick * Config.TickSeconds;

        /// <summary>Moves the clock forward by one step; only the ticker calls this, nothing else moves time.</summary>
        internal void AdvanceTick()
        {
            Tick++;
        }
    }
}

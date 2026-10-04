#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Everything that exists in one run. Owned by the simulation and changed only through ticks and commands, so it
    /// can be saved whole and rebuilt from the seed plus the command stream (ARCHITECTURE, "The simulation").
    /// Holds the seed, the clock, the random source, the id sequences and the demons; food joins in M2.
    /// </summary>
    public sealed class RunState
    {
        private readonly Dictionary<DemonId, Demon> _demonsById = new Dictionary<DemonId, Demon>();
        private readonly List<Demon> _demons = new List<Demon>();

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

        /// <summary>The generated world, attached once at run start; null only while the run is being set up.</summary>
        public WorldLayout? World { get; private set; }

        /// <summary>Every demon in this run in spawn order; iterate by index, the list never allocates.</summary>
        public IReadOnlyList<Demon> Demons => _demons;

        /// <summary>Attaches the world generated from this run's seed; a second world or a foreign seed is a bug.</summary>
        public void AttachWorld(WorldLayout world)
        {
            if (world == null)
            {
                throw new ArgumentNullException(nameof(world));
            }

            if (World != null)
            {
                throw new InvalidOperationException("The run already has a world.");
            }

            if (world.Seed != Seed)
            {
                throw new ArgumentException("The world was generated from a different seed than the run.", nameof(world));
            }

            World = world;
        }

        /// <summary>
        /// Brings a demon into the run with an id this run issued. It starts standing still at the given feet
        /// position; the layout decides where that is.
        /// </summary>
        public Demon SpawnDemon(ControllerKind controller, DemonTemplate template, Vector3 position, float yaw)
        {
            var demon = new Demon(new DemonId(DemonIds.Next()), controller, template, position, yaw);
            _demonsById.Add(demon.Id, demon);
            _demons.Add(demon);
            return demon;
        }

        /// <summary>Looks a demon up by id; false for ids this run never issued.</summary>
        public bool TryGetDemon(DemonId id, [MaybeNullWhen(false)] out Demon demon)
        {
            return _demonsById.TryGetValue(id, out demon);
        }

        /// <summary>Moves the clock forward by one step; only the ticker calls this, nothing else moves time.</summary>
        internal void AdvanceTick()
        {
            Tick++;
        }
    }
}

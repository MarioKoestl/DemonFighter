#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Food;
using DemonFighter.Simulation.Hazards;
using DemonFighter.Simulation.Worldgen;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// Everything that exists in one run. Owned by the simulation and changed only through ticks and commands, so it
    /// can be saved whole and rebuilt from the seed plus the command stream (ARCHITECTURE, "The simulation").
    /// Holds the seed, the clock, the random source, the id sequences, the content and the demons.
    /// </summary>
    public sealed class RunState
    {
        private readonly Dictionary<DemonId, Demon> _demonsById = new Dictionary<DemonId, Demon>();
        private readonly List<Demon> _demons = new List<Demon>();
        private readonly Dictionary<FoodId, FoodItem> _foodById = new Dictionary<FoodId, FoodItem>();
        private readonly List<FoodItem> _food = new List<FoodItem>();

        /// <summary>Starts a run from a seed with the content every spec is looked up in.</summary>
        public RunState(int seed, SimulationConfig config, ContentCatalog catalog)
        {
            Seed = seed;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Rng = new Rng(seed);
            DemonIds = new IdSequence();
            FoodIds = new IdSequence();
        }

        /// <summary>Resumes a run from saved values (D-073); demons and food follow through RestoreDemon and RestoreFood.</summary>
        internal RunState(int seed, SimulationConfig config, ContentCatalog catalog, ulong rngState, int lastDemonId, int lastFoodId, long tick)
        {
            if (tick < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(tick), tick, "The tick is never negative.");
            }

            Seed = seed;
            Config = config ?? throw new ArgumentNullException(nameof(config));
            Catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));
            Rng = new Rng(seed, rngState);
            DemonIds = new IdSequence(lastDemonId);
            FoodIds = new IdSequence(lastFoodId);
            Tick = tick;
        }

        /// <summary>The seed this run was generated from; same seed and same commands give the same run.</summary>
        public int Seed { get; }

        /// <summary>Tick timing of this run.</summary>
        public SimulationConfig Config { get; }

        /// <summary>The immutable content this run plays with.</summary>
        public ContentCatalog Catalog { get; }

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

        /// <summary>Run-wide pressure rising with time (GAME_DESIGN, "The run"; D-016, D-069): levels per minute from the biome, capped; zero until the world is attached.</summary>
        public float Threat => World == null ? 0f : MathF.Min(World.Biome.ThreatMaxLevel, Time / 60f * World.Biome.ThreatPerMinute);

        /// <summary>The whole threat level that spawns and the HUD read.</summary>
        public int ThreatLevel => (int)MathF.Floor(Threat + 0.0001f);

        /// <summary>The generated world, attached once at run start; null only while the run is being set up.</summary>
        public WorldLayout? World { get; private set; }

        /// <summary>Where the world burns (D-086), built with the world; empty until it is attached.</summary>
        public HazardMap Hazards { get; private set; } = HazardMap.Empty;

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
            Hazards = HazardMap.From(world);
        }

        /// <summary>
        /// Brings a demon of the given kind into the run with an id this run issued. It starts standing still at the
        /// given feet position with a fresh body; the layout decides where that is.
        /// </summary>
        public Demon SpawnDemon(ControllerKind controller, DemonSpec spec, Vector3 position, float yaw)
        {
            var demon = new Demon(new DemonId(DemonIds.Next()), controller, spec, Catalog, position, yaw);
            demon.ApplyStartingPackage(Rng);
            _demonsById.Add(demon.Id, demon);
            _demons.Add(demon);
            return demon;
        }

        /// <summary>Brings a saved demon back with its saved id, without the starting package of its kind (D-073).</summary>
        internal Demon RestoreDemon(DemonId id, ControllerKind controller, DemonSpec spec, Vector3 position, float yaw)
        {
            var demon = new Demon(id, controller, spec, Catalog, position, yaw);
            _demonsById.Add(demon.Id, demon);
            _demons.Add(demon);
            return demon;
        }

        /// <summary>Puts saved food back with its saved id and remaining decay time (D-073).</summary>
        internal FoodItem RestoreFood(FoodId id, FoodKind kind, Vector3 position, float biomass, DemonId source, int sourceTier, long decayTicksLeft)
        {
            var food = new FoodItem(id, kind, position, biomass, source, sourceTier, decayTicksLeft);
            _foodById.Add(food.Id, food);
            _food.Add(food);
            return food;
        }

        /// <summary>Looks a demon up by id; false for ids this run never issued.</summary>
        public bool TryGetDemon(DemonId id, [MaybeNullWhen(false)] out Demon demon)
        {
            return _demonsById.TryGetValue(id, out demon);
        }

        /// <summary>Every food item lying in the world, oldest first; iterate by index.</summary>
        public IReadOnlyList<FoodItem> Food => _food;

        /// <summary>Looks a food item up by id; false once it was eaten or decayed.</summary>
        public bool TryGetFood(FoodId id, [MaybeNullWhen(false)] out FoodItem food)
        {
            return _foodById.TryGetValue(id, out food);
        }

        /// <summary>Puts food into the world with a fresh id and the biome's decay time.</summary>
        internal FoodItem SpawnFood(FoodKind kind, Vector3 position, float biomass, DemonId source, int sourceTier)
        {
            long decayTicks = Config.TicksFor(Catalog.Tuning.FoodDecaySeconds);
            var food = new FoodItem(new FoodId(FoodIds.Next()), kind, position, biomass, source, sourceTier, decayTicks);
            _foodById.Add(food.Id, food);
            _food.Add(food);
            return food;
        }

        /// <summary>Takes food out of the world after it was eaten up or rotted away.</summary>
        internal bool RemoveFood(FoodId id)
        {
            if (!_foodById.Remove(id, out FoodItem food))
            {
                return false;
            }

            _food.Remove(food);
            return true;
        }

        /// <summary>Moves the clock forward by one step; only the ticker calls this, nothing else moves time.</summary>
        internal void AdvanceTick()
        {
            Tick++;
        }
    }
}

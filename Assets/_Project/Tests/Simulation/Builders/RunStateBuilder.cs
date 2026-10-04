#nullable enable
using DemonFighter.Simulation.Content;
using DemonFighter.Simulation.Tests.Content;

namespace DemonFighter.Simulation.Tests.Builders
{
    /// <summary>
    /// Builds a <see cref="RunState"/> for tests, so tests read like design statements and defaults live in one place.
    /// </summary>
    internal sealed class RunStateBuilder
    {
        public const int DefaultSeed = 12345;

        private int _seed = DefaultSeed;
        private SimulationConfig _config = SimulationConfig.Default;
        private ContentCatalog? _catalog;

        public RunStateBuilder WithSeed(int seed)
        {
            _seed = seed;
            return this;
        }

        public RunStateBuilder WithTicksPerSecond(int ticksPerSecond)
        {
            _config = new SimulationConfig(ticksPerSecond);
            return this;
        }

        public RunStateBuilder WithCatalog(ContentCatalog catalog)
        {
            _catalog = catalog;
            return this;
        }

        public RunState Build()
        {
            return new RunState(_seed, _config, _catalog ?? TestContent.Catalog());
        }
    }
}

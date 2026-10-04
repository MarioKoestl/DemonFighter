#nullable enable
namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// Produces a world from a seed and a biome. Same seed and biome give the same layout, every time, on every
    /// machine; that is what makes seeds shareable (GAME_DESIGN, "Seeds").
    /// </summary>
    public interface IWorldGenerator
    {
        /// <summary>Generates the complete layout; throws for invalid biome content.</summary>
        WorldLayout Generate(int seed, BiomeSpec biome);
    }
}

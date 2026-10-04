#nullable enable
namespace DemonFighter.Simulation.Worldgen
{
    /// <summary>
    /// The terrain features of the v1 cavern (GAME_DESIGN, "World generation"). Each is placed as data; Presentation
    /// decides how it looks.
    /// </summary>
    public enum FeatureKind
    {
        Rock,
        Fissure,
        LavaPool,
        WaterPool,
        BonePile,
    }
}

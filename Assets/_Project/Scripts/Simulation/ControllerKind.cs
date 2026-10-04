#nullable enable
namespace DemonFighter.Simulation
{
    /// <summary>
    /// Who sends a demon's commands. The simulation treats both the same; the kind only exists so Presentation and
    /// Input know which demon to bind to the player (GAME_DESIGN, "Everyone plays by the same rules").
    /// </summary>
    public enum ControllerKind
    {
        Player,
        Ai,
    }
}

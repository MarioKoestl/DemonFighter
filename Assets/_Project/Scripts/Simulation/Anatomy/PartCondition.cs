#nullable enable
namespace DemonFighter.Simulation.Anatomy
{
    /// <summary>
    /// The three states a part shows (GAME_DESIGN, "Visible damage and gore"): intact, visibly wounded, or gone.
    /// Lost parts are severed or destroyed depending on their spec and never regenerate (D-024).
    /// </summary>
    public enum PartCondition
    {
        Healthy,
        Wounded,
        Lost,
    }
}

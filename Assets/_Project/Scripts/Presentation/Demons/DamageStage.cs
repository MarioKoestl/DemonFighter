#nullable enable
namespace DemonFighter.Presentation.Demons
{
    /// <summary>
    /// The look a body part shows for its health (GAME_DESIGN, "Visible damage and gore", stage 2): a view-side
    /// refinement of the simulation's Healthy, Wounded and Lost (D-080). Mesh sets carry one mesh per stage;
    /// primitives shrink and darken instead.
    /// </summary>
    public enum DamageStage
    {
        Intact,
        Wounded,
        Mangled,
        Lost,
    }
}

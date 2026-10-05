#nullable enable
namespace DemonFighter.Data
{
    /// <summary>
    /// How the view moves a body part procedurally (D-082): legs step with the gait, limbs swing and strike, jaws
    /// open and snap on a bite, tails sway and swing. Content data on the part asset, so a new part picks a motion
    /// without a code change; a part with a clip of its own plays that instead.
    /// </summary>
    public enum PartMotion
    {
        None,
        Legs,
        Limb,
        Jaws,
        Tail,
    }
}

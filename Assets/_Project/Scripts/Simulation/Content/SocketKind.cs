#nullable enable
namespace DemonFighter.Simulation.Content
{
    /// <summary>
    /// Where a part plugs into the body (GAME_DESIGN, "The body: parts and sockets"). The core is the root; it has
    /// no socket of its own but owns all the others.
    /// </summary>
    public enum SocketKind
    {
        Core,
        Head,
        Limb,
        Locomotion,
        Hide,
        Tail,
    }
}

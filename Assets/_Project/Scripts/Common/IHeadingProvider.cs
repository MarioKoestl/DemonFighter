#nullable enable
namespace DemonFighter.Common
{
    /// <summary>
    /// Something that knows which way is forward for the player: the camera rig. Input turns WASD into a world
    /// direction with it, and the body follows it in first person.
    /// </summary>
    public interface IHeadingProvider
    {
        /// <summary>Yaw in radians around the up axis, zero toward north, clockwise positive.</summary>
        float YawRadians { get; }
    }
}

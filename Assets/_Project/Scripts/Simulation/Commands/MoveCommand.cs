#nullable enable
using System.Numerics;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Asks a demon to move on the ground plane. Applied every tick while the key is held; a zero direction stops.
    /// C# 9 has no record structs, so commands are readonly structs with explicit constructors.
    /// </summary>
    public readonly struct MoveCommand : ICommand
    {
        public MoveCommand(DemonId actor, Vector2 direction, bool sprint)
        {
            Actor = actor;
            Direction = direction;
            Sprint = sprint;
        }

        /// <inheritdoc />
        public DemonId Actor { get; }

        /// <summary>World ground-plane direction, X east and Y north; clamped to unit length when applied.</summary>
        public Vector2 Direction { get; }

        /// <summary>True while sprinting.</summary>
        public bool Sprint { get; }
    }
}

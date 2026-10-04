#nullable enable
using System.Numerics;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// What a demon is trying to do with its legs this tick: a direction on the ground plane with length at most one
    /// (keyboard gives one, an analog stick less), whether it sprints, and optionally a direction to face while it
    /// stands or moves. Set by a move command, read by the movement system and by the view that drives the Unity body.
    /// </summary>
    public readonly struct MovementIntent
    {
        /// <summary>Below this input length the demon stands still, so drifting sticks do not creep.</summary>
        private const float DeadZone = 0.0001f;

        /// <summary>Standing still: the default value of the struct.</summary>
        public static readonly MovementIntent None = default;

        /// <summary>
        /// Clamps the direction to unit length; X is east and Y is north on the ground plane (Unity X and Z). A zero
        /// facing means the demon faces the way it moves.
        /// </summary>
        public MovementIntent(Vector2 direction, bool sprint, Vector2 facing = default)
        {
            float length = direction.Length();
            if (length > 1f)
            {
                direction /= length;
            }

            Direction = length > DeadZone ? direction : Vector2.Zero;
            Sprint = sprint;
            float facingLength = facing.Length();
            Facing = facingLength > DeadZone ? facing / facingLength : Vector2.Zero;
        }

        /// <summary>Ground-plane direction with length at most one; zero when standing still.</summary>
        public Vector2 Direction { get; }

        /// <summary>True while the sprint key is held; costs stamina from M2 on (D-023).</summary>
        public bool Sprint { get; }

        /// <summary>Unit direction to face regardless of movement, for standing attacks; zero faces the movement.</summary>
        public Vector2 Facing { get; }

        /// <summary>True when the direction has any length.</summary>
        public bool IsMoving => Direction.LengthSquared() > 0f;

        /// <summary>True when a facing was given.</summary>
        public bool HasFacing => Facing.LengthSquared() > 0f;
    }
}

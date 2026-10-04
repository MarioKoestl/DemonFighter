#nullable enable
using System;
using System.Numerics;

namespace DemonFighter.Simulation
{
    /// <summary>
    /// One demon in a run, player or AI alike (ARCHITECTURE, "Entities"). Holds where it is, what body it has and
    /// what it is trying to do; changed only by the simulation systems and by the pose the view writes back after
    /// physics. Stats, body parts and skills join in M2 and M3.
    /// </summary>
    public sealed class Demon
    {
        internal Demon(DemonId id, ControllerKind controller, DemonTemplate template, Vector3 position, float yaw)
        {
            if (!id.IsValid)
            {
                throw new ArgumentException("A demon needs an issued id.", nameof(id));
            }

            Id = id;
            Controller = controller;
            Template = template ?? throw new ArgumentNullException(nameof(template));
            SizeMeters = template.SizeMeters;
            Position = position;
            Yaw = yaw;
        }

        /// <summary>Run-wide identity, issued by the run.</summary>
        public DemonId Id { get; }

        /// <summary>Who sends this demon's commands.</summary>
        public ControllerKind Controller { get; }

        /// <summary>The body this demon was spawned with.</summary>
        public DemonTemplate Template { get; }

        /// <summary>Current body height in meters; grows with evolutions later, so it is state, not spec.</summary>
        public float SizeMeters { get; private set; }

        /// <summary>Tier label shown in the HUD and used for placeholder colors.</summary>
        public int Tier => Template.Tier;

        /// <summary>Feet position in world space, X east, Y up, Z north.</summary>
        public Vector3 Position { get; private set; }

        /// <summary>Facing angle in radians around the up axis, zero toward north, increasing clockwise (Unity yaw).</summary>
        public float Yaw { get; private set; }

        /// <summary>What the demon is trying to do with its legs this tick.</summary>
        public MovementIntent Intent { get; private set; }

        /// <summary>
        /// True while a Unity body moves this demon. The simulation then stops integrating its position and trusts
        /// the pose the view writes back; without a body (tests, a headless server) it integrates itself.
        /// </summary>
        public bool HasBody { get; private set; }

        /// <summary>Top speed for the current intent: walking speed, times the sprint factor while sprinting.</summary>
        public float MaxSpeed => Intent.Sprint ? Template.MoveSpeed * Template.SprintMultiplier : Template.MoveSpeed;

        /// <summary>Ground-plane velocity the body should move with: intent direction scaled by the top speed.</summary>
        public Vector3 Velocity
        {
            get
            {
                Vector2 planar = Intent.Direction * MaxSpeed;
                return new Vector3(planar.X, 0f, planar.Y);
            }
        }

        /// <summary>The direction the demon faces on the ground plane, derived from the yaw.</summary>
        public Vector2 FacingDirection => new Vector2(MathF.Sin(Yaw), MathF.Cos(Yaw));

        /// <summary>
        /// Overwrites position and facing with what the Unity body ended up with after collisions. The simulation
        /// trusts the view here (ARCHITECTURE, "Movement, collision and hits"); nothing else may call it.
        /// </summary>
        public void SetPose(Vector3 position, float yaw)
        {
            Position = position;
            Yaw = yaw;
        }

        /// <summary>Yaw that faces the given ground-plane direction; the current yaw when the direction is zero.</summary>
        public float YawToward(Vector2 direction)
        {
            return direction.LengthSquared() > 0f ? MathF.Atan2(direction.X, direction.Y) : Yaw;
        }

        /// <summary>Called by the view that takes over moving this demon; a second body is a wiring bug.</summary>
        public void AttachBody()
        {
            if (HasBody)
            {
                throw new InvalidOperationException(Id + " already has a body.");
            }

            HasBody = true;
        }

        /// <summary>Called by the view when it is destroyed; the simulation integrates the demon again.</summary>
        public void DetachBody()
        {
            HasBody = false;
        }

        internal void SetIntent(MovementIntent intent)
        {
            Intent = intent;
        }

        /// <summary>Advances position along the intent and turns the body toward it; standing still changes nothing.</summary>
        internal void Integrate(float seconds)
        {
            if (!Intent.IsMoving)
            {
                return;
            }

            Position += Velocity * seconds;
            Yaw = YawToward(Intent.Direction);
        }
    }
}

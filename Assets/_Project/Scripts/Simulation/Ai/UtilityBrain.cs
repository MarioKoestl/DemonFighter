#nullable enable
using System;
using System.Collections.Generic;
using System.Numerics;
using DemonFighter.Simulation.Commands;

namespace DemonFighter.Simulation.Ai
{
    /// <summary>
    /// Decides what one AI demon does and expresses it as the same move commands a player sends. Goals are picked by
    /// weighted chance from the archetype whenever the current goal completes, so demons with the same archetype
    /// still behave differently while the whole run stays deterministic for its seed (ARCHITECTURE, "AI").
    /// </summary>
    public sealed class UtilityBrain
    {
        private readonly GroundBounds _bounds;
        private readonly IReadOnlyList<Vector3>? _route;
        private long _restUntilTick;

        /// <summary>Creates a brain for one AI demon; the route is optional and only patrolling archetypes use it.</summary>
        public UtilityBrain(Demon demon, ArchetypeSpec archetype, GroundBounds bounds, IReadOnlyList<Vector3>? route)
        {
            Demon = demon ?? throw new ArgumentNullException(nameof(demon));
            Archetype = archetype ?? throw new ArgumentNullException(nameof(archetype));
            if (demon.Controller != ControllerKind.Ai)
            {
                throw new ArgumentException("Only AI demons get a brain.", nameof(demon));
            }

            _bounds = bounds;
            _route = route != null && route.Count > 0 ? route : null;
            CurrentTarget = demon.Position;
        }

        /// <summary>The demon this brain drives.</summary>
        public Demon Demon { get; }

        /// <summary>The personality this brain follows.</summary>
        public ArchetypeSpec Archetype { get; }

        /// <summary>What the demon is doing right now.</summary>
        public AiGoal CurrentGoal { get; private set; }

        /// <summary>Where the demon is heading while wandering or patrolling.</summary>
        public Vector3 CurrentTarget { get; private set; }

        /// <summary>Index of the route waypoint being walked to.</summary>
        public int RouteIndex { get; private set; }

        /// <summary>Thinks once: finishes or keeps the current goal and submits the command that carries it out.</summary>
        public void Decide(RunState state, CommandQueue commands)
        {
            switch (CurrentGoal)
            {
                case AiGoal.None:
                    ChooseGoal(state);
                    break;
                case AiGoal.Rest:
                    if (state.Tick >= _restUntilTick)
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Wander:
                    if (HasArrived())
                    {
                        ChooseGoal(state);
                    }

                    break;
                case AiGoal.Patrol:
                    if (HasArrived())
                    {
                        RouteIndex = (RouteIndex + 1) % _route!.Count;
                        ChooseGoal(state);
                    }

                    break;
                default:
                    throw new InvalidOperationException("Unknown goal " + CurrentGoal + ".");
            }

            Act(commands);
        }

        private bool HasArrived()
        {
            Vector3 toTarget = CurrentTarget - Demon.Position;
            toTarget.Y = 0f;
            return toTarget.LengthSquared() <= Archetype.ArriveDistance * Archetype.ArriveDistance;
        }

        private void ChooseGoal(RunState state)
        {
            float patrolWeight = _route != null ? Archetype.PatrolWeight : 0f;
            float total = Archetype.WanderWeight + Archetype.RestWeight + patrolWeight;
            if (total <= 0f)
            {
                StartRest(state);
                return;
            }

            float roll = state.Rng.NextFloat(0f, total);
            if (roll < Archetype.WanderWeight)
            {
                StartWander(state);
            }
            else if (roll < Archetype.WanderWeight + Archetype.RestWeight)
            {
                StartRest(state);
            }
            else
            {
                StartPatrol();
            }
        }

        private void StartWander(RunState state)
        {
            CurrentGoal = AiGoal.Wander;
            float angle = state.Rng.NextFloat(0f, MathF.PI * 2f);
            float distance = state.Rng.NextFloat(Archetype.ArriveDistance, Archetype.WanderRadius);
            var offset = new Vector3(MathF.Sin(angle) * distance, 0f, MathF.Cos(angle) * distance);
            CurrentTarget = _bounds.Clamp(Demon.Position + offset);
        }

        private void StartRest(RunState state)
        {
            CurrentGoal = AiGoal.Rest;
            float seconds = Archetype.RestSecondsMax > Archetype.RestSecondsMin
                ? state.Rng.NextFloat(Archetype.RestSecondsMin, Archetype.RestSecondsMax)
                : Archetype.RestSecondsMin;
            _restUntilTick = state.Tick + (long)MathF.Ceiling(seconds * state.Config.TicksPerSecond);
        }

        private void StartPatrol()
        {
            CurrentGoal = AiGoal.Patrol;
            CurrentTarget = _route![RouteIndex];
        }

        private void Act(CommandQueue commands)
        {
            if (CurrentGoal == AiGoal.Rest)
            {
                commands.Submit(new MoveCommand(Demon.Id, Vector2.Zero, sprint: false));
                return;
            }

            Vector3 toTarget = CurrentTarget - Demon.Position;
            var direction = new Vector2(toTarget.X, toTarget.Z);
            float length = direction.Length();
            if (length > 0f)
            {
                direction /= length;
            }

            commands.Submit(new MoveCommand(Demon.Id, direction, sprint: false));
        }
    }
}

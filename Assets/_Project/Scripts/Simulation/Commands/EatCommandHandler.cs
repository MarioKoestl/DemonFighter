#nullable enable
using System.Numerics;
using DemonFighter.Simulation.Food;

namespace DemonFighter.Simulation.Commands
{
    /// <summary>
    /// Accepts a tick of eating when the actor is alive, free and within reach of food that still exists; the eating
    /// stage then moves the Biomass. Eat costs no stamina and only works on dead things, which food always is.
    /// </summary>
    internal sealed class EatCommandHandler : ICommandHandler<EatCommand>
    {
        internal const string UnknownActor = "Unknown actor";
        internal const string ActorDead = "Actor is dead";
        internal const string Staggered = "Staggered";
        internal const string Transforming = "Transforming";
        internal const string Held = "Held";
        internal const string Busy = "Busy with a skill";
        internal const string UnknownFood = "No such food";
        internal const string OutOfReach = "Out of reach";

        /// <inheritdoc />
        public CommandResult Handle(in EatCommand command, RunState state)
        {
            if (!state.TryGetDemon(command.Actor, out Demon? demon))
            {
                return CommandResult.Rejected(UnknownActor);
            }

            if (!demon.IsAlive)
            {
                return CommandResult.Rejected(ActorDead);
            }

            if (demon.IsTransforming(state.Tick))
            {
                return CommandResult.Rejected(Transforming);
            }

            if (demon.IsHeld(state.Tick))
            {
                return CommandResult.Rejected(Held);
            }

            if (demon.IsStaggered(state.Tick))
            {
                return CommandResult.Rejected(Staggered);
            }

            if (demon.CurrentSkillUse != null)
            {
                return CommandResult.Rejected(Busy);
            }

            if (!state.TryGetFood(command.Food, out FoodItem? food))
            {
                return CommandResult.Rejected(UnknownFood);
            }

            float reach = state.Catalog.Tuning.EatReachPerMeter * demon.SizeMeters * demon.ReachMultiplier;
            Vector3 offset = food.Position - demon.Position;
            if (new Vector2(offset.X, offset.Z).Length() > reach)
            {
                return CommandResult.Rejected(OutOfReach);
            }

            demon.RequestEat(food.Id, state.Tick);
            return CommandResult.Accepted;
        }
    }
}

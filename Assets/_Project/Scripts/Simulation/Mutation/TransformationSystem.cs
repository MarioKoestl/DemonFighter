#nullable enable
using System.Collections.Generic;
using DemonFighter.Simulation.Events;

namespace DemonFighter.Simulation.Mutation
{
    /// <summary>Ends transformations whose time is up and announces it, so the body may act and be hurt again (D-014).</summary>
    internal static class TransformationSystem
    {
        public static void Advance(RunState state, SimulationEvents events)
        {
            IReadOnlyList<Demon> demons = state.Demons;
            for (int i = 0; i < demons.Count; i++)
            {
                Demon demon = demons[i];
                if (demon.TransformingUntilTick >= 0 && state.Tick >= demon.TransformingUntilTick)
                {
                    demon.EndTransformation();
                    events.Publish(new MutationCompleted(demon.Id));
                }
            }
        }
    }
}
